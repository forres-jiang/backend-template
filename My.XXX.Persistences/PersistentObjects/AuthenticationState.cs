using LinqToDB;
using LinqToDB.Mapping;
using System;
namespace My.XXX.Persistences.PersistentObjects;
[Table(Schema = "dbo", Name = "AuthenticationUsers")]
[Table(Configuration = ProviderName.PostgreSQL, Schema = "public", Name = "AuthenticationUsers")]
public sealed class AuthenticationUser
{
    [PrimaryKey, Column, NotNull] public string UserId { get; set; }
    [Column, NotNull] public string UserJson { get; set; }
    [Column] public bool Enabled { get; set; }
}
[Table(Schema = "dbo", Name = "AuthenticationSessions")]
[Table(Configuration = ProviderName.PostgreSQL, Schema = "public", Name = "AuthenticationSessions")]
public sealed class AuthenticationSessionRow
{
    [PrimaryKey, Column, NotNull] public string SessionId { get; set; }
    [Column, NotNull] public string UserId { get; set; }
    [Column, NotNull] public string RefreshTokenId { get; set; }
    [Column] public DateTime ExpiresUtc { get; set; }
    [Column] public bool Revoked { get; set; }
}
