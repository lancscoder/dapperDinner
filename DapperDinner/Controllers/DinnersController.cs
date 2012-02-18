using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DapperDinner.Helpers;
using DapperDinner.Models;
using PagedList;

namespace DapperDinner.Controllers
{
    [HandleErrorWithELMAH]
    public class DinnersController : Controller
    {

        IDinnerRepository dinnerRepository;

        NerdIdentity _nerdIdentity;



        private NerdIdentity nerdIdentity
        {
            get { return (_nerdIdentity ?? User.Identity as NerdIdentity); }
        }

        private const int PageSize = 25;

        //
        // Dependency Injection enabled constructors

        public DinnersController()
            : this(new DapperDinnerRepository(), null)
        {
        }

        public DinnersController(IDinnerRepository repository, NerdIdentity nerdIdentity)
        {
            dinnerRepository = repository;
            _nerdIdentity = nerdIdentity;

        }

        //
        // GET: /Dinners/
        //      /Dinners/Page/2
        //      /Dinners?q=term

        public ActionResult Index(string q, int? page)
        {

            IQueryable<Dinner> dinners = null;

            //Searching?
            if (!string.IsNullOrWhiteSpace(q))
                dinners = dinnerRepository.FindDinnersByText(q).OrderBy(d => d.EventDate);
            else
                dinners = dinnerRepository.FindUpcomingDinners();

            int pageIndex = page ?? 1;
            return View(dinners.ToPagedList(pageIndex, PageSize));
        }

        //
        // GET: /Dinners/Details/5

        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new FileNotFoundResult { Message = "No Dinner found due to invalid dinner id" };
            }

            Dinner dinner = dinnerRepository.Find(id.Value);

            if (dinner == null)
            {
                return new FileNotFoundResult { Message = "No Dinner found for that id" };
            }

            return View(dinner);
        }

        //
        // GET: /Dinners/Edit/5

        [Authorize]
        public ActionResult Edit(int id)
        {

            Dinner dinner = dinnerRepository.Find(id);

            if (dinner == null)
                return View("NotFound");

            if (!dinner.IsHostedBy(User.Identity.Name))
                return View("InvalidOwner");

            return View(dinner);
        }

        //
        // POST: /Dinners/Edit/5
        [HttpPost, Authorize, ValidateAntiForgeryToken]
        public ActionResult Edit(int id, FormCollection collection)
        {

            Dinner dinner = dinnerRepository.Find(id);

            if (!dinner.IsHostedBy(User.Identity.Name))
                return View("InvalidOwner");

            try
            {
                UpdateModel(dinner);

                dinnerRepository.Save();

                return RedirectToAction("Details", new { id = dinner.DinnerID });
            }
            catch
            {
                return View(dinner);
            }
        }

        //
        // GET: /Dinners/Create

        [Authorize]
        public ActionResult Create()
        {

            Dinner dinner = new Dinner()
            {
                EventDate = DateTime.Now.AddDays(7)
            };

            return View(dinner);
        }

        //
        // POST: /Dinners/Create

        [HttpPost, Authorize, ValidateAntiForgeryToken]
        public ActionResult Create(Dinner dinner)
        {

            if (ModelState.IsValid)
            {
                dinner.HostedById = this.nerdIdentity.Name;
                dinner.HostedBy = this.nerdIdentity.FriendlyName;

                RSVP rsvp = new RSVP();
                rsvp.AttendeeNameId = this.nerdIdentity.Name;
                rsvp.AttendeeName = this.nerdIdentity.FriendlyName;

                dinner.RSVPs = new List<RSVP>();
                dinner.RSVPs.Add(rsvp);

                dinnerRepository.InsertOrUpdate(dinner);
                dinnerRepository.Save();

                return RedirectToAction("Details", new { id = dinner.DinnerID });
            }

            return View(dinner);
        }

        //
        // HTTP GET: /Dinners/Delete/1

        [Authorize, ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {

            Dinner dinner = dinnerRepository.Find(id);

            if (dinner == null)
                return View("NotFound");

            if (!dinner.IsHostedBy(User.Identity.Name))
                return View("InvalidOwner");

            return View(dinner);
        }

        // 
        // HTTP POST: /Dinners/Delete/1

        [HttpPost, Authorize]
        public ActionResult Delete(int id, string confirmButton)
        {

            Dinner dinner = dinnerRepository.Find(id);

            if (dinner == null)
                return View("NotFound");

            if (!dinner.IsHostedBy(User.Identity.Name))
                return View("InvalidOwner");

            dinnerRepository.Delete(id);
            dinnerRepository.Save();

            return View("Deleted");
        }


        protected override void HandleUnknownAction(string actionName)
        {
            throw new HttpException(404, "Action not found");
        }

        public ActionResult Lost()
        {
            return View();
        }

        public ActionResult Trouble()
        {
            return View("Error");
        }

        [Authorize]
        public ActionResult My()
        {
            _nerdIdentity = this.nerdIdentity;

            var userDinners = from dinner in dinnerRepository.All
                              where
                                (
                                String.Equals((dinner.HostedById ?? dinner.HostedBy), _nerdIdentity.Name)
                                    ||
                                dinner.RSVPs.Any(r => r.AttendeeNameId == _nerdIdentity.Name || (r.AttendeeNameId == null && r.AttendeeName == _nerdIdentity.Name))
                                )
                              orderby dinner.EventDate
                              select dinner;

            return View(userDinners);
        }

        public ActionResult WebSlicePopular()
        {
            ViewData["Title"] = "Popular Nerd Dinners";
            var model = from dinner in dinnerRepository.FindUpcomingDinners()
                        orderby dinner.RSVPs.Count descending
                        select dinner;
            return View("WebSlice", model.Take(5));
        }

        public ActionResult WebSliceUpcoming()
        {
            ViewData["Title"] = "Upcoming Nerd Dinners";
            DateTime d = DateTime.Now.AddMonths(2);
            var model = from dinner in dinnerRepository.FindUpcomingDinners()
                        where dinner.EventDate < d
                        orderby dinner.EventDate descending
                        select dinner;
            return View("WebSlice", model.Take(5));
        }

    }
}