using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Web;
using Dapper;

namespace DapperDinner.Models
{
    public class DapperDinnerRepository : IDinnerRepository
    {
        private const string pagedQuery = @"SELECT * FROM (SELECT *, ROW_NUMBER() OVER (/**orderby**/) AS RowNumber FROM (
            SELECT d.*, COUNT(r.DinnerID) AS RsvpCount 
            FROM Dinners d LEFT OUTER JOIN RSVP r ON d.DinnerID = r.DinnerID 
            /**where**/
            GROUP BY d.DinnerID, d.Title, d.EventDate, d.Description, d.HostedById, d.HostedBy, d.ContactPhone, d.Address, d.Country, d.Latitude, d.Longitude
            ) as X ) as Y
            WHERE RowNumber BETWEEN @start AND @finish";

        private const string totalQuery = @"SELECT COUNT(*) FROM Dinners d /**where**/";

        public Dinner NewDinner()
        {
            return new Dinner
            {
                State = ObjectState.Added,
            };
        }

        public RSVP NewRsvp(int dinnerId)
        {
            return new RSVP
            {
                DinnerID = dinnerId,
                State = ObjectState.Added,
            };
        }

        public PagedList<Dinner> FindByLocation(float latitude, float longitude, string orderBy = "DinnerID", int page = 1, int pageSize = 20)
        {
            var where = @"EventDate >= @EventDate AND dbo.DistanceBetween(@Lat, @Long, Latitude, Longitude) < 1000";

            return FindDinners(where, new { EventDate = DateTime.Now, Lat = latitude, Long = longitude }, orderBy, page, pageSize);
        }

        public PagedList<Dinner> FindUpcomingDinners(string orderBy = "DinnerID", int page = 1, int pageSize = 20)
        {
            return FindUpcomingDinners(null, orderBy, page, pageSize);
        }

        public PagedList<Dinner> FindUpcomingDinners(DateTime? eventDate, string orderBy = "DinnerID", int page = 1, int pageSize = 20)
        {
            var where = @"EventDate >= @EventDate";

            if (!eventDate.HasValue)
            {
                eventDate = DateTime.Now;
            }

            return FindDinners(where, new { EventDate = eventDate.Value }, orderBy, page, pageSize);
        }

        public PagedList<Dinner> FindDinnersByText(string q, string orderBy = "DinnerID", int page = 1, int pageSize = 20)
        {
            var query = String.Format("%{0}%", q);
            var where = @"Title like @query OR Description like @query OR HostedBy like @query";

            return FindDinners(where, new { Query = q }, orderBy, page, pageSize);
        }

        private PagedList<Dinner> FindDinners(string where, object parameters, string orderBy = "DinnerID", int page = 1, int pageSize = 20)
        {
            using (var connection = MvcApplication.GetOpenConnection())
            {
                var builder = new SqlBuilder();

                var start = (page - 1) * pageSize + 1;
                var finish = page * pageSize;

                var selectTemplate = builder.AddTemplate(pagedQuery, new { start, finish });
                var countTemplate = builder.AddTemplate(totalQuery);

                builder.Where(where, parameters);

                builder.OrderBy(orderBy);

                var results = connection.Query<Dinner>(selectTemplate.RawSql, selectTemplate.Parameters);
                var count = connection.Query<int>(countTemplate.RawSql, countTemplate.Parameters).First();

                return new PagedList<Dinner>(results, page, count, pageSize);
            }
        }

        public IEnumerable<Dinner> AllDinnersByUser(string name)
        {
            using (var connection = MvcApplication.GetOpenConnection())
            {
                var results = connection.Query<Dinner>(@"SELECT DISTINCT Dinners.* 
                        FROM Dinners LEFT OUTER JOIN RSVP ON Dinners.DinnerID = RSVP.DinnerID 
                        WHERE Dinners.HostedById = @name OR Dinners.HostedBy = @name
                        OR RSVP.AttendeeNameId = @name OR RSVP.AttendeeNameId = @name
                        ORDER BY Dinners.EventDate", new { name });

                return results;
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

        public void InsertOrUpdate(RSVP rsvp)
        {
            using (var connection = MvcApplication.GetOpenConnection())
            {
                InsertOrUpdateRsvp(rsvp, connection);
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

        public void DeleteRsvp(RSVP rsvp)
        {
            using (var connection = MvcApplication.GetOpenConnection())
            {
                connection.Execute(@"DELETE FROM rsvp where RsvpID = @id", new { id = rsvp.RsvpID });
            }
        }

        private void InsertOrUpdateDinner(Dinner dinner, DbConnection connection)
        {
            if (dinner.State == ObjectState.Added)
            {
                InsertDinner(dinner, connection);
            }
            else if (dinner.State == ObjectState.Modified)
            {
                UpdateDinner(dinner, connection);
            }
        }

        private void InsertDinner(Dinner dinner, DbConnection connection)
        {
            dinner.DinnerID = connection.Query<int>(@"insert into Dinners " +
                "(Title, EventDate, Description, HostedBy, ContactPhone, Address, Country, Latitude, Longitude, HostedById) " +
                "VALUES (@Title, @EventDate, @Description, @HostedBy, @ContactPhone, @Address, @Country, @Latitude, @Longitude, @HostedById); " +
                "SELECT CAST(scope_identity() as int)",
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
            if (rsvp.State == ObjectState.Added)
            {
                InsertRsvp(rsvp, connection);
            }
            else if (rsvp.State == ObjectState.Modified)
            {
                UpdateRsvp(rsvp, connection);
            }
        }

        private void InsertRsvp(RSVP rsvp, DbConnection connection)
        {
            rsvp.RsvpID = connection.Query<int>(@"insert into Rsvp " +
                "(DinnerId, AttendeeName, AttendeeNameId) " +
                "VALUES (@DinnerId, @AttendeeName, @AttendeeNameId); " +
                "SELECT CAST(scope_identity() as int)",
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