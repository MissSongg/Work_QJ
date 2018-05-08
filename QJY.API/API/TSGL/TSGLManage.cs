using QJY.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using FastReflectionLib;
using QJY.Data;
using Newtonsoft.Json;
using System.Data;

namespace QJY.API
{
    public class TSGLManage : IWsService
    {
        public void ProcessRequest(HttpContext context, ref Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            MethodInfo methodInfo = typeof(TSGLManage).GetMethod(msg.Action.ToUpper());
            TSGLManage model = new TSGLManage();
            methodInfo.FastInvoke(model, new object[] { context, msg, P1, P2, UserInfo });
        }
        #region 图书管理

        #region 获取图书列表
        /// <summary>
        /// 获取图书列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETTSLIST_PAGE(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strWhere = string.Format(" SZHL_TSGL_TS.ComId=" + UserInfo.User.ComId);
            if (P1 != "") //图书码
            {
                strWhere += string.Format("and SZHL_TSGL_TS.TSNum like '%{0}%'", P1);
            }
            if (P2 != "")//图书类型
            {
                strWhere += string.Format(" And SZHL_TSGL_TS.TSType='{0}'", P2); ;
            }
            int recordCount = 0;
            int page = 0;
            int pagecount = 8;
            int.TryParse(context.Request.QueryString["p"] ?? "1", out page);
            int.TryParse(context.Request.QueryString["pagecount"] ?? "8", out pagecount);//页数
            DataTable dt = new SZHL_TSGL_TSB().GetDataPager("SZHL_TSGL_TS   left join  JH_Auth_ZiDian zd on SZHL_TSGL_TS.tsType=zd.ID and zd.Class=24 ", "SZHL_TSGL_TS.*,zd.TypeName", pagecount, page, "SZHL_TSGL_TS.CRDate desc", strWhere, ref recordCount);
            msg.Result = dt;
            msg.Result1 = recordCount;
        }
        #endregion

        #region 添加图书信息
        /// <summary>
        /// 添加图书信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void ADDTSINFO(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_TSGL_TS Info = JsonConvert.DeserializeObject<SZHL_TSGL_TS>(P1);
            if (string.IsNullOrEmpty(Info.TSType))
            {
                msg.ErrorMsg = "请选择图书类型";
                return;
            }
            if (string.IsNullOrEmpty(Info.TSNum))
            {
                msg.ErrorMsg = "请填写图书编码";
                return;
            }
            if (Info.ID == 0)
            {
                SZHL_TSGL_TS MODEL = new SZHL_TSGL_TSB().GetEntity(d => d.TSNum == Info.TSNum && d.ComId == UserInfo.User.ComId);
                if (MODEL != null)
                {
                    msg.ErrorMsg = "已有此编码的图书";
                }
                else
                {
                    Info.CRDate = DateTime.Now;
                    Info.CRUser = UserInfo.User.UserName;
                    Info.ComId = UserInfo.User.ComId;
                    Info.IsDel = 0;
                    new SZHL_TSGL_TSB().Insert(Info);
                }
            }
            else
            {
                new SZHL_TSGL_TSB().Update(Info);
            }
        }
        #endregion

        #region 获取图书信息
        /// <summary>
        /// 获取图书信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETTSINFO(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);
            var info = new SZHL_TSGL_TSB().GetEntity(d => d.ID == Id && d.ComId == UserInfo.User.ComId);
            msg.Result = info;
            if (!string.IsNullOrEmpty(info.Files))
            {
                msg.Result4 = new FT_FileB().GetEntities(" ID in (" + info.Files + ")");
            }
        }
        #endregion

        #region 更新图书状态
        /// <summary>
        /// 更新图书状态
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void MODIFYTSSTATUS(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);
            string strSql = string.Format(" update SZHL_TSGL_TS set status={0}  where Id={1} and ComId={2}", P2, P1, UserInfo.User.ComId);
            new SZHL_TSGL_TSB().ExsSql(strSql);
        }
        #endregion

        #region 获取所有图书
        /// <summary>
        /// 获取所有图书
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETALLTSLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strSql = string.Format("SELECT ts.*,zd.TypeName from SZHL_TSGL_TS ts left join  JH_Auth_ZiDian zd on ts.tsType=zd.ID and Class=24 Where ts.ComId={0} and ts.Status=0", UserInfo.User.ComId);
            msg.Result = new SZHL_TSGL_TSB().GetDTByCommand(strSql);
        }
        #endregion

        #region 删除图书
        public void DELTS(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int id = int.Parse(P1);
            new SZHL_TSGL_TSB().Delete(d => d.ID == id && d.ComId == UserInfo.User.ComId.Value);
        }
        #endregion

        #endregion

        #region 借阅管理

        #region 借阅列表
        /// <summary>
        /// 借阅列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETJYGLLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string userName = UserInfo.User.UserName;
            string strWhere = " 1=1 and jy.ComId=" + UserInfo.User.ComId;

            string leibie = context.Request["lb"] ?? "";
            if (leibie != "")
            {
                strWhere += string.Format(" And jy.TSID='{0}' ", leibie);
            }
            string strContent = context.Request["Content"] ?? "";
            strContent = strContent.TrimEnd();
            if (strContent != "")
            {
                strWhere += string.Format(" And ( jy.Remark like '%{0}%')", strContent);
            }
            int DataID = -1;
            int.TryParse(context.Request.QueryString["ID"] ?? "-1", out DataID);//记录Id
            if (DataID != -1)
            {
                string strIsHasDataQX = new JH_Auth_QY_ModelB().ISHASDATAREADQX("TSGL", DataID, UserInfo);
                if (strIsHasDataQX == "Y")
                {
                    strWhere += string.Format(" And jy.ID = '{0}'", DataID);
                }

            }

            if (P1 != "")
            {
                int page = 0;
                int pagecount = 8;
                int.TryParse(context.Request.QueryString["p"] ?? "1", out page);
                int.TryParse(context.Request.QueryString["pagecount"] ?? "8", out pagecount);//页数
                page = page == 0 ? 1 : page;
                int total = 0;

                DataTable dt = new DataTable();
                switch (P1)
                {
                    case "0": //手机单条数据
                        {
                            //设置usercenter已读
                            new JH_Auth_User_CenterB().ReadMsg(UserInfo, DataID, "YCGL");
                        }
                        break;
                    case "1": //创建的
                        {
                            strWhere += " And jy.CRUser ='" + userName + "'";
                        }
                        break;
                    case "2": //待审核
                        {
                            var intProD = new Yan_WF_PIB().GetDSH(UserInfo.User).Select(d => d.PIID.ToString()).ToList();
                            if (intProD.Count > 0)
                            {
                                strWhere += " And jy.intProcessStanceid in (" + (intProD.ListTOString(',') == "" ? "0" : intProD.ListTOString(',')) + ")";
                            }
                            else
                            {
                                strWhere += " And 1=0";
                            }
                        }
                        break;
                    case "3":  //已审核
                        {
                            var intProD = new Yan_WF_PIB().GetYSH(UserInfo.User).Select(d => d.PIID.ToString()).ToList();
                            if (intProD.Count > 0)
                            {
                                strWhere += " And jy.intProcessStanceid in (" + (intProD.ListTOString(',') == "" ? "0" : intProD.ListTOString(',')) + ")";

                            }
                            else
                            {
                                strWhere += " And 1=0";
                            }
                        }
                        break;
                }

                dt = new SZHL_TSGLB().GetDataPager("SZHL_TSGL yc left join SZHL_TSGL_TS ts on jy.tsID=ts.ID", "jy.*,ts.tsType,ts.tsNum ,dbo.fn_PDStatus(jy.intProcessStanceid) AS StateName", pagecount, page, " jy.CRDate desc", strWhere, ref total);

                if (dt.Rows.Count > 0)
                {
                    dt.Columns.Add("FileList", Type.GetType("System.Object"));
                    foreach (DataRow dr in dt.Rows)
                    {
                        if (dr["Files"] != null && dr["Files"].ToString() != "")
                        {
                            dr["FileList"] = new FT_FileB().GetEntities(" ID in (" + dr["Files"].ToString() + ")");
                        }
                    }
                }
                msg.Result = dt;
                msg.Result1 = total;
            }
        }
        #endregion

        #region 借阅管理日历视图
        /// <summary>
        /// 借阅管理日历视图
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETYCGLVIEW(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strSql = string.Format("SELECT  ycgl.ID,ycgl.intProcessStanceid,ts.tsBrand+'-'+ts.tsType+'-'+ts.tsNum+'  '+CONVERT(VARCHAR(5),ycgl.StartTime,8)+'~'+CONVERT(VARCHAR(5),ycgl.EndTime,8) title,ycgl.StartTime start,ycgl.EndTime [end]  from SZHL_TSGL  ycgl left outer join SZHL_TSGL_TS ts on ycgl.tsID=ts.ID   where ( dbo.fn_PDStatus(ycgl.intProcessStanceid)='已审批' or dbo.fn_PDStatus(ycgl.intProcessStanceid)='正在审批' or dbo.fn_PDStatus(ycgl.intProcessStanceid)='-1' ) and ycgl.ComId=" + UserInfo.User.ComId + " and isnull(ts.tsType,'')!=''");
            if (P1 != "0")
            {
                strSql += string.Format(" and ycgl.tsID={0} ", P1);
            }
            msg.Result = new SZHL_CCXJB().GetDTByCommand(strSql);
        }
        #endregion

        #region 部门数据
        /// <summary>
        /// 部门数据
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETTSGLLIST_PAGE(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strWhere = " jygl.ComId=" + UserInfo.User.ComId;

            if (P1 != "")
            {

                strWhere += string.Format(" And  jygl.XCType='{0}'", P1);
            }
            if (P2 != "")
            {
                strWhere += string.Format(" And  jygl.Remark like '%{0}%'", P2);
            }

            int page = 0;
            int.TryParse(context.Request.QueryString["p"] ?? "1", out page);
            page = page == 0 ? 1 : page;
            int total = 0;
            string colNme = @"jygl.*,ts.tsBrand,ts.tsType,ts.tsNum ,    case WHEN wfpi.isComplete is null and wfpi.IsCanceled is null  THEN '正在审批' 
                                            when wfpi.isComplete='Y' then '已审批'  WHEN wfpi.IsCanceled='Y' then '已退回' END StateName";
            DataTable dt = new SZHL_CCXJB().GetDataPager("SZHL_TSGL jygl left outer join SZHL_TSGL_TS  ts on jygl.tsID=ts.ID  inner join  Yan_WF_PI wfpi  on jygl.intProcessStanceid=wfpi.ID", colNme, 8, page, " ycgl.CRDate desc", strWhere, ref total);
            msg.Result = dt;
            msg.Result1 = total;
        }
        #endregion

        #region 获取可用图书
        /// <summary>
        /// 获取可用图书
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETTSIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            // string strSql = string.Format("SELECT  ts.* from  SZHL_TSGL_TS ts LEFT JOIN SZHL_TSGL ycgl on ts.ID=ycgl.tsID where ts.Status=0 and (ycgl.Status=0 or ycgl.Status is NULL) and ts.ComId={0}", UserInfo.User.ComId);
            string strSql = string.Format("SELECT  ts.* from  SZHL_TSGL_TS ts  where ts.Status=0  and ts.ComId={0}", UserInfo.User.ComId);
            //if (P1 != "")
            //{
            //    strSql += string.Format(" and ycgl.EndTime<'{0}' ", P1);
            //}
            msg.Result = new SZHL_TSGL_TSB().GetDTByCommand(strSql);
        }
        #endregion

        #region 查看可用图书列表（微信端）
        /// <summary>
        /// 查看可用图书列表（微信端）
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETKYTSLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strwhere = string.Empty;
            
            DataTable dt = new SZHL_TSGL_TSB().GetDTByCommand("select * from dbo.SZHL_TSGL_TS where IsDel=0 and Status='0'  and comid=" + UserInfo.QYinfo.ComId + strwhere);

            dt.Columns.Add("tsTypeName", Type.GetType("System.String"));
            dt.Columns.Add("ZT", Type.GetType("System.String"));
            dt.Columns.Add("ZYSJ", Type.GetType("System.String"));

            foreach (DataRow dr in dt.Rows)
            {
               
            }

            msg.Result = dt;
        }
        #endregion

        #region 添加借阅管理
        /// <summary>
        /// 添加借阅管理
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void ADDJYGL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_TSGL jygl = JsonConvert.DeserializeObject<SZHL_TSGL>(P1);
            if (jygl == null)
            {
                msg.ErrorMsg = "操作失败";
                return;
            }
            if (jygl.ID == 0)
            {
                if (P2 != "") // 处理微信上传的图片
                {
                    string fids = CommonHelp.ProcessWxIMG(P2, "YCGL", UserInfo);
                    if (!string.IsNullOrEmpty(jygl.Files))
                    {
                        jygl.Files += "," + fids;
                    }
                    else
                    {
                        jygl.Files = fids;
                    }
                }
                jygl.CRDate = DateTime.Now;
                jygl.CRUser = UserInfo.User.UserName;
                jygl.ComId = UserInfo.User.ComId;
                jygl.Status = "1";
                jygl.IsDel = 0;
                new SZHL_TSGLB().Insert(jygl);
            }
            else
            {
                new SZHL_TSGLB().Update(jygl);
            }
            msg.Result = jygl;
        }
        #endregion

        #region 获取借阅信息
        /// <summary>
        /// 获取借阅信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETJYGLMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);
            var model = new SZHL_TSGLB().GetEntity(d => d.ID == Id && d.ComId == UserInfo.User.ComId);
            if (model != null)
            {
                msg.Result = model;
                if (!string.IsNullOrEmpty(model.Files))
                {
                    msg.Result1 = new FT_FileB().GetEntities(" ID in (" + model.Files + ")");
                }
                if (model.TSID != null)
                {
                    msg.Result2 = new SZHL_TSGL_TSB().GetEntity(p => p.ID == model.TSID);
                }

                new JH_Auth_User_CenterB().ReadMsg(UserInfo, model.ID, "TSGL");
            }
        }
        #endregion

        #region 更新归还图书记录
        /// <summary>
        /// 更新归还图书记录
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void BACKJYJL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int ID = int.Parse(P1);
            SZHL_TSGL ysgl = new SZHL_TSGLB().GetEntity(d => d.ID == ID && d.ComId == UserInfo.User.ComId);
            ysgl.Status = "0";//0 归还  1正在使用
            ysgl.BackDate = DateTime.Now;
            new SZHL_TSGLB().Update(ysgl);
            msg.Result = ysgl;
        }
        #endregion

        

        #endregion
    }
}