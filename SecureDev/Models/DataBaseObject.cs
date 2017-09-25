using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Vladi2.Models
{
    public class DataBaseObject
    {
        public DataBaseObject(string i_ColumnName, string i_ColumnValue)
        {
            ColumnName = i_ColumnName;
            ColumnValue = i_ColumnValue;
        }
        public string ColumnName { get; set; }
        public string ColumnValue { get; set; }
    }
}