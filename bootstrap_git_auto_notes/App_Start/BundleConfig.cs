using System.Web;
using System.Web.Optimization;

namespace bootstrap_git_auto_notes
{
    public class BundleConfig
    {

        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.IgnoreList.Clear();

            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js",
                        "~/Scripts/FileSave.js",
                        "~/Scripts/bootstrap-select.min.js",
                        "~/Scripts/jQuery.flashMessage.js",
                        "~/Scripts/jquery.cookie.js"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrapjs").Include(
                        "~/Scripts/bootstrap.min.js"));

            bundles.Add(new StyleBundle("~/Content/bootstrapcss").Include(
                        "~/Content/bootstrap.min.css",
                        "~/Content/bootstrap-responsive.min.css",
                        "~/Content/bootstrap-select.min.css",
                        "~/Content/Custom.css"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryuijs").Include(
                       "~/Scripts/jquery-ui-1.10.3.custom.min.js"));

            bundles.Add(new StyleBundle("~/Content/jqueryuicss").Include(
                       "~/Content/jquery-ui-1.10.3.custom.min.css"));
        }
    }
}