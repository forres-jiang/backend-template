using Microsoft.VisualStudio.TestTools.UnitTesting;
using My.XXX.Contracts.DTOs;
using My.XXX.Persistences.Mapping;
using My.XXX.Persistences.PersistentObjects;
using My.XXX.Services.Menus.Mapping;
using My.XXX.Services.Menus.Models;
using Newtonsoft.Json;
using System;

namespace My.XXX.UnitTests;

[TestClass]
public class MappingTests
{
    private readonly PersistenceMapper mapper = new();
    private readonly ApplicationMapper application = new();

    [TestMethod]
    public void ObjectMappingsPreserveMatchingPropertiesIncludingInheritedMembers()
    {
        var demo = Populate<DemoModel>();
        AssertMatchingProperties(demo, mapper.ToDemo(demo));
        var detail = Populate<DemoDetailModel>();
        AssertMatchingProperties(detail, mapper.ToDemoDetail(detail));

        var menu = Populate<Menus>();
        var state = mapper.ToMenuState(menu);
        AssertMatchingProperties(menu, state);
        AssertMatchingProperties(menu, mapper.ToMenuEntity(state));
        AssertMatchingProperties(menu, application.ToMenuDto(state));
        AssertMatchingProperties(menu, application.ToMenuBaseDto(state));
        var baseDto = application.ToMenuBaseDto(state);
        AssertMatchingProperties(baseDto, application.ToMenuSearchPickerFromBase(baseDto));
        Assert.AreEqual(menu.Id, application.ToMenuSearchPickersFromBase(new[] { baseDto })[0].Value);
        var metrics = Populate<MetricsInfo>();
        AssertMatchingProperties(metrics, mapper.ToOperation(metrics), "Inputs", "ReturnValue");
    }

    [TestMethod]
    public void NullObjectsStayNullAndNullCollectionsBecomeEmpty()
    {
        Assert.IsNull(mapper.ToDemo(null));
        Assert.IsNull(mapper.ToDemoDetail(null));
        Assert.IsNull(mapper.ToMenuState(null));
        Assert.IsNull(mapper.ToMenuEntity(null));
        Assert.IsNull(application.ToMenuDto(null));
        Assert.IsNull(application.ToMenuBaseDto(null));
        Assert.IsNull(application.ToMenuSearchPickerFromBase(null));
        Assert.IsNull(mapper.ToOperation(null));
        Assert.AreEqual(0, mapper.ToDemoDetails(null).Count);
        Assert.AreEqual(0, mapper.ToMenuStates(null).Count);
        Assert.AreEqual(0, application.ToMenuDtos(null).Count);
        Assert.AreEqual(0, application.ToMenuBaseDtos(null).Count);
        Assert.AreEqual(0, application.ToMenuSearchPickersFromBase(null).Count);
    }

    [TestMethod]
    public void ListsPreserveOrderAndCreateNewObjects()
    {
        var menus = new[] { new Menus { Id = 9 }, new Menus { Id = 2 } };
        var states = mapper.ToMenuStates(menus);
        var dtos = application.ToMenuDtos(states);
        Assert.AreEqual(9, dtos[0].Id);
        Assert.AreEqual(2, dtos[1].Id);
        dtos[0].DisplayName = "changed";
        Assert.IsNull(menus[0].DisplayName);
        Assert.AreEqual(2, application.ToMenuBaseDtos(states)[1].Id);
        Assert.AreEqual(9, application.ToMenuSearchPickersFromBase(application.ToMenuBaseDtos(states))[0].Value);
        var details = mapper.ToDemoDetails(new[] { new DemoDetailModel { DemoId = 7, DemoString = "detail" } });
        Assert.AreEqual(7, details[0].DemoId);
        Assert.AreEqual("detail", details[0].DemoString);
        Assert.AreEqual(0, application.ToMenuDtos(Array.Empty<MenuState>()).Count);
    }

    [TestMethod]
    public void UnmappedFieldsKeepTheirDefaults()
    {
        var menu = mapper.ToMenuEntity(new MenuState());
        Assert.IsFalse(menu.IsAction);
        Assert.AreEqual(0, menu.Id);
        Assert.AreEqual(default(DateTime), menu.CreatedTime);
        Assert.IsNull(menu.DisplayName);
        var dto = application.ToMenuDto(new MenuState());
        Assert.IsFalse(dto.Checked);
        Assert.IsNull(dto.Actions);
        Assert.IsNull(dto.Children);
        Assert.IsNull(mapper.ToDemo(new DemoModel()).DemoString);
        Assert.AreEqual(Guid.Empty, mapper.ToDemo(new DemoModel()).DemoGUID);
    }

    [TestMethod]
    public void MetricsPreserveSerializedPayloadsWithoutDoubleEncoding()
    {
        var metrics = new MetricsInfo
        {
            Inputs = JsonConvert.SerializeObject(new { Name = "中文", Values = new[] { 1, 2 } }),
            ReturnValue = JsonConvert.SerializeObject("quoted\"value")
        };
        var operation = mapper.ToOperation(metrics);
        Assert.AreEqual(metrics.Inputs, operation.Inputs);
        Assert.AreEqual("\"quoted\\\"value\"", operation.ReturnValue);
        operation = mapper.ToOperation(new MetricsInfo());
        Assert.AreEqual("null", operation.Inputs);
        Assert.AreEqual("null", operation.ReturnValue);
    }

    private static T Populate<T>() where T : new()
    {
        var value = new T();
        foreach (var property in typeof(T).GetProperties())
        {
            if (!property.CanWrite) continue;
            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            object sample = type == typeof(string) ? property.Name + "中文"
                : type == typeof(int) ? 42
                : type == typeof(bool) ? true
                : type == typeof(double) ? 12.5
                : type == typeof(DateTime) ? new DateTime(2026, 9, 18, 12, 30, 0)
                : type == typeof(Guid) ? Guid.Parse("6455bbcd-9db1-4b0d-8ec6-831301902f6a")
                : null;
            if (sample != null) property.SetValue(value, sample);
        }
        return value;
    }

    private static void AssertMatchingProperties(object source, object target, params string[] ignored)
    {
        Assert.IsNotNull(target);
        foreach (var property in target.GetType().GetProperties())
        {
            if (Array.IndexOf(ignored, property.Name) >= 0) continue;
            var sourceProperty = source.GetType().GetProperty(property.Name);
            if (sourceProperty != null)
                Assert.AreEqual(sourceProperty.GetValue(source), property.GetValue(target), property.Name);
        }
    }
}
