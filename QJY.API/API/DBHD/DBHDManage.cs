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

namespace QJY.API
{
    public class DBHDManage : IWsService
    {
        public void ProcessRequest(HttpContext context, ref Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            MethodInfo methodInfo = typeof(DBHDManage).GetMethod(msg.Action.ToUpper());
            DBHDManage model = new DBHDManage();
            methodInfo.FastInvoke(model, new object[] { context, msg, P1, P2, UserInfo });
        }
        public void GETDBHDLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strWhere = "ComId=" + UserInfo.User.ComId;
            string strContent = context.Request["Content"] ?? "";
            strContent = strContent.TrimEnd();
            if (strContent != "")
            {
                strWhere += string.Format(" And ( TITLE like '%{0}%' )", strContent);
            }
            //根据创建时间查询
            string time = context.Request.QueryString["time"] ?? "";
            if (time != "")
            {
                if (time == "1")   //近一周
                {
                    strWhere += string.Format(" And datediff(day,CRDate,getdate())<7");
                }
                else if (time == "2")
                {  //近一月
                    strWhere += string.Format(" And datediff(day,CRDate,getdate())<30");
                }
                else if (time == "4")
                {  //今年
                    strWhere += string.Format(" And datediff(year,CRDate,getdate())=0");
                }
                else if (time == "5")
                {  //上一年
                    strWhere += string.Format(" And datediff(year,CRDate,getdate())=1");
                }
                else if (time == "3")  //自定义时间
                {
                    string strTime = context.Request.QueryString["starTime"] ?? "";
                    string endTime = context.Request.QueryString["endTime"] ?? "";
                    if (strTime != "")
                    {
                        strWhere += string.Format(" And convert(varchar(10),CRDate,120) >='{0}'", strTime);
                    }
                    if (endTime != "")
                    {
                        strWhere += string.Format(" And convert(varchar(10),CRDate,120) <='{0}'", endTime);
                    }
                }
            }
            int DataID = -1;
            int.TryParse(context.Request.QueryString["ID"] ?? "-1", out DataID);//记录Id
            if (DataID != -1)
            {
                string strIsHasDataQX = new JH_Auth_QY_ModelB().ISHASDATAREADQX("YXHD", DataID, UserInfo);
                if (strIsHasDataQX == "Y")
                {
                    strWhere += string.Format(" And  ID = '{0}'", DataID);
                }

            }


            int page = 0;
            int pagecount = 8;
            int.TryParse(context.Request.QueryString["p"] ?? "1", out page);
            int.TryParse(context.Request.QueryString["pagecount"] ?? "8", out pagecount);//页数
            page = page == 0 ? 1 : page;
            int total = 0;
            DataTable dtList = new SZHL_YX_HDB().GetDataPager("SZHL_YX_HD ", "*", pagecount, page, " CRDate ", strWhere, ref total);

            dtList.Columns.Add("PLList", Type.GetType("System.Object"));
            dtList.Columns.Add("FileList", Type.GetType("System.Object"));
            for (int i = 0; i < dtList.Rows.Count; i++)
            {
                dtList.Rows[i]["PLList"] = new JH_Auth_TLB().GetTL("DBHD", dtList.Rows[i]["ID"].ToString());
                if (dtList.Rows[i]["Files"].ToString() != "")
                    dtList.Rows[i]["FileList"] = new FT_FileB().GetEntities(" ID in (" + dtList.Rows[i]["Files"].ToString() + ")");
            }


            msg.Result = dtList;
            msg.Result1 = total;
        }
      
        public void GETDBHDMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int id = 0;
            int.TryParse(P1, out id);
            SZHL_YX_HD MODEL = new SZHL_YX_HDB().GetEntity(d => d.ID == id);
            if (MODEL != null)
            {
                msg.Result = MODEL;
                msg.Result1 = new SZHL_YX_HD_ITEMB().GetEntities(d => d.HDID == id);
                if (!string.IsNullOrEmpty(MODEL.Files))
                {
                    int[] fileIds = MODEL.Files.SplitTOInt(',');
                    msg.Result2 = new FT_FileB().GetEntities(d => fileIds.Contains(d.ID));

                }
               
            }


        }
        public void ADDDBHD(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_YX_HD  Model = JsonConvert.DeserializeObject<SZHL_YX_HD>(P1);
            List<SZHL_YX_HD_ITEM> itemList = JsonConvert.DeserializeObject<List<SZHL_YX_HD_ITEM>>(P2);
            if (itemList == null || itemList.Count() == 0)
            {
                msg.Result = "请添加消费记录";
                return;
            }

            string wximg = context.Request["wximg"] ?? "";
            if (wximg != "") // 处理微信上传的图片
            {

                string fids = CommonHelp.ProcessWxIMG(wximg, "JFBX", UserInfo);
                if (!string.IsNullOrEmpty(Model.Files))
                {
                    Model.Files += "," + fids;
                }
                else
                {
                    Model.Files = fids;
                }
            }
            if (Model.ID == 0)
            {
                Model.CRDate = DateTime.Now;
                Model.CRUser = UserInfo.User.UserName;
                Model.ComId = UserInfo.User.ComId;
            
                new SZHL_YX_HDB().Insert(Model);
                foreach (SZHL_YX_HD_ITEM item in itemList)
                {
                    item.HDID = Model.ID;
                    item.CRDate = DateTime.Now;
                    item.CRUser = UserInfo.User.UserName;
                    item.ComId = UserInfo.User.ComId;
                    new SZHL_YX_HD_ITEMB().Insert(item);
                }
            }
            else
            {

                new SZHL_YX_HDB().Update(Model);
                new SZHL_YX_HD_ITEMB().Delete(d => d.HDID == Model.ID);
                foreach (SZHL_YX_HD_ITEM item in itemList)
                {
                    item.HDID = Model.ID;
                    item.CRDate = DateTime.Now;
                    item.CRUser = UserInfo.User.UserName;
                    item.ComId = UserInfo.User.ComId;
                    new SZHL_YX_HD_ITEMB().Insert(item);
                }
            }
            msg.Result = Model;
        }

        public void GETDBHDTJ(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int page = 0;
            int pagecount = 8;
            int.TryParse(context.Request.QueryString["p"] ?? "1", out page);
            int.TryParse(context.Request.QueryString["pagecount"] ?? "8", out pagecount);//页数
            page = page == 0 ? 1 : page;
            int total = 0;


            int year = 0;
            int.TryParse(P1, out year);
            int month = 0;
            int.TryParse(P2, out month);
            string strWhere = "";
            if (month > 0)
            {
                DateTime newData = new DateTime(year, month, 1);
                strWhere = string.Format("  and DATEDIFF(MONTH, jfbx.BXDate,'{0}')=0 ", newData);
            }
            string content = context.Request["search"] ?? "";
            content = content.TrimEnd();
            if (content != "")
            {
                strWhere += string.Format("and (jfbx.ShenQingRen like '%{0}%' or jfbx.BranchName like '%{0}%')", content);
            }
            string strSql = string.Format(@"SELECT  jfbx.ShenQingRen,jfbx.CRUser,jfbx.BranchName,DATEPART(YEAR, jfbx.BXDate) BXYear,DATEPART(month, jfbx.BXDate) BXMonth,SUM(BXZJE) totalMoney FROM SZHL_JFBX jfbx
                                            Left join Yan_WF_PI wfpi on intProcessStanceid=wfpi.ID WHERE jfbx.ComId={0} and  ((wfpi.Id is not null and wfpi.isComplete='Y') or wfpi.Id is null)    {1} group by jfbx.ShenQingRen,jfbx.CRUser,jfbx.BranchName,DATEPART(YEAR, jfbx.BXDate),DATEPART(month, jfbx.BXDate)", UserInfo.User.ComId, strWhere);
            DataTable dtJFBX = new SZHL_JFBXB().GetDataPager("(" + strSql + ") as newjf", "*", pagecount, page, "BranchName,ShenQingRen", "1=1", ref total);
            msg.Result = dtJFBX;
            msg.Result1 = total;
        }



        public void DELDBHD(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = 0;
            int.TryParse(P2, out Id);
            new SZHL_YX_HDB().Delete(D => D.ID == Id);
            new SZHL_YX_HD_ITEMB().Delete(D => D.HDID == Id);

        }
    }
}