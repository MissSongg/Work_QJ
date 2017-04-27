using QJY.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using FastReflectionLib;
using QJY.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using Senparc.Weixin.QY.Entities;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Senparc.Weixin.MP;
using Senparc.Weixin.MP.AdvancedAPIs.OAuth;
using Senparc.Weixin.MP.TenPayLibV3;
using Senparc.Weixin.MP.AdvancedAPIs;
using Senparc.Weixin;

namespace QJY.API
{
    /// <summary>
    /// 微信公众号使用
    /// </summary>
    public class WXManage : IWsService2
    {
        public void ProcessRequest(HttpContext context, ref Msg_Result msg, int ComId, string P1, string P2, SZHL_YX_USER UserInfo)
        {
            MethodInfo methodInfo = typeof(WXManage).GetMethod(msg.Action.ToUpper());
            WXManage model = new WXManage();
            methodInfo.FastInvoke(model, new object[] { context, msg, ComId, P1, P2, UserInfo });
        }

        private static TenPayV3Info _tenPayV3Info = null;

        /// <summary>
        /// 获取微信支付所需参数
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">code</param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETPAYJSAPI(HttpContext context, Msg_Result msg, int ComId, string P1, string P2, SZHL_YX_USER UserInfo)
        {

            var qy = new JH_Auth_QYB().GetEntity(p => p.ComId == UserInfo.ComId);

            if (_tenPayV3Info == null)
            {
                _tenPayV3Info = new TenPayV3Info(qy.wxappid, qy.wxappsecret, qy.wxmchid, qy.wxmchidkey, "");
            }
            if (string.IsNullOrEmpty(P1))
            {
                msg.ErrorMsg = "您拒绝了授权！";
                return;
            }

            //var ord = new PT_Order_HeaderB().GetEntity(p => p.ID == orderid);
            //if (ord == null)
            //{
            //    msg.error = "订单信息错误！";
            //    return;
            //}
            //if (ord.Status != 0)
            //{
            //    msg.error = "订单状态有误,或您已支付.";
            //    return;
            //}
            //通过，用code换取access_token
            var openIdResult = OAuthApi.GetAccessToken(_tenPayV3Info.AppId, _tenPayV3Info.AppSecret, P1);
            if (openIdResult.errcode != ReturnCode.请求成功)
            {
                msg.ErrorMsg = "错误：" + openIdResult.errmsg;
                return;
            }

            string timeStamp = "";
            string nonceStr = "";

            Random MyRandom = new Random();
            int RandomNum = MyRandom.Next(1001, 9999);
            int RandomNum2 = MyRandom.Next(1001, 9999);
            string sp_billno = DateTime.Now.ToString("HHssmmfff") + RandomNum.ToString() + RandomNum2.ToString();
            //ord.BusinessNo = sp_billno;
            //new PT_Order_HeaderB().ExsSql("UPDATE pt_order_header SET BusinessNo='" + sp_billno + "' WHERE ID='" + ord.ID + "'");

            decimal payprice = 0; //ord.SumPrice.Value;
            //当前时间 yyyyMMdd
            string date = DateTime.Now.ToString("yyyyMMdd");

            timeStamp = TenPayV3Util.GetTimestamp();
            nonceStr = TenPayV3Util.GetNoncestr();

            //Tools.Common.WriteLog("微信订单参数", "", data);

            TenPayV3UnifiedorderRequestData datainfo = new TenPayV3UnifiedorderRequestData(
                _tenPayV3Info.AppId,
                _tenPayV3Info.MchId,
                "商品",
                sp_billno,
                (int)(payprice * 100),
                HttpContext.Current.Request.UserHostAddress,
                _tenPayV3Info.TenPayV3Notify,
                TenPayV3Type.JSAPI,
                openIdResult.openid,
                _tenPayV3Info.Key,
                nonceStr
               );


            var payResult = TenPayV3.Unifiedorder(datainfo);

            msg.Result = new JObject(
                    new JProperty("appId", _tenPayV3Info.AppId),
                    new JProperty("timeStamp", timeStamp),
                    new JProperty("nonceStr", nonceStr),
                    new JProperty("package", string.Format("prepay_id={0}", payResult.prepay_id)),
                    new JProperty("signType", "MD5"),
                    new JProperty("paySign", payResult.sign)
                );


        }


        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            if (errors == SslPolicyErrors.None)
                return true;
            return false;
        }
    }
}