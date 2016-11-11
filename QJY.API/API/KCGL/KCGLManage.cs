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
        #region 课程管理
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
            DataTable dt = new SZHL_GZBGB().GetDataPager("  SZHL_PX_KCGL kcgl inner join  JH_Auth_ZiDian kcfl on kcfl.ID=kcgl.KCTypeID ", " kcgl.*,kcfl.TypeName  ", pagecount, page, "kcgl.CRDate desc", strWhere, ref recordCount);
            msg.Result = dt;
            msg.Result1 = recordCount;
        }
        public void ADDKCGL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_PX_KCGL kcgl = JsonConvert.DeserializeObject<SZHL_PX_KCGL>(P1);
            if (string.IsNullOrEmpty(kcgl.KCName))
            {
                msg.ErrorMsg = "课程名称不能为空";
                return;
            }
            if (kcgl.ID == 0)
            {
                kcgl.CRDate = DateTime.Now;
                kcgl.CRUser = UserInfo.User.UserName;
                kcgl.ComId = UserInfo.User.ComId;
                new SZHL_PX_KCGLB().Insert(kcgl);
            }
            else
            {
                new SZHL_PX_KCGLB().Update(kcgl);
            }

            msg.Result = kcgl;
        }
        //删除课程
        public void DELKCGLBYID(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                if (new SZHL_PX_KCGLB().Delete(d => d.ID.ToString() == P1))
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
            SZHL_PX_KCGL kcgl = new SZHL_PX_KCGLB().GetEntity(d => d.ID == Id);
            msg.Result = kcgl;
            int[] kjIds = kcgl.KJID.SplitTOInt(',');
            msg.Result1 = new SZHL_PX_KJGLB().GetEntities(d => kjIds.Contains(d.ID)).Select(d => d.KJName).ToList().ListTOString(',');
        }

        #endregion
        #region 课件管理
        public void GETPXKJLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int page = 0;
            int.TryParse(context.Request.QueryString["p"] ?? "1", out page);//页码
            page = page == 0 ? 1 : page;
            int recordCount = 0;
            string strWhere = string.Format(" ComId={0} ", UserInfo.User.ComId);
            if (P1 != "")
            {
                strWhere += string.Format(" and KJType={0}", P1);
            }
            if (P2 != "")
            {
                strWhere += string.Format(" and KJZZType={0}", P2);
            }
            string content = context.Request["Content"] ?? "";
            if (content != "")
            {
                strWhere += string.Format(" and KJName like '%{0}%'", content);
            }
            int pagecount = 0;
            int.TryParse(context.Request.QueryString["pagecount"] ?? "1", out pagecount);//页码
            pagecount = pagecount == 0 ? 10 : pagecount;
            DataTable dt = new SZHL_GZBGB().GetDataPager(" SZHL_PX_KJGL", "*  ", pagecount, page, "CRDate desc", strWhere, ref recordCount);

            msg.Result = dt;
            msg.Result1 = recordCount;
        }
        public void ADDKJGL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_PX_KJGL kjgl = JsonConvert.DeserializeObject<SZHL_PX_KJGL>(P1);

            if (string.IsNullOrEmpty(kjgl.KJName))
            {
                msg.ErrorMsg = "课件名称不能为空";
                return;
            }
            if (string.IsNullOrEmpty(kjgl.Files))
            {
                msg.ErrorMsg = "课件必须有课件文件";
                return;
            }

            if (kjgl.ID == 0)
            {
                kjgl.CRDate = DateTime.Now;
                kjgl.CRUser = UserInfo.User.UserName;
                kjgl.ComId = UserInfo.User.ComId;

                new SZHL_PX_KJGLB().Insert(kjgl);

            }
            else
            {
                SZHL_PX_KJGL kjgl1 = new SZHL_PX_KJGLB().GetEntity(d => d.ID == kjgl.ID);
                new SZHL_PX_KJGLB().Update(kjgl);
            }

            msg.Result = kjgl;
        }
        public void DELKJGLBYID(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                if (new SZHL_PX_KJGLB().Delete(d => d.ID.ToString() == P1))
                {
                    msg.ErrorMsg = "";
                }
            }
            catch (Exception ex)
            {
                msg.ErrorMsg = ex.Message;
            }
        }
        public void GETKJGLMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                SZHL_PX_KJGLB kjglb = new SZHL_PX_KJGLB();

                int Id = 0;
                int.TryParse(P1, out Id);
                SZHL_PX_KJGL kjglModel = kjglb.GetEntity(d => d.ID == Id && d.ComId == UserInfo.User.ComId);
                if (kjglModel != null && !string.IsNullOrEmpty(kjglModel.Files))
                {
                    List<FT_File> files = new FT_FileB().GetEntities(" ID in (" + kjglModel.Files + ")").ToList();
                    //if (kjglModel.KJZZType == 2)
                    //{
                    //    FT_File file = files[0];
                    //    DateTime time = kjglModel.CRDate.Value;
                    //    msg.Result4 = "http://office.qijieyun.com/" + UserInfo.QYinfo.QYCode + "/" + time.Year + "/" + time.Month + "/" + file.FileMD5 + "/" + kjglModel.FilePath;
                    //}
                    msg.Result1 = files;

                }
                msg.Result = kjglModel;
            }
            catch (Exception ex)
            {
                msg.ErrorMsg = "请检查课件文件";
            }
        }

        #endregion

    }
}