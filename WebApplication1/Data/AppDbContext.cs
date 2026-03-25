using Microsoft.EntityFrameworkCore;
using CarPooling.Models;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace CarPooling.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Voznja> Voznje { get; set; }
    public DbSet<UsputnaStanica> UsputneStanice { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Voznja>(e =>
        {
            e.ToTable("voznje");
            e.HasKey(v => v.Id);
            e.Property(v => v.PocetniGrad).IsRequired().HasMaxLength(100);
            e.Property(v => v.KrajnjiGrad).IsRequired().HasMaxLength(100);
            e.Property(v => v.VremePolaska).IsRequired();
            e.Property(v => v.VremeDolaska).IsRequired();
        });

        modelBuilder.Entity<UsputnaStanica>(e =>
        {
            e.ToTable("usputne_stanice");
            e.HasKey(s => s.Id);
            e.Property(s => s.Stanica).IsRequired().HasMaxLength(100);
            e.Property(s => s.VremeDolaska).IsRequired();

            e.HasOne(s => s.Voznja)
             .WithMany(v => v.UsputneStanice)
             .HasForeignKey(s => s.VoznjaId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}