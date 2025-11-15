using Microsoft.EntityFrameworkCore;
using Projekt.Infrastructure.Entities;

namespace Projekt.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<EmailLog> EmailLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(128);
        });

        // Role configuration
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(32).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // UserRole configuration (many-to-many)
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId });
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Document configuration
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OriginalFilename).HasMaxLength(255).IsRequired();
            entity.Property(e => e.ContentType).HasMaxLength(127).IsRequired();
            entity.Property(e => e.StorageKey).HasMaxLength(512).IsRequired();
            entity.Property(e => e.UploadedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Owner)
                .WithMany(u => u.Documents)
                .HasForeignKey(e => e.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // EmailLog configuration
        modelBuilder.Entity<EmailLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RecipientEmail).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(32).IsRequired();
            entity.Property(e => e.ProviderMessageId).HasMaxLength(256);
            entity.Property(e => e.ErrorMessage).HasMaxLength(512);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Document)
                .WithMany(d => d.EmailLogs)
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Sender)
                .WithMany(u => u.SentEmails)
                .HasForeignKey(e => e.SenderUserId)
                .OnDelete(DeleteBehavior.NoAction); // Prevent multiple cascade paths
        });
    }
}

