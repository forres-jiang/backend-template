from pathlib import Path
import re
root=Path('.')
def edit(path, fn):
 p=root/path; p.write_text(fn(p.read_text(encoding='utf-8-sig')),encoding='utf-8')
def usings(s):
 return 'using System.Threading;\nusing System.Threading.Tasks;\n'+s
def declarations(s, methods, interfaces=False):
 # Single-line method signatures in these small use-case/port files.
 pat=r'(?m)^(\s*)((?:public|private)\s+)?(Result(?:<[^\n]+?>)?|List<[^\n]+?>|Paged<[^\n]+?>|MenuState|MenuBaseDto|int|bool) ('+'|'.join(methods)+r')\(([^\n]*)\)'
 def sub(m):
  args=m[5]; args=args+', CancellationToken cancellationToken = default' if args else 'CancellationToken cancellationToken = default'
  return m[1]+(m[2] or '')+('' if interfaces else 'async ')+f'Task<{m[3]}> {m[4]}({args})'
 return re.sub(pat,sub,s)
def calls(s, receivers, methods, token=True):
 pat=r'\b('+ '|'.join(re.escape(x) for x in receivers)+r')\.('+ '|'.join(methods)+r')\('
 matches=list(re.finditer(pat,s))
 for m in reversed(matches):
  start=m.end(); depth=1; i=start; quote=None; escape=False
  while depth:
   c=s[i]
   if quote:
    if escape: escape=False
    elif c=='\\': escape=True
    elif c==quote: quote=None
   elif c in '\"\'': quote=c
   elif c=='(': depth+=1
   elif c==')': depth-=1
   i+=1
  args=s[start:i-1]
  if token: args+=(', ' if args.strip() else '')+'cancellationToken'
  s=s[:m.start()]+'(await '+s[m.start():start]+args+'))'+s[i:]
 return s
mutation=['Add','Update','Remove','Move','SetRoleMenus']
reads=['Get','Search','GetMenus','GetRoleMenuByRoles']
session=['LoadMenus','LoadRoleMenus','Insert','Update','Remove','ApplyRoleChanges']
facade=['Add','Update','Remove','UpdateSort','Get','GetMenus','GetTreeMenus','GetMenuByRoles','GetMenuTreeCheckedByRoles','SearchMenus','RoleMenus','RoleMenuRelation','RoleMenusRelation','RemoveRoleMenu','RoleMenuAction']
edit('My.XXX.Service/Ports/IMenuReadRepository.cs',lambda s: declarations(usings(s),reads,True))
edit('My.XXX.Service/Ports/IMenuTransaction.cs',lambda s: declarations(usings(s).replace('Result Execute(Func<IMenuWriteSession, Result> operation);','Task<Result> Execute(Func<IMenuWriteSession, Task<Result>> operation, CancellationToken cancellationToken = default);'),session,True))
edit('My.XXX.Service/Interfaces/IMenuService.cs',lambda s: declarations(usings(s),facade,True))
edit('My.XXX.Service/Services/MenuMutations.cs',lambda s: calls(calls(declarations(usings(s),mutation).replace('session =>','async session =>'),['session'],session),['transaction'],['Execute']))
edit('My.XXX.Service/Services/MenuCommandService.cs',lambda s: calls(declarations(usings(s),facade),['mutations'],mutation))
edit('My.XXX.Service/Services/RolePermissionService.cs',lambda s: calls(calls(declarations(usings(s),facade+['Change']),['mutations'],mutation),['this'],['Change']))
# Unqualified Change calls.
edit('My.XXX.Service/Services/RolePermissionService.cs',lambda s: re.sub(r'(?<!>)\bChange\(([^;\n]+)\);',lambda m:'(await Change('+m[1]+', cancellationToken));',s))
edit('My.XXX.Service/Services/MenuQueryService.cs',lambda s: calls(declarations(usings(s),facade+['Search']),['_menuRepository'],reads).replace('Search(query, picker: true)','await Search(query, picker: true, cancellationToken)').replace('Search(query, picker: false)','await Search(query, picker: false, cancellationToken)'))
edit('My.XXX.Service/Services/MenuService.cs',lambda s: calls(declarations(usings(s),facade),['commands','queries','roles'],facade))
# Real database adapter: all menu IO is asynchronous, including transaction control.
def repo(s):
 s=declarations(s,reads+session)
 s=s.replace('public Result Execute(Func<IMenuWriteSession, Result> operation)','public async Task<Result> Execute(Func<IMenuWriteSession, Task<Result>> operation, CancellationToken cancellationToken = default)')
 s=s.replace('return AtomicWrite.Execute(db, () =>','return await AtomicWrite.ExecuteAsync(db, async () =>').replace('return operation(session);','return await operation(session);').replace('}, result => result.IsSuccess);','}, result => result.IsSuccess, cancellationToken);')
 s=s.replace('mapper.ToMenuState(db.Menus.FirstOrDefault(m => m.Id == id && !m.IsDeleted))','mapper.ToMenuState(await db.Menus.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, cancellationToken))')
 s=s.replace('mapper.ToMenuStates(query.ToList())','mapper.ToMenuStates(await query.ToListAsync(cancellationToken))').replace('mapper.ToMenuStates(RoleMenus(roleIds).ToList())','mapper.ToMenuStates(await RoleMenus(roleIds).ToListAsync(cancellationToken))')
 s=s.replace('var total = menus.Count();','var total = await menus.CountAsync(cancellationToken);').replace('var list = menus.OrderByDescending','var list = await menus.OrderByDescending').replace('.Skip(query.PageIndex * query.PageSize).Take(query.PageSize).ToList();','.Skip(query.PageIndex * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);')
 s=s.replace('mapper.ToMenuStates(db.Menus.Where(m => !m.IsDeleted).ToList())','mapper.ToMenuStates(await db.Menus.Where(m => !m.IsDeleted).ToListAsync(cancellationToken))')
 s=s.replace('return db.RoleMenu.Where(m => m.RoleId == roleId && !m.IsDeleted).Select(m => m.MenuId).ToList();','return await db.RoleMenu.Where(m => m.RoleId == roleId && !m.IsDeleted).Select(m => m.MenuId).ToListAsync(cancellationToken);')
 s=s.replace('return db.Insert(mapper.ToMenuEntity(menu));','return await db.InsertAsync(mapper.ToMenuEntity(menu), token: cancellationToken);')
 s=s.replace('if (db.GetTable<PermissionRevision>()','if (await db.GetTable<PermissionRevision>()')
 s=s.replace('return db.Menus.Where','return await db.Menus.Where').replace('            db.RoleMenu.Where','            await db.RoleMenu.Where').replace('removals.Count > 0 && db.RoleMenu.Where','removals.Count > 0 && await db.RoleMenu.Where')
 s=s.replace('.Update()', '.UpdateAsync(cancellationToken)')
 s=s.replace('if (db.Insert(new RoleMenu { RoleId = roleId, MenuId = id, CreatedBy = userId, CreatedTime = timestamp }) != 1)','if (await db.InsertAsync(new RoleMenu { RoleId = roleId, MenuId = id, CreatedBy = userId, CreatedTime = timestamp }, token: cancellationToken) != 1)')
 return s
edit('My.XXX.Persistence/Repositories/MenuRepository.cs',repo)
# Active controllers expose the actual envelope and pass request cancellation.
def controller(s):
 s=usings(s)
 pat=r'public (MyResult|MenuBaseDto|List<MenuDto>|bool) (\w+)\(([^\n]*)\)'
 s=re.sub(pat,lambda m:'public async Task<'+({'MenuBaseDto':'MyResult<MenuBaseDto>','List<MenuDto>':'MyResult<List<MenuDto>>','bool':'MyResult<bool>'}.get(m[1],m[1]))+'> '+m[2]+'('+m[3]+')',s)
 s=calls(s,['_menuService'],facade).replace(', cancellationToken)',', HttpContext.RequestAborted)').replace('(cancellationToken)','(HttpContext.RequestAborted)')
 s=s.replace('return (await _menuService.Get(menuId, HttpContext.RequestAborted));','return MyResult<MenuBaseDto>.Success((await _menuService.Get(menuId, HttpContext.RequestAborted)));')
 s=s.replace('return result;','return MyResult<List<MenuDto>>.Success(result);')
 # Preserve legacy boolean data while surfacing command failure in the envelope.
 s=re.sub(r'return (\(await _menuService\.[^\n]+\))\.IsSuccess;',r'return \1.ToBooleanApiResult();',s)
 return s
edit('My.XXX.APIs/Controllers/MenuController.cs',controller)
