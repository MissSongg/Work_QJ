﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Unity;
using QJY.Data;

namespace QJY.API
{
    public class ServiceContainerV
    {
        public static IUnityContainer Current()
        {

            IUnityContainer container = new UnityContainer();





            //免注册接口类
            container.RegisterType<IWsService, Commanage>("Commanage".ToUpper());//


            #region 基础模块接口

            //基础接口
            container.RegisterType<IWsService, AuthManage>("XTGL".ToUpper());//
            container.RegisterType<IWsService, INITManage>("INIT".ToUpper());//系统配置相关API



            #endregion

            #region 信息发布
            container.RegisterType<IWsService, XXFBManage>("XXFB");



            #endregion

            #region 出差休假
            container.RegisterType<IWsService, CCXJManage>("CCXJ".ToUpper());//根据部门获取用户列表

            #endregion


            #region 流程审批
            container.RegisterType<IWsService, LCSPManage>("LCSP".ToUpper());//


            #endregion

            #region 会议管理
            container.RegisterType<IWsService, HYGLManage>("HYGL");



            #endregion


            #region 用车管理
            container.RegisterType<IWsService, YCGLManage>("YCGL".ToUpper());//根据部门获取用户列表



            #endregion


            #region JSAPI
            container.RegisterType<IWsService, JSAPI>("JSSDK".ToUpper());
            #endregion


            #region 企业活动
            container.RegisterType<IWsService, QYHDManage>("QYHD");
            #endregion


            #region 短信管理
            container.RegisterType<IWsService, DXGLManage>("DXGL".ToUpper());//删除短信 
            #endregion

            #region 通讯录
            container.RegisterType<IWsService, TXLManage>("QYTX".ToUpper());//通讯录 
            #endregion

            #region 提醒事项
            container.RegisterType<IWsService, TXSXManage>("TXSX".ToUpper());//删除短信 

            #endregion

            #region 工作报告
            container.RegisterType<IWsService, GZBGManage>("GZBG");

            #endregion







            #region 文档管理
            container.RegisterType<IWsService, QYWDManage>("QYWD".ToUpper());//企业文档 




            #endregion


            //任务管理
            container.RegisterType<IWsService, RWGLManage>("RWGL".ToUpper());//添加任务管理 
            //项目管理
            container.RegisterType<IWsService, XMGLManage>("XMGL".ToUpper());//项目管理

            //记事本
            container.RegisterType<IWsService, NOTEManage>("NOTE".ToUpper());//记事本管理 
            //问题反馈
            container.RegisterType<IWsService, WTFKManage>("WTFK".ToUpper());//问题反馈 

            //CRM
            container.RegisterType<IWsService, CRMManage>("CRM".ToUpper());//CRM管理 

            //企业回话
            container.RegisterType<IWsService, QYIMManage>("QYIM".ToUpper());//企业会话
            container.RegisterType<IWsService, TSSQManage>("TSSQ".ToUpper());//同事社区
            container.RegisterType<IWsService, KDDYManage>("KDDY".ToUpper());//快递打印
            container.RegisterType<IWsService, JFBXManage>("JFBX".ToUpper());//经费报销
            container.RegisterType<IWsService, KQGLManage>("KQGL".ToUpper());//考勤管理
            container.RegisterType<IWsService, HELPManage>("HELP".ToUpper());//帮助中心
            container.RegisterType<IWsService, WQQDManage>("WQQD".ToUpper());//外勤签到

            container.RegisterType<IWsService, XZGLManage>("XZGL".ToUpper());//薪资管理
            container.RegisterType<IWsService, CHATManage>("CHAT".ToUpper());//及时聊天

            container.RegisterType<IWsService, DBGLManage>("DBGL".ToUpper());//及时聊天
            container.RegisterType<IWsService, KSGLManage>("KSGL".ToUpper());//考试管理
            container.RegisterType<IWsService, KCGLManage>("KCGL".ToUpper());//课程管理



            //营销活动
            container.RegisterType<IWsService, DBHDManage>("DBHD".ToUpper());//夺宝活动

            return container;
        }

    }


    /// <summary>
    /// 微信公众号接口
    /// </summary>
    public class ServiceContainerV2
    {
        public static IUnityContainer Current()
        {

            IUnityContainer container = new UnityContainer();
            container.RegisterType<IWsService2, WXManage>("WX".ToUpper());//微信操作


            return container;
        }
    }

}
