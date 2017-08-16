using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Vladi2.Models;

namespace Vladi2.Controllers
{
    public class HomeController : BaseController
    {
        //entry point for main page as determined in the route config
        public ActionResult Index(string validationError = null)
        {
            var vm = new homeVM() { data = validationError };
            return View(vm);
        }
        //GET: home/login 
        public ActionResult Login(string username,string password)
        {
            //the path is absolute and should be changed.
            var connectionString = string.Format("DataSource={0}", "C:\\Users\\user\\Documents\\Visual Studio 2015\\Projects\\SecureDev\\SecureDev\\Sqlite\\db.sqlite");

            using (var m_dbConnection = new SQLiteConnection(connectionString))
            {
                m_dbConnection.Open();
                using (SQLiteCommand command = new SQLiteCommand("SELECT * FROM tblusers Where username = '" +username+"' and password = '"+ password + "'", m_dbConnection))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        //if we got here - the select succeded , the user exist in db - redirect to userHome page
                        var userName = reader.GetString(1).Trim();
                        
                      return  RedirectToAction("UserHome","Home",new { userName });
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
            var vm = new homeVM {data = userName};
            return View(vm);
        }   
    }
}