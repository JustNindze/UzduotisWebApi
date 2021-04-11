using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UzduotisWebApi
{
    public class CreateRequest
    {
        public string Key { get; set; }
        public List<object> Value { get; set; }
        public int? ExpirationPeriod { get; set; }
    }

    public class AppendRequest
    {
        public string Key { get; set; }
        public List<object> Value { get; set; }
    }

    public class DeleteRequest
    {
        public string Key { get; set; }
    }
}
