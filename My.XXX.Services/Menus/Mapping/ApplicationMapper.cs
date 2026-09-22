using My.XXX.Services.Menus.Models;
#nullable enable
using My.XXX.Contracts.DTOs;
using Riok.Mapperly.Abstractions;
using System.Collections.Generic;
namespace My.XXX.Services.Menus.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target,
    ThrowOnMappingNullMismatch = false, ThrowOnPropertyMappingNullMismatch = false)]
public partial class ApplicationMapper
{
    public static LocalizedText? ReadLocalizedText(string? value) => value == null ? null :
        new LocalizedText(My.XXX.Contracts.Serialization.LocalizedNamesJson.Read(value));
    public static string? WriteLocalizedText(LocalizedText? value) =>
        My.XXX.Contracts.Serialization.LocalizedNamesJson.Write(value?.Values);
    [MapperIgnoreTarget(nameof(MenuDto.Checked))]
    [MapperIgnoreTarget(nameof(MenuDto.Actions))]
    [MapperIgnoreTarget(nameof(MenuDto.Children))]
    public partial MenuDto? ToMenuDto(MenuState? source);
    public partial List<MenuDto> ToMenuDtos(IEnumerable<MenuState>? source);
    public partial MenuBaseDto? ToMenuBaseDto(MenuState? source);
    public partial List<MenuBaseDto> ToMenuBaseDtos(IEnumerable<MenuState>? source);
    public partial List<MenuBase> ToMenuBases(IEnumerable<MenuState>? source);
    [MapperIgnoreTarget(nameof(MenuSearchPickerDto.DisplayNames))]
    public partial MenuSearchPickerDto? ToMenuSearchPickerFromBase(MenuBaseDto? source);
    public partial List<MenuSearchPickerDto> ToMenuSearchPickersFromBase(IEnumerable<MenuBaseDto>? source);
}
