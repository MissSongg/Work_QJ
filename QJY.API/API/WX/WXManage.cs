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
using System.Text;

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



        /// <summary>
        /// 获取微信支付所需参数
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1">订单号</param>
        /// <param name="P2">code</param>
        /// <param name="UserInfo"></param>
        public void GETPAYJSAPI(HttpContext context, Msg_Result msg, int ComId, string P1, string P2, SZHL_YX_USER UserInfo)
        {

            var qy = new JH_Auth_QYB().GetEntity(p => p.ComId == UserInfo.ComId);

            TenPayV3Info _tenPayV3Info = new TenPayV3Info(qy.wxappid, qy.wxappsecret, qy.wxmchid, qy.wxmchidkey, "http://www.qijiekeji.com/API/WXAPI2.ashx?Action=WX_NOTIFY");
            if (string.IsNullOrEmpty(P2))
            {
                msg.ErrorMsg = "您拒绝了授权！";
                return;
            }


            var order = new SZHL_YX_HD_GMB().GetEntities(p => p.batchnumber == P1);
            if (order.Count() == 0)
            {
                msg.ErrorMsg = "订单信息错误！";
                return;
            }
            if (order.FirstOrDefault().wxbillstatus != "0")
            {
                msg.ErrorMsg = "订单状态有误,或您已支付.";
                return;
            }
            //通过，用code换取access_token
            var openIdResult = OAuthApi.GetAccessToken(_tenPayV3Info.AppId, _tenPayV3Info.AppSecret, P2);
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

            new SZHL_YX_HD_GMB().ExsSql("UPDATE SZHL_YX_HD_GM SET wxbillnumber='" + sp_billno + "' WHERE ComId='" + ComId + "' and batchnumber='" + P1 + "'");

            //微信支付单号
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

        /// <summary>
        /// 微信异步通知
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="ComId"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void NOTIFY(HttpContext context, Msg_Result msg, int ComId, string P1, string P2, SZHL_YX_USER UserInfo)
        {
            //1.更新支付状态

            try
            {
                var qy = new JH_Auth_QYB().GetEntity(p => p.ComId == UserInfo.ComId);
                ResponseHandler resHandler = new ResponseHandler(null);

                string return_code = resHandler.GetParameter("return_code");
                string return_msg = resHandler.GetParameter("return_msg");

                string res = null;

                resHandler.SetKey(qy.wxmchidkey);
                //验证请求是否从微信发过来（安全）
                if (resHandler.IsTenpaySign())
                {
                    res = "success";

                    //正确的订单处理

                    //修改订单状态为已支付
                    string transaction_id = resHandler.GetParameter("transaction_id");
                    string out_trade_no = resHandler.GetParameter("out_trade_no"); //订单号码
                    string total_fee = resHandler.GetParameter("total_fee");  //订单金额

                    //验证订单号
                    var order = new SZHL_YX_HD_GMB().GetEntities(p => p.wxbillnumber == out_trade_no);
                    if (order.Count() == 0)
                    {
                        throw new Exception("订单信息不存在!" + out_trade_no);
                    }
                    //if (order.OrderID == "" || order.TotalAmount == null)
                    //{
                    //    throw new Exception("订单信息错误!" + out_trade_no);
                    //}
                    if (order.First().wxbillstatus != "0")
                    {
                        throw new Exception("订单状态有误!或您已支付");
                    }
                    //if (order.TotalAmount == null || ((int)(order.TotalAmount * 100)).ToString() != total_fee)
                    //{
                    //    throw new Exception("订单金额有误!");
                    //}

                    new SZHL_YX_HD_GMB().ExsSql("UPDATE SZHL_YX_HD_GM SET wxbillstatus='1',wxbilltime=GETDATE() WHERE wxbillnumber='" + out_trade_no + "' ");

                    //保存微信返回参数，用户后续退款等操作
                    #region 保存参数
                    try
                    {
                        SZHL_YX_PAYINFO info = new SZHL_YX_PAYINFO();
                        info.OrderID = out_trade_no;
                        info.PayType = "weixin";
                        info.appid = resHandler.GetParameter("appid");
                        info.mch_id = resHandler.GetParameter("mch_id");
                        info.device_info = resHandler.GetParameter("device_info");
                        info.nonce_str = resHandler.GetParameter("nonce_str");
                        info.sign = resHandler.GetParameter("sign");
                        info.result_code = resHandler.GetParameter("result_code");
                        info.err_code = resHandler.GetParameter("err_code");
                        info.err_code_des = resHandler.GetParameter("err_code_des");
                        info.openid = resHandler.GetParameter("openid");
                        info.is_subscribe = resHandler.GetParameter("is_subscribe");
                        info.trade_type = resHandler.GetParameter("trade_type");
                        info.bank_type = resHandler.GetParameter("bank_type");
                        info.total_fee = resHandler.GetParameter("total_fee");
                        info.settlement_total_fee = resHandler.GetParameter("settlement_total_fee");
                        info.fee_type = resHandler.GetParameter("fee_type");
                        info.cash_fee = resHandler.GetParameter("cash_fee");
                        info.cash_fee_type = resHandler.GetParameter("cash_fee_type");
                        info.coupon_fee = resHandler.GetParameter("coupon_fee");
                        info.coupon_count = resHandler.GetParameter("coupon_count");
                        info.coupon_type_n = resHandler.GetParameter("coupon_type_$n");
                        info.coupon_id_n = resHandler.GetParameter("coupon_id_$n");
                        info.coupon_fee_n = resHandler.GetParameter("coupon_fee_$n");
                        info.transaction_id = resHandler.GetParameter("transaction_id");
                        info.out_trade_no = resHandler.GetParameter("out_trade_no");
                        info.attach = resHandler.GetParameter("attach");
                        info.time_end = resHandler.GetParameter("time_end");
                        if (new SZHL_YX_PAYINFOB().GetEntities(d => d.OrderID == info.OrderID).ToList().Count == 0)
                        {
                            new SZHL_YX_PAYINFOB().Insert(info);
                        }
                    }
                    catch (Exception ex)
                    {
                        //Tools.Common.WriteLog("异常lalala", "", ex.Message);
                    }
                    #endregion
                }
                else
                {
                    res = "wrong";

                    //错误的订单处理
                }

                var fileStream = System.IO.File.OpenWrite(HttpContext.Current.Server.MapPath("/LOG/wxLOG.txt"));
                fileStream.Write(Encoding.Default.GetBytes(res), 0, Encoding.Default.GetByteCount(res));
                fileStream.Close();

                string xml = string.Format(@"<xml>
                   <return_code><![CDATA[{0}]]></return_code>
                   <return_msg><![CDATA[{1}]]></return_msg>
                </xml>", return_code, return_msg);

                context.Response.Write(xml);
            }
            catch (Exception ex)
            {
                //Tools.Common.WriteLog("异常", "", ex.Message);
                throw;
            }

            //2.生成商品吗
        }
    }
}