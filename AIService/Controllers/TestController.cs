using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AIService.Helper;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Text;
using Newtonsoft.Json;
using AIService.Helper.StockApiHelper.show.api;
using AIService.Models;

namespace AIService.Controllers
{
    [Route("api/test/[action]")]
    [ApiController]
    public class TestController : Controller
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

        #region Form测试
        public IActionResult PostData()
        {
            string data1 = Request.Form["data1"];
            return Json(new 
            {
                data = "你发的数据是："+data1
            });
        }
        #endregion

        #region 项目路径
        public IActionResult GetResult()
        {
            return Json(new { data = Environment.CurrentDirectory });
        }
        #endregion

        #region 获取Python Django平台数据
        [HttpGet]
        public IActionResult GetPython()
        {
            string url = "http://127.0.0.1:8000/blog/get_blogs";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
            string retString = myStreamReader.ReadToEnd();
            JObject bupo = JsonConvert.DeserializeObject<JObject>(retString);
            myStreamReader.Close();
            myResponseStream.Close();
            return Json(new { bupo });
        }
        #endregion 

        #region 获取测试数据
        [HttpGet]
        public HttpResponseMessage GetTestData()
        {
            String res = new ShowApiRequest("http://route.showapi.com/131-46", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                .addTextPara("stocks", "sh601007,sh601008,sh601009,sz000018,hk00941")
                .addTextPara("needIndex", "0")
                .post();
            JObject jObject = JsonConvert.DeserializeObject<JObject>(res);
            return ApiResponse.Ok(new { data = jObject });
        }
        #endregion

        #region 股票实时行情数据获取-批量
        [HttpGet]
        public HttpResponseMessage GetRealtimeStockListQuotation(string Stocks)
        {
            JObject jObject = null;
            try
            {
                string res = new ShowApiRequest("http://route.showapi.com/131-46", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("stocks", Stocks)
                    .addTextPara("needIndex", "0")
                    .post();
                jObject = JsonConvert.DeserializeObject<JObject>(res);
                if (jObject["showapi_res_body"]["ret_code"].ToString() != "0" || jObject["showapi_res_body"]["list"].Count() == 0)
                    return ApiResponse.NotFound("未找到数据");
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return ApiResponse.BadRequest("糟糕，网络好像出问题了");
            }
            return ApiResponse.Ok(jObject);

        }
        #endregion

        #region 股票实时K线图
        [HttpGet]
        public HttpResponseMessage GetRealTimeKLine(string NameorCode, string Time, string BeginDay)
        {
            ParamHelper paramHelper = new ParamHelper();
            string res = null;
            string StockCode = NameorCode;
            JObject jObject = null;
            if (paramHelper.HaveHanZi(NameorCode))
            {
                Stock tempStock = db.Stocks.FirstOrDefault(s => s.StockName == NameorCode || s.StockName.Equals(NameorCode));
                if (tempStock == null)
                    return ApiResponse.NotFound("未找到数据");
                StockCode = tempStock.StockCode;
            }
            try
            {
                if (Time == null && BeginDay == null)
                {
                    res = new ShowApiRequest("http://route.showapi.com/131-50", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("code", StockCode)
                    .post();
                }
                if (Time != null && BeginDay == null)
                {
                    res = new ShowApiRequest("http://route.showapi.com/131-50", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("code", StockCode)
                    .addTextPara("time", Time)
                    .post();
                }
                if (Time == null && BeginDay != null)
                {
                    res = new ShowApiRequest("http://route.showapi.com/131-50", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("code", StockCode)
                    .addTextPara("beginDay", BeginDay)
                    .post();
                }
                if (Time != null && BeginDay != null)
                {
                    res = new ShowApiRequest("http://route.showapi.com/131-50", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("code", StockCode)
                    .addTextPara("time", Time)
                    .addTextPara("beginDay", BeginDay)
                    .post();
                }

                jObject = JsonConvert.DeserializeObject<JObject>(res);
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return ApiResponse.BadRequest("糟糕，网络好像出问题了");
            }
            return ApiResponse.Ok(jObject);
        }
        #endregion

        #region 股票实时分时线数据
        [HttpGet]
        public HttpResponseMessage GetRealTimeAndTimeShareLine(string Code, int Day)
        {
            JObject jObject = null;
            string res = null;
            try
            {
                if (Day.ToString() != null)
                {
                    if (Day > 5)
                        return ApiResponse.BadRequest("天数不得大于5天！");
                    res = new ShowApiRequest("http://route.showapi.com/131-49", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("code", Code)
                    .addTextPara("day", Day.ToString())
                    .post();
                }
                if (Day.ToString() == "0")
                {
                    res = new ShowApiRequest("http://route.showapi.com/131-49", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("code", Code)
                    .post();
                }
                jObject = JsonConvert.DeserializeObject<JObject>(res);
                if (jObject["showapi_res_body"]["ret_code"].ToString() != "0")
                    return ApiResponse.NotFound("未找到数据！");

            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return ApiResponse.BadRequest("糟糕，网络好像出问题了");
            }
            return ApiResponse.Ok(jObject);
        }
        #endregion

        #region 沪深股票最新50条逐笔交易
        [HttpGet]
        public HttpResponseMessage GetNewestTransactions(string Code)
        {
            JObject jObject = null;
            try
            {
                string res = new ShowApiRequest("http://route.showapi.com/131-54", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("code", Code)
                    .post();
                jObject = JsonConvert.DeserializeObject<JObject>(res);
                if (jObject["showapi_res_body"]["ret_code"].ToString() != "0")
                    return ApiResponse.NotFound("未找到数据！");
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return ApiResponse.BadRequest("糟糕，网络好像出问题了");
            }
            return ApiResponse.Ok(jObject);
        }
        #endregion  

        #region 股票历史日线行情查询
        public HttpResponseMessage GetStockHistoryQuotation(string BeginDate, string EndDate, string Code)
        {
            JObject jObject = null;
            try
            {
                DateTime beginDate = DateTime.Parse(BeginDate);
                DateTime endDate = DateTime.Parse(EndDate);
                if (beginDate.CompareTo(endDate) > 0)
                    return ApiResponse.BadRequest("开始日期不得大于结束日期");
                int interval = endDate.Subtract(beginDate).Days;
                if (interval > 31)
                    return ApiResponse.BadRequest("时间间隔不能大于31天！");
                string res = new ShowApiRequest("http://route.showapi.com/131-47", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("begin", BeginDate)
                    .addTextPara("end", EndDate)
                    .addTextPara("code", Code)
                    .post();
                jObject = JsonConvert.DeserializeObject<JObject>(res);
                var temp = jObject["showapi_res_body"]["list"].Count();
                if (jObject["showapi_res_body"]["list"].Count() == 0)
                    return ApiResponse.NotFound("未找到数据！");
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return ApiResponse.BadRequest("糟糕，网络好像出问题了");
            }
            return ApiResponse.Ok(jObject);
        }
        #endregion

        #region 沪深股票内外盘数据
        public HttpResponseMessage GetInandOutData(string Code)
        {
            JObject jObject = null;
            try
            {
                string res = new ShowApiRequest("http://route.showapi.com/131-62", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("code", Code)
                    .post();
                jObject = JsonConvert.DeserializeObject<JObject>(res);
                if (jObject["showapi_res_body"]["ret_code"].ToString() != "0")
                    return ApiResponse.NotFound("未找到数据！");
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return ApiResponse.BadRequest("糟糕，网络好像出问题了");
            }
            return ApiResponse.Ok(jObject);
        }
        #endregion

        #region 大盘股指实时行情-批量[默认为上证指数，深圳成指，中小板指，创业板指，恒生指数]
        [HttpGet]
        public HttpResponseMessage GetDefaultRealTimeStockQuotation()
        {
            JObject jObject = null;
            try
            {
                string res = new ShowApiRequest("http://route.showapi.com/131-45", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("stocks", "sh000001,sz399001,sz399005,sz399006,hkhsi")
                    .post();
                jObject = JsonConvert.DeserializeObject<JObject>(res);
                if (jObject["showapi_res_body"]["ret_code"].ToString() != "0")
                    return ApiResponse.Ok("未找到数据！");
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return ApiResponse.Ok("糟糕，网络好像出问题了");
            }
            return ApiResponse.Ok(jObject);
        }
        #endregion

        #region 大盘股指实时K线图[参数是股指编码、查询的周期、开始日期]
        [HttpGet]
        public HttpResponseMessage GetMarketIndexRealtimeKLine(string Code, string Time, string BeginDay)
        {
            JObject jObject = null;
            string res = null;
            try
            {
                if (Time == null && BeginDay == null)
                {
                    res = new ShowApiRequest("http://route.showapi.com/131-52", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("code", Code)
                    .post();
                }
                if (Time != null && BeginDay == null)
                {
                    res = new ShowApiRequest("http://route.showapi.com/131-52", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("code", Code)
                    .addTextPara("time", Time)
                    .post();
                }
                if (Time == null && BeginDay != null)
                {
                    res = new ShowApiRequest("http://route.showapi.com/131-52", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("code", Code)
                    .addTextPara("beginDay", BeginDay)
                    .post();
                }
                if (Time != null || BeginDay != null)
                {
                    res = new ShowApiRequest("http://route.showapi.com/131-52", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("code", Code)
                    .addTextPara("time", Time)
                    .addTextPara("beginDay", BeginDay)
                    .post();
                }

                jObject = JsonConvert.DeserializeObject<JObject>(res);
                if (jObject["showapi_res_body"]["ret_code"].ToString() != "0")
                    return ApiResponse.NotFound("未找到数据！");
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return ApiResponse.BadRequest("糟糕，网络好像出问题了");
            }
            return ApiResponse.Ok(jObject);
        }
        #endregion

        #region 大盘股指实时分时线[参数为股指代码、时间（天）]
        [HttpGet]
        public HttpResponseMessage GetRealtimeTimeShareMarketIndexLine(string Code, string Day)
        {
            JObject jObject = null;
            string res = null;
            try
            {
                if (Day == null)
                {
                    res = new ShowApiRequest("http://route.showapi.com/131-51", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("code", Code)
                    .post();
                }
                if (Day != null)
                {
                    res = new ShowApiRequest("http://route.showapi.com/131-51", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("code", Code)
                    .addTextPara("day", Day)
                    .post();
                }

                jObject = JsonConvert.DeserializeObject<JObject>(res);
                if (jObject["showapi_res_body"]["ret_code"].ToString() != "0")
                    return ApiResponse.NotFound("找不到此股指！");
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return ApiResponse.BadRequest("糟糕，网络好像出问题了");
            }
            return ApiResponse.Ok(jObject);
        }
        #endregion

        #region 大盘历史日线行情查询（支持沪深数百支大盘指数查询，以及3支港股大盘（恒生指数hsi，国企指数hscei,红筹指数hscci））
        [HttpGet]
        public HttpResponseMessage GetMarketDayLineQuotation(string Code, string Month)//Month格式为yyyyMM
        {
            JObject jObject = null;
            try
            {
                string res = new ShowApiRequest("http://route.showapi.com/131-56", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("code", Code)
                    .addTextPara("month", Month)
                    .post();
                jObject = JsonConvert.DeserializeObject<JObject>(res);
                if (jObject["showapi_res_code"].ToString() != "0")
                    return ApiResponse.BadRequest("对不起，您无法使用此功能！");
                if (jObject["showapi_res_body"]["ret_code"].ToString() != "0")
                    return ApiResponse.NotFound("不支持此股指！");

            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return ApiResponse.BadRequest("糟糕，网络好像出问题了");
            }
            return ApiResponse.Ok(jObject);
        }
        #endregion

        #region 查询股票列表（参数是股票交易市场缩写和页码，返回是一个市场的所有股票列表）[20200212考虑三种参数都传递的情况，如何容错返回结果]
        [HttpGet]
        public HttpResponseMessage GetStockList(string Code, string Name, string Market, int Page)
        {
            JObject jObject = null;
            string res = null;
            try
            {
                if (Code != null && Name == null && Market == null)
                {
                    res = new ShowApiRequest("http://route.showapi.com/131-53", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("code", Code)
                    .addTextPara("page", Page.ToString())
                    .post();
                }

                if (Code == null && Name != null && Market == null)
                {
                    res = new ShowApiRequest("http://route.showapi.com/131-53", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("name", Name)
                    .addTextPara("page", Page.ToString())
                    .post();
                }

                if (Code == null && Name == null && Market != null)
                {
                    res = new ShowApiRequest("http://route.showapi.com/131-53", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("market", Market)
                    .addTextPara("page", Page.ToString())
                    .post();
                }

                if (Code == null && Name != null && Market != null)
                {
                    res = new ShowApiRequest("http://route.showapi.com/131-53", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("market", Market)
                    .addTextPara("name", Name)
                    .addTextPara("page", Page.ToString())
                    .post();
                }

                if (Code != null && Name != null && Market != null)
                {
                    res = new ShowApiRequest("http://route.showapi.com/131-53", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("code", Code)
                    .addTextPara("page", Page.ToString())
                    .post();
                    jObject = JsonConvert.DeserializeObject<JObject>(res);
                    if (jObject["showapi_res_body"]["ret_code"].ToString() != "0" || jObject["showapi_res_body"]["contentlist"].Count() == 0 || jObject["showapi_res_body"]["allNum"].ToString() == "0")
                    {
                        res = new ShowApiRequest("http://route.showapi.com/131-53", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                            .addTextPara("name", Name)
                            .addTextPara("page", Page.ToString())
                            .post();
                        jObject = JsonConvert.DeserializeObject<JObject>(res);
                        if (jObject["showapi_res_body"]["ret_code"].ToString() != "0" || jObject["showapi_res_body"]["contentlist"].Count() == 0 || jObject["showapi_res_body"]["allNum"].ToString() == "0")
                        {
                            res = new ShowApiRequest("http://route.showapi.com/131-53", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                            .addTextPara("market", Market)
                            .addTextPara("page", Page.ToString())
                            .post();
                        }
                        else
                        {
                            return ApiResponse.Ok(jObject);
                        }
                    }
                    else
                    {
                        return ApiResponse.Ok(jObject);
                    }
                }
                jObject = JsonConvert.DeserializeObject<JObject>(res);
                if (jObject["showapi_res_code"].ToString() != "0")
                    return ApiResponse.BadRequest("对不起，您无法使用此功能！");
                if (jObject["showapi_res_body"]["ret_code"].ToString() != "0" || jObject["showapi_res_body"]["contentlist"].Count() == 0)
                    return ApiResponse.NotFound("找不到此股指！");
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return ApiResponse.BadRequest("糟糕，网络好像出问题了");
            }
            return ApiResponse.Ok(jObject);
        }
        #endregion

        #region 沪深股票板块列表（无参数）
        [HttpGet]
        public HttpResponseMessage GetHuShenPlateList()
        {
            JObject jObject = null;
            string res = new ShowApiRequest("http://route.showapi.com/131-58", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                .post();
            try
            {
                jObject = JsonConvert.DeserializeObject<JObject>(res);
                if (jObject["showapi_res_body"]["ret_code"].ToString() != "0")
                    return ApiResponse.NotFound("未找到数据");
               JArray jArray = JArray.Parse(jObject["showapi_res_body"]["list"][3]["childList"][11]["childList"].ToString());
                //for(int i=0;i<jArray.Count;i++)
                //{
                //    AShareIndustry temp = new AShareIndustry();
                //    temp.ParentId = 16;
                //    temp.Parent = db.ASharePlates.FirstOrDefault(s => s.Id == 16);
                //    temp.IndustryName = jArray[i]["name"].ToString();
                //    temp.IndustryCode = jArray[i]["code"].ToString();
                //    temp.ParentPlateName = "传播与文化产业";
                //    db.AShareIndustries.Add(temp);
                    
                //}
                //db.SaveChanges();
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                ApiResponse.BadRequest("糟糕，网络好像出问题了");
            }
            return ApiResponse.Ok(jObject);
        }
        #endregion

        #region 查询沪深股票板块中的股票列表（参数来源于沪深股票板块列表（无参数））
        [HttpGet]
        public HttpResponseMessage GetStockListFromHuShenPlate(string TypeId, int Page)
        {
            JObject jObject = null;
            try
            {
                string res = new ShowApiRequest("http://route.showapi.com/131-59", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("typeId", TypeId)
                    .addTextPara("page", Page.ToString())
                    .post();
                jObject = JsonConvert.DeserializeObject<JObject>(res);
                if (jObject["showapi_res_body"]["ret_code"].ToString() != "0" || jObject["showapi_res_body"]["pagebean"]["contentlist"].Count() == 0)
                    return ApiResponse.NotFound("未找到数据");
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                ApiResponse.BadRequest("糟糕，网络好像出问题了");
            }
            return ApiResponse.Ok(jObject);
        }
        #endregion

        #region 港股板块列表（无参数）
        [HttpGet]
        public HttpResponseMessage GetHKPlateList()
        {
            JObject jObject = null;
            try
            {
                string res = new ShowApiRequest("http://route.showapi.com/131-60", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .post();
                jObject = JsonConvert.DeserializeObject<JObject>(res);
                if (jObject["showapi_res_body"]["ret_code"].ToString() != "0")
                    return ApiResponse.NotFound("未找到数据");
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return ApiResponse.BadRequest("糟糕，网络好像出问题了");
            }
            return ApiResponse.Ok(jObject);
        }
        #endregion

        #region 科创版股票列表
        [HttpGet]
        public HttpResponseMessage GetKeChuangVersionStockList(int Page)
        {
            JObject jObject = null;
            try
            {
                string res = new ShowApiRequest("http://route.showapi.com/131-63", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                .addTextPara("page", Page.ToString())
                .post();
                jObject = JsonConvert.DeserializeObject<JObject>(res);
                //JArray jArray = JArray.Parse(jObject["showapi_res_body"]["data"].ToString());
                //for(int i=0;i<jArray.Count;i++)
                //{
                //    Stock temp = new Stock
                //    {
                //        StockName = jArray[i]["name"].ToString(),
                //        StockCode = jArray[i]["code"].ToString(),
                //        StockType = Enums.StockType.A股
                //    };
                //    if (jArray[i]["market"].ToString() == "sh")
                //        temp.StockExchangeName = Enums.StockExchange.上海证券交易所;
                //    if (jArray[i]["market"].ToString() == "sz")
                //        temp.StockExchangeName = Enums.StockExchange.深圳证券交易所;
                //    db.Stocks.Add(temp);
                //}
                //db.SaveChanges();
                if (jObject["showapi_res_body"]["ret_code"].ToString() != "0")
                    return ApiResponse.NotFound("未找到数据");
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return ApiResponse.BadRequest("糟糕，网络好像出问题了");
            }
            return ApiResponse.Ok(jObject);
        }
        #endregion

        #region 查询港股板块中的股票列表（参数来源于港股板块股票列表）
        [HttpGet]
        public HttpResponseMessage GetHKPlateStockList(string TypeId, int Page)
        {
            JObject jObject = null;
            try
            {
                string res = new ShowApiRequest("http://route.showapi.com/131-61", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("typeId", TypeId)
                    .addTextPara("page", Page.ToString())
                    .post();
                jObject = JsonConvert.DeserializeObject<JObject>(res);
                if (jObject["showapi_res_body"]["ret_code"].ToString() != "0" || jObject["showapi_res_body"]["pagebean"]["contentlist"].Count() == 0 || jObject["showapi_res_body"]["pagebean"]["allNum"].ToString() == "0")
                    return ApiResponse.NotFound("未找到数据");
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return ApiResponse.BadRequest("糟糕，网络好像出问题了");
            }
            return ApiResponse.Ok(jObject);
        }
        #endregion

        #region 大盘股指列表查询
        [HttpGet]
        public HttpResponseMessage GetMarketIndexList(string Name, string Market, int Page)
        {
            JObject jObject = null;
            string res = null;
            try
            {
                if (Name != null && Market == null)
                {
                    res = new ShowApiRequest("http://route.showapi.com/131-55", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("name", Name)
                    .addTextPara("page", Page.ToString())
                    .post();
                }
                if (Name == null && Market != null)
                {
                    res = new ShowApiRequest("http://route.showapi.com/131-55", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("market", Market)
                    .addTextPara("page", Page.ToString())
                    .post();
                }
                if (Name != null && Market != null)
                {
                    res = new ShowApiRequest("http://route.showapi.com/131-55", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("market", Market)
                    .addTextPara("name", Name)
                    .addTextPara("page", Page.ToString())
                    .post();
                }

                jObject = JsonConvert.DeserializeObject<JObject>(res);
                if (jObject["showapi_res_body"]["ret_code"].ToString() != "0" || jObject["showapi_res_body"]["contentlist"].Count() == 0 || jObject["showapi_res_body"]["allNum"].ToString() == "0")
                    return ApiResponse.NotFound("未找到数据");
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return ApiResponse.BadRequest("糟糕，网络好像出问题了");
            }
            return ApiResponse.Ok(jObject);
        }
        #endregion

        #region 当日除权|停复牌|上市股票
        [HttpGet]
        public HttpResponseMessage GetTodayStockNews(string Date)
        {
            JObject jObject = null;
            try
            {
                string res = new ShowApiRequest("http://route.showapi.com/131-57", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                .addTextPara("date", Date)
                .post();
                jObject = JsonConvert.DeserializeObject<JObject>(res);
                if (jObject["showapi_res_body"]["ret_code"].ToString() != "0")
                    return ApiResponse.NotFound("未找到数据");
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return ApiResponse.BadRequest("糟糕，网络好像出问题了");
            }
            return ApiResponse.Ok(jObject);
        }
        #endregion

        #region 单只股票实时行情--!!!弃用!!!--（移步->股票实时行情数据获取-批量）
        [HttpGet]
        public HttpResponseMessage GetRealtimeSingleStockQuotation(string CodeorName)
        {
            JObject jObject = null;
            ParamHelper paramHelper = new ParamHelper();
            string Code = CodeorName;
            if (paramHelper.HaveHanZi(Code) || paramHelper.HaveEnglish(Code))
            {
                Stock tempStock = db.Stocks.FirstOrDefault(s => s.StockName == Code);
                if (tempStock == null)
                    return ApiResponse.NotFound("未找到数据，请输入股票代码试试！");
                Code = tempStock.StockCode;
            }
            try
            {
                String res = new ShowApiRequest("http://route.showapi.com/131-44", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
      .addTextPara("code", "600887")
      .addTextPara("need_k_pic", "0")
      .addTextPara("needIndex", "0")
      .post();
                jObject = JsonConvert.DeserializeObject<JObject>(res);
                if (jObject["showapi_res_body"]["ret_code"].ToString() != "0" || jObject["showapi_res_body"]["stockMarket"] == null)
                {
                    return ApiResponse.NotFound("未找到数据");
                }
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return ApiResponse.BadRequest("糟糕，网络好像出问题了");
            }
            return ApiResponse.Ok(jObject);
        }
        #endregion

        #region 名称|编码|拼音查询股票信息
        #region 获取数据
        [HttpGet]
        public HttpResponseMessage GetStockInfoList(string NameorCodeorPinyin)
        {
            ParamHelper paramHelper = new ParamHelper();
            string one = NameorCodeorPinyin;
            string res;
            JObject jObject = null;
            try
            {
                if (paramHelper.HaveHanZi(one))
                {
                    res = new ShowApiRequest("http://route.showapi.com/131-43", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                        .addTextPara("name", one)
                        .post();
                }
                else
                {
                    if (paramHelper.HaveNumber(one))
                    {
                        res = new ShowApiRequest("http://route.showapi.com/131-43", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                            .addTextPara("code", one)
                            .post();
                    }
                    else
                    {
                        res = new ShowApiRequest("http://route.showapi.com/131-43", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                            .addTextPara("pinyin", one)
                            .post();
                    }
                }
                jObject = JsonConvert.DeserializeObject<JObject>(res);
                StockInfo.Info info = JsonConvert.DeserializeObject<StockInfo.Info>(res);
                if (info.showapi_res_body.list.Count == 0)
                    return ApiResponse.NotFound("您输入有误，请重新输入！");
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return ApiResponse.BadRequest("糟糕，网络好像出问题了");
            }
            return ApiResponse.Ok(jObject);
        }
        #endregion

        #region 解析上面（名称|编码|拼音查询股票信息）接收的json
        protected static class StockInfo
        {
            public class Info
            {
                public int showapi_res_code { get; set; }
                public string showapi_res_error { get; set; }
                public showapi_res_body showapi_res_body { get; set; }
            }

            public class showapi_res_body
            {
                public int ret_code { get; set; }
                public List<Item> list { get; set; }
            }

            public class Item
            {
                public string market { get; set; }
                public string name { get; set; }
                public string currcapital { get; set; }
                public string profit_four { get; set; }
                public string code { get; set; }
                public string totalcapital { get; set; }
                public string mgjzc { get; set; }
                public string pinyin { get; set; }
            }
        }

        #endregion
        #endregion

        #region 沪深A股龙虎榜
        [HttpGet]
        public HttpResponseMessage GetASharesChart(string SortField, int SortType, int Page)
        {
            JObject jObject = null;
            try
            {
                string res = new ShowApiRequest("http://route.showapi.com/131-64", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("sortFeild", SortField)
                    .addTextPara("sortType", SortType.ToString())
                    .addTextPara("page", Page.ToString())
                    .post();
                jObject = JsonConvert.DeserializeObject<JObject>(res);
                if (jObject["showapi_res_body"]["ret_code"].ToString() != "0")
                {
                    return ApiResponse.NotFound("未找到数据");
                }
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return ApiResponse.BadRequest("糟糕，网络好像出问题了");
            }
            return ApiResponse.Ok(jObject);
        }
        #endregion

        #region 并发测试
        [HttpPost]
        public IActionResult PostAdd(long num)
        {
            num += 1;
            return Json(new 
            {
                num
            });
        }
        #endregion


    }
}