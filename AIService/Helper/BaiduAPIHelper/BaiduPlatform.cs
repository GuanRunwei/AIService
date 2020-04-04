using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIService.Helper.BaiduAPIHelper
{
    public static class BaiduPlatform
    {
        public static Double GetSimilarity(String text1, String text2)
        {
            Double score;
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            var Client = new Baidu.Aip.Nlp.Nlp(BaiduAccount.API_KEY, BaiduAccount.SECRET_KEY);
            try
            {
                // 如果有可选参数
                var options = new Dictionary<string, object>
            {
                {"model", "GRNN"}
            };
                // 带参数调用短文本相似度
                var result = Client.Simnet(text1, text2, options);
                score = Double.Parse(result["score"].ToString());
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch(Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return -1;
            }
            return score;         
        }

        public static List<String> GetKeyWords(String Title, String Content)
        {
            List<String> keywords = new List<String>();
            try
            {
                if (Title.Length > 40)
                    Title = Title.Substring(0, 40);
                var Client = new Baidu.Aip.Nlp.Nlp(BaiduAccount.API_KEY, BaiduAccount.SECRET_KEY);
                var result = Client.Keyword(Title, Content);
                JArray jArray = JArray.Parse(result["items"].ToString());
                for(int i=0;i<jArray.Count;i++)
                {
                    keywords.Add(jArray[i]["tag"].ToString());
                }
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch(Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return null;
            }
            return keywords;
        }
    }
}
