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
    }
}
