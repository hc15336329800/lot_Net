using RuoYi.Common.Enums;
using RuoYi.Data.Dtos.IOT;
using RuoYi.Framework;
using RuoYi.Iot.Services;
using RuoYi.System;
using SqlSugar;

namespace RuoYi.Iot.Controllers
{
    [ApiDescriptionSettings("Iot")]
    [Route("iot/product")]
    [AllowAnonymous] //匿名
    public class IotProductController : ControllerBase
    {
        private readonly ILogger<IotProductController> _logger;
        private readonly IotProductService _service;

        public IotProductController(ILogger<IotProductController> logger,IotProductService service)
        {
            _logger = logger;
            _service = service;
        }

        [HttpGet("list")]
        public async Task<SqlSugarPagedList<IotProductDto>> List([FromQuery] IotProductDto dto)
        {
            return await _service.GetDtoPagedListAsync(dto);
        }

        [HttpGet("{id}")]
        public async Task<AjaxResult> Get(long id)
        {
            var data = await _service.GetDtoAsync(id);
            return AjaxResult.Success(data);
        }

        [HttpPost("add")]
        [Log(Title = "产品",BusinessType = BusinessType.INSERT)]
        public async Task<AjaxResult> Add([FromBody] IotProductDto dto)
        {
            var ok = await _service.InsertAsync(dto);
            return AjaxResult.Success(ok);
        }

        [HttpPost("edit")]
        [Log(Title = "产品",BusinessType = BusinessType.UPDATE)]
        public async Task<AjaxResult> Edit([FromBody] IotProductDto dto)
        {
            var data = await _service.UpdateAsync(dto);
            return AjaxResult.Success(data);
        }

  
        [HttpPost("delete")]
        [Log(Title = "产品",BusinessType = BusinessType.DELETE)]
        public async Task<AjaxResult> Delete([FromBody] long[] ids)
        {
            var data = await _service.DeleteAsync(ids);
            return AjaxResult.Success(data);
        }
    }
}
