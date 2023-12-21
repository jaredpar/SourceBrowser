using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Microsoft.SourceBrowser.SourceIndexServer.Pages;

public class AdminModel : PageModel
{
    public RepositoryManager Manager { get; }

    public AdminModel(RepositoryManager manager)
    {
        Manager = manager;
    }

    public IActionResult OnPost()
    {
        var names = new List<string>();
        foreach (var pair in Request.Form)
        {
            if (pair.Key.StartsWith("repo-", StringComparison.Ordinal))
            {
                names.Add(pair.Value);
            }
        }

        Manager.DeleteRangeAsync(names);
        return RedirectToPage("Index");
    }
}