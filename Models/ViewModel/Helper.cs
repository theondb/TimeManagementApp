using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Web;

namespace TimeManagementApp.Models.ViewModel
{
    public class Helper
    {
        public String CreateSalt(int size)
        {
            var rng = new RNGCryptoServiceProvider();
            var b = new byte[size];
            rng.GetBytes(b);
            return Convert.ToBase64String(b);
        }
        public String GenerateSha256Hash(string input, string salt)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(input + salt);
            SHA256Managed shaString = new SHA256Managed();
            byte[] hash = shaString.ComputeHash(bytes);

            return BitConverter.ToString(hash);
        }
    }
}