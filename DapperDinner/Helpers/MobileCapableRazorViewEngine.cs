using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace Microsoft.Web.Mvc
{
    public class MobileCapableRazorViewEngine : RazorViewEngine
    {
        public override ViewEngineResult FindView(ControllerContext controllerContext, string viewName,
                                                  string masterName, bool useCache)
        {
            string overrideViewName = controllerContext.HttpContext.Request.Browser.IsMobileDevice
                                          ? viewName + ".Mobile"
                                          : viewName;
            ViewEngineResult result = NewFindView(controllerContext, overrideViewName, masterName, useCache);

            // If we're looking for a Mobile view and couldn't find it try again without modifying the viewname
            if (overrideViewName.Contains(".Mobile") && (result == null || result.View == null))
            {
                result = NewFindView(controllerContext, viewName, masterName, useCache);
            }
            return result;
        }

        private ViewEngineResult NewFindView(ControllerContext controllerContext, string viewName, string masterName,
                                             bool useCache)
        {
            // Get the name of the controller from the path
            string controller = controllerContext.RouteData.Values["controller"].ToString();
            string area = "";
            try
            {
                area = controllerContext.RouteData.DataTokens["area"].ToString();
            }
            catch
            {
            }

            // Create the key for caching purposes           
            string keyPath = Path.Combine(area, controller, viewName);

            // Try the cache           
            if (useCache)
            {
                //If using the cache, check to see if the location is cached.               
                string cacheLocation = ViewLocationCache.GetViewLocation(controllerContext.HttpContext, keyPath);
                if (!string.IsNullOrWhiteSpace(cacheLocation))
                {
                    return new ViewEngineResult(CreateView(controllerContext, cacheLocation, masterName), this);
                }
            }

            // Remember the attempted paths, if not found display the attempted paths in the error message.           
            var attempts = new List<string>();

            string[] locationFormats = string.IsNullOrEmpty(area) ? ViewLocationFormats : AreaViewLocationFormats;

            // for each of the paths defined, format the string and see if that path exists. When found, cache it.           
            foreach (string rootPath in locationFormats)
            {
                string currentPath = string.IsNullOrEmpty(area)
                                         ? string.Format(rootPath, viewName, controller)
                                         : string.Format(rootPath, viewName, controller, area);

                if (FileExists(controllerContext, currentPath))
                {
                    ViewLocationCache.InsertViewLocation(controllerContext.HttpContext, keyPath, currentPath);

                    return new ViewEngineResult(CreateView(controllerContext, currentPath, masterName), this);
                }

                // If not found, add to the list of attempts.               
                attempts.Add(currentPath);
            }

            // if not found by now, simply return the attempted paths.           
            return new ViewEngineResult(attempts.Distinct().ToList());
        }
    }
}