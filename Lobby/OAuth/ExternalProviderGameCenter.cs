using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Lobby.OAuth
{
    public class ExternalProviderGameCenter
    {
        public async Task<bool> CheckAuth(string platformId, GameCenterAuth auth)
        {
            string resultString = string.Empty;

            try
            {
                var timestamp = ulong.Parse(auth.Timestamp);
                var cert = await GetCertificate(auth.PublicKeyUrl);
                if (cert.Verify())
                {
                    var csp = cert.PublicKey.Key as RSACryptoServiceProvider;
                    if (csp != null)
                    {
                        var sha1 = new SHA256Managed();
                        var sig = ConcatSignature(platformId, auth.BundleId, timestamp, auth.Salt);
                        var hash = sha1.ComputeHash(sig);

                        if (csp.VerifyHash(hash, CryptoConfig.MapNameToOID("SHA256"), Convert.FromBase64String(auth.Signature)))
                        {
                            // Valid user.
                            // Do server related user management stuff.
                            //Logger.Info($"GameCenter TokenCheck success, usn:{venderUID}, {auth.ToString()}");
                            return true;
                        }
                        else
                        {
                            resultString = "public key verify error";
                        }
                    }
                    else
                    {
                        resultString = "key type error, public key is not RSA";
                    }
                }
                else
                {
                    resultString = "certificate verify error";
                }

                // Failure                
                return false;
            }
            catch (Exception exception)
            {
                resultString = exception.ToString();
            }

            Log.Error($"GameCenter TokenCheck failed, cause:{resultString}, usn:{platformId} {auth.ToString()}");
            return false;
        }


        private static byte[] ToBigEndian(ulong value)
        {
            var buffer = new byte[8];
            for (int i = 0; i < 8; i++)
            {
                buffer[7 - i] = unchecked((byte)(value & 0xff));
                value = value >> 8;
            }
            return buffer;
        }

        private static async Task<X509Certificate2> GetCertificate(string url)
        {
            var client = new WebClient();
            var rawData = await client.DownloadDataTaskAsync(url);
            return new X509Certificate2(rawData);
        }

        private static byte[] ConcatSignature(string playerId, string bundleId, ulong timestamp, string salt)
        {
            var data = new List<byte>();
            data.AddRange(Encoding.UTF8.GetBytes(playerId));
            data.AddRange(Encoding.UTF8.GetBytes(bundleId));
            data.AddRange(ToBigEndian(timestamp));
            data.AddRange(Convert.FromBase64String(salt));
            return data.ToArray();
        }
    }
}
