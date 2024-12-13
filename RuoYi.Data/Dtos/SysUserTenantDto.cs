using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RuoYi.Data.Entities;
using SqlSugar;

namespace RuoYi.Data.Dtos
{
    /// <summary>
    /// 用户和组织关联表DTO
    /// </summary>
    public class SysUserTenantDto:BaseDto
    {
        /// <summary>
        /// 用户ID (user_id)
        /// </summary>
        public long UserId { get; set; }
        /// <summary>
        /// 组织ID (T_id)
        /// </summary>
        public long TId { get; set; }

        /// <summary>
        /// 所属组织
        /// </summary>
        public long TenantId { get; set; }
    }
}
