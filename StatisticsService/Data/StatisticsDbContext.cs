using Microsoft.EntityFrameworkCore;
using StatisticsService.Models;

namespace StatisticsService.Data;

public class StatisticsDbContext : DbContext
{
    public StatisticsDbContext(DbContextOptions<StatisticsDbContext> options)
        : base(options)
    {
    }

    public DbSet<CompletedTrainingRecord> CompletedTrainings =>
        Set<CompletedTrainingRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("statistics");

        var completedTraining = modelBuilder.Entity<CompletedTrainingRecord>();

        completedTraining.ToTable("completed_trainings");
        completedTraining.HasKey(record => record.EventId);
        completedTraining.HasIndex(record => record.UserId);
        completedTraining.Property(record => record.Payload)
            .HasColumnType("jsonb")
            .IsRequired();
    }
}