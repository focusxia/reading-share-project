using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace book.Models
{
    public class Book
    {
        public int Id { get; set; }
        public string BookName { get; set; }
        public int BookTypeId { get; set; }
        public int BookTypeId1 { get; set; }
        public int BookTypeId2 { get; set; }
        public string BookTypeName { get; set; }
        public string BookTypeName1 { get; set; }
        public string BookTypeName2 { get; set; }
        public string Author { get; set; }
        public string Country { get; set; }
        public string ImageData { get; set; }
        public string Score { get; set; }
        public string BookDes { get; set; }
        public string RecommendReason { get; set; }
        public string CreateUser { get; set; }
        public string CreateTime { get; set; }
        public string UpdateTime { get; set; }
    }
}