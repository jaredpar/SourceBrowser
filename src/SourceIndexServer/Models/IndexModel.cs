using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.SourceBrowser.SourceIndexServer.Models
{
    public class IndexModel : PageModel
    {
        public ProjectManager Manager { get; }

        public IndexModel(ProjectManager manager)
        {
            Manager = manager;
        }
    }
}
