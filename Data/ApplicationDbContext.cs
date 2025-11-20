using Microsoft.EntityFrameworkCore;
using dotnet_gs2_2025.Models;

namespace dotnet_gs2_2025.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuração compatível com MySQL e Oracle
        modelBuilder.Entity<User>(entity =>
        {
            // Nome da tabela em maiúsculas para Oracle
            entity.ToTable("USERS");
            
            entity.HasIndex(e => e.Email)
                .IsUnique();

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(e => e.Password)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Phone)
                .HasMaxLength(20);

            // Usar CURRENT_TIMESTAMP para ambos os bancos
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}
