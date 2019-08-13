using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace EPAY
{
    public static class EPAYHelper
    {
        public static bool IsValidEPAYRequest(HttpRequest httpRequest, EPAYRequest epayRequest, EPAYConfiguration epayConfig, out string responseContent)
        {
            responseContent = string.Empty;

            if (epayRequest.Username != epayConfig.Username && epayRequest.Password != epayConfig.Password)
            {
                responseContent = BuildResponseContent(ResponseStatusCode.InvalidUsernamePassword);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(epayRequest.ServiceId) && epayRequest.ServiceId != epayConfig.ServiceId)
            {
                responseContent = BuildResponseContent(ResponseStatusCode.InvalidServiceId);
                return false;
            }

            using (MD5 md5Hash = MD5.Create())
            {
                var queryValues = httpRequest.Query.Where(q => q.Key != "HASH_CODE").Select(s => s.Value.ToString()).ToList();

                string hashOfInput = GetMd5Hash(md5Hash, string.Join(string.Empty, queryValues) + epayConfig.SecretKey);

                if (!VerifyMd5Hash(md5Hash, hashOfInput, epayRequest.HashCode))
                {
                    responseContent = BuildResponseContent(ResponseStatusCode.InvalidHashCode);
                    return false;
                }
            }

            return true;
        }

        public static string BuildResponseContent(ResponseStatusCode responseStatusCode, int? debt = null, string firstName = null, string lastName = null, int? receiptId = null)
        {
            if (responseStatusCode == ResponseStatusCode.OK)
            {
                var receiptIdXElement = receiptId.HasValue ? new XElement("receipt-id", receiptId.Value) : null;
                var debtXElement = debt.HasValue ? new XElement("debt", debt.Value) : null;

                var additionalInfoXElement = !string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName) ?
                    new XElement("additional-info",
                        new XElement("parameter", new XAttribute("name", "first_name"), firstName),
                        new XElement("parameter", new XAttribute("name", "last_name"), lastName)
                    ) : null;

                var result =
                    new XElement("pay-response",
                        new XElement("status",
                            new XAttribute("code", (int)responseStatusCode),
                            responseStatusCode.ToString()),
                        new XElement("timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                        receiptIdXElement,
                        debtXElement,
                        additionalInfoXElement
                    );

                return result.ToString();
            }
            else
            {
                var result =
                    new XElement("pay-response",
                        new XElement("status",
                            new XAttribute("code", (int)responseStatusCode),
                            new XAttribute("retry", false),
                            responseStatusCode.ToString()),
                        new XElement("timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                    );

                return result.ToString();
            }
        }

        public static int ConvertGELToEPAYAmount(decimal input)
        {
            return Convert.ToInt32(input * 100);
        }

        public static decimal ConvertEPAYAmountToGEL(int input)
        {
            return Convert.ToDecimal(input) / 100;
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
