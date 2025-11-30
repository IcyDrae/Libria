using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class CollectionsModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public CollectionsModel(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public List<Collection> Collections { get; set; } = new();

    [BindProperty]
    public string NewCollectionName { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        Collections = await _db.Collection
            .Include(c => c.Files)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!string.IsNullOrWhiteSpace(NewCollectionName))
        {
            var exists = await _db.Collection.AnyAsync(c => c.Name == NewCollectionName);
            if (!exists)
            {
                _db.Collection.Add(new Collection { Name = NewCollectionName.Trim() });
                await _db.SaveChangesAsync();
            }
        }

        return RedirectToPage();
    }

    // Delete a collection (removes association between collection and files)
    public async Task<IActionResult> OnPostDeleteCollectionAsync(int id)
    {
        var collection = await _db.Collection
            .Include(c => c.Files)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (collection != null)
        {
            // Remove relationships (many-to-many)
            collection.Files.Clear();
            _db.Collection.Remove(collection);
            await _db.SaveChangesAsync();
        }

        return RedirectToPage();
    }

    // Remove a single file from a collection (keeps file in DB/disk)
    public async Task<IActionResult> OnPostRemoveFromCollectionAsync(int collectionId, int fileId)
    {
        var collection = await _db.Collection
            .Include(c => c.Files)
            .FirstOrDefaultAsync(c => c.Id == collectionId);

        var file = await _db.File.FindAsync(fileId);

        if (collection != null && file != null)
        {
            if (collection.Files.Contains(file))
            {
                collection.Files.Remove(file);
                await _db.SaveChangesAsync();
            }
        }

        return RedirectToPage();
    }

    // Delete file record and disk file entirely
    public async Task<IActionResult> OnPostDeleteFileAsync(int id)
    {
        var fileRecord = await _db.File
            .Include(f => f.Metadata)
            .ThenInclude(m => m.FileTags)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (fileRecord != null)
        {
            // delete physical file if exists
            try
            {
                if (System.IO.File.Exists(fileRecord.FilePath))
                    System.IO.File.Delete(fileRecord.FilePath);
            }
            catch
            {
                // ignore file-delete errors but log in real app
            }

            // Remove file from any collections first (EF many-to-many)
            var collectionsContaining = await _db.Collection
                .Where(c => c.Files.Any(f => f.Id == id))
                .Include(c => c.Files)
                .ToListAsync();

            foreach (var c in collectionsContaining)
            {
                var ff = c.Files.FirstOrDefault(x => x.Id == id);
                if (ff != null) c.Files.Remove(ff);
            }

            // Remove metadata & tags if present
            if (fileRecord.Metadata != null)
            {
                _db.FileTag.RemoveRange(fileRecord.Metadata.FileTags);
                _db.FileMetadata.Remove(fileRecord.Metadata);
            }

            _db.File.Remove(fileRecord);
            await _db.SaveChangesAsync();
        }

        return RedirectToPage();
    }
}
