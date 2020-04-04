using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AIService.Helper.BaiduAPIHelper;
using AIService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIService.Controllers
{
    [Route("api/ai/[action]")]
    [ApiController]
    public class AIController : Controller
    {
        #region 数据库连接
        private readonly DbEntity db = new DbEntity();
        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
        #endregion

        #region 获取答案
        public IActionResult GetAnswer(string text1,string text2)
        {
            double result = BaiduPlatform.GetSimilarity(text1, text2);
            return Json(result);
            
        }
        #endregion

    }
}