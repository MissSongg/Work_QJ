using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QjySaaSWeb
{
    public class HtmlHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
     

        }



        public bool IsReusable
        {
            get { return false; }
        }
    }
}