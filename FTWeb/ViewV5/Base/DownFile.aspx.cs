
using QJY.Data;
using QJY.API;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace QjySaaSWeb.ViewV5.Base
{
    public partial class DownFile : Page
    {

        public string type
        {
            get { return Request.QueryString["type"] ?? "file"; }
        }

        public string MD5
        {
            get { return Request.QueryString["MD5"] ?? ""; }
        }
        public string Name
        {
            get { return Request.QueryString["Name"] ?? "测试文件"; }
        }
        public string FileId
        {
            get { return Request.QueryString["fileId"] ?? ""; }
        }
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {

                JH_Auth_UserB.UserInfo UserInfo = new JH_Auth_UserB.UserInfo();
                string userName = Request.QueryString["user"] ?? "";
                if (Request.Cookies["szhlcode"] != null)
                {
                    UserInfo = new JH_Auth_UserB().GetUserInfo(Request.Cookies["szhlcode"].Value.ToString());

                    if (type == "TX" && !string.IsNullOrEmpty(userName))
                    {
                        JH_Auth_User userinfo = new JH_Auth_UserB().GetUserByUserName(UserInfo.QYinfo.ComId, userName);
                        if (userinfo != null)
                        {
                            string filename = "";
                            if (userinfo.UserLogoId != null)
                            {
                                FT_File file = new FT_FileB().GetEntity(d => d.ID == userinfo.UserLogoId);
                                List<string> extends = new List<string>() { "jpg", "png", "gif", "jpeg" };
                                if (!extends.Contains(file.FileExtendName.ToLower()))//文件不是图片的不返回地址，此方法只用于图片查看
                                {
                                    return;
                                }
                                filename = UserInfo.QYinfo.FileServerUrl + file.FileMD5;
                                Response.AddHeader("Content-Disposition", "attachment;filename=" + file.Name);
                                Response.ContentType = "application/octet-stream";
                            }
                            else if (!string.IsNullOrEmpty(userinfo.txurl))
                            {
                                Response.AddHeader("Content-Disposition", "attachment;filename=" + Name);
                                Response.ContentType = "application/octet-stream";
                                filename = userinfo.txurl;

                            }
                            else
                            {

                                Response.AddHeader("Content-Disposition", "attachment;filename=" + Name);
                                Response.ContentType = "application/octet-stream";
                                filename = "/ViewV5/images/tx.png";
                            }
                            Response.Redirect(filename);
                            return;
                        }
                    }

                }



                if (!string.IsNullOrEmpty(FileId))
                {
                    string filename = "";
                    int fileId = int.Parse(FileId.Split(',')[0]);
                    FT_File file = new FT_FileB().GetEntity(d => d.ID == fileId);
                    List<string> extends = new List<string>() { "jpg", "png", "gif", "jpeg" };
                    if (!extends.Contains(file.FileExtendName.ToLower()))//文件不是图片的不返回地址，此方法只用于图片查看
                    {
                        Response.AddHeader("Content-Disposition", "attachment;filename=" + Name);
                        Response.ContentType = "application/octet-stream";
                        filename = "/ViewV5/images/qywd/" + file.FileExtendName + ".png";
                    }
                    else
                    {
                        string width = Request["width"] ?? "";
                        string height = Request["height"] ?? "";
                        Response.AddHeader("Content-Disposition", "attachment;filename=" + file.Name);
                        Response.ContentType = "application/octet-stream";
                        filename = UserInfo.QYinfo.FileServerUrl + file.FileMD5;
                        if(width+height!="")
                        {
                            filename = UserInfo.QYinfo.FileServerUrl + "thumbnail/" + file.FileMD5 + (width + height != "" ? ("/" + width + "/" + height) : "");
                        
                        }
                       
                    }

                    Response.Redirect(filename);
                    return;
                }

                if (type == "file" && string.IsNullOrEmpty(FileId))
                {
                    Response.AddHeader("Content-Disposition", "attachment;filename=" + Name);
                    Response.ContentType = "application/octet-stream";
                    string filename = UserInfo.QYinfo.FileServerUrl + MD5;
                    Response.Redirect(filename);
                }
                if (type == "folder")
                {

                    Response.AddHeader("Content-Disposition", "attachment;filename=" + Name);
                    Response.ContentType = "application/octet-stream";
                    string filename = UserInfo.QYinfo.FileServerUrl + "zipfile/" + MD5;
                    Response.Redirect(filename);

                }

            }
            catch (Exception ex) { }
            // Response.ContentType = "application/x-zip-compressed";
        }
    }
}