using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Sockets;

namespace QJY.API
{
    public class FileHelp
    {


        public void CoverOffice(string strMD5, string strName, string strType, JH_Auth_UserB.UserInfo UserInfo)
        {
            string CovOfficeUrl = CommonHelp.GetConfig("Covoffice").ToString();
            if (CovOfficeUrl != "")
            {
                Task<string> TaskCover = Task.Factory.StartNew<string>(() =>
                {
                    Dictionary<String, String> DATA = new Dictionary<String, String>();
                    DATA.Add("downloadUrl", UserInfo.QYinfo.FileServerUrl + strMD5);
                    if (strType.ToLower().Contains("pdf"))
                    {
                        DATA.Add("convertType", "14");

                    }
                    else
                    {
                        DATA.Add("convertType", "0");
                    }
                    HttpWebResponse ResponseData = CommonHelp.CreatePostHttpResponse(CovOfficeUrl, DATA, 0, "", null);
                    string Returndata = CommonHelp.GetResponseString(ResponseData);
                    JObject json = (JObject)JsonConvert.DeserializeObject(Returndata);
                    JArray array = (JArray)json["data"];
                    string strUrl = array[0].ToString();

                    string strYear = DateTime.Now.Year.ToString();
                    string strMonth = DateTime.Now.Month.ToString();
                    string strUrl1 = string.Empty;
                    string strSum = GetWDPages(CovOfficeUrl, UserInfo.QYinfo.FileServerUrl + strMD5, strType);
                    string strPath = UserInfo.QYinfo.QYCode + "/" + strYear + "/" + strMonth + strUrl.Substring(strUrl.LastIndexOf('/'), strUrl.Length - strUrl.LastIndexOf('/'));
                    string strWDIP = CommonHelp.GetConfig("WDIP");
                    string strWDDK = CommonHelp.GetConfig("WDDK");

                    if (strWDIP != "" && strWDDK != "")
                    {
                        IPAddress ip = IPAddress.Parse(strWDIP);
                        Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        try
                        {
                            string str = strUrl.Substring(strUrl.LastIndexOf('/') + 1, strUrl.Length - strUrl.LastIndexOf('/') - 6);
                            strUrl1 = "/ViewV5/Base/" + strType.Substring(0, 3) + ".html?code=" + str;
                            int dk = Int32.Parse(strWDDK);
                            clientSocket.Connect(new IPEndPoint(ip, dk)); //配置服务器IP与端口
                            clientSocket.Send(Encoding.ASCII.GetBytes(str + "_" + UserInfo.QYinfo.QYCode + "_" + strYear + "_" + strMonth + "_" + strType));//发送请求到服务器
                            //采用新接口获取页码
                            string SQL = string.Format("update FT_File set YLUrl='{0}',ISYL='Y',YLCode='{2}',YLPath='{3}',YLCount='{4}' WHERE FileMD5='{1}'", strUrl1, strMD5, str, strPath.Substring(0, strPath.Length - 5), strSum);
                            Debug.WriteLine("d" + DateTime.Now);

                            new FT_FileB().ExsSclarSql(SQL);


                        }
                        catch (Exception ex)
                        {
                            CommonHelp.WriteLOG(ex.ToString());
                        }
                    }

                    return strUrl.Replace(":8000", "");

                });

                TaskCover.ContinueWith((task) =>
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine(DateTime.Now);
                        string strUrl = TaskCover.Result;

                    }
                    catch (Exception ex)
                    {
                        CommonHelp.WriteLOG(ex.ToString());
                    }
                });

            }

        }


        public string GetWDPages(string CovOfficeUrl, string downloadUrl, string strType)
        {
            Dictionary<String, String> DATA = new Dictionary<String, String>();
            DATA.Add("downloadUrl", downloadUrl);
            if (strType.ToLower().Contains("pdf"))
            {
                DATA.Add("convertType", "28");

            }
            else
            {
                DATA.Add("convertType", "27");
            }
            HttpWebResponse ResponseData = CommonHelp.CreatePostHttpResponse(CovOfficeUrl, DATA, 0, "", null);
            string Returndata = CommonHelp.GetResponseString(ResponseData);
            JObject json = (JObject)JsonConvert.DeserializeObject(Returndata);
            Returndata = json.GetValue("pagecount").ToString();
            return Returndata;
        }
        /// <summary>
        /// 压缩需求
        /// </summary>
        /// <param name="strFileData"></param>
        /// <param name="UserInfo"></param>
        /// <returns></returns>
        public string CompressZip(string strFileData, JH_Auth_UserB.UserInfo UserInfo)
        {
            Dictionary<String, String> DATA = new Dictionary<String, String>();
            DATA.Add("data", strFileData);
            HttpWebResponse ResponseData = CommonHelp.CreatePostHttpResponse(UserInfo.QYinfo.FileServerUrl + "zipfolder", DATA, 0, "", null);
            return CommonHelp.GetResponseString(ResponseData);
        }


        /// <summary>
        /// 注册新的文件接口
        /// 传入三个参数，qycode name 和description  第一个不能为空， 后两个可以为空，  第一个如果已经插入，就返回错误， 插入正确就返回true
        /// </summary>
        /// <param name="qycode"></param>
        /// <param name="strQYName"></param>
        /// <returns></returns>
        public void AddQycode(string qycode, string strQYName)
        {
            Dictionary<String, String> DATA = new Dictionary<String, String>();
            DATA.Add("qycode", qycode);
            DATA.Add("name", strQYName);
            string strFileAPIRegUrl = CommonHelp.GetConfig("FileAPIReg").ToString() + "addqycode";
            HttpWebResponse ResponseData = CommonHelp.CreatePostHttpResponse(strFileAPIRegUrl, DATA, 0, "", null);
            CommonHelp.GetResponseString(ResponseData);
        }
        //获取文件服务器
        public string GetFileServerUrl(string qycode)
        {
            string strFileAPIRegUrl = CommonHelp.GetConfig("FileAPIReg").ToString() + qycode + "/document/";
            return strFileAPIRegUrl;
        }
    }
}
