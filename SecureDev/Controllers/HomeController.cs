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
            DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
            encriptedPassword = EncryptionManager.Encrypt(password, c_passwordKey);
            string loginQuery = "SELECT * FROM tblusers Where Username = @Username";



            using (var m_dbConnection = new SQLiteConnection(connectionString))
            {
                m_dbConnection.Open();
                string loginQuery = "SELECT * FROM tblusers Where Username = @Username";
                using (SQLiteCommand command = new SQLiteCommand(loginQuery, m_dbConnection))
                {
                    command.Parameters.Add(@"Username", System.Data.DbType.String);
                    command.Parameters["@Username"].Value = username;
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
            string connectionString = string.Format("DataSource={0}", @"C:\Users\Nadav\Desktop\SecureDev\SecureDev\Sqlite\db.sqlite");
            DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
            if (user.Password != ConfirmPassword)
            {
                ViewBag.errorConfirm = "The passwords is not same";
                return View();
            }

            encriptedPassword = EncryptionManager.Encrypt(user.Password, c_passwordKey);
            user.Password = encriptedPassword;

            var query = "SELECT * FROM tblusers Where Username = @UserName or Email = @Email";
            Func<SQLiteCommand, SQLiteDataReader, ViewResult> MethodToBeInvoked;
            MethodToBeInvoked = (commad, reader) =>
            {
                if (reader.Read() == true)
                {
                    ViewBag.ExistUsernameoremail = "your email or username is already been chosen";
                    return View();
                }
                return View();
            };
            databaseConnection.ContactToDataBase(query, user, MethodToBeInvoked, "@UserName", "@Email");



            string insetrToDataBaseQuery = "Insert INTO tblusers (FirstName, UserName, Password, LastName, PhoneNumber, Email) VALUES(@FirstName,@UserName,@Password,@LastName,@PhoneNumber,@Email)";
            databaseConnection.ContactToDataBase(insetrToDataBaseQuery, user, null, "@FirstName", "@Password", "@UserName", "@LastName", "@PhoneNumber", "@Email");

            return View();

        }
    }
}




