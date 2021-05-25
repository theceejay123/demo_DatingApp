using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
  public class LikesRepository : ILikesRepository
  {
    private readonly DataContext _context;
    public LikesRepository(DataContext context)
    {
      _context = context;
    }

    public async Task<AppUserLike> GetUserLike(int sourceUserId, int likedUserId)
    {
      return await _context.Likes.FindAsync(sourceUserId, likedUserId);
    }

    public async Task<PagedList<LikeDTO>> GetUserLikes(LikesParams likesParams)
    {
      var users = _context.Users.OrderBy(u => u.UserName).AsQueryable();
      var likes = _context.Likes.AsQueryable();


      if (likesParams.Predicate == "liked")
      {
        likes = likes.Where(x => x.SourceUserId == likesParams.UserId);
        users = likes.Select(x => x.LikedUser);
      }

      if (likesParams.Predicate == "likedBy")
      {
        likes = likes.Where(x => x.LikedUserId == likesParams.UserId);
        users = likes.Select(x => x.SourceUser);
      }

      var likedUsers = users.Select(x => new LikeDTO
      {
        UserName = x.UserName,
        KnownAs = x.KnownAs,
        Age = x.DateOfBirth.CalculateAge(),
        PhotoUrl = x.Photos.FirstOrDefault(p => p.IsMain).Url,
        City = x.City,
        Id = x.Id
      });

      return await PagedList<LikeDTO>.CreateAsync(likedUsers, likesParams.PageNumber, likesParams.PageSize);
    }

    public async Task<AppUser> GetUserWithLikes(int userId)
    {
      return await _context.Users
        .Include(x => x.LikedUsers)
        .FirstOrDefaultAsync(x => x.Id == userId);
    }
  }
}