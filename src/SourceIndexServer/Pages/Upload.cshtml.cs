using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Build.Evaluation;

namespace Microsoft.SourceBrowser.SourceIndexServer.Pages;

public class UploadModel : PageModel
{
    private RepositoryManager Manager { get; }
    private RepositoryGenerator Generator { get; }

    [BindProperty]
    public string ProjectName { get; set; }

    [BindProperty]
    public IFormFile Upload { get; set; }

    public UploadModel(RepositoryManager manager, RepositoryGenerator generator)
    {
        Manager = manager;
        Generator = generator;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        using var stream = Upload.OpenReadStream();
        var dirName = await Generator.Generate(stream);
        Manager.AddRepository(dirName);
        return RedirectToPage("Index");
    }
}