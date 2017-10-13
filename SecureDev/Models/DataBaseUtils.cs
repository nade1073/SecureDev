using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Web;
using System.Data;
using System.Web.Mvc;
using System.Web.Security.AntiXss;
using System.Web.Routing;

namespace Vladi2.Models
{
    public class DataBaseUtils
    {
        //public event ActionResultDelegate MethodToBeInvoked;
        public string ConnectionDirectoryInMyComputer { get; private set; }

        private const string m_NadavServer = @"C:\Users\Nadav\Desktop\SecureDev\SecureDev\Sqlite\db.sqlite";
        private const string m_NetanelServer = @"C:\לימודים HIT\שנה ג סמסטר קיץ\פרוייקט ולדי\SecureDev\Sqlite\db.sqlite";

        private TypeMapFromTypeToDbType m_typeConverter;
        public DataBaseUtils(string i_ConnectionDirectoryInMyComputer)
        {
            ConnectionDirectoryInMyComputer = i_ConnectionDirectoryInMyComputer;
            m_typeConverter = new TypeMapFromTypeToDbType();
        }

   

        //public ActionResult ContactToDataBaseAndExecute
        //    (string i_QueryActionOnDataBase, object i_objectToGetDataFromIt, Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvoked, params string[] i_ParametersOfTheQuery)
        //{
        //    using (var m_dbConnection = new SQLiteConnection(ConnectionDirectoryInMyComputer))
        //    {
        //        m_dbConnection.Open();
        //        using (SQLiteCommand command = new SQLiteCommand(i_QueryActionOnDataBase, m_dbConnection))
        //        {
        //            foreach (string parameter in i_ParametersOfTheQuery)
        //            {
        //                command.Parameters.Add(parameter, m_typeConverter.typeMap[parameter.GetType()]);
        //            }
        //            foreach (string parameter in i_ParametersOfTheQuery)
        //            {
        //                command.Parameters[parameter].Value = matchingParams(parameter, i_objectToGetDataFromIt);
        //            }

        //            using (SQLiteDataReader reader = command.ExecuteReader())
        //            {
        //                   return MethodToBeInvoked(command, reader);
        //            }
        //        }
        //    }

        //}


        public ActionResult ContactToDataBaseAndExecute
           (string i_QueryActionOnDataBase, object i_objectToGetDataFromIt, Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvoked, params string[] i_ParametersOfTheQuery)
        {
            var m_dbConnection = new SQLiteConnection(ConnectionDirectoryInMyComputer);
            ActionResult ReturnValue = new RedirectToRouteResult(new RouteValueDictionary { { "action", "error" },{ "controller", "home" }});

            try
            {
                m_dbConnection.Open();
                SQLiteCommand command = new SQLiteCommand(i_QueryActionOnDataBase, m_dbConnection);
                try
                {
                    foreach (string parameter in i_ParametersOfTheQuery)
                    {
                        command.Parameters.Add(parameter, m_typeConverter.typeMap[parameter.GetType()]);
                    }
                    foreach (string parameter in i_ParametersOfTheQuery)
                    {
                        command.Parameters[parameter].Value = matchingParams(parameter, i_objectToGetDataFromIt);
                    }

                    SQLiteDataReader reader = command.ExecuteReader();
                    ReturnValue = MethodToBeInvoked(command, reader);
                  
                }

                catch (Exception ex)
                {
                    ExceptionLogging.WriteToLog(ex);
                }
            }

            catch (Exception ex)
            {
                ExceptionLogging.WriteToLog(ex);
            }

            finally
            {
                m_dbConnection.Dispose();
            }
            return ReturnValue;

        }

        public bool CheckingInformation(string Tabel,string ClumnName, string UniqueData, object i_ObjectToCompare)
        {
            List<DataBaseObject> RowFromDatabase = new List<DataBaseObject>();
            var query = string.Format("SELECT * FROM {0} Where {1} = @UniqueData", Tabel, ClumnName);
            using (var m_dbConnection = new SQLiteConnection(ConnectionDirectoryInMyComputer))
            {
                m_dbConnection.Open();
                using (SQLiteCommand command = new SQLiteCommand(query, m_dbConnection))
                {
                    command.Parameters.Add("@UniqueData", m_typeConverter.typeMap[UniqueData.GetType()]);
                    command.Parameters["@UniqueData"].Value = UniqueData;

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        for(int i=0; i<reader.FieldCount;i++)
                        {
                            RowFromDatabase.Add(new DataBaseObject(reader.GetName(i), reader.GetString(i)));
                        }
                    }
                }
            }
            bool isAllGood = true;
            foreach(DataBaseObject obj in RowFromDatabase)
            {
                if(isAllGood)
                {
                    isAllGood = matchingParamsForCheckingInformation(obj.ColumnName, obj.ColumnValue, i_ObjectToCompare);
                }
                else
                {
                    break;
                }
            }
            return isAllGood;


        }

        private string matchingParams(string i_parameterForMatcing, object i_ObjectParameters)
        {
            string[] words = i_parameterForMatcing.Split('@');
            string wordAfterSplitting = words[1];
            string UserData = AntiXssEncoder.HtmlEncode(i_ObjectParameters.ToString(),false);
            string [] UserDataWords = i_ObjectParameters.ToString().Split(new string[] { wordAfterSplitting }, StringSplitOptions.None);
            string[] anotherAfterStringOperation = UserDataWords[1].Split('#');
            string TheStringToBeReturn = anotherAfterStringOperation[0].Trim();
            return TheStringToBeReturn;

        }

        private bool matchingParamsForCheckingInformation(string i_parameterForMatcing, string TheValue, object i_ObjectParameters)
        {
            //string[] words = i_parameterForMatcing.Split('@');
            string wordAfterSplitting = i_parameterForMatcing;
            string UserData = i_ObjectParameters.ToString();
            string[] UserDataWords = i_ObjectParameters.ToString().Split(new string[] { wordAfterSplitting }, StringSplitOptions.None);
            string[] anotherAfterStringOperation = UserDataWords[1].Split('#');
            string TheStringToBeReturn = anotherAfterStringOperation[0].Trim();
            return TheStringToBeReturn == TheValue;

        }
    }
}