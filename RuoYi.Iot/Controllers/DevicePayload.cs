using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuoYi.Iot.Controllers
{
    /// <summary>
    /// Incoming device payload for variable reporting
    /// </summary>
    public class DevicePayload
    {
        public long DeviceId { get; set; }
        public Dictionary<string,string>? Values { get; set; }
    }
}
