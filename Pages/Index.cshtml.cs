using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public IWebHostEnvironment Env => _env;

    public IndexModel(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [BindProperty]
    public IFormFile? File { get; set; }
    public List<File> UploadedFiles { get; set; } = new();

    public string? UploadMessage { get; set; }

    public string Extension;

    public string OriginalName;

    public string FinalFileName;

    public string PDFSavePath;

    public string FinalPath;

    public FileMetadata Metadata;

    public async Task OnGetAsync()
    {
        UploadedFiles = await _db.File
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (File == null || File.Length == 0)
        {
            UploadMessage = "No file selected.";
            return Page();
        }

        HandleFileName();

        BuildPDFPath();

        HandleDirectory(PDFSavePath);
        this.FinalPath = Path.Combine(PDFSavePath, FinalFileName);

        HandleDuplicateFileNames();

        await SaveFileLocally();

        await StorePDFMetadata();

        await StoreFileRecordInDatabase();

        UploadMessage = "File uploaded and saved to database!";
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var fileRecord = await _db.File
            .Include(f => f.Metadata)
            .ThenInclude(m => m.FileTags)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (fileRecord == null)
            return RedirectToPage();

        // Delete physical file
        if (System.IO.File.Exists(fileRecord.FilePath))
            System.IO.File.Delete(fileRecord.FilePath);

        // Delete tags connecting to metadata (FileTag)
        if (fileRecord.Metadata != null)
        {
            _db.FileTag.RemoveRange(fileRecord.Metadata.FileTags);
            _db.FileMetadata.Remove(fileRecord.Metadata);
        }

        // Delete the main file entry
        _db.File.Remove(fileRecord);

        await _db.SaveChangesAsync();

        return RedirectToPage();
    }


    private void HandleFileName()
    {
        this.Extension = Path.GetExtension(File.FileName);
        this.OriginalName = Path.GetFileNameWithoutExtension(File.FileName);
        this.FinalFileName = File.FileName;
    }

    private void BuildPDFPath()
    {
        this.PDFSavePath = Path.Combine("data", "library", "pdfs");
    }

    private void HandleDirectory(string Path)
    {
        if (!Directory.Exists(Path))
            Directory.CreateDirectory(Path);
    }

    private void HandleDuplicateFileNames()
    {
        int counter = 1;
        while (System.IO.File.Exists(FinalPath))
        {
            FinalFileName = $"{OriginalName} ({counter}){Extension}";
            FinalPath = Path.Combine(PDFSavePath, FinalFileName);
            counter++;
        }
    }

    private async Task SaveFileLocally()
    {
        using (var stream = new FileStream(FinalPath, FileMode.Create))
        {
            await File.CopyToAsync(stream);
        }
    }

    private async Task StorePDFMetadata()
    {
        if (Extension.ToLower() == ".pdf")
        {
            this.Metadata = new FileMetadata
            {
                MimeType = "application/pdf",
                // Could add Title, Author, PageCount later when I implement extraction
            };

            _db.FileMetadata.Add(Metadata);
            await _db.SaveChangesAsync(); // must save so metadata.Id exists
        }
    }

    private async Task StoreFileRecordInDatabase()
    {
        var fileRecord = new File
        {
            FileName = FinalFileName,
            FilePath = FinalPath,
            FileSize = File.Length,
            UploadedAt = DateTime.UtcNow,
            MetadataId = Metadata?.Id
        };

        _db.File.Add(fileRecord);
        await _db.SaveChangesAsync();
    }
}
