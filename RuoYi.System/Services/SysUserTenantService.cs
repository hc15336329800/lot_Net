using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RuoYi.System.Repositories;

namespace RuoYi.System.Services
{
    /// <summary>
    /// 用户和组织关联表Service
    /// </summary>
    public class SysUserTenantService : BaseService<SysUserTenant,SysUserTenantDto>, ITransient
    {
        private readonly ILogger<SysTenantService> _logger;
        private readonly SysUserTenantRepository _sysUserTenantRepository;


        public SysUserTenantService(ILogger<SysTenantService> logger, SysUserTenantRepository sysUserTenantRepository )
        {
            _sysUserTenantRepository = sysUserTenantRepository;
            _logger = logger;
        }

        /// <summary>
        /// 查询组织信息  
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public List<long> GetTenantIdsListByUserId(long userId)
        {
            // 查询符合 UserId 条件的租户信息
            var usertenants = _sysUserTenantRepository
                .Queryable(new SysUserTenantDto { UserId = userId }) // 过滤条件
                .Where(ut => ut.UserId == userId)                    // 增加显式过滤条件
                .ToList();

            // 提取 TId（租户ID），去重并转换为列表
            var tIds = usertenants
                .Select(up => up.TId)
                .Distinct()
                .ToList();

            return tIds; // 返回租户ID列表

  
        }
    }
}
