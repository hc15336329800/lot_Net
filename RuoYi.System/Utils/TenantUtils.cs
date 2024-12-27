using RuoYi.Data.Enums;
using RuoYi.Data.Models;

namespace RuoYi.System.Utils
{

    /// <summary>
    /// 组织通用方案
    /// </summary>
    public static class TenantUtils
    {

        /// <summary>
        /// el-select组件（将参数格式化为下拉框格式）: 根据条件获取用户类型下拉框
        /// </summary>
        /// <param name="string"></param>
        /// <returns></returns>
        public static List<ElSelect> GetElSelectByTenant(string userTypeString)
        {
            // 使用 Enum.TryParse 方法将字符串转换为枚举 UserType。!!!注意：此处的 UserType 是 RuoYi.Data.Enums.UserType
            if(!Enum.TryParse<UserType>(userTypeString,true,out var userType))
            {
                // 解析失败时，返回空列表
                return new List<ElSelect>();
            }

            // 定义所有用户类型
            var allUserTypes = new List<ElSelect>
    {
        new ElSelect { Label = "集团管理员", Value = UserType.GROUP_ADMIN.ToString() },
        new ElSelect { Label = "集团普通用户", Value = UserType.GROUP_USER.ToString() },
        new ElSelect { Label = "公司管理员", Value = UserType.COMPANY_ADMIN.ToString() },
        new ElSelect { Label = "公司普通用户", Value = UserType.COMPANY_USER.ToString() }
    };

            switch(userType)
            {
                case UserType.SUPER_ADMIN:
                    // 返回所有用户类型
                    return allUserTypes;

                case UserType.GROUP_ADMIN:
                    // 返回集团普通用户、公司管理员、公司普通用户
                    return allUserTypes.Where(type =>
                        type.Value == UserType.GROUP_USER.ToString() ||
                        type.Value == UserType.COMPANY_ADMIN.ToString() ||
                        type.Value == UserType.COMPANY_USER.ToString()).ToList();

                case UserType.COMPANY_ADMIN:
                    // 返回公司普通用户
                    return allUserTypes.Where(type =>
                        type.Value == UserType.COMPANY_USER.ToString()).ToList();

                case UserType.GROUP_USER:
                case UserType.COMPANY_USER:
                    // 返回空列表
                    return new List<ElSelect>();

                default:
                    // 默认返回空列表
                    return new List<ElSelect>();
            }
        }


    }
}
