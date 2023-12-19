using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.SourceBrowser.SourceIndexServer.Models
{
    public class IndexModel : PageModel
    {
        public string Project { get; set; }

        public void OnGet(string project = null)
        {
            Project = project ?? "complog";
        }
    }
}
