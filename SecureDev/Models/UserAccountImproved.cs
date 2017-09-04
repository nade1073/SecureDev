using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Vladi2.Models
{
    public class UserAccountImproved
    {
        public UserAccount UserDetails { get; set; }
        public int AdminDetails { get; set; }

        public override string ToString()
        {
            string UserAcoountString = string.Format(UserDetails.ToString() + string.Format(" #Admin {0}", AdminDetails));
            return UserAcoountString;
        }
    }
}