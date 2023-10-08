using System;
using System.Security.Cryptography;
using System.Text;

namespace Hector.Core.Cryptography
{
    public static class HashHelper
    {
        public static string ComputeGenericHash(Func<HashAlgorithm> hashProvider, string sourceForHash, Encoding? encoding = null)
        {
            byte[] bytes = (encoding ?? Encoding.UTF8).GetBytes(sourceForHash);
            using HashAlgorithm hashAlgorithm = hashProvider();
            byte[] array = hashAlgorithm.ComputeHash(bytes);
            return Convert.ToBase64String(array);
        }

        public static string ComputeGenericHash(Func<HashAlgorithm> hashProvider, byte[] bContent)
        {
            using HashAlgorithm hashAlgorithm = hashProvider();
            byte[] array = hashAlgorithm.ComputeHash(bContent);
            return Convert.ToBase64String(array);
        }

        public static string ComputeSHA512(string sourceForHash) => ComputeGenericHash(new Func<HashAlgorithm>(SHA512.Create), sourceForHash);

        public static string ComputeSHA512(byte[] bContent) => ComputeGenericHash(new Func<HashAlgorithm>(SHA512.Create), bContent);

        public static string ComputeMD5(string sourceForHash) => ComputeGenericHash(new Func<HashAlgorithm>(MD5.Create), sourceForHash);

        public static string ComputeMD5(byte[] bContent) => ComputeGenericHash(new Func<HashAlgorithm>(MD5.Create), bContent);
    }
}
