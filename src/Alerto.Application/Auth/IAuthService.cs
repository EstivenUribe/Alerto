namespace Alerto.Application.Auth;

public interface IAuthService
{
    Task<AuthenticationResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AuthenticationResponse> VerifyTwoFactorAsync(VerifyTwoFactorRequest request, CancellationToken cancellationToken);
    Task<AuthenticationResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken);
    Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken);
    Task<AuthenticationResponse> RequestMachineTokenAsync(M2MTokenRequest request, CancellationToken cancellationToken);
    Task<TwoFactorSetupResponse> SetupTwoFactorAsync(CancellationToken cancellationToken);
    Task EnableTwoFactorAsync(EnableTwoFactorRequest request, CancellationToken cancellationToken);
}
