using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuoYi.Iot.Vo
{
    /// <summary>
    /// 删除定时任务（支持批量）
    /// </summary>
    public class JobDeleteVo
    {
        public List<long> job_id { get; set; } = new List<long>();
    }

}
