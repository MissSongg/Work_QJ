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
    public class KCGLManage : IWsService
    {
        public void ProcessRequest(HttpContext context, ref Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            MethodInfo methodInfo = typeof(KCGLManage).GetMethod(msg.Action.ToUpper());
            KCGLManage model = new KCGLManage();
            methodInfo.FastInvoke(model, new object[] { context, msg, P1, P2, UserInfo });
        }
        public void GETKCGLLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int page = 0;
            int.TryParse(context.Request.QueryString["p"] ?? "1", out page);//页码
            page = page == 0 ? 1 : page;
            int recordCount = 0;
            string strWhere = string.Format(" kcgl.ComId={0} ", UserInfo.User.ComId);
            if (P1 != "")
            {
                strWhere += string.Format(" and kcgl.KCTypeID={0}", P1);
            }
            string content = context.Request["Content"] ?? "";
            if (content != "")
            {
                strWhere += string.Format(" and kcgl.KCName like '%{0}%'", content);
            }

            int pagecount = 0;
            int.TryParse(context.Request.QueryString["pagecount"] ?? "1", out pagecount);//页码
            pagecount = pagecount == 0 ? 10 : pagecount;
            DataTable dt = new SZHL_GZBGB().GetDataPager("  SZHL_KS_KCGL kcgl inner join  JH_Auth_ZiDian kcfl on kcfl.ID=kcgl.KCTypeID ", " kcgl.*,kcfl.TypeName  ", pagecount, page, "kcgl.CRDate desc", strWhere, ref recordCount);
            msg.Result = dt;
            msg.Result1 = recordCount;
        }
        public void ADDKCGL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_KS_KCGL kcgl = JsonConvert.DeserializeObject<SZHL_KS_KCGL>(P1);
            if (string.IsNullOrEmpty(kcgl.KCName))
            {
                msg.ErrorMsg = "课件名称不能为空";
                return;
            }
            if (kcgl.ID == 0)
            {
                kcgl.CRDate = DateTime.Now;
                kcgl.CRUser = UserInfo.User.UserName;
                kcgl.ComId = UserInfo.User.ComId;
                new SZHL_KS_KCGLB().Insert(kcgl);
            }
            else
            {
                new SZHL_KS_KCGLB().Update(kcgl);
            }

            msg.Result = kcgl;
        }
        //删除课程
        public void DELKCGLBYID(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                if (new SZHL_KS_KCGLB().Delete(d => d.ID.ToString() == P1))
                {
                    // new SZHL_PX_KJKCB().Delete(d => d.KCID.ToString() == P1);
                    msg.ErrorMsg = "";
                }
            }
            catch (Exception ex)
            {
                msg.ErrorMsg = ex.Message;
            }
        }
        public void GETKCGLMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);
            msg.Result = new SZHL_KS_KCGLB().GetEntity(d => d.ID == Id);
        }
    }
}