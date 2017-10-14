using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Vladi2.Models
{
    public static class EncryptionManager
    {
        private  static string salt = "Ben&Netanel&Nadav";
        public static String getSHA256Password(String password)
        {
            SHA256 sha256 = SHA256Managed.Create();
            var retPassword = String.Empty;
            byte[] cryptPassword = sha256.ComputeHash(Encoding.ASCII.GetBytes(password), 0, Encoding.ASCII.GetByteCount(password));
            foreach (byte theByte in cryptPassword)
            {
                retPassword += theByte.ToString("x2");
            }

            return retPassword;
        }

    }
}