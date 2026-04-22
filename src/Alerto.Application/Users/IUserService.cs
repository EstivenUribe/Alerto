namespace Alerto.Application.Users;

public interface IUserService
{
    Task<UserListResponse> SearchAsync(UserQueryRequest request, CancellationToken cancellationToken);
    Task<UserResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);
    Task<UserResponse> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken);
    Task<UserResponse> ActivateAsync(Guid id, ChangeUserStatusRequest request, CancellationToken cancellationToken);
    Task<UserResponse> DeactivateAsync(Guid id, ChangeUserStatusRequest request, CancellationToken cancellationToken);
}
