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
            return ComputeGenericHash(hashProvider, bytes);
        }

        public static string ComputeGenericHash(Func<HashAlgorithm> hashProvider, byte[] bContent)
        {
            using HashAlgorithm hashAlgorithm = hashProvider();
            byte[] array = hashAlgorithm.ComputeHash(bContent);
            return array.ToHexString();
        }

        public static string ComputeMD5(string sourceForHash) => ComputeGenericHash(new Func<HashAlgorithm>(MD5.Create), sourceForHash);

        public static string ComputeMD5(byte[] bContent) => ComputeGenericHash(new Func<HashAlgorithm>(MD5.Create), bContent);

        public static string ComputeSHA1(string sourceForHash) => ComputeGenericHash(new Func<HashAlgorithm>(SHA1.Create), sourceForHash);

        public static string ComputeSHA1(byte[] bContent) => ComputeGenericHash(new Func<HashAlgorithm>(SHA1.Create), bContent);

        public static string ComputeSHA256(string sourceForHash) => ComputeGenericHash(new Func<HashAlgorithm>(SHA256.Create), sourceForHash);

        public static string ComputeSHA256(byte[] bContent) => ComputeGenericHash(new Func<HashAlgorithm>(SHA256.Create), bContent);

        public static string ComputeSHA512(string sourceForHash) => ComputeGenericHash(new Func<HashAlgorithm>(SHA512.Create), sourceForHash);

        public static string ComputeSHA512(byte[] bContent) => ComputeGenericHash(new Func<HashAlgorithm>(SHA512.Create), bContent);
    }
}
