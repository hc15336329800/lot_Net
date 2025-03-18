using RuoYi.Data.Dtos;

namespace RuoYi.Data.Models
{
    /// <summary>
    /// 登录用户信息
    /// </summary>
    public class LoginUser
    {
        public LoginUser( ) { }


        /// <summary>
        /// 部门子集
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="deptId"></param>
        /// <param name="user"></param>
        /// <param name="permissions"></param>
        /// <param name="tenantId"></param>
        /// <param name="userType">用户类型</param>
        /// <param name="deptChildId">部门子集</param>
        /// <param name="tenantChildId">组织子集</param>
        public LoginUser(long userId,long deptId,SysUserDto user,List<string> permissions,long tenantId,string userType,long[] deptChildId,long[] tenantChildId)
        {
            this.UserId = userId;
            this.DeptId = deptId;
            this.User = user;
            this.Permissions = permissions;

            this.TenantId = tenantId; // 新增
            this.UserType = userType; // 新增
            this.DeptChildId = deptChildId; // 新增
            this.TenantChildId = tenantChildId; // 新增

        }

        /// <summary>
        /// 部门子集
        /// </summary>
        public long[] DeptChildId { get; set; }

        /// <summary>
        /// 组织子集
        /// </summary>
        public long[] TenantChildId { get; set; }


        /// <summary>
        /// 用户名
        /// </summary>
        [Newtonsoft.Json.JsonProperty(Order = 0)]
        public string UserName { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string Password { get; set; }

        /// <summary>
        /// 是否可用
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        public long UserId { get; set; }

        /// <summary>
        /// 部门ID
        /// </summary>
        public long DeptId { get; set; }

        /// <summary>
        /// 租户ID
        /// </summary>
        public long TenantId { get; set; } // 新增字段



        /// <summary>
        /// 用户类型
        /// 用户类型
        /// SUPER_ADMIN：  超级管理员、
        /// GROUP_ADMIN：  集团管理员、GROUP_USER：集团普通用户、
        /// COMPANY_ADMIN：公司管理员、COMPANY_USER：公司普通用户
        /// </summary>
        public string UserType { get; set; } // 新增字段  

        /// <summary>
        /// 用户唯一标识
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// 登录时间
        /// </summary>
        public long LoginTime { get; set; }

        /// <summary>
        /// 过期时间
        /// </summary>
        public long ExpireTime { get; set; }

        /// <summary>
        /// 登录IP地址
        /// </summary>
        public string IpAddr { get; set; }

        /// <summary>
        /// 登录地点
        /// </summary>
        public string LoginLocation { get; set; }

        /// <summary>
        /// 浏览器类型
        /// </summary>
        public string Browser { get; set; }

        /// <summary>
        /// 操作系统
        /// </summary>
        public string OS { get; set; }

        /// <summary>
        /// 用户信息
        /// </summary>
        public SysUserDto User { get; set; }

        /// <summary>
        /// 权限列表
        /// </summary>
        public List<string> Permissions { get; set; }
    }
}
