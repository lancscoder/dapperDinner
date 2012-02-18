using System;
using System.Linq;
using System.Web.Mvc;
using System.Xml.Linq;
using DapperDinner.Helpers;
using DapperDinner.Models;
using DapperDinner.Services;
using PagedList;

namespace DapperDinner.Controllers
{
    public class JsonDinner
    {
        public int DinnerID { get; set; }
        public DateTime EventDate { get; set; }
        public string Title { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Description { get; set; }
        public int RSVPCount { get; set; }
        public string Url { get; set; }
    }

    [HandleErrorWithELMAH]
    public class SearchController : Controller
    {

        IDinnerRepository dinnerRepository;

        //
        // Dependency Injection enabled constructors

        public SearchController()
            : this(new DapperDinnerRepository())
        {
        }

        public SearchController(IDinnerRepository repository)
        {
            dinnerRepository = repository;
        }

        //
        // AJAX: /Search/FindByLocation?longitude=45&latitude=-90

        [HttpPost]
        public ActionResult SearchByLocation(float latitude, float longitude)
        {

            var dinners = dinnerRepository.FindByLocation(latitude, longitude);

            var jsonDinners = from dinner in dinners.AsEnumerable()
                              select JsonDinnerFromDinner(dinner);

            return Json(jsonDinners.ToList());
        }

        [HttpPost]
        public ActionResult SearchByPlaceNameOrZip(string Location)
        {
            if (String.IsNullOrEmpty(Location)) return null; ;
            LatLong location = GeolocationService.PlaceOrZipToLatLong(Location);
            if (location != null)
            {
                var dinners = dinnerRepository.
                                FindByLocation(location.Lat, location.Long).
                                OrderByDescending(p => p.EventDate);

                return View("Results", dinners.ToPagedList(1, 20));
            }
            return View("Results", null);
        }


        //
        // AJAX: /Search/GetMostPopularDinners
        // AJAX: /Search/GetMostPopularDinners?limit=5

        [HttpPost]
        public ActionResult GetMostPopularDinners(int? limit)
        {
            var dinners = dinnerRepository.FindUpcomingDinners();

            // Default the limit to 40, if not supplied.
            if (!limit.HasValue)
                limit = 40;

            var mostPopularDinners = from dinner in dinners
                                     orderby dinner.RSVPs.Count descending
                                     select dinner;

            var jsonDinners =
                mostPopularDinners.Take(limit.Value).AsEnumerable()
                .Select(item => JsonDinnerFromDinner(item));

            return Json(jsonDinners.ToList());
        }

        private JsonDinner JsonDinnerFromDinner(Dinner dinner)
        {
            return new JsonDinner
            {
                DinnerID = dinner.DinnerID,
                EventDate = dinner.EventDate,
                Latitude = dinner.Latitude,
                Longitude = dinner.Longitude,
                Title = dinner.Title,
                Description = dinner.Description,
                RSVPCount = dinner.RSVPs.Count,

                //TODO: Need to mock this out for testing...
                //Url = Url.RouteUrl("PrettyDetails", new { Id = dinner.DinnerID } )
                Url = dinner.DinnerID.ToString()
            };
        }

    }
}