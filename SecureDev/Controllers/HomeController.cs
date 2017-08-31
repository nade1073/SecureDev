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
        const string m_ConnectionItzik = @"C:\Users\shalev itzhak\Source\Repos\SecureDev\SecureDev\Sqlite\db.sqlite";
        const string m_ConectionNetanel = @"C:\לימודים HIT\שנה ג סמסטר קיץ\פרוייקט ולדי\SecureDev\Sqlite\db.sqlite";
        const string m_ConnectionBen = @"C:\Users\benma\Source\Repos\SecureDev\SecureDev\Sqlite\db.sqlite";
        //entry point for main page as determined in the route config
        public ActionResult Index()
        {
            if (TempData["ErrorUserNameAndPassword"] != null)
            {
                ViewBag.loginError = TempData["ErrorUserNameAndPassword"];
            }
            return View();
        }

        [HttpPost]
        public ActionResult Login(string username, string password)
        {
            if(!ValidationLoginUserProperty(username, password))
            {
                TempData["ErrorUserNameAndPassword"] = "The username or password are incorrect";
                return RedirectToAction("Index", "Home");
            }
            //the path is absolute and should be changed.
            string encriptedPassword;
            UserAccount userDetailes = new UserAccount();
            userDetailes.UserName = username;
            userDetailes.Password = password;
            var connectionString = string.Format("DataSource={0}", m_ConnectionBen);
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
                TempData["ErrorUserNameAndPassword"] = "The username or password are incorrect";
                return RedirectToAction("Index", "Home");

            };
            return databaseConnection.ContactToDataBaseAndExecute(loginQuery, userDetailes, MethodToBeInvoked, "@UserName");
        }

        //this is for xss demonstration.
        public ActionResult XSS(string xss)
        {
            var vm = new homeVM() { data = xss };
            return View(vm);
        }
        //returns the user home page
        public ActionResult UserHome()
        {
            if(Session["UserName"]==null)
            {
                return RedirectToAction("Index", "Home");
            }
            
            return View();
        }

        [HttpPost]
        public ActionResult Register(UserAccount user, string ConfirmPassword, HttpPostedFileBase file)
        {
            if (file!=null && IsImage(file))
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
                string connectionString = string.Format("DataSource={0}", m_ConnectionBen);
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


        public ActionResult HomePageForum()
        {
            if(Session["UserName"]!=null)
            {
                return View();
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        public ActionResult AccountProfile()
        {
            var connectionString = string.Format("DataSource={0}", m_ConnectionBen);
            DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
            string userNameFromSession = (string)Session["UserName"];
            string accountProfileQuery = "SELECT * FROM tblusers Where Username = @UserName";
            UserAccount userDetails = new UserAccount();//new UserAccount("nadav", "@Nade87491", "nade1073@gmail.com", "0546960200", "Nadav", "Shalev");
            userDetails.UserName = userNameFromSession;
            Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvoked;
            MethodToBeInvoked = (commad, reader) =>
            {
                if (reader.Read() == true)// it's mean that I found the user name in the data base
                {
                    //if we got here - the select succeded , the user exist in db - redirect to userHome page
                    var encriptionPassword = reader.GetString(2).Trim();
                    var decriptionis = EncryptionManager.Decrypt(encriptionPassword, c_passwordKey);
                    var userName = reader.GetString(1).Trim();
                    userDetails.FirstName = reader.GetString(0).Trim();
                    userDetails.LastName = reader.GetString(3).Trim();
                    userDetails.PhoneNumber = reader.GetString(4).Trim();
                    userDetails.Email = reader.GetString(5).Trim();
                    userDetails.PictureUser = reader.GetString(6).Trim();
                    ViewBag.User = userDetails;
                    return View();

                }
                return RedirectToAction("Index", "Home");

            };

            return databaseConnection.ContactToDataBaseAndExecute(accountProfileQuery, userDetails, MethodToBeInvoked, "@UserName");
            //if (Session["UserName"]==null)
            //{
            //    return RedirectToAction("Index", "Home");
            //}
            //ViewBag.User = userDetails;
            //return View();
        }
        [HttpPost]
        public ActionResult AccountProfile(string PhoneNumber,string LastName,string FirstName,string passwordRegister,string Email,HttpPostedFileBase file)
        {
            UserAccount UpdateUser = new UserAccount();
        
            UpdateUser.Email = Email;
            UpdateUser.FirstName = FirstName;
            UpdateUser.LastName = LastName;
            UpdateUser.PhoneNumber = PhoneNumber;
            UpdateUser.UserName =(string)Session["UserName"];
            string connectionString = string.Format("DataSource={0}", m_ConnectionBen);
            DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
            string profileQuriy = "UPDATE tblusers SET FirstName = @FirstName, LastName = @LastName,PhoneNumber=@PhoneNumber,Email=@Email WHERE UserName = @UserName";

            updateUserBasicInformatiom(profileQuriy, databaseConnection, UpdateUser, "@FirstName", "@LastName", "@PhoneNumber", "@Email", "@UserName");

            profileQuriy = "UPDATE tblusers SET Password = @Password WHERE UserName = @UserName";

            bool isGoodPassword = passwordCheckingAndUpdatingifNeeded(profileQuriy, databaseConnection, passwordRegister, UpdateUser, "@Password", "@UserName");

            if(!isGoodPassword)
            {
                ViewBag.Error = "Error in password";// Add to view!!!@@#!@#!#!@#!
                return View();
            }
            profileQuriy = "UPDATE tblusers SET PictureUser = @PictureUser WHERE UserName = @UserName";

            bool isGoodFileFormatToUpload = fileCheckingAndUpdatingifNeeded(profileQuriy, databaseConnection, file, UpdateUser, "@PictureUser", "@UserName");

            if(!isGoodFileFormatToUpload)
            {
                return RedirectToAction("AccountProfile", "Home");
            }

            return RedirectToAction("AccountProfile", "Home");
        }

        public ActionResult SportsForum()
        {
            return View();
        }

        private bool fileCheckingAndUpdatingifNeeded(string profileQuriy, DataBaseUtils databaseConnection, HttpPostedFileBase file, UserAccount updateUser, params string[] i_ParametersOfTheQuery)
        {
            bool isValidateFileFormat = true;
            Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvoked;
            MethodToBeInvoked = (commad, reader) =>
            {
                return View();
            };
            if (file != null)
            {
                if (IsImage(file))
                {
                    byte[] fileInBytes = new byte[file.ContentLength];
                    using (BinaryReader theReader = new BinaryReader(file.InputStream))
                    {
                        fileInBytes = theReader.ReadBytes(file.ContentLength);
                    }
                    string fileAsString = Convert.ToBase64String(fileInBytes); // Last String base64 for image!
                    updateUser.PictureUser = fileAsString;
                    databaseConnection.ContactToDataBaseAndExecute(profileQuriy, updateUser, MethodToBeInvoked, "@PictureUser", "@UserName");
                }
                else
                {
                    isValidateFileFormat = false;
                }
            }
            return isValidateFileFormat;

        }

        private bool passwordCheckingAndUpdatingifNeeded(string profileQuriy, DataBaseUtils databaseConnection, string passwordRegister, UserAccount updateUser, params string[] i_ParametersOfTheQuery)
        {
            bool isValidatePassword = true;
            Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvoked;
            MethodToBeInvoked = (commad, reader) =>
            {
                return View();
            };
            if (passwordRegister != "*****")
            {
                if (ValidatePassword(passwordRegister))
                {
                    string encriptedPassword = EncryptionManager.Encrypt(passwordRegister, c_passwordKey);
                    updateUser.Password = encriptedPassword;
                    databaseConnection.ContactToDataBaseAndExecute(profileQuriy, updateUser, MethodToBeInvoked, i_ParametersOfTheQuery);
                }
                else
                {
                    isValidatePassword = false;
                }
            }
            return isValidatePassword;
        }

        private void updateUserBasicInformatiom(string i_ProfileQuriy, DataBaseUtils i_DatabaseConnection, UserAccount i_User, params string[] i_ParametersOfTheQuery)
        {

            Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvoked;
            MethodToBeInvoked = (commad, reader) =>
            {
                return View();
            };
            i_DatabaseConnection.ContactToDataBaseAndExecute(i_ProfileQuriy, i_User, MethodToBeInvoked, i_ParametersOfTheQuery);
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
        private bool ValidatePassword(string i_Password)
        {
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