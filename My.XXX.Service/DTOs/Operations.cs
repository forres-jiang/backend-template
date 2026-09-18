using System;
using System.ComponentModel.DataAnnotations;

namespace My.XXX.Service.DTOs
{
    public class OperationQeury : QueryBase
    {
        [DataType(DataType.Text)]
        public string Controller { get; set; }

        [DataType(DataType.Text)]
        public string Action { get; set; }

        public DateTime? Date { get; set; }
    }
}