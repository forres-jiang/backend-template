using FluentResults;
using My.XXX.Service.Common;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using My.XXX.Service.Mapping;
using My.XXX.Service.Models;
using My.XXX.Service.Ports;
using My.XXX.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
namespace My.XXX.Service;

public sealed class MenuQueryService(IMenuReadRepository _menuRepository, ICurrentCulture _currentRequest, ApplicationMapper _mapper)
{
    public MenuBaseDto Get(int menuId)
    {
        var menu = _menuRepository.Get(menuId);
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

    public List<MenuDto> GetMenuByRoles(RoleMenuQuery query)
    {
        var dbMenuAction = _menuRepository.GetRoleMenuByRoles(query.RoleIds)
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

    public List<MenuDto> GetMenuTreeCheckedByRoles(List<Guid> roleIds)
    {
        var menus = _menuRepository.GetMenus();
        var list = _mapper.ToMenuDtos(menus);

        SetMenuLanguage(list);

        var roleMenus = _menuRepository.GetRoleMenuByRoles(roleIds).Select(m => m.Id).Distinct().ToList();

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

    public List<MenuDto> GetTreeMenus(bool? isDisplay)
    {
        var list = _mapper.ToMenuDtos(_menuRepository.GetMenus(isDisplay: isDisplay));
        SetMenuLanguage(list);
        return MenuTree.Build(list);
    }

    public Paged<MenuSearchPickerDto> SearchMenus(QueryMenu query)
    {
        var result = Search(query, picker: true);
        var dtoList = _mapper.ToMenuSearchPickersFromBase(result.List)
            .OrderBy(m => m.ParentId).ThenBy(m => m.Number).ToList();

        SetMenuLanguage(dtoList);
        return Paged<MenuSearchPickerDto>.Create(dtoList, result.Total);
    }
    public Result<List<MenuBase>> GetMenus() => Result.Ok(_mapper.ToMenuBases(_menuRepository.GetMenus()));
    public Paged<MenuBaseDto> GetMenus(QueryMenu query)
    {
        return Search(query, picker: false);
    }
    private Paged<MenuBaseDto> Search(QueryMenu query, bool picker)
    {
        var criteria = new MenuSearch(query.DisplayName, picker ? false : query.IsAction,
            picker || query.IsDisplay, query.ParentId, query.PageIndex, Math.Clamp(query.PageSize, 1, 100));
        var page = _menuRepository.Search(criteria);
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
