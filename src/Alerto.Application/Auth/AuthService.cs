using System.Text.Json;
using Alerto.Application.Common.Exceptions;
using Alerto.Application.Common.Interfaces;
using Alerto.Domain.Entities;
using Alerto.Domain.Exceptions;
using FluentValidation;

namespace Alerto.Application.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditTrailRepository _auditTrailRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITotpService _totpService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IMachineClientValidator _machineClientValidator;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<VerifyTwoFactorRequest> _verifyTwoFactorValidator;
    private readonly IValidator<RefreshTokenRequest> _refreshValidator;
    private readonly IValidator<LogoutRequest> _logoutValidator;
    private readonly IValidator<M2MTokenRequest> _m2mValidator;
    private readonly IValidator<EnableTwoFactorRequest> _enableTwoFactorValidator;

    public AuthService(
        IUserRepository userRepository,
        IAuditTrailRepository auditTrailRepository,
        ICurrentUserService currentUserService,
        IPasswordHasher passwordHasher,
        ITotpService totpService,
        IJwtTokenService jwtTokenService,
        IMachineClientValidator machineClientValidator,
        IClock clock,
        IUnitOfWork unitOfWork,
        IValidator<LoginRequest> loginValidator,
        IValidator<VerifyTwoFactorRequest> verifyTwoFactorValidator,
        IValidator<RefreshTokenRequest> refreshValidator,
        IValidator<LogoutRequest> logoutValidator,
        IValidator<M2MTokenRequest> m2mValidator,
        IValidator<EnableTwoFactorRequest> enableTwoFactorValidator)
    {
        _userRepository = userRepository;
        _auditTrailRepository = auditTrailRepository;
        _currentUserService = currentUserService;
        _passwordHasher = passwordHasher;
        _totpService = totpService;
        _jwtTokenService = jwtTokenService;
        _machineClientValidator = machineClientValidator;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _loginValidator = loginValidator;
        _verifyTwoFactorValidator = verifyTwoFactorValidator;
        _refreshValidator = refreshValidator;
        _logoutValidator = logoutValidator;
        _m2mValidator = m2mValidator;
        _enableTwoFactorValidator = enableTwoFactorValidator;
    }

    public async Task<AuthenticationResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        await _loginValidator.ValidateAndThrowAsync(request, cancellationToken);

        var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken)
            ?? throw new UnauthorizedAccessException("Credenciales invalidas.");

        if (!user.IsActive || !_passwordHasher.VerifyPassword(user.PasswordHash, request.Password))
        {
            throw new UnauthorizedAccessException("Credenciales invalidas.");
        }

        if (user.IsTwoFactorEnabled)
        {
            var twoFactorToken = _jwtTokenService.CreateTwoFactorToken(user.Id, user.Username, user.Role.ToString());

            await AppendAuditAsync(user.Id, "LoginChallenged", user.Id, new { user.Username }, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new AuthenticationResponse(
                "Bearer",
                user.Username,
                user.Role.ToString(),
                true,
                null,
                null,
                null,
                null,
                twoFactorToken.Token,
                twoFactorToken.ExpiresAtUtc);
        }

        return await IssueSessionAsync(user, "pwd", "UserLoggedIn", cancellationToken);
    }

    public async Task<AuthenticationResponse> VerifyTwoFactorAsync(VerifyTwoFactorRequest request, CancellationToken cancellationToken)
    {
        await _verifyTwoFactorValidator.ValidateAndThrowAsync(request, cancellationToken);

        var ticketPayload = _jwtTokenService.ValidateTwoFactorToken(request.TwoFactorToken)
            ?? throw new UnauthorizedAccessException("El ticket de verificacion 2FA no es valido o expiro.");

        var user = await _userRepository.GetByIdAsync(ticketPayload.UserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("No se encontro el usuario asociado al proceso 2FA.");

        if (!user.IsActive || !user.IsTwoFactorEnabled || string.IsNullOrWhiteSpace(user.TotpSecret))
        {
            throw new UnauthorizedAccessException("El usuario no tiene 2FA habilitado.");
        }

        if (!_totpService.ValidateCode(user.TotpSecret, request.Code))
        {
            throw new UnauthorizedAccessException("El codigo TOTP es invalido.");
        }

        return await IssueSessionAsync(user, "pwd+mfa", "UserLoggedInWith2Fa", cancellationToken);
    }

    public async Task<AuthenticationResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await _refreshValidator.ValidateAndThrowAsync(request, cancellationToken);

        var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken, cancellationToken)
            ?? throw new UnauthorizedAccessException("Refresh token invalido.");

        var existingRefreshToken = user.GetActiveRefreshToken(request.RefreshToken);
        existingRefreshToken.Revoke(_clock.UtcNow);

        return await IssueSessionAsync(
            user,
            user.IsTwoFactorEnabled ? "refresh+mfa" : "refresh",
            "RefreshTokenRotated",
            cancellationToken);
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken)
    {
        await _logoutValidator.ValidateAndThrowAsync(request, cancellationToken);

        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
        {
            throw new UnauthorizedAccessException("La operacion requiere un usuario autenticado.");
        }

        var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken, cancellationToken);
        if (user is null)
        {
            return;
        }

        if (user.Id != _currentUserService.UserId.Value)
        {
            throw new UnauthorizedAccessException("El refresh token no pertenece al usuario autenticado.");
        }

        var refreshToken = user.RefreshTokens.SingleOrDefault(token => token.Token == request.RefreshToken);
        if (refreshToken is null || !refreshToken.IsActiveAt(_clock.UtcNow))
        {
            return;
        }

        refreshToken.Revoke(_clock.UtcNow);
        await AppendAuditAsync(user.Id, "UserLoggedOut", user.Id, new { user.Username }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<AuthenticationResponse> RequestMachineTokenAsync(M2MTokenRequest request, CancellationToken cancellationToken)
    {
        await _m2mValidator.ValidateAndThrowAsync(request, cancellationToken);

        if (!_machineClientValidator.IsValid(request.ClientId, request.ClientSecret, out var clientDefinition) ||
            clientDefinition is null)
        {
            throw new UnauthorizedAccessException("Credenciales M2M invalidas.");
        }

        var token = _jwtTokenService.CreateMachineToken(
            clientDefinition.ClientId,
            clientDefinition.Role,
            clientDefinition.Scope);

        await AppendAuditAsync(
            Guid.Empty,
            "MachineTokenIssued",
            Guid.Empty,
            new { clientDefinition.ClientId, clientDefinition.Scope },
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthenticationResponse(
            "Bearer",
            clientDefinition.ClientId,
            clientDefinition.Role,
            false,
            token.Token,
            token.ExpiresAtUtc,
            null,
            null,
            null,
            null);
    }

    public async Task<TwoFactorSetupResponse> SetupTwoFactorAsync(CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        var secret = _totpService.GenerateSecret();
        user.ProvisionTwoFactor(secret, _clock.UtcNow);

        var provisioningUri = _totpService.BuildProvisioningUri("Alerto API", user.Username, secret);
        await AppendAuditAsync(user.Id, "TwoFactorProvisioned", user.Id, new { user.Username }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TwoFactorSetupResponse(secret, provisioningUri, user.IsTwoFactorEnabled);
    }

    public async Task EnableTwoFactorAsync(EnableTwoFactorRequest request, CancellationToken cancellationToken)
    {
        await _enableTwoFactorValidator.ValidateAndThrowAsync(request, cancellationToken);
        var user = await GetCurrentUserAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(user.TotpSecret))
        {
            throw new DomainRuleException("Debe provisionar primero el secreto TOTP.");
        }

        if (!_totpService.ValidateCode(user.TotpSecret, request.Code))
        {
            throw new UnauthorizedAccessException("El codigo TOTP no es valido.");
        }

        user.EnableTwoFactor(_clock.UtcNow);
        await AppendAuditAsync(user.Id, "TwoFactorEnabled", user.Id, new { user.Username }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
        {
            throw new UnauthorizedAccessException("La operacion requiere un usuario autenticado.");
        }

        return await _userRepository.GetByIdAsync(_currentUserService.UserId.Value, cancellationToken)
            ?? throw new NotFoundException("No se encontro el usuario autenticado.");
    }

    private async Task<AuthenticationResponse> IssueSessionAsync(
        User user,
        string authenticationMethod,
        string auditAction,
        CancellationToken cancellationToken)
    {
        var accessToken = _jwtTokenService.CreateUserToken(user.Id, user.Username, user.Role.ToString(), authenticationMethod);
        var refreshTokenExpiresAtUtc = _clock.UtcNow.AddDays(7);
        var refreshToken = RefreshToken.Issue(
            user.Id,
            _jwtTokenService.GenerateRefreshToken(),
            refreshTokenExpiresAtUtc,
            _currentUserService.ClientIp);

        user.AddRefreshToken(refreshToken, _clock.UtcNow);
        await _userRepository.AddRefreshTokenAsync(refreshToken, cancellationToken);
        await AppendAuditAsync(user.Id, auditAction, user.Id, new { user.Username }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthenticationResponse(
            "Bearer",
            user.Username,
            user.Role.ToString(),
            false,
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            refreshToken.Token,
            refreshTokenExpiresAtUtc,
            null,
            null);
    }

    private async Task AppendAuditAsync(
        Guid actorId,
        string action,
        Guid entityId,
        object details,
        CancellationToken cancellationToken)
    {
        var audit = AuditLog.Create(
            actorId,
            action,
            nameof(User),
            entityId,
            JsonSerializer.Serialize(details),
            _currentUserService.TraceId,
            _clock.UtcNow);

        await _auditTrailRepository.AddAsync(audit, cancellationToken);
    }
}
