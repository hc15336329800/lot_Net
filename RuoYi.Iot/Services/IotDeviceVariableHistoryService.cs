using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RuoYi.Data.Dtos.Iot;
using RuoYi.Data.Dtos.IOT;
using RuoYi.Iot.Repositories;

namespace RuoYi.Iot.Services
{
    public class IotDeviceVariableHistoryService : BaseService<IotDeviceVariableHistory,IotDeviceVariableHistoryDto>, ITransient
    {
        private readonly IotDeviceVariableHistoryRepository _repo;
        private readonly ILogger<IotDeviceVariableHistoryService> _logger;

        public IotDeviceVariableHistoryService(ILogger<IotDeviceVariableHistoryService> logger,IotDeviceVariableHistoryRepository repo)
        {
            _logger = logger;
            _repo = repo;
            BaseRepo = repo;
        }

        public async Task<IotDeviceVariableHistory> GetAsync(long id)
        {
            return await base.FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<IotDeviceVariableHistoryDto> GetDtoAsync(long id)
        {
            var dto = new IotDeviceVariableHistoryDto { Id = id };
            return await _repo.GetDtoFirstAsync(dto);
        }

 
    }
}
