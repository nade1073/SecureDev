using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Vladi2.Models
{
    public class UserNameAndCarID
    {
        public string UserName { get; set; }
        public string CarID { get; set; }

        public override string ToString()
        {
            return string.Format(
@"#UserName {0}
#CarID {1}", UserName, CarID);
        }
    }
}