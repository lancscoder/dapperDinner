using System.Linq;
using System.Web.Mvc;
using Microsoft.Web.Mvc;

[assembly: WebActivator.PreApplicationStartMethod(typeof(DapperDinner.App_Start.MobileViewEngines), "Start")]
namespace DapperDinner.App_Start
{
    public static class MobileViewEngines
    {
        public static void Start()
        {
            ViewEngines.Engines.Remove(ViewEngines.Engines.OfType<RazorViewEngine>().First());
            ViewEngines.Engines.Add(new MobileCapableRazorViewEngine());
        }
    }
}