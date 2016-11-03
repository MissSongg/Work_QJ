﻿using QJY.API;
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
using System.Text;

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

        #region 数据库备份还原列表
        /// <summary>
        /// 数据库备份还原列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETDBBRLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string userName = UserInfo.User.UserName;
            string strWhere = " 1=1 and ComId=" + UserInfo.User.ComId;

            int DataID = -1;
            int.TryParse(context.Request.QueryString["ID"] ?? "-1", out DataID);//记录Id
            if (DataID != -1)
            {
                string strIsHasDataQX = new JH_Auth_QY_ModelB().ISHASDATAREADQX("DBGL", DataID, UserInfo);
                if (strIsHasDataQX == "Y")
                {
                    strWhere += string.Format(" And ID = '{0}'", DataID);
                }

            }

            if (P1 != "")
            {
                int page = 0;
                int pagecount = 8;
                int.TryParse(context.Request.QueryString["p"] ?? "1", out page);
                int.TryParse(context.Request.QueryString["pagecount"] ?? "8", out pagecount);//页数
                page = page == 0 ? 1 : page;
                int total = 0;
                DataTable dt = new DataTable();
                switch (P1)
                {
                    case "0": //手机单条数据
                        {
                            //设置usercenter已读
                            new JH_Auth_User_CenterB().ReadMsg(UserInfo, DataID, "DBGL");
                        }
                        break;
                    case "1": //备份
                        {
                            strWhere += " And Type ='" + P1 + "'";
                        }
                        break;
                    case "2": //还原
                        {
                            strWhere += " And Type ='" + P1 + "'";
                        }
                        break;
                }
                dt = new SZHL_CRM_KHGLB().GetDataPager(" SZHL_DBGL ", " * ", pagecount, page, "  CRDate desc", strWhere, ref total);

                msg.Result = dt;
                msg.Result1 = total;
            }
        }
        #endregion

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

                FileInfo fi = new FileInfo(path);
                SZHL_DBGL sd = new SZHL_DBGL();
                sd.ComId = UserInfo.QYinfo.ComId;
                sd.Type = "1";
                sd.Name = fi.Name + ".bak";
                sd.Path = path;
                sd.Size = (fi.Length / 1024.00).ToString("F2") + " KB"; ;
                sd.CRUser = UserInfo.User.UserName;
                sd.CRDate = DateTime.Now;
                new SZHL_DBGLB().Insert(sd);
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

        #region 数据库下载
        /// <summary>
        /// 数据库下载
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void DBDOWNLOAD(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                int id = Int32.Parse(P1);
                var sd = new SZHL_DBGLB().GetEntity(p => p.ID == id);
                if (sd != null)
                {
                    FileInfo fi = new FileInfo(sd.Path);

                    // 设置编码和附件格式
                    context.Response.Clear();
                    context.Response.ClearHeaders();
                    context.Response.Buffer = false;
                    context.Response.ContentType = "application/octet-stream";
                    context.Response.ContentEncoding = Encoding.UTF8;
                    context.Response.Charset = "";
                    context.Response.AppendHeader("Content-Disposition",
                        "attachment;filename=" + HttpUtility.UrlEncode(sd.Name, Encoding.UTF8));

                    context.Response.AppendHeader("Content-Length", fi.Length.ToString());
                    context.Response.WriteFile(fi.FullName);
                    context.Response.End();
                }
                else
                {
                    msg.ErrorMsg = "下载失败!";
                }
            }
            catch (Exception ex)
            {
                msg.ErrorMsg = "下载失败!";
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

        #region 数据库上传
        /// <summary>
        /// 数据库上传
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void DBUPLOAD(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
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

                        DBRESTORE(context, msg, path, P2, UserInfo);
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

                string strsql = "use master;declare @name varchar(max);SELECT @name= DB_NAME(dbid) FROM master.dbo.sysprocesses WHERE status='runnable';restore database @name to disk='" + P1 + "';";

                new JH_Auth_QYB().ExsSql(strsql);

                FileInfo fi = new FileInfo(P1);
                SZHL_DBGL sd = new SZHL_DBGL();
                sd.ComId = UserInfo.QYinfo.ComId;
                sd.Type = "2";
                sd.Name = fi.Name + "." + fi.Extension;
                sd.Path = P1;
                sd.Size = (fi.Length / 1024.00).ToString("F2") + " KB"; ;
                sd.CRUser = UserInfo.User.UserName;
                sd.CRDate = DateTime.Now;
                new SZHL_DBGLB().Insert(sd);

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