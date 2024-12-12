using RuoYi.Common.Constants;
using RuoYi.Common.Interceptors;
using RuoYi.Common.Utils;
using RuoYi.Data.Models;
using RuoYi.Framework.Exceptions;
using RuoYi.System.Repositories;

namespace RuoYi.System.Services;

/// <summary>
///  租户表 Service
///  author ruoyi
///  date   2023-09-04 17:49:57
/// </summary>
public class SysTenantService : BaseService<SysTenant, SysTenantDto>, ITransient
{
    private readonly ILogger<SysTenantService> _logger;
    private readonly SysTenantRepository _sysTenantRepository;
    private readonly SysUserRepository _sysUserRepository;
    private readonly SysRoleRepository _sysRoleRepository;

    public SysTenantService(ILogger<SysTenantService> logger,
        SysTenantRepository sysTenantRepository,
        SysUserRepository sysUserRepository,
        SysRoleRepository sysRoleRepository)
    {
        BaseRepo = sysTenantRepository;

        _logger = logger;
        _sysTenantRepository = sysTenantRepository;
        _sysUserRepository = sysUserRepository;
        _sysRoleRepository = sysRoleRepository;
    }

    /// <summary>
    /// 查询 部门表 详情
    /// </summary>
    public async Task<SysTenant> GetAsync(long id)
    {
        return await base.FirstOrDefaultAsync(e => e.Id == id);
    }
    public async Task<SysTenantDto> GetDtoAsync(long id)
    {
        var entity = await GetAsync(id);
        return entity.Adapt<SysTenantDto>();
    }

    //[DataScope(DeptAlias = "d")]
    public override async Task<List<SysTenantDto>> GetDtoListAsync(SysTenantDto dto)
    {
        return await _sysTenantRepository.GetDtoListAsync(dto);
    }

    /// <summary>
    /// 根据角色ID查询部门树信息
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns>选中部门列表</returns>
    public async Task<List<long>> GetDeptListByRoleIdAsync(long roleId)
    {
        SysRole role = _sysRoleRepository.GetRoleById(roleId);

        return await _sysTenantRepository.GetDeptListByRoleIdAsync(roleId, role.DeptCheckStrictly);
    }

    /// <summary>
    /// 根据ID查询所有子部门（正常状态）数量
    /// </summary>
    /// <param name="deptId">部门ID</param>
    /// <returns>子部门数</returns>
    public async Task<int> CountNormalChildrenDeptByIdAsync(long deptId)
    {
        return await _sysTenantRepository.CountNormalChildrenDeptByIdAsync(deptId);
    }

    #region TreeSelectTenant
    /// <summary>
    /// 查询部门树结构信息
    /// </summary>
    public async Task<List<TreeSelectTenant>> GetDeptTreeListAsync(SysTenantDto dto)
    {
        List<SysTenantDto> depts = await this.GetDtoListAsync(dto);
        return BuildDeptTreeSelect(depts);
    }

    /// <summary>
    /// 构建前端所需要下拉树结构
    /// </summary>
    private List<TreeSelectTenant> BuildDeptTreeSelect(List<SysTenantDto> depts)
    {
        List<SysTenantDto> deptTrees = BuildDeptTree(depts);
        return deptTrees.Select(dept => new TreeSelectTenant(dept)).ToList();
    }

    /// <summary>
    /// 构建前端所需要树结构
    /// </summary>
    private List<SysTenantDto> BuildDeptTree(List<SysTenantDto> depts)
    {
        List<SysTenantDto> returnList = new List<SysTenantDto>();
        List<long> tempList = depts.Where(d => d.Id.HasValue).Select(d => d.Id!.Value).ToList();
        foreach (SysTenantDto dept in depts)
        {
            // 如果是顶级节点, 遍历该父节点的所有子节点
            if (dept.ParentId.HasValue && !tempList.Contains(dept.ParentId.Value))
            {
                RecursionFn(depts, dept);
                returnList.Add(dept);
            }
        }
        if (returnList.IsEmpty())
        {
            returnList = depts;
        }
        return returnList;
    }

    /// <summary>
    /// 递归列表
    /// </summary>
    private void RecursionFn(List<SysTenantDto> list, SysTenantDto t)
    {
        // 得到子节点列表
        List<SysTenantDto> childList = GetChildList(list, t);
        t.Children = childList;
        foreach (SysTenantDto tChild in childList)
        {
            if (HasChild(list, tChild))
            {
                RecursionFn(list, tChild);
            }
        }
    }

    /// <summary>
    /// 得到子节点列表
    /// </summary>
    private List<SysTenantDto> GetChildList(List<SysTenantDto> list, SysTenantDto t)
    {
        List<SysTenantDto> tList = new List<SysTenantDto>();
        foreach (SysTenantDto n in list)
        {
            if (n.ParentId > 0 && n.ParentId == t.Id)
            {
                tList.Add(n);
            }
        }
        return tList;
    }

    /// <summary>
    /// 是否存在子节点
    /// </summary>
    /// <param name="deptId">部门ID</param>
    /// <returns></returns>
    public async Task<bool> HasChildByDeptIdAsync(long deptId)
    {
        return await _sysTenantRepository.HasChildByDeptIdAsync(deptId);
    }

    /// <summary>
    /// 查询部门是否存在用户
    /// </summary>
    /// <param name="deptId">部门ID</param>
    /// <returns></returns>
    public async Task<bool> CheckDeptExistUserAsync(long deptId)
    {
        return await _sysUserRepository.CheckDeptExistUserAsync(deptId);
    }

    private bool HasChild(List<SysTenantDto> list, SysTenantDto t)
    {
        return GetChildList(list, t).Count > 0;
    }

    #endregion

    /// <summary>
    /// 校验部门名称是否唯一
    /// </summary>
    public async Task<bool> CheckDeptNameUniqueAsync(SysTenantDto dept)
    {
        SysTenant info = await _sysTenantRepository.GetFirstAsync(new SysTenantDto { DeptName = dept.DeptName, ParentId = dept.ParentId });
        if (info != null && info.Id != dept.Id)
        {
            return UserConstants.NOT_UNIQUE;
        }
        return UserConstants.UNIQUE;
    }

    /// <summary>
    /// 校验部门是否有数据权限
    /// </summary>
    /// <param name="deptId">部门id</param>
    public async Task CheckDeptDataScopeAsync(long deptId)
    {
        if (!SecurityUtils.IsAdmin())
        {
            SysTenantDto dto = new SysTenantDto { Id = deptId };
            List<SysTenant> depts = await _sysTenantRepository.GetDeptListAsync(dto);
            if (depts.IsEmpty())
            {
                throw new ServiceException("没有权限访问部门数据！");
            }
        }
    }

    /// <summary>
    /// 新增保存部门信息
    /// </summary>
    public async Task<bool> InsertDeptAsync(SysTenantDto dept)
    {
        SysTenant info = await _sysTenantRepository.FirstOrDefaultAsync(d => d.Id == dept.ParentId); // 父节点
        // 如果父节点不为正常状态,则不允许新增子节点
        if (!UserConstants.DEPT_NORMAL.Equals(info.Status))
        {
            throw new ServiceException("部门停用，不允许新增");
        }
        dept.Ancestors = info.Ancestors + "," + dept.ParentId;
        dept.DelFlag = DelFlag.No;
        return await _sysTenantRepository.InsertAsync(dept);
    }

    /// <summary>
    /// 修改保存部门信息
    /// </summary>
    public async Task<int> UpdateDeptAsync(SysTenantDto dept)
    {
        SysTenant newParentDept = await this.GetAsync(dept.ParentId.Value);
        SysTenant oldDept = await this.GetAsync(dept.Id.Value);
        if (newParentDept != null && oldDept != null)
        {
            string newAncestors = newParentDept.Ancestors + "," + newParentDept.Id;
            string oldAncestors = oldDept.Ancestors!;
            dept.Ancestors = newAncestors;
            await UpdateDeptChildrenAsync(dept.Id.Value, newAncestors, oldAncestors);
        }
        int result = await _sysTenantRepository.UpdateAsync(dept, true);
        if (UserConstants.DEPT_NORMAL.Equals(dept.Status) && StringUtils.IsNotEmpty(dept.Ancestors)
                && !StringUtils.Equals("0", dept.Ancestors))
        {
            // 如果该部门是启用状态，则启用该部门的所有上级部门
            await UpdateParentDeptStatusNormalAsync(dept);
        }
        return result;
    }

    /// <summary>
    /// 修改子元素关系
    /// </summary>
    /// <param name="deptId">被修改的部门ID</param>
    /// <param name="newAncestors">新的父ID集合</param>
    /// <param name="oldAncestors">旧的父ID集合</param>
    public async Task UpdateDeptChildrenAsync(long deptId, string newAncestors, string oldAncestors)
    {
        List<SysTenant> children = await _sysTenantRepository.GetChildrenDeptByIdAsync(deptId);
        foreach (SysTenant child in children)
        {
            child.Ancestors = child.Ancestors!.ReplaceFirst(oldAncestors, newAncestors);
        }
        if (children.Count > 0)
        {
            await _sysTenantRepository.UpdateAsync(children);
        }
    }

    /// <summary>
    /// 修改该部门的父级部门状态
    /// </summary>
    /// <param name="dept">当前部门</param>
    private async Task UpdateParentDeptStatusNormalAsync(SysTenantDto dept)
    {
        string ancestors = dept.Ancestors!;
        long[] deptIds = ConvertUtils.ToLongArray(ancestors);
        await _sysTenantRepository.UpdateDeptStatusNormalAsync(deptIds);
    }

    /// <summary>
    /// 删除部门管理信息
    /// </summary>
    public async Task<int> DeleteDeptByIdAsync(long deptId)
    {
        return await _sysTenantRepository.DeleteDeptByIdAsync(deptId);
    }
}