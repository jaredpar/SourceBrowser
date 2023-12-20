using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.SourceBrowser.SourceIndexServer.Models;

namespace Microsoft.SourceBrowser.SourceIndexServer.Pages;
public class IndexModel : PageModel
{
    public ProjectManager Manager { get; }

    public IndexModel(ProjectManager manager)
    {
        Manager = manager;
    }
}