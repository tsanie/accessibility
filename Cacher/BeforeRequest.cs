using Fiddler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cacher
{
    partial class Program
    {
        private static readonly object fromCache = new object();

        private static void FiddlerApplication_BeforeRequest(Session oSession)
        {
            if (sProxy != null && oSession.hostname != null && NeedProxy(oSession.hostname))
            {
                oSession["X-OverrideGateway"] = sProxy;
            }

            if (!NeedDecrypt(oSession.hostname))
            {
                if (oSession.HTTPMethodIs("CONNECT"))
                {
                    oSession["x-no-decrypt"] = "do not care.";
                }
                return;
            }

            if (!oSession.HTTPMethodIs("GET") && !oSession.HTTPMethodIs("POST"))
            {
                return;
            }

            var url = oSession.fullUrl;
            Log(url);

            var cache = oSession.PathAndQuery.Substring(1);
            int index = cache.IndexOf('?');
            if (index > 0)
            {
                cache = cache.Substring(0, index);
            }
            cache += ".cache";
            if (File.Exists(cache))
            {
                oSession.utilCreateResponseAndBypassServer();
                oSession.LoadResponseFromFile(Path.Combine(Environment.CurrentDirectory, cache));
                oSession.oResponse["Date"] = DateTime.Now.ToUniversalTime().ToString("r");
                oSession.Tag = fromCache;

                Console.WriteLine("...[replaced]");
            }
            else
            {
                Console.WriteLine();
            }
        }
    }
}
