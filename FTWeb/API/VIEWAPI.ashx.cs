using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Practices.Unity;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System.Web.SessionState;
using QJY.API;
using QJY.Data;
using System.Diagnostics;

namespace QjySaaSWeb.APP
{
    /// <summary>
    /// VIEWAPI 的摘要说明
    /// </summary>
    public class VIEWAPI : IHttpHandler, IRequiresSessionState
    {
        public string ComId { get; set; }

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
            string UserName = context.Request["UserName"] ?? "";

            Msg_Result Model = new Msg_Result() { Action = strAction.ToUpper(), ErrorMsg = "" };
            if (!string.IsNullOrEmpty(strAction))
            {
                try
                {
                    //TODO: 未实现,分享页面的接口通过暂时将分享人的code传递给打开链接的人来调用接口(不安全)
                    #region 必须登录执行接口
                    Model.ErrorMsg = "";

                    var bl = true;
                    var acs = Model.Action.Split('_');
                    if (Model.Action.IndexOf("_") > 0)
                    {
                        if (acs[0].ToUpper() == "Commanage".ToUpper())
                        {
                            bl = false;
                            var container = ServiceContainerV.Current().Resolve<IWsService>(acs[0].ToUpper());
                            Model.Action = acs[1];
                            container.ProcessRequest(context, ref Model, P1.TrimEnd(), P2.TrimEnd(), new JH_Auth_UserB.UserInfo());
                            new JH_Auth_LogB().InsertLog(Model.Action, "调用接口", context.Request.Url.AbsoluteUri, UserName, 0);

                        }
                    }
                    if (bl)
                    {
                        if (context.Request.Cookies["szhlcode"] != null && context.Request.Cookies["szhlcode"].ToString() != "")//如果存在TOKEN,根据TOKEN找到用户信息，并根据权限执行具体ACTION
                        {
                            string strSZHLCode = context.Request.Cookies["szhlcode"].Value;
                            //通过Code获取用户名，然后执行接口方法
                            var container = ServiceContainerV.Current().Resolve<IWsService>(acs[0].ToUpper());
                            JH_Auth_UserB.UserInfo UserInfo = new JH_Auth_UserB().GetUserInfo(strSZHLCode);
                            if (UserInfo != null)
                            {
                                Model.Action = Model.Action.Substring(acs[0].Length + 1);
                                container.ProcessRequest(context, ref Model, P1.TrimEnd(), P2.TrimEnd(), UserInfo);
                                if (strAction != "CHAT_GETNOREADMSG")
                                {
                                    new JH_Auth_LogB().InsertLog(Model.Action, "调用接口", context.Request.Url.AbsoluteUri, UserInfo.User.UserName, UserInfo.QYinfo.ComId);
                                }
                            }
                            else
                            {
                                Model.ErrorMsg = "NOSESSIONCODE";
                            }

                        }
                        else
                        {
                            Model.ErrorMsg = "NOSESSIONCODE";
                        }
                    }
                    #endregion

                }
                catch (Exception ex)
                {
                    Model.ErrorMsg = strAction + "接口调用失败,请检查日志";
                    Model.Result = ex.Message;
                    new JH_Auth_LogB().InsertLog(strAction,Model.ErrorMsg, ex.Message.ToString(), UserName, 0);

                }
            }
            string jsonpcallback = context.Request["jsonpcallback"] ?? "";
            IsoDateTimeConverter timeConverter = new IsoDateTimeConverter();
            timeConverter.DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
            string Result = JsonConvert.SerializeObject(Model, Formatting.Indented, timeConverter).Replace("null", "\"\"");
            if (jsonpcallback != "")
            {
                Result = jsonpcallback + "(" + Result + ")";//支持跨域
            }
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