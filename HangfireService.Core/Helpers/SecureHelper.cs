using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace HangfireService.Core.Helpers
{
    public class SecureHelper
    {
        public string ToMD5(string encodeStr)
        {
            using var cryptoMD5 = MD5.Create();
            var bytes = Encoding.UTF8.GetBytes(encodeStr);
            var hash = cryptoMD5.ComputeHash(bytes);
            var md5 = BitConverter.ToString(hash)
              .Replace("-", string.Empty);

            return md5;
        }
    }
}
