using System.Reflection;
using System.Web;
using FastReflectionLib;
using QJY.API;
using QJY.Data;
using System.Data;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;

namespace QJY.API
{
    public class CHATManage : IWsService
    {
        public void ProcessRequest(HttpContext context, ref Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            MethodInfo methodInfo = typeof(CHATManage).GetMethod(msg.Action.ToUpper());
            methodInfo.FastInvoke(new CHATManage(), new object[] { context, msg, P1, P2, UserInfo });
        }

        /// <summary>
        /// 初始化聊天窗口
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void INITCHAT(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_CHAT_MSGB msgB = new SZHL_CHAT_MSGB();

            string userName = UserInfo.User.UserName;

            //获取当前用户信息
            msg.Result = UserInfo.User;

            //获取群组
            msg.Result1 = new SZHL_CHAT_GROUPB().GetDTByCommand("select * from SZHL_CHAT_GROUP where id in (select groupid from SZHL_CHAT_GROUPUSER where UserName='" + userName + "')");

            //获取组织机构
            string branchId = new JH_Auth_BranchB().GetBranchQX(UserInfo);
            msg.Result2 = new JH_Auth_BranchB().GetBranchList(-1, UserInfo.User.ComId.Value, branchId);

            //获取最近聊天信息
            msg.Result3 = msgB.GetDTByCommand("exec ChatInit @User = N'" + UserInfo.User.UserName + "',@ComId=" + UserInfo.User.ComId.Value + "");
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void SENDMSG(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int msgId = 0;
            if (P2 == "0")
            {
                SZHL_CHAT_MSGB mgb = new SZHL_CHAT_MSGB();
                SZHL_CHAT_MSG chatMsg = JsonConvert.DeserializeObject<SZHL_CHAT_MSG>(P1);

                chatMsg.FromUser = UserInfo.User.UserName;
                chatMsg.CRDate = DateTime.Now;
                chatMsg.ComId = UserInfo.User.ComId.Value;

                if (chatMsg.ConverId == -1)
                {
                    chatMsg.ConverId = int.Parse(mgb.ExsSclarSql("select max(ConverId) from SZHL_CHAT_MSG").ToString()) + new Random().Next(1, 100);
                }

                mgb.Insert(chatMsg);

                chatMsg.ID = int.Parse(mgb.ExsSclarSql("select max(id) from SZHL_CHAT_MSG").ToString());

                msg.Result = chatMsg;
            }
            else if (P2 == "1")
            {
                SZHL_CHAT_MSG_GROUPB cmgb = new SZHL_CHAT_MSG_GROUPB();
                SZHL_CHAT_MSG_GROUP_USERB cmgub = new SZHL_CHAT_MSG_GROUP_USERB();

                SZHL_CHAT_MSG_GROUP chatGroupMsg = JsonConvert.DeserializeObject<SZHL_CHAT_MSG_GROUP>(P1);

                chatGroupMsg.FromUser = UserInfo.User.UserName;
                chatGroupMsg.CRDate = DateTime.Now;
                chatGroupMsg.ComId = UserInfo.User.ComId.Value;

                cmgb.Insert(chatGroupMsg);

                msgId = int.Parse(cmgb.ExsSclarSql("select max(id) from SZHL_CHAT_MSG_GROUP").ToString());

                chatGroupMsg.ID = msgId;

                IEnumerable<SZHL_CHAT_GROUPUSER> groupUserList = new SZHL_CHAT_GROUPUSERB().GetEntities(d => d.GroupId == chatGroupMsg.GroupId);
                List<SZHL_CHAT_MSG_GROUP_USER> msgList = new List<SZHL_CHAT_MSG_GROUP_USER>();

                foreach (SZHL_CHAT_GROUPUSER groupUser in groupUserList)
                {
                    SZHL_CHAT_MSG_GROUP_USER cmgu = new SZHL_CHAT_MSG_GROUP_USER
                    {
                        GroupId = chatGroupMsg.GroupId,
                        MsgId = msgId,
                        IsReceived = 0,
                        ReceiveTime = null,
                        ToUser = groupUser.UserName,
                        ToUserDeleteFlag = 0,
                        ToUserDeleteTime = null
                    };

                    if (groupUser.UserName == chatGroupMsg.FromUser)
                    {
                        cmgu.IsReceived = 1;
                        cmgu.ReceiveTime = DateTime.Now;
                    }

                    msgList.Add(cmgu);
                }

                cmgub.Insert(msgList);

                msg.Result = chatGroupMsg;
            }
        }

        /// <summary>
        /// 获取群组用户列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETGROUPUSER(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            msg.Result = new SZHL_CHAT_GROUPB().GetDTByCommand("SELECT JAU.* FROM dbo.JH_Auth_User AS JAU INNER JOIN dbo.SZHL_CHAT_GROUPUSER AS SCG ON JAU.UserName = SCG.UserName AND JAU.ComId = SCG.ComId WHERE SCG.GroupId=" + P1);
        }

        /// <summary>
        /// 会话列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETCONVERSATION(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_CHAT_MSGB msgB = new SZHL_CHAT_MSGB();

            string sql = "";
            string id = context.Request.QueryString["p"];
            string type = context.Request.QueryString["t"];
            const string msgCount = "20";

            //普通消息
            if (P2 == "0")
            {
                sql = "SELECT TOP " + msgCount + " M.*,U.UserRealName AS FromUserRealName FROM dbo.SZHL_CHAT_MSG AS M INNER JOIN dbo.JH_Auth_User AS U ON M.FromUser = U.UserName AND M.ComId = U.ComId WHERE M.ConverId= " + P1 + (string.IsNullOrEmpty(id) ? "" : (" AND M.ID " + (type == "1" ? "> " : "< ") + id)) + " ORDER BY M.ConverId ASC,M.ID DESC";

                msg.Result = msgB.GetDTByCommand(sql);

                if (string.IsNullOrEmpty(id))
                {
                    msgB.ExsSql("update SZHL_CHAT_MSG set IsReceived=1, ReceiveTime = GETDATE() where IsReceived=0 and ConverId= " + P1 + " and ToUser = '" + UserInfo.User.UserName + "'");
                }
            }
            //群组消息
            else
            {
                sql = "SELECT T1.*,SCG.GroupName,JAU.UserRealName AS FromUserRealName FROM (SELECT TOP " + msgCount + " * FROM dbo.SZHL_CHAT_MSG_GROUP WHERE GroupId = " + P1 + (string.IsNullOrEmpty(id) ? "" : (" AND ID " + (type == "1" ? "> " : "< ") + id)) + " ORDER BY ID DESC) AS T1 INNER JOIN dbo.SZHL_CHAT_GROUP AS SCG ON T1.GroupId = SCG.ID INNER JOIN dbo.JH_Auth_User AS JAU ON T1.FromUser = JAU.UserName AND T1.ComId = JAU.ComId ORDER BY T1.ID DESC";

                msg.Result = msgB.GetDTByCommand(sql);

                if (string.IsNullOrEmpty(id))
                {
                    msgB.ExsSql("UPDATE dbo.SZHL_CHAT_MSG_GROUP_USER SET IsReceived=1,ReceiveTime = GETDATE() WHERE IsReceived=0 and GroupId=" + P1 + " AND ToUser = '" + UserInfo.User.UserName + "'");
                }
            }
        }

        /// <summary>
        /// 查询未读消息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETNOREADMSG(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_CHAT_MSGB msgb = new SZHL_CHAT_MSGB();

            //查询普通未读消息
            string sql = "SELECT M.*,U.UserRealName AS FromUserRealName FROM dbo.SZHL_CHAT_MSG AS M INNER JOIN dbo.JH_Auth_User AS U ON M.ComId = U.ComId AND M.FromUser = U.UserName WHERE M.ToUser = '" + UserInfo.User.UserName + "' AND IsReceived=0 AND M.ComId = " + UserInfo.User.ComId + " ORDER BY M.ID ASC";

            DataTable dtMsg = msgb.GetDTByCommand(sql);

            //更新为已读
            if (dtMsg.Rows.Count > 0)
            {
                string converId = dtMsg.Rows[0]["ConverId"].ToString();
                msgb.ExsSql("update SZHL_CHAT_MSG set IsReceived=1, ReceiveTime = GETDATE() where ConverId= " + converId + " and ToUser = '" + UserInfo.User.UserName + "'");
            }

            //查询群组未读消息
            sql = "SELECT SCMG.*,SCG.GroupName,JAU.UserRealName AS FromUserRealName FROM dbo.SZHL_CHAT_MSG_GROUP AS SCMG INNER JOIN dbo.JH_Auth_User JAU ON SCMG.ComId = JAU.ComId AND SCMG.FromUser = JAU.UserName INNER JOIN dbo.SZHL_CHAT_GROUP AS SCG ON SCMG.GroupId = SCG.ID INNER JOIN dbo.SZHL_CHAT_MSG_GROUP_USER AS SCMGU ON SCMG.ID = SCMGU.MsgId WHERE SCMGU.IsReceived=0 AND SCMGU.ToUser='" + UserInfo.User.UserName + "' AND SCMG.ComId=" + UserInfo.User.ComId + " ORDER BY SCMG.ID ASC";

            DataTable dtGroupMsg = msgb.GetDTByCommand(sql);

            //更新为已读
            if (dtGroupMsg.Rows.Count > 0)
            {
                string groupId = dtGroupMsg.Rows[0]["GroupId"].ToString();
                msgb.ExsSql("UPDATE dbo.SZHL_CHAT_MSG_GROUP_USER SET IsReceived=1,ReceiveTime = GETDATE() WHERE GroupId=" + groupId + " AND ToUser = '" + UserInfo.User.UserName + "'");
            }

            msg.Result = dtMsg;
            msg.Result1 = dtGroupMsg;
        }

        /// <summary>
        /// 创建群组
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void CREATEGROUP(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string groupName = P1;
            string personCodeString = P2;

            SZHL_CHAT_GROUP group = new SZHL_CHAT_GROUP();
            SZHL_CHAT_GROUPB groupB = new SZHL_CHAT_GROUPB();
            group.ComId = UserInfo.User.ComId.Value;
            group.GroupName = groupName;
            group.GroupPerson = string.Empty;
            group.CRUser = UserInfo.User.UserName;
            group.CRDate = DateTime.Now;

            bool result = groupB.Insert(group);

            if (result)
            {
                int groupId = int.Parse(groupB.ExsSclarSql("select max(id) from SZHL_CHAT_GROUP").ToString());

                string[] userList = personCodeString.Split(',');
                SZHL_CHAT_GROUPUSERB groupUserB = new SZHL_CHAT_GROUPUSERB();
                groupUserB.Insert(new SZHL_CHAT_GROUPUSER
                {
                    ComId = UserInfo.User.ComId.Value,
                    GroupId = groupId,
                    UserName = UserInfo.User.UserName
                });

                foreach (string userName in userList)
                {
                    SZHL_CHAT_GROUPUSER groupUser = new SZHL_CHAT_GROUPUSER();
                    groupUser.ComId = UserInfo.User.ComId.Value;
                    groupUser.GroupId = groupId;
                    groupUser.UserName = userName;
                    groupUserB.Insert(groupUser);
                }

                msg.Result = groupB.GetEntity(d => d.ID == groupId);
            }
        }

        /// <summary>
        /// 获取群组列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETGROUPLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            msg.Result = new SZHL_CHAT_GROUPB().GetDTByCommand("select * from SZHL_CHAT_GROUP where id in (select groupid from SZHL_CHAT_GROUPUSER where UserName='" + UserInfo.User.UserName + "')");
        }

        /// <summary>
        /// 退出群组/解散群组
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void EXITGROUP(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int groupId = int.Parse(P1);
            string type = P2;

            if (type == "delete")
            {
                new SZHL_CHAT_MSG_GROUPB().Delete(d => d.GroupId == groupId);

                new SZHL_CHAT_MSG_GROUP_USERB().Delete(d => d.GroupId == groupId);

                new SZHL_CHAT_GROUPUSERB().Delete(d => d.GroupId == groupId);

                new SZHL_CHAT_GROUPB().Delete(d => d.ID == groupId);
            }
            else if (type == "exit")
            {
                new SZHL_CHAT_MSG_GROUPB().Delete(d => d.GroupId == groupId && d.FromUser == UserInfo.User.UserName);

                new SZHL_CHAT_MSG_GROUP_USERB().Delete(d => d.GroupId == groupId && d.ToUser == UserInfo.User.UserName);

                new SZHL_CHAT_GROUPUSERB().Delete(d => d.GroupId == groupId && d.UserName == UserInfo.User.UserName);
            }
        }

        /// <summary>
        /// 搜索
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void SEARCHPERSON(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_CHAT_GROUPB groupB = new SZHL_CHAT_GROUPB();

            //查询联系人
            DataTable dtPerson = groupB.GetDTByCommand("select top 8 * from JH_Auth_User where ComId = " + UserInfo.User.ComId + " and userrealname like '%" + P1 + "%' and username <> '" + UserInfo.User.UserName + "'");

            //查询群组
            DataTable dtGroup = groupB.GetDTByCommand("select top 8 * from SZHL_CHAT_GROUP where ComId = " + UserInfo.User.ComId + " and groupname like '%" + P1 + "%'");

            if (dtPerson.Rows.Count > 3 && dtGroup.Rows.Count > 3)
            {
                while (dtPerson.Rows.Count > 3)
                {
                    dtPerson.Rows.RemoveAt(3);
                }

                while (dtGroup.Rows.Count > 3)
                {
                    dtGroup.Rows.RemoveAt(3);
                }
            }
            else if (dtPerson.Rows.Count > 3 && dtGroup.Rows.Count <= 3)
            {
                while (dtPerson.Rows.Count > 6 - dtGroup.Rows.Count)
                {
                    dtPerson.Rows.RemoveAt(dtPerson.Rows.Count - 1);
                }
            }
            else if (dtGroup.Rows.Count > 3 && dtPerson.Rows.Count <= 3)
            {
                while (dtGroup.Rows.Count > 6 - dtPerson.Rows.Count)
                {
                    dtGroup.Rows.RemoveAt(dtGroup.Rows.Count - 1);
                }
            }

            msg.Result = dtPerson;
            msg.Result1 = dtGroup;
        }

        /// <summary>
        /// 根据用户编码获取用户信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETPERSON(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            JH_Auth_User user = new JH_Auth_UserB().GetEntity(d => d.ComId == UserInfo.User.ComId && d.UserName == P1);
            if (user != null)
            {
                JH_Auth_Branch dept = new JH_Auth_BranchB().GetEntity(d => d.ComId == UserInfo.User.ComId && d.DeptCode == user.BranchCode);

                msg.Result = user;
                msg.Result1 = dept;
            }
        }

        /// <summary>
        /// 查询未读消息数量
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETALLNOREADCOUNT(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_CHAT_MSGB msgB = new SZHL_CHAT_MSGB();

            int msgCount = msgB.ExsSclarSql("select count(0) from SZHL_CHAT_MSG where touser = '" + UserInfo.User.UserName + "' and IsReceived=0 and ComId=" + UserInfo.User.ComId).ToInt32();

            int groupMsgCount = msgB.ExsSclarSql("select count(0) from SZHL_CHAT_MSG_GROUP_USER where touser = '" + UserInfo.User.UserName + "' and IsReceived=0 and ComId=" + UserInfo.User.ComId).ToInt32();

            msg.Result = msgCount + groupMsgCount;
        }
    }
}