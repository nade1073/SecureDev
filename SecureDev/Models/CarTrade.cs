using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Vladi2.Models
{
    public class CarTrade
    {
        public string UserName { get; set; }

        public string Year { get; set; }

        public int UniqueID { get; set; }

        public string EngineCapacity { get; set; }

        public string Gear { get; set; }

        public string Color { get; set; }

        public int Price { get; set; }

        public string Picture { get; set; }

        public string Model { get; set; }

        public int PostID { get; set; }

        public override string ToString()
        {
            return string.Format(
@"#Price {0} 
#Year {1} 
#Color {2} 
#Gear {3} 
#EngineCapacity {4} 
#Picture {5}
#Model {6}
#UserName {7}
#PostID {8} 
#UniqueID {9}", this.Price, this.Year, this.Color, this.Gear, this.EngineCapacity, this.Picture, this.Model, this.UserName, this.PostID, this.UniqueID);
        }
    }
}