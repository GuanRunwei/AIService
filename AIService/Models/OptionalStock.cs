using AIService.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIService.Models
{
    public class OptionalStock
    {
        #region 导航属性
        public virtual User User { get; set; }
        #endregion
        public long Id { get; set; }
        public long UserId { get; set; }
        public string StockCode { get; set; }  //股票代码
        public string StockName { get; set; }  //股票名称
        public string StockPinyin { get; set; }  //股票拼音
        public string StockValue { get; set; }  //股票市值
        public StockType StockType { get; set; }  //股票类型（A股0 B股1 港股2）
        public StockExchange StockExchange { get; set; }  //股票交易所
        public StockTendency StockTendency { get; set; }  //股票趋势
        public string NowPrice { get; set; } //最新
        public string Diff_Rate { get; set; } //涨跌幅
        public string Diff_Money { get; set; } //涨跌金额
        public string Swing { get; set; } //振幅
        public string OpenPrice { get; set; } //今开
        public string YesterdayClosePrice { get; set; } //昨收
        public string TodayMax { get; set; }  //今日最高价
        public string TodayMin { get; set; }  //今日最低价
        public string TradeNum { get; set; } //总手（单位为万）
        public string Turnover { get; set; }  //换手率
        public string Pe { get; set; } //市盈率
        public string Pb { get; set; }  //市净率
        public string AppointRate { get; set; } //委比

        
    }
}
