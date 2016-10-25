using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FastReflectionLib;
using System.Web;
using QJY.API;
using QJY.Data;
using Newtonsoft.Json;
using System.Data;
using Senparc.Weixin.QY.Entities;


namespace QJY.API
{
    public class KDGLManage : IWsService
    {
        public void ProcessRequest(HttpContext context, ref Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            MethodInfo methodInfo = typeof(KDGLManage).GetMethod(msg.Action.ToUpper());
            KDGLManage model = new KDGLManage();
            methodInfo.FastInvoke(model, new object[] { context, msg, P1, P2, UserInfo });
        }

        #region 快递列表
        /// <summary>
        /// 快递列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETKDGLLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string userName = UserInfo.User.UserName;
            string strWhere = " 1=1 and ComId=" + UserInfo.User.ComId;

            string sts = context.Request["sts"] ?? "";
            if (sts != "")
            {
                strWhere += string.Format(" And Status='{0}'", P1);
            }

            int DataID = -1;
            int.TryParse(context.Request.QueryString["ID"] ?? "-1", out DataID);//记录Id
            if (DataID != -1)
            {
                string strIsHasDataQX = new JH_Auth_QY_ModelB().ISHASDATAREADQX("KDGL", DataID, UserInfo);
                if (strIsHasDataQX == "Y")
                {
                    strWhere += string.Format(" And ID = '{0}'", DataID);
                }

            }

            if (P1 != "")
            {
                int page = 0;
                int.TryParse(context.Request.QueryString["p"] ?? "1", out page);
                page = page == 0 ? 1 : page;
                int total = 0;
                DataTable dt = new DataTable();
                switch (P1)
                {
                    case "0": //手机单条数据
                        {
                            //设置usercenter已读
                            new JH_Auth_User_CenterB().ReadMsg(UserInfo, DataID, "KDGL");
                        }
                        break;
                    case "1": //所有的
                        {
                        }
                        break;
                    case "2": //自己的
                        {
                            strWhere += string.Format(" And JSUser='{0}'", userName);
                        }
                        break;
                }
                dt = new SZHL_CCXJB().GetDataPager("SZHL_KDGL ", "*", 8, page, " Status asc,CRDate desc", strWhere, ref total);

                msg.Result = dt;
                msg.Result1 = total;
            }
        }
        #endregion

        #region 我的快递
        /// <summary>
        /// 我的快递
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETMYKDGLLIST_PAGE(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strWhere = " ComId=" + UserInfo.User.ComId;
            //strWhere += string.Format(" And JSUser='{0}' ", UserInfo.User.UserName);
            if (P1 != "")
            {
                strWhere += string.Format(" And Status='{0}'", P1);
            }
            if (P2 == "")
            {
                strWhere += string.Format(" And JSUser='{0}'", UserInfo.User.UserName);
            }

            int page = 0;
            int.TryParse(context.Request.QueryString["p"] ?? "1", out page);
            page = page == 0 ? 1 : page;
            int total = 0;

            DataTable dt = new SZHL_CCXJB().GetDataPager("SZHL_KDGL", "*", 8, page, "Status asc,CRDate desc", strWhere, ref total);
            msg.Result = dt;
            msg.Result1 = total;
        } 
        #endregion

        #region 添加快递
        /// <summary>
        /// 添加快递
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void ADDKDGL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                SZHL_KDGL kdgl = JsonConvert.DeserializeObject<SZHL_KDGL>(P1);

                string strJSUser = context.Request.QueryString["jsr"] ?? "";

                //if (kdgl.JSUser == "")
                //{
                //    msg.ErrorMsg = "接收人不能为空";
                //    return;
                //}

                if (strJSUser == "")
                {
                    msg.ErrorMsg = "接收人不能为空";
                    return;
                }
                else
                {
                    if (P2 != "") // 处理微信上传的图片
                    {
                        string fids = CommonHelp.ProcessWxIMG(P2, "KDGL", UserInfo);
                        if (!string.IsNullOrEmpty(kdgl.ImgUrl))
                        {
                            kdgl.ImgUrl += "," + fids;
                        }
                        else
                        {
                            kdgl.ImgUrl = fids;
                        }
                    }

                    kdgl.CRDate = DateTime.Now;
                    kdgl.CRUser = UserInfo.User.UserName;
                    kdgl.IsDel = 0;
                    kdgl.Status = "0";
                    kdgl.ComId = UserInfo.User.ComId;

                    foreach (var r in strJSUser.Split(','))
                    {
                        SZHL_KDGL kd = kdgl;
                        kd.JSUser = r;

                        new SZHL_KDGLB().Insert(kd);

                        SZHL_TXSX TX = new SZHL_TXSX();
                        TX.Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                        TX.APIName = "KDGL";
                        TX.ComId = UserInfo.User.ComId;
                        TX.FunName = "KDGLMSG";
                        TX.TXMode = "KDGL";
                        TX.CRUserRealName = UserInfo.User.UserRealName;
                        TX.MsgID = kd.ID.ToString();
                        TX.Remark = kdgl.ImgUrl.Split(',')[0];
                        TX.TXContent = UserInfo.User.UserRealName + "通知您有一个新快递，请及时领取";
                        TX.TXUser = kd.JSUser;
                        TX.CRUser = UserInfo.User.UserName;
                        TXSX.TXSXAPI.AddALERT(TX); //时间为发送时间
                    }
                }

            }
            catch (Exception ex)
            {
                msg.ErrorMsg = ex.ToString();
            }
        } 
        #endregion

        #region 确认收到快递
        /// <summary>
        /// 确认收到快递
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void QRSDKD(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);
            SZHL_KDGL kdgl = new SZHL_KDGLB().GetEntity(d => d.ID == Id);
            if (kdgl.Status == "0")
            {
                kdgl.Status = "1";
                kdgl.QRDate = DateTime.Now;
                kdgl.QRUser = UserInfo.User.UserName;

                new SZHL_KDGLB().Update(kdgl);
            }
            else
            {
                msg.Result1 = "1";
            }


            //SZHL_TXSX TX = new SZHL_TXSX();
            //TX.Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            //TX.APIName = "KDGL";
            //TX.ComId = UserInfo.User.ComId;
            //TX.FunName = "KDGLQRMSG";
            //TX.TXMode = "KDGL";
            //TX.CRUserRealName = UserInfo.User.UserRealName;
            //TX.MsgID = P1;
            //TX.Remark = kdgl.ImgUrl.Split(',')[0];
            //TX.TXContent = UserInfo.User.UserRealName + "已经确认收件";
            //TX.TXUser = kdgl.CRUser;
            //TX.CRUser = UserInfo.User.UserName;
            //TXSX.TXSXAPI.AddALERT(TX); //时间为发送时间


            msg.Result = kdgl;

            new JH_Auth_User_CenterB().ReadMsg(UserInfo, kdgl.ID, "KDGL");
        } 
        #endregion

        #region 发送消息的接口
        public void KDGLMSG(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_TXSX TX = JsonConvert.DeserializeObject<SZHL_TXSX>(P1);
            Article ar0 = new Article();
            ar0.Title = "通知：您有一个新快递到达";
            ar0.Description = TX.TXContent;
            ar0.Url = TX.MsgID;
            ar0.PicUrl = TX.Remark;
            List<Article> al = new List<Article>();
            al.Add(ar0);

            int id = Int32.Parse(TX.MsgID);
            if (!string.IsNullOrEmpty(TX.TXUser))
            {
                try
                {
                    //发送PC消息
                    UserInfo = new JH_Auth_UserB().GetUserInfo(TX.ComId.Value,TX.CRUser);
                    new JH_Auth_User_CenterB().SendMsg(UserInfo, TX.TXMode, TX.TXContent, TX.MsgID, TX.TXUser);
                }
                catch (Exception)
                {
                }

                //发送微信消息
                WXHelp wx = new WXHelp(UserInfo.QYinfo);
                wx.SendTH(al, TX.TXMode , "A", TX.TXUser);
            }


        }
        public void KDGLQRMSG(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_TXSX TX = JsonConvert.DeserializeObject<SZHL_TXSX>(P1);
            Article ar0 = new Article();
            ar0.Title = "通知：确认收件";
            ar0.Description = TX.TXContent;
            ar0.Url = TX.MsgID;

            List<Article> al = new List<Article>();
            al.Add(ar0);

            int id = Int32.Parse(TX.MsgID);
            if (!string.IsNullOrEmpty(TX.TXUser))
            {
                try
                {
                    //发送PC消息
                    UserInfo = new JH_Auth_UserB().GetUserInfo(TX.ComId.Value,TX.CRUser);
                    new JH_Auth_User_CenterB().SendMsg(UserInfo, TX.TXMode, TX.TXContent, TX.MsgID, TX.TXUser);
                }
                catch (Exception)
                {
                }

                //发送微信消息
                WXHelp wx = new WXHelp(UserInfo.QYinfo);
                wx.SendTH(al, TX.TXMode , "A", TX.TXUser);
            }


        }
        #endregion
    }
}