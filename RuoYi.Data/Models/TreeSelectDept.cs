using Newtonsoft.Json;
using RuoYi.Data.Dtos;
using RuoYi.Data.Entities;

namespace RuoYi.Data.Models
{
    public class TreeSelectDept
    {
        /** 节点ID */
        public long Id { get; set; }

        /** 节点名称 */
        public string Label { get; set; }

        /** 子节点 */
        public List<TreeSelectDept>? Children { get; set; }

        public TreeSelectDept( )
        {
        }

        public TreeSelectDept(SysDept dept)
        {
            this.Id = dept.DeptId;
            this.Label = dept.DeptName!;
            this.Children = dept.Children?.Select(c => new TreeSelectDept(c)).ToList();
        }

        //public TreeSelectDept(SysMenu menu)
        //{
        //    this.Id = menu.MenuId;
        //    this.Label = menu.MenuName!;
        //    this.Children = menu.Children?.Select(m => new TreeSelectDept(m)).ToList();
        //}

        // 按条件忽略字段: https://www.newtonsoft.com/json/help/html/conditionalproperties.htm
        public bool ShouldSerializeChildren()
        {
            return Children != null && Children.Any();
        }
    }
}
