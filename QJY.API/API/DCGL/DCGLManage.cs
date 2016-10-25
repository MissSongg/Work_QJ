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
    public class DCGLManage : IWsService
    {
        public void ProcessRequest(HttpContext context, ref Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            MethodInfo methodInfo = typeof(DCGLManage).GetMethod(msg.Action.ToUpper());
            DCGLManage model = new DCGLManage();
            methodInfo.FastInvoke(model, new object[] { context, msg, P1, P2, UserInfo });
        }

        #region 添加菜品
        /// <summary>
        /// 添加菜品
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void ADDDCGL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_DCGL dcgl = JsonConvert.DeserializeObject<SZHL_DCGL>(P1);

            if (dcgl.Name == "")
            {
                msg.ErrorMsg = "菜品名称不能为空";
                return;
            }
            if (dcgl.Price <= 0)
            {
                msg.ErrorMsg = "价格不能为空或为0";
                return;
            }
            if (P2 != "") // 处理微信上传的图片
            {
                string fids = CommonHelp.ProcessWxIMG(P2, "CCXJ", UserInfo);
                if (!string.IsNullOrEmpty(dcgl.ImgUrl))
                {
                    dcgl.ImgUrl += "," + fids;
                }
                else
                {
                    dcgl.ImgUrl = fids;
                }
            }
            if (dcgl.ID == 0)
            {
                dcgl.CRDate = DateTime.Now;
                dcgl.CRUser = UserInfo.User.UserName;
                dcgl.IsDel = 0;
                dcgl.ComId = UserInfo.User.ComId;
                new SZHL_DCGLB().Insert(dcgl);
            }
            else
            {
                dcgl.CRDate = DateTime.Now;
                dcgl.CRUser = UserInfo.User.UserName;
                new SZHL_DCGLB().Update(dcgl);
            }
            msg.Result = dcgl;
        } 
        #endregion

        #region 菜品列表
        /// <summary>
        /// 菜品列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETDCGLLIST_PAGE(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            DataTable dt = new JH_Auth_ZiDianB().GetDTByCommand("select * from JH_Auth_ZiDian where class=3 and (comid=" + UserInfo.User.ComId + " or comid=0)");
            dt.Columns.Add("Item", Type.GetType("System.Object"));
            dt.Columns.Add("Qty", Type.GetType("System.String"));
            dt.Columns.Add("xsQty", Type.GetType("System.String"));
            foreach (DataRow dr in dt.Rows)
            {
                int rid = Int32.Parse(dr["ID"].ToString());
                var list = new SZHL_DCGLB().GetEntities(p => p.TypeID == rid && p.IsDel == 0).OrderByDescending(p => p.CRDate).Select(p => new
                {
                    p.ID,
                    p.ComId,
                    p.CRDate,
                    p.CRUser,
                    p.DelDate,
                    p.DelUser,
                    ImgUrl = string.IsNullOrEmpty(p.ImgUrl) ? "" : p.ImgUrl.Split(',')[0],
                    p.IsDel,
                    p.Name,
                    p.Price,
                    p.Remark,
                    p.Status,
                    p.TypeID,
                    Qty = 0
                });
                dr["Item"] = list;
                dr["Qty"] = 0;
                dr["xsQty"] = list.Where(p=>p.Status=="1").Count();
            }

            msg.Result = dt;
        } 
        #endregion

        #region 获取菜品信息
        /// <summary>
        /// 获取菜品信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETDCGLMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);
            SZHL_DCGL DCGL = new SZHL_DCGLB().GetEntity(d => d.ID == Id);
            msg.Result = DCGL;

            if (DCGL != null)
            {
                var zd = new JH_Auth_ZiDianB().GetEntity(p => p.ID == DCGL.TypeID);
                msg.Result1 = zd.TypeName;
                if (!string.IsNullOrEmpty(DCGL.ImgUrl))
                {
                    msg.Result2 = new FT_FileB().GetEntities(" ID in (" + DCGL.ImgUrl + ")");
                }
            }
        } 
        #endregion

        #region 添加订单
        /// <summary>
        /// 添加订单
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void ADDDCGLDD(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                DCGL_Order dcgl = JsonConvert.DeserializeObject<DCGL_Order>(P1);

                //if (dcgl.YJYCDate == null)
                //{
                //    msg.ErrorMsg = "请选择预计用餐时间";
                //    return;
                //}
                if (dcgl.IDS == "")
                {
                    msg.ErrorMsg = "请选择菜品";
                    return;
                }

                string strid = string.Empty;

                foreach (var v in dcgl.IDS.Split(','))
                {
                    int id = Int32.Parse(v.Split('_')[0]);

                    var sd = new SZHL_DCGLB().GetEntity(p => p.ID == id);

                    if (sd != null)
                    {
                        if (sd.Status == "0")
                        {
                            if (string.IsNullOrEmpty(strid))
                            {
                                strid = id.ToString();
                            }
                            else
                            {
                                strid = strid + "," + id.ToString();
                            }
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(strid))
                        {
                            strid = id.ToString();
                        }
                        else
                        {
                            strid = strid + "," + id.ToString();
                        }
                    }
                }
                if (string.IsNullOrEmpty(strid))
                {
                    SZHL_DCGL_HEADER sdh = new SZHL_DCGL_HEADER();
                    sdh.YJYCDate = dcgl.YJYCDate;
                    sdh.ComId = UserInfo.User.ComId;
                    sdh.IsDel = 0;
                    sdh.Status = "0";
                    sdh.CRDate = DateTime.Now;
                    sdh.CRUser = UserInfo.User.UserName;

                    new SZHL_DCGL_HEADERB().Insert(sdh);

                    decimal? tpe = 0;

                    foreach (var v in dcgl.IDS.Split(','))
                    {
                        int id = Int32.Parse(v.Split('_')[0]);
                        int qty = Int32.Parse(v.Split('_')[1]);

                        var sd = new SZHL_DCGLB().GetEntity(p => p.ID == id);

                        SZHL_DCGL_ITEM sdi = new SZHL_DCGL_ITEM();
                        sdi.CID = sd.ID;
                        sdi.ComId = UserInfo.User.ComId;
                        sdi.HID = sdh.ID;

                        sdi.ImgUrl = sd.ImgUrl;
                        sdi.Name = sd.Name;
                        sdi.Price = sd.Price;
                        sdi.Qty = qty;

                        sdi.IsDel = 0;
                        sdi.Status = "0";
                        sdi.CRDate = DateTime.Now;
                        sdi.CRUser = UserInfo.User.UserName;

                        new SZHL_DCGL_ITEMB().Insert(sdi);

                        tpe = tpe + sdi.Price * sdi.Qty;
                    }

                    sdh.TTPrice = tpe;

                    new SZHL_DCGL_HEADERB().Update(sdh);

                    JH_Auth_ZiDian jaz = new JH_Auth_ZiDianB().GetEntity(p => p.ComId == UserInfo.User.ComId && p.TypeNO == "DCGLA");

                    if (jaz != null)
                    {
                        if (!string.IsNullOrEmpty(jaz.Remark))
                        {
                            SZHL_TXSX TX = new SZHL_TXSX();
                            TX.Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                            TX.APIName = "DCGL";
                            TX.ComId = UserInfo.User.ComId;
                            TX.FunName = "DCGLMSG";
                            TX.TXMode = "DCGL";
                            TX.CRUserRealName = UserInfo.User.UserRealName;
                            TX.MsgID = sdh.ID.ToString();
                            TX.TXContent = UserInfo.User.UserRealName + "下了一个新订单，请及时查看";
                            TX.TXUser = jaz.Remark;
                            TX.CRUser = UserInfo.User.UserName;
                            TXSX.TXSXAPI.AddALERT(TX); //时间为发送时间
                        }
                    }
                }
                else
                {
                    msg.ErrorMsg = "下单失败!";
                    msg.Result = strid;
                }

            }
            catch (Exception ex)
            {
                msg.ErrorMsg = ex.ToString();
            }
        } 
        #endregion

        #region 订单列表
        /// <summary>
        /// 订单列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETDCGLHEADERLIST_PAGE(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strWhere = " cc.IsDel=0 and cc.ComId=" + UserInfo.User.ComId;
            //strWhere += string.Format(" And cc.CRUser='{0}' ", UserInfo.User.UserName);
            if (P1 == "")
            {
                strWhere += string.Format(" And cc.CRUser = '{0}'", UserInfo.User.UserName);
            }

            int page = 0;
            int.TryParse(context.Request.QueryString["p"] ?? "1", out page);
            page = page == 0 ? 1 : page;
            int total = 0;

            DataTable dt = new SZHL_DCGL_HEADERB().GetDataPager(" SZHL_DCGL_HEADER cc ", "*", 8, page, " cc.CRDate desc", strWhere, ref total);


            dt.Columns.Add("Item", Type.GetType("System.Object"));
            //dt.Columns.Add("ZYSJ", Type.GetType("System.String"));

            foreach (DataRow dr in dt.Rows)
            {
                int hid = Int32.Parse(dr["ID"].ToString());

                var list = new SZHL_DCGL_ITEMB().GetEntities(p => p.HID == hid && p.IsDel == 0).OrderByDescending(p => p.CRDate);
                dr["Item"] = list;

            }
            msg.Result = dt;
        }

        /// <summary>
        /// 订餐列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETDCGLLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
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
                string strIsHasDataQX = new JH_Auth_QY_ModelB().ISHASDATAREADQX("DCGL", DataID, UserInfo);
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
                            new JH_Auth_User_CenterB().ReadMsg(UserInfo, DataID, "DCGL");
                        }
                        break;
                    case "1": //所有的
                        {
                        }
                        break;
                    case "2": //自己的
                        {
                            strWhere += string.Format(" And CRUser='{0}'", userName);
                        }
                        break;
                }
                dt = new SZHL_CCXJB().GetDataPager("SZHL_DCGL_HEADER ", "*", 8, page, " Status asc,CRDate desc", strWhere, ref total);

                dt.Columns.Add("Item", Type.GetType("System.Object"));
                //dt.Columns.Add("ZYSJ", Type.GetType("System.String"));

                foreach (DataRow dr in dt.Rows)
                {
                    int hid = Int32.Parse(dr["ID"].ToString());

                    var list = new SZHL_DCGL_ITEMB().GetEntities(p => p.HID == hid && p.IsDel == 0).OrderByDescending(p => p.CRDate);
                    dr["Item"] = list;
                }

                msg.Result = dt;
                msg.Result1 = total;
            }
        }
        #endregion

        #region 确认收到订单
        /// <summary>
        /// 确认收到订单
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void QRSDDD(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);
            SZHL_DCGL_HEADER dcgl = new SZHL_DCGL_HEADERB().GetEntity(d => d.ID == Id);
            dcgl.Status = "1";
            dcgl.QRDate = DateTime.Now;
            dcgl.QRUser = UserInfo.User.UserName;
            dcgl.Remark = P2;

            new SZHL_DCGL_HEADERB().Update(dcgl);

            SZHL_TXSX TX = new SZHL_TXSX();
            TX.Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            TX.APIName = "DCGL";
            TX.ComId = UserInfo.User.ComId;
            TX.FunName = "DCGLQRMSG";
            TX.TXMode = "DCGL";
            TX.CRUserRealName = UserInfo.User.UserRealName;
            TX.MsgID = dcgl.ID.ToString();
            TX.TXContent = UserInfo.User.UserRealName + "已经确认了您的订单，请及时查看";
            TX.TXUser = dcgl.CRUser;
            TX.CRUser = UserInfo.User.UserName;
            TXSX.TXSXAPI.AddALERT(TX); //时间为发送时间


            msg.Result = dcgl;

            new JH_Auth_User_CenterB().ReadMsg(UserInfo, dcgl.ID, "DCGL");
        } 
        #endregion

        #region 下架菜品
        /// <summary>
        /// 下架菜品
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void XJCP(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            if (!string.IsNullOrEmpty(P1)) {
                new SZHL_DCGLB().ExsSql("update SZHL_DCGL set Status='0' where ID in (" + P1 + ")");
            }
        }
        #endregion

        #region 任务发送消息的接口
        public void DCGLMSG(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_TXSX TX = JsonConvert.DeserializeObject<SZHL_TXSX>(P1);
            Article ar0 = new Article();
            ar0.Title = "通知：您有一个新订单";
            ar0.Description = TX.TXContent;
            ar0.Url = TX.MsgID;
            List<Article> al = new List<Article>();
            al.Add(ar0);
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
                wx.SendTH(al, TX.TXMode, "A", TX.TXUser);
            }


        }

        public void DCGLQRMSG(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_TXSX TX = JsonConvert.DeserializeObject<SZHL_TXSX>(P1);
            Article ar0 = new Article();
            ar0.Title = "通知：您的订单已经被确认";
            ar0.Description = TX.TXContent;
            ar0.Url = TX.MsgID;
            List<Article> al = new List<Article>();
            al.Add(ar0);
            if (!string.IsNullOrEmpty(TX.TXUser))
            {
                try
                {
                    //发送PC消息
                    UserInfo = new JH_Auth_UserB().GetUserInfo(TX.ComId.Value, TX.CRUser);
                    new JH_Auth_User_CenterB().SendMsg(UserInfo, TX.TXMode, TX.TXContent, TX.MsgID, TX.TXUser);
                }
                catch (Exception)
                {
                }

                //发送微信消息
                WXHelp wx = new WXHelp(UserInfo.QYinfo);
                wx.SendTH(al, TX.TXMode, "A", TX.TXUser);
            }


        }
        #endregion

        public class DCGL_Order
        {
            public DateTime? YJYCDate { get; set; }

            public string IDS { get; set; }

        }
    }
}