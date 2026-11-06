using Microsoft.EntityFrameworkCore;
using PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Model.Aggregates;
using PC212242.UPractica.Clerky.Platform.Paperwork.Infrastructure.Persistence.EFC.Configuration;

namespace PC212242.UPractica.Clerky.Platform.Shared.Infrastructure.Persistence.EFC;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public DbSet<Contract> Contracts => Set<Contract>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new ContractConfiguration());
    }
}