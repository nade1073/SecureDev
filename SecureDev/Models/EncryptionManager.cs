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
        private  static String salt = "Ben&Netanel&Nadav";
        public static String getSHA256Password(String password)
        {
            String newPasswordwithsalt = password + salt;
            SHA256 sha256 = SHA256Managed.Create();
            var retPassword = String.Empty;
            byte[] cryptPassword = sha256.ComputeHash(Encoding.ASCII.GetBytes(newPasswordwithsalt), 0, Encoding.ASCII.GetByteCount(newPasswordwithsalt));
            foreach (byte theByte in cryptPassword)
            {
                retPassword += theByte.ToString("x2");
            }

            return retPassword;
        }

    }
}