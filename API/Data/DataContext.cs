using API.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
  public class DataContext : IdentityDbContext<AppUser, AppRole, int,
    IdentityUserClaim<int>, AppUserRole, IdentityUserLogin<int>,
    IdentityRoleClaim<int>, IdentityUserToken<int>>
  {
    public DataContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<AppUserLike> Likes { get; set; }
    public DbSet<Message> Messages { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
      base.OnModelCreating(builder);

      builder.Entity<AppUser>()
        .HasMany(roles => roles.UserRoles)
        .WithOne(user => user.User)
        .HasForeignKey(roles => roles.UserId)
        .IsRequired();

      builder.Entity<AppRole>()
        .HasMany(roles => roles.UserRoles)
        .WithOne(user => user.Role)
        .HasForeignKey(roles => roles.RoleId)
        .IsRequired();

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

      builder.Entity<Message>()
        .HasOne(u => u.Recipient)
        .WithMany(m => m.MessagesRecieved)
        .OnDelete(DeleteBehavior.Restrict);

      builder.Entity<Message>()
        .HasOne(u => u.Sender)
        .WithMany(m => m.MessagesSent)
        .OnDelete(DeleteBehavior.Restrict);
    }
  }
}