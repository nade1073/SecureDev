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
        const string m_ConnectionReznik = @"C:\לימודים HIT\שנה ג סמסטר קיץ\פרוייקט ולדי\SecureDev\Sqlite\db.sqlite";
        const string m_ConnectionBen= @"C:\Users\benma\Source\Repos\SecureDev\SecureDev\Sqlite\db.sqlite";
        
        //entry point for main page as determined in the route config
        public ActionResult Index()
        {
            if (Session["UserName"] != null)
            {
                return RedirectToAction("UserHome", "Home");
            }
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
            var connectionString = string.Format("DataSource={0}", m_ConnectionItzik);
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
                    var isAdmin = reader.GetInt32(7);
                    var decriptionis = EncryptionManager.Decrypt(encriptionPassword, c_passwordKey);
                    var userName = reader.GetString(1).Trim();
                    if (decriptionis == password)
                    {
                        Session["UserName"] = username;
                        if(isAdmin==1)
                        {
                            Session["IsAdmin"] = "1";
                        }
                        else
                        { 
                            Session["IsAdmin"] = "0";
                        }
                        return RedirectToAction("UserHome", "Home");
                    }

                }
                TempData["ErrorUserNameAndPassword"] = "The username or password are incorrect";
                return RedirectToAction("Index", "Home");

            };
            return databaseConnection.ContactToDataBaseAndExecute(loginQuery, userDetailes, MethodToBeInvoked, "@UserName");
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
            string connectionString = string.Format("DataSource={0}", m_ConnectionItzik);
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
            if (Session["UserName"] == null)
            {
                return RedirectToAction("index", "Home");
            }
            return View();
        }

        public ActionResult AccountProfile()
        {
            if (Session["UserName"] == null)
            {
                return RedirectToAction("index", "Home");
            }
            var connectionString = string.Format("DataSource={0}", m_ConnectionItzik);
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
            string connectionString = string.Format("DataSource={0}", m_ConnectionItzik);
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

        public ActionResult Forum(string topic)
        {
            if(Session["UserName"]==null)
            {
                return RedirectToAction("Index", "Home");
            }
            if (!(topic == "Sport" || topic == "Question" || topic == "Luxury"))
            {
                return RedirectToAction("HomePageForum", "Home");
            }

            string connectionString = string.Format("DataSource={0}", m_ConnectionItzik);
            DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
            List<ForumMessage> messagesOFTheForum = new List<ForumMessage>();
             ForumMessage MessageofTheDataBase = new ForumMessage();
            MessageofTheDataBase.TopicMessage = topic;
            var query = "SELECT * FROM Forum Where Topic = @Topic";
            Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvoked;
            MethodToBeInvoked = (commad, reader) =>
            {
  
                while (reader.Read() == true)
                {
                    ForumMessage Message = new ForumMessage();    
                    Message.UserName = reader.GetString(0).Trim();
                    Message.TopicMessage = reader.GetString(1).Trim();
                    Message.SubjectMessage = reader.GetString(2).Trim();
                    Message.Message = reader.GetString(3).Trim();
                    messagesOFTheForum.Add(Message);
                }
                ViewBag.ListOfMessages = messagesOFTheForum;
                ViewBag.Topic = topic;
                return View();
            };
            return databaseConnection.ContactToDataBaseAndExecute(query, MessageofTheDataBase, MethodToBeInvoked,"@Topic");

        }
        [HttpPost]
        public ActionResult PostMessage(string Subject, string Message, string Topic)
        {
           

            if (messageValidation(Subject, Message) )
            {

                string connectionString = string.Format("DataSource={0}", m_ConnectionItzik);
                DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
                ForumMessage messageToLoad = new ForumMessage();
                messageToLoad.SubjectMessage = Subject;
                messageToLoad.Message = Message;
                messageToLoad.TopicMessage = Topic;
                messageToLoad.UserName = (string)Session["UserName"];
                string insetrToDataBaseQuery = "Insert INTO Forum (UserName, Topic, Subject, Message) VALUES(@UserName,@Topic,@Subject,@Message)";
                Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvokedAfterTheValidation;
                MethodToBeInvokedAfterTheValidation = (commad1, reader1) =>
                {
                    return RedirectToAction("Forum", "Home", new { topic = Topic });
                };
                return databaseConnection.ContactToDataBaseAndExecute(insetrToDataBaseQuery, messageToLoad, MethodToBeInvokedAfterTheValidation, "@UserName", "@Topic", "@Subject", "@Message");
            }
            return RedirectToAction("Forum", "Home", new { topic = Topic });
        }

        public ActionResult SignOut()
        {
            Session.Abandon();
            Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public ActionResult ControlPanelUpdate(string username,bool checkbox)
        {
            return RedirectToAction("ControlPanel", "Home");
        }

        public ActionResult ControlPanel()
        {
            if (!(Session["UserName"] != null && (string)Session["isAdmin"] == "1"))
            {
                return RedirectToAction("Index", "Home");

            }

            UserAccount userDetailes = new UserAccount();
            var connectionString = string.Format("DataSource={0}", m_ConnectionNadav);
                DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
                List<UserAccount> users = new List<UserAccount>();
                List<int> usersIsAdmin = new List<int>();
                string loginQuery = "SELECT * FROM tblusers";
                Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvoked;
                MethodToBeInvoked = (commad, reader) =>
                {
                    while (reader.Read())
                    {
                        //if we got here - the select succeded , the user exist in db - redirect to userHome page
                        UserAccount userDetails = new UserAccount();
                        userDetails.FirstName = reader.GetString(0).Trim();
                        userDetails.UserName = reader.GetString(1).Trim();
                        //   userDetailes.Password = reader.GetString(2).Trim();
                        userDetails.LastName = reader.GetString(3).Trim();
                        userDetails.PhoneNumber = reader.GetString(4).Trim();
                        userDetails.Email = reader.GetString(5).Trim();
                        userDetails.PictureUser = reader.GetString(6).Trim();

                        usersIsAdmin.Add(reader.GetInt32(7));
                        users.Add(userDetails);




                    }
                    ViewBag.usersDetails = users;
                    ViewBag.usersIsAdmin = usersIsAdmin;
                    return View();

                };
        
            return databaseConnection.ContactToDataBaseAndExecute(loginQuery, userDetailes, MethodToBeInvoked);
            
        }


        [HttpPost]
        public ActionResult DeleteMessage(string i_Subject, string i_Topic)
        {
            string connectionString = string.Format("DataSource={0}", m_ConnectionItzik);
            DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
            ForumMessage messageToDelete = new ForumMessage();
            messageToDelete.UserName = (string)Session["UserName"];
            messageToDelete.SubjectMessage = i_Subject;
            messageToDelete.TopicMessage = i_Topic;
            string deleteFromDataBaseQuery = "DELETE FROM Forum WHERE UserName = @UserName and Topic = @Topic and Subject = @Subject";
            Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvokedAfterTheValidation;
            MethodToBeInvokedAfterTheValidation = (commad, reader) =>
            {
                return RedirectToAction("Forum", "Home", new { topic = i_Topic });
            };
            return databaseConnection.ContactToDataBaseAndExecute(deleteFromDataBaseQuery, messageToDelete, MethodToBeInvokedAfterTheValidation, "@UserName", "@Topic", "@Subject");
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

        private bool messageValidation(string i_Subject, string i_Message)
        {
            bool isValide = false;

            if (i_Subject != "" && i_Message != "")
            {
                if(!(i_Message.Contains("#") || i_Subject.Contains("#")))
                {
                    isValide = true;
                }
            }
            return isValide;
        }
    }
}