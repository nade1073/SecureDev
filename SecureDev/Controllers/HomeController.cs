using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using Vladi2.Models;

namespace Vladi2.Controllers
{
    public class HomeController : BaseController
    {
        const string c_passwordKey = "Nadav&Netanel";
        const string m_ConnectionNadav = @"C:\Users\Nadav\Desktop\SecureDev\SecureDev\Sqlite\db.sqlite";
        const string m_ConectionNetanel = @"C:\לימודים HIT\שנה ג סמסטר קיץ\פרוייקט ולדי\SecureDev\Sqlite\db.sqlite";
        //entry point for main page as determined in the route config
        public ActionResult Index(string validationError = null)
        {
   
            return View();
        }

    
        //GET: home/login 
        [HttpPost]
        public ActionResult Login(string username, string password)
        {
            if(!ValidationLoginUserProperty(username, password))
            {
                return RedirectToAction("Index", "Home");
            }
            //the path is absolute and should be changed.
            string encriptedPassword;
            UserAccount userDetailes = new UserAccount();
            userDetailes.UserName = username;
            userDetailes.Password = password;
            var connectionString = string.Format("DataSource={0}", m_ConnectionNadav);
            DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
            encriptedPassword = EncryptionManager.Encrypt(password, c_passwordKey);
            string loginQuery = "SELECT * FROM tblusers Where Username = @UserName";
            Func<SQLiteCommand, SQLiteDataReader, RedirectToRouteResult> MethodToBeInvoked;
            MethodToBeInvoked = (commad, reader) =>
            {
                while (reader.Read())
                {
                    //if we got here - the select succeded , the user exist in db - redirect to userHome page
                    var encriptionPassword = reader.GetString(2).Trim();
                    var decriptionis = EncryptionManager.Decrypt(encriptionPassword, c_passwordKey);
                    var userName = reader.GetString(1).Trim();
                    if (decriptionis == password)
                    {
                        Session["UserName"] = username;
                        return RedirectToAction("UserHome", "Home");
                    }
                        
                }
                return RedirectToAction("Index", "Home", new { validationError = "The username or password are invalid" });

            };
            return databaseConnection.ContactToDataBaseAndExecute(loginQuery, userDetailes, MethodToBeInvoked, "@UserName");
        }
        //gets the string from the input and returns a xss view with the string as model
        //this is for xss demonstration.
        public ActionResult XSS(string xss)
        {
            var vm = new homeVM() { data = xss };
            return View(vm);
        }
        //returns the user home page
        public ActionResult UserHome()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Register(UserAccount user, string ConfirmPassword, HttpPostedFileBase file)
        {
            if (IsImage(file))
            { 
            byte[] fileInBytes = new byte[file.ContentLength];
            using (BinaryReader theReader = new BinaryReader(file.InputStream))
            {
                fileInBytes = theReader.ReadBytes(file.ContentLength);
            }
            string fileAsString = Convert.ToBase64String(fileInBytes);
            user.PictureUser = fileAsString;
             }
            else
            {
                return RedirectToAction("Index", "Home");
            }
            string encriptedPassword;
                string connectionString = string.Format("DataSource={0}", m_ConnectionNadav);
            DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
            if (user.Password != ConfirmPassword)
            {
                ViewBag.errorConfirm = "The passwords is not same";
                return RedirectToAction("Index", "Home");
            }

            if(!ValidationRegUserProperty(user))
            {
                return RedirectToAction("Index", "Home");
            }

            encriptedPassword = EncryptionManager.Encrypt(user.Password, c_passwordKey);
            user.Password = encriptedPassword;

            var query = "SELECT * FROM tblusers Where Username = @UserName or Email = @Email";
            Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvokedAfterTheValidation;
            MethodToBeInvokedAfterTheValidation = (commad1, reader1) =>
            {
                return RedirectToAction("Index", "Home");
            };
            Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvoked;
            MethodToBeInvoked = (commad, reader) =>
            {
                if (reader.Read() == true)
                {
                    ViewBag.ExistUsernameoremail = "your email or username is already been chosen";
                    return RedirectToAction("Index", "Home");
                }

                string insetrToDataBaseQuery = "Insert INTO tblusers (FirstName, UserName, Password, LastName, PhoneNumber, Email, PictureUser) VALUES(@FirstName,@UserName,@Password,@LastName,@PhoneNumber,@Email,@PictureUser)";
                return databaseConnection.ContactToDataBaseAndExecute(insetrToDataBaseQuery, user, MethodToBeInvokedAfterTheValidation, "@FirstName", "@Password", "@UserName", "@LastName", "@PhoneNumber", "@Email", "@PictureUser");
            };
            return databaseConnection.ContactToDataBaseAndExecute(query, user, MethodToBeInvoked, "@UserName", "@Email");

            //string insetrToDataBaseQuery = "Insert INTO tblusers (FirstName, UserName, Password, LastName, PhoneNumber, Email) VALUES(@FirstName,@UserName,@Password,@LastName,@PhoneNumber,@Email)";
            //return databaseConnection.ContactToDataBaseAndExecute(insetrToDataBaseQuery, user, MethodToBeInvoked, "@FirstName", "@Password", "@UserName", "@LastName", "@PhoneNumber", "@Email");
        }
        private bool ValidationRegUserProperty(UserAccount i_User)
        {
            if(i_User.FirstName==null || i_User.LastName ==null || i_User.Email == null || i_User.Password == null || i_User.PhoneNumber == null || i_User.UserName == null)
            {
                return false;
            }
            string passwordRegex = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[$@$!%*?&])[A-Za-z\d$@$!%*?&]{8,10}";
            var matchPassword = Regex.Match(i_User.Password, passwordRegex, RegexOptions.IgnoreCase);
            if (!matchPassword.Success)
            {
                return false;
            }

            string emailRegex = @"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$";
            var matchEmail = Regex.Match(i_User.Email, emailRegex, RegexOptions.IgnoreCase);
            if (!matchEmail.Success)
            {
                return false;
            }

            if(!IsDigitsOnly(i_User.PhoneNumber))
            {
                return false;
            }

            //if(!(IsCharacterOnly(i_User.FirstName)&&IsCharacterOnly(i_User.LastName)))
            //{
            //    return false;
            //}
            return true;

        }

        private bool ValidationLoginUserProperty(string i_UserName, string i_Password)
        {
            if (i_Password == null || i_UserName == null)
            {
                return false; 
            }
            string passwordRegex = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[$@$!%*?&])[A-Za-z\d$@$!%*?&]{8,10}";
            var matchPassword = Regex.Match(i_Password, passwordRegex, RegexOptions.IgnoreCase);
            if (!matchPassword.Success)
            {
                return false;
            }

            return true;

        }

        bool IsDigitsOnly(string str)
        {
            foreach (char c in str)
            {
                if (c < '0' || c > '9')
                    return false;
            }

            return true;
        }

        bool IsCharacterOnly(string str)
        {
            foreach (char c in str)
            {
                if (!((c > 'a' && c < 'z')|| (c > 'A' && c < 'Z')))
                    return false;
            }

            return true;
        }
        private bool IsImage(HttpPostedFileBase file)
        {
            if (file.ContentType.Contains("image"))
            {
                return true;
            }

            string[] formats = new string[] { ".jpg", ".png", ".gif", ".jpeg" }; // add more if u like...

            // linq from Henrik Stenbæk
            return formats.Any(item => file.FileName.EndsWith(item, StringComparison.OrdinalIgnoreCase));
        }
    }
}