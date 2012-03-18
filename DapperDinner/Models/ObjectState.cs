using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DapperDinner.Models
{
    public enum ObjectState
    {
        Unchanged = 1,
        Modified = 2,
        Added = 3,
        Deleted = 4,
    }
}