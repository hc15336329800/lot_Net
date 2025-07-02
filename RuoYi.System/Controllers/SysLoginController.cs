using RuoYi.Common.Utils;
using RuoYi.Data.Enums;
using RuoYi.Data.Models;
using RuoYi.System.Services;

namespace RuoYi.Admin
{
    /// <summary>
    /// 登录验证
    /// </summary>
    [ApiDescriptionSettings("System")]
    public class SysLoginController : ControllerBase
    {
        private readonly ILogger<SysLoginController> _logger;
        private readonly TokenService _tokenService;

        private readonly SysLoginService _sysLoginService;
        private readonly SysPermissionService _sysPermissionService;
        private readonly SysMenuService _sysMenuService;
        private readonly SysLogininforService _sysLogininforService;
        private readonly SysUserTenantService _sysUserTenantService;

        public SysLoginController(ILogger<SysLoginController> logger,
            TokenService tokenService,
            SysLoginService sysLoginService,
            SysPermissionService sysPermissionService,
            SysMenuService sysMenuService,
            SysLogininforService sysLogininforService,
            SysUserTenantService sysUserTenantService)
        {
            _logger = logger;
            _tokenService = tokenService;
            _sysLoginService = sysLoginService;
            _sysPermissionService = sysPermissionService;
            _sysMenuService = sysMenuService;
            _sysLogininforService = sysLogininforService;
            _sysUserTenantService = sysUserTenantService;
            
        }

        /// <summary>
        /// 登录验证 - 组织登录
        /// </summary>
        /// <returns></returns>
        [HttpPost("/login")]
        public async Task<AjaxResult> Login([FromBody]LoginBody loginBody)
        {
 
            AjaxResult ajax = AjaxResult.Success();
            //1、登录界面切换组织写法   2、生成令牌
            //loginBody.Tenantid = 0;
            string token = await _sysLoginService.LoginAsync(loginBody.Username,loginBody.Password,loginBody.Code,loginBody.Uuid,loginBody.Tenantid,loginBody.Usertype);
            ajax.Add(Constants.TOKEN,token);



            // 注意：修改逻辑：登录后默认使用集合组织的1号组织，可切换
            // 后台管理员只准属于一个组织！    公司员工可以设置多个归属！
            
            return ajax;

        }

        /// <summary>
        /// 退出
        /// </summary>
        [HttpPost("/logout")]
        public AjaxResult Logout()
        {
            LoginUser loginUser = _tokenService.GetLoginUser(App.HttpContext.Request);
            if (loginUser != null)
            {
                string userName = loginUser.UserName;
                // 删除用户缓存记录
                _tokenService.DelLoginUser(loginUser.Token);
                // 记录用户退出日志
                _ = Task.Factory.StartNew(async () =>
                {
                    await _sysLogininforService.AddAsync(userName, Constants.LOGOUT, "退出成功");
                });
            }
            return AjaxResult.Success("退出成功");
        }

        /// <summary>
        /// 获取用户信息，以及权限集合
        /// </summary>
        [HttpGet("/getInfo")]
        public async Task<AjaxResult> GetInfo()
        {
            SysUserDto user = SecurityUtils.GetLoginUser().User;
            // 角色集合  已整改
            List<string> roles = await _sysPermissionService.GetRolePermissionAsync(user);
            // 权限集合  
            List<string> permissions = _sysPermissionService.GetMenuPermission(user);

            AjaxResult ajax = AjaxResult.Success();
            ajax.Add("user", user);
            ajax.Add("roles", roles);
            ajax.Add("permissions", permissions);
            return ajax;
        }

        /// <summary>
        /// 获取路由信息    
        /// 【重要】/getRouters 方法根据登录用户的 userType 来给路由的 Component 字段添加前缀，从而让前端在 src/views 不同文件夹下加载对应的页面
        /// 
        /// </summary>
        [HttpGet("/getRouters")]
        public AjaxResult GetRouters()
        {
            // 原始代码
            //long userId = SecurityUtils.GetUserId();
            //List<SysMenu> menus = _sysMenuService.SelectMenuTreeByUserId(userId);
            //var treeMenus = _sysMenuService.BuildMenus(menus);
            //return AjaxResult.Success(treeMenus);

            long userId = SecurityUtils.GetUserId();
            string userType = SecurityUtils.GetUserType(); // 如：SUPER_ADMIN、GROUP_ADMIN、COMPANY_ADMIN、GROUP_USER、COMPANY_USER

            List<SysMenu> menus = null;

            if(userType == "SUPER_ADMIN") //超级管理员1
            {
                menus = _sysMenuService.SelectMenuTreeByUserId(userId);

                 // 动态确定前缀，比如根据用户类型或者配置：
                string componentPrefix = "sys_manage/"; // 或 "sys_manage_group/" 或 "sys_manage_company/"
                // 为 menus 中所有的 Component 字段添加前缀
                AddPrefixToComponent(menus,componentPrefix);
            }
            else if(userType == "GROUP_ADMIN") //集团管理员2   已验证
            {
                // 禁止删除：此语句为固定菜单，根据type字段筛选！！
                // 这里传入 int 类型的菜单类型，例如 2 表示集团管理员菜单
                // 改造：根据菜单 type 查询菜单树（不基于用户 ID，仅根据 type 字段）
                // menus = _sysMenuService.SelectMenuTreeByType(2);


                List<SysMenu> menusGROUP_ADMIN = _sysMenuService.SelectMenuTreeByUserId(userId);
                AddPrefixToComponent(menusGROUP_ADMIN,"sys_manage_group/");  // 动态确定前缀，sys_manage_group
                var treeMenusGROUP_ADMIN = _sysMenuService.BuildMenus(menusGROUP_ADMIN);
                return AjaxResult.Success(treeMenusGROUP_ADMIN);
            }
            else if(userType == "GROUP_USER") //集团员工
            {
 
                List<SysMenu> menusGROUP_ADMIN = _sysMenuService.SelectMenuTreeByUserId(userId);
                AddPrefixToComponent(menusGROUP_ADMIN,"sys_user/");   
                var treeMenusGROUP_ADMIN = _sysMenuService.BuildMenus(menusGROUP_ADMIN);
                return AjaxResult.Success(treeMenusGROUP_ADMIN);
            }
            else if(userType == "COMPANY_ADMIN") //公司管理员3
            {
                // 传入 3 表示公司管理员菜单
                //menus = _sysMenuService.SelectMenuTreeByType(3);

                List<SysMenu> menusGROUP_ADMIN = _sysMenuService.SelectMenuTreeByUserId(userId);
                AddPrefixToComponent(menusGROUP_ADMIN,"sys_manage_company/");  // 动态确定前缀，sys_manage_company
                var treeMenusGROUP_ADMIN = _sysMenuService.BuildMenus(menusGROUP_ADMIN);
                return AjaxResult.Success(treeMenusGROUP_ADMIN);
            }
            else if(userType == "COMPANY_USER") //公司员工
            {
 

                List<SysMenu> menusGROUP_ADMIN = _sysMenuService.SelectMenuTreeByUserId(userId);
                AddPrefixToComponent(menusGROUP_ADMIN,"sys_user/");  
                var treeMenusGROUP_ADMIN = _sysMenuService.BuildMenus(menusGROUP_ADMIN);
                return AjaxResult.Success(treeMenusGROUP_ADMIN);
            }
            else // 普通用户4
            {
                // 对于普通用户，使用原有的用户 ID 查询方法
                menus = _sysMenuService.SelectMenuTreeByUserId(userId);
            }

            var treeMenus = _sysMenuService.BuildMenus(menus);
            return AjaxResult.Success(treeMenus);
        }



        /// <summary>
        /// 递归遍历整个菜单树，然后对每个菜单项的 Component（组件路径） 字段进行修改，添加前缀 "sys_manage/
        /// </summary>
        /// <param name="menus"></param>
        /// <param name="prefix"></param>
        private void AddPrefixToComponent(List<SysMenu> menus,string prefix)
        {
            if(menus == null || !menus.Any())
                return;

            foreach(var menu in menus)
            {
                // 如果 Component 不为空且不已经包含指定的前缀，则添加前缀
                if(!string.IsNullOrEmpty(menu.Component) && !menu.Component.StartsWith(prefix))
                {
                    menu.Component = prefix + menu.Component;
                }

                // 递归处理子菜单
                if(menu.Children != null && menu.Children.Any())
                {
                    AddPrefixToComponent(menu.Children,prefix);
                }
            }
        }


    }
}