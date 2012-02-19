using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Dapper;
using System.Data.Common;

namespace DapperDinner.Models
{
    public class DapperDinnerRepository : IDinnerRepository
    {
        public IQueryable<Dinner> FindByLocation(float latitude, float longitude)
        {
            using (var connection = MvcApplication.GetOpenConnection())
            {
                var results = connection.Query<Dinner>("SELECT * FROM Dinners " +
                    "WHERE EventDate >= @EventDate AND dbo.DistanceBetween(@Lat, @Long, Latitude, Longitude) < 1000",
                    new { EventDate = DateTime.Now, Lat = latitude, Long = longitude });

                foreach (Dinner dinner in results)
                {
                    dinner.RSVPs = new List<RSVP>();

                    var rsvps = connection.Query<RSVP>("SELECT * FROM RSVP WHERE DinnerID = @DinnerID", new { DinnerId = dinner.DinnerID });

                    foreach (RSVP rsvp in rsvps)
                    {
                        dinner.RSVPs.Add(rsvp);
                    }
                }

                return results.AsQueryable<Dinner>();
            }
        }

        public IQueryable<Dinner> FindUpcomingDinners()
        {
            using (var connection = MvcApplication.GetOpenConnection())
            {
                var results = connection.Query<Dinner>("SELECT * FROM Dinners WHERE EventDate >= @EventDate",
                    new { EventDate = DateTime.Now });

                // Needed as doesnt have lazy loading
                // Change to have a count parameter?
                foreach (Dinner dinner in results)
                {
                    dinner.RSVPs = new List<RSVP>();

                    var rsvps = connection.Query<RSVP>("SELECT * FROM RSVP WHERE DinnerID = @DinnerID", new { DinnerId = dinner.DinnerID });

                    foreach (RSVP rsvp in rsvps)
                    {
                        dinner.RSVPs.Add(rsvp);
                    }
                }

                return results.AsQueryable<Dinner>();
            }
        }

        public IQueryable<Dinner> FindDinnersByText(string q)
        {
            using (var connection = MvcApplication.GetOpenConnection())
            {
                var query = String.Format("%{0}%", q);

                var results = connection.Query<Dinner>("SELECT * FROM Dinners WHERE Title like @query OR Description like @query OR HostedBy like @query",
                    new { Query = q });

                foreach (Dinner dinner in results)
                {
                    dinner.RSVPs = new List<RSVP>();

                    var rsvps = connection.Query<RSVP>("SELECT * FROM RSVP WHERE DinnerID = @DinnerID", new { DinnerId = dinner.DinnerID });

                    foreach (RSVP rsvp in rsvps)
                    {
                        dinner.RSVPs.Add(rsvp);
                    }
                }

                return results.AsQueryable<Dinner>();
            }
        }

        public void DeleteRsvp(RSVP rsvp)
        {
            using (var connection = MvcApplication.GetOpenConnection())
            {
                connection.Execute(@"DELETE FROM rsvp where RsvpID = @id", new { id = rsvp.RsvpID });
            }
        }

        public IQueryable<Dinner> All
        {
            get
            {
                using (var connection = MvcApplication.GetOpenConnection())
                {
                    var results = connection.Query<Dinner>("SELECT * FROM Dinners");

                    foreach (Dinner dinner in results)
                    {
                        dinner.RSVPs = new List<RSVP>();

                        var rsvps = connection.Query<RSVP>("SELECT * FROM RSVP WHERE DinnerID = @DinnerID", new { DinnerId = dinner.DinnerID });

                        foreach (RSVP rsvp in rsvps)
                        {
                            dinner.RSVPs.Add(rsvp);
                        }
                    }

                    return results.AsQueryable<Dinner>();
                }
            }
        }

        public IQueryable<Dinner> AllIncluding(params System.Linq.Expressions.Expression<Func<Dinner, object>>[] includeProperties)
        {
            using (var connection = MvcApplication.GetOpenConnection())
            {
                var results = connection.Query<Dinner>("SELECT * FROM Dinners");

                foreach (Dinner dinner in results)
                {
                    dinner.RSVPs = new List<RSVP>();

                    var rsvps = connection.Query<RSVP>("SELECT * FROM RSVP WHERE DinnerID = @DinnerID", new { DinnerId = dinner.DinnerID });

                    foreach (RSVP rsvp in rsvps)
                    {
                        dinner.RSVPs.Add(rsvp);
                    }
                }

                return results.AsQueryable<Dinner>();
            }
        }

        public Dinner Find(int id)
        {
            using (var connection = MvcApplication.GetOpenConnection())
            {
                var sql = @"SELECT * FROM Dinners WHERE DinnerId = @id
                            SELECT * FROM RSVP WHERE DinnerID = @id";

                Dinner dinner;
                using (var multi = connection.QueryMultiple(sql, new { id = id }))
                {
                    dinner = multi.Read<Dinner>().FirstOrDefault();

                    // Dinner Exists
                    if (dinner != null)
                    {
                        dinner.RSVPs = multi.Read<RSVP>().ToList();
                    }
                }

                return dinner;
            }
        }

        public void InsertOrUpdate(Dinner dinner)
        {
            // TODO : Need insert / update, need to check rsvp to see if they exist....
            using (var connection = MvcApplication.GetOpenConnection())
            {
                InsertOrUpdateDinner(dinner, connection);

                foreach (var rsvp in dinner.RSVPs)
                {
                    InsertOrUpdateRsvp(rsvp, connection);
                }
            }
        }

        public void Delete(int id)
        {
            using (var connection = MvcApplication.GetOpenConnection())
            {
                connection.Execute(@"DELETE FROM rsvp where DinnerId = @id", new { id = id });
                connection.Execute(@"DELETE FROM Dinners where DinnerId = @id", new { id = id });
            }
        }

        public void Save()
        {
            // Not needs.
        }

        private void InsertOrUpdateDinner(Dinner dinner, DbConnection connection)
        {
            if (dinner.DinnerID == 0)
            {
                InsertDinner(dinner, connection);
            }
            else
            {
                UpdateDinner(dinner, connection);
            }
        }

        private void InsertDinner(Dinner dinner, DbConnection connection)
        {
            dinner.DinnerID = connection.Query<int>(@"insert into Dinners " +
                "(Title, EventDate, Description, HostedBy, ContactPhone, Address, Country, Latitude, Longitude, HostedById) " +
                "VALUES (@Title, @EventDate, @Description, @HostedBy, @ContactPhone, @Address, @Country, @Latitude, @Longitude, @HostedById); " +
                "SELECT CASE(scope_identity() as int)",
                new
                {
                    dinner.Title,
                    dinner.EventDate,
                    dinner.Description,
                    dinner.HostedBy,
                    dinner.ContactPhone,
                    dinner.Address,
                    dinner.Country,
                    dinner.Latitude,
                    dinner.Longitude,
                    dinner.HostedById
                }).First();
        }

        private void UpdateDinner(Dinner dinner, DbConnection connection)
        {
            // Future has IsDirtyProperty...
            connection.Execute(@"update Dinners set " +
                "Title = @Title, " +
                "EventDate = @EventDate, " +
                "Description = @Description, " +
                "HostedBy = @HostedBy, " +
                "ContactPhone = @ContactPhone, " +
                "Address = @Address, " +
                "Country = @Country, " +
                "Latitude = @Latitude, " +
                "Longitude = @Longitude, " +
                "HostedById = @HostedById " +
                "WHERE " +
                "DinnerId = @id ",
            new
            {
                id = dinner.DinnerID,
                dinner.Title,
                dinner.EventDate,
                dinner.Description,
                dinner.HostedBy,
                dinner.ContactPhone,
                dinner.Address,
                dinner.Country,
                dinner.Latitude,
                dinner.Longitude,
                dinner.HostedById
            });
        }

        private void InsertOrUpdateRsvp(RSVP rsvp, DbConnection connection)
        {
            if (rsvp.RsvpID == 0)
            {
                InsertRsvp(rsvp, connection);
            }
            else
            {
                UpdateRsvp(rsvp, connection);
            }
        }

        private void InsertRsvp(RSVP rsvp, DbConnection connection)
        {
            rsvp.RsvpID = connection.Query<int>(@"insert into Rsvp " +
                "(DinnerId, AttendeeName, AttendeeNameId) " +
                "VALUES (@DinnerId, @AttendeeName, @AttendeeNameId); " +
                "SELECT CASE(scope_identity() as int)",
                new
                {
                    rsvp.DinnerID,
                    rsvp.AttendeeName,
                    rsvp.AttendeeNameId,
                }).First();
        }

        private void UpdateRsvp(RSVP rsvp, DbConnection connection)
        {
            // Future has IsDirtyProperty...
            connection.Execute(@"update Rsvp set " +
                "DinnerID = @DinnerID, " +
                "AttendeeName = @AttendeeName, " +
                "AttendeeNameId = @AttendeeNameId " +
                "WHERE " +
                "DinnerId = @id ",
            new
            {
                id = rsvp.RsvpID,
                rsvp.DinnerID,
                rsvp.AttendeeName,
                rsvp.AttendeeNameId,
            });
        }
    }
}