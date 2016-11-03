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
using System.IO;

namespace QJY.API
{
    public class DBGLManage : IWsService
    {
        public void ProcessRequest(HttpContext context, ref Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            MethodInfo methodInfo = typeof(DBGLManage).GetMethod(msg.Action.ToUpper());
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
                if (!Directory.Exists(path + "/dbbackup/"))
                {
                    Directory.CreateDirectory(path + "/dbbackup/");
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

        #region 数据库还原
        /// <summary>
        /// 数据库还原
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void DBRESTORE(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                HttpPostedFile _upfile = context.Request.Files["upFile"];
                if (_upfile == null)
                {
                    msg.ErrorMsg = "请选择要上传的文件 ";
                }
                else
                {
                    string path = context.Server.MapPath("/");

                    string fileName = _upfile.FileName;/*获取文件名： C:\Documents and Settings\Administrator\桌面\123.jpg*/
                    string suffix = fileName.Substring(fileName.LastIndexOf(".") + 1).ToLower();/*获取后缀名并转为小写： jpg*/

                    if (suffix == "bak")
                    {
                        byte[] buffer = new Byte[(int)_upfile.InputStream.Length]; //声明文件长度的二进制类型
                        _upfile.InputStream.Read(buffer, 0, buffer.Length); //将文件转成二进制

                        if (!Directory.Exists(path + "/dbupload/"))
                        {
                            Directory.CreateDirectory(path + "/dbupload/");
                        }
                        path = path + "/dbupload/db_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".bak";
                        FileStream fos = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                        fos.Write(buffer, 0, buffer.Length);
                        fos.Close();

                        string strsql = "use master;declare @name varchar(max);SELECT @name= DB_NAME(dbid) FROM master.dbo.sysprocesses WHERE status='runnable';restore database @name to disk='" + path + "';";

                        new JH_Auth_QYB().ExsSql(strsql);
                    }
                    else
                    {
                        msg.ErrorMsg = "请选择.bak文件 ";
                    }
                }
            }
            catch (Exception ex)
            {
                msg.ErrorMsg = "还原失败!";
                new JH_Auth_LogB().Insert(new JH_Auth_Log()
                {
                    ComId = UserInfo.QYinfo.ComId.ToString(),
                    LogType = "DBGL",
                    LogContent = ex.ToString(),
                    CRUser = UserInfo.User.UserName,
                    CRDate = DateTime.Now
                });
            }
        }
        #endregion
    }
}
