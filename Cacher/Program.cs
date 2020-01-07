using Fiddler;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;
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
        private static string sProxy;

        static void Main(string[] args)
        {
            FiddlerApplication.Prefs.SetBoolPref("fiddler.certmaker.bc.Debug", true);

            FiddlerApplication.BeforeRequest += FiddlerApplication_BeforeRequest;
            FiddlerApplication.AfterSessionComplete += FiddlerApplication_AfterSessionComplete;

            var port = Properties.Settings.Default.listenPort;
            bool certLoaded;

            LoadCert();
            if (!InstallCert())
            {
                Console.WriteLine("failed to install cert.");
                certLoaded = false;
            }
            else
            {
                certLoaded = true;
            }

            ReadHosts();
            sProxy = Properties.Settings.Default.proxy;

            var builder = new FiddlerCoreStartupSettingsBuilder()
                .ListenOnPort(port)
                .AllowRemoteClients()
                .MonitorAllConnections();
            if (certLoaded)
            {
                builder.DecryptSSL();
            }

            FiddlerApplication.Startup(builder.Build());
            Console.WriteLine("start...");

            while (Console.ReadKey(true).Key != ConsoleKey.Escape)
            {
                ;
            }

            Stop();
        }

        private static void Stop()
        {
            FiddlerApplication.BeforeRequest -= FiddlerApplication_BeforeRequest;
            FiddlerApplication.AfterSessionComplete -= FiddlerApplication_AfterSessionComplete;

            if (FiddlerApplication.IsStarted())
            {
                FiddlerApplication.Shutdown();
            }
        }

        private static void LoadCert()
        {
            if (File.Exists("bc.crt") && File.Exists("bc.key"))
            {
                FiddlerApplication.Prefs.SetStringPref("fiddler.certmaker.bc.cert", File.ReadAllText("bc.crt"));
                FiddlerApplication.Prefs.SetStringPref("fiddler.certmaker.bc.key", File.ReadAllText("bc.key"));
            }
        }

        private static bool InstallCert()
        {
            if (!CertMaker.rootCertExists())
            {
                if (!CreateRootCert())
                {
                    return false;
                }

                //if (!CertMaker.trustRootCert())
                //{
                //    return false;
                //}

                File.WriteAllText("bc.crt", FiddlerApplication.Prefs.GetStringPref("fiddler.certmaker.bc.cert", null));
                File.WriteAllText("bc.key", FiddlerApplication.Prefs.GetStringPref("fiddler.certmaker.bc.key", null));
            }

            return true;
        }

        private static bool CreateRootCert()
        {
            const int ROOT_KEY_LENGTH = 2048;
            const string OU = "Tsanie Lily";
            const string O = "tsanie.org";
            const string CN = "Cacher";
            const int YEARS_VALID = 10;
            const string SIGNATURE_ALG = "SHA1withRSA";

            var generator = new RsaKeyPairGenerator();
            generator.Init(new KeyGenerationParameters(new SecureRandom(new CryptoApiRandomGenerator()), ROOT_KEY_LENGTH));
            var pair = generator.GenerateKeyPair();
            var generator2 = new X509V3CertificateGenerator();
            var serialNumber = BigInteger.ProbablePrime(120, new Random());
            generator2.SetSerialNumber(serialNumber);
            var issuer = new X509Name(string.Format("OU={0}, O={1}, CN={2}", OU, O, CN));
            generator2.SetIssuerDN(issuer);
            generator2.SetSubjectDN(issuer);
            generator2.SetNotBefore(DateTime.Today.AddDays(-7.0));
            generator2.SetNotAfter(DateTime.Now.AddYears(YEARS_VALID));
            generator2.SetPublicKey(pair.Public);
            generator2.SetSignatureAlgorithm(SIGNATURE_ALG);
            generator2.AddExtension(X509Extensions.BasicConstraints, true, new BasicConstraints(0));
            generator2.AddExtension(X509Extensions.KeyUsage, true, new KeyUsage(4));
            generator2.AddExtension(X509Extensions.SubjectKeyIdentifier, false, new SubjectKeyIdentifierStructure(pair.Public));
            var oCACert = generator2.Generate(pair.Private);
            var oCAKey = pair.Private;

            var encoded = oCACert.GetEncoded();
            FiddlerApplication.Prefs.SetStringPref("fiddler.certmaker.bc.cert", Convert.ToBase64String(encoded));
            var derEncoded = PrivateKeyInfoFactory.CreatePrivateKeyInfo(oCAKey).ToAsn1Object().GetDerEncoded();
            FiddlerApplication.Prefs.SetStringPref("fiddler.certmaker.bc.key", Convert.ToBase64String(derEncoded));

            return CertMaker.rootCertExists();
        }
    }
}
