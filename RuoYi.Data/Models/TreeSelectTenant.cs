using Newtonsoft.Json;
using RuoYi.Data.Dtos;
using RuoYi.Data.Entities;

namespace RuoYi.Data.Models
{
    public class TreeSelectTenant
    {
        /** 节点ID */
        public long Id { get; set; }

        /** 节点名称 */
        public string Label { get; set; }

        /** 子节点 */
        public List<TreeSelectTenant>? Children { get; set; }

        public TreeSelectTenant()
        {
        }

        public TreeSelectTenant(SysTenantDto dept)
        {
            this.Id = dept.Id ?? 0;
            this.Label = dept.DeptName!;
            this.Children = dept.Children?.Select(c => new TreeSelectTenant(c)).ToList();
        }

        public TreeSelectTenant(SysMenu menu)
        {
            this.Id = menu.MenuId;
            this.Label = menu.MenuName!;
            this.Children = menu.Children?.Select(m => new TreeSelectTenant(m)).ToList();
        }

        // 按条件忽略字段: https://www.newtonsoft.com/json/help/html/conditionalproperties.htm
        public bool ShouldSerializeChildren()
        {
            return Children != null && Children.Any();
        }
    }
}
