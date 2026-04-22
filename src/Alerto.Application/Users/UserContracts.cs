using Alerto.Application.Common.Models;
using Alerto.Domain.Enums;

namespace Alerto.Application.Users;

public sealed record CreateUserRequest(
    string Username,
    string DisplayName,
    string Email,
    string Password,
    UserRole Role);

public sealed record UpdateUserRequest(
    string DisplayName,
    string Email,
    UserRole Role,
    int ExpectedVersion);

public sealed record UserQueryRequest(
    string? Search,
    UserRole? Role,
    bool? IsActive,
    int PageNumber = 1,
    int PageSize = 20);

public sealed record ChangeUserStatusRequest(int ExpectedVersion, string? Reason);

public sealed record UserResponse(
    Guid Id,
    string Username,
    string DisplayName,
    string Email,
    string Role,
    bool IsActive,
    bool IsTwoFactorEnabled,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    int Version);

public sealed record UserListResponse(
    IReadOnlyCollection<UserResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);
