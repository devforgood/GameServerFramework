using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Lobby
{
    public class InAppPurchaseGoogle : InAppPurchase
    {
        private RSAParameters _rsaKeyInfo;

        public InAppPurchaseGoogle(String GooglePublicKey)
        {
            RsaKeyParameters rsaParameters = (RsaKeyParameters)PublicKeyFactory.CreateKey(Convert.FromBase64String(GooglePublicKey));

            byte[] rsaExp = rsaParameters.Exponent.ToByteArray();
            byte[] Modulus = rsaParameters.Modulus.ToByteArray();

            // Microsoft RSAParameters modulo wants leading zero's removed so create new array with leading zero's removed
            int Pos = 0;
            for (int i = 0; i < Modulus.Length; i++)
            {
                if (Modulus[i] == 0)
                {
                    Pos++;
                }
                else
                {
                    break;
                }
            }
            byte[] rsaMod = new byte[Modulus.Length - Pos];
            Array.Copy(Modulus, Pos, rsaMod, 0, Modulus.Length - Pos);

            // Fill the Microsoft parameters
            _rsaKeyInfo = new RSAParameters()
            {
                Exponent = rsaExp,
                Modulus = rsaMod
            };
        }

        bool VerifyData(string receipt, string signature)
        {
            try
            {
                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                {
                    rsa.ImportParameters(_rsaKeyInfo);
                    return rsa.VerifyData(Encoding.ASCII.GetBytes(receipt), "SHA1", Convert.FromBase64String(signature));
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

#pragma warning disable 1998
        public override async Task<bool> VerifyReceipt(string receipt, string signature)
        {
            var googlePlayReceipt = Newtonsoft.Json.JsonConvert.DeserializeObject<GooglePlayReceipt>(receipt);
            ProductId = googlePlayReceipt.productId;

            return VerifyData(receipt, signature);
        }
#pragma warning restore 1998

    }
}
