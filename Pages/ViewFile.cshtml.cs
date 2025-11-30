using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.IO;

public class ViewFileModel : PageModel
{
    private readonly AppDbContext _db;

    public ViewFileModel(AppDbContext db)
    {
        _db = db;
    }

    public List<File> PdfFiles { get; set; } = new();
    public List<File> EpubFiles { get; set; } = new();
    public List<File> ImageFiles { get; set; } = new();
    public List<File> AudioFiles { get; set; } = new();
    public List<File> DocFiles { get; set; } = new();

    public async Task OnGetAsync(int id)
    {
        // Load all relevant files from database first
        var allFiles = await _db.File
            .Where(f => f.Id == id) // adjust as needed
            .ToListAsync();

        // Filter in-memory using Path methods
        PdfFiles = allFiles.Where(f => Path.GetExtension(f.FileName).ToLower() == ".pdf").ToList();
        EpubFiles = allFiles.Where(f => Path.GetExtension(f.FileName).ToLower() == ".epub").ToList();
        ImageFiles = allFiles.Where(f => new[] { ".jpg", ".jpeg", ".png" }.Contains(Path.GetExtension(f.FileName).ToLower())).ToList();
        AudioFiles = allFiles.Where(f => Path.GetExtension(f.FileName).ToLower() == ".mp3").ToList();
        DocFiles = allFiles.Where(f => new[] { ".txt", ".docx" }.Contains(Path.GetExtension(f.FileName).ToLower())).ToList();
    }

    // Helper to generate URL for Razor page
    public string GetFileUrl(string path)
    {
        var folder = Path.GetDirectoryName(path)?.Replace("\\", "/");
        var fileName = Path.GetFileName(path);
        return "/" + folder + "/" + Uri.EscapeDataString(fileName);
    }
}
