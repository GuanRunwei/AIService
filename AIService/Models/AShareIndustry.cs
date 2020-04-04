using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIService.Models
{
    public class AShareIndustry
    {
        #region 导航属性
        public virtual ASharePlate Parent { get; set; } 
        public int ParentId { get; set; }
        #endregion
        public int Id { get; set; }
        public string IndustryName { get; set; }
        public string IndustryCode { get; set; }
        public string ParentPlateName { get; set; }
    }
}
