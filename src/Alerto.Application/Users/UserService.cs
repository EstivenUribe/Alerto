using System.Text.Json;
using Alerto.Application.Common.Exceptions;
using Alerto.Application.Common.Interfaces;
using Alerto.Domain.Entities;
using AutoMapper;
using FluentValidation;

namespace Alerto.Application.Users;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditTrailRepository _auditTrailRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateUserRequest> _createValidator;
    private readonly IValidator<UpdateUserRequest> _updateValidator;
    private readonly IValidator<UserQueryRequest> _queryValidator;
    private readonly IValidator<ChangeUserStatusRequest> _statusValidator;

    public UserService(
        IUserRepository userRepository,
        IAuditTrailRepository auditTrailRepository,
        ICurrentUserService currentUserService,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IClock clock,
        IMapper mapper,
        IValidator<CreateUserRequest> createValidator,
        IValidator<UpdateUserRequest> updateValidator,
        IValidator<UserQueryRequest> queryValidator,
        IValidator<ChangeUserStatusRequest> statusValidator)
    {
        _userRepository = userRepository;
        _auditTrailRepository = auditTrailRepository;
        _currentUserService = currentUserService;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _queryValidator = queryValidator;
        _statusValidator = statusValidator;
    }

    public async Task<UserListResponse> SearchAsync(UserQueryRequest request, CancellationToken cancellationToken)
    {
        await _queryValidator.ValidateAndThrowAsync(request, cancellationToken);
        var users = await _userRepository.SearchAsync(request, cancellationToken);
        var items = _mapper.Map<IReadOnlyCollection<UserResponse>>(users.Items);

        return new UserListResponse(
            items,
            users.PageNumber,
            users.PageSize,
            users.TotalCount,
            users.TotalPages,
            users.HasPreviousPage,
            users.HasNextPage);
    }

    public async Task<UserResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"No existe un usuario con id '{id}'.");

        return _mapper.Map<UserResponse>(user);
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var existing = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException("Ya existe un usuario con el mismo username.");
        }

        var existingEmail = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingEmail is not null)
        {
            throw new ConflictException("Ya existe un usuario con el mismo correo electronico.");
        }

        var user = User.Create(
            request.Username,
            request.DisplayName,
            request.Email,
            request.Role,
            _passwordHasher.HashPassword(request.Password),
            _clock.UtcNow);

        await _userRepository.AddAsync(user, cancellationToken);
        await AppendAuditAsync("UserCreated", user.Id, request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<UserResponse>(user);
    }

    public async Task<UserResponse> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        var user = await _userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"No existe un usuario con id '{id}'.");

        EnsureExpectedVersion(user.Version, request.ExpectedVersion);
        var existingEmail = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingEmail is not null && existingEmail.Id != user.Id)
        {
            throw new ConflictException("Ya existe un usuario con el mismo correo electronico.");
        }

        user.UpdateProfile(request.DisplayName, request.Email, request.Role, user.IsActive, _clock.UtcNow);

        await AppendAuditAsync("UserUpdated", user.Id, request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<UserResponse>(user);
    }

    public async Task<UserResponse> ActivateAsync(Guid id, ChangeUserStatusRequest request, CancellationToken cancellationToken)
    {
        await _statusValidator.ValidateAndThrowAsync(request, cancellationToken);
        var user = await _userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"No existe un usuario con id '{id}'.");

        EnsureExpectedVersion(user.Version, request.ExpectedVersion);
        if (user.IsActive)
        {
            throw new ConflictException("El usuario ya se encuentra activo.");
        }

        user.UpdateProfile(user.DisplayName, user.Email, user.Role, true, _clock.UtcNow);
        await AppendAuditAsync("UserActivated", user.Id, request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<UserResponse>(user);
    }

    public async Task<UserResponse> DeactivateAsync(Guid id, ChangeUserStatusRequest request, CancellationToken cancellationToken)
    {
        await _statusValidator.ValidateAndThrowAsync(request, cancellationToken);
        var user = await _userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"No existe un usuario con id '{id}'.");

        EnsureExpectedVersion(user.Version, request.ExpectedVersion);
        if (!user.IsActive)
        {
            throw new ConflictException("El usuario ya se encuentra inactivo.");
        }

        user.UpdateProfile(user.DisplayName, user.Email, user.Role, false, _clock.UtcNow);
        await AppendAuditAsync("UserDeactivated", user.Id, request, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<UserResponse>(user);
    }

    private async Task AppendAuditAsync(string action, Guid entityId, object details, CancellationToken cancellationToken)
    {
        var audit = AuditLog.Create(
            _currentUserService.UserId ?? Guid.Empty,
            action,
            nameof(User),
            entityId,
            JsonSerializer.Serialize(details),
            _currentUserService.TraceId,
            _clock.UtcNow);

        await _auditTrailRepository.AddAsync(audit, cancellationToken);
    }

    private static void EnsureExpectedVersion(int currentVersion, int expectedVersion)
    {
        if (currentVersion != expectedVersion)
        {
            throw new ConflictException("El usuario fue modificado por otro administrador. Refresque la vista y reintente.");
        }
    }
}
