using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AIService.Helper;
using AIService.Helper.StockApiHelper.show.api;
using AIService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;

namespace AIService.Controllers
{
    [Route("api/customer/[action]")]
    [ApiController]
    public class CustomerController : Controller
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

        #region 获取相似问题
        [HttpGet]
        public IActionResult GetSimilarQuestionList(string Question)
        {
            List<Knowledge> knowledges = db.Knowledges.ToList();
            List<SearchHistory> histories = db.SearchHistories.ToList();
            List<SearchHistory> repetitions = new List<SearchHistory>();
            SearchHistory searchHistory = null;
            for (int i = 0; i < histories.Count(); i++)
            {
                if (GetSimilarity(Question, histories[i].HistoricalText) > 0.85)
                    repetitions.Add(histories[i]);
            }

            if(repetitions.Count()==1)
            {                
                SearchHistory temp = repetitions.FirstOrDefault();
                temp.SearchTime = DateTime.Now;
                db.Entry(temp).State = EntityState.Modified;
                db.SaveChanges();

            }
            Dictionary<Knowledge, double> tempDatas = new Dictionary<Knowledge, double>();
            if (GetSimilarity("我转银行突然冻结了", Question) > 0.8 || GetSimilarity("我银行账户被冻结了", Question) > 0.8 || GetSimilarity("账户被冻结怎么办", Question) > 0.8 || GetSimilarity("账户被冻结", Question) > 0.8 || GetSimilarity("银转证转不了", Question) > 0.8)
                return Json(new
                {
                    code = 200,
                    data = knowledges.Where(s => s.Id == 3).Select(s => new
                    {
                        s.Id,
                        s.Question
                    })
                });
            for (int i = 0; i < knowledges.Count(); i++)
            {
                if (GetSimilarity(knowledges[i].Question, Question) > 0.4)
                {
                    tempDatas.Add(knowledges[i], GetSimilarity(knowledges[i].Question, Question));
                }
            }
            Dictionary<Knowledge, double> results = tempDatas.OrderByDescending(s => s.Value).ToDictionary(s => s.Key, s => s.Value);
            if (results.Count() > 0)
            {
                if (searchHistory != null)
                {
                    searchHistory.Answer = results.First().Key.Answer;
                    db.SearchHistories.Add(searchHistory);
                    db.SaveChanges();
                }
                if (results.First().Value > 0.85)
                {
                    return Json(new
                    {
                        code = 200,
                        data = results.Take(1).Select(s => new
                        {
                            s.Key.Id,
                            s.Key.Question
                        })
                    });
                }
                else if (results.Count() > 2)
                {
                    return Json(new
                    {
                        code = 200,
                        data = results.Take(3).Select(s => new
                        {
                            s.Key.Id,
                            s.Key.Question,
                        })
                    });
                }
                else if (results.Count() <= 2 && results.Count() > 0)
                {
                    return Json(new
                    {
                        code = 200,
                        data = results.Select(s => new
                        {
                            s.Key.Id,
                            s.Key.Question
                        })
                    });
                }
            }
            String res = new ShowApiRequest("http://route.showapi.com/60-27", "138438", "dd520f20268747d4bbda22ac31c9cbdf")
                         .addTextPara("info", Question)
                         .addTextPara("userid", "userid")
                         .post();
            JObject Tuling_result = JsonConvert.DeserializeObject<JObject>(res);
            try
            {
                if(Tuling_result["showapi_res_body"]["code"].ToString() == "200000")
                {
                    string resultAnswer = Tuling_result["showapi_res_body"]["text"].ToString() + ": " + Tuling_result["showapi_res_body"]["url"];
                    resultAnswer = resultAnswer.Contains("图灵机器人") ? resultAnswer.Replace("图灵机器人", "炒股达人阿财") : resultAnswer;
                    return Json(new 
                    {
                        code = 400,
                        data = resultAnswer
                    });
                }
                if (Tuling_result["showapi_res_body"]["code"].ToString() == "100000")
                {
                    return Json(new
                    {
                        code = 400,
                        data = Tuling_result["showapi_res_body"]["text"].ToString().Contains("图灵机器人") ? Tuling_result["showapi_res_body"]["text"].ToString().Replace("图灵机器人", "炒股达人阿财") : Tuling_result["showapi_res_body"]["text"].ToString()
                });
                }
            }
            catch(Exception ex)
            {
                return Json(new
                {
                    code = 400,
                    data = "糟糕，网路好像出问题了"
                });
            }
            return Json(new
            {
                code = 400,
                data = "阿财没听懂，不过阿财会继续升级知识库的!"
            });
        }
        #endregion

        #region 获取问题对应答案
        [HttpGet]
        public IActionResult GetAnswer(long KnowledgeId, long UserId)
        {
            Knowledge knowledge = db.Knowledges.FirstOrDefault(s => s.Id == KnowledgeId);
            try
            {
                if(db.SearchHistories.Where(s=>s.KnowledgeId==KnowledgeId).ToList().Count>0)
                {
                    SearchHistory history = db.SearchHistories.FirstOrDefault(s => s.KnowledgeId == KnowledgeId);
                    history.SearchTime = DateTime.Now;
                    db.Entry(history).State = EntityState.Modified;
                    db.SaveChanges();
                }
                else
                {
                    SearchHistory searchHistory = new SearchHistory()
                    {
                        KnowledgeId = KnowledgeId,
                        SearchTime = DateTime.Now,
                        HistoricalText = knowledge.Question,
                        Answer = knowledge.Answer,
                        UserId = UserId,
                        User = db.Users.FirstOrDefault(s => s.Id == UserId)
                    };
                    db.SearchHistories.Add(searchHistory);
                    db.SaveChanges();
                }
                
            }
            catch(Exception ex)
            {
                return Json(new 
                {
                    code = 400,
                    data = "网络出问题了"
                });
            }
            return Json(new 
            {
                knowledge.Question,
                knowledge.Answer
            });
            
        }
        #endregion

        #region 获取名词解释
        [HttpGet]
        public IActionResult GetExplain(string Word, long UserId)
        {
            IList<Dictionary> dictionaries = db.Dictionaries.ToList();
            IList<WordsHistory> wordsHistories = db.WordsHistories.ToList();
            WordsHistory wordsHistory = new WordsHistory();
            Dictionary<string, string> first_result = new Dictionary<string, string>();
            Dictionary<string, string> second_result = new Dictionary<string, string>();
            foreach (var item in dictionaries)
            {                
                if (GetSimilarity(item.Word, Word) > 0.85)
                {
                    first_result.Add(item.Word, item.Explain);
                    if(wordsHistories.Where(s=>s.Word==item.Word).Count()==0)
                    {
                        wordsHistory.UserId = UserId;
                        wordsHistory.Word = item.Word;
                        wordsHistory.Explain = item.Explain;
                        wordsHistory.SearchTime = DateTime.Now;
                        db.WordsHistories.Add(wordsHistory);
                        db.SaveChanges();
                    }                    
                    break;
                }
                if (GetSimilarity(item.Word, Word) > 0.6)
                {
                    second_result.Add(item.Word, item.Explain);
                    if (wordsHistories.Where(s => s.Word == item.Word).Count() == 0)
                    {
                        wordsHistory.UserId = UserId;
                        wordsHistory.Word = item.Word;
                        wordsHistory.Explain = item.Explain;
                        wordsHistory.SearchTime = DateTime.Now;
                        db.WordsHistories.Add(wordsHistory);
                        db.SaveChanges();
                    }                   
                }
            }

            if (first_result.Count > 0)
            {
                if (wordsHistories.Where(s => s.Word == first_result.First().Key).Count() > 0)
                {
                    wordsHistory = wordsHistories.Where(s => s.Word == first_result.First().Key).FirstOrDefault();
                    wordsHistory.SearchTime = DateTime.Now;
                    db.Entry(wordsHistory).State = EntityState.Modified;
                    db.SaveChanges();
                }
                    return Json(new
                {
                    code = 200,
                    Word = first_result.First().Key,
                    Explain = first_result.First().Value,
                });
            }
                
            if (second_result.Count > 0)
            {
                if (wordsHistories.Where(s => s.Word == second_result.First().Key).Count() > 0)
                {
                    wordsHistory = wordsHistories.Where(s => s.Word == second_result.First().Key).FirstOrDefault();
                    wordsHistory.SearchTime = DateTime.Now;
                    db.Entry(wordsHistory).State = EntityState.Modified;
                    db.SaveChanges();
                }
                return Json(new
                {
                    code = 200,
                    Word = second_result.First().Key,
                    Explain = second_result.First().Value,
                });
            }
                
            return Json(new
            {
                code = 400,
                data = "啥也没有"
            });

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

        #region 跳出用户可能会需要查找的名词解释
        [HttpGet]
        public IActionResult GetPossibleWords()
        {
            IList<Knowledge> knowledges = db.Knowledges.ToList();
            Dictionary<int, Knowledge> result = new Dictionary<int, Knowledge>();
            List<Knowledge> final_result = new List<Knowledge>();
            Random random = new Random();
            int[] number_result = new int[5];
            int count = 0;
            for(int i=1;i< knowledges.Count;i++)
            {
                result.Add(i, knowledges[i]);
            }
            Console.WriteLine(result.Count);
            while(number_result[4]==0)
            {
                int i = random.Next(1, result.Count-1);
                if (!number_result.Contains(i))
                {
                    number_result[count] = i;
                    count++;
                }
            }
            for(int i=0;i<number_result.Length;i++)
            {
                final_result.Add(result[number_result[i]]);
            }
            return Json(new 
            {
                code = 200,
                data = final_result.Select(s=>new { s.Id, s.Question, s.Answer})
            });
            
        }
        #endregion

        #region 名词解释历史
        [HttpGet]
        public IActionResult GetWordRecords(long UserId)
        {
            List<WordsHistory> wordsHistories = db.WordsHistories.OrderByDescending(s => s.SearchTime).Where(s=>s.UserId==UserId).ToList();
            List<Dictionary> dictionaries = db.Dictionaries.ToList();
            if (wordsHistories.Count() > 14)
                return Json(new
                {
                    datas = wordsHistories.Take(15).Select(s => new
                    {
                        s.Word,
                        s.Explain
                    })
                });
            return Json(new
            {
                data = wordsHistories.Select(s => new
                {
                    s.Word,
                    s.Explain
                })
            });
        }
        #endregion



    }
}