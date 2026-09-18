using System;

namespace My.XXX.Service.DTOs
{
    public class Mail
    {
        public string MTO { get; set; }
        public string MFROM { get; set; }
        public string SUBJECT { get; set; }
        public string CONTENT { get; set; }
        public DateTime SENDDATE { get; set; }
        public string CC { get; set; }
        public string REPLYTO { get; set; }
    }
}