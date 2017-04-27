using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Web;
using FastReflectionLib;
using QJY.Data;

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
            msg.Result = new SZHL_YX_HD_ITEMB().GetEntities(p => p.ComId == ComId && p.HDID == ID);
        }


    }
}
