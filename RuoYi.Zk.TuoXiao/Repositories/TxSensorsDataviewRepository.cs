using RuoYi.Common.Data;
using RuoYi.Data;
using RuoYi.Zk.TuoXiao.Dtos;
using RuoYi.Zk.TuoXiao.Entities;
using SqlSugar;

namespace RuoYi.Zk.TuoXiao.Repositories
{
    /// <summary>
    ///  工厂设备_传感器大屏数据 Repository
    ///  author ruoyi.net
    ///  date   2024-10-26 16:11:03
    /// </summary>
    public class TxSensorsDataviewRepository : BaseRepository<TxSensorsDataview, TxSensorsDataviewDto007>
    {
        public TxSensorsDataviewRepository(ISqlSugarRepository<TxSensorsDataview> sqlSugarRepository)
        {
            Repo = sqlSugarRepository;
        }

        public override ISugarQueryable<TxSensorsDataview> Queryable(TxSensorsDataviewDto007 dto)
        {
            return Repo.AsQueryable()
                .WhereIF(dto.Id > 0, (t) => t.Id == dto.Id)
            ;
        }

        public override ISugarQueryable<TxSensorsDataviewDto007> DtoQueryable(TxSensorsDataviewDto007 dto)
        {
            return Repo.AsQueryable()
                .WhereIF(dto.Id > 0, (t) => t.Id == dto.Id)
                .Select((t) => new TxSensorsDataviewDto007
                {
                    Id = t.Id
                }, true);
        }
    }
}