using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AIService.Helper;
using AIService.Helper.StockApiHelper.show.api;
using AIService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;

namespace AIService.Controllers
{
    [Route("api/stock/[action]")]
    [ApiController]
    public class StockController : Controller
    {
        #region 数据库连接
        private readonly DbEntity db = new DbEntity();
        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
        #endregion

        #region 加载Redis连接
        private readonly Lazy<RedisHelper> RedisHelper = new Lazy<RedisHelper>();
        #endregion

        #region A股----------------------------------

        #region 沪深

        #region A股沪深  指数集合||A股指数：000002 B股：000003 工业指数：000004 商业指数：000005 地产指数：000006 公用指数：000007 综合指数：000008
        [HttpGet]
        public IActionResult GetAShareHuShenMarketIndex()
        {
            Dictionary<string, string[]> result = new Dictionary<string, string[]>();
            try
            {
                string res = new ShowApiRequest("http://route.showapi.com/131-45", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                .addTextPara("stocks", "000001,000002,000003,000004,000005,000006,000007,000008,399001,399006")
                .post();
                JObject jObject = JsonConvert.DeserializeObject<JObject>(res);
                JArray jArray = JArray.Parse(jObject["showapi_res_body"]["indexList"].ToString());
                for(int j=0;j<jArray.Count;j++)
                {
                    if(jArray[j]["name"].ToString()=="上证指数"|| jArray[j]["name"].ToString() == "深证成指" || jArray[j]["name"].ToString() == "创业板指" )
                        result.Add(jArray[j]["name"].ToString(), new string[3] { jArray[j]["nowPrice"].ToString(), jArray[j]["diff_money"].ToString(), jArray[j]["diff_rate"].ToString() });
                    else
                        result.Add(jArray[j]["name"].ToString(), new string[1] { jArray[j]["nowPrice"].ToString()+"亿" });

                }
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch(Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return Json(new
                {
                    code = 400,
                    data = "糟糕，网络好像出问题了"
                }
                );
            }
            return Json(new 
            {
                code = 200,
                data = result.Select(s=>new 
                {
                    Name = s.Key,
                    NowPrice = s.Value[0],
                    Diff_Rate = s.Value.Length==3 ? s.Value[2] : null,
                    Diff_Money = s.Value.Length == 3 ? s.Value[1] : null,

                })
            });

        }
        #endregion

        #region A股沪深 市场概况
        [HttpGet]
        public IActionResult GetMarketOverview()
        {
            Dictionary<string, string> rise_and_fall = new Dictionary<string, string>();
            try
            {
                string url = "http://q.10jqka.com.cn/api.php?t=indexflash&";
                //string url1 = "http://74.push2.eastmoney.com/api/qt/clist/get?pn=1&pz=20&po=1&np=1&ut=bd1d9ddb04089700cf9c27f6f7426281&fltt=2&invt=2&fid=f3&fs=m:90+t:2&fields=f1,f2,f3,f4,f5,f6,f7,f8,f9,f10,f12,f13,f14,f15,f16,f17,f18,f20,f21,f23,f24,f25,f26,f22,f33,f11,f62,f128,f136,f115,f152,f124,f107,f104,f105,f140,f141,f207,f222&_=1582597649159";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream myResponseStream = response.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
                string retString = myStreamReader.ReadToEnd();
                JObject result = JsonConvert.DeserializeObject<JObject>(retString);

                rise_and_fall.Add("涨股数", result["zdfb_data"]["znum"].ToString());
                rise_and_fall.Add("跌股数", result["zdfb_data"]["dnum"].ToString());
                rise_and_fall.Add("涨跌停对比", result["zdt_data"]["last_zdt"]["dtzs"].ToString() + ":" + result["zdt_data"]["last_zdt"]["ztzs"].ToString());
                rise_and_fall.Add("昨日涨停表现", result["jrbx_data"]["last_zdf"].ToString()+"%");
                rise_and_fall.Add("大盘评级", result["dppj_data"].ToString()+"分");
                myStreamReader.Close();
                myResponseStream.Close();
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch(Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return Json(new
                {
                    code = 400,
                    data = "糟糕，网络好像出问题了"
                }
                );
            }
            
            return Json(new 
            { 
                code = 200,
                data = rise_and_fall
            });
        }
        #endregion

        #region A股沪深 股市状态
        [HttpGet]
        public IActionResult GetAShareStatus()
        {
            Dictionary<string, string> day_in_weekand = new Dictionary<string, string>
            {
                { "Sunday", "星期日" },
                { "Saturday", "星期六"},
                { "Friday", "星期五"},
                { "Thursday", "星期四"},
                { "Wednesday", "星期三"},
                { "Tuesday", "星期二"},
                { "Monday", "星期一"}
            };
            if (ParamHelper.IsHolidayByDate(DateTime.Today.Date).Result == true)
                return Json(new 
                {
                    code = 200,
                    status = "节假日休盘",
                    date = "以下为上一个交易日的数据"
                });
            else
            {
                if (DateTime.Now.Hour >= 0 && DateTime.Now.Hour < 9)
                    return Json(new 
                    {
                        code = 200,
                        status = "未开盘",
                        date = DateTime.Today.ToString("yyyy-MM-dd ") + day_in_weekand.GetValueOrDefault(DateTime.Today.DayOfWeek.ToString())
                    });
                if(DateTime.Now.Hour == 9 && DateTime.Now.Minute >= 0 && DateTime.Now.Minute < 15)
                    return Json(new
                    {
                        code = 200,
                        status = "未开盘",
                        date = "9:15开始集合竞价"
                    });
                if (DateTime.Now.Hour == 9 && DateTime.Now.Minute >= 15 && DateTime.Now.Minute < 30)
                    return Json(new
                    {
                        code = 200,
                        status = "集合竞价中",
                        date = "9:30开始正式交易"
                    });
                if (DateTime.Parse(DateTime.Now.ToLongTimeString()) <= DateTime.Parse("11:30") && DateTime.Parse(DateTime.Now.ToLongTimeString()) >= DateTime.Parse("9:30"))
                    return Json(new
                    {
                        code = 200,
                        status = "开盘中",
                        date = DateTime.Today.ToString("yyyy-MM-dd ") + day_in_weekand.GetValueOrDefault(DateTime.Today.DayOfWeek.ToString())
                    });
                if (DateTime.Parse(DateTime.Now.ToLongTimeString()) < DateTime.Parse("13:00") && DateTime.Parse(DateTime.Now.ToLongTimeString()) > DateTime.Parse("11:30"))
                    return Json(new
                    {
                        code = 200,
                        status = "休市中",
                        date = DateTime.Today.ToString("yyyy-MM-dd ") + day_in_weekand.GetValueOrDefault(DateTime.Today.DayOfWeek.ToString())
                    });
                if (DateTime.Parse(DateTime.Now.ToLongTimeString()) >= DateTime.Parse("13:00") && DateTime.Parse(DateTime.Now.ToLongTimeString()) <= DateTime.Parse("15:00"))
                    return Json(new
                    {
                        code = 200,
                        status = "开盘中",
                        date = DateTime.Today.ToString("yyyy-MM-dd ") + day_in_weekand.GetValueOrDefault(DateTime.Today.DayOfWeek.ToString())
                    });
                if (DateTime.Parse(DateTime.Now.ToLongTimeString()) > DateTime.Parse("15:00") && DateTime.Parse(DateTime.Now.ToLongTimeString()) <= DateTime.Parse("23:59:59"))
                    return Json(new
                    {
                        code = 200,
                        status = "已收盘",
                        date = DateTime.Today.ToString("yyyy-MM-dd ") + day_in_weekand.GetValueOrDefault(DateTime.Today.DayOfWeek.ToString())
                    });

            }
            return Json(new{ });
        }
        #endregion

        #region A股 股票排行
        [HttpGet]
        public IActionResult GetStocksRankingBrief(string SortField, int SortType, int Page)
        {
            JObject jObject = null;
            JArray jArray = null;
            Dictionary<string, string[]> result = new Dictionary<string, string[]>();
            try
            {
                string res = new ShowApiRequest("http://route.showapi.com/131-64", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("sortFeild", SortField)
                    .addTextPara("sortType", SortType.ToString())
                    .addTextPara("page", Page.ToString())
                    .post();
                jObject = JsonConvert.DeserializeObject<JObject>(res);
                jArray = JArray.Parse(jObject["showapi_res_body"]["data"]["list"].ToString());
                for(int i=0;i<jArray.Count;i++)
                {
                    string StockCode = jArray[i]["code"].ToString();
                    string NewestPrice = jArray[i]["nowPrice"].ToString();
                    string SortEvidence;
                    if (SortField == "tradeAmount" || SortField == "tradeNum")
                        SortEvidence = ParamHelper.ConvertNumber(Double.Parse(jArray[i][SortField].ToString()));
                    else
                        SortEvidence = jArray[i][SortField].ToString();
                    string StockName = jArray[i]["name"].ToString();
                    string StockIndustry = null;
                    Stock tempStock = db.Stocks.FirstOrDefault(s => s.StockCode == StockCode);
                    Stock newStock = null;
                    if (tempStock==null)
                    {
                        newStock = new Stock();
                        newStock.StockCode = StockCode;
                        newStock.StockName = StockName;
                        newStock.StockType = Enums.StockType.A股;
                        string tempResult = new ShowApiRequest("http://route.showapi.com/131-46", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("stocks", StockCode)
                    .addTextPara("needIndex", "0")
                    .post();
                        try
                        {
                            JObject tempObject = JsonConvert.DeserializeObject<JObject>(tempResult);
                            string ex_name;
                            if (tempObject["showapi_res_body"]["list"].Count()==0)
                            {
                                StockIndustry = "";
                                ex_name = "";

                            }
                            else
                            {
                                ex_name = tempObject["showapi_res_body"]["list"].First["market"].ToString();
                                if (ex_name == "sh")
                                    newStock.StockExchangeName = Enums.StockExchange.上海证券交易所;
                                else if (ex_name == "sz")
                                    newStock.StockExchangeName = Enums.StockExchange.深圳证券交易所;
                                else if (ex_name == "hk")
                                    newStock.StockExchangeName = Enums.StockExchange.香港交易所;

                                db.Stocks.Add(newStock);
                                db.SaveChanges();
                                StockIndustry = "";
                            }
                            
                        }
                        catch(Exception ex)
                        {
                            continue;
                        }
                        
                        
                    }
                    else
                    {
                        StockIndustry = db.Stocks.FirstOrDefault(s => s.StockCode == StockCode).IndustryName;
                    }
                    result.Add(StockCode, new string[4] { StockName, NewestPrice, SortEvidence, StockIndustry });
                }
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch(Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return Json(new
                {
                    code = 400,
                    data = "糟糕，网络好像出问题了"
                }
                );
            }
            return Json(new 
            {
                code = 200,
                data = result.Select(s=>new 
                {
                    code = s.Key,
                    name = s.Value[0],
                    nowPrice = s.Value[1],
                    sortField = s.Value[2],
                    stockIndustry = s.Value[3]
                })
            });
        }
        #endregion

        #region 热点板块Top5
        [HttpGet]
        public IActionResult GetHotPlateTop5()
        {
            string url = "http://116.62.208.165/api/get_top5_collections";            
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
            string retString = myStreamReader.ReadToEnd();
            JObject result = JsonConvert.DeserializeObject<JObject>(retString);
            if(result["code"].ToString()!= "200")
                return Json(new
                {
                    code = 400,
                    data = "糟糕，网络好像出问题了"
                }
                );
            return Json(result);
        }
        #endregion

        #region 板块列表
        [HttpGet]
        public IActionResult GetPlateCollections()
        {
            string url = "http://116.62.208.165/api/get_plate_collections";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
            string retString = myStreamReader.ReadToEnd();
            JObject result = JsonConvert.DeserializeObject<JObject>(retString);
            if (result["code"].ToString() != "200")
                return Json(new
                {
                    code = 400,
                    data = "糟糕，网络好像出问题了"
                }
                );
            return Json(result);
        }
        #endregion

        #region 科创版股票列表
        [HttpGet]
        public IActionResult GetKeChuangList(int Page, string SortField, int SortType)
        {
            JObject jObject = null;
            try
            {
                string url = "http://116.62.208.165/api/get_kechuang?Page="+Page+"&SortField="+SortField+"&SortType="+SortType;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream myResponseStream = response.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
                string retString = myStreamReader.ReadToEnd();
                jObject = JsonConvert.DeserializeObject<JObject>(retString);

            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return Json(new
                {
                    code = 400,
                    data = "糟糕，网络好像出问题了"
                }
                    );
            }
            return Json(jObject);
        }
        #endregion

        #endregion

        #region 板块

        #region 各版块资金流入前6
        [HttpGet]
        public IActionResult GetMoneyInflowTop6()
        {
            JObject result = null;
            try
            {
                string url = "http://116.62.208.165/api/get_mainforce_money_top6";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream myResponseStream = response.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
                string retString = myStreamReader.ReadToEnd();
                result = JsonConvert.DeserializeObject<JObject>(retString);
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch(Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return Json(new
                {
                    code = 400,
                    data = "糟糕，网络好像出问题了"
                }
                );
            }
            return Json(result);
        }
        #endregion

        #region 各版块前6
        [HttpGet]
        public IActionResult GetThreePlatesTop6()
        {
            JObject jObject = null;
            try
            {
                string url = "http://116.62.208.165/api/get_plates_top6";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream myResponseStream = response.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
                string retString = myStreamReader.ReadToEnd();
                jObject = JsonConvert.DeserializeObject<JObject>(retString);
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch(Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return Json(new
                {
                    code = 400,
                    data = "糟糕，网络好像出问题了"
                }
                );
            }
            return Json(jObject);
        }
        #endregion

        #endregion

        #endregion-----------------------------------

        #region 自选---------------------------------

        #region 添加自选股
        [HttpPost]
        public HttpResponseMessage AddOptionalStock(string Code, long UserId)
        {
            if (db.OptionalStocks.Where(s => s.UserId == UserId).ToList().Count == 10)
                return ApiResponse.BadRequest("自选股最多只能选10支哟");
            OptionalStock optionalStock = new OptionalStock();
            try
            {
                string res1 = new ShowApiRequest("http://route.showapi.com/131-43", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                            .addTextPara("code", Code)
                            .post();
                string res2 = new ShowApiRequest("http://route.showapi.com/131-46", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("stocks", Code)
                    .addTextPara("needIndex", "0")
                    .post();

                JObject jObject1 = JsonConvert.DeserializeObject<JObject>(res1);
                JObject jObject2 = JsonConvert.DeserializeObject<JObject>(res2);
                if (jObject1["showapi_res_body"]["ret_code"].ToString() != "0" || jObject1["showapi_res_body"]["list"].Count() == 0)
                    return ApiResponse.NotFound("未找到数据");
                if (jObject1["showapi_res_body"]["list"].Count() > 1)
                    return ApiResponse.BadRequest("请输入正确的股票代码！");
                if (db.OptionalStocks.Where(s => s.StockCode == jObject1["showapi_res_body"]["list"].First["code"].ToString()&&s.UserId==UserId).Count() > 0)
                    return ApiResponse.BadRequest("已添加该股票,不可重复添加！");
                optionalStock.UserId = UserId;
                optionalStock.StockCode = jObject1["showapi_res_body"]["list"].First["code"].ToString();
                optionalStock.StockName = jObject1["showapi_res_body"]["list"].First["name"].ToString();
                optionalStock.StockPinyin = jObject1["showapi_res_body"]["list"].First["pinyin"].ToString();
                optionalStock.StockType = db.Stocks.FirstOrDefault(s => s.StockCode == optionalStock.StockCode).StockType;
                optionalStock.StockValue = jObject2["showapi_res_body"]["list"].First["all_value"].ToString();
                optionalStock.NowPrice = jObject2["showapi_res_body"]["list"].First["nowPrice"].ToString();
                optionalStock.Diff_Rate = jObject2["showapi_res_body"]["list"].First["diff_rate"].ToString();
                optionalStock.Diff_Money = jObject2["showapi_res_body"]["list"].First["diff_money"].ToString();
                optionalStock.Swing = jObject2["showapi_res_body"]["list"].First["swing"].ToString();
                optionalStock.OpenPrice = jObject2["showapi_res_body"]["list"].First["openPrice"].ToString();
                optionalStock.YesterdayClosePrice = jObject2["showapi_res_body"]["list"].First["closePrice"].ToString();
                optionalStock.TodayMax = jObject2["showapi_res_body"]["list"].First["todayMax"].ToString();
                optionalStock.TodayMin = jObject2["showapi_res_body"]["list"].First["todayMin"].ToString();
                optionalStock.TradeNum = jObject2["showapi_res_body"]["list"].First["tradeNum"].ToString();
                optionalStock.Turnover = jObject2["showapi_res_body"]["list"].First["turnover"].ToString();
                optionalStock.Pe = jObject2["showapi_res_body"]["list"].First["pe"].ToString();
                optionalStock.Pb = jObject2["showapi_res_body"]["list"].First["pb"].ToString();
                optionalStock.AppointRate = jObject2["showapi_res_body"]["list"].First["appointRate"].ToString();
                if (jObject1["showapi_res_body"]["list"].First["market"].ToString().ToLower() == "sh")
                    optionalStock.StockExchange = Enums.StockExchange.上海证券交易所;
                if (jObject1["showapi_res_body"]["list"].First["market"].ToString().ToLower() == "sz")
                    optionalStock.StockExchange = Enums.StockExchange.深圳证券交易所;
                if (jObject1["showapi_res_body"]["list"].First["market"].ToString().ToLower() == "hk")
                    optionalStock.StockExchange = Enums.StockExchange.香港交易所;

                if (jObject2["showapi_res_body"]["list"].First["diff_rate"].ToString().StartsWith("-"))
                    optionalStock.StockTendency = Enums.StockTendency.跌;
                else
                {
                    if (double.Parse(jObject2["showapi_res_body"]["list"].First["diff_rate"].ToString()) == 0)
                        optionalStock.StockTendency = Enums.StockTendency.横盘;
                    else
                    {
                        optionalStock.StockTendency = Enums.StockTendency.涨;
                    }
                }
                db.OptionalStocks.Add(optionalStock);
                db.SaveChanges();
                
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch(Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return ApiResponse.BadRequest("糟糕，网络好像出问题了");
            }
            return ApiResponse.Ok(new
            {
                optionalStock.Id,
                optionalStock.StockCode,
                StockExchange = optionalStock.StockExchange.ToString(),
                optionalStock.StockName,
                optionalStock.StockPinyin,
                StockTendency = optionalStock.StockTendency.ToString(),
                StockType = optionalStock.StockType.ToString(),
                optionalStock.StockValue,
                optionalStock.NowPrice,
                optionalStock.Diff_Rate,
                optionalStock.Diff_Money,
                optionalStock.Swing,
                optionalStock.OpenPrice,
                optionalStock.YesterdayClosePrice,
                optionalStock.TodayMax,
                optionalStock.TodayMin,
                optionalStock.TradeNum,
                optionalStock.Turnover,
                optionalStock.Pe,
                optionalStock.Pb,
                optionalStock.AppointRate
            });
        }
        #endregion

        #region 删除自选股
        [HttpPost]
        public HttpResponseMessage DeleteOptionalStock(long OptionalStockId)
        {
            OptionalStock optionalStock = db.OptionalStocks.FirstOrDefault(s => s.Id == OptionalStockId);
            try
            {
                db.OptionalStocks.Remove(optionalStock);
                db.SaveChanges();
            }
            catch(Exception ex)
            {
                return ApiResponse.BadRequest("删除失败");
            }
            return ApiResponse.Ok("删除成功");
        }
        #endregion

        #region 自选股左上角
        [HttpGet]
        public IActionResult GetOptionalStockIndex()
        {
            Dictionary<string, string[]> result = new Dictionary<string, string[]>();
            try
            {
                string res = new ShowApiRequest("http://route.showapi.com/131-45", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                .addTextPara("stocks", "sh000001,sz399001,sz399005,sz399006,hkhsi")
                .post();
                JObject jObject = JsonConvert.DeserializeObject<JObject>(res);
                JArray jArray = JArray.Parse(jObject["showapi_res_body"]["indexList"].ToString());
                for (int j = 0; j < jArray.Count; j++)
                {
                    result.Add(jArray[j]["name"].ToString(), new string[3] { jArray[j]["nowPrice"].ToString(), jArray[j]["diff_money"].ToString(), jArray[j]["diff_rate"].ToString() });
                }
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return Json(new
                {
                    code = 400,
                    data = "糟糕，网络好像出问题了"
                }
                );
            }
            return Json(new
            {
                code = 200,
                data = result.Select(s => new
                {
                    Name = s.Key,
                    NowPrice = s.Value[0],
                    Diff_Rate = s.Value[2] + "%",
                    Diff_Money = s.Value[1]

                })
            });
        }
        #endregion

        #region 加载自选股列表
        [HttpGet]
        public IActionResult GetOptionalStockList(long UserId)
        {
            List<OptionalStock> optionalStocks = db.OptionalStocks.Where(s => s.UserId == UserId).ToList();
            if(optionalStocks.Count==0)
            {
                return Json(new 
                {
                    code = 400,
                    data = "没有数据"
                });
            }
            StringBuilder stocks = new StringBuilder();
            for(int i=0;i<optionalStocks.Count;i++)
            {
                if (i == optionalStocks.Count - 1)
                    stocks.Append(optionalStocks[i].StockCode);
                else
                    stocks.Append(optionalStocks[i].StockCode).Append(",");
            }
            try
            {
                string res = new ShowApiRequest("http://route.showapi.com/131-46", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("stocks", stocks.ToString())
                    .addTextPara("needIndex", "0")
                    .post();
                JObject jObject = JsonConvert.DeserializeObject<JObject>(res);
                JArray jArray = JArray.Parse(jObject["showapi_res_body"]["list"].ToString());
                for(int j=0;j<jArray.Count;j++)
                {
                    OptionalStock temp = optionalStocks.FirstOrDefault(s => s.StockName == jArray[j]["name"].ToString());
                    if (temp == null)
                        continue;
                    else
                    {
                        temp.NowPrice = jArray[j]["nowPrice"].ToString();
                        temp.Diff_Rate = jArray[j]["diff_rate"].ToString();
                        temp.Diff_Money = jArray[j]["diff_money"].ToString();
                        temp.Swing = jArray[j]["swing"].ToString();
                        temp.OpenPrice = jArray[j]["openPrice"].ToString();
                        temp.YesterdayClosePrice = jArray[j]["closePrice"].ToString();
                        temp.TodayMax = jArray[j]["todayMax"].ToString();
                        temp.TodayMin = jArray[j]["todayMin"].ToString();
                        temp.TradeNum = jArray[j]["tradeNum"].ToString();
                        temp.Turnover = jArray[j]["turnover"].ToString();
                        temp.Pe = jArray[j]["pe"].ToString();
                        temp.Pb = jArray[j]["pb"].ToString();
                        temp.AppointRate = jArray[j]["appointRate"].ToString();
                        if (temp.Diff_Rate.Contains("-"))
                            temp.StockTendency = Enums.StockTendency.跌;
                        else
                        {
                            if (double.Parse(temp.Diff_Rate) == 0)
                                temp.StockTendency = Enums.StockTendency.横盘;
                            else
                                temp.StockTendency = Enums.StockTendency.涨;
                        }
                        db.Entry(temp).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch(Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return Json(new
                {
                    code = 400,
                    data = "糟糕，网络好像出问题了"
                }
                );
            }
            return Json(new 
            {
                code = 200,
                data = optionalStocks.Select(s=>new 
                {
                    s.Id,
                    s.StockCode,
                    s.StockName,
                    s.StockPinyin,
                    s.StockValue,
                    StockType = s.StockType.ToString(),
                    StockExchange = s.StockExchange.ToString(),
                    StockTendency = s.StockTendency.ToString(),
                    s.NowPrice,
                    Diff_Rate = s.Diff_Rate + "%",
                    s.Diff_Money,
                    Swing = s.Swing + "%",
                    s.OpenPrice,
                    s.YesterdayClosePrice,
                    s.TodayMax,
                    s.TodayMin,
                    Turnover = s.Turnover + "%",
                    Pe = s.Pe + "%",
                    Pb = s.Pb + "%",
                    AppointRate = s.AppointRate + "%"
                })
            });
        }
        #endregion

        #endregion-----------------------------------
         
        #region 港股---------------------------------

        #region 港股三大股指
        [HttpGet]
        public IActionResult GetHKShareIndex()
        {
            JObject jObject = null;
            JArray jArray = null;
            Dictionary<string, string[]> result = new Dictionary<string, string[]>();
            try
            {
                string res = new ShowApiRequest("http://route.showapi.com/131-45", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("stocks", "hkhsi,hscei,hscci")
                    .post();
                jObject = JsonConvert.DeserializeObject<JObject>(res);
                jArray = JArray.Parse(jObject["showapi_res_body"]["indexList"].ToString());
                for(int j=0;j<jArray.Count;j++)
                {
                    result.Add(jArray[j]["name"].ToString(), new string[3] { jArray[j]["nowPrice"].ToString(), jArray[j]["diff_rate"].ToString(), jArray[j]["diff_money"].ToString() });
                }
                if (jObject["showapi_res_body"]["ret_code"].ToString() != "0")
                    return Json(new
                    {
                        code = 400,
                        data = "糟糕，网络好像出问题了"
                    }
                );
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return Json(new
                {
                    code = 400,
                    data = "糟糕，网络好像出问题了"
                }
                );
            }
            return Json(new
            {
                code = 200,
                data = result.Select(s=>new 
                {
                    Name = s.Key,
                    NowPrice = s.Value[0],
                    Diff_Rate = s.Value[1],
                    Diff_Money = s.Value[2]
                })
            });
        }
        #endregion

        #region 港股列表 知名港股
        [HttpGet]
        public IActionResult GetWellknownHKStockList(int Page)
        {
            JObject result = null;
            try
            {
                string url = "http://116.62.208.165/api/get_wellknown_HKStocks?PageNumber=" + Page.ToString();
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream myResponseStream = response.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
                string retString = myStreamReader.ReadToEnd();
                result = JsonConvert.DeserializeObject<JObject>(retString);
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return Json(new
                {
                    code = 400,
                    data = "糟糕，网络好像出问题了"
                }
                );
            }
            return Json(result);
        }
        #endregion

        #region 港股列表 港股主板
        [HttpGet]
        public IActionResult GetHKStocksMainBoardList(int Page)
        {
            JObject result = null;
            try
            {
                string url = "http://116.62.208.165/api/get_hkstock_list?PageNumber=" + Page.ToString();
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream myResponseStream = response.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
                string retString = myStreamReader.ReadToEnd();
                result = JsonConvert.DeserializeObject<JObject>(retString);
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch(Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return Json(new
                {
                    code = 400,
                    data = "糟糕，网络好像出问题了"
                }
                );
            }
            return Json(result);
        }
        #endregion

        #region 港股列表 AH股
        [HttpGet]
        public IActionResult GetAHStockList(int Page)
        {
            JObject result = null;
            try
            {
                string url = "http://116.62.208.165/api/get_AHstocks_list?PageNumber=" + Page.ToString();
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream myResponseStream = response.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
                string retString = myStreamReader.ReadToEnd();
                result = JsonConvert.DeserializeObject<JObject>(retString);
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return Json(new
                {
                    code = 400,
                    data = "糟糕，网络好像出问题了"
                }
                );
            }
            return Json(result);
        }
        #endregion

        #region 港股列表 创业板
        [HttpGet]
        public IActionResult GetHKChuangYeBoardList(int Page)
        {
            JObject result = null;
            try
            {
                string url = "http://116.62.208.165/api/get_ChuangYeBoard_list?PageNumber=" + Page.ToString();
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream myResponseStream = response.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
                string retString = myStreamReader.ReadToEnd();
                result = JsonConvert.DeserializeObject<JObject>(retString);
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return Json(new
                {
                    code = 400,
                    data = "糟糕，网络好像出问题了"
                }
                );
            }
            return Json(result);
        }
        #endregion

        #endregion-----------------------------------

        #region 停复牌-------------------------------
        [HttpGet]
        public IActionResult GetStopRecoverStockList()
        {
            JObject jObject, result = null;
            try
            {
                string Datetime_String = DateTime.Now.ToString("yyyyMMdd");
                string res = new ShowApiRequest("http://route.showapi.com/131-57", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                .addTextPara("date", Datetime_String)
                .post();
                jObject = JsonConvert.DeserializeObject<JObject>(res);
                result = JObject.Parse(jObject["showapi_res_body"].ToString());
                if (jObject["showapi_res_body"]["ret_code"].ToString() != "0")
                    return Json(new
                    {
                        code = 404,
                        data = "未找到数据"
                    }
                );
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return Json(new
                {
                    code = 400,
                    data = "糟糕，网络好像出问题了"
                }
                );
            }
            return Json(new 
            {
                code = 200,
                data = result
            });
        }
        #endregion

        #region 盘面---------------------------------

        #region 沪深涨跌分布
        [HttpGet]
        public IActionResult GetPanMian()
        {
            Dictionary<string, string> rise_and_fall = new Dictionary<string, string>();
            try
            {
                string url = "http://q.10jqka.com.cn/api.php?t=indexflash&";
                string url_hotplate = "http://116.62.208.165/api/get_single_plate";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                HttpWebRequest hotplate_request = (HttpWebRequest)WebRequest.Create(url_hotplate);
                request.Method = "GET";
                hotplate_request.Method = "GET";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                HttpWebResponse hotplate_response = (HttpWebResponse)hotplate_request.GetResponse();
                Stream myResponseStream = response.GetResponseStream();
                Stream myResponseStream_HotPlate = hotplate_response.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
                StreamReader myStreamReader_hotplate = new StreamReader(myResponseStream_HotPlate, Encoding.GetEncoding("utf-8"));
                string retString = myStreamReader.ReadToEnd();
                string retString_hotplate = myStreamReader_hotplate.ReadToEnd();
                JObject result = JsonConvert.DeserializeObject<JObject>(retString);
                JObject hotplate_result = JsonConvert.DeserializeObject<JObject>(retString_hotplate);
                if (int.Parse(result["zdfb_data"]["dnum"].ToString()) - int.Parse(result["zdfb_data"]["znum"].ToString()) > 1000)
                    rise_and_fall.Add("盘面评分", "大盘趋势偏弱");
                if (Math.Abs(int.Parse(result["zdfb_data"]["dnum"].ToString()) - int.Parse(result["zdfb_data"]["znum"].ToString())) <=1000)
                    rise_and_fall.Add("盘面评分", "大盘趋势平稳");
                if (int.Parse(result["zdfb_data"]["znum"].ToString()) - int.Parse(result["zdfb_data"]["dnum"].ToString()) > 1000)
                    rise_and_fall.Add("盘面评分", "大盘趋势走高");
                rise_and_fall.Add("热门板块", hotplate_result["plate"].ToString());

                rise_and_fall.Add("涨家数", result["zdfb_data"]["znum"].ToString());
                rise_and_fall.Add("跌家数", result["zdfb_data"]["dnum"].ToString());
                rise_and_fall.Add("跌停~-8%", result["zdfb_data"]["zdfb"][0].ToString());
                rise_and_fall.Add("-8%~-6%", result["zdfb_data"]["zdfb"][1].ToString());
                rise_and_fall.Add("-6%~-4%", result["zdfb_data"]["zdfb"][2].ToString());
                rise_and_fall.Add("-4%~-2%", result["zdfb_data"]["zdfb"][3].ToString());
                rise_and_fall.Add("-2%~0%", result["zdfb_data"]["zdfb"][4].ToString());
                rise_and_fall.Add("0%~2%", result["zdfb_data"]["zdfb"][5].ToString());
                rise_and_fall.Add("2%~4%", result["zdfb_data"]["zdfb"][6].ToString());
                rise_and_fall.Add("4%~6%", result["zdfb_data"]["zdfb"][7].ToString());
                rise_and_fall.Add("6%~8%", result["zdfb_data"]["zdfb"][8].ToString());
                rise_and_fall.Add("8%~涨停", result["zdfb_data"]["zdfb"][9].ToString());
                rise_and_fall.Add("大盘评级", result["dppj_data"].ToString() + "分");
                if(double.Parse(result["dppj_data"].ToString()) <= 4)
                    rise_and_fall.Add("投资建议", "大盘风险较大，请谨慎参与");
                if (double.Parse(result["dppj_data"].ToString()) <= 6 && double.Parse(result["dppj_data"].ToString()) > 4)
                    rise_and_fall.Add("投资建议", "大盘震荡，适当参与");
                if (double.Parse(result["dppj_data"].ToString()) > 6)
                    rise_and_fall.Add("投资建议", "大盘走势良好，积极参与");
                myStreamReader.Close();
                myResponseStream.Close();
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return Json(new
                {
                    code = 400,
                    data = "糟糕，网络好像出问题了"
                }
                );
            }

            return Json(new
            {
                code = 200,
                data = rise_and_fall
            });
        }
        #endregion

        #region 大盘异动信息
        [HttpGet]
        public IActionResult GetYiDongInfo(int Page)
        {
            JObject jObject = null;
            string url = "http://116.62.208.165/api/get_yidong?PageNumber=" + Page.ToString();
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream myResponseStream = response.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
                string retString = myStreamReader.ReadToEnd();
                jObject = JsonConvert.DeserializeObject<JObject>(retString);
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch(Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return Json(new
                {
                    code = 400,
                    data = "糟糕，网络好像出问题了"
                }
                );
            }
            return Json(jObject);

        }
        #endregion

        #endregion

        #region 股票详情-----------------------------

        #region 单支股票详细信息
        [HttpGet]
        public IActionResult GetOneStockDetail(string NameorCode, long UserId)
        {
            JObject jObject = null;
            JObject result = null;
            ParamHelper paramHelper = new ParamHelper();
            string StockCode = NameorCode;
            Dictionary<string, string[]> result_dict = new Dictionary<string, string[]>();
            if (paramHelper.HaveEnglish(StockCode) || paramHelper.HaveHanZi(StockCode))
            {
                Stock tempStock = db.Stocks.FirstOrDefault(s => s.StockName == StockCode || s.StockName.Equals(StockCode));
                if (tempStock == null)
                    return Json(new
                    {
                        code = 404,
                        message = "未找到数据"
                    });
                StockCode = tempStock.StockCode;
            }
            try
            {
                string res = new ShowApiRequest("http://route.showapi.com/131-46", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("stocks", StockCode)
                    .addTextPara("needIndex", "0")
                    .post();
                jObject = JsonConvert.DeserializeObject<JObject>(res);
                result = JObject.Parse(jObject["showapi_res_body"]["list"].First.ToString());
                if (jObject["showapi_res_body"]["ret_code"].ToString() != "0" || jObject["showapi_res_body"]["list"].Count() == 0)
                    return Json(new
                    {
                        code = 404,
                        message = "未找到数据"
                    });
                if(StockCode.Length != 6)
                {
                    string code = result["code"].ToString();
                    string name = result["name"].ToString();
                    string nowPrice = result["nowPrice"].ToString();
                    string diff_money = result["diff_money"].ToString();
                    string diff_rate = result["diff_rate"].ToString() + "%";
                    string todayMax = result["todayMax"].ToString();
                    string openPrice = result["openPrice"].ToString();
                    string todayMin = result["todayMin"].ToString();
                    string turnover = result["turnover"].ToString();
                    string all_value = result["all_value"].ToString() + "亿";
                    string tradeAmount = ParamHelper.ConvertNumber(Double.Parse(result["tradeAmount"].ToString()));
                    string buy1_n = result["buy1_n"].ToString();
                    string buy1_m = result["buy1_m"].ToString();
                    string buy2_n = result["buy2_n"].ToString();
                    string buy2_m = result["buy2_m"].ToString();
                    string buy3_n = result["buy3_n"].ToString();
                    string buy3_m = result["buy3_m"].ToString();
                    string buy4_n = result["buy4_n"].ToString();
                    string buy4_m = result["buy4_m"].ToString();
                    string buy5_n = result["buy5_n"].ToString();
                    string buy5_m = result["buy5_m"].ToString();
                    string sell1_n = result["sell1_n"].ToString();
                    string sell1_m = result["sell1_m"].ToString();
                    string sell2_n = result["sell2_n"].ToString();
                    string sell2_m = result["sell2_m"].ToString();
                    string sell3_n = result["sell3_n"].ToString();
                    string sell3_m = result["sell3_m"].ToString();
                    string sell4_n = result["sell4_n"].ToString();
                    string sell4_m = result["sell4_m"].ToString();
                    string sell5_m = result["sell5_m"].ToString();
                    string sell5_n = result["sell5_n"].ToString();
                    result_dict.Add(name, new string[] { code, nowPrice, diff_money, diff_rate,
                    todayMax, openPrice, todayMin, turnover,
                    all_value, tradeAmount, buy1_n,buy1_m,buy2_n,
                    buy2_m,buy3_n,buy3_m,buy4_n,buy4_m,buy5_n,buy5_m,
                    sell1_n,sell1_m,sell2_n,sell2_m,sell3_n,sell3_m,sell4_n,sell4_m,sell5_m,sell5_n});
                }
                else
                {
                    string code = result["code"].ToString();
                    string name = result["name"].ToString();
                    string nowPrice = result["nowPrice"].ToString();
                    string diff_money = result["diff_money"].ToString();
                    string diff_rate = result["diff_rate"].ToString() + "%";
                    string todayMax = result["todayMax"].ToString();
                    string openPrice = result["openPrice"].ToString();
                    string todayMin = result["todayMin"].ToString();
                    string turnover = result["turnover"].ToString();
                    string all_value = result["all_value"].ToString() + "亿";
                    string tradeAmount = ParamHelper.ConvertNumber(Double.Parse(result["tradeAmount"].ToString()));
                    string buy1_n = result["buy1_n"].ToString();
                    string buy1_m = result["buy1_m"].ToString();
                    string buy2_n = result["buy2_n"].ToString();
                    string buy2_m = result["buy2_m"].ToString();
                    string buy3_n = result["buy3_n"].ToString();
                    string buy3_m = result["buy3_m"].ToString();
                    string buy4_n = result["buy4_n"].ToString();
                    string buy4_m = result["buy4_m"].ToString();
                    string buy5_n = result["buy5_n"].ToString();
                    string buy5_m = result["buy5_m"].ToString();
                    string sell1_n = result["sell1_n"].ToString();
                    string sell1_m = result["sell1_m"].ToString();
                    string sell2_n = result["sell2_n"].ToString();
                    string sell2_m = result["sell2_m"].ToString();
                    string sell3_n = result["sell3_n"].ToString();
                    string sell3_m = result["sell3_m"].ToString();
                    string sell4_n = result["sell4_n"].ToString();
                    string sell4_m = result["sell4_m"].ToString();
                    string sell5_m = result["sell5_m"].ToString();
                    string sell5_n = result["sell5_n"].ToString();
                    result_dict.Add(name, new string[] { code, nowPrice, diff_money, diff_rate,
                    todayMax, openPrice, todayMin, turnover,
                    all_value, tradeAmount, buy1_n,buy1_m,buy2_n,
                    buy2_m,buy3_n,buy3_m,buy4_n,buy4_m,buy5_n,buy5_m,
                    sell1_n,sell1_m,sell2_n,sell2_m,sell3_n,sell3_m,sell4_n,sell4_m,sell5_m,sell5_n});
                }
                
                
                if(db.StockSearches.Where(s=>s.StockCode == result["code"].ToString()&&s.UserId==UserId).Count() == 0)
                {
                    StockSearch stockSearch = new StockSearch()
                    {
                        UserId = UserId,
                        StockCode = result["code"].ToString(),
                        StockName = result["name"].ToString(),
                        SearchTime = DateTime.Now,
                        Status = 1
                    };
                    db.StockSearches.Add(stockSearch);
                    db.SaveChanges();
                }
                else
                {
                    StockSearch stockSearch = db.StockSearches.FirstOrDefault(s => s.StockCode == result["code"].ToString() && s.UserId == UserId);
                    stockSearch.SearchTime = DateTime.Now;
                    db.Entry(stockSearch).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return Json(new
                {
                    code = 400,
                    message = "糟糕，网络好像出问题了"
                });
            }
            return Json(new
            {
                code = 200,
                data = result_dict.Select(s=>new 
                {
                    name = s.Key,
                    code = s.Value[0],
                    nowPrice = s.Value[1],
                    diff_money = s.Value[2],
                    diff_rate = s.Value[3],
                    todayMax = s.Value[4],
                    openPrice = s.Value[5],
                    todayMin = s.Value[6],
                    turnover = s.Value[7],
                    all_value = s.Value[8],
                    tradeAmount = s.Value[9],
                    buy1_n = s.Value[10],
                    buy1_m = s.Value[11],
                    buy2_n = s.Value[12],
                    buy2_m = s.Value[13],
                    buy3_n = s.Value[14],
                    buy3_m = s.Value[15],
                    buy4_n = s.Value[16],
                    buy4_m = s.Value[17],
                    buy5_n = s.Value[18],
                    buy5_m = s.Value[19],
                    sell1_n = s.Value[20],
                    sell1_m = s.Value[21],
                    sell2_n = s.Value[22],
                    sell2_m = s.Value[23],
                    sell3_n = s.Value[24],
                    sell3_m = s.Value[25],
                    sell4_n = s.Value[26],
                    sell4_m = s.Value[27],
                    sell5_m = s.Value[28],
                    sell5_n = s.Value[29],
                })
            });
        }
        #endregion

        #region 上证综指详细信息
        [HttpGet]
        public IActionResult GetShangZheng()
        {
            JObject jObject = null;
            try
            {
                string res = new ShowApiRequest("http://route.showapi.com/131-45", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                                 .addTextPara("stocks", "sh000001")
                                 .post();
                JObject temp = JsonConvert.DeserializeObject<JObject>(res);
                jObject = JObject.Parse(temp["showapi_res_body"]["indexList"].First.ToString());
                jObject["tradeNum"] = ParamHelper.ConvertNumber(Double.Parse(jObject["tradeNum"].ToString()));
                jObject["tradeAmount"] = ParamHelper.ConvertNumber(Double.Parse(jObject["tradeAmount"].ToString()));
                jObject["time"] = jObject["time"].ToString().Substring(11, 5);
                jObject["diff_rate"] = jObject["diff_rate"].ToString() + "%";
            }
            catch(Exception ex)
            {
                return Json(new 
                {
                    code = 400,
                    data = "糟糕，网络好像出问题了"
                });
            }
            return Json(new
            {
                code = 200,
                data = jObject
            });
        }
        #endregion

        #region 深证成指详细信息
        [HttpGet]
        public IActionResult GetShenZheng()
        {
            JObject jObject = null;
            try
            {
                string res = new ShowApiRequest("http://route.showapi.com/131-45", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                                 .addTextPara("stocks", "sz399001")
                                 .post();
                JObject temp = JsonConvert.DeserializeObject<JObject>(res);
                jObject = JObject.Parse(temp["showapi_res_body"]["indexList"].First.ToString());
                jObject["tradeNum"] = ParamHelper.ConvertNumber(Double.Parse(jObject["tradeNum"].ToString()));
                jObject["tradeAmount"] = ParamHelper.ConvertNumber(Double.Parse(jObject["tradeAmount"].ToString()));
                jObject["time"] = jObject["time"].ToString().Substring(11, 5);
                jObject["diff_rate"] = jObject["diff_rate"].ToString() + "%";
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    code = 400,
                    data = "糟糕，网络好像出问题了"
                });
            }
            return Json(new
            {
                code = 200,
                data = jObject
            });
        }
        #endregion

        #region 创业板指详细信息
        [HttpGet]
        public IActionResult GetChuangYe()
        {
            JObject jObject = null;
            try
            {
                string res = new ShowApiRequest("http://route.showapi.com/131-45", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                                 .addTextPara("stocks", "sz399006")
                                 .post();
                JObject temp = JsonConvert.DeserializeObject<JObject>(res);
                jObject = JObject.Parse(temp["showapi_res_body"]["indexList"].First.ToString());
                jObject["tradeNum"] = ParamHelper.ConvertNumber(Double.Parse(jObject["tradeNum"].ToString()));
                jObject["tradeAmount"] = ParamHelper.ConvertNumber(Double.Parse(jObject["tradeAmount"].ToString()));
                jObject["time"] = jObject["time"].ToString().Substring(11, 5);
                jObject["diff_rate"] = jObject["diff_rate"].ToString() + "%";
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    code = 400,
                    data = "糟糕，网络好像出问题了"
                });
            }
            return Json(new
            {
                code = 200,
                data = jObject
            });
        }
        #endregion

        #region 单只股指实时行情
        [HttpGet]
        public IActionResult GetOneMarketIndexQuotation(string Code)
        {
            string res = null;
            JObject jObject = null;
            JArray result = null;
            try
            {
                if (ParamHelper.IsHolidayByDate(DateTime.Today.Date).Result == false)
                {
                    res = new ShowApiRequest("http://route.showapi.com/131-52", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("code", Code)
                    .post();
                }
                else
                {
                    DateTime yesterday = DateTime.Today.AddDays(-1);
                    while (ParamHelper.IsHolidayByDate(yesterday).Result == true)
                    {
                        yesterday = yesterday.AddDays(-1);
                    }
                    string timeString = yesterday.ToString("yyyyMMdd");
                    res = new ShowApiRequest("http://route.showapi.com/131-52", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("code", Code)
                    .addTextPara("beginDay", timeString)
                    .post();
                }


                jObject = JsonConvert.DeserializeObject<JObject>(res);
                result = JArray.Parse(jObject["showapi_res_body"]["dataList"].ToString());
                result = new JArray(result.OrderBy(s => s["time"]));
                foreach(var item in result)
                {
                    item["time"] = item["time"].ToString().Substring(8, 2) + ":" + item["time"].ToString().Substring(10, 2);
                }
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return Json(new
                {
                    code = 400,
                    message = "糟糕，网络好像出问题了"
                });
            }
            return Json(new
            {
                code = 200,
                y_min = Math.Floor(Double.Parse(result.OrderBy(s => s["open"]).First()["open"].ToString()) - Double.Parse(result.OrderBy(s => s["open"]).First()["open"].ToString()) * 0.05),
                y_max = Math.Ceiling(Double.Parse(result.OrderByDescending(s => s["open"]).First()["open"].ToString()) + Double.Parse(result.OrderBy(s => s["open"]).First()["open"].ToString()) * 0.05),
                data = result
            });
        }
        #endregion

        #region 单只股票实时行情
        [HttpGet]
        public IActionResult GetOneStockRealtimeQuotation(string NameorCode)
        {
            ParamHelper paramHelper = new ParamHelper();
            string res = null;
            string StockCode = NameorCode;
            JObject jObject = null;
            JArray result = null;
            if (paramHelper.HaveHanZi(NameorCode))
            {
                Stock tempStock = db.Stocks.FirstOrDefault(s => s.StockName == NameorCode || s.StockName.Equals(NameorCode));
                if (tempStock == null)
                    return Json(new
                    {
                        code = 404,
                        message = "未找到数据"
                    });
                StockCode = tempStock.StockCode;
            }
            try
            {
                if (ParamHelper.IsHolidayByDate(DateTime.Today.Date).Result == false)
                {
                    if(StockCode.Length!=6)
                    {
                        DateTime yesterday = DateTime.Today.AddDays(-12);
                        string timeString = yesterday.ToString("yyyyMMdd");
                        res = new ShowApiRequest("http://route.showapi.com/131-50", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("code", StockCode)
                    .addTextPara("time","day")
                    .addTextPara("beginDay", timeString)
                    .post();
                    }
                    else
                    {
                        res = new ShowApiRequest("http://route.showapi.com/131-50", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("code", StockCode)
                    .post();
                    }
                    
                }
                else
                {
                    DateTime yesterday = DateTime.Today.AddDays(-1);
                    while(ParamHelper.IsHolidayByDate(yesterday).Result == true)
                    {
                        yesterday = yesterday.AddDays(-1);
                    }
                    if(StockCode.Length!=6)
                    {
                        yesterday = DateTime.Today.AddDays(-12);
                        string timeString = yesterday.ToString("yyyyMMdd");
                        res = new ShowApiRequest("http://route.showapi.com/131-50", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                        .addTextPara("code", StockCode)
                        .addTextPara("time", "day")
                        .addTextPara("beginDay", timeString)
                        .post();
                    }
                    else
                    {
                        string timeString = yesterday.ToString("yyyyMMdd");
                        res = new ShowApiRequest("http://route.showapi.com/131-50", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                        .addTextPara("code", StockCode)
                        .addTextPara("beginDay", timeString)
                        .post();
                    }
                    
                }
                

                jObject = JsonConvert.DeserializeObject<JObject>(res);
                result = JArray.Parse(jObject["showapi_res_body"]["dataList"].ToString());
                if(result.Count==0)
                    return Json(new
                    {
                        code = 200,
                        y_min = -1,
                        y_max = -1,
                        data = result
                    });
                result = new JArray(result.OrderBy(s => s["time"]));
                if(StockCode.Length == 6)
                {
                    foreach (var item in result)
                    {
                        item["time"] = item["time"].ToString().Substring(8, 2) + ":" + item["time"].ToString().Substring(10, 2);
                    }
                }
                else
                {
                    foreach (var item in result)
                    {
                        item["time"] = item["time"].ToString().Substring(4, 4);
                    }
                }

            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return Json(new
                {
                    code = 400,
                    message = "糟糕，网络好像出问题了"
                });
            }
            return Json(new
            {
                code = 200,
                y_min = Math.Floor(Double.Parse(result.OrderBy(s => s["open"]).First()["open"].ToString()) - Double.Parse(result.OrderBy(s => s["open"]).First()["open"].ToString()) * 0.05),
                y_max = Math.Ceiling(Double.Parse(result.OrderByDescending(s => s["open"]).First()["open"].ToString()) + Double.Parse(result.OrderBy(s => s["open"]).First()["open"].ToString()) * 0.05),
                data = result
            });
        }
        #endregion

        #region 论股
        [HttpGet]
        public IActionResult GetStockCommentsList(string StockCode, long UserId)
        {
            IDatabase redisDatabase = RedisHelper.Value.Database;
            User tempUser = db.Users.FirstOrDefault(s => s.Id == UserId);
            List<StockComment> stockComments = db.StockComments.Where(s => s.Stock.StockCode == StockCode).OrderByDescending(s => s.CommentTime).ToList();
            return Json(new
            {
                code = 200,
                data = stockComments.Select(s => new
                {
                    s.Id,
                    s.UserId,
                    tempUser.Username,
                    tempUser.ImageUrl,
                    s.Point,
                    CommentTime = s.CommentTime.ToString(),
                    PraiseNumber = redisDatabase.StringGet("StockCommentId=" + s.Id.ToString() + "&PraiseNumber"),
                    If_Praise = redisDatabase.KeyExists("StockCommentId=" + s.Id.ToString() + "&UserId=" + UserId.ToString()).ToString(),
                    If_Delete = s.UserId == UserId ? "true" : "false"
                })
            });

        }
        #endregion

        #region 发表论股
        [HttpPost]
        public HttpResponseMessage SendStockComments(string StockCode, long UserId, string Point)
        {
            SensitiveWordInterceptor sensitiveWordInterceptor = new SensitiveWordInterceptor();
            IDatabase redisDatabase = RedisHelper.Value.Database;
            sensitiveWordInterceptor.SourctText = Point;
            if(sensitiveWordInterceptor.IsHaveBadWord())
                return ApiResponse.BadRequest("内容中包含敏感词汇，请修改后重新发送！");
            StockComment stockComment = new StockComment()
            {
                Point = Point,
                UserId = UserId,
                User = db.Users.FirstOrDefault(s=>s.Id == UserId),
                StockId = db.Stocks.FirstOrDefault(s => s.StockCode == StockCode).Id,
                CommentTime = DateTime.Now,
            };
            try
            {
                db.StockComments.Add(stockComment);
                db.SaveChanges();
                string StockComment_PraiseNumber_Key = "StockCommentId=" + stockComment.Id.ToString() + "&PraiseNumber";
                redisDatabase.StringSet(StockComment_PraiseNumber_Key, 0);
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch(Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return ApiResponse.BadRequest("发送失败");
            }
            return ApiResponse.Ok(new 
            {
                stockComment.Id,
                UserId = stockComment.UserId,
                ImageUrl = stockComment.User.ImageUrl,
                CommentTime = stockComment.CommentTime.ToString(),
                stockComment.Point           
            });
        }
        #endregion

        #region 论股点赞
        [HttpPost]
        public HttpResponseMessage SendStockCommentPraise(long StockCommentId, long UserId)
        {
            IDatabase redisDatabase = RedisHelper.Value.Database;
            string StockComment_PraiseNumber_Key = "StockCommentId=" + StockCommentId.ToString() + "&PraiseNumber";
            string StockComment_IfPraise_Key = "StockCommentId=" + StockCommentId.ToString() + "&UserId=" + UserId.ToString();
            if (redisDatabase.KeyExists(StockComment_IfPraise_Key))
                return ApiResponse.BadRequest("您已经点过赞了");
            redisDatabase.StringSet(StockComment_IfPraise_Key, 1);
            redisDatabase.StringIncrement(StockComment_PraiseNumber_Key, 1);
            return ApiResponse.Ok("点赞成功");
        }
        #endregion

        #region 论股取消点赞
        [HttpPost]
        public HttpResponseMessage CancelStockCommentPraise(long StockCommentId, long UserId)
        {
            IDatabase redisDatabase = RedisHelper.Value.Database;
            string StockComment_PraiseNumber_Key = "StockCommentId=" + StockCommentId.ToString() + "&PraiseNumber";
            string StockComment_IfPraise_Key = "StockCommentId=" + StockCommentId.ToString() + "&UserId=" + UserId.ToString();
            if (!redisDatabase.KeyExists(StockComment_IfPraise_Key))
                return ApiResponse.BadRequest("您已取消过了");
            redisDatabase.KeyDelete(StockComment_IfPraise_Key);
            redisDatabase.StringDecrement(StockComment_PraiseNumber_Key, 1);
            return ApiResponse.Ok("取消点赞成功");
        }
        #endregion

        #region 删除论股
        [HttpPost]
        public HttpResponseMessage DeleteStockComment(long StockCommentId)
        {
            StockComment stockComment = db.StockComments.FirstOrDefault(s => s.Id == StockCommentId);
            IDatabase redisDatabase = RedisHelper.Value.Database;
            string StockComment_PraiseNumber_Key = "StockCommentId=" + StockCommentId.ToString() + "&PraiseNumber";
            try
            {
                redisDatabase.KeyDelete(StockComment_PraiseNumber_Key);
                db.StockComments.Remove(stockComment);
                db.SaveChanges();
            }
            catch(Exception ex)
            {
                return ApiResponse.BadRequest("删除失败");
            }
            return ApiResponse.Ok("删除成功");
        }
        #endregion

        #endregion

        #region 龙虎榜-------------------------------
        [HttpGet]
        public IActionResult GetLongHuList(string BeginDate, string EndDate, int Page)
        {
            JObject result = null;
            try
            {
                string Start = BeginDate.Insert(4, "-").Insert(7, "-");
                string End = EndDate.Insert(4, "-").Insert(7, "-");
                string url = "http://116.62.208.165/api/get_winnerlist?StartDate=" + Start + "&EndDate=" + End + "&Page=" + Page;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream myResponseStream = response.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
                string retString = myStreamReader.ReadToEnd();
                result = JsonConvert.DeserializeObject<JObject>(retString);
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return Json(new
                {
                    code = 400,
                    data = "糟糕，网络好像出问题了"
                }
                );
            }
            return Json(result);
        }
        #endregion

        #region 移动字幕-----------------------------
        [HttpGet]
        public IActionResult GetWorldStocksIndex()
        {
            JObject result = null;
            try
            {
                string url = "http://116.62.208.165/api/get_worldstock_index";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream myResponseStream = response.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
                string retString = myStreamReader.ReadToEnd();
                result = JsonConvert.DeserializeObject<JObject>(retString);
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return Json(new
                {
                    code = 400,
                    data = "糟糕，网络好像出问题了"
                }
                );
            }
            return Json(result);
        }
        #endregion

        #region 搜索股票初始界面
        [HttpGet]
        public IActionResult GetStartSearchStocks(long UserId)
        {
            List<StockSearch> mySearch = db.StockSearches.Where(s => s.UserId == UserId&&s.Status == 1).OrderByDescending(s => s.SearchTime).ToList();         
            Dictionary<KeyValuePair<string,string>, int> popular = new Dictionary<KeyValuePair<string, string>, int>();
            foreach(var item in db.StockSearches.ToList())
            {
                int temp = popular.GetValueOrDefault(new KeyValuePair<string, string>(item.StockCode, item.StockName), 0) + 1;
                popular[new KeyValuePair<string, string>(item.StockCode, item.StockName)] = temp;
            }
            if (mySearch.Count <= 8 && popular.Count <= 4)
                return Json(new 
                {
                    code = 200,
                    MySearch = mySearch.Select(s=>new 
                    {
                        s.StockName,
                        s.StockCode
                    }),
                    AllSearch = popular.OrderByDescending(s=>s.Value).Select(s=>new 
                    {
                        StockCode = s.Key.Key,
                        StockName = s.Key.Value
                    })
                });
            else if (mySearch.Count <= 8 && popular.Count > 4)
                return Json(new
                {
                    code = 200,
                    MySearch = mySearch.Select(s => new
                    {
                        s.StockName,
                        s.StockCode
                    }),
                    AllSearch = popular.OrderByDescending(s => s.Value).Take(4).Select(s => new
                    {
                        StockCode = s.Key.Key,
                        StockName = s.Key.Value
                    })
                });
            else if (mySearch.Count > 8 && popular.Count <= 4)
                return Json(new
                {
                    code = 200,
                    MySearch = mySearch.Take(8).Select(s => new
                    {
                        s.StockName,
                        s.StockCode
                    }),
                    AllSearch = popular.OrderByDescending(s => s.Value).Select(s => new
                    {
                        StockCode = s.Key.Key,
                        StockName = s.Key.Value
                    })
                });
            else
                return Json(new
                {
                    code = 200,
                    MySearch = mySearch.Take(8).Select(s => new
                    {
                        s.StockName,
                        s.StockCode
                    }),
                    AllSearch = popular.OrderByDescending(s => s.Value).Take(4).Select(s => new
                    {
                        StockCode = s.Key.Key,
                        StockName = s.Key.Value
                    })
                });


        }
        #endregion

        #region 搜索股票
        [HttpGet]
        public IActionResult SearchStocks(string SearchText)
        {
            
            List<Stock> stocks = db.Stocks.Where(s => s.StockCode.ToLower() == SearchText.ToLower() || s.StockName.ToLower() == SearchText.ToLower() || s.StockName.ToLower().Contains(SearchText.ToLower()) || s.StockCode.ToLower().Contains(SearchText.ToLower()) || GetSimilarity(SearchText, s.StockName) >= 0.75 || SearchText.ToLower().Contains(s.StockName.ToLower())).ToList();
            return Json(new 
            {
                code = 200,
                data = stocks.Select(s=>new 
                {
                    s.StockName,
                    s.StockCode
                })
            });
        }
        #endregion

        #region 清除搜索历史记录
        [HttpPost]
        public HttpResponseMessage DeleteMySearch(long UserId)
        {
            IList<StockSearch> stockSearches = db.StockSearches.Where(s => s.UserId == UserId && s.Status == 1).ToList();
            foreach(var item in stockSearches)
            {
                item.Status = 0;
                db.Entry(item).State = EntityState.Modified;
            }
            db.SaveChanges();
            return ApiResponse.Ok("删除成功");
        }
        #endregion

        #region 编辑距离算法文本相似度匹配
        public static double GetSimilarity(String doc1, String doc2)
        {
            if (doc1 != null && doc1.Trim().Length > 0 && doc2 != null
                    && doc2.Trim().Length > 0)
            {
                Dictionary<int, int[]> AlgorithmMap = new Dictionary<int, int[]>();
                //将两个字符串中的中文字符以及出现的总数封装到，AlgorithmMap中
                for (int i = 0; i < doc1.Length; i++)
                {
                    char d1 = doc1.ToCharArray()[i];
                    if (IsHanZi(d1))
                    {
                        int charIndex = GetGB2312Id(d1);
                        if (charIndex != -1)
                        {
                            int[] fq = null;
                            try
                            {
                                fq = AlgorithmMap[charIndex];
                            }
                            catch (Exception)
                            {
                            }
                            finally
                            {
                                if (fq != null && fq.Length == 2)
                                {
                                    fq[0]++;
                                }
                                else
                                {
                                    fq = new int[2];
                                    fq[0] = 1;
                                    fq[1] = 0;
                                    AlgorithmMap.Add(charIndex, fq);
                                }
                            }
                        }
                    }
                }
                for (int i = 0; i < doc2.Length; i++)
                {
                    char d2 = doc2.ToCharArray()[i];
                    if (IsHanZi(d2))
                    {
                        int charIndex = GetGB2312Id(d2);
                        if (charIndex != -1)
                        {
                            int[] fq = null;
                            try
                            {
                                fq = AlgorithmMap[charIndex];
                            }
                            catch (Exception)
                            {
                            }
                            finally
                            {
                                if (fq != null && fq.Length == 2)
                                {
                                    fq[1]++;
                                }
                                else
                                {
                                    fq = new int[2];
                                    fq[0] = 0;
                                    fq[1] = 1;
                                    AlgorithmMap.Add(charIndex, fq);
                                }
                            }
                        }
                    }
                }
                double sqdoc1 = 0;
                double sqdoc2 = 0;
                double denominator = 0;
                foreach (KeyValuePair<int, int[]> par in AlgorithmMap)
                {
                    int[] c = par.Value;
                    denominator += c[0] * c[1];
                    sqdoc1 += c[0] * c[0];
                    sqdoc2 += c[1] * c[1];
                }
                return denominator / Math.Sqrt(sqdoc1 * sqdoc2);
            }
            else
            {
                throw new Exception();
            }
        }
        public static bool IsHanZi(char ch)
        {
            // 判断是否汉字
            return (ch >= 0x4E00 && ch <= 0x9FA5);
        }
        /**
         * 根据输入的Unicode字符，获取它的GB2312编码或者ascii编码，
         * 
         * @param ch
         *            输入的GB2312中文字符或者ASCII字符(128个)
         * @return ch在GB2312中的位置，-1表示该字符不认识
         */
        public static short GetGB2312Id(char ch)
        {
            try
            {
                byte[] buffer = System.Text.Encoding.GetEncoding("gb2312").GetBytes(ch.ToString());
                if (buffer.Length != 2)
                {
                    // 正常情况下buffer应该是两个字节，否则说明ch不属于GB2312编码，故返回'?'，此时说明不认识该字符
                    return -1;
                }
                int b0 = (int)(buffer[0] & 0x0FF) - 161; // 编码从A1开始，因此减去0xA1=161
                int b1 = (int)(buffer[1] & 0x0FF) - 161; // 第一个字符和最后一个字符没有汉字，因此每个区只收16*6-2=94个汉字
                return (short)(b0 * 94 + b1);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return -1;
        }
        #endregion

    }
}