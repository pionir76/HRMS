using Microsoft.EntityFrameworkCore;
using HRMS.Modules.Equipment.Models;

namespace HRMS.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Equipment> Equipments => Set<Equipment>();
    public DbSet<Compressor> Compressors => Set<Compressor>();
    public DbSet<CompressorChannelSetting> CompressorChannelSettings => Set<CompressorChannelSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Equipment>()
            .HasIndex(e => new { e.BuildingName, e.Name })
            .IsUnique();

        modelBuilder.Entity<Compressor>()
            .HasOne<Equipment>()
            .WithMany()
            .HasForeignKey(c => c.EquipmentId);

        modelBuilder.Entity<CompressorChannelSetting>(b =>
        {
            b.HasKey(x => new { x.CompressorId, x.ChannelNo });
            b.HasOne<Compressor>()
                .WithMany()
                .HasForeignKey(x => x.CompressorId);
        });
    }
}
