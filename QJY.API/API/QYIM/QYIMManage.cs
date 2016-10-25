using QJY.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using FastReflectionLib;
using System.Data;
using QJY.Data;
using Newtonsoft.Json;

namespace QJY.API
{
    public class QYIMManage : IWsService
    {
        public void ProcessRequest(HttpContext context, ref Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            MethodInfo methodInfo = typeof(QYIMManage).GetMethod(msg.Action.ToUpper());
            QYIMManage model = new QYIMManage();
            methodInfo.FastInvoke(model, new object[] { context, msg, P1, P2, UserInfo });
        }

        #region 会话列表
        /// <summary>
        /// 会话列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETQYIMLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string userName = UserInfo.User.UserName;
            string strWhere = " 1=1 and ComId=" + UserInfo.User.ComId;
            strWhere += string.Format(" And ('|'+UserList+'|'  like '%|{0}|%' ) ", userName);

            DataTable dt = new SZHL_QYIM_LISTB().GetDTByCommand("select * from SZHL_QYIM_LIST where " + strWhere);

            msg.Result = dt;
        }
        #endregion

        #region 消息列表
        /// <summary>
        /// 消息列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETIMMSGLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            if (P2 != "")
            {
                var items = new SZHL_QYIM_ITEMB().GetEntities(p => p.UID == P1);
                msg.Result = items;
            }
            else
            {
                var items = new SZHL_QYIM_ITEMB().GetEntities(p => p.FromUserName == P1 && p.UID == P2);
                msg.Result = items;
            }
        }
        #endregion

        #region 创建会话
        /// <summary>
        /// 创建会话
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void ADDQYIM(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_QYIM sq = JsonConvert.DeserializeObject<SZHL_QYIM>(P1);

            if (string.IsNullOrEmpty(sq.UserList))
            {
                msg.ErrorMsg = "请选择人员！";
            }
            else if (sq.UserList.Split('|').Length <= 2)
            {
                msg.ErrorMsg = "请选择2个或以上人员！";
            }
            else
            {

                if (sq.ID == 0)
                {
                    sq.ChatId = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                    sq.FromUserName = UserInfo.User.UserName;
                    sq.Owner = UserInfo.User.UserName;
                    sq.MsgType = "event";
                    sq.Event = "create_chat";
                    sq.CreateTime = DateTime.Now;
                    sq.ComId = UserInfo.User.ComId;
                    sq.Status = "0";
                    sq.Sourse = "2";
                    new SZHL_QYIMB().Insert(sq);

                    SZHL_QYIM_LIST sql = new SZHL_QYIM_LIST();
                    sql.ChatId = sq.ChatId;
                    sql.FromUserName = sq.FromUserName;
                    sql.MsgType = "group";
                    sql.Name = sq.Name;
                    sql.Owner = sq.Owner;
                    sql.Sourse = "1";
                    sql.Status = "0";
                    sql.UserList = sq.UserList;
                    new SZHL_QYIM_LISTB().Insert(sql);

                    SZHL_TXSX TX = new SZHL_TXSX();
                    TX.Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                    TX.APIName = "QYIM";
                    TX.ComId = UserInfo.User.ComId;
                    TX.FunName = "SENDWXIM";
                    TX.TXMode = "QYIM";
                    TX.CRUserRealName = UserInfo.User.UserRealName;
                    TX.MsgID = sq.ID.ToString();

                    TX.CRUser = UserInfo.User.UserName;
                    TXSX.TXSXAPI.AddALERT(TX); //时间为发送时间
                }

                msg.Result = sq;
            }

        } 
        #endregion

        #region 会话变更
        /// <summary>
        /// 会话变更
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void UPDATEQYIM(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_QYIM sq = JsonConvert.DeserializeObject<SZHL_QYIM>(P1);

            if (sq.ID == 0)
            {
                sq.FromUserName = UserInfo.User.UserName;
                sq.Owner = UserInfo.User.UserName;
                sq.MsgType = "event";
                sq.Event = "update_chat";
                sq.CreateTime = DateTime.Now;
                sq.ComId = UserInfo.User.ComId;
                sq.Status = "0";
                sq.Sourse = "2";
                new SZHL_QYIMB().Insert(sq);

                SZHL_QYIM_LIST sql1 = new SZHL_QYIM_LISTB().GetEntities(p => p.ComId == UserInfo.User.ComId && p.ChatId == sq.ChatId && p.Status == "0").FirstOrDefault();
                if (sql1 != null)
                {
                    if (!string.IsNullOrEmpty(sq.Name))
                    {
                        sql1.Name = sq.Name;
                    }
                    if (!string.IsNullOrEmpty(sq.Owner))
                    {
                        sql1.Owner = sq.Owner;
                    }
                    if (!string.IsNullOrEmpty(sq.AddUserList))
                    {
                        sql1.UserList = sql1.UserList + "|" + sq.AddUserList;
                    }
                    if (!string.IsNullOrEmpty(sq.DelUserList))
                    {
                        string[] dul = sq.DelUserList.Split('|');
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
                    new SZHL_QYIM_LISTB().Update(sql1);
                }

                SZHL_TXSX TX = new SZHL_TXSX();
                TX.Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                TX.APIName = "QYIM";
                TX.ComId = UserInfo.User.ComId;
                TX.FunName = "SENDWXIM";
                TX.TXMode = "QYIM";
                TX.CRUserRealName = UserInfo.User.UserRealName;
                TX.MsgID = sq.ID.ToString();

                TX.CRUser = UserInfo.User.UserName;
                TXSX.TXSXAPI.AddALERT(TX); //时间为发送时间
            }

            msg.Result = sq;

        } 
        #endregion

        #region 退出会话
        /// <summary>
        /// 退出会话
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void QUITQYIM(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_QYIM sq = JsonConvert.DeserializeObject<SZHL_QYIM>(P1);

            if (sq.ID == 0)
            {
                sq.FromUserName = UserInfo.User.UserName;
                sq.Owner = UserInfo.User.UserName;
                sq.MsgType = "event";
                sq.Event = "quit_chat";
                sq.CreateTime = DateTime.Now;
                sq.ComId = UserInfo.User.ComId;
                sq.Status = "0";
                sq.Sourse = "2";
                new SZHL_QYIMB().Insert(sq);

                SZHL_QYIM_LIST sql1 = new SZHL_QYIM_LISTB().GetEntities(p => p.ComId == UserInfo.User.ComId && p.ChatId == sq.ChatId && p.Status == "0").FirstOrDefault();
                if (sql1 != null)
                {
                    if (!string.IsNullOrEmpty(sql1.UserList))
                    {
                        string uis = string.Empty;
                        string[] strs = sql1.UserList.Split('|');
                        foreach (string s in strs)
                        {
                            bool bl = true;

                            if (s == sq.FromUserName)
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
                        new SZHL_QYIM_LISTB().Update(sql1);
                    }
                }

                SZHL_TXSX TX = new SZHL_TXSX();
                TX.Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                TX.APIName = "QYIM";
                TX.ComId = UserInfo.User.ComId;
                TX.FunName = "SENDWXIM";
                TX.TXMode = "QYIM";
                TX.CRUserRealName = UserInfo.User.UserRealName;
                TX.MsgID = sq.ID.ToString();

                TX.CRUser = UserInfo.User.UserName;
                TXSX.TXSXAPI.AddALERT(TX); //时间为发送时间
            }

            msg.Result = sq;

        }
        #endregion

        #region 发送消息
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void SENDITEM(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_QYIM_ITEM sqi = JsonConvert.DeserializeObject<SZHL_QYIM_ITEM>(P1);

            if (sqi.ID == 0)
            {
                sqi.FromUserName = UserInfo.User.UserName;
                sqi.CreateTime = DateTime.Now;
                sqi.ComId = UserInfo.User.ComId;
                sqi.Status = "0";
                sqi.Sourse = "2";
                new SZHL_QYIM_ITEMB().Insert(sqi);

                SZHL_TXSX TX = new SZHL_TXSX();
                TX.Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                TX.APIName = "QYIM";
                TX.ComId = UserInfo.User.ComId;
                TX.FunName = "SENDWXMSG";
                TX.TXMode = "QYIM";
                TX.CRUserRealName = UserInfo.User.UserRealName;
                TX.MsgID = sqi.ID.ToString();
                TX.CRUser = UserInfo.User.UserName;
                TXSX.TXSXAPI.AddALERT(TX); //时间为发送时间
            }

            msg.Result = sqi;

        } 
        #endregion

        #region 微信操作
        /// <summary>
        /// 微信会话变更
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void SENDWXIM(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            var tx = JsonConvert.DeserializeObject<SZHL_TXSX>(P1);

            int msgid = Int32.Parse(tx.MsgID);

            var sq = new SZHL_QYIMB().GetEntity(p => p.ID == msgid);
            UserInfo = new JH_Auth_UserB().GetUserInfo(tx.ComId.Value, sq.FromUserName);
            if (sq != null)
            {
                WXHelp wx = new WXHelp(UserInfo.QYinfo);
                if (sq.Event == "create_chat")
                {
                    wx.WX_CreateChat(sq.ChatId, sq.Name, sq.Owner, sq.UserList.Split('|'));
                }
                if (sq.Event == "update_chat")
                {
                    string[] aul = null;
                    string[] dul = null;
                    if (!string.IsNullOrEmpty(sq.AddUserList))
                    {
                        aul = sq.AddUserList.Split('|');
                    }
                    if (!string.IsNullOrEmpty(sq.DelUserList))
                    {
                        dul = sq.DelUserList.Split('|');
                    }
                    wx.WX_UpdateChat(sq.ChatId, sq.FromUserName, sq.Name, sq.Owner, aul, dul);
                }
                if (sq.Event == "quit_chat")
                {
                    wx.WX_QuitChat(sq.ChatId, sq.FromUserName);
                }
            }
        }
        /// <summary>
        /// 微信会话消息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void SENDWXMSG(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            var tx = JsonConvert.DeserializeObject<SZHL_TXSX>(P1);

            int msgid = Int32.Parse(tx.MsgID);

            var sq = new SZHL_QYIM_ITEMB().GetEntity(p => p.ID == msgid);
            UserInfo = new JH_Auth_UserB().GetUserInfo(tx.ComId.Value, sq.FromUserName);
            if (sq != null)
            {
                WXHelp wx = new WXHelp(UserInfo.QYinfo);

                Senparc.Weixin.QY.Chat_Type type = Senparc.Weixin.QY.Chat_Type.single;
                Senparc.Weixin.QY.ChatMsgType msgtype = Senparc.Weixin.QY.ChatMsgType.text;
                string strcm = sq.Content;
                if (sq.Type == "group")
                {
                    type = Senparc.Weixin.QY.Chat_Type.group;
                }
                if (sq.MsgType == "image")
                {
                    msgtype = Senparc.Weixin.QY.ChatMsgType.image;
                    strcm = sq.MediaId;
                }
                if (sq.MsgType == "file")
                {
                    msgtype = Senparc.Weixin.QY.ChatMsgType.file;
                    strcm = sq.MediaId;
                }

                wx.WX_SendChatMessage(sq.FromUserName, type, msgtype, sq.UID, strcm);
            }
        } 
        #endregion
    }
}