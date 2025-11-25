using MongoDB.Driver;
using ProductOrderingSystem.IdentityService.Domain.Entities;
using ProductOrderingSystem.IdentityService.Domain.Repositories;
using ProductOrderingSystem.IdentityService.Infrastructure.Data;

namespace ProductOrderingSystem.IdentityService.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _context;

    public UserRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Find(u => u.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;
            
        var normalizedEmail = email.ToLower();
        return await _context.Users
            .Find(u => u.Email.ToLower() == normalizedEmail)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
            return null;
            
        var normalizedUsername = username.ToLower();
        return await _context.Users
            .Find(u => u.Username.ToLower() == normalizedUsername)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.InsertOneAsync(user, cancellationToken: cancellationToken);
        return user;
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.ReplaceOneAsync(
            u => u.Id == user.Id,
            user,
            cancellationToken: cancellationToken);
    }

    public async Task<bool> ExistsAsync(string emailOrUsername, CancellationToken cancellationToken = default)
    {
        var count = await _context.Users
            .CountDocumentsAsync(
                u => u.Email.ToLower() == emailOrUsername.ToLower() || 
                     u.Username.ToLower() == emailOrUsername.ToLower(),
                cancellationToken: cancellationToken);

        return count > 0;
    }
}
