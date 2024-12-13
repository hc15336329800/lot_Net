using RuoYi.Common.Constants;
using RuoYi.Common.Enums;
using RuoYi.Common.Utils;
using RuoYi.Data.Dtos;
using RuoYi.Framework;
using RuoYi.System.Services;

namespace RuoYi.System.Controllers
{
    /// <summary>
    /// 租户表
    /// </summary>
    [ApiDescriptionSettings("System")]
    [Route("system/tenant")]
    public class SysTenantController : ControllerBase
    {
        private readonly ILogger<SysTenantController> _logger;
        private readonly SysTenantService _sysTenantService;

        public SysTenantController(ILogger<SysTenantController> logger,
            SysTenantService sysTenantService)
        {
            _logger = logger;
            _sysTenantService = sysTenantService;
        }



        // 查询用户组织关系表（sys_user_tenant）根据用户ID
        // 根据系统用户名获取id
        [HttpGet("GetTenantIdsIdByUserName/{userName}")]
        public async Task<AjaxResult> GetTenantIdByUserNameAsync(string userName)
        {

            var tenantId = await  _sysTenantService.GetDeptNamesByUserNameAsync(userName);

            if(tenantId == null || tenantId.Length == 0)
            {
                return AjaxResult.Error("未找到对应的部门信息");
            }

         

            // 返回成功结果
            return AjaxResult.Success(tenantId);
 
        }





        /// <summary>
        /// 查询部门表列表
        /// </summary>
        [HttpGet("list")]
        [AppAuthorize("system:tenant:list")]
        public async Task<AjaxResult> GetSysDeptList([FromQuery] SysTenantDto dto)
        {
            // 查询tid
            long tid = SecurityUtils.GetTenantId();
            dto.TenantId = tid;
            // 动态查询
            var data = await _sysTenantService.GetDtoListAsync(dto);
            return AjaxResult.Success(data);
        }

        /// <summary>
        /// 查询部门表列表  不用
        /// </summary>
        [HttpGet("list/exclude/{deptId}")]
        //[AppAuthorize("system:tenant:list")]
        public async Task<AjaxResult> ExcludeChildList(long? deptId)
        {
            var list = await _sysTenantService.GetDtoListAsync(new SysTenantDto());
            var id = deptId ?? 0;
            var data = list.Where(d => d.Id != id || (!d.Ancestors?.Split(",").Contains(id.ToString()) ?? false)).ToList();
            return AjaxResult.Success(data);
        }

        /// <summary>
        /// 获取 部门表 详细信息
        /// </summary>
        [HttpGet("{deptId}")]
        //[AppAuthorize("system:tenant:query")]
        public async Task<AjaxResult> Get(long deptId)
        {
            await _sysTenantService.CheckDeptDataScopeAsync(deptId);
            var data = await _sysTenantService.GetDtoAsync(deptId);
            return AjaxResult.Success(data);
        }

        /// <summary>
        /// 新增 部门表
        /// </summary>
        [HttpPost("")]
        //[AppAuthorize("system:tenant:add")]
        [TypeFilter(typeof(RuoYi.Framework.DataValidation.DataValidationFilter))]
        [Log(Title = "部门管理", BusinessType = BusinessType.INSERT)]
        public async Task<AjaxResult> Add([FromBody] SysTenantDto tenant)
        {
            // 查询tid
            if(tenant.TenantId <= 0)
            {
                long tid = SecurityUtils.GetTenantId();
                tenant.TenantId = tid;
            }
           

            if (!await _sysTenantService.CheckDeptNameUniqueAsync(tenant))
            {
                return AjaxResult.Error($"新增部门'{tenant.DeptName} '失败，部门名称已存在");
            }
            var data = await _sysTenantService.InsertDeptAsync(tenant);
            return AjaxResult.Success(data);
        }

        /// <summary>
        /// 修改 部门表
        /// </summary>
        [HttpPut("")]
        //[AppAuthorize("system:tenant:edit")]
        [TypeFilter(typeof(RuoYi.Framework.DataValidation.DataValidationFilter))]
        [Log(Title = "部门管理", BusinessType = BusinessType.UPDATE)]
        public async Task<AjaxResult> Edit([FromBody] SysTenantDto tenant)
        {
            long deptId = tenant.Id!.Value;
            await _sysTenantService.CheckDeptDataScopeAsync(deptId);
            if (!await _sysTenantService.CheckDeptNameUniqueAsync(tenant))
            {
                return AjaxResult.Error("修改部门'" + tenant.DeptName + "'失败，部门名称已存在");
            }
            else if (tenant.ParentId.Equals(deptId))
            {
                return AjaxResult.Error("修改部门'" + tenant.DeptName + "'失败，上级部门不能是自己");
            }
            else if (UserConstants.DEPT_DISABLE.Equals(tenant.Status) && await _sysTenantService.CountNormalChildrenDeptByIdAsync(deptId) > 0)
            {
                return AjaxResult.Error("该部门包含未停用的子部门！");
            }
            var data = await _sysTenantService.UpdateDeptAsync(tenant);
            return AjaxResult.Success(data);
        }

        /// <summary>
        /// 删除 部门表
        /// </summary>
        [HttpDelete("{deptId}")]
        //[AppAuthorize("system:tenant:remove")]
        [Log(Title = "部门管理", BusinessType = BusinessType.DELETE)]
        public async Task<AjaxResult> Remove(long deptId)
        {
            if (await _sysTenantService.HasChildByDeptIdAsync(deptId))
            {
                return AjaxResult.Error("存在下级部门,不允许删除");
            }
            if (await _sysTenantService.CheckDeptExistUserAsync(deptId))
            {
                return AjaxResult.Error("部门存在用户,不允许删除");
            }
            await _sysTenantService.CheckDeptDataScopeAsync(deptId);
            var data = await _sysTenantService.DeleteDeptByIdAsync(deptId);
            return AjaxResult.Success(data);
        }
    }
}