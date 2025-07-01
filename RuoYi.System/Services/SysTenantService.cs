using RuoYi.Common.Constants;
using RuoYi.Common.Interceptors;
using RuoYi.Common.Utils;
using RuoYi.Data.Enums;
using RuoYi.Data.Models;
using RuoYi.Framework.Exceptions;
using RuoYi.System.Repositories;

namespace RuoYi.System.Services;

/// <summary>
///  组织表 Service
///  author ruoyi
///  date   2023-09-04 17:49:57
/// </summary>
public class SysTenantService : BaseService<SysTenant,SysTenantDto>, ITransient
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
    /// 查询 组织表 详情
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
    /// 根据角色ID查询组织树信息
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <returns>选中组织列表</returns>
    public async Task<List<long>> GetDeptListByRoleIdAsync(long roleId)
    {
        SysRole role = _sysRoleRepository.GetRoleById(roleId);

        return await _sysTenantRepository.GetDeptListByRoleIdAsync(roleId,role.DeptCheckStrictly);
    }

    /// <summary>
    /// 根据ID查询所有子组织（正常状态）数量
    /// </summary>
    /// <param name="deptId">组织ID</param>
    /// <returns>子组织数</returns>
    public async Task<int> CountNormalChildrenDeptByIdAsync(long deptId)
    {
        return await _sysTenantRepository.CountNormalChildrenDeptByIdAsync(deptId);
    }

    #region TreeSelectTenant
    /// <summary>
    /// 查询树结构信息
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
        foreach(SysTenantDto dept in depts)
        {
            // 如果是顶级节点, 遍历该父节点的所有子节点
            if(dept.ParentId.HasValue && !tempList.Contains(dept.ParentId.Value))
            {
                RecursionFn(depts,dept);
                returnList.Add(dept);
            }
        }
        if(returnList.IsEmpty())
        {
            returnList = depts;
        }
        return returnList;
    }

    /// <summary>
    /// 递归列表
    /// </summary>
    private void RecursionFn(List<SysTenantDto> list,SysTenantDto t)
    {
        // 得到子节点列表
        List<SysTenantDto> childList = GetChildList(list,t);
        t.Children = childList;
        foreach(SysTenantDto tChild in childList)
        {
            if(HasChild(list,tChild))
            {
                RecursionFn(list,tChild);
            }
        }
    }

    /// <summary>
    /// 得到子节点列表
    /// </summary>
    private List<SysTenantDto> GetChildList(List<SysTenantDto> list,SysTenantDto t)
    {
        List<SysTenantDto> tList = new List<SysTenantDto>();
        foreach(SysTenantDto n in list)
        {
            if(n.ParentId > 0 && n.ParentId == t.Id)
            {
                tList.Add(n);
            }
        }
        return tList;
    }

    /// <summary>
    /// 是否存在子节点
    /// </summary>
    /// <param name="deptId">组织ID</param>
    /// <returns></returns>
    public async Task<bool> HasChildByDeptIdAsync(long deptId)
    {
        return await _sysTenantRepository.HasChildByDeptIdAsync(deptId);
    }

    /// <summary>
    /// 查询组织是否存在用户
    /// </summary>
    /// <param name="deptId">组织ID</param>
    /// <returns></returns>
    public async Task<bool> CheckDeptExistUserAsync(long deptId)
    {
        return await _sysUserRepository.CheckDeptExistUserAsync(deptId);
    }

    private bool HasChild(List<SysTenantDto> list,SysTenantDto t)
    {
        return GetChildList(list,t).Count > 0;
    }

    #endregion

    /// <summary>
    /// 校验组织名称是否唯一
    /// </summary>
    public async Task<bool> CheckDeptNameUniqueAsync(SysTenantDto dept)
    {
        SysTenant info = await _sysTenantRepository.GetFirstAsync(new SysTenantDto { DeptName = dept.DeptName,ParentId = dept.ParentId });
        if(info != null && info.Id != dept.Id)
        {
            return UserConstants.NOT_UNIQUE;
        }
        return UserConstants.UNIQUE;
    }

    /// <summary>
    /// 校验组织是否有数据权限
    /// </summary>
    /// <param name="deptId">组织id</param>
    public async Task CheckDeptDataScopeAsync(long deptId)
    {
        if(!SecurityUtils.IsAdmin())
        {
            SysTenantDto dto = new SysTenantDto { Id = deptId };
            List<SysTenant> depts = await _sysTenantRepository.GetDeptListAsync(dto);
            if(depts.IsEmpty())
            {
                throw new ServiceException("没有权限访问组织数据！");
            }
        }
    }

    /// <summary>
    /// 新增保存组织信息
    /// </summary>
    public async Task<bool> InsertDeptAsync(SysTenantDto dept)
    {
        SysTenant info = await _sysTenantRepository.FirstOrDefaultAsync(d => d.Id == dept.ParentId); // 父节点
        // 如果父节点不为正常状态,则不允许新增子节点
        if(!UserConstants.DEPT_NORMAL.Equals(info.Status))
        {
            throw new ServiceException("组织停用，不允许新增");
        }
        dept.Ancestors = info.Ancestors + "," + dept.ParentId;
        dept.DelFlag = DelFlag.No;
        return await _sysTenantRepository.InsertAsync(dept);
    }

    /// <summary>
    /// 修改保存组织信息
    /// </summary>
    public async Task<int> UpdateDeptAsync(SysTenantDto dept)
    {
        SysTenant newParentDept = await this.GetAsync(dept.ParentId.Value);
        SysTenant oldDept = await this.GetAsync(dept.Id.Value);
        if(newParentDept != null && oldDept != null)
        {
            string newAncestors = newParentDept.Ancestors + "," + newParentDept.Id;
            string oldAncestors = oldDept.Ancestors!;
            dept.Ancestors = newAncestors;
            await UpdateDeptChildrenAsync(dept.Id.Value,newAncestors,oldAncestors);
        }
        int result = await _sysTenantRepository.UpdateAsync(dept,true);
        if(UserConstants.DEPT_NORMAL.Equals(dept.Status) && StringUtils.IsNotEmpty(dept.Ancestors)
                && !StringUtils.Equals("0",dept.Ancestors))
        {
            // 如果该组织是启用状态，则启用该组织的所有上级组织
            await UpdateParentDeptStatusNormalAsync(dept);
        }
        return result;
    }

    /// <summary>
    /// 修改子元素关系
    /// </summary>
    /// <param name="deptId">被修改的组织ID</param>
    /// <param name="newAncestors">新的父ID集合</param>
    /// <param name="oldAncestors">旧的父ID集合</param>
    public async Task UpdateDeptChildrenAsync(long deptId,string newAncestors,string oldAncestors)
    {
        List<SysTenant> children = await _sysTenantRepository.GetChildrenDeptByIdAsync(deptId);
        foreach(SysTenant child in children)
        {
            child.Ancestors = child.Ancestors!.ReplaceFirst(oldAncestors,newAncestors);
        }
        if(children.Count > 0)
        {
            await _sysTenantRepository.UpdateAsync(children);
        }
    }

    /// <summary>
    /// 修改该组织的父级组织状态
    /// </summary>
    /// <param name="dept">当前组织</param>
    private async Task UpdateParentDeptStatusNormalAsync(SysTenantDto dept)
    {
        string ancestors = dept.Ancestors!;
        long[] deptIds = ConvertUtils.ToLongArray(ancestors);
        await _sysTenantRepository.UpdateDeptStatusNormalAsync(deptIds);
    }

    /// <summary>
    /// 删除组织管理信息
    /// </summary>
    public async Task<int> DeleteDeptByIdAsync(long deptId)
    {
        return await _sysTenantRepository.DeleteDeptByIdAsync(deptId);
    }





    /// <summary>
    /// 根据系统用户名获取当前用户的组织集合    
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<object[]> GetDeptNamesByUserNameAsync(string userName)
    {
        // [
        //    { "label": "深圳代理", "value": 1 },
        //    { "label": "长沙代理", "value": 2 }
        // ]

        // 定义 SQL 查询
        string sql = @"
        SELECT t.dept_name  , t.id
        FROM sys_user u
        JOIN sys_user_tenant ut ON u.user_id = ut.user_id
        JOIN sys_tenant t ON ut.t_id = t.id
        WHERE u.status = '0'  
          AND u.del_flag = '0' 
          AND u.user_name = @UserName
    ";

 
        try
        {
            // 执行 SQL 查询并获取结果
            var tenantData = await _sysTenantRepository.SqlQueryable(sql,
                new List<SugarParameter> { new SugarParameter("@UserName",userName) }
            )
            .Select(t => new { t.Id,t.DeptName }) // 显式指定选择字段
            .ToListAsync(); // 获取结果列表

             

            // 构造返回格式，label = dept_name，value = id
            return tenantData.Select(item => new
            {
                label = item.DeptName, // 组织名称
                value = item.Id        // 组织 ID
            }).ToArray();
        }

        catch(Exception ex)
        {
            throw new Exception("获取组织信息失败",ex); // 抛出更具体的异常
        }
    }



    /// <summary>
    /// 根据系统用户名获取当前用户的组织集合   升级
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<object[]> GetDeptNamesByUserNameAsync(string userName,string userType)
    {
        // [
        //    { "label": "深圳代理", "value": 1 },
        //    { "label": "长沙代理", "value": 2 }
        // ]

        // 定义 SQL 查询
        string sql = @"
        SELECT t.dept_name  , t.id
        FROM sys_user u
        JOIN sys_user_tenant ut ON u.user_id = ut.user_id
        JOIN sys_tenant t ON ut.t_id = t.id
        WHERE u.status = '0'  
          AND u.del_flag = '0' 
          AND u.user_name = @UserName
    ";

        if(userType == "SUPER_ADMIN") //超级管理员1  ,这个不走这里
        {

        }
        else if(userType == "GROUP_ADMIN") //集团管理员2   已验证
        {
            //这个是查询上一级 ，备用
            //sql = @"
            //    SELECT 
            //    pt.dept_name AS dept_name,
            //    pt.id        AS id
            //    FROM sys_user u
            //    JOIN sys_user_tenant ut ON u.user_id = ut.user_id
            //    JOIN sys_tenant t ON ut.t_id = t.id
            //    LEFT JOIN sys_tenant pt ON t.parent_id = pt.id
            //    WHERE u.status = '0'
            //      AND u.del_flag = '0'
            //      AND u.user_name = @UserName  ";

             sql = @"
                           SELECT 
                t.dept_name AS dept_name,
                t.id AS id
            FROM sys_user u
            JOIN sys_user_tenant ut ON u.user_id = ut.user_id
            JOIN sys_tenant t ON ut.t_id = t.id
            WHERE u.status = '0'
              AND u.del_flag = '0'
              AND u.user_name = @UserName  ";

        }
        else if(userType == "COMPANY_ADMIN") //公司管理员3
        {
            sql = @"
                           SELECT 
                t.dept_name AS dept_name,
                t.id AS id
            FROM sys_user u
            JOIN sys_user_tenant ut ON u.user_id = ut.user_id
            JOIN sys_tenant t ON ut.t_id = t.id
            WHERE u.status = '0'
              AND u.del_flag = '0'
              AND u.user_name = @UserName  ";

        }
        else // 普通用户4
        {
        }

        try
        {
            // 执行 SQL 查询并获取结果
            var tenantData = await _sysTenantRepository.SqlQueryable(sql,
                new List<SugarParameter> { new SugarParameter("@UserName",userName) }
            )
            .Select(t => new { t.Id,t.DeptName }) // 显式指定选择字段
            .ToListAsync(); // 获取结果列表



            // 构造返回格式，label = dept_name，value = id
            return tenantData.Select(item => new
            {
                label = item.DeptName, // 组织名称
                value = item.Id        // 组织 ID
            }).ToArray();
        }

        catch(Exception ex)
        {
            throw new Exception("获取组织信息失败",ex); // 抛出更具体的异常
        }
    }








}