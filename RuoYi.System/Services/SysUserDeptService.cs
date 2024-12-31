using RuoYi.Common.Interceptors;
using RuoYi.Data.Models;
using RuoYi.System.Repositories;
using RuoYi.System.Slave.Repositories;
using SqlSugar;

namespace RuoYi.System.Services;

/// <summary>
///  用户和部门关联表 Service
///  author ruoyi.net
///  date   2023-08-23 09:43:52
/// </summary>
public class SysUserDeptService : BaseService<SysUserDept,SysUserDeptDto>, ITransient
{
    private readonly ILogger<SysUserDeptService> _logger;
    private readonly SysUserDeptRepository _sysUserDeptRepository;

    public SysUserDeptService(ILogger<SysUserDeptService> logger,
        SysUserDeptRepository sysUserDeptRepository)
    {
        _logger = logger;
        _sysUserDeptRepository = sysUserDeptRepository;
        BaseRepo = sysUserDeptRepository;
    }


    /// <summary>
    ///  列表
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    //[DataAdmin]
    public virtual async Task<List<SysUserDeptDto>> GetListLineAsync(SysUserDeptDto dto)
    {
        return await base.GetDtoListAsync(dto);
    }


    /// <summary>
    /// 查询 用户和部门关联表 详情
    /// </summary>
    public async Task<SysUserDeptDto> GetAsync(long? id)
    {
        var entity = await base.FirstOrDefaultAsync(e => e.UserId == id);
        var dto = entity.Adapt<SysUserDeptDto>();
        // TODO 填充关联表数据
        return dto;
    }



    /// <summary>
    /// 删除用户部门中间表
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public int DeleteUserDeptByUserId(long userId)
    {
        return _sysUserDeptRepository.Delete(up => up.UserId == userId);
    }

    /// <summary>
    /// 批量删除用户部门中间表  未测试
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public int DeleteUserDeptByUserId(List<long> userIds)
    {
        return _sysUserDeptRepository.Delete(r => userIds.Contains(r.UserId));
    }


    /// <summary>
    /// 修改：查询部门信息  
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public List<long> GetDeptIdsListByUserId(long userId)
    {
        // 通过userId去用户岗位中间表(SysUserPos)拿到岗位PostId字段(SysPost)数据
        var users = _sysUserDeptRepository.Queryable(new SysUserDeptDto { UserId = userId }).ToList();
        var deptIds = users.Select(up => up.DeptId).Distinct().ToList();

        return deptIds;

      }
 
    /// <summary>
    /// 在中间表查询出来部门子集
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public List<long> GetDeptChildIdByUserId(long userId)
    {
         var usertenants = _sysUserDeptRepository
            .Queryable(new SysUserDeptDto { UserId = userId }) // 过滤条件
            .Where(ut => ut.UserId == userId)                    // 增加显式过滤条件
            .ToList();

        // 提取  ），去重并转换为列表
        var tIds = usertenants
            .Select(up => up.DeptId)
            .Distinct()
            .ToList();

        return tIds; //  


    }
}