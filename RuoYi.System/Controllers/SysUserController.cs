using RuoYi.Common.Data;
using RuoYi.Common.Enums;
using RuoYi.Common.Utils;
using RuoYi.Data.Dtos;
using RuoYi.Data.Entities;
using RuoYi.Data.Enums;
using RuoYi.Data.Models;
using RuoYi.Framework;
using RuoYi.System.Services;
using RuoYi.System.Utils;

namespace RuoYi.System.Controllers
{
    /// <summary>
    /// 用户信息表
    /// </summary>
    [ApiDescriptionSettings("System")]
    [Route("system/user")]
    public class SysUserController : ControllerBase
    {
        private readonly ILogger<SysUserController> _logger;
        private readonly SysUserService _sysUserService;
        private readonly SysRoleService _sysRoleService;
        private readonly SysPostService _sysPostService;
        private readonly SysDeptService _sysDeptService;
        private readonly SysTenantService _sysTenantService;
        private readonly SysUserTenantService _sysUserTenantService;
        private readonly SysUserDeptService _sysUserDeptService;



        public SysUserController(ILogger<SysUserController> logger,SysTenantService sysTenantService,SysUserDeptService sysUserDeptService,
            SysUserService sysUserService,SysRoleService sysRoleService,SysPostService sysPostService,SysDeptService sysDeptService,SysUserTenantService sysUserTenantService)
        {
            _logger = logger;
            _sysUserService = sysUserService;
            _sysRoleService = sysRoleService;
            _sysPostService = sysPostService;
            _sysDeptService = sysDeptService;
            _sysTenantService = sysTenantService;
            _sysUserTenantService = sysUserTenantService;
            _sysUserDeptService = sysUserDeptService;


        }

        /// <summary>
        /// 查询用户信息表列表 -- token中的tid
        /// </summary>
        [HttpGet("list")]
        [AppAuthorize("system:user:list")]
        public async Task<SqlSugarPagedList<SysUser>> GetUserList([FromQuery] SysUserDto dto)
        {
            long tid = SecurityUtils.GetTenantId();// tid
            dto.TenantId = tid;
            return await _sysUserService.GetPagedUserListAsync(dto);
        }




        //    当前的需求是根据用户类型对组织树进行筛选并返回：

        //超级管理员（SUPER_ADMIN）：返回所有层级的组织树。
        //集团管理员（GROUP_ADMIN）：只返回两层组织节点（集团 + 下属公司）。
        //公司管理员（COMPANY_ADMIN）：只返回公司层级节点。
        //普通用户（GROUP_USER 和 COMPANY_USER）：返回空组织树。

        /// <summary>
        /// 获取 用户信息表 详细信息
        /// </summary>
        [HttpGet("")] //新增用户前
        [HttpGet("{userId}")] // 修改查询指定用户
        [AppAuthorize("system:user:query")]
        public async Task<AjaxResult> GetInfo(long userId)
        {
            string userType = SecurityUtils.GetUserType(); // 如：SUPER_ADMIN、GROUP_ADMIN、COMPANY_ADMIN、GROUP_USER、COMPANY_USER
            AjaxResult ajax = AjaxResult.Success();

            if(userType == "SUPER_ADMIN") //超级管理员1
            {
                await _sysUserService.CheckUserDataScope(userId);
                var roles = await _sysRoleService.GetRoleListAsync(new SysRoleDto()); //角色列表
                var posts = await _sysPostService.GetListAsync(new SysPostDto()); // 岗位列表
 
                //todo: tid应该是前端传递过来的id？？
                long tid = SecurityUtils.GetTenantId();
                var tenantdto = new SysTenantDto();
                tenantdto.TenantId = tid;
                var tenanttree = await _sysTenantService.GetDeptTreeListAsync(tenantdto);
                tenanttree = FilterTenantTreeByUserType(SecurityUtils.GetTenantId(),tenanttree,userType);  // 根据用户类型筛选组织树


                // 获取部门下拉框
                List<ElSelect> elSelect = TenantUtils.GetElSelectByTenant(userType); // 获取下拉框结构（用户类型）

                ajax.Add("roles",roles);  // 角色下拉框
                ajax.Add("posts",posts);  // 岗位下拉框
                ajax.Add("tenantIds",tenanttree);  // 组织树结构
                ajax.Add("userTypes",elSelect);   // 用户下拉框

                // 用户信息 by  id
                if(userId > 0)
                {
                    // form表单中的实际数值
                    var user = await _sysUserService.GetDtoAsync(userId);
                    List<long> ls = _sysUserTenantService.GetTenantIdsListByUserId(userId);// 用户组织子集
                    List<long> DeptChildId = _sysUserDeptService.GetDeptChildIdByUserId(userId); ; // 用户部门子集
                    user.TenantIds = ls;// 组织组
                    user.DeptIds = DeptChildId;// 部门组
                    user.PostIds = _sysPostService.GetPostIdsListByUserId(userId); //用户所属岗位数组
                    user.RoleIds = user.Roles.Select(x => x.RoleId).ToList();//用户所属角色数组

                    ajax.Add(AjaxResult.DATA_TAG,user);

                }
                return ajax;

            }
            else if(userType == "GROUP_ADMIN") //集团管理员2   已验证
            {

                //todo: tid应该是前端传递过来的id？？
                long tid =  SecurityUtils.GetTenantId();
                var tenantdto = new SysTenantDto();
                tenantdto.TenantId = tid;
                var tenanttree = await _sysTenantService.GetDeptTreeListAsync(tenantdto); //所有tid=0？ 的菜单集合
                //tid = 101; //测试
                tenanttree = FilterTenantTreeByUserType(tid,tenanttree,userType);  // 根据用户类型筛选组织树
  
                ajax.Add("tenantIds",tenanttree);  // 组织树结构
 
                // 用户信息 by  id
                if(userId > 0)
                {
                    // form表单中的实际数值
                    var user = await _sysUserService.GetDtoAsync(userId);
                    List<long> ls = _sysUserTenantService.GetTenantIdsListByUserId(userId);// 用户组织子集
                    user.TenantIds = ls;// 组织组

                    ajax.Add(AjaxResult.DATA_TAG,user);

                }
             }
            else if(userType == "COMPANY_ADMIN") //公司管理员3
            {
            }
            else // 普通用户4
            {
            }


           
            return ajax;

        }



        #region 筛选组织树

      

        /// <summary>
        /// 根据用户类型筛选组织树 返回
        /// </summary>
        /// <param name="tenantTree">完整组织树</param>
        /// <param name="userType">用户类型</param>
        /// <returns>筛选后的组织树</returns>
        private List<TreeSelectTenant> FilterTenantTreeByUserType(long tid,List<TreeSelectTenant> tenantTree,string userType)
        {
            switch(userType)
            {
                case "SUPER_ADMIN":
                    // 超级管理员：返回所有节点
                    return tenantTree;

                case "GROUP_ADMIN":
                    // 集团管理员：只返回指定 tid 下属的一层节点（公司列表）  24-12-18 验证通过 √
                    var groupNode = FindNodeById(tenantTree,tid);
                    if(groupNode != null && groupNode.Children != null)
                    {
                        // 返回子节点列表
                        return groupNode.Children.Select(child => new TreeSelectTenant
                        {
                            Id = child.Id,
                            Label = child.Label
                        }).ToList();
                    }
                    else
                    {
                        // 如果未找到或没有子节点，返回空列表
                        return new List<TreeSelectTenant>();
                    }

                case "COMPANY_ADMIN":
                    // 公司管理员：只返回当前的 tid 节点    24-12-18 验证通过 √
                    var companyNode = FindNodeById(tenantTree,tid);
                    if(companyNode != null)
                    {
                        // 返回当前节点，不包含子节点
                        return new List<TreeSelectTenant>
                {
                    new TreeSelectTenant
                    {
                        Id = companyNode.Id,
                        Label = companyNode.Label
                    }
                };
                    }
                    else
                    {
                        // 如果未找到，返回空列表
                        return new List<TreeSelectTenant>();
                    }

                default:
                    // 普通用户：返回空列表
                    return new List<TreeSelectTenant>();
            }
        }

        /// <summary>
        /// 在树中根据 Id 查找节点
        /// </summary>
        /// <param name="tree">树节点列表</param>
        /// <param name="id">要查找的节点 Id</param>
        /// <returns>找到的节点，未找到则返回 null</returns>
        private TreeSelectTenant? FindNodeById(List<TreeSelectTenant> tree,long id)
        {
            foreach(var node in tree)
            {
                if(node.Id == id)
                    return node;
                if(node.Children != null && node.Children.Any())
                {
                    var found = FindNodeById(node.Children,id);
                    if(found != null)
                        return found;
                }
            }
            return null;
        }

        #endregion

 

        #region 筛选部门树



        /// <summary>
        /// 根据用户类型筛选部门树 返回
        /// </summary>
        /// <param name="deptTree">完整部门树</param>
        /// <param name="userType">用户类型</param>
        /// <returns>筛选后的组织树</returns>
        private List<TreeSelectDept> FilterDeptTreeByUserType(long tid,List<TreeSelectDept> deptTree,string userType)
        {
            switch(userType)
            {
                case "SUPER_ADMIN":
                    // 超级管理员：返回所有节点
                    return deptTree;

                case "GROUP_ADMIN":
                    // 集团管理员：只返回指定 tid 下属的一层节点（公司列表）  24-12-18 验证通过 √
                    var groupNode = FindNodeById(deptTree,tid);
                    if(groupNode != null && groupNode.Children != null)
                    {
                        // 返回子节点列表
                        return groupNode.Children.Select(child => new TreeSelectDept
                        {
                            Id = child.Id,
                            Label = child.Label
                        }).ToList();
                    }
                    else
                    {
                        // 如果未找到或没有子节点，返回空列表
                        return new List<TreeSelectDept>();
                    }

                case "COMPANY_ADMIN":
                    // 公司管理员：只返回当前的 tid 节点    24-12-18 验证通过 √
                    var companyNode = FindNodeById(deptTree,tid);
                    if(companyNode != null)
                    {
                        // 返回当前节点，不包含子节点
                        return new List<TreeSelectDept>
                {
                    new TreeSelectDept
                    {
                        Id = companyNode.Id,
                        Label = companyNode.Label
                    }
                };
                    }
                    else
                    {
                        // 如果未找到，返回空列表
                        return new List<TreeSelectDept>();
                    }

                default:
                    // 普通用户：返回空列表
                    return new List<TreeSelectDept>();
            }
        }

        /// <summary>
        /// 在树中根据 Id 查找节点
        /// </summary>
        /// <param name="tree">树节点列表</param>
        /// <param name="id">要查找的节点 Id</param>
        /// <returns>找到的节点，未找到则返回 null</returns>
        private TreeSelectDept? FindNodeById(List<TreeSelectDept> tree,long id)
        {
            foreach(var node in tree)
            {
                if(node.Id == id)
                    return node;
                if(node.Children != null && node.Children.Any())
                {
                    var found = FindNodeById(node.Children,id);
                    if(found != null)
                        return found;
                }
            }
            return null;
        }

        #endregion




        /// <summary>
        /// 新增用户
        /// </summary>
        [HttpPost("")]
        [AppAuthorize("system:user:add")]
        //[TypeFilter(typeof(RuoYi.Framework.DataValidation.DataValidationFilter))]
        [Log(Title = "用户管理",BusinessType = BusinessType.INSERT)]
        public async Task<AjaxResult> Add([FromBody] SysUserDto user)
        {
            if(!await _sysUserService.CheckUserNameUniqueAsync(user))
            {
                return AjaxResult.Error("新增用户'" + user.UserName + "'失败，登录账号已存在");
            }
            else if(!string.IsNullOrEmpty(user.Phonenumber) && !await _sysUserService.CheckPhoneUniqueAsync(user))
            {
                return AjaxResult.Error("新增用户'" + user.UserName + "'失败，手机号码已存在");
            }
            else if(!string.IsNullOrEmpty(user.Email) && !await _sysUserService.CheckEmailUniqueAsync(user))
            {
                return AjaxResult.Error("新增用户'" + user.UserName + "'失败，邮箱账号已存在");
            }

            if(user.TenantId == 0 || user.TenantId == null)
            {
                user.TenantId = SecurityUtils.GetTenantId(); //增加所属组织
            }
            var data = _sysUserService.InsertUser(user);
            return AjaxResult.Success(data);
        }

        /// <summary>
        /// 修改用户
        /// </summary>
        [HttpPut("")]
        [AppAuthorize("system:user:edit")]
       // [TypeFilter(typeof(RuoYi.Framework.DataValidation.DataValidationFilter))] //数据验证拦截器 DataValidationFilter
        [Log(Title = "用户管理",BusinessType = BusinessType.UPDATE)]
        public async Task<AjaxResult> Edit([FromBody] SysUserDto user)
        {
            _sysUserService.CheckUserAllowed(user);
            await _sysUserService.CheckUserDataScope(user.UserId);
            if(!await _sysUserService.CheckUserNameUniqueAsync(user))
            {
                return AjaxResult.Error("修改用户'" + user.UserName + "'失败，登录账号已存在");
            }
            else if(!string.IsNullOrEmpty(user.Phonenumber) && !await _sysUserService.CheckPhoneUniqueAsync(user))
            {
                return AjaxResult.Error("修改用户'" + user.UserName + "'失败，手机号码已存在");
            }
            else if(!string.IsNullOrEmpty(user.Email) && !await _sysUserService.CheckEmailUniqueAsync(user))
            {
                return AjaxResult.Error("修改用户'" + user.UserName + "'失败，邮箱账号已存在");
            }
 

            var data = _sysUserService.UpdateUser(user);
            return AjaxResult.Success(data);
        }

        /// <summary>
        /// 删除用户
        /// </summary>
        [HttpDelete("{ids}")]
        [AppAuthorize("system:user:remove")]
        [Log(Title = "用户管理",BusinessType = BusinessType.DELETE)]
        public async Task<AjaxResult> Remove(string ids)
        {
            var userIds = ids.SplitToList<long>();
            if(userIds.Contains(SecurityUtils.GetUserId()))
            {
                return AjaxResult.Error("当前用户不能删除");
            }
            var data = await _sysUserService.DeleteUserByIdsAsync(userIds);
            return AjaxResult.Success(data);
        }

        /// <summary>
        /// 重置密码
        /// </summary>
        [HttpPut("resetPwd")]
        [AppAuthorize("system:user:resetPwd")]
        [Log(Title = "用户管理",BusinessType = BusinessType.UPDATE)]
        public async Task<AjaxResult> ResetPwd([FromBody] SysUserDto user)
        {
            _sysUserService.CheckUserAllowed(user);
            await _sysUserService.CheckUserDataScope(user.UserId);
            var data = _sysUserService.ResetPwd(user);
            return AjaxResult.Success(data);
        }

        /// <summary>
        /// 状态修改
        /// </summary>
        [HttpPut("changeStatus")]
        [AppAuthorize("system:user:edit")]
        [Log(Title = "用户管理",BusinessType = BusinessType.UPDATE)]
        public async Task<AjaxResult> ChangeStatus([FromBody] SysUserDto user)
        {
            _sysUserService.CheckUserAllowed(user);
            await _sysUserService.CheckUserDataScope(user.UserId);
            var data = await _sysUserService.UpdateUserStatus(user);
            return AjaxResult.Success(data);
        }

        /// <summary>
        /// 根据用户编号获取授权角色
        /// </summary>
        [HttpGet("authRole/{userId}")]
        [AppAuthorize("system:user:query")]
        public async Task<AjaxResult> GetAuthRole(long userId)
        {
            var user = await _sysUserService.GetDtoAsync(userId);
            var roles = await _sysRoleService.GetRolesByUserIdAsync(userId);

            AjaxResult ajax = AjaxResult.Success();
            ajax.Add("user",user);
            ajax.Add("roles",SecurityUtils.IsAdmin(userId) ? roles : roles.Where(r => !SecurityUtils.IsAdminRole(r.RoleId)));
            return ajax;
        }

        /// <summary>
        /// 用户授权角色
        /// </summary>
        [HttpPut("authRole")]
        [AppAuthorize("system:user:edit")]
        [Log(Title = "用户管理",BusinessType = BusinessType.GRANT)]
        public async Task<AjaxResult> InsertAuthRole(long userId,string roleIds)
        {
            var rIds = roleIds.SplitToList<long>();
            await _sysUserService.CheckUserDataScope(userId);
            _sysUserService.InsertUserAuth(userId,rIds);
            return AjaxResult.Success();
        }

        /// <summary>
        /// 获取部门树列表
        /// </summary>
        /// <param name="dept"></param>
        /// <returns></returns>
        [HttpGet("deptTree")]
        [AppAuthorize("system:user:list")]
        public async Task<AjaxResult> GetDeptTree([FromQuery] SysDeptDto dept)
        {
            var data = await _sysDeptService.GetDeptTreeListAsync(dept);
            return AjaxResult.Success(data);
        }

        /// <summary>
        /// 导入 用户信息表
        /// </summary>
        [HttpPost("importData")]
        [AppAuthorize("system:user:import")]
        [Log(Title = "用户管理",BusinessType = BusinessType.IMPORT)]
        public async Task<AjaxResult> Import([Required] IFormFile file,bool updateSupport)
        {
            var stream = new MemoryStream();
            file.CopyTo(stream);
            var list = await ExcelUtils.ImportAllAsync<SysUserDto>(stream);
            var msg = await _sysUserService.ImportDtosAsync(list,updateSupport,SecurityUtils.GetUsername());
            return AjaxResult.Success(msg);
        }

        /// <summary>
        /// 下载导入模板
        /// </summary>
        /// <returns></returns>
        [HttpPost("importTemplate")]
        [AppAuthorize("system:user:import")]
        public async Task DownloadImportTemplate( )
        {
            await ExcelUtils.GetImportTemplateAsync<SysUserDto>(App.HttpContext.Response,"用户数据");
        }

        /// <summary>
        /// 导出 用户信息表
        /// </summary>
        [HttpPost("export")]
        [AppAuthorize("system:user:export")]
        [Log(Title = "用户管理",BusinessType = BusinessType.EXPORT)]
        public async Task Export(SysUserDto dto)
        {
            var list = await _sysUserService.GetUserListAsync(dto);
            var dtos = _sysUserService.ToDtos(list);
            await ExcelUtils.ExportAsync(App.HttpContext.Response,dtos);
        }
    }
}