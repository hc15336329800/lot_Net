using RuoYi.Data.Dtos;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuoYi.Zk.AC.Model.Dto
{

    /// <summary>
    /// TreeNodeDto 用于构建 el-tree 组件所需的数据格式 √
    /// </summary>
    public class TreeNodeDto : BaseDto
    {
 
        public long TenantId { get; set; }

        public string Label { get; set; }  
 
        public List<TreeNodeDto> Children { get; set; }  // 子节点集合
    }

}
