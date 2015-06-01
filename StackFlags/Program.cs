using StackExchange.Frontend;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using net = System.Net;

namespace StackFlags
{
    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            if (args.Length == 0)
            {
                // three lines, username, password, asccountid
                args = File.ReadAllLines(@"userinfo.user");
            }
#endif
            const int username = 0;
            const int password = 1;
            const int accountid = 2;

            // ONLY WORKS WITH AN STACK EXCHANGE ID!
            var client = new ClientStack(args[username], args[password]);
            client.Login();

            // collect all flags
            var flags = new List<Flag>();

            // ask the api to give all associated accounts
            foreach (var user in client.GetAssociatedAccounts(Int32.Parse(args[accountid])))
            {   
                // login per site
                client.LoginSite(user.site_url);

                // visit the first page of flags
                flags.AddRange(client.GetFlags(user));
            }

            // order all flags, most recent first
            var recentFlags = from flag in flags
                              orderby flag.FlagDate descending
                              select flag;

            // output to the console
            foreach(var recentFlag in recentFlags)
            {
                Console.WriteLine("\"{0}\",\"{1}\",\"{2}\"",recentFlag.FlagDate, recentFlag.CreationDate, recentFlag.Url);
            }
        }
    }
}
