using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIService.Enums
{
    public enum StockType
    {
        A股=0,//内资股在境内上市
        B股=1,//外资股在境内上市，外资股在境外上市
        港股=2
    }
}
