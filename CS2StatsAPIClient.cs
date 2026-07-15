using Newtonsoft.Json.Linq;
using System.IO;

namespace CS2Stats
{

    public class CS2StatsAPIClient
    {
        private readonly string authKey;
        private readonly string baseURL;

        public CS2StatsAPIClient(string authKey, string baseURL)
        {
            this.authKey = authKey;
            this.baseURL = baseURL;
        }



    }
}