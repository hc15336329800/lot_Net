using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlSugar;

namespace RuoYi.Data.Entities
{
    [SugarTable("sys_user_tenant","用户和组织关联表")]

    public class SysUserTenant: BaseEntity
    {
        /// <summary>
        /// 用户ID (user_id)
        /// </summary>
        [SugarColumn(ColumnName = "user_id",ColumnDescription = "用户ID")]
        public long UserId { get; set; }
        /// <summary>
        /// 组织ID (T_id)
        /// </summary>
        [SugarColumn(ColumnName = "T_id",ColumnDescription = "组织ID")]
        public long RoleId { get; set; }

        /// <summary>
        /// 所属组织
        /// </summary>
        [SugarColumn(ColumnName = "tenant_id",ColumnDescription = "所属组织")]
        public long TenantId { get; set; }

    }
}
