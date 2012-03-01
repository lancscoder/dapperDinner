using System;
using System.Linq;
using System.Web.Mvc;
using System.Xml.Linq;
using DapperDinner.Helpers;
using DapperDinner.Models;
using DapperDinner.Services;

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
            var dinners = dinnerRepository.FindByLocation(latitude, longitude).Select(JsonDinnerFromDinner);

            return Json(dinners);
        }

        [HttpPost]
        public ActionResult SearchByPlaceNameOrZip(string Location)
        {
            if (String.IsNullOrEmpty(Location)) return null;

            LatLong location = GeolocationService.PlaceOrZipToLatLong(Location);

            if (location != null)
            {
                var dinners = dinnerRepository.
                                FindByLocation(location.Lat, location.Long, "EventDate");

                return View("Results", dinners);
            }
            
            return View("Results", null);
        }


        //
        // AJAX: /Search/GetMostPopularDinners
        // AJAX: /Search/GetMostPopularDinners?limit=5

        [HttpPost]
        public ActionResult GetMostPopularDinners(int? limit)
        {
            var mostPopularDinners = dinnerRepository.FindUpcomingDinners("RsvpCount desc", 1, limit ?? 40);

            var jsonDinners = mostPopularDinners.Select(JsonDinnerFromDinner);

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
                RSVPCount = dinner.RsvpCount.GetValueOrDefault(0),

                //TODO: Need to mock this out for testing...
                //Url = Url.RouteUrl("PrettyDetails", new { Id = dinner.DinnerID } )
                Url = dinner.DinnerID.ToString()
            };
        }

    }
}