using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AIService.Helper;
using AIService.Helper.StockApiHelper.show.api;
using AIService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AIService.Controllers
{
    [Route("api/account/[action]")]
    [ApiController]
    public class AccountController : Controller
    {
        #region 数据库连接
        private static readonly DbEntity db = new DbEntity();
        //protected override void Dispose(bool disposing)
        //{
        //    if (disposing) db.Dispose();
        //    base.Dispose(disposing);
        //}
        #endregion

        #region 加载Redis连接
        private readonly Lazy<RedisHelper> RedisHelper = new Lazy<RedisHelper>();
        #endregion

        #region 激活账户
        [HttpPost]
        public HttpResponseMessage ActivateAccount(long UserId)
        {
            StockAccount temp = db.StockAccounts.FirstOrDefault(s => s.Id == UserId);
            if (temp != null)
                return ApiResponse.BadRequest("您已创建过一个账户了");
            User user = db.Users.FirstOrDefault(s => s.Id == UserId);
            StockAccount stockAccount = new StockAccount()
            {
                User = user,
                UserId = UserId,
                ValidMoney = 200000,
                SumMoney = 200000
            };
            try
            {
                db.StockAccounts.Add(stockAccount);
                db.SaveChanges();
            }
            catch(Exception ex)
            {
                return ApiResponse.BadRequest("账户创建失败");
            }
            return ApiResponse.Ok("账户创建成功，赠送20w启动资金");
        }
        #endregion

        #region 购买股票
        [HttpPost]
        public HttpResponseMessage BuyStocks(long UserId, string NameorCode, int Number)
        {
            User user = db.Users.FirstOrDefault(s => s.Id == UserId);
            StockAccount stockAccount = db.StockAccounts.FirstOrDefault(s => s.UserId == UserId);
            SimulationStock simulationStock = null;
            JObject jObject = null;
            ParamHelper paramHelper = new ParamHelper();
            string StockCode = NameorCode;
            if (paramHelper.HaveEnglish(StockCode) || paramHelper.HaveHanZi(StockCode))
            {
                Stock tempStock = db.Stocks.FirstOrDefault(s => s.StockName == StockCode || s.StockName.Equals(StockCode));
                if (tempStock == null)
                    return ApiResponse.BadRequest("未找到数据");
                StockCode = tempStock.StockCode;
            }
            try
            {
                string res = new ShowApiRequest("http://route.showapi.com/131-46", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("stocks", StockCode)
                    .addTextPara("needIndex", "0")
                    .post();
                jObject = JsonConvert.DeserializeObject<JObject>(res);
                if (jObject["showapi_res_body"]["ret_code"].ToString() != "0" || jObject["showapi_res_body"]["list"].Count() == 0)
                    return ApiResponse.BadRequest("未找到数据");
                double nowPrice = Double.Parse(jObject["showapi_res_body"]["list"].First["nowPrice"].ToString());
                string stockName = jObject["showapi_res_body"]["list"].First["name"].ToString();
                if (Number * nowPrice > stockAccount.ValidMoney)
                    return ApiResponse.BadRequest("老铁，您的钱好像有点不够");
                else
                {
                    simulationStock = new SimulationStock()
                    {
                        StockAccount = stockAccount,
                        StockAccountId = stockAccount.Id,
                        StockCode = StockCode,
                        BuyPrice = nowPrice,
                        NowPrice = nowPrice,
                        StockNumber = Number,
                        StockName = stockName,
                        BuyTime = DateTime.Now,
                        Valid = true
                    };
                    stockAccount.ValidMoney -= Number * nowPrice;
                    stockAccount.SumStockValue = nowPrice * Number;
                    db.SimulationStocks.Add(simulationStock);
                    db.Entry(stockAccount).State = EntityState.Modified;
                    TradeHistory tradeHistory = new TradeHistory()
                    {
                        StockName = stockName,
                        StockCode = StockCode,
                        StockAccountId = stockAccount.Id,
                        TransactionValue = nowPrice * Number,
                        TransactionPrice = nowPrice,
                        TransactionAmount = Number,
                        TransactionType = Enums.TransactionType.买入,
                        TradeTime = DateTime.Now
                    };
                    db.TradeHistories.Add(tradeHistory);
                    db.SaveChanges();
                    Thread.Sleep(1);
                }
                Thread.Sleep(1);
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                db.Entry(stockAccount).State = EntityState.Unchanged;
                return ApiResponse.BadRequest("未找到数据");
            }
            return ApiResponse.Ok(new 
            {
                simulationStock.BuyPrice,
                simulationStock.StockName,
                simulationStock.StockCode,
                simulationStock.StockNumber,
                StockSum = simulationStock.BuyPrice * simulationStock.StockNumber,
                stockAccount.ValidMoney,
                message = "您买入 " + simulationStock.StockName + "(" + simulationStock.StockCode + ") " + Number + "股"
            });

        }
        #endregion

        #region 卖出股票
        [HttpPost]
        public HttpResponseMessage SellStocks(long UserId, long SimulationStockId, int SellNumber)
        {
            User user = db.Users.FirstOrDefault(s => s.Id == UserId);
            StockAccount stockAccount = db.StockAccounts.FirstOrDefault(s => s.UserId == UserId);
            Double Initial_ValidMoney = stockAccount.ValidMoney;
            SimulationStock simulationStock = db.SimulationStocks.FirstOrDefault(s => s.Id == SimulationStockId);
            if (SellNumber > simulationStock.StockNumber)
                return ApiResponse.BadRequest("超过你的持股数量了");
            else
            {
                SellStock sellStock = new SellStock();
                try
                {                    
                    List<SimulationStock> simulationStocks = db.SimulationStocks.Where(s => s.StockAccountId == stockAccount.Id && s.Valid == true).ToList();
                    if (simulationStocks.Count>0)
                    {

                        string[] stockCodes = simulationStocks.Select(s => s.StockCode).ToArray();
                        StringBuilder request_string = new StringBuilder();
                        for (int i = 0; i < stockCodes.Length; i++)
                        {
                            if (i == stockCodes.Length - 1)
                            {
                                request_string.Append(stockCodes[i]);
                            }
                            else
                            {
                                request_string.Append(stockCodes[i] + ",");
                            }
                        }
                        string res = new ShowApiRequest("http://route.showapi.com/131-46", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                            .addTextPara("stocks", request_string.ToString())
                            .addTextPara("needIndex", "0")
                            .post();
                        JObject jObject = JsonConvert.DeserializeObject<JObject>(res);
                        JArray jArray = JArray.Parse(jObject["showapi_res_body"]["list"].ToString());
                        for(int i=0;i<simulationStocks.Count;i++)
                        {
                            for (int j = 0; j < jArray.Count; j++)
                            {
                                if(simulationStocks[i].StockCode==jArray[j]["code"].ToString())
                                {
                                    simulationStocks[i].NowPrice = Double.Parse(jArray[j]["nowPrice"].ToString());
                                    db.Entry(simulationStocks[i]).State = EntityState.Modified;
                                    db.SaveChanges();
                                }
                            }
                            Thread.Sleep(1);
                        }
                        
                        sellStock.SellPrice = simulationStock.NowPrice;
                        sellStock.SellStockNumber = SellNumber;
                        sellStock.BuyPrice = simulationStock.BuyPrice;
                        sellStock.StockName = simulationStock.StockName;
                        sellStock.StockCode = simulationStock.StockCode;
                        sellStock.SellTime = DateTime.Now;
                        sellStock.StockAccountId = stockAccount.Id;
                        db.SellStocks.Add(sellStock);
                        if (SellNumber == simulationStock.StockNumber)
                        {
                            simulationStock.StockNumber = 0;
                            simulationStock.Valid = false;
                            db.Entry(simulationStock).State = EntityState.Modified;
                            db.SaveChanges();
                            Thread.Sleep(1);
                        }
                        else
                        {
                            simulationStock.StockNumber -= SellNumber;
                            db.Entry(simulationStock).State = EntityState.Modified;
                            db.SaveChanges();
                            Thread.Sleep(1);
                        }
                        try
                        {
                            double StockValue = 0;
                            List<SimulationStock> last = db.SimulationStocks.Where(s => s.StockAccountId == stockAccount.Id && s.Valid == true).ToList();
                            foreach (var item in last)
                            {
                                StockValue += item.NowPrice * item.StockNumber;
                            }
                            StockAccount stockAccount2 = db.StockAccounts.FirstOrDefault(s => s.UserId == UserId);
                            stockAccount2.SumStockValue = StockValue;
                            db.Entry(stockAccount2).State = EntityState.Modified;
                            db.SaveChanges();
                            Thread.Sleep(1);
                            StockAccount stockAccount3 = db.StockAccounts.FirstOrDefault(s => s.UserId == UserId);
                            stockAccount3.Profit_or_Loss += (sellStock.SellPrice - sellStock.BuyPrice) * SellNumber;
                            
                            stockAccount3.ValidMoney += sellStock.SellPrice * SellNumber;
                            stockAccount3.SumMoney = Initial_ValidMoney + sellStock.SellPrice * SellNumber;
                            db.Entry(stockAccount3).State = EntityState.Modified;
                            Thread.Sleep(1);
                            TradeHistory tradeHistory = new TradeHistory()
                            {
                                StockName = simulationStock.StockName,
                                StockCode = simulationStock.StockCode,
                                StockAccountId = stockAccount.Id,
                                TransactionValue = sellStock.SellPrice * SellNumber,
                                TransactionPrice = sellStock.SellPrice,
                                TransactionAmount = SellNumber,
                                TransactionType = Enums.TransactionType.卖出,
                                TradeTime = DateTime.Now
                            };
                            db.TradeHistories.Add(tradeHistory);
                            db.SaveChanges();
                        }
                        catch(Exception ex)
                        {
                            db.Entry(simulationStock).State = EntityState.Unchanged;
                            db.Entry(stockAccount).State = EntityState.Unchanged;
                        }
                        
                    }
                }
                catch(Exception ex)
                {
                    return ApiResponse.BadRequest("糟糕，网络好像出问题了");
                }
                return ApiResponse.Ok(new 
                {
                    sellStock.SellPrice,
                    sellStock.SellStockNumber,
                    sellStock.SellTime,
                    sellStock.StockCode,
                    sellStock.StockName,
                    sellStock.BuyPrice,
                    message = "您卖出 "+sellStock.StockName+"("+sellStock.StockCode+") "+ sellStock.SellStockNumber+"股， "+"收益为￥"+(sellStock.SellPrice-sellStock.BuyPrice)*sellStock.SellStockNumber,
                });
            }
            
        }
        #endregion

        #region 股票个人主页
        [HttpGet]
        public HttpResponseMessage GetStockMainPage(long UserId)
        {

            User user = db.Users.FirstOrDefault(s => s.Id == UserId);
            StockAccount stockAccount = db.StockAccounts.FirstOrDefault(s => s.UserId == UserId);
            JArray stockList = null;
            if (stockAccount == null)
                return ApiResponse.BadRequest("建议您先开个户");
            else
            {
                List<SimulationStock> simulationStocks = db.SimulationStocks.Where(s => s.StockAccountId == stockAccount.Id && s.Valid==true).ToList();
                if (simulationStocks.Count > 0)
                {
                    string[] stockCodes = simulationStocks.Select(s => s.StockCode).ToArray();
                    StringBuilder request_string = new StringBuilder();
                    for (int i = 0; i < stockCodes.Length; i++)
                    {
                        if (i == stockCodes.Length - 1)
                        {
                            request_string.Append(stockCodes[i]);
                        }
                        else
                        {
                            request_string.Append(stockCodes[i] + ",");
                        }
                    }
                    string res = new ShowApiRequest("http://route.showapi.com/131-46", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                        .addTextPara("stocks", request_string.ToString())
                        .addTextPara("needIndex", "0")
                        .post();
                    JObject jObject = JsonConvert.DeserializeObject<JObject>(res);
                    stockList = JArray.Parse(jObject["showapi_res_body"]["list"].ToString());
                    Double SumStockValue = 0;
                    for(int i=0;i<simulationStocks.Count;i++)
                    {
                        for (int j = 0; j < stockList.Count; j++)
                        {
                            if(simulationStocks[i].StockCode==stockList[j]["code"].ToString())
                            {
                                simulationStocks[i].NowPrice = Double.Parse(stockList[j]["nowPrice"].ToString());
                                SumStockValue += Double.Parse(stockList[j]["nowPrice"].ToString()) * simulationStocks[i].StockNumber;
                                db.Entry(simulationStocks[i]).State = EntityState.Modified;
                                db.SaveChanges();                                
                            }
                            
                        }
                        Thread.Sleep(1);
                    }
                    stockAccount.SumStockValue = SumStockValue;
                    stockAccount.SumMoney = SumStockValue + stockAccount.ValidMoney;
                    db.Entry(stockAccount).State = EntityState.Modified;
                    db.SaveChanges();
                    Thread.Sleep(1);
                    try
                    {
                        stockAccount.Rank = RankCalculation(UserId: UserId);
                        db.Entry(stockAccount).State = EntityState.Modified;
                        db.SaveChanges();
                        Thread.Sleep(1);
                    }
                    catch (Exception ex)
                    {

                        Console.WriteLine(ex.Message);
                        db.Entry(stockAccount).State = EntityState.Unchanged;
                        return ApiResponse.BadRequest("糟糕，网络好像出问题了");
                    }
                }
                else
                {
                    try
                    {
                        stockAccount.Rank = RankCalculation(UserId: UserId);
                        db.Entry(stockAccount).State = EntityState.Modified;
                        db.SaveChanges();
                        Thread.Sleep(1);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        db.Entry(stockAccount).State = EntityState.Unchanged;
                        return ApiResponse.BadRequest("糟糕，网络好像出问题了");
                    }
                    
                }
                Thread.Sleep(1);

            }
            return ApiResponse.Ok(new 
            {
                stockAccount.UserId,
                stockAccount.SumMoney,
                SumStockValue = ParamHelper.ConvertNumber(stockAccount.SumStockValue),
                ValidMoney = ParamHelper.ConvertNumber(stockAccount.ValidMoney),
                Profit_or_Loss = Math.Round(stockAccount.Profit_or_Loss, 2),
                stockAccount.Rank
            });
        }
        #endregion

        #region 持仓列表
        [HttpGet]
        public IActionResult GetPositionList(long UserId)
        {           
            StockAccount stockAccount = db.StockAccounts.FirstOrDefault(s => s.UserId == UserId);
            if (stockAccount == null)
                return Json(new
                {
                    code = 400,
                    message = "您还未激活账户，请先激活"
                });
            List<SimulationStock> simulationStocks = db.SimulationStocks.Where(s => s.StockAccountId == stockAccount.Id && s.Valid == true).ToList();
            double Today_Profit_or_Loss = 0;
            
            if (simulationStocks.Count == 0)
                return Json(new 
                {
                    code = 200,
                    stockAccount.SumMoney,
                    stockAccount.ValidMoney,
                    stockAccount.SumStockValue,
                    Today_Profit_or_Loss = Math.Round(Today_Profit_or_Loss, 2),
                    data = new string[] { }
                });
            else
            {
                string[] stockCodes = simulationStocks.Select(s => s.StockCode).ToArray();
                StringBuilder request_string = new StringBuilder();
                for (int i = 0; i < stockCodes.Length; i++)
                {
                    if (i == stockCodes.Length - 1)
                    {
                        request_string.Append(stockCodes[i]);
                    }
                    else
                    {
                        request_string.Append(stockCodes[i] + ",");
                    }
                }
                string res = new ShowApiRequest("http://route.showapi.com/131-46", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("stocks", request_string.ToString())
                    .addTextPara("needIndex", "0")
                    .post();
                JObject jObject = JsonConvert.DeserializeObject<JObject>(res);
                JArray stockList = JArray.Parse(jObject["showapi_res_body"]["list"].ToString());
                Double StockValue = 0;
                for(int i =0;i<simulationStocks.Count;i++)
                {
                    for(int j=0;j<stockList.Count;j++)
                    {
                        if(simulationStocks[i].StockCode==stockList[j]["code"].ToString())
                        {
                            simulationStocks[i].NowPrice = Double.Parse(stockList[j]["nowPrice"].ToString());
                            StockValue += Double.Parse(stockList[j]["nowPrice"].ToString()) * simulationStocks[i].StockNumber;
                            Today_Profit_or_Loss += (simulationStocks[i].NowPrice - simulationStocks[i].BuyPrice) * simulationStocks[i].StockNumber;
                            db.Entry(simulationStocks[i]).State = EntityState.Modified;
                            db.SaveChanges();
                            
                        }
                    }
                }
                
                Thread.Sleep(1);
                try
                {
                    stockAccount.SumMoney = StockValue + stockAccount.ValidMoney;                   
                    stockAccount.SumStockValue = StockValue;
                    stockAccount.Profit_or_Loss = Today_Profit_or_Loss;
                    stockAccount.Rank = RankCalculation(UserId: UserId);
                    db.Entry(stockAccount).State = EntityState.Modified;
                    db.SaveChanges();
                    Thread.Sleep(1);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    db.Entry(stockAccount).State = EntityState.Unchanged;
                    return Json(new 
                    {
                        code = 400,
                        data = "糟糕，网络好像出问题了"
                    });
                }
            }
            return Json(new
            {
                code = 200,
                SumMoney = Math.Round(stockAccount.SumMoney, 2),
                ValidMoney = Math.Round(stockAccount.ValidMoney, 2),
                SumStockValue = Math.Round(stockAccount.SumStockValue, 2),
                Today_Profit_or_Loss = Math.Round(Today_Profit_or_Loss, 2),
                data = simulationStocks.Select(s=>new 
                {
                    s.Id,
                    s.StockName,
                    s.StockCode,                   
                    Profit_or_Loss_Value = Math.Round(s.NowPrice-s.BuyPrice, 2).ToString(),
                    Profit_or_Loss_Ratio = Math.Round((s.NowPrice - s.BuyPrice)/s.BuyPrice, 2).ToString() + "%",
                    s.StockNumber,
                    s.NowPrice
                })
            });
        }
        #endregion

        #region 模拟炒股中买入一栏查询股票返回数据
        [HttpGet]
        public IActionResult GetBuyStockInfo(string NameorCode)
        {
            JObject jObject = null;
            JObject result = null;
            Double nowPrice = 0;
            Double diff_rate = 0;
            Double diff_money = 0;
            string name = "";
            string code = "";
            ParamHelper paramHelper = new ParamHelper();
            string StockCode = NameorCode;
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
                nowPrice = Double.Parse(result["nowPrice"].ToString());
                diff_rate = Double.Parse(result["diff_rate"].ToString());
                diff_money = Double.Parse(result["diff_money"].ToString());
                code = result["code"].ToString();
                name = result["name"].ToString();

                if (jObject["showapi_res_body"]["ret_code"].ToString() != "0" || jObject["showapi_res_body"]["list"].Count() == 0)
                    return Json(new
                    {
                        code = 404,
                        message = "未找到数据"
                    });
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
                name,
                code,
                nowPrice,
                diff_rate = diff_rate + "%",
                diff_money
            });
        }
        #endregion

        #region 模拟炒股中卖出一栏点击列表中的某只股票的细节
        [HttpGet]
        public IActionResult GetPositionDetail(long SimulationStockId)
        {
            SimulationStock simulationStock = db.SimulationStocks.FirstOrDefault(s => s.Id == SimulationStockId);
            JObject jObject = null;
            Double nowPrice = 0;
            Double diff_rate = 0;
            Double diff_money = 0;

            try
            {
                string res = new ShowApiRequest("http://route.showapi.com/131-46", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                    .addTextPara("stocks", simulationStock.StockCode)
                    .addTextPara("needIndex", "0")
                    .post();
                jObject = JsonConvert.DeserializeObject<JObject>(res);
                JObject result = JObject.Parse(jObject["showapi_res_body"]["list"].First.ToString());
                nowPrice = Double.Parse(result["nowPrice"].ToString());
                diff_rate = Double.Parse(result["diff_rate"].ToString());
                diff_money = Double.Parse(result["diff_money"].ToString());
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
                simulationStock.StockName,
                simulationStock.StockCode,
                simulationStock.StockNumber,
                nowPrice,
                diff_rate = diff_rate + "%",
                diff_money
            });

            
        }
        #endregion

        #region 交易历史
        [HttpGet]
        public IActionResult GetTradeHistory(long UserId)
        {
            StockAccount stockAccount = db.StockAccounts.FirstOrDefault(s => s.UserId == UserId);
            if (stockAccount == null)
                return Json(new 
                {
                    code = 400,
                    message = "没有数据"
                });
            else
            {
                List<TradeHistory> tradeHistories = db.TradeHistories.Where(s => s.StockAccountId == stockAccount.Id).ToList();
                if(tradeHistories.Count==0)
                    return Json(new
                    {
                        code = 400,
                        message = "没有数据"
                    });
                else
                {
                    return Json(new 
                    {
                        code = 200,
                        data = tradeHistories.OrderByDescending(s=>s.TradeTime).Select(s=>new 
                        {
                            s.Id,
                            s.StockCode,
                            s.StockName,
                            TransactionType = s.TransactionType.ToString(),
                            s.TransactionValue,
                            s.TransactionPrice,
                            s.TransactionAmount,
                            TradeTime = ParamHelper.TalkTimeConvert(s.TradeTime)
                        })
                    });
                }
            }
        }
        #endregion

        #region 资产排行榜
        [HttpGet]
        public HttpResponseMessage GetAssetsRankingList()
        {
            var result = db.StockAccounts.Join(db.Users, s => s.UserId, u => u.Id, (s, u) => new { s.UserId, u.Username, u.ImageUrl, u.StockAge, s.SumMoney }).OrderByDescending(s=>s.SumMoney);
            //List<StockAccount> stockAccounts = db.StockAccounts.ToList();
            //List<User> users = db.Users.ToList();

            //var result = from stockAccount in stockAccounts
            //             join user in users 
            //             on stockAccount.UserId equals user.Id
            //             select new { UserId = user.Id, user.Username, user.ImageUrl, stockAccount.SumMoney };

            return ApiResponse.Ok(result);
        }
        #endregion


        #region Helper方法
        public static Double SumCalculation(long UserId)
        {
            var stockAccount = db.StockAccounts.FirstOrDefault(s => s.UserId == UserId);
            List<SimulationStock> simulationStocks = db.SimulationStocks.Where(s => s.StockAccountId == stockAccount.Id && s.Valid == true).ToList();
            if (simulationStocks.Count == 0)
            {
                return stockAccount.ValidMoney;
            }
            else
            {
                Double money = stockAccount.ValidMoney;
                foreach (var item in simulationStocks)
                {
                    money += item.NowPrice * item.StockNumber;
                }
                return money;
            }
        }

        public static Double SumStockValueCalculation(long UserId)
        {
            StockAccount stockAccount = db.StockAccounts.FirstOrDefault(s => s.UserId == UserId);
            List<SimulationStock> simulationStocks = db.SimulationStocks.Where(s => s.StockAccountId == stockAccount.Id && s.Valid == true).ToList();
            if (simulationStocks.Count == 0)
            {
                return 0;
            }

            else
            {
                Double SumStockValue = 0;
                foreach (SimulationStock item in simulationStocks)
                {
                    SumStockValue += item.NowPrice * item.StockNumber;
                }
                return SumStockValue;
            }
        }

        public static Double ProfitandLossCalculation(long UserId)
        {
            StockAccount stockAccount = db.StockAccounts.FirstOrDefault(s => s.UserId == UserId);
            List<SimulationStock> simulationStocks = db.SimulationStocks.Where(s => s.StockAccountId == stockAccount.Id && s.Valid == true).ToList();
            if (simulationStocks.Count == 0)
            {
                return 0;
            }

            else
            {
                Double SumStockValue = 0;
                Double BuyStockValue = 0;
                foreach (SimulationStock item in simulationStocks)
                {
                    SumStockValue += item.NowPrice * item.StockNumber;
                    BuyStockValue += item.BuyPrice * item.StockNumber;
                }
                return stockAccount.Profit_or_Loss + SumStockValue - BuyStockValue;
            }
        }

        public static long RankCalculation(long UserId)
        {
            List<StockAccount> stockAccounts = db.StockAccounts.OrderByDescending(s => s.SumMoney).ToList();
            for (int i = 0; i < stockAccounts.Count; i++)
            {
                if (stockAccounts[i].UserId == UserId)
                {
                    return i + 1;
                }

            }
            return -1;
        }
        #endregion

    }
}