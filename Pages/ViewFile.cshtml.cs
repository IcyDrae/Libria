using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class ViewFileModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ViewFileModel(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public File File { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        File = await _db.File.FirstOrDefaultAsync(f => f.Id == id);
        if (File == null)
            return NotFound();

        // Build correct absolute path
        var physicalPath = Path.Combine(_env.ContentRootPath, File.FilePath);

        if (!System.IO.File.Exists(physicalPath))
            return NotFound();

        return Page();
    }
}
