using API.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
  public class DataContext : DbContext
  {
    public DataContext(DbContextOptions options) : base(options)
    {
    }
    public DbSet<AppUser> Users { get; set; }
    public DbSet<AppUserLike> Likes { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
      base.OnModelCreating(builder);
      builder.Entity<AppUserLike>()
        .HasKey(k => new { k.SourceUserId, k.LikedUserId });

      builder.Entity<AppUserLike>()
        .HasOne(s => s.SourceUser)
        .WithMany(l => l.LikedUsers)
        .HasForeignKey(s => s.SourceUserId)
        .OnDelete(DeleteBehavior.Cascade);

      builder.Entity<AppUserLike>()
        .HasOne(s => s.LikedUser)
        .WithMany(l => l.LikedByUsers)
        .HasForeignKey(s => s.LikedUserId)
        .OnDelete(DeleteBehavior.Cascade);
    }
  }
}