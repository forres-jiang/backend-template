using LinqToDB.Mapping;
using System;

namespace My.XXX.Persistence.PersistantObjects
{
    [Table(Name = "Demo")]
    public class Demo
    {
        [PrimaryKey, Identity]
        public int Id { get; set; }

        [Column(Name = "DemoGUID"), Nullable]
        public Guid DemoGUID { get; set; }

        [Column(Name = "DemoString"), Nullable]
        public string DemoString { get; set; }

        [Column(Name = "DemoInt"), Nullable]
        public int DemoInt { get; set; }

        [Column(Name = "DemoDateTime"), Nullable]
        public DateTime? DemoDateTime { get; set; }

        [Column(Name = "DemoBoolean"), Nullable]
        public bool DemoBoolean { get; set; }

        [Column(Name = "DemoDecimal"), Nullable]
        public decimal DemoDecimal { get; set; }
    }

    [Table(Name = "DemoDetail")]
    public class DemoDetail
    {
        [PrimaryKey, Identity]
        public int Id { get; set; }

        [Column(Name = "DemoString"), NotNull]
        public string DemoString { get; set; }

        [Column(Name = "DemoInt"), NotNull]
        public int DemoInt { get; set; }

        [Column(Name = "DemoId"), NotNull]
        public int DemoId { get; set; }
    }

    [Table(Schema = "dbo", Name = "Menus")]
    public class Menus
    {
        [PrimaryKey, Identity]
        public int Id { get; set; }

        [Column, Nullable]
        public string DisplayName { get; set; }

        /// <summary>
        /// 多语言名称
        /// </summary>
        [Column, Nullable]
        public string DisplayNames { get; set; }

        [Column, Nullable]
        public string Description { get; set; }

        [Column, Nullable]
        public string Icon { get; set; }

        [Column, NotNull]
        public int Number { get; set; }

        [Column, Nullable]
        public string Url { get; set; }

        /// <summary>
        /// 前端组件路基
        /// </summary>
        [Column, Nullable]
        public string Component { get; set; }

        [Column, Nullable]
        public string ControllerName { get; set; }

        [Column, Nullable]
        public string ActionName { get; set; }

        [Column, Nullable]
        public string LinkTarget { get; set; }

        [Column, NotNull]
        public int ParentId { get; set; }

        [Column, NotNull]
        public bool IsDisplay { get; set; }

        [Column, NotNull]
        public bool IsAction { get; set; }

        [Column, NotNull]
        public bool IsDeleted { get; set; }

        [Column, NotNull]
        public string CreatedBy { get; set; }

        [Column, NotNull]
        public DateTime CreatedTime { get; set; }

        [Column, Nullable]
        public string UpdatedBy { get; set; }

        [Column, Nullable]
        public DateTime? UpdatedTime { get; set; }
    }

    [Table(Schema = "dbo", Name = "RoleMenu")]
    public class RoleMenu
    {
        [PrimaryKey, Identity]
        public int Id { get; set; }

        [Column, NotNull]
        public Guid RoleId { get; set; }

        [Column, NotNull]
        public int MenuId { get; set; }

        [Column, NotNull]
        public bool IsDeleted { get; set; }

        [Column, NotNull]
        public string CreatedBy { get; set; }

        [Column, NotNull]
        public DateTime CreatedTime { get; set; }

        [Column, Nullable]
        public string UpdatedBy { get; set; }

        [Column, Nullable]
        public DateTime? UpdatedTime { get; set; }
    }

    [Table(Schema = "dbo", Name = "RequestLogs")]
    public class Operation
    {
        [Column, NotNull]
        public string AppCode { get; set; }

        [Column, NotNull]
        public string HostName { get; set; }

        [Column, NotNull]
        public DateTime CreateTime { get; set; }

        [Column, NotNull]
        public string ControllerName { get; set; }

        [Column, NotNull]
        public string ActionName { get; set; }

        [Column, Nullable]
        public double TotalTime { get; set; }

        [Column, Nullable]
        public string ClientIP { get; set; }

        [Column, Nullable]
        public string UserId { get; set; }

        [Column, Nullable]
        public string UserName { get; set; }

        [Column, Nullable]
        public string Inputs { get; set; }

        [Column, Nullable]
        public string Url { get; set; }

        [Column, Nullable]
        public string ReturnValue { get; set; }

        [Column, Nullable]
        public Guid? RequestId { get; set; }

        [Column, Nullable]
        public string RequestType { get; set; }

        [Column, NotNull]
        public bool IsException { get; set; }

        [Column, Nullable]
        public string Message { get; set; }

        [Column, Nullable]
        public string StackTrace { get; set; }
    }
}