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
            var connectionString = string.Format("DataSource={0}", m_ConnectionReznik);
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
                        return RedirectToAction("UserHome", "Home");
                    }

                }
                TempData["ErrorUserNameAndPassword"] = "The username or password are incorrect";
                return RedirectToAction("Index", "Home");

            };
            return databaseConnection.ContactToDataBaseAndExecute(loginQuery, userDetailes, MethodToBeInvoked, "@UserName");
        }

        public ActionResult CarTrade()
        {
            if (Session["UserName"] == null)
            {
                return RedirectToAction("Index", "Home");
            }

            string connectionString = string.Format("DataSource={0}", m_ConnectionReznik);
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
            return databaseConnection.ContactToDataBaseAndExecute(query, null, MethodToBeInvoked);
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
            string connectionString = string.Format("DataSource={0}", m_ConnectionReznik);
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
 

        public ActionResult Information()
        {
            UserAccount user = new UserAccount();
            user.UserName = (string)Session["UserName"];
            if (user.UserName == null)
            {
                return RedirectToAction("Index", "Home");
            }

            string connectionString = string.Format("DataSource={0}", m_ConnectionReznik);
            DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
            List<CarForSell> carForInfo = new List<CarForSell>();

            string query = @"select A.UserName,A.UniqueID,B.Year,B.EngineCapacity,B.Gear,B.Color,B.Price,B.Picture,B.Model 
from userscars as A
join carforsell as B
 where A.carID = B.carID And A.UserName = @UserName  and not exists(select UniqueID from PublishCars p where p.UniqueID = A.UniqueID)
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
            var connectionString = string.Format("DataSource={0}", m_ConnectionReznik);
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
            string connectionString = string.Format("DataSource={0}", m_ConnectionReznik);
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

        [HttpPost]
        public ActionResult CarBuyLogic(string CarID)
        {
            //  Take all data about the carID  V
            //Take all data about the user V
            //Check if the user can buy by amountofcars && amout left users
            //down by 1 amountcars and price from user
            // update the table
            // update the table UsersCar with the that the users buy
            string connectionString = string.Format("DataSource={0}", m_ConnectionReznik);
            DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
            var query = "SELECT * FROM CarForSell WHERE CarID = @CarID";
            CarForSell carToLoad = new CarForSell();
            carToLoad.CarID = CarID;
            Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvoked;
            MethodToBeInvoked = (commad, reader) =>
            {
                if (reader.Read() == true)// it's mean that I found the user name in the data base
                {
                    //if we got here - the select succeded , the user exist in db - redirect to userHome page
                    carToLoad.Price = int.Parse(reader.GetString(4));
                    carToLoad.Inventory = int.Parse(reader.GetString(7));
                }
                return RedirectToAction("Index", "Home");

            };
            databaseConnection.ContactToDataBaseAndExecute(query, carToLoad, MethodToBeInvoked, "@CarID");
            query = "SELECT * FROM tblusers WHERE UserName = @UserName";
            UserAccount userToLoad = new UserAccount();
            userToLoad.UserName = (string)Session["UserName"];
            MethodToBeInvoked = (commad, reader) =>
            {
                if (reader.Read() == true)// it's mean that I found the user name in the data base
                {
                    userToLoad.Amount = int.Parse(reader.GetString(8));
                }
                return RedirectToAction("Index", "Home");

            };
            databaseConnection.ContactToDataBaseAndExecute(query, userToLoad, MethodToBeInvoked, "@UserName");

            if(!(userToLoad.Amount >= carToLoad.Price))
            {
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
            UserNameAndCarID UserCarID = new UserNameAndCarID();
            UserCarID.CarID = carToLoad.CarID;
            UserCarID.UserName = userToLoad.UserName;
            databaseConnection.ContactToDataBaseAndExecute(query, UserCarID, MethodToBeInvoked, "@UserName", "@CarID");
            return RedirectToAction("CarSellCompany", "Home");

        }
        [HttpPost]
        public ActionResult BuyCarsFromUserLogic2(int PostID)
        {
            //upload Post Details to var  V
            //get username from the post and take the data  V
            //get username from the session and take the data  V
            //Check if the user can buy by amount left from tblusers V
            //Delete  Post Details  from dataBase V
            //down the price from amount from the user in current session V
            // add the price to the amount to the user that the car bought from V
            // update the table V
            //check  if UniqueID is null if yes add new row to table UsersCars with CarID = 0 else just update the userName of the Table usersCars
            //
            string connectionString = string.Format("DataSource={0}", m_ConnectionReznik);
            DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
            var query = "SELECT * FROM PublishCars WHERE PostID = @PostID";
            CarTrade carToLoad = new CarTrade();
            carToLoad.PostID = PostID;
            Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvoked;
            MethodToBeInvoked = (commad, reader) =>
            {
                if (reader.Read() == true)// it's mean that I found the user name in the data base
                {
                    //if we got here - the select succeded , the user exist in db - redirect to userHome page
                    
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
            query = "SELECT * FROM tblusers WHERE UserName = @UserName";
            UserAccount userToLoad = new UserAccount();
            userToLoad.UserName = (string)Session["UserName"];
            MethodToBeInvoked = (commad, reader) =>
            {
                if (reader.Read() == true)// it's mean that I found the user name in the data base
                {
                    userToLoad.Amount = int.Parse(reader.GetString(8));
                }
                return RedirectToAction("Index", "Home");

            };
            databaseConnection.ContactToDataBaseAndExecute(query, userToLoad, MethodToBeInvoked, "@UserName");

            bool isUserSucCessedTobuy =  UpdatePriceOfUserName((string)Session["UserName"], -carToLoad.Price,
                (price) => { if (!(userToLoad.Amount >= -price)) return false; return true; });

            if (isUserSucCessedTobuy == false)
            {
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
            return RedirectToAction("CarTrade", "Home");

        }

        private bool UpdatePriceOfUserName(string UserName, int Price , Predicate<int> ConditionToUpdate)
        {
            string connectionString = string.Format("DataSource={0}", m_ConnectionReznik);
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

        public ActionResult CarSellCompany ()
        {
            if (Session["UserName"] == null)
            {
                return RedirectToAction("Index", "Home");
            }

            string connectionString = string.Format("DataSource={0}", m_ConnectionReznik);
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
            return databaseConnection.ContactToDataBaseAndExecute(query, null, MethodToBeInvoked);
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

            string connectionString = string.Format("DataSource={0}", m_ConnectionReznik);
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
                    Message.UniqueID = reader.GetInt32(4);

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

                string connectionString = string.Format("DataSource={0}", m_ConnectionReznik);
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
            string connectionString = string.Format("DataSource={0}", m_ConnectionReznik);
            DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
            UserAccount UserDetails = new UserAccount();
            UserDetails.UserName = username;
            UserAccountImproved User = new UserAccountImproved();
            User.UserDetails = UserDetails;
            User.AdminDetails = (checkbox == true) ? 1 : 0;
            string profileQuriy = "UPDATE tblusers SET isAdmin = @Admin WHERE UserName = @UserName";
            Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvoked;
            MethodToBeInvoked = (commad1, reader1) =>
            {
                return RedirectToAction("ControlPanel", "Home");
            };


            return databaseConnection.ContactToDataBaseAndExecute(profileQuriy, User, MethodToBeInvoked, "@UserName", "@Admin");
        }

        public ActionResult ControlPanel()
        {
            if (!(Session["UserName"] != null && (string)Session["isAdmin"] == "1"))
            {
                return RedirectToAction("Index", "Home");

            }

            UserAccount userDetailes = new UserAccount();
            var connectionString = string.Format("DataSource={0}", m_ConnectionReznik);
                DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
                List<UserAccount> users = new List<UserAccount>();
                List<string> usersIsAdmin = new List<string>();
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

                        usersIsAdmin.Add(reader.GetString(7));
                        if (userDetailes.UserName != (string)Session["UserName"])
                        {
                            users.Add(userDetails);
                        }

                    }
                    ViewBag.usersDetails = users;
                    ViewBag.usersIsAdmin = usersIsAdmin;
                    return View();

                };
        
            return databaseConnection.ContactToDataBaseAndExecute(loginQuery, userDetailes, MethodToBeInvoked);
            
        }
        public ActionResult PostCar()
        {

            if (Session["UserName"] == null)
            {
                return RedirectToAction("Index", "Home");
            }
            string connectionString = string.Format("DataSource={0}", m_ConnectionReznik);
            DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
            string query= @"select A.UserName,A.UniqueID,B.Year,B.EngineCapacity,B.Gear,B.Color,B.Price,B.Picture,B.Model 
from userscars as A
join carforsell as B
 where A.carID = B.carID And A.UserName = @UserName  and not exists(select UniqueID from PublishCars p where p.UniqueID = A.UniqueID)
 Union
 select UserName,UniqueID,Year,EngineCapacity,Gear,Color,Price,Picture,Model
 from userscars as u2
 where carid = 0 And UserName = @UserName and not exists(select UniqueID from PublishCars p where p.UniqueID = u2.UniqueID)";
            UserAccount user = new UserAccount();
            user.UserName = (string)Session["UserName"];
            List<CarTrade> AllCarsOfUser = new List<CarTrade>();
            Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvoked;
            MethodToBeInvoked = (commad, reader) =>
            {
                while (reader.Read())
                {
                  
                    CarTrade userDetails = new CarTrade();
                    userDetails.UserName = reader.GetString(0).Trim();
                    userDetails.UniqueID = reader.GetInt32(1);
                    userDetails.Year = reader.GetString(2).Trim();
                    userDetails.EngineCapacity = reader.GetString(3).Trim();
                    userDetails.Gear = reader.GetString(4).Trim();
                    userDetails.Color = reader.GetString(5).Trim();
                    userDetails.Picture = reader.GetString(7).Trim();
                    userDetails.Model = reader.GetString(8).Trim();
                    AllCarsOfUser.Add(userDetails);
                }
                ViewBag.CarUsers = AllCarsOfUser;
                return View();
               
            };
            databaseConnection.ContactToDataBaseAndExecute(query, user, MethodToBeInvoked, "@UserName");
            return View();

        }

        [HttpPost]
        public ActionResult PostCar(string Model,string Color,string Gear,string Year,string EngineCapacity,string Price,HttpPostedFileBase file,string UniqueId)
        {
            //Picture is pitcure and convert base64
            //price not minus
            //Bdikot ota mechonit
            string connectionString = string.Format("DataSource={0}", m_ConnectionReznik);
            DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
            Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvokedAfterTheValidation;
            int price = int.Parse(Price);
            int CurrentCarId = -1;
            bool isExistMoreThanOnes = false;
            string QueryForTakingData;
            CarTrade carToPost = new CarTrade();
            carToPost.UniqueID = int.Parse(UniqueId);
            carToPost.UserName = (string)Session["UserName"];
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
                    if (reader1.Read() == true)// it's mean that I found the user name in the data base
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
                return RedirectToAction("CarTrade", "Home");
            };
            return databaseConnection.ContactToDataBaseAndExecute(insetrToDataBaseQuery, carToPost, MethodToBeInvokedAfterTheValidation, "@UserName", "@Year", "@UniqueID", "@EngineCapacity", "@Gear", "@Color", "@Price", "@Picture","@Model");
        }

        [HttpPost]
        public ActionResult DeleteMessage(string i_Subject, string i_UniqueID)
        {
            string connectionString = string.Format("DataSource={0}", m_ConnectionReznik);
            DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
            ForumMessage messageToDelete = new ForumMessage();
            messageToDelete.UserName = (string)Session["UserName"];
            messageToDelete.SubjectMessage = i_Subject;
            messageToDelete.UniqueID = int.Parse(i_UniqueID);
            string deleteFromDataBaseQuery = "DELETE FROM Forum WHERE UserName = @UserName and UniqueID = @UniqueID";
            Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvokedAfterTheValidation;
            MethodToBeInvokedAfterTheValidation = (commad, reader) =>
            {
                return RedirectToAction("CarTrade", "Home");
            };
            return databaseConnection.ContactToDataBaseAndExecute(deleteFromDataBaseQuery, messageToDelete, MethodToBeInvokedAfterTheValidation, "@UserName", "@UniqueID");
        }
        [HttpPost]
        public ActionResult DeleteCarTrade(int PostID)
        {
            string connectionString = string.Format("DataSource={0}", m_ConnectionReznik);
            DataBaseUtils databaseConnection = new DataBaseUtils(connectionString);
            CarTrade CarToDelete = new CarTrade();
            CarToDelete.PostID = PostID;
            string deleteFromDataBaseQuery = "DELETE FROM PublishCars WHERE PostID = @PostID";
            Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvokedAfterTheValidation;
            MethodToBeInvokedAfterTheValidation = (commad, reader) =>
            {
                return RedirectToAction("CarTrade", "Home");
            };
            return databaseConnection.ContactToDataBaseAndExecute(deleteFromDataBaseQuery, CarToDelete, MethodToBeInvokedAfterTheValidation, "@PostID");

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

    //pivatep PostCar(string Model, string Color, string Gear, string Year, string EngineCapacity, string Price, HttpPostedFileBase Picture, string UniqueId)
    //{

    //}


}