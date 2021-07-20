using Hector.Core.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Hector.Core.Cryptography
{
    public static class Hashing
    {
        public static string ToMD5(string input)
        {
            input.AssertNotNull("input");

            MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            return
                hash
                    .Select(x => x.ToString("x2"))
                    .StringJoin(string.Empty);
        }
    }
}
