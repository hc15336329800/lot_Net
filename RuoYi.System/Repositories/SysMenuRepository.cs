using RuoYi.System.Services;

namespace RuoYi.System.Repositories;

/// <summary>
///  菜单权限表 Repository
///  author ruoyi
///  date   2023-08-21 14:40:23
/// </summary>
public class SysMenuRepository : BaseRepository<SysMenu, SysMenuDto>
{
    public SysMenuRepository(ISqlSugarRepository<SysMenu> sqlSugarRepository)
    {
        Repo = sqlSugarRepository;
    }

    public override ISugarQueryable<SysMenu> Queryable(SysMenuDto dto)
    {


        var queryable = Repo.AsQueryable()

            .WhereIF(dto.Type > 0,m => m.Type == dto.Type) //判断它是否大于 0
                        //.WhereIF(!string.IsNullOrEmpty(dto.IsAdmin),(m) => m.IsAdmin == dto.IsAdmin)  //是否是管理员 

            .WhereIF(!string.IsNullOrEmpty(dto.Status), (m) => m.Status == dto.Status)
            .WhereIF(!string.IsNullOrEmpty(dto.MenuName), (m) => m.MenuName!.Contains(dto.MenuName!))
            .WhereIF(!string.IsNullOrEmpty(dto.Visible), (m) => m.Visible == dto.Visible)
            .WhereIF(dto.MenuTypes.Count > 0, (m) => dto.MenuTypes.Contains(m.MenuType!))
            .WhereIF(dto.MenuTypes.Count > 0, (m) => dto.MenuTypes.Contains(m.MenuType!))
            .OrderBy(m => new { m.ParentId, m.OrderNum })
        ;

        //  打印sql
        var sqlInfo = queryable.ToSql();
        Console.WriteLine($"SQL语句: {sqlInfo.Key}");

        return queryable;
    }

    public override ISugarQueryable<SysMenuDto> DtoQueryable(SysMenuDto dto)
    {
        return Repo.AsQueryable()
            .LeftJoin<SysRoleMenu>((m, rm) => m.MenuId == rm.MenuId)
            .LeftJoin<SysUserRole>((m, rm, ur) => rm.RoleId == ur.RoleId)
            .LeftJoin<SysRole>((m, rm, ur, r) => rm.RoleId == r.RoleId)
             
            .WhereIF(!string.IsNullOrEmpty(dto.Status), (m, rm, ur, r) => m.Status == dto.Status)
            .WhereIF(!string.IsNullOrEmpty(dto.MenuName), (m, rm, ur, r) => m.MenuName!.Contains(dto.MenuName!))
            .WhereIF(!string.IsNullOrEmpty(dto.Visible), (m, rm, ur, r) => m.Visible == dto.Visible)
            .WhereIF(dto.UserId > 0, (m, rm, ur, r) => ur.UserId == dto.UserId)
            .WhereIF(dto.RoleId > 0, (m, rm, ur, r) => rm.RoleId == dto.RoleId)
            .WhereIF(!string.IsNullOrEmpty(dto.RoleStatus), (m, rm, ur, r) => r.Status == dto.RoleStatus)
            .WhereIF(dto.MenuTypes.Count > 0, (m) => dto.MenuTypes.Contains(m.MenuType!))
            .OrderBy(m => new { m.ParentId, m.OrderNum })
            .Select((m, rm, ur, r) => new SysMenuDto
            {
                CreateBy = m.CreateBy,
                CreateTime = m.CreateTime,
                UpdateBy = m.UpdateBy,
                UpdateTime = m.UpdateTime,
                Type = m.Type, //后加
                MenuId = m.MenuId,
                ParentId = m.ParentId,
                MenuName = m.MenuName,
                Path = m.Path,
                Component = m.Component,
                Query = m.Query,
                Visible = m.Visible,
                Status = m.Status,
                Perms = m.Perms,
                IsFrame = m.IsFrame,
                IsCache = m.IsCache,
                MenuType = m.MenuType,
                Icon = m.Icon,
                OrderNum = m.OrderNum,
            }).Distinct();
    }

    // 获取用户菜单
    public async Task<List<SysMenu>> SelectMenuListAsync(SysMenuDto dto)
    {
        var entities = await this.GetListAsync(dto);
        return entities;
    }

    public async Task<List<SysMenuDto>> SelectMenuDtoListByUserIdAsync(SysMenuDto dto)
    {
        var dtos = await this.GetDtoListAsync(dto);
        return dtos;
    }


    // 获取用户菜单
    public async Task<List<SysMenu>> SelectMenuListByUserIdAsync(SysMenuDto dto)
    {
        var dtos = await this.GetListAsync(dto);
        return dtos.Adapt<List<SysMenu>>();
    }

    /// <summary>
    /// 根据用户ID查询菜单
    /// </summary>
    public List<string> SelectMenuPermsByUserId(long userId)
    {
        var query = new SysMenuDto { Status = Status.Enabled, UserId = userId };

        var dtos = this.GetDtoList(query);

        return dtos.Where(d => !string.IsNullOrEmpty(d.Perms)).Select(d => d.Perms).ToList()!;
    }

    /// <summary>
    /// 根据角色ID查询权限
    /// </summary>
    public List<string> SelectMenuPermsByRoleId(long roleId)
    {
        var query = new SysMenuDto { Status = Status.Enabled, RoleId = roleId };

        var dtos = this.GetDtoList(query);

        return dtos.Where(d => !string.IsNullOrEmpty(d.Perms)).Select(d => d.Perms).ToList()!;
    }

    /// <summary>
    /// 查询菜单  管理
    /// </summary>
    public List<SysMenu> SelectMenuTreeAll()
    {
        SysMenuDto dto = new SysMenuDto { Status = Status.Enabled, MenuTypes = new List<string> { "M", "C" } };
        return this.GetList(dto);
    }



    /// <summary>
    /// 改造：根据菜单 type 获取菜单树
    /// </summary>
    /// <param name="menuType">菜单类型（数据库中的 type 字段值）</param>
    /// <returns>该类型对应的菜单列表（树状结构）</returns>
    /// <summary>
    public List<SysMenu> SelectMenuListByType(int menuType)
    {
        return Repo.AsQueryable()
            .Where(m => m.Status == Status.Enabled && m.Type == menuType)
            .OrderBy(m => new { m.ParentId,m.OrderNum })
            .ToList();
    }





    /// <summary>
    /// 根据用户ID查询菜单
    /// </summary>
    public List<SysMenu> SelectMenuTreeByUserId(long userId)
    {
        SysMenuDto dto = new SysMenuDto { UserId = userId, Status = Status.Enabled, RoleStatus = Status.Enabled, MenuTypes = new List<string> { "M", "C" } };

        var dtos = this.GetDtoList(dto);

        return dtos.Adapt<List<SysMenu>>();
    }

    public List<long> SelectMenuListByRoleId(long roleId, bool isMenuCheckStrictly)
    {
        var queryable = Repo.AsQueryable()
            .LeftJoin<SysRoleMenu>((m, rm) => m.MenuId == rm.MenuId)
            .Where((m, rm) => rm.RoleId == roleId)
            .WhereIF(isMenuCheckStrictly, (m, rm) => SqlFunc.Subqueryable<SysMenu>()
                .InnerJoin<SysRoleMenu>((sm, srm) => sm.MenuId == srm.MenuId && srm.RoleId == roleId)
                .Where(sm => sm.ParentId == m.MenuId).NotAny())
            .OrderBy(m => new { m.ParentId, m.OrderNum });

        return queryable.Select(m => m.MenuId).ToList();
    }


    public SysMenu SelectMenuById(long menuId)
    {
        return this.FirstOrDefault(m => m.MenuId == menuId);
    }

    public bool HasChildByMenuId(long parentId)
    {
        return this.Count(m => m.ParentId == parentId) > 0;
    }

    public SysMenu CheckMenuNameUnique(string menuName, long parentMenuId)
    {
        return this.FirstOrDefault(m => m.MenuName == menuName && m.ParentId == parentMenuId);
    }
}