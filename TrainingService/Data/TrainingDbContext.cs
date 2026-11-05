using Microsoft.EntityFrameworkCore;
using TrainingService.Models;

namespace TrainingService.Data;

public class TrainingDbContext : DbContext
{
    public TrainingDbContext(DbContextOptions<TrainingDbContext> options)
        : base(options)
    {
    }

    public DbSet<Training> Trainings => Set<Training>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("training");

        modelBuilder.Entity<Training>().ToTable("trainings");
        modelBuilder.Entity<Exercise>().ToTable("exercises");

        modelBuilder.Entity<Training>()
            .HasMany(training => training.Exercises)
            .WithOne()
            .HasForeignKey("TrainingId")
            .IsRequired();

        var outbox = modelBuilder.Entity<OutboxMessage>();

        outbox.ToTable("outbox_messages");
        outbox.HasKey(message => message.EventId);
        outbox.Property(message => message.EventId).ValueGeneratedNever();
        outbox.Property(message => message.Payload)
            .HasColumnType("jsonb")
            .IsRequired();
    }
}