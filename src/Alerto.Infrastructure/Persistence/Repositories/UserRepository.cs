using Alerto.Application.Common.Interfaces;
using Alerto.Application.Common.Models;
using Alerto.Application.Users;
using Alerto.Domain.Entities;
using Alerto.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Alerto.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly AlertoDbContext _dbContext;

    public UserRepository(AlertoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        await _dbContext.Users.AddAsync(user, cancellationToken);
    }

    public async Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        await _dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Users
            .Include(x => x.RefreshTokens)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        return _dbContext.Users
            .Include(x => x.RefreshTokens)
            .SingleOrDefaultAsync(x => x.Username == username, cancellationToken);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return _dbContext.Users
            .Include(x => x.RefreshTokens)
            .SingleOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken);
    }

    public Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        return _dbContext.Users
            .Include(x => x.RefreshTokens)
            .SingleOrDefaultAsync(user => user.RefreshTokens.Any(token => token.Token == refreshToken), cancellationToken);
    }

    public Task<User?> GetFirstAdminAsync(CancellationToken cancellationToken)
    {
        return _dbContext.Users
            .Where(x => x.Role == UserRole.Admin && x.IsActive)
            .OrderBy(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResponse<User>> SearchAsync(UserQueryRequest request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Users
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Username.ToLower().Contains(search) ||
                x.DisplayName.ToLower().Contains(search) ||
                x.Email.ToLower().Contains(search));
        }

        if (request.Role.HasValue)
        {
            query = query.Where(x => x.Role == request.Role.Value);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == request.IsActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)request.PageSize);
        var items = await query
            .OrderBy(x => x.Username)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<User>(
            items,
            request.PageNumber,
            request.PageSize,
            totalCount,
            totalPages,
            request.PageNumber > 1,
            totalPages > 0 && request.PageNumber < totalPages);
    }
}
