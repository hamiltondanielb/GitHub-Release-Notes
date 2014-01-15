using System.Web;
using System.Web.Mvc;

namespace bootstrap_git_auto_notes
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}