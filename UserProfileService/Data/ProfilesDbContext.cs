using Microsoft.EntityFrameworkCore;
using UserProfileService.Models;

namespace UserProfileService.Data;

public sealed class ProfilesDbContext(
    DbContextOptions<ProfilesDbContext> options) : DbContext(options)
{
    public DbSet<UserProfile> Users => Set<UserProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserProfile>().ToTable("users", "profiles");
    }
}