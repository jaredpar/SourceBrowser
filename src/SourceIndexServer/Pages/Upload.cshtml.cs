using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Build.Evaluation;

namespace Microsoft.SourceBrowser.SourceIndexServer.Pages;

public class UploadModel : PageModel
{
    private RepositoryManager Manager { get; }
    private RepositoryGenerator Generator { get; }

    public string ErrorMessage { get; set; }

    [BindProperty]
    public string RepositoryName { get; set; }

    [BindProperty]
    public IFormFile Upload { get; set; }

    public UploadModel(RepositoryManager manager, RepositoryGenerator generator)
    {
        Manager = manager;
        Generator = generator;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrEmpty(RepositoryName))
        {
            ErrorMessage = "Repository name cannot be empty";
            return Page();
        }

        if (!Regex.IsMatch(RepositoryName, @"[a-z0-9-]+"))
        {
            ErrorMessage = "Repository name must be letters, numbers and dashes only";
            return Page();
        }

        if (Upload is null || Path.GetExtension(Upload.FileName) != ".complog")
        {
            ErrorMessage = "Must provide a compiler log file";
            return Page();
        }

        try
        {
            using var stream = Upload.OpenReadStream();
            var indexName = await Generator.Generate(RepositoryName, stream);
            Manager.AddOrUpdateRepository(RepositoryName, indexName);
            return Redirect($"/{RepositoryName}/index.html");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.ToString();
            return Page();
        }
    }
}