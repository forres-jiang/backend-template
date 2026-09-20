using LinqToDB;
using LinqToDB.Mapping;
namespace My.XXX.Persistence.PersistantObjects;
[Table(Schema = "dbo", Name = "PermissionRevision")]
[Table(Configuration = ProviderName.PostgreSQL, Schema = "public", Name = "PermissionRevision")]
public sealed class PermissionRevision
{
    [PrimaryKey, Column] public int Id { get; set; }
    [Column, NotNull] public long Version { get; set; }
}
