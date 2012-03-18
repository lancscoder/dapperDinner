using System.ComponentModel.DataAnnotations;
using System.Data.Services.Common;

namespace DapperDinner.Models
{
    [DataServiceKey("RsvpID")]
    public class RSVP : ITracker
    {
        public int RsvpID { get; set; }
        public int DinnerID { get; set; }
        public string AttendeeName { get; set; }
        public string AttendeeNameId { get; set; }

        public virtual Dinner Dinner { get; set; }

        public ObjectState State { get; set; }
    }
}