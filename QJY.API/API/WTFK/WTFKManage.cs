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
    public class WTFKManage : IWsService
    {

        public void ProcessRequest(HttpContext context, ref Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            MethodInfo methodInfo = typeof(WTFKManage).GetMethod(msg.Action.ToUpper());
            WTFKManage model = new WTFKManage();
            methodInfo.FastInvoke(model, new object[] { context, msg, P1, P2, UserInfo });
        }

        /// <summary>
        /// 获取反馈列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">类型</param>
        /// <param name="P2">查询条件</param>
        /// <param name="strUserName"></param>
        public void GETWTFKLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int DataID = -1;
            int.TryParse(context.Request.QueryString["ID"] ?? "-1", out DataID);//页码
            int page = 0;
            int.TryParse(context.Request.QueryString["p"] ?? "1", out page);//页码
            page = page == 0 ? 1 : page;
            int recordCount = 0;
            string strWhere = string.Format(" wtfk.ComId={0} ", UserInfo.User.ComId);
            //string type = context.Request.QueryString["type"] ?? "1";
            //if (type == "1") //当前登录人反馈
            //{
            //    strWhere += string.Format(" and wtfk.CRUser='{0}'", UserInfo.User.UserName);
            //}
            //string stutas = context.Request.QueryString["stutas"] ?? "0";
            //if (stutas == "1") //已处理
            //{
            //    strWhere += string.Format("And  wtfk.CLStatus=1 ");
            //}
            //else if (stutas == "0")//未处理
            //{
            //    strWhere += string.Format("And (wtfk.CLStatus <> 1 OR wtfk.CLStatus is NULL)");
            //}

            //if (!string.IsNullOrWhiteSpace(P1)) //类别
            //{
            //    strWhere += string.Format("And  wtfk.LeiBie='{0}'", P1);
            //}

            if (P2 != "")//内容查询
            {
                strWhere += string.Format(" And wtfk.FKContent like '%{0}%'", P2);
            }
            if (DataID != -1)
            {
                string strIsHasDataQX = new JH_Auth_QY_ModelB().ISHASDATAREADQX("WTFK", DataID, UserInfo);
                if (strIsHasDataQX == "Y")
                {
                    strWhere += string.Format(" And wtfk.ID = '{0}'", DataID);
                }
            }
            //DataTable dt = new SZHL_WTFKB().GetDataPager(" SZHL_WTFK wtfk inner join JH_Auth_ZiDian zd on LeiBie= zd.ID and Class=9  "
            //    , " wtfk.*,zd.TypeName ", 8, page, "wtfk.CRDate desc", strWhere, ref recordCount);
            DataTable dt = new SZHL_WTFKB().GetDataPager(" SZHL_WTFK wtfk ", " wtfk.*", 8, page, "wtfk.CRDate desc", strWhere, ref recordCount);
            #region 附件评论
            string Ids = "";
            string fileIDs = "";
            foreach (DataRow row in dt.Rows)
            {
                Ids += row["ID"].ToString() + ",";
                if (!string.IsNullOrEmpty(row["Files"].ToString()))
                {
                    fileIDs += row["Files"].ToString() + ",";
                }
            }
            Ids = Ids.TrimEnd(',');
            fileIDs = fileIDs.TrimEnd(',');
            if (Ids != "")
            {
                List<FT_File> FileList = new List<FT_File>();
                DataTable dtPL = new JH_Auth_TLB().GetDTByCommand(string.Format("SELECT tl.ID,tl.MSGTLYID,tl.MSGType,tl.MSGContent,tl.Points,tl.CRDate,tl.CRUser,tl.CRUserName  FROM JH_Auth_TL tl WHERE tl.MSGType='WTFK' AND  tl.MSGTLYID in ({0})", Ids));
                if (!string.IsNullOrEmpty(fileIDs))
                {
                    int[] fileId = fileIDs.SplitTOInt(',');
                    FileList = new FT_FileB().GetEntities(d => fileId.Contains(d.ID)).ToList();
                }
                dt.Columns.Add("PLList", Type.GetType("System.Object"));
                dt.Columns.Add("FileList", Type.GetType("System.Object"));
                foreach (DataRow row in dt.Rows)
                {
                    row["PLList"] = dtPL.FilterTable("MSGTLYID='" + row["ID"] + "'");
                    if (FileList.Count > 0)
                    {

                        string[] fileIds = row["Files"].ToString().Split(',');
                        row["FileList"] = FileList.Where(d => fileIds.Contains(d.ID.ToString()));
                    }
                }
            }
            #endregion
            msg.Result = dt;
        //    msg.Result1 = Math.Ceiling(recordCount * 1.0 / 8);
            msg.Result1 = recordCount;
        }

        /// <summary>
        /// 添加反馈
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="strUserName"></param>
        public void ADDWTFK(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_WTFK WTFK = JsonConvert.DeserializeObject<SZHL_WTFK>(P1);

            if (WTFK.FKContent == null)
            {
                msg.ErrorMsg = "反馈内容不能为空";
                return;
            }

            if (P2 != "") // 处理微信上传的图片
            {

                string fids = CommonHelp.ProcessWxIMG(P2, "WTFK", UserInfo);
                if (!string.IsNullOrEmpty(WTFK.Files))
                {
                    WTFK.Files += "," + fids;
                }
                else
                {
                    WTFK.Files = fids;
                }
            }

            if (WTFK.ID == 0)
            {
                WTFK.CRDate = DateTime.Now;
                WTFK.CRUser = UserInfo.User.UserName;
                WTFK.ComId = UserInfo.User.ComId;
                new SZHL_WTFKB().Insert(WTFK);
            }
            else
            {
                string type = context.Request["type"] ?? "";
                if (type == "1")
                {
                    WTFK.ZDR = UserInfo.User.UserName;
                }
                if (type == "2")
                {
                    if (string.IsNullOrWhiteSpace(WTFK.CLContent))
                    {
                        msg.ErrorMsg = "反馈情况不能为空";
                        return;
                    }
                    WTFK.CLR = UserInfo.User.UserName;
                    WTFK.CLDate = DateTime.Now;
                    WTFK.CLStatus = 1;
                }
                WTFK.CRDate = DateTime.Now;
                new SZHL_WTFKB().Update(WTFK);
            }
            msg.Result = WTFK;
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">ID</param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void DELWTFKBYID(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                if (new SZHL_WTFKB().Delete(d => d.ID.ToString() == P1))
                {
                    msg.ErrorMsg = "";
                }
            }
            catch (Exception ex)
            {
                msg.ErrorMsg = ex.Message;
            }
        }

        public void GETWTFKMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                int Id = 0;
                int.TryParse(P1, out Id);
                SZHL_WTFK wtfk = new SZHL_WTFKB().GetEntity(d=>d.ID==Id);
                msg.Result = wtfk;
                DataTable dtPL = new SZHL_WTFKB().GetDTByCommand("  SELECT *  FROM JH_Auth_TL WHERE MSGType='WTFK' AND  MSGTLYID='" + P1 + "'");
                dtPL.Columns.Add("FileList", Type.GetType("System.Object"));
                foreach (DataRow dr in dtPL.Rows)
                {
                    if (dr["MSGisHasFiles"] != null && dr["MSGisHasFiles"].ToString() != "")
                    {
                        int[] fileIds = dr["MSGisHasFiles"].ToString().SplitTOInt(',');
                        dr["FileList"] = new FT_FileB().GetEntities(d => fileIds.Contains(d.ID));
                    }
                }

                msg.Result1 = dtPL;
                if (!string.IsNullOrEmpty(wtfk.Files))
                {
                    int[] fileIds = wtfk.Files.SplitTOInt(',');
                    msg.Result2 = new FT_FileB().GetEntities(d => fileIds.Contains(d.ID));
                }
            }
            catch (Exception ex)
            {
                msg.ErrorMsg = ex.Message;
            }
        }
     
    }
}