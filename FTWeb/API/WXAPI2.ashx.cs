using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Practices.Unity;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System.Web.SessionState;
using System.IO;
using System.Xml;
using Senparc.Weixin;
using Senparc.Weixin.Entities;
using Senparc.Weixin.QY.AdvancedAPIs;
using QJY.API;
using QJY.Data;
using System.Net;
using System.Text;
using System.Configuration;

namespace QjySaaSWeb.API
{
    /// <summary>
    /// 微信公众号接口
    /// </summary>
    public class WXAPI2 : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/plain";
            context.Response.AddHeader("pragma", "no-cache");
            context.Response.AddHeader("cache-control", "");
            context.Response.CacheControl = "no-cache";
            string strAction = context.Request["Action"] ?? "";
            string P1 = context.Request["P1"] ?? "";
            string P2 = context.Request["P2"] ?? "";
            string P3 = context.Request["P3"] ?? "";
            string strIP = CommonHelp.getIP(context);

            Msg_Result Model = new Msg_Result() { Action = strAction.ToUpper(), ErrorMsg = "" };

            if (!string.IsNullOrEmpty(strAction))
            {
                var acs = Model.Action.Split('_');
                var container = ServiceContainerV2.Current().Resolve<IWsService2>(acs[0].ToUpper());
                Model.Action = Model.Action.Substring(acs[0].Length + 1);
                int comid = 10334;

                SZHL_YX_USER UserInfo = new SZHL_YX_USER();
                if (context.Request.Cookies["wxuser"] != null && context.Request.Cookies["wxuser"].ToString() != "")
                {
                    string code = context.Request.Cookies["wxuser"].ToString();
                    //根据code找到用户
                    var usr = new SZHL_YX_USERB().GetEntity(p => p.ComId == comid && p.code == code);
                    if (usr != null)
                    {
                        UserInfo = usr;
                    }

                }
                
                container.ProcessRequest(context, ref Model, comid, P1.TrimEnd(), P2.TrimEnd(), UserInfo);
            }

            IsoDateTimeConverter timeConverter = new IsoDateTimeConverter();
            timeConverter.DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
            string Result = JsonConvert.SerializeObject(Model, Newtonsoft.Json.Formatting.Indented, timeConverter).Replace("null", "\"\"");
            context.Response.Write(Result);
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}