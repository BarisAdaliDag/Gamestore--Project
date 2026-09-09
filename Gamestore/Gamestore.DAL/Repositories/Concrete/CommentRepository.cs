using Gamestore.DAL.Context;
using Gamestore.DAL.Repositories.Abstract;
using Gamestore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gamestore.DAL.Repositories.Concrete;

public class CommentRepository(GamestoreDbContext context)
    : Repository<Comment>(context), ICommentRepository
{
    public async Task<IEnumerable<Comment>> GetByGameIdAsync(Guid gameId)
    {
        return await DbSet
            .AsNoTracking()
            .Where(c => c.GameId == gameId)
            .OrderByDescending(c => c.CreatedAt)
            .ThenBy(c => c.Id)
            .ToListAsync();
    }
}