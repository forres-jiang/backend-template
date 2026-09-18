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

    public class MailQueueDto
    {
        public int MAILSEQ { get; set; }
        public string APPCODE { get; set; }
        public string MTO { get; set; }
        public string MFROM { get; set; }
        public string REPLYTO { get; set; }
        public string CC { get; set; }
        public string BCC { get; set; }
        public string ORGANISATION { get; set; }
        public string SUBJECT { get; set; }
        public string CONTENT { get; set; }
        public string SUBMITBY { get; set; }
        public DateTime SUBMITDATE { get; set; }
        public char POSTEDFLAG { get; set; }
        public DateTime SENDDATE { get; set; }
        public char IMMEDIATEFLAG { get; set; }
        public DateTime? POSTDATE { get; set; }
        public string ERRMSG { get; set; }
        public string ENCODE { get; set; }
        public string ReferID { get; set; }
    }

    public class AttachmentDto
    {
        public int AttachmentId { get; set; }
        public string AttachmentName { get; set; }
        public string AttachmentFileName { get; set; }
        public byte[] AttachmentContent { get; set; }
        public string AttachmentMimeType { get; set; }
        public bool LinkedResourceFlag { get; set; }
    }
}
