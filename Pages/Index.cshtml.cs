using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public List<File> PdfFiles { get; set; } = new();
    public List<File> EpubFiles { get; set; } = new();
    public List<File> ImageFiles { get; set; } = new();
    public List<File> AudioFiles { get; set; } = new();
    public List<File> DocFiles { get; set; } = new();

    public IndexModel(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [BindProperty]
    public IFormFile? File { get; set; }
    public List<File> UploadedFiles { get; set; } = new();
    public List<Collection> Collections { get; set; } = new();
    public string? UploadMessage { get; set; }

    // Internal helper fields
    private string Extension;
    private string OriginalName;
    private string FinalFileName;
    private string FolderPath;
    private string FinalPath;
    private FileMetadata? Metadata;

    public string GetFileUrl(string path)
    {
        var folder = Path.GetDirectoryName(path)?.Replace("\\", "/");
        var fileName = Path.GetFileName(path);
        return "/" + folder + "/" + Uri.EscapeDataString(fileName);
    }

    public async Task OnGetAsync()
    {
        await PopulateFileLists();
        Collections = await _db.Collection.ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (File == null || File.Length == 0)
        {
            TempData["UploadMessage"] = "No file selected.";
            return RedirectToPage();
        }

        ExtractFileInfo();
        BuildFolderPath();
        EnsureDirectoryExists();
        HandleDuplicateFileNames();
        await SaveFileLocally();
        await StoreMetadataIfPdf();
        await SaveFileRecordInDatabase();

        UploadMessage = "File uploaded and saved successfully!";

        await PopulateFileLists();

        
        TempData["UploadMessage"] = "File uploaded and saved successfully!";

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var fileRecord = await _db.File
            .Include(f => f.Metadata)
            .ThenInclude(m => m.FileTags)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (fileRecord == null)
            return RedirectToPage();

        if (System.IO.File.Exists(fileRecord.FilePath))
            System.IO.File.Delete(fileRecord.FilePath);

        if (fileRecord.Metadata != null)
        {
            _db.FileTag.RemoveRange(fileRecord.Metadata.FileTags);
            _db.FileMetadata.Remove(fileRecord.Metadata);
        }

        _db.File.Remove(fileRecord);
        await _db.SaveChangesAsync();

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAddToCollection(int fileId, int collectionId)
    {
        var file = await _db.File.Include(f => f.Collections)
                                .FirstOrDefaultAsync(f => f.Id == fileId);

        var collection = await _db.Collection.FindAsync(collectionId);

        if (file != null && collection != null)
        {
            if (!file.Collections.Any(c => c.Id == collectionId))
            {
                file.Collections.Add(collection);
                await _db.SaveChangesAsync();
            }
        }

        return RedirectToPage();
    }

    private async Task PopulateFileLists()
    {
        UploadedFiles = await _db.File
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync();

        // Group by type
        PdfFiles = UploadedFiles.Where(f => Path.GetExtension(f.FileName).ToLower() == ".pdf").ToList();
        EpubFiles = UploadedFiles.Where(f => Path.GetExtension(f.FileName).ToLower() == ".epub").ToList();
        ImageFiles = UploadedFiles.Where(f => new[] { ".jpg", ".jpeg", ".png" }.Contains(Path.GetExtension(f.FileName).ToLower())).ToList();
        AudioFiles = UploadedFiles.Where(f => Path.GetExtension(f.FileName).ToLower() == ".mp3").ToList();
        DocFiles = UploadedFiles.Where(f => new[] { ".txt", ".docx" }.Contains(Path.GetExtension(f.FileName).ToLower())).ToList();
    }

    private void ExtractFileInfo()
    {
        Extension = Path.GetExtension(File.FileName).ToLower();
        OriginalName = Path.GetFileNameWithoutExtension(File.FileName);
        FinalFileName = File.FileName;
    }

    private void BuildFolderPath()
    {
        string subfolder = Extension switch
        {
            ".pdf" => "pdfs",
            ".epub" => "epubs",
            ".jpg" or ".jpeg" or ".png" => "images",
            ".mp3" => "audio",
            ".docx" or ".txt" => "docs",
            _ => "others"
        };

        FolderPath = Path.Combine("data", "library", subfolder);
    }

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(FolderPath))
            Directory.CreateDirectory(FolderPath);

        FinalPath = Path.Combine(FolderPath, FinalFileName);
    }

    private void HandleDuplicateFileNames()
    {
        int counter = 1;
        while (System.IO.File.Exists(FinalPath))
        {
            FinalFileName = $"{OriginalName} ({counter}){Extension}";
            FinalPath = Path.Combine(FolderPath, FinalFileName);
            counter++;
        }
    }

    private async Task SaveFileLocally()
    {
        using var stream = new FileStream(FinalPath, FileMode.Create);
        await File.CopyToAsync(stream);
    }

    private async Task StoreMetadataIfPdf()
    {
        if (Extension == ".pdf")
        {
            Metadata = new FileMetadata
            {
                MimeType = "application/pdf"
                // Optionally add Title, Author, PageCount later
            };
            _db.FileMetadata.Add(Metadata);
            await _db.SaveChangesAsync(); // Save to get Metadata.Id
        }
    }

    private async Task SaveFileRecordInDatabase()
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
