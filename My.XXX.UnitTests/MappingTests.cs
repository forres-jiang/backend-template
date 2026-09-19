using Microsoft.VisualStudio.TestTools.UnitTesting;
using My.XXX.Persistence.PersistantObjects;
using My.XXX.Service.DTOs;
using My.XXX.Service.Mapping;
using Newtonsoft.Json;
using System;

namespace My.XXX.UnitTests;

[TestClass]
public class MappingTests
{
    private readonly ApplicationMapper mapper = new();

    [TestMethod]
    public void ObjectMappingsPreserveMatchingPropertiesIncludingInheritedMembers()
    {
        var demo = Populate<DemoModel>();
        AssertMatchingProperties(demo, mapper.ToDemo(demo));
        var detail = Populate<DemoDetailModel>();
        AssertMatchingProperties(detail, mapper.ToDemoDetail(detail));
        var mail = Populate<Mail>();
        AssertMatchingProperties(mail, mapper.ToMailQueue(mail));
        var saveMenu = Populate<SaveMenu>();
        AssertMatchingProperties(saveMenu, mapper.ToMenu(saveMenu));
        var menu = Populate<Menus>();
        AssertMatchingProperties(menu, mapper.ToSaveMenu(menu));
        AssertMatchingProperties(menu, mapper.ToMenuDto(menu));
        AssertMatchingProperties(menu, mapper.ToMenuBaseDto(menu));
        var picker = mapper.ToMenuSearchPickerDto(menu);
        AssertMatchingProperties(menu, picker);
        Assert.AreEqual(menu.Id, picker.Value);
        var baseDto = mapper.ToMenuBaseDto(menu);
        AssertMatchingProperties(baseDto, mapper.ToMenuSearchPickerFromBase(baseDto));
        Assert.AreEqual(menu.Id, mapper.ToMenuSearchPickersFromBase(new[] { baseDto })[0].Value);
        var metrics = Populate<MetricsInfo>();
        AssertMatchingProperties(metrics, mapper.ToOperation(metrics), "Inputs", "ReturnValue");
    }

    [TestMethod]
    public void NullObjectsStayNullAndNullCollectionsBecomeEmpty()
    {
        Assert.IsNull(mapper.ToDemo(null));
        Assert.IsNull(mapper.ToDemoDetail(null));
        Assert.IsNull(mapper.ToMailQueue(null));
        Assert.IsNull(mapper.ToMenu(null));
        Assert.IsNull(mapper.ToSaveMenu(null));
        Assert.IsNull(mapper.ToMenuDto(null));
        Assert.IsNull(mapper.ToMenuBaseDto(null));
        Assert.IsNull(mapper.ToMenuSearchPickerDto(null));
        Assert.IsNull(mapper.ToOperation(null));
        Assert.AreEqual(0, mapper.ToDemoDetails(null).Count);
        Assert.AreEqual(0, mapper.ToMenuDtos(null).Count);
        Assert.AreEqual(0, mapper.ToMenuBaseDtos(null).Count);
        Assert.AreEqual(0, mapper.ToMenuSearchPickerDtos(null).Count);
        Assert.AreEqual(0, mapper.ToMenuSearchPickersFromBase(null).Count);
    }

    [TestMethod]
    public void ListsPreserveOrderAndCreateNewObjects()
    {
        var menus = new[] { new Menus { Id = 9 }, new Menus { Id = 2 } };
        var dtos = mapper.ToMenuDtos(menus);
        Assert.AreEqual(9, dtos[0].Id);
        Assert.AreEqual(2, dtos[1].Id);
        dtos[0].DisplayName = "changed";
        Assert.IsNull(menus[0].DisplayName);
        Assert.AreEqual(2, mapper.ToMenuBaseDtos(menus)[1].Id);
        Assert.AreEqual(9, mapper.ToMenuSearchPickerDtos(menus)[0].Value);
        var details = mapper.ToDemoDetails(new[] { new DemoDetailModel { DemoId = 7, DemoString = "detail" } });
        Assert.AreEqual(7, details[0].DemoId);
        Assert.AreEqual("detail", details[0].DemoString);
        Assert.AreEqual(0, mapper.ToMenuDtos(Array.Empty<Menus>()).Count);
    }

    [TestMethod]
    public void UnmappedFieldsAndNullableBooleanKeepTheirDefaults()
    {
        var menu = mapper.ToMenu(new SaveMenu { IsAction = null });
        Assert.IsFalse(menu.IsAction);
        Assert.AreEqual(0, menu.Id);
        Assert.AreEqual(default(DateTime), menu.CreatedTime);
        Assert.IsNull(menu.DisplayName);
        var dto = mapper.ToMenuDto(new Menus());
        Assert.IsFalse(dto.Checked);
        Assert.IsNull(dto.Actions);
        Assert.IsNull(dto.Children);
        Assert.IsNull(mapper.ToDemo(new DemoModel()).DemoString);
        Assert.AreEqual(Guid.Empty, mapper.ToDemo(new DemoModel()).DemoGUID);
    }

    [TestMethod]
    public void MetricsKeepNewtonsoftJsonSerializationForObjectsStringsAndNulls()
    {
        var metrics = new MetricsInfo
        {
            Inputs = new { Name = "中文", Values = new[] { 1, 2 } },
            ReturnValue = "quoted\"value"
        };
        var operation = mapper.ToOperation(metrics);
        Assert.AreEqual(JsonConvert.SerializeObject(metrics.Inputs), operation.Inputs);
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
