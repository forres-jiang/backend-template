#nullable enable
using My.XXX.Service.DTOs;
using My.XXX.Service.Models;
using Riok.Mapperly.Abstractions;
using System.Collections.Generic;
namespace My.XXX.Service.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target,
    ThrowOnMappingNullMismatch = false, ThrowOnPropertyMappingNullMismatch = false)]
public partial class ApplicationMapper
{
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
