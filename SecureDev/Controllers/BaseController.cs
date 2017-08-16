using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Vladi2.Controllers
{
    //This attributs cancels the input validation provided out of the box by mvc framework - for study purposes only
    //in real life - dont use this attribute in most of the cases.
    [ValidateInput(false)]
    public class BaseController : Controller
    {
        // create base methods here - also possible to override onActionExecuting etc.
        // this is not must
        public BaseController()
        {
          
        }
        
    }
}