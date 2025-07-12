using RuoYi.Common.Data;
using RuoYi.Data.Dtos.IOT;
using RuoYi.Data.Entities.Iot;
using SqlSugar;

namespace RuoYi.Iot.Repositories;

public class IotProductRepository : BaseRepository<IotProduct,IotProductDto>
{
    public IotProductRepository(ISqlSugarRepository<IotProduct> repo)
    {
        Repo = repo;
    }

    public override ISugarQueryable<IotProduct> Queryable(IotProductDto dto)
    {
        return Repo.AsQueryable()
            .WhereIF(dto.Id > 0,d => d.Id == dto.Id)
            .WhereIF(!string.IsNullOrEmpty(dto.ProductName),d => d.ProductName.Contains(dto.ProductName!));
    }

    public override ISugarQueryable<IotProductDto> DtoQueryable(IotProductDto dto)
    {
        return Repo.AsQueryable()
            .WhereIF(dto.Id > 0,d => d.Id == dto.Id)
            .WhereIF(!string.IsNullOrEmpty(dto.ProductName),d => d.ProductName.Contains(dto.ProductName!))
            .Select(d => new IotProductDto
            {
                Id = d.Id,
                ProductName = d.ProductName,
                OrgId = d.OrgId,
                ProductModel = d.ProductModel,
                ProductCode = d.ProductCode,
                BrandName = d.BrandName,
                NetworkProtocol = d.NetworkProtocol,
                AccessProtocol = d.AccessProtocol,
                DataProtocol = d.DataProtocol,
                IsShared = d.IsShared,
                Description = d.Description,
                Status = d.Status,
                DelFlag = d.DelFlag,
                Remark = d.Remark,
                CreateBy = d.CreateBy,
                CreateTime = d.CreateTime,
                UpdateBy = d.UpdateBy,
                UpdateTime = d.UpdateTime
            });
    }
}