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
        private static void FiddlerApplication_AfterSessionComplete(Session oSession)
        {
            if (oSession.Tag == fromCache)
            {
                return;
            }

            if (!oSession.HTTPMethodIs("GET") && !oSession.HTTPMethodIs("POST"))
            {
                return;
            }

            if (oSession.responseCode >= 400)
            {
#if WARNING
                LogLine("failed with code: {0} - {1}", oSession.responseCode, oSession.fullUrl);
#endif
                return;
            }

            if (!NeedDecrypt(oSession.hostname))
            {
                return;
            }

            var file = oSession.PathAndQuery.Substring(1);
            int index = file.IndexOf('?');
            if (index > 0)
            {
                file = file.Substring(0, index);
            }

            bool cache = false;
            bool noExts = exts == null || exts.Length == 0;
            bool noMatches = matches == null || matches.Length == 0;
            if (noExts && noMatches)
            {
                cache = true;
            }
            else if (!noExts && file.OICEndsWithAny(exts))
            {
                cache = true;
            }
            else if (!noMatches && matches.Any(m => file.OICContains(m)))
            {
                cache = true;
            }
            if (!cache)
            {
                return;
            }

            index = file.LastIndexOf('/');
            if (index > 0)
            {
                var path = file.Substring(0, index);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }

            file += ".cache";
            oSession.SaveResponse(Path.Combine(Environment.CurrentDirectory, file), false);

            var gmTime = oSession.oResponse["Last-Modified"];
            if (!string.IsNullOrEmpty(gmTime))
            {
                var fi = new FileInfo(file);
                var dt = GMT2Local(gmTime);
                if (dt.Year > 1900)
                {
                    fi.LastWriteTime = dt;
                }
            }

            LogLine("{0}...[saved]", file);
        }
    }
}
