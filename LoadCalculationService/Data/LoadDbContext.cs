using LoadCalculationService.Models;
using Microsoft.EntityFrameworkCore;

namespace LoadCalculationService.Data;

public sealed class LoadDbContext(
    DbContextOptions<LoadDbContext> options) : DbContext(options)
{
    public DbSet<TrainingPlan> TrainingPlans => Set<TrainingPlan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var plan = modelBuilder.Entity<TrainingPlan>();
        plan.ToTable("training_plans", "load");
        plan.Property<int>("Id").ValueGeneratedOnAdd();
        plan.HasKey("Id");
    }
}