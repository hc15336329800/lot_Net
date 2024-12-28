using SqlSugar;

namespace RuoYi.Data.Entities
{
    /// <summary>
    ///  用户和部门关联表 对象 sys_user_dept
    /// </summary>
    [SugarTable("sys_user_dept", "用户和部门关联表")]
    public class SysUserDept : BaseEntity
    {
        /// <summary>
        /// 用户ID (user_id)
        /// </summary>
        [SugarColumn(ColumnName = "user_id", ColumnDescription = "用户ID")]
        public long UserId { get; set; }
        /// <summary>
        /// 部门ID (dept_id)
        /// </summary>
        [SugarColumn(ColumnName = "dept_id", ColumnDescription = "部门ID")]
        public long DeptId { get; set; }

        /** 所属组织 */
        [SugarColumn(ColumnName = "tenant_id",ColumnDescription = "所属组织")]
        public long TenantId { get; set; }
    }
}
