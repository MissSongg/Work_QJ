using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Web;
using FastReflectionLib;
using QJY.Data;
using System.Data;

namespace QJY.API
{
    /// <summary>
    /// 微信营销活动接口
    /// </summary>
    public class YXHDManage : IWsService2
    {
        public void ProcessRequest(HttpContext context, ref Msg_Result msg, int ComId, string P1, string P2, SZHL_YX_USER UserInfo)
        {
            MethodInfo methodInfo = typeof(YXHDManage).GetMethod(msg.Action.ToUpper());
            YXHDManage model = new YXHDManage();
            methodInfo.FastInvoke(model, new object[] { context, msg, ComId, P1, P2, UserInfo });
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="ComId"></param>
        /// <param name="P1">手机号</param>
        /// <param name="P2">密码</param>
        /// <param name="UserInfo"></param>
        public void LOGIN(HttpContext context, Msg_Result msg, int ComId, string P1, string P2, SZHL_YX_USER UserInfo)
        {
            var usr = new SZHL_YX_USERB().GetEntities(p => p.ComId == ComId && p.mobphone == P1 && p.Pasd == P2).FirstOrDefault();
            if (usr == null)
            {
                msg.ErrorMsg = "登录失败,手机号或密码不正确";
            }
            else
            {
                string code = "";
                if (string.IsNullOrEmpty(usr.code))
                {
                    code = Guid.NewGuid().ToString();
                    new SZHL_YX_USERB().ExsSql(string.Format("update SZHL_YX_USER set code='{0}',codetime=getdate()+30 where ID='{1}'", code, usr.ID));
                }
                else
                {
                    code = usr.code;
                }


                msg.Result = code;
            }

        }

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void REGISTER(HttpContext context, Msg_Result msg, int ComId, string P1, string P2, SZHL_YX_USER UserInfo)
        {
            if (P1 == "" || P2 == "")
            {
                msg.ErrorMsg = "请输入手机号和密码";
                return;
            }
            var usr = new SZHL_YX_USERB().GetEntity(p => p.ComId == ComId && p.mobphone == P1);
            if (usr == null)
            {
                new SZHL_YX_USERB().Insert(new SZHL_YX_USER()
                {
                    mobphone = P1,
                    Pasd = P2,
                    ComId = ComId,
                    CRDate = DateTime.Now

                });
            }
            else
            {
                msg.ErrorMsg = "手机号已注册";
            }

        }

        /// <summary>
        /// 活动列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETHDLIST(HttpContext context, Msg_Result msg, int ComId, string P1, string P2, SZHL_YX_USER UserInfo)
        {
            DateTime now = DateTime.Now;
            msg.Result = new SZHL_YX_HDB().GetEntities(p => p.ComId == ComId && p.KSDate <= now && p.JSDate >= now);
        }

        /// <summary>
        /// 根据活动ID查看明细
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="ComId"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETHDMXLIST(HttpContext context, Msg_Result msg, int ComId, string P1, string P2, SZHL_YX_USER UserInfo)
        {
            int ID = Int32.Parse(P1);
            string strSQL = string.Format("SELECT  *  FROM SZHL_YX_HD_ITEM  WHERE HDID='{0}'", ID);
            DataTable dtReturn = new SZHL_YX_HD_CYB().GetDTByCommand(strSQL);
            dtReturn.Columns.Add("GMRS");
            dtReturn.Columns.Add("ZJLIST", Type.GetType("System.Object"));
            foreach (DataRow dr in dtReturn.Rows)
            {
                int hdmxid = int.Parse(dr["ID"].ToString());
                dr["GMRS"] = new SZHL_YX_HD_CYB().GetEntities(D => D.hdmxid == hdmxid).Count();
                dr["ZJLIST"] = new SZHL_YX_HD_CYB().GetEntities(D => D.hdmxid == hdmxid && D.iszj == "Y");
              
            }
            msg.Result = dtReturn;
            msg.Result1 = new SZHL_YX_HD_ITEMB().GetEntities(p => p.ComId == ComId && p.HDID == ID);
        }


        /// <summary>
        /// 查看活动明细Model
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="ComId"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETMXMODEL(HttpContext context, Msg_Result msg, int ComId, string P1, string P2, SZHL_YX_USER UserInfo)
        {
            int ID = Int32.Parse(P1);
            msg.Result = new SZHL_YX_HD_ITEMB().GetEntity(p => p.ComId == ComId && p.ID == ID);
        }


        /// <summary>
        /// 购买商品
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="ComId"></param>
        /// <param name="P1">活动明细ID</param>
        /// <param name="P2">购买数量</param>
        /// <param name="UserInfo"></param>
        public void BUYGOODS(HttpContext context, Msg_Result msg, int ComId, string P1, string P2, SZHL_YX_USER UserInfo)
        {
            int ID = Int32.Parse(P1);
            var item = new SZHL_YX_HD_ITEMB().GetEntity(p => p.ComId == ComId && p.ID == ID);
            if (item == null)
            {
                msg.ErrorMsg = "活动错误";
                return;
            }
            int total = Int32.Parse(P2);
            if (total < 1)
            {
                msg.ErrorMsg = "购买数量错误";
                return;
            }

            string batchnumber = DateTime.Now.ToString("yyyyMMddHHssmmfff");

            while (total >= 1)
            {
                SZHL_YX_HD_GM gm = new SZHL_YX_HD_GM();
                gm.hdid = item.HDID;
                gm.hdmxid = item.ID;
                gm.ComId = ComId;
                gm.CRDate = DateTime.Now;
                gm.userid = UserInfo.ID;
                gm.zfje = item.GMJE;
                gm.iscyhd = "N";
                gm.gmdate = DateTime.Now;
                gm.ishx = "N";
                gm.batchnumber = batchnumber;

                new SZHL_YX_HD_GMB().Insert(gm);

                total -= 1;
            }

            msg.Result = batchnumber;

        }
        /// <summary>
        /// 根据组团ID查看组团用户列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="ComId"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETHDZTLIST(HttpContext context, Msg_Result msg, int ComId, string P1, string P2, SZHL_YX_USER UserInfo)
        {
            int ID = Int32.Parse(P1);
            msg.Result = new SZHL_YX_HD_ZTB().GetEntities(p => p.ComId == ComId && p.ID == ID);
            msg.Result1 = new SZHL_YX_HD_CYB().GetEntities(p => p.ComId == ComId && p.ztid == ID);
        }


        /// <summary>
        /// 我的夺宝团(包括发起的,参与的)
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="ComId"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETHDCYLIST(HttpContext context, Msg_Result msg, int ComId, string P1, string P2, SZHL_YX_USER UserInfo)
        {

            string strSQL = string.Format("SELECT DISTINCT ZT.ID , ZT.ztname,ZT.fquserid,ZT.hdmxid,ZT.iskj,HDITEM.CTRS,HDUSER.mobphone, '' as YZTRS,'' as  zjuserphone  FROM SZHL_YX_HD_CY  CY INNER JOIN   SZHL_YX_HD_ZT  ZT ON CY.ztid=ZT.ID  LEFT JOIN SZHL_YX_HD_ITEM  HDITEM on ZT.hdmxid=HDITEM.ID  LEFT JOIN SZHL_YX_USER  HDUSER on ZT.fquserid=HDUSER.ID WHERE userid='{0}'", UserInfo.ID);
            DataTable dtReturn = new SZHL_YX_HD_CYB().GetDTByCommand(strSQL);
            for (int i = 0; i < dtReturn.Rows.Count; i++)
            {
                int ztID = Int32.Parse(dtReturn.Rows[i]["ID"].ToString());
                List<SZHL_YX_HD_CY> ListTemp = new SZHL_YX_HD_CYB().GetEntities(p => p.ComId == ComId && p.ztid == ztID).ToList();
                dtReturn.Rows[i]["YZTRS"] = ListTemp.Count;
                if (ListTemp.Where(d => d.iszj == "Y").Count() > 0)
                {
                    dtReturn.Rows[i]["zjuserphone"] = ListTemp.FirstOrDefault(d => d.iszj == "Y").cyuserphone;
                }

            }
            msg.Result = dtReturn;
        }



        /// <summary>
        /// 获取当前用户的所有商品码
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="ComId"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETHDGMLIST(HttpContext context, Msg_Result msg, int ComId, string P1, string P2, SZHL_YX_USER UserInfo)
        {
            string strSQL = string.Format(" SELECT GM.goodscode,CY.goodscode as iscyhd, GM.ishx,CY.iszj FROM SZHL_YX_HD_GM GM LEFT  JOIN   SZHL_YX_HD_CY  CY ON GM.goodscode=CY.goodscode  WHERE GM.userid='{0}'", UserInfo.ID);
            DataTable dtReturn = new SZHL_YX_HD_CYB().GetDTByCommand(strSQL);

            msg.Result = dtReturn;
        }






        /// <summary>
        /// 发起团
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">活动ID</param>
        /// <param name="P2">活动明细ID</param>
        /// <param name="UserInfo"></param>
        public void ADDHDGROUP(HttpContext context, Msg_Result msg, int ComId, string P1, string P2, SZHL_YX_USER UserInfo)
        {

            int hdid = 0;
            int.TryParse(P1, out hdid);

            int hdmxid = 0;
            int.TryParse(P2, out hdmxid);

            string strContent = context.Request["Content"] ?? "";
            strContent = strContent.TrimEnd();

            string goodscode = context.Request["goodscode"] ?? "";



            SZHL_YX_HD_ZT ZT = new SZHL_YX_HD_ZT();
            ZT.ComId = ComId;
            ZT.CRDate = DateTime.Now;
            ZT.fqdate = DateTime.Now;
            ZT.fquserid = UserInfo.ID;
            ZT.hdid = hdid;
            ZT.hdmxid = hdmxid;
            ZT.ztname = strContent;
            new SZHL_YX_HD_ZTB().Insert(ZT);

            //发起的时候默认参加
            SZHL_YX_HD_CY MODEL = new SZHL_YX_HD_CY();
            MODEL.ComId = ComId;
            MODEL.CRDate = DateTime.Now;
            MODEL.hdid = hdid;
            MODEL.hdmxid = hdmxid;
            MODEL.goodscode = goodscode;
            MODEL.iszj = "N";
            MODEL.userid = UserInfo.ID;
            MODEL.cyuserphone = UserInfo.mobphone;
            MODEL.ztid = ZT.ID;
            new SZHL_YX_HD_CYB().Insert(MODEL);

            msg.Result = ZT;



        }


        /// <summary>
        /// 参加团
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">活动ID</param>
        /// <param name="P2">活动明细ID</param>
        /// <param name="UserInfo"></param>
        public void JOINHDGROUP(HttpContext context, Msg_Result msg, int ComId, string P1, string P2, SZHL_YX_USER UserInfo)
        {

            int ztid = 0;
            int.TryParse(P1, out ztid);

            int hdmxid = 0;
            int.TryParse(P2, out hdmxid);

            string goodscode = context.Request["goodscode"] ?? "";
            List<string> ListGoodscode = new List<string>();
            ListGoodscode = goodscode.SplitTOList(',');

            List<SZHL_YX_HD_CY> ListCY = new List<SZHL_YX_HD_CY>();


            //活动明细信息
            SZHL_YX_HD_ITEM HDITEM = new SZHL_YX_HD_ITEMB().GetEntity(d => d.ID == hdmxid);
            if (HDITEM == null)
            {
                msg.ErrorMsg = "无法找到活动信息";
                return;
            }

            //组团信息
            SZHL_YX_HD_ZT ZT = new SZHL_YX_HD_ZTB().GetEntity(d => d.ID == ztid);
            if (ZT == null)
            {
                msg.ErrorMsg = "无法找到组团信息";
                return;
            }

            foreach (string tempgoodscode in ListGoodscode)
            {
                //添加参与信息
                SZHL_YX_HD_CY MODEL = new SZHL_YX_HD_CY();
                MODEL.ComId = ComId;
                MODEL.CRDate = DateTime.Now;
                MODEL.hdid = HDITEM.HDID;
                MODEL.hdmxid = hdmxid;
                MODEL.goodscode = tempgoodscode;
                MODEL.iszj = "N";
                MODEL.userid = UserInfo.ID;
                MODEL.ztid = ztid;
                new SZHL_YX_HD_CYB().Insert(MODEL);
                ListCY.Add(MODEL);
                //开奖
                new SZHL_YX_HD_CYB().DBKJ(ZT, MODEL, HDITEM);
            }
            msg.Result = ListCY;
        }
    }
}
