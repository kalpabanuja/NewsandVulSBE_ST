using Microsoft.EntityFrameworkCore;
using NewsAndVulBackend.Core.Entities;

namespace NewsAndVulBackend.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Device> Devices { get; set; } = null!;
    public DbSet<StoredFile> Files { get; set; } = null!;
    public DbSet<NewsAndVulBackend.Core.Entities.FileAccess> FileAccesses { get; set; } = null!;
    public DbSet<PublicFileShare> PublicFileShares { get; set; } = null!;
    public DbSet<Document> Documents { get; set; } = null!;
    public DbSet<DocumentBlock> DocumentBlocks { get; set; } = null!;
    public DbSet<DocumentAttachment> DocumentAttachments { get; set; } = null!;
    public DbSet<PublicDocumentShare> PublicDocumentShares { get; set; } = null!;
    public DbSet<AuditEvent> AuditEvents { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<StoredFile>()
            .HasOne(f => f.OwnerUser)
            .WithMany()
            .HasForeignKey(f => f.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StoredFile>()
            .HasOne(f => f.OwnerDevice)
            .WithMany()
            .HasForeignKey(f => f.OwnerDeviceId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Document>()
            .HasOne(d => d.OwnerUser)
            .WithMany()
            .HasForeignKey(d => d.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Document>()
            .HasOne(d => d.OwnerDevice)
            .WithMany()
            .HasForeignKey(d => d.OwnerDeviceId)
            .OnDelete(DeleteBehavior.SetNull);
            
        modelBuilder.Entity<PublicFileShare>()
            .HasIndex(p => p.TokenHash)
            .IsUnique();
            
        modelBuilder.Entity<PublicDocumentShare>()
            .HasIndex(p => p.TokenHash)
            .IsUnique();
    }
}
