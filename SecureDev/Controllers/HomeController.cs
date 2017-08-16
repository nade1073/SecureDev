using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using System.Web.Mvc;
using Vladi2.Models;

namespace Vladi2.Controllers
{
    public class HomeController : BaseController
    {
        const string c_passwordKey = "Nadav&Netanel";
        //entry point for main page as determined in the route config
        public ActionResult Index(string validationError = null)
        {
            var vm = new homeVM() { data = validationError };
            return View(vm);
        }
        //GET: home/login 
        public ActionResult Login(string username, string password)
        {
            //the path is absolute and should be changed.
            string encriptedPassword;
            var connectionString = string.Format("DataSource={0}", @"C:\Users\Nadav\Desktop\SecureDev\SecureDev\Sqlite\db.sqlite");

                encriptedPassword = EncryptionManager.Encrypt(password, c_passwordKey);

              
            using (var m_dbConnection = new SQLiteConnection(connectionString))
            {
                m_dbConnection.Open();
                using (SQLiteCommand command = new SQLiteCommand("SELECT * FROM tblusers Where Username = '" + username + "'", m_dbConnection))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        //if we got here - the select succeded , the user exist in db - redirect to userHome page
                        var encriptionPassword = reader.GetString(2).Trim();
                        var decriptionis = EncryptionManager.Decrypt(encriptionPassword, c_passwordKey);
                        var userName = reader.GetString(1).Trim();
                        if (decriptionis == password)
                            return RedirectToAction("UserHome", "Home", new { userName });
                    }
                }
            }
            //the login failed - redirect to login page with the login error
            return RedirectToAction("Index", "Home", new { validationError = "The username or password are invalid" });
        }
        //gets the string from the input and returns a xss view with the string as model
        //this is for xss demonstration.
        public ActionResult XSS(string xss)
        {
            var vm = new homeVM() { data = xss };
            return View(vm);
        }
        //returns the user home page
        public ActionResult UserHome(string userName)
        {
            var vm = new homeVM { data = userName };
            return View(vm);
        }
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Register(UserAccount user, string ConfirmPassword)
        {
            string encriptedPassword;
            if (user.Password != ConfirmPassword)
            {
                ViewBag.errorConfirm = "The passwords is not same";
                return View();
            }

            encriptedPassword = EncryptionManager.Encrypt(user.Password, c_passwordKey);

            var connectionString = string.Format("DataSource={0}", @"C:\Users\Nadav\Desktop\SecureDev\SecureDev\Sqlite\db.sqlite");

            using (var m_dbConnection = new SQLiteConnection(connectionString))
            {
                m_dbConnection.Open();
                using (SQLiteCommand command = new SQLiteCommand("SELECT * FROM tblusers Where Username = '" + user.Username + "' and Email = '" + user.Email + "'", m_dbConnection))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    if(reader.Read() == true)
                    {
                        ViewBag.ExistUsernameoremail = "your email or username is already been chosen";
                        return View();
                    }
                }

                using (SQLiteCommand command = new SQLiteCommand(
                    "Insert INTO tblusers (FirstName,UserName,Password,LastName,PhoneNumber,Email) VALUES ('"+user.FirstName+"','"+user.Username+"','"+ encriptedPassword + "','"+user.LastName+"','"+user.PhoneNumber+"','"+user.Email+"')", m_dbConnection))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                
                }

            }
            return View();
    
        }
        private string EncryptString_Aes(string plainText, byte[] Key, byte[] IV)
        {
            // Check arguments. 
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");
            byte[] encrypted;
            // Create an AesCryptoServiceProvider object 
            // with the specified key and IV. 
            using (AesCryptoServiceProvider aesAlg = new AesCryptoServiceProvider())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption. 
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {

                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }


            // Return the encrypted bytes from the memory stream. 
            //return encrypted;
            return System.Text.Encoding.UTF8.GetString(encrypted);

        }



    }
}




