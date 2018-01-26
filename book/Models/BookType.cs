using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace book.Models
{
    public class BookType
    {
        public int BookTypeId { get; set; }
        public string BookTypeName { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }
        public int PId { get; set; }
        public int Grade { get; set; }
    }
}