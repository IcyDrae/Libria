using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FileTag>()
            .HasKey(ft => new { ft.FileMetadataId, ft.TagId }); // composite PK

        modelBuilder.Entity<FileTag>()
            .HasOne(ft => ft.FileMetadata)
            .WithMany(fm => fm.FileTags)
            .HasForeignKey(ft => ft.FileMetadataId);

        modelBuilder.Entity<FileTag>()
            .HasOne(ft => ft.Tag)
            .WithMany(t => t.FileTags)
            .HasForeignKey(ft => ft.TagId);
    }

    // Define your tables here
    public DbSet<File> File { get; set; }
    public DbSet<FileMetadata> FileMetadata { get; set; }
    public DbSet<Tag> Tag { get; set; }
    public DbSet<FileTag> FileTag { get; set; }
    public DbSet<Collection> Collection { get; set; }
}

public class File
{
    public int Id { get; set; }
    public string FileName { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }

    public int? MetadataId { get; set; }
    public FileMetadata? Metadata { get; set; }

    public ICollection<Collection> Collections { get; set; } = new List<Collection>();
}

public class FileMetadata
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string MimeType { get; set; } = null!;
    public int? PageCount { get; set; } // for PDFs/EPUBs
    public TimeSpan? Duration { get; set; } // for audio/video
    public DateTime? PublishedAt { get; set; }

    // Many-to-many with tags
    public ICollection<FileTag> FileTags { get; set; } = new List<FileTag>();
}

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;

    // Many-to-many with files
    public ICollection<FileTag> FileTags { get; set; } = new List<FileTag>();
}

public class FileTag
{
    public int FileMetadataId { get; set; }
    public FileMetadata FileMetadata { get; set; } = null!;

    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}

public class Collection
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;

    // Many-to-many with files
    public ICollection<File> Files { get; set; } = new List<File>();
}
