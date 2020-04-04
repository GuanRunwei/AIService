using AIService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIService.Helper
{
    public class ParamHelper
    {
        public bool HaveHanZi(String text)
        {
            char[] c = text.ToCharArray();
            bool res = false;
            for(int i=0; i<c.Length; i++)
            {
                if (c[i] >= 0x4E00 && c[i] <= 0x9FA5)
                {
                    res = true;
                    break;
                }
            }
            return res;
        }

        public bool HaveNumber(String text)
        {
            char[] c = text.ToCharArray();
            bool res = false;
            for(int i=0;i<c.Length;i++)
            {
                if((int)c[i]>=48&&(int)c[i]<=57)
                {
                    res = true;
                    break;
                }
            }
            return res;
        }

        public bool HaveEnglish(String text)
        {
            char[] c = text.ToCharArray();
            bool res = false;
            for (int i = 0; i < c.Length; i++)
            {
                if ((c[i] >= 'a' && c[i] <= 'z')||(c[i] >= 'A' && c[i] <= 'Z'))
                {
                    res = true;
                    break;
                }
            }
            return res;
        }

        public static string ConvertNumber(Double money)
        {
            if (money > 0)
            {
                if (money.ToString().Length >= 9)
                    return (Math.Round(money / 100000000, 2)).ToString() + "亿";
                if (money.ToString().Length > 4 && money.ToString().Length < 9)
                    return (Math.Round(money / 10000, 2)).ToString() + "万";
            }
            if (money < 0)
            {
                if (money.ToString().Substring(1).Length >= 9)
                    return "-" + (Math.Round(Math.Abs(money) / 100000000, 2)).ToString() + "亿";
                if (money.ToString().Substring(1).Length > 4 && money.ToString().Length < 9)
                    return "-" + (Math.Round(Math.Abs(money) / 10000, 2)).ToString() + "万";
            }
            return money.ToString();
        }


        public static string TalkTimeConvert(DateTime dateTime)
        {
            if (DateTime.Now.Subtract(dateTime).TotalSeconds <= 60)
                return DateTime.Now.Subtract(dateTime).Seconds + "秒前";
            if (DateTime.Now.Subtract(dateTime).TotalHours < 1)
                return DateTime.Now.Subtract(dateTime).Minutes + "分钟前";
            if (DateTime.Now.Date == dateTime.Date || DateTime.Now.Date.Equals(dateTime.Date)&& DateTime.Now.Subtract(dateTime).TotalHours >= 1)
                return "今天" + dateTime.ToString("HH:mm");
            if (DateTime.Now.Date.Subtract(dateTime.Date).Days == 1)
                return "昨天" + dateTime.ToString("HH:mm");
            if (DateTime.Now.Year != dateTime.Year)
                return dateTime.ToString("yyyy年MM月dd日 HH:mm");
            return dateTime.ToString("MM月dd日 HH:mm");
        }
    }
}
