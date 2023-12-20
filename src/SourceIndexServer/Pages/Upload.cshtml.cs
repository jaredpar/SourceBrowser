using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.SourceBrowser.SourceIndexServer.Pages;

public class UploadModel : PageModel
{
    private ProjectManager Manager { get; }

    [BindProperty]
    public string ProjectName { get; set; }

    [BindProperty]
    public IFormFile Upload { get; set; }

    public UploadModel(ProjectManager manager)
    {
        Manager = manager;
    }

    public void OnGet()
    {

    }

    public async Task<IActionResult> OnPostAsync()
    {
        throw new NotImplementedException();
        /*
        var file = Path.Combine(_environment.ContentRootPath, "uploads", Upload.FileName);
        using (var fileStream = new FileStream(file, FileMode.Create))
        {
            await Upload.CopyToAsync(fileStream);
        }
        */
    }
}