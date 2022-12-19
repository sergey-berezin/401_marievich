using System;
using System.Linq;
using System.Security.Cryptography;

namespace ArcFaceWPF.Helpers
{
    public class ImageHelper
    {
        public static string Hash(byte[] data)
        {
            using var sha256 = SHA256.Create();
            return string.Concat(
                sha256
                .ComputeHash(data)
                .Select(x => x.ToString("X2"))
            );
        }

        public static float[]? ByteToFloat(byte[]? array)
        {
            if (array == null)
            {
                return null;
            }
               
            var float_array = new float[array.Length / 4];
            Buffer.BlockCopy(array, 0, float_array, 0, array.Length);
            return float_array;
        }

        public static byte[]? FloatToByte(float[]? array)
        {
            if (array == null)
            {
                return null;
            }
                
            var result = new byte[array.Length * 4];
            Buffer.BlockCopy(array, 0, result, 0, result.Length);
            return result;
        }
    }
}
