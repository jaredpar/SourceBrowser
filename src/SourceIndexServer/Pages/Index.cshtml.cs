using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.SourceBrowser.SourceIndexServer.Pages;
public class IndexModel : PageModel
{
    public RepositoryManager Manager { get; }

    public IndexModel(RepositoryManager manager)
    {
        Manager = manager;
    }
}