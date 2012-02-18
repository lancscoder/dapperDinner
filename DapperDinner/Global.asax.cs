using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Reflection;
using System.Web.Security;
using System.Security.Principal;
using System.Data.Common;
using System.Data.SqlClient;
using MvcMiniProfiler;
using MvcMiniProfiler.Data;
using System.Configuration;

namespace DapperDinner
{
    public class MvcApplication : System.Web.HttpApplication
    {
        public static DbConnection GetOpenConnection()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["NerdDinners"].ConnectionString;

            var connection = new SqlConnection(connectionString);
            connection.Open();

            return new ProfiledDbConnection(connection, MiniProfiler.Current);
        }

        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                    "PrettyDetails",
                    "{Id}",
                        new { controller = "Dinners", action = "Details" },
                        new { Id = @"\d+" }
                    );

            routes.MapRoute(
                    "UpcomingDinners",
                    "Dinners/Page/{page}",
                    new { controller = "Dinners", action = "Index" }
            );

            routes.MapRoute(
                    "Default",                                              // Route name
                    "{controller}/{action}/{id}",                           // URL with parameters
                    new { controller = "Home", action = "Index", id = "" }  // Parameter defaults
            );

            routes.MapRoute(
                "OpenIdDiscover",
                "Auth/Discover");

        }

        public override void Init()
        {
            this.AuthenticateRequest += new EventHandler(MvcApplication_AuthenticateRequest);
            this.PostAuthenticateRequest += new EventHandler(MvcApplication_PostAuthenticateRequest);
            base.Init();
        }

        void Application_Start()
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            Application["Version"] = string.Format("{0}.{1}", version.Major, version.Minor);

            RegisterRoutes(RouteTable.Routes);
        }

        void MvcApplication_PostAuthenticateRequest(object sender, EventArgs e)
        {
            HttpCookie authCookie = HttpContext.Current.Request.Cookies[FormsAuthentication.FormsCookieName];
            if (authCookie != null)
            {
                string encTicket = authCookie.Value;
                if (!String.IsNullOrEmpty(encTicket))
                {
                    FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(encTicket);
                    NerdIdentity id = new NerdIdentity(ticket);
                    GenericPrincipal prin = new GenericPrincipal(id, null);
                    HttpContext.Current.User = prin;
                }
            }
        }

        void MvcApplication_AuthenticateRequest(object sender, EventArgs e)
        {
        }
    }
}