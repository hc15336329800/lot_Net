using SqlSugar;
using static System.Net.Mime.MediaTypeNames;

namespace RuoYi.System.Repositories;

/// <summary>
///  组织表 Repository
///  author ruoyi
///  date   2023-09-04 17:49:57
/// </summary>
public class SysTenantRepository : BaseRepository<SysTenant,SysTenantDto>
{
    private readonly ISqlSugarClient _sqlSugarClient; // 注入 SqlSugarClient 实例  

    public SysTenantRepository(ISqlSugarClient sqlSugarClient,ISqlSugarRepository<SysTenant> sqlSugarRepository)
    {
        Repo = sqlSugarRepository;
        _sqlSugarClient = sqlSugarClient; //  SqlSugarClient 实例

    }

    public override ISugarQueryable<SysTenant> Queryable(SysTenantDto dto)
    {
        return Repo.AsQueryable()
            .Where((d) => d.DelFlag == DelFlag.No)
            .WhereIF(dto.Id > 0,(d) => d.Id == dto.Id)
            .WhereIF(dto.ParentId > 0,(d) => d.ParentId == dto.ParentId)
            .WhereIF(!string.IsNullOrEmpty(dto.DelFlag),(d) => d.DelFlag == dto.DelFlag)
            .WhereIF(!string.IsNullOrEmpty(dto.DeptName),(d) => d.DeptName!.Contains(dto.DeptName!))
            .WhereIF(!string.IsNullOrEmpty(dto.Status),(d) => d.Status == dto.Status)
        ;
    }

    public override ISugarQueryable<SysTenantDto> DtoQueryable(SysTenantDto dto)
    {
        // 按道理来说是自己查询自己的

        return Repo.AsQueryable()
 
            //新增
            //.WhereIF(dto.TenantId > 0,(d) => d.TenantId == dto.TenantId)
             .Where((d) => d.TenantId == dto.TenantId)


            .Where((d) => d.DelFlag == DelFlag.No)
            .WhereIF(dto.Id > 0,(d) => d.Id == dto.Id)
            .WhereIF(dto.ParentId > 0,(d) => d.ParentId == dto.ParentId)
            .WhereIF(!string.IsNullOrEmpty(dto.DelFlag),(d) => d.DelFlag == dto.DelFlag)
            .WhereIF(!string.IsNullOrEmpty(dto.DeptName),(d) => d.DeptName!.Contains(dto.DeptName!))
            .WhereIF(!string.IsNullOrEmpty(dto.Status),(d) => d.Status == dto.Status)
            .Select((d) => new SysTenantDto
            {
                //Id = d.Id,   //正常返回全部
            },
            true);
    }

    // dtos 关联表数据
    protected override async Task FillRelatedDataAsync(IEnumerable<SysTenantDto> dtos)
    {
        if(dtos.IsEmpty()) return;

        // 关联表处理
        var parentIds = dtos.Where(d => d.ParentId.HasValue).Select(d => d.ParentId!.Value).Distinct().ToList();
        var parentDepts = await this.DtoQueryable(new SysTenantDto { ParentIds = parentIds }).ToListAsync();
        foreach(var dto in dtos)
        {
            dto.ParentName = parentDepts.FirstOrDefault(p => p.Id == dto.ParentId)?.DeptName;
        }
    }

    public async Task<List<SysTenant>> GetDeptListAsync(SysTenantDto dto)
    {
        dto.DelFlag = DelFlag.No;

        return await base.GetListAsync(dto);
    }

    /// <summary>
    /// 根据角色ID查询部门树信息
    /// </summary>
    public async Task<List<long>> GetDeptListByRoleIdAsync(long roleId,bool isDeptCheckStrictly)
    {
        SysTenantDto query = new SysTenantDto { RoleId = roleId,DeptCheckStrictly = isDeptCheckStrictly };

        var list = await base.GetDtoListAsync(query);

        return list.Where(d => d.Id.HasValue).Select(d => d.Id!.Value).Distinct().ToList();
    }

    /// <summary>
    /// 根据ID查询所有子部门（正常状态）
    /// </summary>
    /// <param name="deptId">部门ID</param>
    public async Task<int> CountNormalChildrenDeptByIdAsync(long deptId)
    {
        return await base.CountAsync(d => d.DelFlag == DelFlag.No && d.Status == "0" && SqlFunc.SplitIn(d.Ancestors,deptId.ToString()));
    }

    /// <summary>
    /// 根据ID查询所有子部门  -- 解决字符集不匹配的问题！！！utf8mb4_general_ci  和 utf8mb4_0900_ai_ci
    /// </summary>
    /// <param name="deptId">部门ID</param>
    /// <returns></returns>
    public async Task<List<SysTenant>> GetChildrenDeptByIdAsync(long deptId)
    {

        //var queryable = Repo.AsQueryable()
        //    .Where(d => SqlFunc.SplitIn(d.Ancestors,deptId.ToString()));
        //return await queryable.ToListAsync();

        // 使用 LIKE 查询，包含三种可能性以及精确匹配 ancestors 的逻辑
        string sql = @"
        SELECT * 
        FROM sys_tenant 
        WHERE 
            ancestors LIKE CONCAT('%,', @Id, ',%') OR 
            ancestors LIKE CONCAT(@Id, ',%') OR 
            ancestors LIKE CONCAT('%,', @Id) OR 
            ancestors = @Id";

        // 执行 SQL 查询并返回结果
        var list = await _sqlSugarClient.Ado.SqlQueryAsync<SysTenant>(sql,new { Id = deptId });

        return list;
    }

    /// <summary>
    /// 修改所在部门正常状态
    /// </summary>
    /// <param name="deptIds">部门ID组</param>
    /// <returns></returns>
    public async Task<int> UpdateDeptStatusNormalAsync(IEnumerable<long> deptIds)
    {
        return await base.Updateable()
              .SetColumns(col => col.Status == Status.Enabled)
              .Where(col => deptIds.Contains(col.Id))
              .ExecuteCommandAsync();
    }

    /// <summary>
    /// 是否存在子节点
    /// </summary>
    /// <returns></returns>
    public async Task<bool> HasChildByDeptIdAsync(long parentDeptId)
    {
        var query = new SysTenantDto { DelFlag = DelFlag.No,ParentId = parentDeptId };
        return await base.AnyAsync(query);
    }

    /// <summary>
    /// 删除部门管理信息
    /// </summary>
    public async Task<int> DeleteDeptByIdAsync(long deptId)
    {
        return await base.Updateable()
              .SetColumns(col => col.DelFlag == DelFlag.Yes)
              .Where(col => col.Id == deptId)
              .ExecuteCommandAsync();
    }
}