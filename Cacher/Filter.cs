using Fiddler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cacher
{
    partial class Program
    {
        private static string[] proxyHosts;
        private static string[] proxySuffixes;
        private static string[] decryptHosts;
        private static string[] decryptSuffixes;

        private static string[] exts;

        private static void ReadHosts()
        {
            FillHosts(Properties.Settings.Default.proxyHosts, ref proxyHosts, ref proxySuffixes);
            FillHosts(Properties.Settings.Default.decryptHosts, ref decryptHosts, ref decryptSuffixes);
            exts = Properties.Settings.Default.caches.Split('|');
        }

        private static bool NeedProxy(string hostname)
        {
            return IsTest(hostname, proxyHosts, proxySuffixes);
        }

        private static bool NeedDecrypt(string hostname)
        {
            return IsTest(hostname, decryptHosts, decryptSuffixes);
        }

        private static bool IsTest(string hostname, string[] hosts, string[] suffixes)
        {
            if (hosts != null)
            {
                for (var i = 0; i < hosts.Length; i++)
                {
                    if (hostname.OICEquals(hosts[i]))
                    {
                        return true;
                    }
                }
            }
            if (suffixes == null || suffixes.Length == 0)
            {
                return true;
            }
            return hostname.OICEndsWithAny(suffixes);
        }

        private static void FillHosts(string s, ref string[] hosts, ref string[] suffixes)
        {
            var lstHosts = new List<string>();
            var lstSuffixes = new List<string>();

            foreach (var item in s.Split('|'))
            {
                if (string.IsNullOrEmpty(item))
                {
                    continue;
                }
                if (item.StartsWith("."))
                {
                    lstSuffixes.Add(item);
                }
                else
                {
                    lstHosts.Add(item);
                }
            }

            hosts = lstHosts.ToArray();
            suffixes = lstSuffixes.ToArray();
        }

        private static void Log(string str)
        {
            Console.Write("[{0:HH:mm:ss}] - {1}", DateTime.Now, str);
        }

        private static void LogLine(string str)
        {
            Log(str);
            Console.WriteLine();
        }

        private static void Log(string format, object arg)
        {
            Console.Write("[{0:HH:mm:ss}] - {1}", DateTime.Now, string.Format(format, arg));
        }

        private static void LogLine(string format, object arg)
        {
            Log(format, arg);
            Console.WriteLine();
        }

        private static void Log(string format, params object[] args)
        {
            Console.Write("[{0:HH:mm:ss}] - {1}", DateTime.Now, string.Format(format, args));
        }

        private static void LogLine(string format, params object[] args)
        {
            Log(format, args);
            Console.WriteLine();
        }
    }
}
