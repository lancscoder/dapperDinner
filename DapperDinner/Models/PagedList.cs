using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DapperDinner.Models
{
    public class PagedList<T> : List<T>
    {
        public int PageNumber { get; set; }
        public int PageCount { get; set; }

        public PagedList(IEnumerable<T> list, int pageNumber, int itemCount, int pageSize = 20)
        {
            this.AddRange(list);

            PageNumber = pageNumber;
            PageCount = (int)Math.Ceiling((Decimal)itemCount / pageSize);
        }

        public bool IsFirstPage { get { return PageNumber == 1; } }
        public bool HasPreviousPage { get { return PageNumber > 1; } }
        public bool HasNextPage { get { return PageNumber < PageCount; } }
        public bool IsLastPage { get { return PageCount == PageCount; } }
    }
}