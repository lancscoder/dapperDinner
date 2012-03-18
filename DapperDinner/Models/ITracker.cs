using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DapperDinner.Models
{
    public interface ITracker
    {
        ObjectState State { get; set; }
    }
}