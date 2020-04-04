using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIService.Models
{
    public class HKSharePlate
    {
        public int Id { get; set; }
        public string PlateName { get; set; }
        public string PlateCode { get; set; }
        public string ParentName { get; set; }
    }
}
