using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DapperDinner.Models;

namespace DapperDinner.Models
{
    public class FlairViewModel
    {
        public IList<Dinner> Dinners { get; set; }
        public string LocationName { get; set; }
    }
}
