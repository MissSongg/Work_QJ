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
    public class KDDYManage : IWsService
    {
        public void ProcessRequest(HttpContext context, ref Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            MethodInfo methodInfo = typeof(KDDYManage).GetMethod(msg.Action.ToUpper());
            KDDYManage model = new KDDYManage();
            methodInfo.FastInvoke(model, new object[] { context, msg, P1, P2, UserInfo });
        }

        #region 打印记录

        #region 获取打印记录列表
        /// <summary>
        /// 获取打印记录列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETDYLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {

            int page = 0;
            int pagecount = 8;
            int.TryParse(context.Request.QueryString["p"] ?? "1", out page);//页码
            int.TryParse(context.Request.QueryString["pagecount"] ?? "8", out pagecount);//页数
            page = page == 0 ? 1 : page;
            int recordCount = 0;
            string strWhere = string.Format(" kl.ComId={0} and kl.CRUser='{1}'", UserInfo.User.ComId, UserInfo.User.UserName);

            string strContent = context.Request["Content"] ?? "";
            strContent = strContent.TrimEnd();
            if (strContent != "")
            {
                strWhere += string.Format(" And kl.Info like '%{0}%'", strContent);
            }

            string leibie = context.Request["lb"] ?? "";
            if (leibie != "")
            {
                strWhere += string.Format(" And kl.GongSi='{0}' ", leibie);
            }

            int DataID = -1;
            int.TryParse(context.Request.QueryString["ID"] ?? "-1", out DataID);//记录Id
            if (DataID != -1)
            {
                string strIsHasDataQX = new JH_Auth_QY_ModelB().ISHASDATAREADQX("KDDY", DataID, UserInfo);
                if (strIsHasDataQX == "Y")
                {
                    strWhere += string.Format(" And kl.ID = '{0}'", DataID);
                }

            }
            DataTable dt = new SZHL_KDDY_LISTB().GetDataPager(@" SZHL_KDDY_LIST kl
LEFT JOIN SZHL_KDDY_PZ kp ON kl.GongSi=kp.ID", " kl.*,kp.KDName ", pagecount, page, " kl.CRDate DESC ", strWhere, ref recordCount);

            msg.Result = dt;
            //msg.Result1 = Math.Ceiling(recordCount * 1.0 / 8);
            msg.Result1 = recordCount;
        }
        #endregion

        #region 添加打印记录
        /// <summary>
        /// 添加打印记录
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="strUserName"></param>
        public void ADDDYJL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_KDDY_LIST kdList = new SZHL_KDDY_LIST();
            kdList.GongSi = int.Parse(P2);
            kdList.Info = P1;
            if (kdList.ID == 0)
            {
                kdList.CRDate = DateTime.Now;
                kdList.CRUser = UserInfo.User.UserName;
                kdList.ComId = UserInfo.User.ComId;
                new SZHL_KDDY_LISTB().Insert(kdList);
            }
            else
            {
                new SZHL_KDDY_LISTB().Update(kdList);
            }

            string strPZ = context.Request["PZ"] ?? "";
            string strCZ = context.Request["Vertical"] ?? "";
            string strSP = context.Request["Horizontal"] ?? "";

            var pz = new SZHL_KDDY_PZB().GetEntity(p => p.ID == kdList.GongSi);
            if (pz != null)
            {
                if (strCZ != "")
                {
                    try
                    {
                        double dbcz = double.Parse(strCZ);
                        pz.Vertical = dbcz.ToString();
                    }
                    catch
                    { }
                }
                if (strSP != "")
                {
                    try
                    {
                        double dbsp = double.Parse(strSP);
                        pz.Horizontal = dbsp.ToString();
                    }
                    catch
                    { }
                }
                if (strPZ != "")
                {
                    pz.objects = strPZ;
                }
                new SZHL_KDDY_PZB().Update(pz);

            }

            msg.Result = kdList;
        }
        #endregion

        #region 删除打印记录
        /// <summary>
        /// 删除打印记录
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void DELDYJL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                int id = 0;
                int.TryParse(P1, out id);
                if (new SZHL_KDDY_LISTB().Delete(d => d.ID == id))
                {
                    msg.ErrorMsg = "";
                }
            }
            catch (Exception)
            {
                msg.ErrorMsg = "删除失败";
            }
        }
        #endregion

        #region 查询打印记录（单个记录）
        /// <summary>
        /// 查询打印记录（单个记录）
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETDYJLMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = 0;
            int.TryParse(P1, out Id);
            SZHL_KDDY_LIST sg = new SZHL_KDDY_LISTB().GetEntity(d => d.ID == Id && d.ComId == UserInfo.User.ComId);
            msg.Result = sg;
        }

        #endregion

        #endregion


        #region 打印配置

        #region 获取配置列表
        /// <summary>
        /// 获取配置列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETPZALL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string sql = string.Format("SELECT * FROM SZHL_KDDY_PZ WHERE ComId={0}", UserInfo.User.ComId);
            DataTable dt = new SZHL_KDDY_PZB().GetDTByCommand(sql);
            msg.Result = dt;
        }
        #endregion

        #region 获取打印记录列表
        /// <summary>
        /// 获取打印记录列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETPZLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {

            int page = 0;
            int.TryParse(context.Request.QueryString["p"] ?? "1", out page);//页码
            page = page == 0 ? 1 : page;
            int recordCount = 0;
            string strWhere = string.Format(" ComId={0} ", UserInfo.User.ComId);

            string strContent = context.Request["Content"] ?? "";
            if (strContent != "")
            {
                strWhere += string.Format(" and KDName like '%{0}%'", strContent);
            }

            //string leibie = context.Request["lb"] ?? "";
            //if (leibie != "")
            //{
            //    strWhere += string.Format("  ", leibie);
            //}

            int DataID = -1;
            int.TryParse(context.Request.QueryString["ID"] ?? "-1", out DataID);//记录Id
            if (DataID != -1)
            {
                string strIsHasDataQX = new JH_Auth_QY_ModelB().ISHASDATAREADQX("KDDY", DataID, UserInfo);
                if (strIsHasDataQX == "Y")
                {
                    strWhere += string.Format(" And ID = '{0}'", DataID);
                }

            }
            DataTable dt = new SZHL_KDDY_PZB().GetDataPager(@" SZHL_KDDY_PZ ", " * ", 8, page, "  CRDate DESC ", strWhere, ref recordCount);

            msg.Result = dt;
            msg.Result1 = Math.Ceiling(recordCount * 1.0 / 8);

        }
        #endregion

        #region 添加打印配置
        /// <summary>
        /// 添加配置
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="strUserName"></param>
        public void ADDDYPZ(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_KDDY_PZ kdpz = JsonConvert.DeserializeObject<SZHL_KDDY_PZ>(P1);
            if (string.IsNullOrEmpty(kdpz.KDName))
            {
                msg.ErrorMsg = "快递公司不能为空";
                return;
            }
            else
            {
                if (kdpz.ID == 0)
                {
                    SZHL_KDDY_PZ sch = new SZHL_KDDY_PZB().GetEntity(p => p.KDName == kdpz.KDName && p.ComId == UserInfo.User.ComId);
                    if (sch != null)
                    {
                        msg.ErrorMsg = "快递公司已经存在";
                        return;
                    }
                }
                else
                {
                    SZHL_KDDY_PZ sch = new SZHL_KDDY_PZB().GetEntity(p => p.KDName == kdpz.KDName && p.ComId == UserInfo.User.ComId && p.ID != kdpz.ID);
                    if (sch != null)
                    {
                        msg.ErrorMsg = "快递公司已经存在";
                        return;
                    }
                }
            }
            if (kdpz.ID == 0)
            {
                kdpz.Vertical = "0";
                kdpz.Horizontal = "0";
                kdpz.CRDate = DateTime.Now;
                kdpz.CRUser = UserInfo.User.UserName;
                kdpz.ComId = UserInfo.User.ComId;
                new SZHL_KDDY_PZB().Insert(kdpz);
            }
            else
            {
                new SZHL_KDDY_PZB().Update(kdpz);
            }

            msg.Result = kdpz;
        }
        #endregion

        #region 删除配置
        /// <summary>
        /// 删除打印配置
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void DELPZBYID(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                int id = 0;
                int.TryParse(P1, out id);
                if (new SZHL_KDDY_PZB().Delete(d => d.ID == id))
                {
                    msg.ErrorMsg = "";
                }
            }
            catch (Exception)
            {
                msg.ErrorMsg = "删除失败";
            }
        }
        #endregion

        #region 查询快递配置
        /// <summary>
        /// 查询快递配置
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETDYPZMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = 0;
            int.TryParse(P1, out Id);
            SZHL_KDDY_PZ sg = new SZHL_KDDY_PZB().GetEntity(d => d.ID == Id && d.ComId == UserInfo.User.ComId);
            msg.Result = sg;

            if (!string.IsNullOrEmpty(sg.KDImg))
            {
                try
                {
                    int[] fileIds = sg.KDImg.SplitTOInt(',');
                    msg.Result1 = new FT_FileB().GetEntities(d => fileIds.Contains(d.ID));
                }
                catch { }
            }
        }

        #endregion
        #endregion


        #region 获取参数信息
        /// <summary>
        /// 获取参数信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETCSINFO(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            JH_Auth_ZiDian jaz = new JH_Auth_ZiDianB().GetEntities(p => p.TypeNO == "KDDY").FirstOrDefault();
            if (jaz != null)
            {
                msg.Result = jaz.Remark;
            }
            else
            {
                msg.ErrorMsg = "参数未配置";
            }
        }
        #endregion

        #region 常用地址

        #region 获取常用地址列表
        /// <summary>
        /// 获取常用地址列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETCYDZLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {

            int page = 0;
            int.TryParse(context.Request.QueryString["p"] ?? "1", out page);//页码
            page = page == 0 ? 1 : page;
            int recordCount = 0;
            string strWhere = string.Format(" ComId={0} and CRUser='{1}'", UserInfo.User.ComId, UserInfo.User.UserName);

            string strContent = context.Request["Content"] ?? "";
            if (strContent != "")
            {
                strWhere += string.Format(" And (sendUser like '%{0}%' or sendTel like '%{0}%' or sendHomePhone like '%{0}%' or sendCompany like '%{0}%' or sendAddress like '%{0}%')", strContent);
            }


            DataTable dt = new SZHL_KDDY_LISTB().GetDataPager(@" SZHL_KDDY_CYDZ ", " * ", 8, page, " CRDate DESC ", strWhere, ref recordCount);

            msg.Result = dt;
            msg.Result1 = Math.Ceiling(recordCount * 1.0 / 8);

        }
        #endregion

        #region 添加常用地址
        /// <summary>
        /// 添加常用地址
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="strUserName"></param>
        public void ADDCYDZ(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_KDDY_CYDZ kdList = JsonConvert.DeserializeObject<SZHL_KDDY_CYDZ>(P1);
            string strWhere = " CRUser ='" + UserInfo.User.UserName + "' and ComId='" + UserInfo.User.ComId + "'";
            if (!string.IsNullOrEmpty(kdList.sendUser))
            {
                strWhere += " and sendUser='" + kdList.sendUser + "'";
            }
            if (!string.IsNullOrEmpty(kdList.sendTel))
            {
                strWhere += " and sendTel='" + kdList.sendTel + "'";
            }
            if (!string.IsNullOrEmpty(kdList.sendHomePhone))
            {
                strWhere += " and sendHomePhone='" + kdList.sendHomePhone + "'";
            }
            if (!string.IsNullOrEmpty(kdList.sendCompany))
            {
                strWhere += " and sendCompany='" + kdList.sendCompany + "'";
            }
            if (!string.IsNullOrEmpty(kdList.sendAddress))
            {
                strWhere += " and sendAddress='" + kdList.sendAddress + "'";
            }
            if (!string.IsNullOrEmpty(kdList.sendProvince))
            {
                strWhere += " and sendProvince='" + kdList.sendProvince + "'";
            }
            if (!string.IsNullOrEmpty(kdList.sendCity))
            {
                strWhere += " and sendCity='" + kdList.sendCity + "'";
            }
            if (!string.IsNullOrEmpty(kdList.sendCounty))
            {
                strWhere += " and sendCounty='" + kdList.sendCounty + "'";
            }

            var dz = new SZHL_KDDY_CYDZB().GetEntities(strWhere).FirstOrDefault();
            if (dz == null)
            {
                if (kdList.ID == 0)
                {
                    kdList.CRDate = DateTime.Now;
                    kdList.CRUser = UserInfo.User.UserName;
                    kdList.ComId = UserInfo.User.ComId;
                    new SZHL_KDDY_CYDZB().Insert(kdList);
                }
            }
            

            msg.Result = kdList;
        }
        #endregion

        #region 删除常用地址
        /// <summary>
        /// 删除常用地址
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void DELCYDZ(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                int id = 0;
                int.TryParse(P1, out id);
                if (new SZHL_KDDY_CYDZB().Delete(d => d.ID == id))
                {
                    msg.ErrorMsg = "";
                }
            }
            catch (Exception)
            {
                msg.ErrorMsg = "删除失败";
            }
        }
        #endregion

        #region 查询常用地址
        /// <summary>
        /// 查询常用地址
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETCYDZMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = 0;
            int.TryParse(P1, out Id);
            SZHL_KDDY_CYDZ sg = new SZHL_KDDY_CYDZB().GetEntity(d => d.ID == Id && d.ComId == UserInfo.User.ComId);
            msg.Result = sg;
        }

        #endregion

        #endregion

    }
}