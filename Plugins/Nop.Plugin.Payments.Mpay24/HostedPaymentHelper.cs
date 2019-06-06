using System;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.MPay24
{
    public class HostedPaymentHelper
    {
        #region Properties
        internal static string OrderPageTimestamp
        {
            get
            {
                return (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds.ToString().Split('.')[0];
            }
        }
        #endregion

        #region Methods
        public static string CalcRequestSign(NameValueCollection reqParams, string publicKey)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(reqParams["merchantID"]);
            sb.Append(reqParams["amount"]);
            sb.Append(reqParams["currency"]);
            sb.Append(reqParams["orderPage_timestamp"]);
            sb.Append(reqParams["orderPage_transactionType"]);

            return CalcHMACSHA1Hash(sb.ToString(), publicKey).Replace("\n", "");
        }

        public static bool ValidateResponseSign(NameValueCollection rspParams, string publicKey)
        {
            string transactionSignature = null;
            string[] signedFieldsArr = null;

            try
            {
                transactionSignature = rspParams["transactionSignature"];
                signedFieldsArr = rspParams["signedFields"].Split(',');
            }
            catch (Exception)
            {
                return false;
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < signedFieldsArr.Length; i++)
            {
                sb.Append(rspParams[signedFieldsArr[i]]);
            }

            string s = CalcHMACSHA1Hash(sb.ToString(), publicKey);

            return transactionSignature.Equals(s);
        }
        #endregion

        #region Utilities
        private static string CalcHMACSHA1Hash(string s, string publicKey)
        {
            using (HMACSHA1 cs = new HMACSHA1(Encoding.UTF8.GetBytes(publicKey)))
            {
                return Convert.ToBase64String(cs.ComputeHash(Encoding.UTF8.GetBytes(s)));
            }
        }
        #endregion
    }
}
