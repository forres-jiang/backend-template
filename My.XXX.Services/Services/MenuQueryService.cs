using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using My.XXX.Services.Common;
using My.XXX.Service.DTOs;
using My.XXX.Services.Interfaces;
using My.XXX.Services.Mapping;
using My.XXX.Services.Models;
using My.XXX.Services.Ports;
using My.XXX.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
namespace My.XXX.Services;

public sealed class MenuQueryService(IMenuReadRepository _menuRepository, ICurrentCulture _currentRequest, ApplicationMapper _mapper)
{
    public async Task<MenuBaseDto> Get(int menuId, CancellationToken cancellationToken = default)
    {
        var menu = (await _menuRepository.Get(menuId, cancellationToken));
        if (menu != null)
        {
            var menuDto = _mapper.ToMenuBaseDto(menu);
            menuDto.DisplayName = MenuDisplayNames.Get(menu.DisplayNames, _currentRequest.CultureName, menu.DisplayName);
            return menuDto;
        }
        else
        {
            return null;
        }
    }

    public async Task<List<MenuDto>> GetMenuByRoles(RoleMenuQuery query, CancellationToken cancellationToken = default)
    {
        var dbMenuAction = (await _menuRepository.GetRoleMenuByRoles(query.RoleIds, cancellationToken))
            .Where(m => m.IsDisplay).ToList();

        var dtoList = _mapper.ToMenuDtos(dbMenuAction);

        SetMenuLanguage(dtoList);

        var dbMenus = dtoList.Where(m => !m.IsAction).ToList();

        foreach (var item in dbMenus)
        {
            var actions = dbMenuAction.Where(m => m.ParentId == item.Id && m.IsAction).ToList();
            if (actions != null && actions.Count > 0)
            {
                item.Actions = _mapper.ToMenuDtos(actions);
                SetMenuLanguage(item.Actions);
            }
        }

        var tree = MenuTree.Build(dbMenus);
        return tree;
    }

    public async Task<List<MenuDto>> GetMenuTreeCheckedByRoles(List<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        var menus = (await _menuRepository.GetMenus(cancellationToken: cancellationToken));
        var list = _mapper.ToMenuDtos(menus);

        SetMenuLanguage(list);

        var roleMenus = (await _menuRepository.GetRoleMenuByRoles(roleIds, cancellationToken)).Select(m => m.Id).Distinct().ToList();

        foreach (var item in roleMenus)
        {
            var m = list.Where(m => m.Id == item).FirstOrDefault();
            if (m != null)
            {
                m.Checked = true;
            }
        }

        var tree = MenuTree.Build(list);
        return tree;
    }

    public async Task<List<MenuDto>> GetTreeMenus(bool? isDisplay, CancellationToken cancellationToken = default)
    {
        var list = _mapper.ToMenuDtos((await _menuRepository.GetMenus(isDisplay: isDisplay, cancellationToken: cancellationToken)));
        SetMenuLanguage(list);
        return MenuTree.Build(list);
    }

    public async Task<Paged<MenuSearchPickerDto>> SearchMenus(QueryMenu query, CancellationToken cancellationToken = default)
    {
        var result = await Search(query, picker: true, cancellationToken);
        var dtoList = _mapper.ToMenuSearchPickersFromBase(result.List)
            .OrderBy(m => m.ParentId).ThenBy(m => m.Number).ToList();

        SetMenuLanguage(dtoList);
        return Paged<MenuSearchPickerDto>.Create(dtoList, result.Total);
    }
    public async Task<Result<List<MenuBase>>> GetMenus(CancellationToken cancellationToken = default) => Result.Ok(_mapper.ToMenuBases((await _menuRepository.GetMenus(cancellationToken: cancellationToken))));
    public async Task<Paged<MenuBaseDto>> GetMenus(QueryMenu query, CancellationToken cancellationToken = default)
    {
        return await Search(query, picker: false, cancellationToken);
    }
    private async Task<Paged<MenuBaseDto>> Search(QueryMenu query, bool picker, CancellationToken cancellationToken = default)
    {
        var criteria = new MenuSearch(query.DisplayName, picker ? false : query.IsAction,
            picker || query.IsDisplay, query.ParentId, query.PageIndex, Math.Clamp(query.PageSize, 1, 100));
        var page = (await _menuRepository.Search(criteria, cancellationToken));
        var result = page.List.Select(menu =>
        {
            var dto = _mapper.ToMenuBaseDto(menu);
            dto.DisplayName = MenuDisplayNames.Get(menu.DisplayNames, _currentRequest.CultureName, menu.DisplayName);
            return dto;
        }).ToList();
        return Paged<MenuBaseDto>.Create(result, page.Total);
    }
    public void SetMenuLanguage<T>(List<T> menus) where T : ILocalizedMenuDto
    {
        foreach (var menu in menus)
            menu.DisplayName = MenuDisplayNames.Get(menu.DisplayNames, _currentRequest.CultureName, menu.DisplayName);
    }
}
