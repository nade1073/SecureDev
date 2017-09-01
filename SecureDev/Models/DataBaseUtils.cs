using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Web;
using System.Data;
using System.Web.Mvc;

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

        public ActionResult ContactToDataBaseAndExecute
            (string i_QueryActionOnDataBase, object i_objectToGetDataFromIt, Func<SQLiteCommand, SQLiteDataReader, ActionResult> MethodToBeInvoked, params string[] i_ParametersOfTheQuery)
        {
            using (var m_dbConnection = new SQLiteConnection(ConnectionDirectoryInMyComputer))
            {
                m_dbConnection.Open();
                using (SQLiteCommand command = new SQLiteCommand(i_QueryActionOnDataBase, m_dbConnection))
                {
                    foreach (string parameter in i_ParametersOfTheQuery)
                    {
                        command.Parameters.Add(parameter, m_typeConverter.typeMap[parameter.GetType()]);
                    }
                    foreach (string parameter in i_ParametersOfTheQuery)
                    {
                        command.Parameters[parameter].Value = matchingParams(parameter, i_objectToGetDataFromIt);
                    }

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                           return MethodToBeInvoked(command, reader);
                    }
                }
            }

        }

        private string matchingParams(string i_parameterForMatcing, object i_ObjectParameters)
        {
            string[] words = i_parameterForMatcing.Split('@');
            string wordAfterSplitting = words[1];
            string UserData = i_ObjectParameters.ToString();
            string [] UserDataWords = i_ObjectParameters.ToString().Split(new string[] { wordAfterSplitting }, StringSplitOptions.None);
            string[] anotherAfterStringOperation = UserDataWords[1].Split('#');
            string TheStringToBeReturn = anotherAfterStringOperation[0].Trim();
            return TheStringToBeReturn;

        }
    }
}