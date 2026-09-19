using System;
using System.ComponentModel.DataAnnotations;

namespace My.XXX.Service.DTOs
{
    public class QueryBase
    {
        private int pageindex;
        private int pagesize;

        [Required]
        [Range(1, 100)]
        public int PageSize
        {
            get
            {
                return pagesize > 100 ? 100 : pagesize;
            }
            set
            {
                pagesize = value;
            }
        }

        [Required]
        public int PageIndex
        {
            get { return pageindex <= 0 ? 0 : pageindex - 1; }
            set { pageindex = value; }
        }
    }
}