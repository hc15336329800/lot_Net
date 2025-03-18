namespace RuoYi.Data.Models
{
    public class LoginBody
    {

        /// <summary>
        /// 租户
        /// </summary>
        public long Tenantid { get; set; }

        /// <summary>
        /// 登录类型   如：SUPER_ADMIN、GROUP_ADMIN、COMPANY_ADMIN、GROUP_USER、COMPANY_USER
        /// </summary>
        public string Usertype { get; set; }


        /// <summary>
        /// 用户名
        /// </summary>
        public string Username { get;set; }

        /// <summary>
        /// 用户密码
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// 验证码
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 唯一标识
        /// </summary>
        public string Uuid { get; set; }
    }
}
