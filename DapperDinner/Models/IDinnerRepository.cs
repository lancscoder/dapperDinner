using System.Linq;
using System;
using System.Collections.Generic;

namespace DapperDinner.Models
{

    public interface IDinnerRepository
    {
        Dinner NewDinner();
        RSVP NewRsvp(int dinnerId);

        PagedList<Dinner> FindByLocation(float latitude, float longitude, string orderBy = "DinnerID", int page = 1, int pageSize = 20);
        PagedList<Dinner> FindUpcomingDinners(string orderBy = "DinnerID", int page = 1, int pageSize = 20);
        PagedList<Dinner> FindUpcomingDinners(DateTime? eventDate, string orderBy = "DinnerID", int page = 1, int pageSize = 20);
        PagedList<Dinner> FindDinnersByText(string q, string orderBy = "DinnerID", int page = 1, int pageSize = 20);
        IEnumerable<Dinner> AllDinnersByUser(string name);

        Dinner Find(int id);

        void InsertOrUpdate(Dinner dinner);
        void InsertOrUpdate(RSVP rsvp);

        void Delete(int id);
        void DeleteRsvp(RSVP rsvp);
    }
}
