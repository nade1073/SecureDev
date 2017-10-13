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
        const string m_ConnectionNadav = @"C:\Users\Nadav\Desktop\SecureDev\SecureDev\SecureDev\Sqlite\db.sqlite";
        const string m_ConnectionItzik = @"C:\Users\shalev itzhak\Source\Repos\SecureDev\SecureDev\Sqlite\db.sqlite";
        const string m_ConnectionReznik = @"C:\לימודים HIT\שנה ג סמסטר קיץ\פרוייקט ולדי\SecureDev\Sqlite\db.sqlite";
        const string m_ConnectionBen = @"C:\Users\benma\Source\Repos\SecureDev\SecureDev\Sqlite\db.sqlite";
        
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

        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult Login(string username, string password)
        {
            if (!ValidationLoginUserProperty(username, password))
            {
                ExceptionLogging.WriteToLog(username+" trying to login and the password was incorrect");
                TempData["ErrorUserNameAndPassword"] = "The username or password are incorrect";
                return RedirectToAction("Index", "Home");
            }

            UserAccount userDetailes = new UserAccount() { UserName = username, Password = password };
            var connectionString = string.Format("DataSource={0}", m_ConnectionNadav);
            DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
            string encriptedPassword = EncryptionManager.Encrypt(password, c_passwordKey);
            string loginQuery = "SELECT * FROM tblusers Where Username = @UserName";

            Func<SQLiteCommand, SQLiteDataReader, RedirectToRouteResult> MethodToBeInvoked;
            MethodToBeInvoked = (commad, reader) =>
            {
                while (reader.Read())
                {
                    var encriptionPassword = reader.GetString(2).Trim();
                    var isAdmin = reader.GetString(7);
                    var decriptionis = EncryptionManager.Decrypt(encriptionPassword, c_passwordKey);
                    var userName = reader.GetString(1).Trim();
                    if (decriptionis == password)
                    {
                        Session["UserName"] = username;
                        if(isAdmin == "1")
                        {
                            Session["IsAdmin"] = "1";
                        }
                        else
                        { 
                            Session["IsAdmin"] = "0";
                        }
                        ExceptionLogging.WriteToLog(username + " Logged in");
                        return RedirectToAction("UserHome", "Home");
                    }

                }
                TempData["ErrorUserNameAndPassword"] = "The username or password are incorrect";
                ExceptionLogging.WriteToLog("The username or password are incorrect");
                return RedirectToAction("Index", "Home");

            };
            return databaseConnection.ContactToDataBaseAndExecute(loginQuery, userDetailes, MethodToBeInvoked, "@UserName");
        }

        public ActionResult UserHome()
        {
            if (Session["UserName"] == null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        public ActionResult Register(UserAccount user, string ConfirmPassword, HttpPostedFileBase file)
        {
            user.PictureUser = ConfertToBase64IfPossible(file);
            if(user.PictureUser == null)
            {
                ExceptionLogging.WriteToLog("Wrong picture Format-Register");
                return RedirectToAction("Index", "Home");
            }

            string connectionString = string.Format("DataSource={0}", m_ConnectionNadav);
            DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
            if (user.Password != ConfirmPassword)
            {
                ViewBag.errorConfirm = "The passwords are not the same";
                return RedirectToAction("Index", "Home");
            }
            if (!ValidationRegUserProperty(user))
            {
                ExceptionLogging.WriteToLog("User register property were not validate-Register");
                return RedirectToAction("Index", "Home");
            }

            string encriptedPassword = EncryptionManager.Encrypt(user.Password, c_passwordKey);
            user.Password = encriptedPassword;

            var query = "SELECT * FROM tblusers Where Username = @UserName or Email = @Email";
            Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvokedAfterTheValidation;
            MethodToBeInvokedAfterTheValidation = (commad1, reader1) =>
            {
                ExceptionLogging.WriteToLog(user.UserName + "Register succeed");
                return RedirectToAction("Index", "Home");
            };
            Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvoked;
            MethodToBeInvoked = (commad, reader) =>
            {
                if (reader.Read() == true)
                {
                    ExceptionLogging.WriteToLog("Email or username is alredy been chosen-Register");
                    ViewBag.ExistUsernameoremail = "your email or username is already been chosen";
                    return RedirectToAction("Index", "Home");
                }

                string insetrToDataBaseQuery = "Insert INTO tblusers (FirstName, UserName, Password, LastName, PhoneNumber, Email, PictureUser) VALUES(@FirstName,@UserName,@Password,@LastName,@PhoneNumber,@Email,@PictureUser)";
                return databaseConnection.ContactToDataBaseAndExecute(insetrToDataBaseQuery, user, MethodToBeInvokedAfterTheValidation, "@FirstName", "@Password", "@UserName", "@LastName", "@PhoneNumber", "@Email", "@PictureUser");
            };
            return databaseConnection.ContactToDataBaseAndExecute(query, user, MethodToBeInvoked, "@UserName", "@Email");
        }

        public ActionResult AccountProfile()
        {
            if (Session["UserName"] == null)
            {
                return RedirectToAction("index", "Home");
            }
            var connectionString = string.Format("DataSource={0}", m_ConnectionNadav);
            DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
            string userNameFromSession = (string)Session["UserName"];
            string accountProfileQuery = "SELECT * FROM tblusers Where Username = @UserName";
            UserAccount userDetails = new UserAccount() { UserName = userNameFromSession } ;
            Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvoked;
            MethodToBeInvoked = (commad, reader) =>
            {
                if (reader.Read() == true)
                {
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

        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult AccountProfile(string PhoneNumber, string LastName, string FirstName, string passwordRegister, string Email, HttpPostedFileBase file)
        {
            UserAccount UpdateUser = new UserAccount() { Email = Email, FirstName = FirstName, LastName= LastName, PhoneNumber= PhoneNumber, UserName= (string)Session["UserName"] };
            string connectionString = string.Format("DataSource={0}", m_ConnectionNadav);
            DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
       
        
            bool CheckProprety = ValidationRegUserPropertyForUpdateAccountProfile(UpdateUser);
            if(!CheckProprety)
            {
                ExceptionLogging.WriteToLog("User property were not pass the validation-UpdateProfile");
                return View();
            }
            if (passwordRegister != "*****")
            {
                if (!ValidatePassword(passwordRegister))
                {
                    ExceptionLogging.WriteToLog("User password was not passwod the validation-UpdateProfile");
                    ViewBag.Error = "Error in password";
                    return View();
                }
            }
            if(file!=null)
            {
                if (!IsImage(file))
                {
                    ViewBag.Error = "Error in password";
                    ExceptionLogging.WriteToLog("User picture was not pass the validation-UpdateProfile");
                    return RedirectToAction("AccountProfile", "Home");

                }
               
            }

            string profileQuery1 = "UPDATE tblusers SET FirstName = @FirstName, LastName = @LastName,PhoneNumber=@PhoneNumber,Email=@Email WHERE UserName = @UserName";
            updateUserBasicInformatiom(profileQuery1, databaseConnection, UpdateUser, "@FirstName", "@LastName", "@PhoneNumber", "@Email", "@UserName");
            if (passwordRegister != "*****")
            {
                string profileQuery2 = "UPDATE tblusers SET Password = @Password WHERE UserName = @UserName";
                passwordUpdate(profileQuery2, databaseConnection, passwordRegister, UpdateUser, "@Password", "@UserName");
            }
            if (file != null)
            {
                string profileQuery3 = "UPDATE tblusers SET PictureUser = @PictureUser WHERE UserName = @UserName";
                fileUpdating(profileQuery3, databaseConnection, file, UpdateUser, "@PictureUser", "@UserName");
            }
         
            ExceptionLogging.WriteToLog(UpdateUser.UserName+ " Succeed to update his profile-UpdateProfile ");
            return RedirectToAction("AccountProfile", "Home");
        }

        public ActionResult HomePageForum()
        {
            if (Session["UserName"] == null)
            {
                return RedirectToAction("index", "Home");
            }
            return View();
        }
        public ActionResult CarTrade()
        {
            if (Session["UserName"] == null)
            {
                return RedirectToAction("Index", "Home");
            }

            string connectionString = string.Format("DataSource={0}", m_ConnectionNadav);
            DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
            List<CarTrade> CarForumObjects = new List<CarTrade>();
            var query = "SELECT * FROM PublishCars";
            Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvoked;
            MethodToBeInvoked = (commad, reader) =>
            {

                while (reader.Read() == true)
                {
                    CarTrade CarToSee = new CarTrade();
                    CarToSee.UserName = reader.GetString(0).Trim();
                    CarToSee.Year = reader.GetString(1).Trim();
                    CarToSee.UniqueID = int.Parse(reader.GetString(2).Trim());
                    CarToSee.EngineCapacity = reader.GetString(3).Trim();
                    CarToSee.Gear = reader.GetString(4).Trim();
                    CarToSee.Color = reader.GetString(5).Trim();
                    CarToSee.Price = int.Parse(reader.GetString(6).Trim());
                    CarToSee.Picture = reader.GetString(7).Trim();
                    CarToSee.Model = reader.GetString(8).Trim();
                    CarToSee.PostID = reader.GetInt32(9);
                    CarForumObjects.Add(CarToSee);                  
                }
                ViewBag.ListOfCarsTrade = CarForumObjects;
                return View();
            };
            databaseConnection.ContactToDataBaseAndExecute(query, null, MethodToBeInvoked);
            UserAccount user = new UserAccount();
            user.UserName = (string)Session["UserName"];
            query = "SELECT * FROM tblusers WHERE UserName = @UserName";
            MethodToBeInvoked = (commad, reader) =>
            {

                if (reader.Read() == true)
                {
                    user.Amount = int.Parse(reader.GetString(8).Trim());
                }
                ViewBag.Amount = user.Amount;
                return View();
            };

            return databaseConnection.ContactToDataBaseAndExecute(query, user, MethodToBeInvoked, "@UserName");
        }

        public ActionResult Information()
        {
            if (Session["UserName"] == null)
            {
                return RedirectToAction("Index", "Home");
            }
            UserAccount user = new UserAccount() { UserName = (string)Session["UserName"] };
            string connectionString = string.Format("DataSource={0}", m_ConnectionNadav);
            DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
            List<CarForSell> carForInfo = new List<CarForSell>();
            string query = @"select A.UserName,A.UniqueID,B.Year,B.EngineCapacity,B.Gear,B.Color,B.Price,B.Picture,B.Model 
                            from userscars as A
                            join carforsell as B
                            where A.carID = B.carID And A.UserName = @UserName 
                            Union
                            select UserName,UniqueID,Year,EngineCapacity,Gear,Color,Price,Picture,Model
                            from userscars as u2
                            where carid = 0 And UserName = @UserName and not exists(select UniqueID from PublishCars p where p.UniqueID = u2.UniqueID)";
            Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvoked;
            MethodToBeInvoked = (commad, reader) =>
            {

                while (reader.Read() == true)
                {
                    CarForSell car = new CarForSell();
                    car.Year = reader.GetString(2).Trim();
                    car.EngineCapacity = reader.GetString(3).Trim();
                    car.Gear = reader.GetString(4).Trim();
                    car.Color = reader.GetString(5).Trim();
                    car.Picture = reader.GetString(7).Trim();
                    car.Model = reader.GetString(8).Trim();
                    carForInfo.Add(car);
                }
                ViewBag.CarsDetailes = carForInfo;
                return View();
            };
            databaseConnection.ContactToDataBaseAndExecute(query, user, MethodToBeInvoked, "@UserName");
            query = "SELECT * FROM tblusers WHERE UserName = @UserName";
            MethodToBeInvoked = (commad, reader) =>
            {

                if (reader.Read() == true)
                {
                    user.Amount = int.Parse(reader.GetString(8).Trim());
                }
                ViewBag.Amount = user.Amount;
                return View();
            };
            return databaseConnection.ContactToDataBaseAndExecute(query, user, MethodToBeInvoked, "@UserName");
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult CarBuyLogic(string CarID)
        {
      
            string connectionString = string.Format("DataSource={0}", m_ConnectionNadav);
            DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
            var query = "SELECT * FROM CarForSell WHERE CarID = @CarID";
            CarForSell carToLoad = new CarForSell() { CarID = CarID };
            Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvoked;
            MethodToBeInvoked = (commad, reader) =>
            {
                if (reader.Read() == true)
                {
                    carToLoad.Price = int.Parse(reader.GetString(4));
                    carToLoad.Inventory = int.Parse(reader.GetString(7));
                }
                return RedirectToAction("Index", "Home");

            };
            databaseConnection.ContactToDataBaseAndExecute(query, carToLoad, MethodToBeInvoked, "@CarID");
            query = "SELECT * FROM tblusers WHERE UserName = @UserName";
            UserAccount userToLoad = new UserAccount() { UserName = (string)Session["UserName"] };
            MethodToBeInvoked = (commad, reader) =>
            {
                if (reader.Read() == true)
                {

                    userToLoad.Amount = int.Parse(reader.GetString(8));
                }
                return RedirectToAction("Index", "Home");
            };
            databaseConnection.ContactToDataBaseAndExecute(query, userToLoad, MethodToBeInvoked, "@UserName");

            if(!(userToLoad.Amount >= carToLoad.Price))
            {
                ExceptionLogging.WriteToLog(userToLoad.UserName + "Trying to buy car and don't have enough money");
                return RedirectToAction("CarSellCompany", "Home");
            }

            userToLoad.Amount -= carToLoad.Price;
            carToLoad.Inventory--;
            query = "UPDATE tblusers SET Amount = @Amount WHERE UserName = @UserName";
            MethodToBeInvoked = (commad, reader) =>
            {
                return View();
            };
            databaseConnection.ContactToDataBaseAndExecute(query, userToLoad, MethodToBeInvoked, "@UserName", "@Amount");
            query = "UPDATE CarForSell SET Inventory = @Inventory WHERE CarID = @CarID";
            databaseConnection.ContactToDataBaseAndExecute(query, carToLoad, MethodToBeInvoked, "@Inventory", "@CarID");
            query = "Insert INTO UsersCars (UserName,CarID ) VALUES(@UserName,@CarID)";
            UserNameAndCarID UserCarID = new UserNameAndCarID() { CarID = carToLoad.CarID, UserName= userToLoad.UserName };
            databaseConnection.ContactToDataBaseAndExecute(query, UserCarID, MethodToBeInvoked, "@UserName", "@CarID");
            ExceptionLogging.WriteToLog(userToLoad.UserName + "bought car No=" + carToLoad.CarID+"Sucseed");
            return RedirectToAction("CarSellCompany", "Home");

        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult BuyCarsFromUserLogic2(int PostID)
        {

            string connectionString = string.Format("DataSource={0}", m_ConnectionNadav);
            DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
            var query = "SELECT * FROM PublishCars WHERE PostID = @PostID";
            CarTrade carToLoad = new CarTrade() { PostID = PostID };
    
            Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvoked;
            MethodToBeInvoked = (commad, reader) =>
            {
                if (reader.Read() == true)
                {
                    carToLoad.UserName = reader.GetString(0).Trim();
                    carToLoad.Year = reader.GetString(1).Trim();
                    carToLoad.UniqueID = int.Parse(reader.GetString(2));
                    carToLoad.EngineCapacity = reader.GetString(3).Trim();
                    carToLoad.Gear = reader.GetString(4).Trim();
                    carToLoad.Color = reader.GetString(5).Trim();
                    carToLoad.Price = int.Parse(reader.GetString(6));
                    carToLoad.Picture = reader.GetString(7).Trim();
                    carToLoad.Model = reader.GetString(8).Trim();
                }
                return RedirectToAction("Index", "Home");

            };

            databaseConnection.ContactToDataBaseAndExecute(query, carToLoad, MethodToBeInvoked, "@PostID");
            string NameOfTheSeller = carToLoad.UserName;
            query = "SELECT * FROM tblusers WHERE UserName = @UserName";
            UserAccount userToLoad = new UserAccount() { UserName = (string)Session["UserName"] };
            MethodToBeInvoked = (commad, reader) =>
            {
                if (reader.Read() == true)
                {
                    userToLoad.Amount = int.Parse(reader.GetString(8));
                }
                return RedirectToAction("Index", "Home");

            };
            databaseConnection.ContactToDataBaseAndExecute(query, userToLoad, MethodToBeInvoked, "@UserName");

            bool isUserSucCessedTobuy = UpdatePriceOfUserName((string)Session["UserName"], -carToLoad.Price,(price) => { if (!(userToLoad.Amount >= -price)) return false; return true; });

            if (isUserSucCessedTobuy == false)
            {
                ExceptionLogging.WriteToLog(userToLoad.UserName + "Trying to buy car and don't have enough money");
                return RedirectToAction("CarTrade", "Home");
            }
            UpdatePriceOfUserName(carToLoad.UserName, carToLoad.Price, (price) => true);
            DeleteCarTrade(PostID);
            carToLoad.UserName = (string)Session["UserName"];
            if (carToLoad.UniqueID == 0)
            {
                query = "Insert INTO UsersCars (UserName,Year,EngineCapacity,Gear,Color,Price,Picture,Model ) VALUES(@UserName,@Year,@EngineCapacity,@Gear,@Color,@Price,@Picture,@Model)";
                MethodToBeInvoked = (commad, reader) =>
                {
                    return View();
                };
                databaseConnection.ContactToDataBaseAndExecute(query, carToLoad, MethodToBeInvoked, "@UserName", "@Year", "@EngineCapacity", "@Gear", "@Color", "@Price", "@Picture", "@Model");
            }

            else
            {
                query = "UPDATE UsersCars SET UserName = @UserName WHERE UniqueID = @UniqueID";
                databaseConnection.ContactToDataBaseAndExecute(query, carToLoad, MethodToBeInvoked, "@UserName", "@UniqueID");
            }
            ExceptionLogging.WriteToLog(userToLoad.UserName + "bought car  from"+ NameOfTheSeller + "sucsses-CarTrade");
            return RedirectToAction("CarTrade", "Home");

        }

        public ActionResult CarSellCompany ()
        {
            if (Session["UserName"] == null)
            {
                return RedirectToAction("Index", "Home");
            }

            string connectionString = string.Format("DataSource={0}", m_ConnectionNadav);
            DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
            List<CarForSell> carForSell = new List<CarForSell>();

            var query = "SELECT * FROM CarForSell";
            Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvoked;
            MethodToBeInvoked = (commad, reader) =>
            {

                while (reader.Read() == true)
                {
                    CarForSell car = new CarForSell();
                    car.Year = reader.GetString(0).Trim();
                    car.EngineCapacity = reader.GetString(1).Trim();
                    car.Gear = reader.GetString(2).Trim();
                    car.Color = reader.GetString(3).Trim();
                    car.Price = int.Parse(reader.GetString(4).Trim());
                    car.Picture = reader.GetString(5).Trim();
                    car.Model = reader.GetString(6).Trim();
                    car.Inventory = int.Parse(reader.GetString(7).Trim());
                    car.CarID = reader.GetString(8).Trim();
                    if (car.Inventory > 0)
                    {
                        carForSell.Add(car);
                    }
                    
                }
                ViewBag.ListOfCarToSell = carForSell;
                return View();
            };
            databaseConnection.ContactToDataBaseAndExecute(query, null, MethodToBeInvoked);

            UserAccount user = new UserAccount() { UserName = (string)Session["UserName"] };
            query = "SELECT * FROM tblusers WHERE UserName = @UserName";
            MethodToBeInvoked = (commad, reader) =>
            {

                if (reader.Read() == true)
                {
                    user.Amount = int.Parse(reader.GetString(8).Trim());
                }
                ViewBag.Amount = user.Amount;
                return View();
            };

            return databaseConnection.ContactToDataBaseAndExecute(query, user, MethodToBeInvoked, "@UserName");
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
            string connectionString = string.Format("DataSource={0}", m_ConnectionNadav);
            DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
            List<ForumMessage> messagesOFTheForum = new List<ForumMessage>();
            ForumMessage MessageofTheDataBase = new ForumMessage() { TopicMessage = topic };
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
                    Message.UniqueID = reader.GetInt32(4);

                    messagesOFTheForum.Add(Message);
                }
                ViewBag.ListOfMessages = messagesOFTheForum;
                ViewBag.Topic = topic;
                return View();
            };
            return databaseConnection.ContactToDataBaseAndExecute(query, MessageofTheDataBase, MethodToBeInvoked,"@Topic");
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult PostMessage(string Subject, string Message, string Topic)
        {
            if (messageValidation(Subject, Message) )
            {
                string connectionString = string.Format("DataSource={0}", m_ConnectionNadav);
                DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
                ForumMessage messageToLoad = new ForumMessage() { SubjectMessage = Subject, Message = Message, TopicMessage = Topic, UserName = (string)Session["UserName"]};

                string insetrToDataBaseQuery = "Insert INTO Forum (UserName, Topic, Subject, Message) VALUES(@UserName,@Topic,@Subject,@Message)";
                Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvokedAfterTheValidation;
                MethodToBeInvokedAfterTheValidation = (commad1, reader1) =>
                {
                    ExceptionLogging.WriteToLog((string)Session["UserName"] + " Uploaded message to :"+messageToLoad.TopicMessage);
                    return RedirectToAction("Forum", "Home", new { topic = Topic });
                };
                return databaseConnection.ContactToDataBaseAndExecute(insetrToDataBaseQuery, messageToLoad, MethodToBeInvokedAfterTheValidation, "@UserName", "@Topic", "@Subject", "@Message");
            }
            ExceptionLogging.WriteToLog((string)Session["UserName"] + "Trying to upload Messsage and not pass the validation");
            return RedirectToAction("Forum", "Home", new { topic = Topic });
        }

        public ActionResult SignOut()
        {
            Session.Abandon();
            Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        public ActionResult ControlPanel()
        {
            if (!(Session["UserName"] != null && (string)Session["isAdmin"] == "1"))
            {
                return RedirectToAction("Index", "Home");
            }
            var connectionString = string.Format("DataSource={0}", m_ConnectionNadav);
            DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
            List<UserAccount> users = new List<UserAccount>();
            List<string> usersIsAdmin = new List<string>();
            string loginQuery = "SELECT * FROM tblusers";
            Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvoked;
            MethodToBeInvoked = (commad, reader) =>
            {
                while (reader.Read())
                {
                    UserAccount userDetails = new UserAccount();
                    userDetails.FirstName = reader.GetString(0).Trim();
                    userDetails.UserName = reader.GetString(1).Trim();
                    userDetails.LastName = reader.GetString(3).Trim();
                    userDetails.PhoneNumber = reader.GetString(4).Trim();
                    userDetails.Email = reader.GetString(5).Trim();
                    userDetails.PictureUser = reader.GetString(6).Trim();
                    if (userDetails.UserName != (string)Session["UserName"])
                    {
                        usersIsAdmin.Add(reader.GetString(7));
                        users.Add(userDetails);
                    }
                }
                ViewBag.usersDetails = users;
                ViewBag.usersIsAdmin = usersIsAdmin;
                return View();

            };
            return databaseConnection.ContactToDataBaseAndExecute(loginQuery, null, MethodToBeInvoked);

        }
        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult ControlPanelUpdate(string username,bool checkbox)
        {
            string connectionString = string.Format("DataSource={0}", m_ConnectionNadav);
            DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
            UserAccount UserDetails = new UserAccount() {UserName = username};
            UserAccountImproved User = new UserAccountImproved() { UserDetails = UserDetails };
            User.AdminDetails = (checkbox == true) ? 1 : 0;
            string profileQuriy = "UPDATE tblusers SET isAdmin = @Admin WHERE UserName = @UserName";
            Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvoked;
            MethodToBeInvoked = (commad1, reader1) =>
            {
                ExceptionLogging.WriteToLog((string)Session["UserName"] + " Change Admin porpety to "+username);
                return RedirectToAction("ControlPanel", "Home");
            };
            return databaseConnection.ContactToDataBaseAndExecute(profileQuriy, User, MethodToBeInvoked, "@UserName", "@Admin");
        }

        public ActionResult PostCar()
        {

            if (Session["UserName"] == null)
            {
                return RedirectToAction("Index", "Home");
            }
            string connectionString = string.Format("DataSource={0}", m_ConnectionNadav);
            DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
            string query = @"select A.UserName,A.UniqueID,B.Year,B.EngineCapacity,B.Gear,B.Color,B.Price,B.Picture,B.Model 
                             from userscars as A
                             join carforsell as B
                             where A.carID = B.carID And A.UserName = @UserName  and not exists(select UniqueID from PublishCars p where p.UniqueID = A.UniqueID)
                             Union
                             select UserName,UniqueID,Year,EngineCapacity,Gear,Color,Price,Picture,Model
                             from userscars as u2
                             where carid = 0 And UserName = @UserName and not exists(select UniqueID from PublishCars p where p.UniqueID = u2.UniqueID)";
            UserAccount user = new UserAccount(){UserName=(string)Session["UserName"]};
            List<CarTrade> AllCarsOfUser = new List<CarTrade>();
            Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvoked;
            MethodToBeInvoked = (commad, reader) =>
            {
                while (reader.Read())
                {
                    CarTrade carTrade = new CarTrade();
                    carTrade.UserName = reader.GetString(0).Trim();
                    carTrade.UniqueID = reader.GetInt32(1);
                    carTrade.Year = reader.GetString(2).Trim();
                    carTrade.EngineCapacity = reader.GetString(3).Trim();
                    carTrade.Gear = reader.GetString(4).Trim();
                    carTrade.Color = reader.GetString(5).Trim();
                    carTrade.Picture = reader.GetString(7).Trim();
                    carTrade.Model = reader.GetString(8).Trim();
                    AllCarsOfUser.Add(carTrade);
                }
                ViewBag.CarUsers = AllCarsOfUser;
                return View();
            };
            databaseConnection.ContactToDataBaseAndExecute(query,user,MethodToBeInvoked,"@UserName");
            return View();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult Search(string searchString)
        {
            if (Session["UserName"] == null)
            {
                return RedirectToAction("Index", "Home");
            }
            CarTrade CarForSearchString = new CarTrade();
            string newString = "%"+searchString+"%";
            CarForSearchString.UserName = newString;
            string connectionString = string.Format("DataSource={0}", m_ConnectionNadav);
            DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
            List<CarForSell> carForInfo = new List<CarForSell>();
            List<CarTrade> carTradeInfo = new List<CarTrade>();
            string query = @"select * from CarForSell where Year LIKE @UserName or EngineCapacity LIKE @UserName or Gear LIKE @UserName
                             or Color LIKE @UserName or Price LIKE @UserName  or Model LIKE @UserName";
            Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvoked;
            MethodToBeInvoked = (commad, reader) =>
            {
                while (reader.Read() == true)
                {
                    CarForSell car = new CarForSell();
                    car.Year = reader.GetString(0).Trim();
                    car.EngineCapacity = reader.GetString(1).Trim();
                    car.Gear = reader.GetString(2).Trim();
                    car.Color = reader.GetString(3).Trim();
                    car.Price = int.Parse(reader.GetString(4).Trim());
                    car.Model = reader.GetString(6).Trim();
                    car.Inventory = int.Parse(reader.GetString(7).Trim());
                    car.CarID = reader.GetString(8).Trim();
                    if (car.Inventory > 0)
                    {
                        carForInfo.Add(car);
                    }
                }
                TempData["CarsDetailes"] = carForInfo;
                return View();
            };
            databaseConnection.ContactToDataBaseAndExecute(query, CarForSearchString, MethodToBeInvoked, "@UserName");
            query = @"select * from PublishCars where Year LIKE @UserName or EngineCapacity LIKE @UserName or Gear LIKE @UserName
                       or Color LIKE @UserName or Price LIKE @UserName  or Model LIKE @UserName or UserName LIKE @UserName";
            MethodToBeInvoked = (commad, reader) =>
            { 
                if (reader.Read() == true)
                {
                    CarTrade CarToUploadToScreen = new CarTrade();
                    CarToUploadToScreen.UserName = reader.GetString(0).Trim();
                    CarToUploadToScreen.Year = reader.GetString(1).Trim();
                    CarToUploadToScreen.EngineCapacity = reader.GetString(3).Trim();
                    CarToUploadToScreen.Gear = reader.GetString(4).Trim();
                    CarToUploadToScreen.Color = reader.GetString(5).Trim();
                    CarToUploadToScreen.Price = int.Parse(reader.GetString(6).Trim());
                    CarToUploadToScreen.Model = reader.GetString(8).Trim();
                    CarToUploadToScreen.PostID = reader.GetInt32(9);
                    if(CarToUploadToScreen.UserName!=(string)Session["UserName"])
                    {
                        carTradeInfo.Add(CarToUploadToScreen);
                    }
                }
                TempData["CarsTradeDetails"] = carTradeInfo;
                return RedirectToAction("Search", "Home");
            };
            return databaseConnection.ContactToDataBaseAndExecute(query, CarForSearchString, MethodToBeInvoked, "@UserName");
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult PostCar(string Model,string Color,string Gear,string Year,string EngineCapacity,string Price,HttpPostedFileBase file,string UniqueId)
        {
            string connectionString = string.Format("DataSource={0}", m_ConnectionNadav);
            DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
            Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvokedAfterTheValidation;
            int price = int.Parse(Price);
            int CurrentCarId = -1;
            bool isExistMoreThanOnes = false;
            string QueryForTakingData;
            CarTrade carToPost = new CarTrade() { UniqueID = int.Parse(UniqueId),UserName = (string)Session["UserName"] };
            string QueryForcheckingUniqueID = "select * from PublishCars where UniqueId=@UniqueId and uniqueId!=0";
            MethodToBeInvokedAfterTheValidation = (commad1, reader1) =>
            {
                if (reader1.Read() == true)
                {
                    isExistMoreThanOnes = true;
                }
                return RedirectToAction("CarTrade", "Home");
            };
            databaseConnection.ContactToDataBaseAndExecute(QueryForcheckingUniqueID, carToPost, MethodToBeInvokedAfterTheValidation, "@UniqueID");
            if(isExistMoreThanOnes)
            {
                return RedirectToAction("CarTrade", "Home");
            }
            carToPost.Price = IsDigitsOnly(Price) && price > 0 ? price : -1;
            if (carToPost.Price == -1)
            {
                return RedirectToAction("PostCar", "Home");
            }
            if (carToPost.UniqueID == 0)
            {
                string PossiblePicture = ConfertToBase64IfPossible(file);
                if (PossiblePicture == null)
                {
                    return RedirectToAction("PostCar", "Home");
                }
                carToPost.Picture = PossiblePicture;
                if (IsDigitsOnly(Year) && IsDigitsOnly(EngineCapacity))
                {
                    if (int.Parse(Year) > 0 && int.Parse(EngineCapacity) > 0)
                    {
                        carToPost.Year = Year;
                        carToPost.EngineCapacity = EngineCapacity;
                    }
                    else
                    {
                        return RedirectToAction("PostCar", "Home");
                    }
                }
                if (IsCharacterOnly(Model) && IsCharacterOnly(Color) && IsCharacterOnly(Gear))
                {
                    carToPost.Model = Model;
                    carToPost.Color = Color;
                    carToPost.Gear = Gear;                 
                }
                else
                {
                    return RedirectToAction("PostCar", "Home");
                }
            }
            else
            {
                string AnotherQueryForLogic = "select CarID from UsersCars where UniqueID = @UniqueID";
                MethodToBeInvokedAfterTheValidation = (commad1, reader1) =>
                {
                    if (reader1.Read() == true)
                    {
                        CurrentCarId = int.Parse(reader1.GetString(0));
                    }
                    return RedirectToAction("CarTrade", "Home");
                };
                databaseConnection.ContactToDataBaseAndExecute(AnotherQueryForLogic, carToPost, MethodToBeInvokedAfterTheValidation, "@UniqueID");
                if (CurrentCarId == -1)
                {
                    return RedirectToAction("CarTrade", "Home");
                }
                else if (CurrentCarId != 0)
                {
                    QueryForTakingData = "select B.Year,B.EngineCapacity,B.Gear,B.Color,B.picture,B.Model from UsersCars as A join CarForSell as B where A.uniqueID ==@UniqueID AND A.CarID == B.CarID And A.UserName == @UserName";
                }
                else
                {
                    QueryForTakingData = "select Year,EngineCapacity,Gear,Color,picture,Model from UsersCars where uniqueID ==@UniqueID  And UserName == @UserName";
                }
                MethodToBeInvokedAfterTheValidation = (commad1, reader1) =>
                {
                    if (reader1.Read() == true)
                    {
                        carToPost.Year = reader1.GetString(0);
                        carToPost.EngineCapacity = reader1.GetString(1);
                        carToPost.Gear = reader1.GetString(2);
                        carToPost.Color = reader1.GetString(3);
                        carToPost.Picture = reader1.GetString(4);
                        carToPost.Model = reader1.GetString(5);
                    }
                    return RedirectToAction("CarTrade", "Home");
                };
                databaseConnection.ContactToDataBaseAndExecute(QueryForTakingData, carToPost, MethodToBeInvokedAfterTheValidation, "@UniqueID", "@UserName");
            }
            string insetrToDataBaseQuery = "Insert INTO PublishCars (UserName, Year, UniqueID, EngineCapacity,Gear,Color,Price,Picture,Model) VALUES(@UserName,@Year,@UniqueID,@EngineCapacity,@Gear,@Color,@Price,@Picture,@Model)";
            MethodToBeInvokedAfterTheValidation = (commad1, reader1) =>
            {
                ExceptionLogging.WriteToLog((string)Session["UserName"] + " posted new car for selling");
                return RedirectToAction("CarTrade", "Home");
            };
            return databaseConnection.ContactToDataBaseAndExecute(insetrToDataBaseQuery, carToPost, MethodToBeInvokedAfterTheValidation, "@UserName", "@Year", "@UniqueID", "@EngineCapacity", "@Gear", "@Color", "@Price", "@Picture","@Model");
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult DeleteMessage(string i_Subject, string i_UniqueID)
        {
            string connectionString = string.Format("DataSource={0}", m_ConnectionNadav);
            DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
            ForumMessage messageToDelete = new ForumMessage() { UserName = (string)Session["UserName"], SubjectMessage = i_Subject, UniqueID = int.Parse(i_UniqueID) };
            string deleteFromDataBaseQuery = "DELETE FROM Forum WHERE UserName = @UserName and UniqueID = @UniqueID";
            Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvokedAfterTheValidation;
            MethodToBeInvokedAfterTheValidation = (commad, reader) =>
            {
                ExceptionLogging.WriteToLog((string)Session["UserName"] + " Deleted his own pots message");
                return RedirectToAction("CarTrade", "Home");
            };
            return databaseConnection.ContactToDataBaseAndExecute(deleteFromDataBaseQuery, messageToDelete, MethodToBeInvokedAfterTheValidation, "@UserName", "@UniqueID");
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult DeleteCarTrade(int PostID)
        {
            string connectionString = string.Format("DataSource={0}", m_ConnectionNadav);
            DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
            CarTrade CarToDelete = new CarTrade();
            CarToDelete.PostID = PostID;
            string deleteFromDataBaseQuery = "DELETE FROM PublishCars WHERE PostID = @PostID";
            Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvokedAfterTheValidation;
            MethodToBeInvokedAfterTheValidation = (commad, reader) =>
            {
                ExceptionLogging.WriteToLog((string)Session["UserName"] + " Deleted his own pots car");
                return RedirectToAction("CarTrade", "Home");
            };
            return databaseConnection.ContactToDataBaseAndExecute(deleteFromDataBaseQuery, CarToDelete, MethodToBeInvokedAfterTheValidation, "@PostID");

        }

        public ActionResult Search()
        {
            if (Session["UserName"] == null)
            {
                return RedirectToAction("Index", "Home");
            }
            string connectionString = string.Format("DataSource={0}", m_ConnectionNadav);
            DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
            Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvoked;
            if(TempData["CarsTradeDetails"]==null && TempData["CarsDetailes"]==null)
            {
                return RedirectToAction("UserHome", "Home");
            }

            if (TempData["CarsTradeDetails"] != null)
            {
                ViewBag.CarTradeDetails = TempData["CarsTradeDetails"];
            }
            if (TempData["CarsDetailes"] != null)
            {
                ViewBag.CarDetails = TempData["CarsDetailes"];
            }
            UserAccount user = new UserAccount() { UserName = (string)Session["UserName"] };
           string  query = "SELECT * FROM tblusers WHERE UserName = @UserName";
            MethodToBeInvoked = (commad, reader) =>
            {

                if (reader.Read() == true)
                {
                    user.Amount = int.Parse(reader.GetString(8).Trim());
                }
                ViewBag.Amount = user.Amount;
                return View();
            };
            databaseConnection.ContactToDataBaseAndExecute(query, user, MethodToBeInvoked, "@UserName");
            return View();
        }

        public ActionResult Error()
        {
            return View();
        }
        //Validations + Updates
        private bool UpdatePriceOfUserName(string UserName, int Price, Predicate<int> ConditionToUpdate)
        {
            string connectionString = string.Format("DataSource={0}", m_ConnectionNadav);
            DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
            string query = "SELECT * FROM tblusers WHERE UserName = @UserName";
            UserAccount user = new UserAccount();
            user.UserName = UserName;
            Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvoked;
            MethodToBeInvoked = (commad, reader) =>
            {
                if (reader.Read() == true)// it's mean that I found the user name in the data base
                {
                    user.Amount = int.Parse(reader.GetString(8));
                }
                return RedirectToAction("Index", "Home");

            };
            databaseConnection.ContactToDataBaseAndExecute(query, user, MethodToBeInvoked, "@UserName");
            bool needToUpdate = ConditionToUpdate.Invoke(Price);
            if (needToUpdate)
            {
                user.Amount += Price;

                query = "UPDATE tblusers SET Amount = @Amount WHERE UserName = @UserName";
                MethodToBeInvoked = (commad, reader) =>
                {
                    return View();
                };
                databaseConnection.ContactToDataBaseAndExecute(query, user, MethodToBeInvoked, "@UserName", "@Amount");
            }
            return needToUpdate;
        }

        private void fileUpdating(string profileQuriy, DataBaseUtils databaseConnection, HttpPostedFileBase file, UserAccount updateUser, params string[] i_ParametersOfTheQuery)
        {
            Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvoked;
            MethodToBeInvoked = (commad, reader) =>
            {
                return View();
            };
                    byte[] fileInBytes = new byte[file.ContentLength];
                    using (BinaryReader theReader = new BinaryReader(file.InputStream))
                    {
                        fileInBytes = theReader.ReadBytes(file.ContentLength);
                    }
            string fileAsString = Convert.ToBase64String(fileInBytes);
           updateUser.PictureUser = fileAsString;
         databaseConnection.ContactToDataBaseAndExecute(profileQuriy, updateUser, MethodToBeInvoked, "@PictureUser", "@UserName");
                
           
         

        }
        private void passwordUpdate(string profileQuriy, DataBaseUtils databaseConnection, string passwordRegister, UserAccount updateUser, params string[] i_ParametersOfTheQuery)
        {
         
            Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvoked;
            MethodToBeInvoked = (commad, reader) =>
            {
                return View();
            };
             string encriptedPassword = EncryptionManager.Encrypt(passwordRegister, c_passwordKey);
            updateUser.Password = encriptedPassword;
           databaseConnection.ContactToDataBaseAndExecute(profileQuriy, updateUser, MethodToBeInvoked, i_ParametersOfTheQuery);
                
     
            
         
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
        //Validations Strings
        bool IsDigitsOnly(string str)
        {
            foreach (char c in str)
            {
                if (!(c >= '0' || c <= '9'))
                    return false;
            }

            return true;
        }
        bool IsCharacterOnly(string str)
        {
            foreach (char c in str)
            {
                if (!((c >= 'a' && c <= 'z')|| (c >= 'A' && c <= 'Z')))
                    return false;
            }

            return true;
        }
        private bool messageValidation(string i_Subject, string i_Message)
        {
            bool isValide = false;

            if (i_Subject != "" && i_Message != "")
            {
                if (!(i_Message.Contains("#") || i_Subject.Contains("#")))
                {
                    isValide = true;
                }
            }
            return isValide;
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
        private bool ValidationRegUserProperty(UserAccount i_User)
        {
            if (i_User.FirstName == null || i_User.LastName == null || i_User.Email == null || i_User.Password == null || i_User.PhoneNumber == null || i_User.UserName == null)
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

            if (!IsDigitsOnly(i_User.PhoneNumber))
            {
                return false;
            }
            if(!IsCharacterOnly(i_User.FirstName))
            {
                return false;
            }
            if (!IsCharacterOnly(i_User.LastName))
            {
                return false;
            }



            return true;

        }
        private bool ValidationRegUserPropertyForUpdateAccountProfile(UserAccount i_User)
        {
            if (i_User.FirstName == null || i_User.LastName == null || i_User.Email == null || i_User.PhoneNumber == null )
            {
                return false;
            }
            if (!IsCharacterOnly(i_User.FirstName))
            {
                return false;
            }
            if (!IsCharacterOnly(i_User.LastName))
            {
                return false;
            }
            string emailRegex = @"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$";
            var matchEmail = Regex.Match(i_User.Email, emailRegex, RegexOptions.IgnoreCase);
            if (!matchEmail.Success)
            {
                return false;
            }
            if (!IsDigitsOnly(i_User.PhoneNumber))
            {
                return false;
            }
            return true;


        }
        //Image Functions:
        private bool IsImage(HttpPostedFileBase file)
        {
            if(file==null)
            {
                return false;
            }

            if (!ValidMinimumImageSize(file))
            {
                return false;
            }
            if (!ValidMaximumImageSize(file))
            {
                return false;
            }
            if (!ImageFile(file))
            {
                return false;
            }
            return true;
        }
        private bool ValidMinimumImageSize(HttpPostedFileBase postedFile)
        {
            int ImageMinimumBytes = 512;
            return postedFile.ContentLength > ImageMinimumBytes;
        }
        private bool ValidMaximumImageSize(HttpPostedFileBase postedFile)
        {
            int ImageMaximumBytes = 10 * 1024 * 1024;
            return postedFile.ContentLength <= ImageMaximumBytes;
        }
        private bool ImageFile(HttpPostedFileBase postedFile)
        {
            List<string> ImageMimeTypes = new List<string> { "image/jpg", "image/jpeg", "image/pjpeg", "image/gif", "image/x-png", "image/png" };
            bool flag1=postedFile.FileName.EndsWith(".jpg");
            var contentType = postedFile.ContentType.ToLower();
            if (ImageMimeTypes.All(x => x != contentType))
            {
                return false;
            }
            return true;
        }
        private string ConfertToBase64IfPossible (HttpPostedFileBase file)
        {
            if (file != null && IsImage(file))
            {
                byte[] fileInBytes = new byte[file.ContentLength];
                using (BinaryReader theReader = new BinaryReader(file.InputStream))
                {
                    fileInBytes = theReader.ReadBytes(file.ContentLength);
                }
                return Convert.ToBase64String(fileInBytes);
            }
            return null;
        }
      
    }



}