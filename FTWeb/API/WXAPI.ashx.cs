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
namespace QjySaaSWeb.APP
{
    /// <summary>
    /// WXAPI 的摘要说明
    /// </summary>
    public class WXAPI : IHttpHandler, IRequiresSessionState
    {

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/plain";
            context.Response.AddHeader("pragma", "no-cache");
            context.Response.AddHeader("cache-control", "");
            context.Response.CacheControl = "no-cache";
            string strAction = context.Request["Action"] ?? "";
            string UserName = context.Request["UserName"] ?? "";
            string strIP = CommonHelp.getIP(context);

            Msg_Result Model = new Msg_Result() { Action = strAction.ToUpper(), ErrorMsg = "" };

            if (!string.IsNullOrEmpty(strAction))
            {

                #region 企业号应用callback
                if (strAction == "XXJS")
                {
                    String strCorpID = context.Request["corpid"] ?? "";
                    string strCode = context.Request["Code"] ?? "";
                    try
                    {
                        JH_Auth_QY jaq = new JH_Auth_QYB().GetALLEntities().FirstOrDefault();
                        JH_Auth_Model jam = new JH_Auth_ModelB().GetEntity(p => p.ModelCode == strCode);
                        //if (jaq != null && jam != null && !string.IsNullOrEmpty(jam.TJId))
                        if (jaq != null && jam != null)
                        {
                            #region POST
                            if (HttpContext.Current.Request.HttpMethod.ToUpper() == "POST")
                            {
                                string signature = HttpContext.Current.Request.QueryString["msg_signature"];//企业号的 msg_signature
                                string timestamp = HttpContext.Current.Request.QueryString["timestamp"];
                                string nonce = HttpContext.Current.Request.QueryString["nonce"];

                                // 获得客户端RAW HttpRequest  
                                StreamReader srResult = new StreamReader(context.Request.InputStream);
                                string str = srResult.ReadToEnd();
                                XmlDocument XmlDocument = new XmlDocument();
                                XmlDocument.LoadXml(HttpContext.Current.Server.UrlDecode(str));
                                string ToUserName = string.Empty;
                                string strde = string.Empty;
                                string msgtype = string.Empty;//微信响应类型
                                foreach (XmlNode xn in XmlDocument.ChildNodes[0].ChildNodes)
                                {
                                    if (xn.Name == "ToUserName")
                                    {
                                        ToUserName = xn.InnerText;
                                    }
                                }
                                var pj = new JH_Auth_WXPJB().GetEntity(p => p.TJID == jam.TJId);
                                Tencent.WXBizMsgCrypt wxcpt = new Tencent.WXBizMsgCrypt(pj.Token, pj.EncodingAESKey, ToUserName);
                                int n = wxcpt.DecryptMsg(signature, timestamp, nonce, str, ref strde);
                                XmlDocument XmlDocument1 = new XmlDocument();
                                XmlDocument1.LoadXml(HttpContext.Current.Server.UrlDecode(strde));
                                foreach (XmlNode xn1 in XmlDocument1.ChildNodes[0].ChildNodes)
                                {
                                    if (xn1.Name == "MsgType")
                                    {
                                        msgtype = xn1.InnerText;
                                    }
                                    //CommonHelp.WriteLOG(XmlDocument1.OuterXml);

                                }
                                if (msgtype == "event")//处理事件
                                {
                                    //需要处理进入应用的菜单更改事件
                                    string strEvent = XmlDocument1.ChildNodes[0]["Event"].InnerText;
                                    string strUserName = XmlDocument1.ChildNodes[0]["FromUserName"].InnerText;
                                    string strAgentID = XmlDocument1.ChildNodes[0]["AgentID"].InnerText;
                                    string strEventKey = XmlDocument1.ChildNodes[0]["EventKey"].InnerText;

                                    if (strEvent.ToLower() == "enter_agent" || strEvent.ToLower() == "view")
                                    {
                                        //进入应用和点击菜单
                                        //JH_Auth_User jau = new JH_Auth_UserB().GetEntity(p => p.ComId == jaq.ComId && p.UserName == strUserName);
                                        //JH_Auth_QY_Model jhqm = new JH_Auth_QY_ModelB().GetEntity(p => p.ComId == jaq.ComId && p.AgentId == strAgentID);
                                        //if (jau != null && jhqm != null)
                                        //{
                                        //    JH_Auth_YYLog jay = new JH_Auth_YYLog();
                                        //    jay.ComId = jaq.ComId;
                                        //    jay.AgentID = strAgentID;
                                        //    jay.CorpID = strCorpID;
                                        //    jay.CRDate = DateTime.Now;
                                        //    jay.CRUser = strUserName;
                                        //    jay.Event = strEvent;
                                        //    jay.EventKey = strEventKey;
                                        //    jay.ModelCode = strCode;
                                        //    jay.ModelID = jhqm.ModelID;
                                        //    jay.QYName = jaq.QYName;
                                        //    jay.TJID = jam.TJId;
                                        //    jay.Type = msgtype;
                                        //    jay.UserName = strUserName;
                                        //    jay.UserRealName = jau.UserRealName;

                                        //    new JH_Auth_YYLogB().Insert(jay);

                                        //    if (strEvent.ToLower() == "enter_agent")
                                        //    {
                                        //        var jays = new JH_Auth_YYLogB().GetEntities(p => p.ComId == jaq.ComId && p.Event == "enter_agent" && p.AgentID == strAgentID && p.CRUser == strUserName);
                                        //        if (jays.Count() <= 1)
                                        //        {
                                        //        }
                                        //    }
                                        //}

                                    }

                                }
                                if (new List<string> { "text", "image", "voice", "video", "shortvideo", "link" }.Contains(msgtype))//处理消息事件
                                {

                                    if (XmlDocument1.ChildNodes.Count > 0)
                                    {
                                        JH_Auth_WXMSG wxmsgModel = new JH_Auth_WXMSG();
                                        wxmsgModel.AgentID = int.Parse(XmlDocument1.ChildNodes[0]["AgentID"].InnerText);
                                        wxmsgModel.ComId = jaq.ComId;
                                        wxmsgModel.ToUserName = XmlDocument1.ChildNodes[0]["ToUserName"].InnerText;
                                        wxmsgModel.FromUserName = XmlDocument1.ChildNodes[0]["FromUserName"].InnerText;
                                        wxmsgModel.CRDate = DateTime.Now;
                                        wxmsgModel.CRUser = XmlDocument1.ChildNodes[0]["FromUserName"].InnerText;
                                        wxmsgModel.MsgId = XmlDocument1.ChildNodes[0]["MsgId"].InnerText;
                                        wxmsgModel.MsgType = msgtype;
                                        wxmsgModel.ModeCode = strCode;
                                        wxmsgModel.Tags = "微信收藏";

                                        switch (msgtype)
                                        {
                                            case "text":
                                                wxmsgModel.MsgContent = XmlDocument1.ChildNodes[0]["Content"].InnerText;
                                                break;
                                            case "image":
                                                wxmsgModel.PicUrl = XmlDocument1.ChildNodes[0]["PicUrl"].InnerText;
                                                wxmsgModel.MediaId = XmlDocument1.ChildNodes[0]["MediaId"].InnerText;
                                                break;
                                            case "voice":
                                                wxmsgModel.MediaId = XmlDocument1.ChildNodes[0]["MediaId"].InnerText;
                                                wxmsgModel.Format = XmlDocument1.ChildNodes[0]["Format"].InnerText;
                                                break;
                                            case "video":
                                                wxmsgModel.MediaId = XmlDocument1.ChildNodes[0]["MediaId"].InnerText;
                                                wxmsgModel.ThumbMediaId = XmlDocument1.ChildNodes[0]["ThumbMediaId"].InnerText;
                                                break;
                                            case "shortvideo":
                                                wxmsgModel.MediaId = XmlDocument1.ChildNodes[0]["MediaId"].InnerText;
                                                wxmsgModel.ThumbMediaId = XmlDocument1.ChildNodes[0]["ThumbMediaId"].InnerText;
                                                break;
                                            case "link":
                                                wxmsgModel.Description = XmlDocument1.ChildNodes[0]["Description"].InnerText;
                                                wxmsgModel.Title = XmlDocument1.ChildNodes[0]["Title"].InnerText;
                                                wxmsgModel.URL = XmlDocument1.ChildNodes[0]["Url"].InnerText;
                                                wxmsgModel.PicUrl = XmlDocument1.ChildNodes[0]["PicUrl"].InnerText;
                                                break;
                                        }
                                        if (new List<string>() { "link", "text" }.Contains(msgtype))
                                        {
                                            if (msgtype == "link")
                                            {
                                                var jaw = new JH_Auth_WXMSGB().GetEntity(p => p.ComId == jaq.ComId && p.MsgId == wxmsgModel.MsgId);
                                                if (jaw == null)
                                                {
                                                    string strMedType = ".jpg";
                                                    JH_Auth_UserB.UserInfo UserInfo = new JH_Auth_UserB.UserInfo();
                                                    UserInfo = new JH_Auth_UserB().GetUserInfo(jaq.ComId, wxmsgModel.FromUserName);
                                                    string fileID = CommonHelp.ProcessWxIMGUrl(wxmsgModel.PicUrl, UserInfo, strMedType);

                                                    wxmsgModel.FileId = fileID;
                                                    new JH_Auth_WXMSGB().Insert(wxmsgModel);

                                                    if (strCode == "TSSQ")
                                                    {
                                                        SZHL_TXSX tx1 = new SZHL_TXSX();
                                                        tx1.ComId = jaq.ComId;
                                                        tx1.APIName = "TSSQ";
                                                        tx1.MsgID = wxmsgModel.ID.ToString();
                                                        tx1.FunName = "SENDWXMSG";
                                                        tx1.Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                                        tx1.CRUser = wxmsgModel.CRUser;
                                                        tx1.CRDate = DateTime.Now;
                                                        TXSX.TXSXAPI.AddALERT(tx1); //时间为发送时间
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                new JH_Auth_WXMSGB().Insert(wxmsgModel);
                                            }
                                        }
                                        if (!string.IsNullOrEmpty(wxmsgModel.MediaId))
                                        {
                                            var jaw = new JH_Auth_WXMSGB().GetEntity(p => p.ComId == jaq.ComId && p.MediaId == wxmsgModel.MediaId);
                                            if (jaw == null)
                                            {
                                                string strMedType = ".jpg";
                                                if (strCode == "QYWD" || strCode == "CRM")//判断模块
                                                {
                                                    if (msgtype == "shortvideo" || msgtype == "video")//视频,小视频
                                                    {
                                                        strMedType = ".mp4";
                                                    }
                                                    if (new List<string>() { "image", "shortvideo", "video", "voice" }.Contains(msgtype))//下载到本地服务器
                                                    {
                                                        JH_Auth_UserB.UserInfo UserInfo = new JH_Auth_UserB.UserInfo();
                                                        UserInfo = new JH_Auth_UserB().GetUserInfo(jaq.ComId, wxmsgModel.FromUserName);
                                                        string fileID = CommonHelp.ProcessWxIMG(wxmsgModel.MediaId, strCode, UserInfo, strMedType);
                                                        wxmsgModel.FileId = fileID;
                                                        new JH_Auth_WXMSGB().Insert(wxmsgModel);

                                                    }

                                                }
                                                //CommonHelp.WriteLOG("1");
                                                #region CRM
                                                if (strCode == "CRM")
                                                {
                                                    //名片识别
                                                    if (!string.IsNullOrEmpty(wxmsgModel.PicUrl))
                                                    {
                                                        try
                                                        {
                                                            string url = CommonHelp.GetConfig("CADEAPI");
                                                            if (url != "")
                                                            {
                                                                string verb = "POST";
                                                                HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;

                                                                req.Timeout = 60 * 1000;
                                                                //req.Headers.Add("Accept-Encoding", "gzip,deflate");
                                                                req.UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_8_4) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/27.0.1453.110 Safari/537.36";

                                                                req.Method = verb;

                                                                string strData = "image=" + wxmsgModel.PicUrl;
                                                                //string strData = "[{\"image\":"+wxmsgModel.PicUrl+"}]" ;


                                                                try
                                                                {
                                                                    //CommonHelp.WriteLOG("2");
                                                                    if (verb == "POST")
                                                                    {
                                                                        byte[] data = Encoding.UTF8.GetBytes(strData);

                                                                        //req.ContentType = "application/text; charset=utf-8";
                                                                        req.ContentType = "application/x-www-form-urlencoded";
                                                                        req.ContentLength = data.Length;

                                                                        Stream requestStream = req.GetRequestStream();
                                                                        requestStream.Write(data, 0, data.Length);
                                                                        requestStream.Close();
                                                                    }


                                                                    using (var res = req.GetResponse())
                                                                    {
                                                                        using (var stream = res.GetResponseStream())
                                                                        {
                                                                            StreamReader sr = new StreamReader(stream, Encoding.UTF8);
                                                                            string response = sr.ReadToEnd();

                                                                            sr.Close();

                                                                            //CommonHelp.WriteLOG("3");

                                                                            SZHL_CRM_CARD scc = JsonConvert.DeserializeObject<SZHL_CRM_CARD>(response);
                                                                            scc.ComId = jaq.ComId;
                                                                            scc.Files = wxmsgModel.FileId;
                                                                            scc.Status = "0";
                                                                            scc.Del = 0;
                                                                            scc.CRDate = DateTime.Now;
                                                                            scc.CRUser = wxmsgModel.CRUser;
                                                                            new SZHL_CRM_CARDB().Insert(scc);
                                                                            //CommonHelp.WriteLOG(response);
                                                                            //return response;

                                                                            SZHL_TXSX tx1 = new SZHL_TXSX();
                                                                            tx1.ComId = jaq.ComId;
                                                                            tx1.APIName = "CRM";
                                                                            tx1.MsgID = scc.ID.ToString();
                                                                            tx1.FunName = "SENDWXMSG_MP";
                                                                            tx1.Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                                                            tx1.CRUser = scc.CRUser;
                                                                            tx1.CRDate = DateTime.Now;
                                                                            TXSX.TXSXAPI.AddALERT(tx1); //时间为发送时间
                                                                        }
                                                                    }
                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    CommonHelp.WriteLOG(ex.ToString());
                                                                }
                                                                //return null;
                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            CommonHelp.WriteLOG(ex.ToString());
                                                        }
                                                    }
                                                }
                                                #endregion
                                            }
                                        }
                                    }
                                }
                            }
                            #endregion

                            #region GET
                            if (HttpContext.Current.Request.HttpMethod.ToUpper() == "GET")
                            {
                                Auth(jam.Token, jam.EncodingAESKey, jaq.corpId);
                            }
                            #endregion

                        }
                    }
                    catch (Exception ex)
                    {
                        Model.ErrorMsg = ex.ToString();
                        CommonHelp.WriteLOG(ex.ToString());
                    }

                }
                #endregion

                #region 企业会话
                if (strAction == "QYIM")
                {
                    if (HttpContext.Current.Request.HttpMethod.ToUpper() == "POST")
                    {
                        string corpId = context.Request["corpid"] ?? "";

                        try
                        {
                            JH_Auth_QY jaq = new JH_Auth_QYB().GetEntity(p => p.corpId == corpId);
                            if (jaq != null)
                            {
                                string signature = HttpContext.Current.Request.QueryString["msg_signature"];//企业号的 msg_signature
                                string timestamp = HttpContext.Current.Request.QueryString["timestamp"];
                                string nonce = HttpContext.Current.Request.QueryString["nonce"];

                                // 获得客户端RAW HttpRequest  
                                StreamReader srResult = new StreamReader(context.Request.InputStream);
                                string str = srResult.ReadToEnd();

                                string strde = string.Empty;

                                var pj = new JH_Auth_WXPJB().GetEntity(p => p.TJID == "tj7882b1f8bc56f05f");

                                Tencent.WXBizMsgCrypt wxcpt = new Tencent.WXBizMsgCrypt(pj.Token, pj.EncodingAESKey, corpId);

                                wxcpt.DecryptMsg(signature, timestamp, nonce, str, ref strde);

                                //string strde = HttpContext.Current.Request.QueryString[0];

                                XmlDocument XmlDocument = new XmlDocument();
                                XmlDocument.LoadXml(HttpContext.Current.Server.UrlDecode(strde));

                                string AgentType = string.Empty;
                                string ToUserName = string.Empty;
                                string ItemCount = string.Empty;
                                string PackageId = string.Empty;
                                string Item = string.Empty;

                                #region XML文档处理
                                foreach (XmlNode xn in XmlDocument.ChildNodes[0].ChildNodes)
                                {
                                    if (xn.Name == "AgentType")
                                    {
                                        AgentType = xn.InnerText;
                                    }
                                    if (xn.Name == "ToUserName")
                                    {
                                        ToUserName = xn.InnerText;
                                    }
                                    if (xn.Name == "ItemCount")
                                    {
                                        ItemCount = xn.InnerText;
                                    }
                                    if (xn.Name == "PackageId")
                                    {
                                        PackageId = xn.InnerText;
                                    }
                                    if (xn.Name == "Item")
                                    {
                                        Item += xn.InnerXml;

                                        string MsgType = xn.ChildNodes[2].InnerText;

                                        if (MsgType == "event")
                                        {
                                            #region event处理
                                            SZHL_QYIM zj = new SZHL_QYIM();

                                            //try
                                            //{
                                            //    zj.FromUserName = xn.ChildNodes[0]["FromUserName"].InnerText;
                                            //    zj.MsgType = xn.ChildNodes[0]["MsgType"].InnerText;
                                            //    zj.Event = xn.ChildNodes[0]["Event"].InnerText;
                                            //    zj.ChatId = xn.ChildNodes[0]["ChatId"].InnerText;
                                            //    zj.Name = xn.ChildNodes[0]["Name"].InnerText;
                                            //    zj.Owner = xn.ChildNodes[0]["Owner"].InnerText;
                                            //    zj.UserList = xn.ChildNodes[0]["UserList"].InnerText;
                                            //    zj.AddUserList = xn.ChildNodes[0]["AddUserList"].InnerText;
                                            //    zj.DelUserList = xn.ChildNodes[0]["DelUserList"].InnerText;

                                            //    DateTime time = DateTime.MinValue;
                                            //    DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));
                                            //    time = startTime.AddSeconds(int.Parse(xn.ChildNodes[0]["CreateTime"].InnerText));

                                            //    zj.CreateTime = time;
                                            //}
                                            //catch { }


                                            foreach (XmlNode xnc in xn.ChildNodes)
                                            {
                                                if (xnc.Name == "FromUserName")
                                                {
                                                    zj.FromUserName = xnc.InnerText;
                                                }
                                                if (xnc.Name == "MsgType")
                                                {
                                                    zj.MsgType = xnc.InnerText;
                                                }
                                                if (xnc.Name == "Event")
                                                {
                                                    zj.Event = xnc.InnerText;
                                                }
                                                if (xnc.Name == "ChatId")
                                                {
                                                    zj.ChatId = xnc.InnerText;
                                                }
                                                if (xnc.Name == "Name")
                                                {
                                                    zj.Name = xnc.InnerText;
                                                }
                                                if (xnc.Name == "Owner")
                                                {
                                                    zj.Owner = xnc.InnerText;
                                                }
                                                if (xnc.Name == "UserList")
                                                {
                                                    zj.UserList = xnc.InnerText;
                                                }
                                                if (xnc.Name == "AddUserList")
                                                {
                                                    zj.AddUserList = xnc.InnerText;
                                                }
                                                if (xnc.Name == "DelUserList")
                                                {
                                                    zj.DelUserList = xnc.InnerText;
                                                }
                                                if (xnc.Name == "CreateTime")
                                                {
                                                    DateTime time = DateTime.MinValue;
                                                    DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));
                                                    time = startTime.AddSeconds(int.Parse(xnc.InnerText));

                                                    zj.CreateTime = time;
                                                }
                                                if (xnc.ChildNodes.Count > 1)
                                                {
                                                    foreach (XmlNode xncn in xnc.ChildNodes)
                                                    {
                                                        if (xncn.Name == "ChatId")
                                                        {
                                                            zj.ChatId = xncn.InnerText;
                                                        }
                                                        if (xncn.Name == "Name")
                                                        {
                                                            zj.Name = xncn.InnerText;
                                                        }
                                                        if (xncn.Name == "Owner")
                                                        {
                                                            zj.Owner = xncn.InnerText;
                                                        }
                                                        if (xncn.Name == "UserList")
                                                        {
                                                            zj.UserList = xncn.InnerText;
                                                        }
                                                    }
                                                }
                                            }
                                            zj.Sourse = "1";
                                            zj.Status = "0";
                                            zj.ComId = jaq.ComId;
                                            new SZHL_QYIMB().Insert(zj);
                                            if (zj.Event == "create_chat")
                                            {
                                                SZHL_QYIM_LIST sql = new SZHL_QYIM_LIST();
                                                sql.ChatId = zj.ChatId;
                                                sql.FromUserName = zj.FromUserName;
                                                sql.MsgType = "group";
                                                sql.Name = zj.Name;
                                                sql.Owner = zj.Owner;
                                                sql.Sourse = "1";
                                                sql.Status = "0";
                                                sql.UserList = zj.UserList;
                                                sql.ComId = jaq.ComId;
                                                new SZHL_QYIM_LISTB().Insert(sql);
                                            }
                                            if (zj.Event == "update_chat")
                                            {
                                                SZHL_QYIM_LIST sql1 = new SZHL_QYIM_LISTB().GetEntities(p => p.ComId == jaq.ComId && p.ChatId == zj.ChatId && p.Status == "0").FirstOrDefault();
                                                if (sql1 != null)
                                                {
                                                    if (!string.IsNullOrEmpty(zj.Name))
                                                    {
                                                        sql1.Name = zj.Name;
                                                    }
                                                    if (!string.IsNullOrEmpty(zj.Owner))
                                                    {
                                                        sql1.Owner = zj.Owner;
                                                    }
                                                    if (!string.IsNullOrEmpty(zj.AddUserList))
                                                    {
                                                        sql1.UserList = sql1.UserList + "|" + zj.AddUserList;
                                                    }
                                                    if (!string.IsNullOrEmpty(zj.DelUserList))
                                                    {
                                                        string[] dul = zj.DelUserList.Split('|');
                                                        if (!string.IsNullOrEmpty(sql1.UserList))
                                                        {
                                                            string uis = string.Empty;
                                                            string[] strs = sql1.UserList.Split('|');
                                                            foreach (string s in strs)
                                                            {
                                                                bool bl = true;
                                                                foreach (string d in dul)
                                                                {
                                                                    if (s == d)
                                                                    {
                                                                        bl = false;
                                                                    }
                                                                }
                                                                if (bl)
                                                                {
                                                                    if (string.IsNullOrEmpty(uis))
                                                                    {
                                                                        uis = s;
                                                                    }
                                                                    else
                                                                    {
                                                                        uis = uis + "|" + s;
                                                                    }
                                                                }
                                                            }
                                                            sql1.UserList = uis;
                                                        }
                                                    }
                                                    sql1.ComId = jaq.ComId;
                                                    new SZHL_QYIM_LISTB().Update(sql1);
                                                }
                                            }
                                            if (zj.Event == "quit_chat")
                                            {
                                                SZHL_QYIM_LIST sql1 = new SZHL_QYIM_LISTB().GetEntities(p => p.ComId == jaq.ComId && p.ChatId == zj.ChatId && p.Status == "0").FirstOrDefault();
                                                if (sql1 != null)
                                                {
                                                    if (!string.IsNullOrEmpty(sql1.UserList))
                                                    {
                                                        string uis = string.Empty;
                                                        string[] strs = sql1.UserList.Split('|');
                                                        foreach (string s in strs)
                                                        {
                                                            bool bl = true;

                                                            if (s == zj.FromUserName)
                                                            {
                                                                bl = false;
                                                            }
                                                            if (bl)
                                                            {
                                                                if (string.IsNullOrEmpty(uis))
                                                                {
                                                                    uis = s;
                                                                }
                                                                else
                                                                {
                                                                    uis = uis + "|" + s;
                                                                }
                                                            }
                                                        }
                                                        sql1.UserList = uis;
                                                        sql1.ComId = jaq.ComId;
                                                        new SZHL_QYIM_LISTB().Update(sql1);
                                                    }
                                                }
                                            }
                                            #endregion
                                        }
                                        else if (new List<string> { "text", "image", "voice", "file", "link" }.Contains(MsgType))
                                        {
                                            #region 内容处理

                                            SZHL_QYIM_ITEM zj = new SZHL_QYIM_ITEM();

                                            //zj.FromUserName = xn.ChildNodes[0]["FromUserName"].InnerText;
                                            //zj.MsgType = xn.ChildNodes[0]["MsgType"].InnerText;
                                            //zj.Event = xn.ChildNodes[0]["Event"].InnerText;
                                            //zj.MsgId = xn.ChildNodes[0]["MsgId"].InnerText;

                                            //string strMedType = ".jpg";
                                            //if (new List<string>() { "image", "voice" }.Contains(MsgType))//下载到本地服务器
                                            //{
                                            //    if (MsgType == "voice")//视频,小视频
                                            //    {
                                            //        strMedType = ".mp3";
                                            //    }
                                            //    JH_Auth_UserB.UserInfo UserInfo = new JH_Auth_UserB.UserInfo();
                                            //    UserInfo = new JH_Auth_UserB().GetUserInfo(jaq.ComId, zj.FromUserName);
                                            //    string fileID = CommonHelp.ProcessWxIMG(xn.ChildNodes[0]["MediaId"].InnerText, "QYIM", UserInfo, strMedType);
                                            //    zj.FileID = Int32.Parse( fileID);
                                            //}

                                            //DateTime time = DateTime.MinValue;
                                            //DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));
                                            //time = startTime.AddSeconds(int.Parse(xn.ChildNodes[0]["CreateTime"].InnerText));
                                            //zj.CreateTime = time;

                                            foreach (XmlNode xnc in xn.ChildNodes)
                                            {
                                                if (xnc.Name == "FromUserName")
                                                {
                                                    zj.FromUserName = xnc.InnerText;
                                                }
                                                if (xnc.Name == "MsgType")
                                                {
                                                    zj.MsgType = xnc.InnerText;
                                                }
                                                if (xnc.Name == "Event")
                                                {
                                                    zj.Event = xnc.InnerText;
                                                }
                                                if (xnc.Name == "Content")
                                                {
                                                    zj.Content = xnc.InnerText;
                                                }
                                                if (xnc.Name == "MsgId")
                                                {
                                                    zj.MsgId = xnc.InnerText;
                                                }

                                                if (xnc.Name == "PicUrl")
                                                {
                                                    zj.PicUrl = xnc.InnerText;
                                                }
                                                if (xnc.Name == "MediaId")
                                                {
                                                    zj.MediaId = xnc.InnerText;

                                                    string strMedType = ".jpg";
                                                    if (new List<string>() { "image", "voice" }.Contains(MsgType))//下载到本地服务器
                                                    {
                                                        if (MsgType == "voice")//视频,小视频
                                                        {
                                                            strMedType = ".amr";
                                                        }
                                                        JH_Auth_UserB.UserInfo UserInfo = new JH_Auth_UserB.UserInfo();
                                                        UserInfo = new JH_Auth_UserB().GetUserInfo(jaq.ComId, zj.FromUserName);
                                                        string fileID = CommonHelp.ProcessWxIMG(xnc.InnerText, "QYIM", UserInfo, strMedType);
                                                        zj.FileID = Int32.Parse(fileID);
                                                    }
                                                }
                                                if (xnc.Name == "CreateTime")
                                                {
                                                    DateTime time = DateTime.MinValue;
                                                    DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));
                                                    time = startTime.AddSeconds(int.Parse(xnc.InnerText));
                                                    zj.CreateTime = time;
                                                }
                                                if (xnc.Name == "Content")
                                                {
                                                    zj.Content = xnc.InnerText;
                                                }
                                                if (xnc.Name == "Title")
                                                {
                                                    zj.Title = xnc.InnerText;
                                                }
                                                if (xnc.Name == "Description")
                                                {
                                                    zj.Description = xnc.InnerText;
                                                }
                                                if (xnc.Name == "Url")
                                                {
                                                    zj.Url = xnc.InnerText;
                                                }

                                                if (xnc.ChildNodes.Count > 1)
                                                {
                                                    foreach (XmlNode xncn in xnc.ChildNodes)
                                                    {
                                                        if (xncn.Name == "Type")
                                                        {
                                                            zj.Type = xncn.InnerText;
                                                        }
                                                        if (xncn.Name == "Id")
                                                        {
                                                            zj.UID = xncn.InnerText;
                                                        }
                                                    }
                                                }
                                            }
                                            zj.Sourse = "1";
                                            zj.Status = "0";
                                            zj.ComId = jaq.ComId;
                                            new SZHL_QYIM_ITEMB().Insert(zj);

                                            if (zj.Type == "single")
                                            {
                                                SZHL_QYIM_LIST sql1 = new SZHL_QYIM_LISTB().GetEntities(p => p.ComId == jaq.ComId && p.FromUserName == zj.FromUserName && p.UserList == zj.UID && p.Status == "0").FirstOrDefault();
                                                if (sql1 == null)
                                                {
                                                    SZHL_QYIM_LIST sql = new SZHL_QYIM_LIST();
                                                    sql.FromUserName = zj.FromUserName;
                                                    sql.MsgType = "single";
                                                    sql.Sourse = "1";
                                                    sql.Status = "0";
                                                    sql.UserList = zj.UID;
                                                    sql.ComId = jaq.ComId;
                                                    new SZHL_QYIM_LISTB().Insert(sql);
                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                }
                                #endregion

                                HttpContext.Current.Response.Write(PackageId);
                                HttpContext.Current.Response.End();
                            }
                        }
                        catch (Exception ex)
                        {
                            CommonHelp.WriteLOG("QYIM:" + ex.ToString() + "\r\n" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        }
                    }
                }
                #endregion

                #region 获取唯一code
                if (strAction.ToUpper() == "GetUserCodeByCode".ToUpper())
                {
                    #region 获取Code
                    Model.ErrorMsg = "获取Code错误，请重试";

                    string strCode = context.Request["code"] ?? "";
                    string strCorpID = context.Request["corpid"] ?? "";
                    string strModelCode = context.Request["funcode"] ?? "";

                    if (!string.IsNullOrEmpty(strCode))
                    {

                        var qy = new JH_Auth_QYB().GetEntity(p => p.corpId == strCorpID);
                        if (qy != null)
                        {
                            try
                            {

                                //获取用户名
                                WXHelp wx = new WXHelp(qy);
                                string username = wx.GetUserDataByCode(strCode);

                                if (!string.IsNullOrEmpty(username))
                                {
                                    var jau = new JH_Auth_UserB().GetUserByUserName(qy.ComId, username);
                                    if (jau != null)
                                    {
                                        if (string.IsNullOrEmpty(jau.pccode))
                                        {
                                            string strGuid = CommonHelp.CreatePCCode(jau);
                                            jau.pccode = strGuid;
                                            new JH_Auth_UserB().Update(jau);
                                        }
                                        Model.ErrorMsg = "";
                                        Model.Result = jau.pccode;
                                        Model.Result1 = jau.UserName;
                                    }

                                }
                                else
                                {
                                    Model.ErrorMsg = "当前用户名不存在";
                                }
                            }
                            catch (Exception ex)
                            {
                                Model.ErrorMsg = ex.ToString();
                            }
                        }
                        else
                        {
                            Model.ErrorMsg = "当前企业号未在电脑端注册";
                        }

                    }
                    else
                    {
                        Model.ErrorMsg = "Code为空";
                    }
                    #endregion
                }
                #endregion
                #region 是否存在
                if (strAction.ToUpper() == "isexist".ToUpper())
                {
                    string strcorpid = context.Request["corpid"] ?? "";
                    if (strcorpid != "")
                    {
                        var qy = new JH_Auth_QYB().GetEntity(p => p.corpId == strcorpid);
                        if (qy == null)
                        {
                            Model.ErrorMsg = "当前企业号未注册此平台";
                        }
                        else
                        {
                            if (context.Request.Cookies["szhlcode"] != null)
                            {
                                //通过Cookies获取Code
                                //string szhlcode = "5ab470be-4988-4bb3-9658-050481b98fca"; 
                                string szhlcode = context.Request.Cookies["szhlcode"].Value.ToString();
                                //通过Code获取用户名，然后执行接口方法
                                var jau = new JH_Auth_UserB().GetUserByPCCode(szhlcode);
                                if (jau == null)
                                {
                                    Model.ErrorMsg = "用户Code不存在";
                                }
                                else
                                {
                                    if (new JH_Auth_QYB().GetEntity(d=>d.ComId==jau.ComId.Value).corpId != strcorpid)
                                    {
                                        Model.ErrorMsg = "企业需要重新选择";
                                    }
                                    //重写CODE


                                }
                            }
                        }
                    }
                    else
                    {
                        Model.ErrorMsg = "企业号连接有误，请重新连接";
                    }

                }
                #endregion
                #region 发送提醒
                if (strAction.ToUpper() == "AUTOALERT")
                {
                    TXSX.TXSXAPI.AUTOALERT();
                    CommonHelp.WriteLOG("调用提醒接口");
                }
                #endregion
            }
            else
            {
                #region 获取SuiteTicket
                if (HttpContext.Current.Request.HttpMethod.ToUpper() == "POST")
                {

                    string signature = HttpContext.Current.Request.QueryString["msg_signature"];//企业号的 msg_signature
                    string timestamp = HttpContext.Current.Request.QueryString["timestamp"];
                    string nonce = HttpContext.Current.Request.QueryString["nonce"];

                    // 获得客户端RAW HttpRequest  
                    StreamReader srResult = new StreamReader(context.Request.InputStream);
                    string str = srResult.ReadToEnd();

                    XmlDocument XmlDocument = new XmlDocument();
                    XmlDocument.LoadXml(HttpContext.Current.Server.UrlDecode(str));

                    string ToUserName = string.Empty;
                    string Encrypt = string.Empty;

                    string strde = string.Empty;
                    string strinfotype = string.Empty;


                    foreach (XmlNode xn in XmlDocument.ChildNodes[0].ChildNodes)
                    {
                        if (xn.Name == "ToUserName")
                        {
                            ToUserName = xn.InnerText;
                        }
                        if (xn.Name == "Encrypt")
                        {
                            Encrypt = xn.InnerText;
                        }
                    }

                    var pj = new JH_Auth_WXPJB().GetEntity(p => p.TJID == ToUserName);

                    Tencent.WXBizMsgCrypt wxcpt = new Tencent.WXBizMsgCrypt(pj.Token, pj.EncodingAESKey, ToUserName);
                    int n = wxcpt.DecryptMsg(signature, timestamp, nonce, str, ref strde);

                    string strtct = string.Empty;
                    string strSuiteId = string.Empty;
                    string strtAuthCorpId = string.Empty;

                    XmlDocument XmlDocument1 = new XmlDocument();
                    XmlDocument1.LoadXml(HttpContext.Current.Server.UrlDecode(strde));

                    foreach (XmlNode xn1 in XmlDocument1.ChildNodes[0].ChildNodes)
                    {
                        if (xn1.Name == "SuiteId")
                        {
                            strSuiteId = xn1.InnerText;
                        }
                        if (xn1.Name == "SuiteTicket")
                        {
                            strtct = xn1.InnerText;
                        }
                        if (xn1.Name == "InfoType")
                        {
                            strinfotype = xn1.InnerText;
                        }
                        if (xn1.Name == "AuthCorpId")
                        {
                            strtAuthCorpId = xn1.InnerText;
                        }
                    }
                    if (strinfotype == "suite_ticket")
                    {
                        pj.Ticket = strtct;

                        new JH_Auth_WXPJB().Update(pj);
                    }


                    HttpContext.Current.Response.Write("success");
                    HttpContext.Current.Response.End();
                }

                #endregion
            }

            IsoDateTimeConverter timeConverter = new IsoDateTimeConverter();
            timeConverter.DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
            string Result = JsonConvert.SerializeObject(Model, Newtonsoft.Json.Formatting.Indented, timeConverter).Replace("null", "\"\"");
            context.Response.Write(Result);
        }

        /// <summary>
        /// 成为开发者的第一步，验证并相应服务器的数据
        /// </summary>
        private void Auth(string token, string encodingAESKey, string corpId)
        {

            string echoString = HttpContext.Current.Request.QueryString["echoStr"];
            string signature = HttpContext.Current.Request.QueryString["msg_signature"];//企业号的 msg_signature
            string timestamp = HttpContext.Current.Request.QueryString["timestamp"];
            string nonce = HttpContext.Current.Request.QueryString["nonce"];

            string decryptEchoString = "";
            if (CheckSignature(token, signature, timestamp, nonce, corpId, encodingAESKey, echoString, ref decryptEchoString))
            {
                if (!string.IsNullOrEmpty(decryptEchoString))
                {
                    Int64 v = Convert.ToInt64(decryptEchoString);
                    HttpContext.Current.Response.Clear();
                    HttpContext.Current.Response.Write(v);
                    HttpContext.Current.Response.End();
                }
            }
        }

        #region 验证企业号签名
        /// <summary>
        /// 验证企业号签名
        /// </summary>
        /// <param name="token">企业号配置的Token</param>
        /// <param name="signature">签名内容</param>
        /// <param name="timestamp">时间戳</param>
        /// <param name="nonce">nonce参数</param>
        /// <param name="corpId">企业号ID标识</param>
        /// <param name="encodingAESKey">加密键</param>
        /// <param name="echostr">内容字符串</param>
        /// <param name="retEchostr">返回的字符串</param>
        /// <returns></returns>
        public bool CheckSignature(string token, string signature, string timestamp, string nonce, string corpId, string encodingAESKey, string echostr, ref string retEchostr)
        {
            Tencent.WXBizMsgCrypt wxcpt = new Tencent.WXBizMsgCrypt(token, encodingAESKey, corpId);
            int result = wxcpt.VerifyURL(signature, timestamp, nonce, echostr, ref retEchostr);
            if (result != 0)
            {
                //LogTextHelper.Error("ERR: VerifyURL fail, ret: " + result);
                return false;
            }

            return true;

            //ret==0表示验证成功，retEchostr参数表示明文，用户需要将retEchostr作为get请求的返回参数，返回给企业号。
            // HttpUtils.SetResponse(retEchostr);
        }

        #endregion
        public bool IsReusable
        {
            get
            {
                return false;
            }
        }



    }

}