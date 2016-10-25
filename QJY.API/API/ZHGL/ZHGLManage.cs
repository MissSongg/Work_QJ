using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using FastReflectionLib;
using QJY.API;
using Newtonsoft.Json;
using QJY.Data;
using System.Data;
using Newtonsoft.Json.Linq;
using Senparc.Weixin.QY.Entities;
using Newtonsoft.Json.Converters;

namespace QJY.API
{
    public class ZHGLManage : IWsService
    {
        public void ProcessRequest(HttpContext context, ref Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            MethodInfo methodInfo = typeof(ZHGLManage).GetMethod(msg.Action.ToUpper());
            ZHGLManage model = new ZHGLManage();
            methodInfo.FastInvoke(model, new object[] { context, msg, P1, P2, UserInfo.User.UserName });
        }


      
    }
}