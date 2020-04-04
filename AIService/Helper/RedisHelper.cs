using Microsoft.IdentityModel.Protocols;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace AIService.Helper
{
    public class RedisHelper
    {
        private ConnectionMultiplexer RedisClient = ConnectionMultiplexer.Connect("116.62.208.165:6379,password=Grw19980628");

        public ConnectionMultiplexer SafeConn
        {
            get
            {
                return RedisClient;
            }
        }

        public IDatabase Database
        {
            get
            {
                return SafeConn.GetDatabase();
            }
        }

        ~RedisHelper()
        {
            RedisClient.Close();
            RedisClient.Dispose();
        }
    }
}
