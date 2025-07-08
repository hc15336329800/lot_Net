using RuoYi.Data.Entities;
using SqlSugar;
using System;

namespace RuoYi.Zk.AC.Model.Entities
{
    /// <summary>
    /// 租户或者公司组织信息表
    /// </summary>
    [SugarTable("tenant_user")]
    public class TenantUser : BaseEntity  // 继承 BaseEntity
    {
        /// <summary>
        /// 租户ID
        /// </summary>
        [SugarColumn(ColumnName = "tenant_id")]
        public long TenantId { get; set; }

        /// <summary>
        /// 父租户ID
        /// </summary>
        [SugarColumn(ColumnName = "parent_id")]
        public long? ParentId { get; set; }

        /// <summary>
        /// 编码
        /// </summary>
        [SugarColumn(ColumnName = "code")]
        public string Code { get; set; }

        /// <summary>
        /// 租户名称
        /// </summary>
        [SugarColumn(ColumnName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// 类型：2为代理，1为公司
        /// </summary>
        [SugarColumn(ColumnName = "type")]
        public char Type { get; set; } = '2';

        /// <summary>
        /// 联系人
        /// </summary>
        [SugarColumn(ColumnName = "contact_person")]
        public string ContactPerson { get; set; }

        /// <summary>
        /// 联系电话
        /// </summary>
        [SugarColumn(ColumnName = "contact_phone")]
        public string ContactPhone { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        [SugarColumn(ColumnName = "email")]
        public string Email { get; set; }

        /// <summary>
        /// 地址
        /// </summary>
        [SugarColumn(ColumnName = "address")]
        public string Address { get; set; }

        /// <summary>
        /// 系统用户ID
        /// </summary>
        [SugarColumn(ColumnName = "user_id")]
        public long? UserId { get; set; }

        /// <summary>
        /// 租户菜单ID
        /// </summary>
        [SugarColumn(ColumnName = "tenant_menu_id")]
        public long? TenantMenuId { get; set; }

        /// <summary>
        /// 状态：0表示正常，1表示停用
        /// </summary>
        [SugarColumn(ColumnName = "status")]
        public char Status { get; set; } = '0';

        /// <summary>
        /// 创建人
        /// </summary>
        [SugarColumn(ColumnName = "create_by")]
        public string CreateBy { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [SugarColumn(ColumnName = "create_time")]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 更新人
        /// </summary>
        [SugarColumn(ColumnName = "update_by")]
        public string UpdateBy { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        [SugarColumn(ColumnName = "update_time")]
        public DateTime UpdateTime { get; set; }

        /// <summary>
        /// 删除状态：0为正常，1为已删除
        /// </summary>
        [SugarColumn(ColumnName = "is_deleted")]
        public char IsDeleted { get; set; } = '0';
    }
}
