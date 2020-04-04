using AIService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace AIService.Helper
{
    public class StockAgeCalculation
    {
        #region 数据库连接
        private static readonly DbEntity db = new DbEntity();
        #endregion

        public static string Age(long UserId)
        {
            User user = db.Users.FirstOrDefault(s => s.Id == UserId);
            int interval = DateTime.Today.Subtract(user.CreateTime.Date).Days;
            if (interval <= 30)
                return interval + "天";
            if(interval > 30 && interval < 365)
            {
                int flag = interval / 30;
                return flag.ToString() + "个月";
            }
            if(interval >= 365)
            {
                int flag = interval / 365;
                return flag.ToString() + "年";
            }
            return null;
        }
    }
}
