using System.ComponentModel.DataAnnotations;
using SqlSugar;

namespace RuoYi.Data.Dtos
{
    public class SysTenantDto : BaseDto
    {
        /** 租户ID */
        public long? Id{ get; set; }

        /** 父ID */
        public long? ParentId{ get; set; }

        /** 祖级列表 */
        public string? Ancestors{ get; set; }

        /** 名称 */
        [Required(ErrorMessage = "部门名称不能为空"), MaxLength(30, ErrorMessage = "部门名称不能超过30个字符")]
        public string? DeptName{ get; set; }

        /** 显示顺序 */
        [Required(ErrorMessage = "显示顺序不能为空")]
        public int? OrderNum{ get; set; }

        /** 负责人 */
        public string? Leader{ get; set; }

        /** 联系电话 */
        [MaxLength(30, ErrorMessage = "联系电话长度不能超过11个字符")]
        public string? Phone{ get; set; }

        /** 邮箱 */
        [EmailAddress(ErrorMessage = "邮箱格式不正确"), MaxLength(50, ErrorMessage = "邮箱长度不能超过50个字符")]
        public string? Email{ get; set; }

        /** 部门状态:0正常,1停用 */
        public string? Status{ get; set; }

        /** 删除标志（0代表存在 2代表删除） */
        public string? DelFlag{ get; set; }

        /** 父租户名称 */
        public string? ParentName{ get; set; }

        /** 子租户 */
        public List<SysDeptDto>? Children { get; set; }

        /** 组织树选择项是否关联显示（0：父子不互相关联显示 1：父子互相关联显示 ） */
        public bool? DeptCheckStrictly { get; set; }
        // 角色ID
        public long? RoleId { get; set; }


        /** 父组织ID */
        public List<long>? ParentIds { get; set; }


        /** 所属组织 */
        public long TenantId { get; set; }
    }
}
