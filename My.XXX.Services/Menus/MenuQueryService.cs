using FluentResults;
using My.XXX.Contracts.DTOs;
using My.XXX.Services.Abstractions.Interfaces;
using My.XXX.Services.Menus.Mapping;
using My.XXX.Services.Menus.Models;
using My.XXX.Services.Menus.Policies;
using My.XXX.Services.Menus.Ports;
using My.XXX.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace My.XXX.Services.Menus;

public sealed class MenuQueryService(IMenuReadRepository _menuRepository, ICurrentCulture _currentRequest, ApplicationMapper _mapper)
{
    public async Task<Result<MenuBaseDto>> FindAsync(int menuId, CancellationToken cancellationToken = default)
    {
        var menu = (await _menuRepository.Get(menuId, cancellationToken));
        if (menu != null)
        {
            var menuDto = _mapper.ToMenuBaseDto(menu);
            menuDto.DisplayName = MenuDisplayNames.Get(menu.DisplayNames, _currentRequest.CultureName, menu.DisplayName);
            return Result.Ok(menuDto);
        }
        else
        {
            return Result.Fail<MenuBaseDto>(MenuErrors.NotFound());
        }
    }

    public async Task<List<MenuDto>> GetMenuByRoles(RoleMenuQuery query, CancellationToken cancellationToken = default)
    {
        var menus = (await _menuRepository.GetRoleMenuByRoles(query.RoleIds, cancellationToken))
            .Where(m => m.IsDisplay).Select(Localize).ToList();
        var actions = menus.Where(m => m.IsAction).ToLookup(m => m.ParentId);
        var nodes = menus.Where(m => !m.IsAction).Select(menu => new MenuNode(menu)
        {
            Actions = actions.Contains(menu.Id) ? actions[menu.Id].Select(action => new MenuNode(action)).ToList() : null
        }).ToList();
        return _mapper.ToTreeDtos(MenuTree.Build(nodes));
    }

    public async Task<List<MenuDto>> GetMenuTreeCheckedByRoles(List<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        var menus = await _menuRepository.GetMenus(cancellationToken: cancellationToken);
        var selected = (await _menuRepository.GetRoleMenuByRoles(roleIds, cancellationToken)).Select(m => m.Id).ToHashSet();
        var nodes = menus.Select(menu => new MenuNode(Localize(menu)) { Checked = selected.Contains(menu.Id) }).ToList();
        return _mapper.ToTreeDtos(MenuTree.Build(nodes));
    }

    public async Task<List<MenuDto>> GetTreeMenus(bool? isDisplay, CancellationToken cancellationToken = default)
    {
        var menus = await _menuRepository.GetMenus(isDisplay: isDisplay, cancellationToken: cancellationToken);
        return _mapper.ToTreeDtos(MenuTree.Build(menus.Select(menu => new MenuNode(Localize(menu))).ToList()));
    }

    public async Task<Paged<MenuSearchPickerDto>> SearchMenus(QueryMenu query, CancellationToken cancellationToken = default)
    {
        var result = await Search(query, picker: true, cancellationToken);
        var dtoList = _mapper.ToMenuSearchPickersFromBase(result.List)
            .OrderBy(m => m.ParentId).ThenBy(m => m.Number).ToList();

        return Paged<MenuSearchPickerDto>.Create(dtoList, result.Total);
    }
    public async Task<Result<List<MenuBase>>> GetMenus(CancellationToken cancellationToken = default) => Result.Ok(_mapper.ToMenuBases((await _menuRepository.GetMenus(cancellationToken: cancellationToken))));
    public async Task<Paged<MenuBaseDto>> GetMenus(QueryMenu query, CancellationToken cancellationToken = default)
    {
        return await Search(query, picker: false, cancellationToken);
    }
    private async Task<Paged<MenuBaseDto>> Search(QueryMenu query, bool picker, CancellationToken cancellationToken = default)
    {
        var paging = My.XXX.Services.Abstractions.Models.PageWindow.FromRequest(query.PageIndex, query.PageSize);
        var criteria = new MenuSearch(query.DisplayName, picker ? false : query.IsAction,
            picker || query.IsDisplay, query.ParentId, paging.PageIndex, paging.Size);
        var page = (await _menuRepository.Search(criteria, cancellationToken));
        var result = page.List.Select(menu => _mapper.ToMenuBaseDto(Localize(menu))).ToList();
        return Paged<MenuBaseDto>.Create(result, page.Total);
    }
    private MenuState Localize(MenuState menu)
    {
        var copy = menu.Copy();
        copy.DisplayName = MenuDisplayNames.Get(menu.DisplayNames, _currentRequest.CultureName, menu.DisplayName);
        return copy;
    }
}
