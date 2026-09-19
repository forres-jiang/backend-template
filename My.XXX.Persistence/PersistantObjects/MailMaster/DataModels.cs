using LinqToDB;
using LinqToDB.Mapping;
using System;

namespace My.XXX.Persistence.PersistantObjects
{

    [Table(Schema = "dbo", Name = "MAILQUEUE")]
    [Table(Configuration = ProviderName.PostgreSQL, Schema = "public", Name = "MAILQUEUE")]
    public partial class MailQueue
    {
        [PrimaryKey, Identity]
        public int MAILSEQ { get; set; } // int

        [Column, NotNull]
        public string APPCODE { get; set; } // varchar(20)

        [Column, NotNull]
        public string MTO { get; set; } // varchar(400)

        [Column, NotNull]
        public string MFROM { get; set; } // varchar(60)

        [Column, NotNull]
        public string REPLYTO { get; set; } // varchar(400)

        [Column, Nullable]
        public string CC { get; set; } // varchar(400)

        [Column, Nullable]
        public string BCC { get; set; } // varchar(1000)

        [Column, Nullable]
        public string ORGANISATION { get; set; } // varchar(60)

        [Column, Nullable]
        public string SUBJECT { get; set; } // nvarchar(1000)

        [Column, Nullable]
        public string CONTENT { get; set; } // ntext

        [Column, NotNull]
        public string SUBMITBY { get; set; } // varchar(60)

        [Column, NotNull]
        public DateTime SUBMITDATE { get; set; } // datetime

        [Column, NotNull]
        public char POSTEDFLAG { get; set; } // char(1)

        [Column, NotNull]
        public DateTime SENDDATE { get; set; } // datetime

        [Column, NotNull]
        public char IMMEDIATEFLAG { get; set; } // varchar(1)

        [Column, Nullable]
        public DateTime? POSTDATE { get; set; } // datetime

        [Column, Nullable]
        public string ERRMSG { get; set; } // varchar(200)

        [Column, Nullable]
        public string ENCODE { get; set; } // varchar(12)

        [Column, Nullable]
        public string ReferID { get; set; } // varchar(50)
    }

    [Table("AttachmentMapping")]
    public class AttachmentMapping
    {
        [Column("MappingID", IsPrimaryKey = true, IsIdentity = true, SkipOnInsert = true, SkipOnUpdate = true)]
        public int MappingId { get; set; }

        [Column("MailSeq")]
        public int? MailSeq { get; set; }

        [Column("AttachmentID")]
        public int AttachmentId { get; set; }
    }

    [Table("Attachment")]
    public class Attachment
    {
        [Column("AttachmentID", IsPrimaryKey = true, IsIdentity = true, SkipOnInsert = true, SkipOnUpdate = true)]
        public int AttachmentId { get; set; } // int

        [Column("AttachmentName")]
        public string AttachmentName { get; set; } // varchar(50)

        [Column("AttachmentFileName")]
        public string AttachmentFileName { get; set; } // varchar(100)

        [Column("AttachmentContent")]
        public byte[] AttachmentContent { get; set; } // varbinary(max)

        [Column("AttachmentMIMEType")]
        public string AttachmentMimeType { get; set; } // varchar(100)

        [Column("LinkedResourceFlag")]
        public bool LinkedResourceFlag { get; set; } // bit
    }
}