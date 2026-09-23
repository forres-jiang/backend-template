using My.XXX.Services.Menus.Models;
#nullable enable
using My.XXX.Contracts.DTOs;
using Riok.Mapperly.Abstractions;
using System.Collections.Generic;
using System.Linq;
namespace My.XXX.Services.Menus.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target,
    ThrowOnMappingNullMismatch = false, ThrowOnPropertyMappingNullMismatch = false)]
public partial class ApplicationMapper
{
    public static string? WriteLocalizedText(LocalizedText? value) =>
        My.XXX.Contracts.Serialization.LocalizedNamesJson.Write(value?.Values);
    // Encoding happens after localization and tree composition, at the output boundary.
    public List<MenuDto> ToTreeDtos(IEnumerable<MenuNode> nodes) => nodes.Select(node =>
    {
        var dto = ToMenuDto(node.Menu)!;
        dto.Checked = node.Checked;
        dto.Actions = node.Actions == null ? null : ToTreeDtos(node.Actions);
        dto.Children = node.Children == null ? null : ToTreeDtos(node.Children);
        return dto;
    }).ToList();

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
