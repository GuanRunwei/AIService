using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AIService.Enums;


namespace AIService.Models
{
    public class Stock //此实体属性会继续扩充
    {
        public long Id { get; set; } //股票Id
        public string CompanyId { get; set; } //上市公司Id
        public string StockCode { get; set; } //股票代码
        public string StockName { get; set; } //股票名称
        public string IndustryName { get; set; } //行业名称
        public string RegisterAddress { get; set; } //注册具体地址
        public string OfficeAddress { get; set; } //公司办公地址
        public string StockValue { get; set; } //市值
        public string EstablishDate { get; set; } //公司成立日期
        public string BusinessScope { get; set; } //经营范围
        //public string ShenWanIndustry { get; set; }
        //public string ConceptPlate { get; set; }
        //public string RegionPlate { get; set; }
        //public string AFAF_Industry { get; set; }
        //public string ExtractiveIndustry { get; set; }
        //public string ManufacturingIndustry { get; set; }
        public StockExchange StockExchangeName { get; set; }
        public StockType StockType { get; set; }

        

    }
}
