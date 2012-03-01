using System.Web.Mvc;
using DapperDinner.Models;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc.Html;
using System.Text;

namespace DapperDinner.ViewClasses.Dinners
{
    public class MyViewClass : WebViewPage<IEnumerable<Dinner>>
    {
        public MyViewClass()
        {
            HydrateTitle();
        }

        public MyViewClass(HtmlHelper<IEnumerable<Dinner>> htmlHelper)
            : this()
        {
            Html = htmlHelper;
        }

        void HydrateTitle()
        {
            ViewBag.Title = "My Dinners";
        }

        public MvcHtmlString getMyDinners() { return getMyDinners(Model); }

        public MvcHtmlString getMyDinners(IEnumerable<Dinner> dinners)
        {
            var _dinners = Model ?? dinners;

            if (_dinners.Count() == 0)
                return new MvcHtmlString("<li>You don't own or aren't registered for any dinners.</li>");

            var htmlString = new StringBuilder();

            htmlString.Append("<ul class=\"upcomingdinners\">");

            foreach (var dinner in _dinners)
            {

                htmlString.Append("<li>");
                htmlString.Append(Html.ActionLink(dinner.Title, "Details", new { id = dinner.DinnerID }));
                htmlString.Append("&nbsp;on&nbsp;");
                htmlString.Append("<strong>");
                htmlString.Append(dinner.EventDate.ToString("yyyy-MMM-dd"));
                htmlString.Append("&nbsp;");
                htmlString.Append(dinner.EventDate.ToString("HH:mm tt"));
                htmlString.Append("</strong>");
                htmlString.Append("&nbsp;at&nbsp;");
                htmlString.Append(string.Format("{0} {1}", dinner.Address, dinner.Country));
                htmlString.Append("</li>");
            }

            htmlString.Append("</ul>");

            return new MvcHtmlString(htmlString.ToString());
        }


        public override void Execute()
        {

        }
    }
}