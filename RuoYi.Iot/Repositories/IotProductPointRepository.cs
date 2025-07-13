using RuoYi.Common.Data;
using RuoYi.Data.Dtos.IOT;
using RuoYi.Data.Entities.Iot;
using SqlSugar;

namespace RuoYi.Iot.Repositories;

public class IotProductPointRepository : BaseRepository<IotProductPoint,IotProductPointDto>
{
    public IotProductPointRepository(ISqlSugarRepository<IotProductPoint> repo)
    {
        Repo = repo;
    }

    public override ISugarQueryable<IotProductPoint> Queryable(IotProductPointDto dto)
    {
        return Repo.AsQueryable()
            .WhereIF(dto.Id > 0,d => d.Id == dto.Id)
            .WhereIF(dto.ProductId.HasValue,d => d.ProductId == dto.ProductId)
             .Where(d => d.Status == "0" && d.DelFlag == "0"); // 无论如何都加

    }

    public override ISugarQueryable<IotProductPointDto> DtoQueryable(IotProductPointDto dto)
    {
        return Repo.AsQueryable()
            .WhereIF(dto.Id > 0,d => d.Id == dto.Id)
            .WhereIF(dto.ProductId.HasValue,d => d.ProductId == dto.ProductId)
            .Where(d => d.Status == "0" && d.DelFlag == "0")  // 无论如何都加

            .Select(d => new IotProductPointDto
            {
                Id = d.Id,
                ProductId = d.ProductId,
                PointName = d.PointName,
                PointKey = d.PointKey,
                VariableType = d.VariableType,
                DataType = d.DataType,
                Unit = d.Unit,
                DefaultValue = d.DefaultValue,
                DecimalDigits = d.DecimalDigits,
                MaxValue = d.MaxValue,
                MinValue = d.MinValue,
                SlaveAddress = d.SlaveAddress,
                FunctionCode = d.FunctionCode,
                DataLength = d.DataLength,
                RegisterAddress = d.RegisterAddress,
                ByteOrder = d.ByteOrder,
                Signed = d.Signed,
                ReadType = d.ReadType,
                StorageMode = d.StorageMode,
                DisplayOnDashboard = d.DisplayOnDashboard,
                CollectFormula = d.CollectFormula,
                ControlFormula = d.ControlFormula,
                Status = d.Status,
                DelFlag = d.DelFlag,
                Remark = d.Remark,
                CloudAccessInfo = d.CloudAccessInfo,
                CreateBy = d.CreateBy,
                CreateTime = d.CreateTime,
                UpdateBy = d.UpdateBy,
                UpdateTime = d.UpdateTime
            });
    }
}