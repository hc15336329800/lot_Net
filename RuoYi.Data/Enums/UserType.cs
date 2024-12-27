using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuoYi.Data.Enums
{


    /// <summary>
    /// 用户类型枚举
    /// </summary>
    public enum UserType
    {
        SUPER_ADMIN,   // 超级管理员
        GROUP_ADMIN,   // 集团管理员
        GROUP_USER,    // 集团普通用户
        COMPANY_ADMIN, // 公司管理员
        COMPANY_USER   // 公司普通用户
    }

}
