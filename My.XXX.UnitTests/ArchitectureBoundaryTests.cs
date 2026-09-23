using Microsoft.VisualStudio.TestTools.UnitTesting;
using My.XXX.Contracts.DTOs;
using My.XXX.Persistences.Repositories;
using My.XXX.Services.Abstractions.Interfaces;
using My.XXX.Services.AccessControl.Ports;
using My.XXX.Services.Authentication;
using My.XXX.Services.Authentication.Interfaces;
using My.XXX.Services.Authentication.Models;
using My.XXX.Services.Authentication.Ports;
using My.XXX.Services.Authorization;
using My.XXX.Services.Authorization.Interfaces;
using My.XXX.Services.Menus;
using My.XXX.Services.Menus.Models;
using My.XXX.Services.Menus.Ports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static My.XXX.UnitTests.ArchitectureDependencies;

namespace My.XXX.UnitTests;

[TestClass]
public class ArchitectureBoundaryTests
{
    [TestMethod]
    public void IdentityAndNavigationModelsDoNotTransitivelyContainWireTypes()
    {
        foreach (var root in new[] { typeof(UserIdentity), typeof(ActiveSession), typeof(MenuNode) })
            foreach (var type in ModelGraph(root))
                Assert.AreNotEqual(typeof(UserInfo).Assembly, type.Assembly, $"{root} -> {type}");
        foreach (var root in new[] { typeof(ICurrentUser), typeof(IAuthenticationStore) })
            foreach (var dependency in Dependencies(root))
                Assert.AreNotEqual(typeof(UserInfo).Assembly, dependency.Assembly, $"{root} -> {dependency}");
    }

    [TestMethod]
    public void AuthorizationUsesIdentityInsteadOfTheLegacyUserResponse()
    {
        foreach (var type in typeof(My.XXX.APIs.Common.PermissionsHandler).Assembly.GetTypes()
            .Where(t => Owner(t) == typeof(My.XXX.APIs.Common.PermissionsHandler)))
            foreach (var dependency in Dependencies(type))
            {
                Assert.AreNotEqual(typeof(UserInfo), dependency);
                Assert.AreNotEqual(typeof(IUserService), dependency);
            }
    }

    [TestMethod]
    public void IdentityCollectionsAndLegacyResponseCannotMutateCurrentIdentity()
    {
        var roles = new List<string> { "Reader" };
        var ids = new List<Guid> { Guid.NewGuid() };
        var identity = new UserIdentity { UserId = "u", Roles = roles, RoleIds = ids };
        roles.Clear(); ids.Clear();
        CollectionAssert.AreEqual(new[] { "Reader" }, identity.Roles.ToArray());
        Assert.HasCount(1, identity.RoleIds);
        Assert.ThrowsExactly<NotSupportedException>(() => ((IList<string>)identity.Roles).Clear());
        var users = new UserService(new Context(identity));
        var response = users.CurrentUser;
        Assert.IsNotNull(response.Menus);
        response.Roles.Clear(); response.RoleIds.Clear();
        Assert.HasCount(1, users.CurrentUser.Roles);
        Assert.HasCount(1, identity.RoleIds);
        var replacement = identity with { Roles = Array.Empty<string>() };
        Assert.HasCount(0, replacement.Roles);
        Assert.HasCount(1, identity.Roles);
    }

    [TestMethod]
    public void MutationOperationsCannotOpenTransactionsOrReadAmbientIdentity()
    {
        var mutations = new[] { typeof(MenuMutations), typeof(RoleMenuMutations), typeof(PermissionMutations) };
        foreach (var type in typeof(MenuMutations).Assembly.GetTypes().Where(t => mutations.Contains(Owner(t))))
            foreach (var dependency in Dependencies(type))
            {
                Assert.AreNotEqual(typeof(IAccessControlTransaction), dependency, type.FullName);
                Assert.AreNotEqual(typeof(ICurrentUser), dependency, type.FullName);
                Assert.AreNotEqual(typeof(ICurrentCulture), dependency, type.FullName);
            }
        Assert.IsFalse(typeof(IAccessControlTransaction).IsAssignableFrom(typeof(MenuReadRepository)));
        Assert.IsFalse(typeof(IMenuReadRepository).IsAssignableFrom(typeof(AccessControlTransaction)));
    }

    [TestMethod]
    public void PermissionQueriesExposeOnlyAsyncBusinessReads()
    {
        foreach (var method in typeof(IPermissionQuery).GetMethods())
        {
            Assert.IsFalse(method.Name.Contains("Cache", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(typeof(Task).IsAssignableFrom(method.ReturnType), method.Name);
        }
        foreach (var dependency in Dependencies(typeof(MenuQueryService)))
            Assert.IsFalse((dependency.Namespace ?? "").StartsWith("My.XXX.Contracts.Serialization") ||
                (dependency.Namespace ?? "").StartsWith("Newtonsoft"), dependency.FullName);
    }

    [TestMethod]
    public void DependencyScannerFindsNestedGenericTypesAndMethodOnlyDependencies()
    {
        Assert.IsTrue(Dependencies(typeof(SignatureFixture)).Contains(typeof(MenuDto)));
        Assert.IsTrue(Dependencies(typeof(BodyFixture)).Contains(typeof(IMenuReadRepository)),
            "Generic method arguments in executable code must be inspected.");
        Assert.IsTrue(ModelGraph(typeof(SignatureFixture)).Contains(typeof(MenuDto)),
            "Model graph traversal must follow nested generic properties.");
    }

    private sealed class SignatureFixture
    {
        public List<Task<MenuDto[]>> Menus { get; set; }
    }
    private sealed class BodyFixture
    {
        public object Resolve() => Generic<IMenuReadRepository>();
        private static object Generic<T>() => typeof(T);
    }
    private sealed class Context(UserIdentity identity) : IAuthenticationSession
    {
        public UserIdentity User => identity;
        public bool IsAuthenticated => true;
        public DateTime TokenExpirationTime => DateTime.UtcNow;
        public string SessionId => "session";
        public string TokenId => "token";
    }
}
