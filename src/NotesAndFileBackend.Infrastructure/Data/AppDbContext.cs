using Microsoft.EntityFrameworkCore;
using NotesAndFileBackend.Domain.Entities;

namespace NotesAndFileBackend.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Device> Devices { get; set; } = null!;
    public DbSet<StoredFile> Files { get; set; } = null!;
    public DbSet<NotesAndFileBackend.Domain.Entities.FileAccess> FileAccesses { get; set; } = null!;
    public DbSet<PublicFileShare> PublicFileShares { get; set; } = null!;
    
    // Notes and related
    public DbSet<Note> Notes { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Tag> Tags { get; set; } = null!;
    public DbSet<NoteTag> NoteTags { get; set; } = null!;
    public DbSet<NoteRevision> NoteRevisions { get; set; } = null!;
    public DbSet<NoteLink> NoteLinks { get; set; } = null!;
    public DbSet<NoteAttachment> NoteAttachments { get; set; } = null!;
    public DbSet<PublicNoteShare> PublicNoteShares { get; set; } = null!;
    
    public DbSet<AuditEvent> AuditEvents { get; set; } = null!;

    public DbSet<CustomInteractiveTool> CustomInteractiveTools { get; set; } = null!;
    public DbSet<NoteImportJob> NoteImportJobs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.NormalizedEmail).IsUnique();
            entity.Property(u => u.RowVersion).IsRowVersion(); // Concurrency token
        });

        // StoredFile
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

        // Note
        modelBuilder.Entity<Note>(entity =>
        {
            entity.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(n => n.Device)
                .WithMany()
                .HasForeignKey(n => n.DeviceId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasOne(n => n.CreatedByUser).WithMany().HasForeignKey(n => n.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(n => n.UpdatedByUser).WithMany().HasForeignKey(n => n.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
            
            entity.Property(n => n.Title).HasMaxLength(300).IsRequired();
            entity.Property(n => n.Slug).HasMaxLength(340).IsRequired();
            entity.Property(n => n.Summary).HasMaxLength(1000);
            entity.Property(n => n.ToolName).HasMaxLength(120);
            
            entity.Property(n => n.ContentJsonb).HasColumnType("jsonb").IsRequired();
            
            // Concurrency token
            entity.Property(n => n.Version).IsConcurrencyToken();
            
            // Check constraint for Title
            entity.ToTable(t => t.HasCheckConstraint("CK_Note_TitleLength", "char_length(\"Title\") BETWEEN 1 AND 300"));

            // Indexes
            entity.HasIndex(n => new { n.UserId, n.UpdatedAt });
            entity.HasIndex(n => new { n.UserId, n.IsDeleted });
            entity.HasIndex(n => new { n.UserId, n.CategoryId });
            entity.HasIndex(n => new { n.UserId, n.IsArchived });
            entity.HasIndex(n => new { n.UserId, n.Slug });
        });

        // GIN Index for SearchText using EF Core Npgsql extension
        modelBuilder.Entity<Note>()
            .HasIndex(n => n.SearchText)
            .HasMethod("GIN");

        // Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(c => new { c.UserId, c.Slug }).IsUnique();
        });

        // Tag
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasIndex(t => new { t.UserId, t.Normalized }).IsUnique();
        });

        // NoteTag (Many-to-Many)
        modelBuilder.Entity<NoteTag>(entity =>
        {
            entity.HasKey(nt => new { nt.NoteId, nt.TagId });
            
            entity.HasOne(nt => nt.Note)
                .WithMany(n => n.NoteTags)
                .HasForeignKey(nt => nt.NoteId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(nt => nt.Tag)
                .WithMany()
                .HasForeignKey(nt => nt.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // NoteRevision
        modelBuilder.Entity<NoteRevision>(entity =>
        {
            entity.Property(nr => nr.ContentJsonb).HasColumnType("jsonb").IsRequired();
            entity.Property(nr => nr.Title).HasMaxLength(300).IsRequired();
            entity.Property(nr => nr.Summary).HasMaxLength(1000);
            
            entity.HasOne(nr => nr.EditedByUser)
                .WithMany()
                .HasForeignKey(nr => nr.EditedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // NoteLink
        modelBuilder.Entity<NoteLink>(entity =>
        {
            entity.Property(nl => nl.Title).HasMaxLength(300);
            entity.Property(nl => nl.BlockId).HasMaxLength(100);
        });

        // NoteAttachment
        modelBuilder.Entity<NoteAttachment>(entity =>
        {
            entity.Property(a => a.AttachmentType).HasMaxLength(20).IsRequired();
            entity.Property(a => a.DisplayName).HasMaxLength(255);
            entity.Property(a => a.Filename).HasMaxLength(255).IsRequired();
            entity.Property(a => a.MimeType).HasMaxLength(120).IsRequired();
            entity.Property(a => a.ObjectKey).HasMaxLength(500).IsRequired();
            entity.Property(a => a.Checksum).HasMaxLength(64);

            entity.HasOne(a => a.Note)
                .WithMany(n => n.Attachments)
                .HasForeignKey(a => a.NoteId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.OwnerUser)
                .WithMany()
                .HasForeignKey(a => a.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(a => a.NoteId);
            entity.HasIndex(a => a.OwnerUserId);
        });

        // Shares
        modelBuilder.Entity<PublicFileShare>()
            .HasIndex(p => p.TokenHash)
            .IsUnique();
            
        modelBuilder.Entity<PublicNoteShare>(entity =>
        {
            entity.HasIndex(p => p.TokenHash).IsUnique();
            // Slug index requested in instructions (Wait, NoteShare uses TokenHash as slug. Let's index TokenHash, which is done.)
        });
        modelBuilder.Entity<CustomInteractiveTool>(entity =>
        {
            entity.ToTable("custom_interactive_tools");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2048);
            
            entity.Property(e => e.HtmlSource).IsRequired();
            entity.Property(e => e.CssSource).IsRequired();
            entity.Property(e => e.JavascriptSource).IsRequired();
            
            entity.Property(e => e.ContentHash).IsRequired().HasMaxLength(128);
            entity.Property(e => e.ValidationStatus).IsRequired().HasMaxLength(40);
            entity.Property(e => e.SecurityStatus).IsRequired().HasMaxLength(40);
            
            entity.HasOne(e => e.Note)
                  .WithMany()
                  .HasForeignKey(e => e.NoteId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.OwnerUser)
                  .WithMany()
                  .HasForeignKey(e => e.OwnerUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.UpdatedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.UpdatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.NoteId);
            entity.HasIndex(e => e.OwnerUserId);
            entity.HasIndex(e => new { e.NoteId, e.IsDeleted });
            entity.HasIndex(e => new { e.NoteId, e.Name });
            entity.HasIndex(e => e.ContentHash);
        });
        modelBuilder.Entity<NoteImportJob>(entity =>
        {
            entity.ToTable("imports");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.FileName).HasMaxLength(255);
            entity.Property(e => e.Status).HasMaxLength(40).IsRequired();
            entity.Property(e => e.ErrorJsonb).HasColumnType("jsonb");

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
