using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace My.XXX.Contracts.DTOs
{
    public class DemoModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "The DemoString field is required.")]
        public string DemoString { get; set; }

        public int DemoInt { get; set; }
        public DateTime? DemoDateTime { get; set; }

        public List<DemoDetailModel> Details { get; set; }
    }

    public class DemoDetailModel
    {
        [DataType(DataType.Text)]
        public string DemoString { get; set; }

        public int DemoInt { get; set; }
        public int DemoId { get; set; }
    }

    public class LogsModel
    {
        [DataType(DataType.Text)]
        public string AppCode { get; set; }

        [DataType(DataType.Text)]
        public string Message { get; set; }
    }
}