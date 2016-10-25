using Newtonsoft.Json;
using QJY.Data;
using QJY.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using FastReflectionLib;
using System.Data;

namespace QJY.API
{
    public class HELPManage : IWsService
    {
        public void ProcessRequest(HttpContext context, ref Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            MethodInfo methodInfo = typeof(HELPManage).GetMethod(msg.Action.ToUpper());
            HELPManage model = new HELPManage();
            methodInfo.FastInvoke(model, new object[] { context, msg, P1, P2, UserInfo });
        }

        public void ADDHMENU(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_HelpMenu hMenu = JsonConvert.DeserializeObject<SZHL_HelpMenu>(P1);
            if (hMenu.ID == 0)
            {
                hMenu.CRDate = DateTime.Now;
                hMenu.CRUser = UserInfo.User.UserName;
                hMenu.CRUserName = UserInfo.User.UserRealName;
                hMenu.ComId = UserInfo.User.ComId;
                new SZHL_HelpMenuB().Insert(hMenu);
            }
            else
            {
                hMenu.CRDate = DateTime.Now;
                new SZHL_HelpMenuB().Update(hMenu);
            }
            msg.Result = hMenu;
        }

        public void DELGZBGBYID(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                int id = int.Parse(P1);
                hMenu(id);
                new SZHL_HelpMenuB().Delete(d => d.ID == id);

            }
            catch (Exception)
            {
                msg.ErrorMsg = "";
            }
        }

        public void hMenu(int id)
        {
            List<SZHL_HelpMenu> hmlist = new SZHL_HelpMenuB().GetEntities(d => d.PID == id).ToList();
            if (hmlist.Count == 0)
                return;
            for (int i = 0; i < hmlist.Count; i++)
            {
                hMenu(hmlist[i].ID);
                new SZHL_HelpMenuB().Delete(d => d.ID == hmlist[i].ID);
            }
        }

        public void GETBZMENU(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string sql = string.Format("SELECT ID,MenuName,PID,Title,CRDate,CRUserName,MenuChapter FROM SZHL_HelpMenu ");

            DataTable dt = new SZHL_HelpMenuB().GetDTByCommand(sql);
            dt.Columns.Add("SubDept", Type.GetType("System.Object"));
            DataTable menu = dt.FilterTable("PID is null OR PID=0 ");
            msg.Result = GetNextWxUser(menu, dt);
        }

        public DataTable GetNextWxUser(DataTable dt, DataTable dtm)
        {
            foreach (DataRow dr in dt.Rows)
            {
                DataTable dtp = dtm.FilterTable(" PID=" + dr["ID"]);
                dr["SubDept"] = GetNextWxUser(dtp, dtm);
            }
            return dt;
        }
        public void GETBZMENUBYID(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int id = int.Parse(P1);
            SZHL_HelpMenu hm = new SZHL_HelpMenuB().GetEntity(d => d.ID == id);
            msg.Result = hm;
            if (hm == null)
                return;
            if (hm.PID != null)
                msg.Result1 = new SZHL_HelpMenuB().GetEntity(d => d.ID == hm.PID);
        }
    }
}