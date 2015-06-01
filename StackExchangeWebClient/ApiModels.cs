using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackExchange.Frontend
{
    // web
    public class Flag
    {
        public string Url { get; set; }
        public string Title { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime FlagDate { get; set; }
    }

    // stack api

    public class network_user
    {
        public string site_url { get; set; }
        public int user_id { get; set; }
    }

    public class Wrapper
    {
    
        public List<network_user> items { get; set; }

        public bool has_more { get; set; }
        public int quota_max { get; set; }
        public int quota_remaining { get; set; }
        public int backoff { get; set; }
    }
}
