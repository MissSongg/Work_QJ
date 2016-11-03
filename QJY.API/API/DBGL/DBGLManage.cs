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
using Senparc.Weixin.QY.Entities;

namespace QJY.API
{
    public class DBGLManage : IWsService
    {
        public void ProcessRequest(HttpContext context, ref Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            MethodInfo methodInfo = typeof(DCGLManage).GetMethod(msg.Action.ToUpper());
            DBGLManage model = new DBGLManage();
            methodInfo.FastInvoke(model, new object[] { context, msg, P1, P2, UserInfo });
        }

        #region 数据库备份
        /// <summary>
        /// 数据库备份
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void DBBACKUP(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                string path = context.Server.MapPath("/");
                if (!System.IO.Directory.Exists(path + "/dbbackup/"))
                {
                    System.IO.Directory.CreateDirectory(path + "/dbbackup/");
                }
                path = path + "/dbbackup/db_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".bak";
                string strsql = "use master;declare @name varchar(max);SELECT @name= DB_NAME(dbid) FROM master.dbo.sysprocesses WHERE status='runnable';backup database @name to disk='" + path + "';";

                new JH_Auth_QYB().ExsSql(strsql);
            }
            catch (Exception ex)
            {
                msg.ErrorMsg = "备份失败!";
                new JH_Auth_LogB().Insert(new JH_Auth_Log()
                {
                    ComId=UserInfo.QYinfo.ComId.ToString(),
                    LogType = "DBGL",
                    LogContent = ex.ToString(),
                    CRUser=UserInfo.User.UserName,
                    CRDate = DateTime.Now
                });
            }
        }
        #endregion
    }
}
