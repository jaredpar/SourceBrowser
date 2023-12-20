using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.SourceBrowser.SourceIndexServer.Pages;
public class IndexModel : PageModel
{
    public ProjectManager Manager { get; }

    public IndexModel(ProjectManager manager)
    {
        Manager = manager;
    }
}