using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace WebApplication.EPAY
{
    public static class EPAYHelper
    {
        public static bool IsValidEPAYRequest(this HttpRequest httpRequest, EPAYRequest epayRequest, EPAYConfiguration config, out string responseContent)
        {
            responseContent = string.Empty;

            using (MD5 md5Hash = MD5.Create())
            {
                var queryValues = httpRequest.Query.Where(q => q.Key != "HASH_CODE").Select(s => s.Value.ToString()).ToList();

                string hashOfInput = GetMd5Hash(md5Hash, string.Join(string.Empty, queryValues) + config.SecretKey);

                var result = VerifyMd5Hash(md5Hash, hashOfInput, epayRequest.HashCode);

                if (!result)
                {
                    responseContent = BuildREsponse(ResponseStatusCode.InvalidHashCode).ToString();
                }

                return result;
            }
        }

        public static XElement BuildREsponse(ResponseStatusCode statusCode, int? customerBalance = null, string customerFirstName = null, string custmerLastName = null, int? paymentId = null)
        {
            if (statusCode == ResponseStatusCode.OK)
            {
                var receiptIdXElement = paymentId.HasValue ? new XElement("receipt-id", paymentId.Value) : null;
                var debtXElement = customerBalance.HasValue ? new XElement("debt", customerBalance.Value) : null;

                var additionalInfoXElement = !string.IsNullOrWhiteSpace(customerFirstName) && !string.IsNullOrWhiteSpace(custmerLastName) ?
                    new XElement("additional-info",
                        new XElement("parameter", new XAttribute("name", "first_name"), customerFirstName),
                        new XElement("parameter", new XAttribute("name", "last_name"), custmerLastName)
                    ) : null;

                var result =
                    new XElement("pay-response",
                        new XElement("status",
                            new XAttribute("code", (int)statusCode),
                            statusCode.ToString()),
                        new XElement("timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                        receiptIdXElement,
                        debtXElement,
                        additionalInfoXElement
                    );

                return result;
            }
            else
            {
                var result =
                    new XElement("pay-response",
                        new XElement("status",
                            new XAttribute("code", (int)statusCode),
                            new XAttribute("retry", false),
                            statusCode.ToString()),
                        new XElement("timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                    );

                return result;
            }
        }

        static bool VerifyMd5Hash(MD5 md5Hash, string hashOfInput, string hash)
        {
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;

            return 0 == comparer.Compare(hashOfInput, hash);
        }

        static string GetMd5Hash(MD5 md5Hash, string input)
        {
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            var stringBuilder = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
            {
                stringBuilder.Append(data[i].ToString("x2"));
            }

            return stringBuilder.ToString();
        }
    }
}
