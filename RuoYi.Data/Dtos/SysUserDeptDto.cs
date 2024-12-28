using System.Collections.Generic;
using SqlSugar;
namespace RuoYi.Data.Dtos
{
    /// <summary>
    ///  用户和部门关联表 对象 sys_user_dept
    ///  author ruoyi.net
    ///  date   2023-08-23 09:43:52
    /// </summary>
    public class SysUserDeptDto : BaseDto
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public long? UserId { get; set; }
        /// <summary>
        /// 部门ID
        /// </summary>
        public long? DeptId { get; set; }

        /** 所属组织 */
        public long TenantId { get; set; }
    }
}
