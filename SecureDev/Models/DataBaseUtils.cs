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
        public event ActionResultDelegate MethodToBeInvoked;
        public string ConnectionDirectoryInMyComputer { get; private set; }
        public DataBaseUtils(string i_ConnectionDirectoryInMyComputer)
        {
            ConnectionDirectoryInMyComputer = i_ConnectionDirectoryInMyComputer;
        }
        public void ContactToDataBase
            (string i_QueryActionOnDataBase, UserAccount i_User, Func<SQLiteCommand, SQLiteDataReader, RedirectToRouteResult> MethodToBeInvoked, params string[] i_ParametersOfTheQuery)
        {
            using (var m_dbConnection = new SQLiteConnection(ConnectionDirectoryInMyComputer))
            {
                m_dbConnection.Open();
                using (SQLiteCommand command = new SQLiteCommand(i_QueryActionOnDataBase, m_dbConnection))
                {
                    foreach (string parameter in i_ParametersOfTheQuery)
                    {
                        command.Parameters.Add(parameter, TypeMapFromTypeToDbType.typeMap[parameter.GetType()]);
                    }
                    foreach (string parameter in i_ParametersOfTheQuery)
                    {
                        command.Parameters[parameter].Value = matchingParams(parameter, i_User);
                    }

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (MethodToBeInvoked != null)
                        {
                            MethodToBeInvoked(command, reader);
                        }
                    }
                }
            }
        }
        public void ContactToDataBase
            (string i_QueryActionOnDataBase, UserAccount i_User, Func<SQLiteCommand, SQLiteDataReader, ViewResult> MethodToBeInvoked, params string[] i_ParametersOfTheQuery)
        {
            using (var m_dbConnection = new SQLiteConnection(ConnectionDirectoryInMyComputer))
            {
                m_dbConnection.Open();
                using (SQLiteCommand command = new SQLiteCommand(i_QueryActionOnDataBase, m_dbConnection))
                {
                    foreach (string parameter in i_ParametersOfTheQuery)
                    {
                        command.Parameters.Add(parameter, TypeMapFromTypeToDbType.typeMap[parameter.GetType()]);
                    }
                    foreach (string parameter in i_ParametersOfTheQuery)
                    {
                        command.Parameters[parameter].Value = matchingParams(parameter, i_User);
                    }

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (MethodToBeInvoked != null)
                        {
                            MethodToBeInvoked(command, reader);
                        }
                    }
                }
            }

        }

        private string matchingParams(string i_parameterForMatcing, UserAccount i_User)
        {
            string[] words = i_parameterForMatcing.Split('@');
            string wordAfterSplitting = words[1];
            string UserData = i_User.ToString();
            string [] UserDataWords = i_User.ToString().Split(new string[] { wordAfterSplitting }, StringSplitOptions.None);
            string[] anotherAfterStringOperation = UserDataWords[1].Split(' ');
            return anotherAfterStringOperation[1];
        }
    }
}