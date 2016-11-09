using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FastReflectionLib;
using System.Web;
using System.Data;
using QJY.Data;
using Newtonsoft.Json;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using Aspose.Words.Saving;
using System.Net;


namespace QJY.API
{
    public class KSGLManage : IWsService
    {
        public void ProcessRequest(HttpContext context, ref Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            MethodInfo methodInfo = typeof(KSGLManage).GetMethod(msg.Action.ToUpper());
            KSGLManage model = new KSGLManage();
            methodInfo.FastInvoke(model, new object[] { context, msg, P1, P2, UserInfo });
        }
        #region 题库分类
        /// <summary>
        /// 题库分类ztree获取列表
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETTKFLLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int page = 0;
            int.TryParse(context.Request.QueryString["p"] ?? "1", out page);//页码
            page = page == 0 ? 1 : page;
            int recordCount = 0;
            int pagecount = 0;
            int.TryParse(context.Request.QueryString["pagecount"] ?? "1", out pagecount);//页码
            pagecount = pagecount == 0 ? 10 : pagecount;
            DataTable dt = new SZHL_GZBGB().GetDataPager("  SZHL_KS_TKFL ", " ID,TKFLName,Remark,CRUser,CRDate  ", pagecount, page, "CRDate desc", string.Format(" comId='{0}' and isDel!=1", UserInfo.User.ComId), ref recordCount);
            msg.Result = dt;
            msg.Result1 = recordCount;

        }
        public void ADDTKFL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_KS_TKFL type = JsonConvert.DeserializeObject<SZHL_KS_TKFL>(P1);
            if (type != null)
            {
                if (new SZHL_KS_TKFLB().GetEntities(d => d.TKFLName == type.TKFLName && d.ComId == UserInfo.User.ComId && d.ID != type.ID).Count() > 0)
                {
                    msg.ErrorMsg = "分类已存在";
                    return;
                }
                if (type.ID == 0)
                {


                    type.CRDate = DateTime.Now;
                    type.CRUser = UserInfo.User.UserName;
                    type.ComId = UserInfo.User.ComId;
                    type.ISDel = 0;
                    new SZHL_KS_TKFLB().Insert(type);

                }
                else
                {
                    new SZHL_KS_TKFLB().Update(type);
                }
            }
        }
        public void DELTKFL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int ID = int.Parse(P1);
            SZHL_KS_TKFL type = new SZHL_KS_TKFLB().GetEntity(d => d.ID == ID && d.ComId == UserInfo.User.ComId);
            string typepath = type.TypePath == "" ? type.ID + "-" : type.TypePath + "-" + type.ID;
            if (new SZHL_KS_TKFLB().GetEntities(d => d.ComId == UserInfo.User.ComId).ToList().Where(d => (d.TypePath + "-").IndexOf(typepath) > -1).Count() > 0)
            {
                msg.ErrorMsg = "请先删除子分类";
            }
            else
            {
                type.ISDel = 1;
                new SZHL_KS_TKFLB().Update(type);
            }
        }
        public void GETKSGLTYPE(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);
            msg.Result = new SZHL_KS_TKFLB().GetEntity(d => d.ID == Id);
        }
        //获取题库分类的子项
        public void GETTKFLSEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            msg.Result = new SZHL_KS_TKFLB().GetEntities(d => d.ISDel == 0);

        }

        #endregion

        #region 题库管理
        public void GETTKGLMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);
            msg.Result = new SZHL_KS_TKB().GetEntity(d => d.ID == Id);
        }
        public void ADDTKGL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_KS_TK type = JsonConvert.DeserializeObject<SZHL_KS_TK>(P1);
            if (type != null)
            {
                if (type.ID == 0)
                {
                    type.CRDate = DateTime.Now;
                    type.CRUser = UserInfo.User.UserName;
                    type.ComId = UserInfo.User.ComId;
                    new SZHL_KS_TKB().Insert(type);

                }
                else
                {
                    new SZHL_KS_TKB().Update(type);
                }
            }
        }
        public void GETTKGLLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int page = 0;
            int.TryParse(context.Request.QueryString["p"] ?? "1", out page);//页码
            page = page == 0 ? 1 : page;
            int recordCount = 0;
            string strWhere = string.Format(" tk.ComId={0} ", UserInfo.User.ComId);
            if (P1 != "")
            {
                strWhere += string.Format(" and tk.TKTypeId={0}", P1);
            }
            string content = context.Request["Content"] ?? "";
            if (content != "")
            {
                strWhere += string.Format(" and tk.TKName like '%{0}%'", content);
            }
            int pagecount = 0;
            int.TryParse(context.Request.QueryString["pagecount"] ?? "1", out pagecount);//页码
            pagecount = pagecount == 0 ? 10 : pagecount;
            DataTable dt = new SZHL_GZBGB().GetDataPager("  SZHL_KS_TK tk inner join  SZHL_KS_TKFL tkfl on tk.TKTypeId=tkfl.ID ", " tk.*,tkfl.TKFLName   ", pagecount, page, "tk.CRDate desc", strWhere, ref recordCount);
            msg.Result = dt;
            msg.Result1 = recordCount;
        }
        public void DELETETKGL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int tkId = 0;
            int.TryParse(P1, out tkId);
            new SZHL_KS_TKB().Delete(d => d.ComId == UserInfo.User.ComId && d.ID == tkId);
            new SZHL_KS_STB().Delete(d => d.ComId == UserInfo.User.ComId && d.TKID == tkId);
        }
        //查询已发布的题库列表，切换题库
        public void GETTKGLLISTBYFL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int page = 0;
            int.TryParse(context.Request.QueryString["p"] ?? "1", out page);//页码
            page = page == 0 ? 1 : page;
            int recordCount = 0;
            string strWhere = string.Format(" tk.ComId={0} And tk.Status=1", UserInfo.User.ComId);
            if (P1 != "")
            {
                strWhere += string.Format(" and (tk.TKTypeId={0} or tkfl.TypePath+'-' like '{0}-%')", P1);
            }
            string content = context.Request["Content"] ?? "";
            if (content != "")
            {
                strWhere += string.Format(" and tk.TKName like '%{0}%'", content);
            }
            DataTable dt = new SZHL_GZBGB().GetDataPager("  SZHL_KS_TK tk inner join  SZHL_KS_TKFL tkfl on tk.TKTypeId=tkfl.ID ", " tk.*,tkfl.TKFLName   ", 8, page, "tk.CRDate desc", strWhere, ref recordCount);
            msg.Result = dt;
            msg.Result1 = recordCount;
        }
        //选择题使用的选择题库
        public void GETSELTKGLLISTBYFL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strWhere = "";
            if (!string.IsNullOrEmpty(P1))
            {
                strWhere = " and tk.TKTypeId=" + P1;
            }

            msg.Result = new SZHL_KS_TKB().GetDTByCommand(string.Format("select * from SZHL_KS_TK tk inner join  SZHL_KS_TKFL tkfl on tk.TKTypeId=tkfl.ID where tk.ComId={1} {0}  ", strWhere, UserInfo.User.ComId));
            msg.Result1 = new SZHL_KS_KSAPB().GetDTByCommand("SELECT* from SZHL_KS_KSAP WHERE ','+KSUser+',' LIKE '%" + UserInfo.User.UserName + "%' AND GETDATE() BETWEEN KSDate and  DATEADD(MINUTE,(KSSC+YCSY),KSDate)");

        }
        //分页题库列表
        public void GETSELTKGLLISTBYFLPAGE(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int page = 0;
            int.TryParse(context.Request.QueryString["p"] ?? "1", out page);//页码
            page = page == 0 ? 1 : page;
            int recordCount = 0;
            string strWhere = "tk.ComId=" + UserInfo.User.ComId;
            if (!string.IsNullOrEmpty(P1))
            {
                strWhere += " and tk.TKTypeId=" + P1;
            }
            DataTable dt = new SZHL_GZBGB().GetDataPager("  SZHL_KS_TK tk inner join  SZHL_KS_TKFL tkfl on tk.TKTypeId=tkfl.ID ", " tk.*,tkfl.TKFLName   ", 8, page, "tk.CRDate desc", strWhere, ref recordCount);
            msg.Result = dt;
            msg.Result2 = recordCount;
            msg.Result1 = new SZHL_KS_KSAPB().GetDTByCommand("SELECT* from SZHL_KS_KSAP WHERE ','+KSUser+',' LIKE '%" + UserInfo.User.UserName + "%' AND GETDATE() BETWEEN KSDate and  DATEADD(MINUTE,(KSSC+YCSY),KSDate)");
        }
        //获取知识点
        public void GETKNOWLEDGE(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strWhere = "";
            if (!string.IsNullOrEmpty(P1))
            {
                strWhere = "and st.TKID=" + P1;

            }
            if (!string.IsNullOrEmpty(P2))
            {
                strWhere = "and tk.TKTypeId=" + P2;
            }
            string strSql = string.Format("SELECT  DISTINCT KnowLedge FROM SZHL_KS_ST st  inner JOIN SZHL_KS_TK tk on st.TKID=tk.ID Where st.ComId={0} {1} and st.KnowLedge is not null and st.KnowLedge!=''", UserInfo.User.ComId, strWhere);
            msg.Result = new SZHL_KS_STB().GetDTByCommand(strSql);
        }
        #endregion

        #region 试题管理
        #region 获取题库的试题列表
        public void GETTKSTLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {

            string zjType = context.Request["zjType"] ?? "";
            string sjcount = context.Request["sjCount"] ?? "";
            string strCol = "";
            string strOrder = " st.ID ";
            if (zjType == "1" && !string.IsNullOrEmpty(sjcount))
            {

                strCol = " top " + sjcount + " ";
                strOrder = " NEWID()";
            }
            string strSql = string.Format("SELECT " + strCol + " st.* from SZHL_KS_ST st inner join SZHL_KS_TK  tk on st.TKID=tk.ID where ", UserInfo.User.ComId);

            string strWhere = string.Format(" st.ComId={0} ", UserInfo.User.ComId);
            string tkTypeId = context.Request["tktypeid"] ?? "";
            if (!string.IsNullOrEmpty(tkTypeId)) //题库类型
            {
                strWhere += string.Format(" And tk.TKTypeId={0} ", tkTypeId);
            }
            //else
            //{
            //    List<SZHL_KS_TKFL> allTypeList = new SZHL_KS_TKFLB().GetEntities(d => d.ComId == UserInfo.User.ComId && d.ISDel == 0).ToList();
            //    if (allTypeList.Count > 0)
            //    {
            //        string pIds = allTypeList.Select(d => d.PID.Value).Distinct().ToList().ListTOString(',');
            //        string strSqlTK = string.Format("SELECT * from SZHL_KS_TKFL where Id not in ({1}) and ComId={2} and isDel=0 order by  PID", UserInfo.User.UserName, pIds, UserInfo.User.ComId);
            //        DataTable dtType = new SZHL_KS_TKFLB().GetDTByCommand(strSqlTK);
            //        foreach (DataRow row in dtType.Rows)
            //        {
            //            string parentTypeName = allTypeList.Where(d => row["TypePath"].ToString().Split('-').Contains(d.ID.ToString()) && d.ComId == UserInfo.User.ComId).OrderBy(d => d.ID).Select(d => d.TKFLName).ToList<string>().ListTOString('-');
            //            row["TKFLName"] = parentTypeName + "-" + row["TKFLName"];
            //        }
            //        if (dtType != null && dtType.Rows.Count > 0)
            //        {
            //            strWhere += string.Format(" And tk.TKTypeId={0} ", dtType.Rows[0]["ID"]);
            //        }
            //    }
            //}
            string tkid = context.Request["tkid"] ?? "";
            if (!string.IsNullOrEmpty(tkid)) //题库ID
            {
                strWhere += string.Format(" And st.TKId={0} ", tkid);
            }

            //知识点
            if (!string.IsNullOrEmpty(P1))
            {
                strWhere += string.Format(" And st.Knowledge='{0}' ", P1);
            }
            //题型
            if (!string.IsNullOrEmpty(P2))
            {
                strWhere += string.Format("and  st.TYPE='{0}'", P2);
            }
            //难易程度
            string level = context.Request["level"] ?? "";
            if (!string.IsNullOrEmpty(level))
            {
                strWhere += string.Format("and  st.level={0}", level);
            }
            string strContent = context.Request["contnet"];
            if (!string.IsNullOrEmpty(strContent))
            {
                strWhere += string.Format("and  st.QContent like '%{0}%'", strContent);
            }
            int recordCount = 0;
            DataTable dt = null;
            strSql = strSql + strWhere + " ORDER BY " + strOrder;
            if (zjType == "1" && !string.IsNullOrEmpty(sjcount))
            {
                dt = new SZHL_KS_STB().GetDTByCommand(strSql);
                //recordCount = dt.Rows.Count;
            }
            else
            {
                int page = 0;
                int.TryParse(context.Request.QueryString["p"] ?? "1", out page);//页码
                page = page == 0 ? 1 : page;
                int pagecount = 0;
                int.TryParse(context.Request.QueryString["count"] ?? "1", out pagecount);//页码
                pagecount = pagecount == 0 ? 10 : pagecount;
                dt = new SZHL_KS_STB().GetDataPager(" SZHL_KS_ST st inner join SZHL_KS_TK  tk on st.TKID=tk.ID ", strCol + " st.* ", pagecount, page, strOrder, strWhere, ref recordCount);
            }
            if (dt.Rows.Count > 0)
            {
                string Ids = "";
                foreach (DataRow row in dt.Rows)
                {
                    Ids += row["ID"] + ",";
                }
                Ids = Ids.Substring(0, Ids.Length - 1);
                string strItemSql = string.Format("SELECT * from SZHL_KS_STItem where STID in ({0})", Ids);
                DataTable questionItem = new SZHL_KS_STItemB().GetDTByCommand(strItemSql);
                dt.Columns.Add("QItem", Type.GetType("System.Object"));
                foreach (DataRow row in dt.Rows)
                {
                    row["QItem"] = questionItem.FilterTable(" STID=" + row["ID"]);
                }
            }
            msg.Result = dt;
            msg.Result2 = recordCount;
            string sjID = context.Request["sjID"] ?? "";
            if (sjID != "")
            {
                string sql = string.Format(@"SELECT  STID ID,KnowLedge,STType Type,Level,QContent,QAnswer,QAnalyze from SZHL_KS_SJSTGL where SJID={0} and STType='{1}'", sjID, P2);
                DataTable dt3 = new SZHL_KS_SJSTB().GetDTByCommand(sql);
                dt3.Columns.Add("QItem", Type.GetType("System.Object"));
                if (dt3.Rows.Count > 0)
                {
                    string Ids = "";
                    foreach (DataRow row in dt3.Rows)
                    {
                        Ids += row["ID"] + ",";
                    }
                    Ids = Ids.Substring(0, Ids.Length - 1);
                    string strItemSql = string.Format("SELECT * from SZHL_KS_STItem where STID in ({0})", Ids);
                    DataTable questionItem = new SZHL_KS_STItemB().GetDTByCommand(strItemSql);
                    foreach (DataRow row in dt3.Rows)
                    {
                        row["QItem"] = questionItem.FilterTable(" STID=" + row["ID"]);
                    }
                }
                msg.Result1 = dt3;
            }
        }
        #endregion



        #region 试题导入

        public void IMPORTTKST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            HttpPostedFile _upfile = context.Request.Files["upFile"];
            if (_upfile != null)
            {
                int tkid = int.Parse(context.Request["tkid"] ?? "0");
                //替换原有WORD内部的特殊字符
                byte[] space = new byte[] { 0xc2, 0xa0 };
                string UTFSpace = Encoding.GetEncoding("UTF-8").GetString(space);
                //替换原有WORD内部的特殊字符
                string fileName = _upfile.FileName;
                _upfile.SaveAs(HttpContext.Current.Request.MapPath("~/ViewV5/upload/" + fileName));

                string URL = UserInfo.QYinfo.FileServerUrl + "fileupload?qycode=" + UserInfo.QYinfo.QYCode;
                string result = WortToHtml(DocSave(fileName, URL, UserInfo));
                SZHL_KS_ST item = null;
                string[] info = result.Split(new string[] { "[题型]" }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < info.Length; i++)
                {
                    string line = info[i];
                    try
                    {
                        item = new SZHL_KS_ST();
                        string type = line.Substring(0, line.IndexOf("[题文]"));
                        item.Type = type.Trim();
                        string anw = line.Substring(line.IndexOf("[答案]"));
                        item.QAnswer = anw.Substring(anw.IndexOf("]") + 1);
                        if (item.Type == "单选题" || item.Type == "多选题" || item.Type == "判断题")
                        {
                            item.QAnswer = item.QAnswer.Replace(UTFSpace, "&nbsp;").Replace(" ", "").Replace("<br /> ", "");
                        }
                        else
                        {

                            item.QAnswer = item.QAnswer.Replace(UTFSpace, "&nbsp;").Replace("<br /> ", "");
                        }
                        item.ComId = UserInfo.User.ComId;
                        item.TKID = tkid;
                        item.CRDate = DateTime.Now;
                        item.CRUser = UserInfo.User.UserName;
                        item.Level = int.Parse(P2);
                        item.KnowLedge = P1;
                        string title = "";
                        if (item.Type == "多选题" || item.Type == "单选题" || item.Type == "判断题")
                        {
                            title = line.Substring(line.IndexOf("[题文]"), line.IndexOf("[选项]") - line.IndexOf("[题文]"));
                        }
                        else
                        {
                            title = line.Substring(line.IndexOf("[题文]"), line.IndexOf("[答案]") - line.IndexOf("[题文]"));
                        }

                        item.QContent = title.Substring(title.IndexOf("]") + 1);


                        item.QContent = item.QContent.Replace(UTFSpace, "&nbsp;");
                        //判断已存在的题文不保存
                        if (new SZHL_KS_STB().GetEntities(d => d.ComId == UserInfo.User.ComId && d.QContent == item.QContent && d.TKID == tkid).Count() > 0)
                        {
                            continue;
                        }
                        //替换原有WORD内部的特殊字符

                        new SZHL_KS_STB().Insert(item);
                        if ("多选题".Equals(item.Type))
                        {
                            //[选项]A.0.5%    B.1.0%    C.2.0%
                            string change = line.Substring(line.IndexOf("[选项]"), line.IndexOf("[答案]") - line.IndexOf("[选项]"));
                            SaveOptions(change, item.ID, UserInfo.User.ComId.Value);

                        }
                        else if ("单选题".Equals(item.Type))
                        {

                            //[选项]A.0.5%    B.1.0%    C.2.0%
                            string change = line.Substring(line.IndexOf("[选项]"), line.IndexOf("[答案]") - line.IndexOf("[选项]"));
                            SaveOptions(change, item.ID, UserInfo.User.ComId.Value);
                        }
                        else if ("判断题".Equals(item.Type))
                        {

                            //[选项]A.0.5%    B.1.0%    C.2.0%
                            string change = line.Substring(line.IndexOf("[选项]"), line.IndexOf("[答案]") - line.IndexOf("[选项]"));
                            SaveOptions(change, item.ID, UserInfo.User.ComId.Value);
                        }
                    }
                    catch (Exception ex)
                    {
                        msg.ErrorMsg += "请检查第" + (i + 1) + "试题格式";
                    }
                }
            }
            else
            {
                msg.ErrorMsg = "请选择要导入试题的文件";
            }
        }
        #region 判断需要添加的选项

        /// <summary>
        /// 判断需要添加的选项
        /// </summary>
        /// <param name="test">所有选项内容</param>
        /// <param name="questionId">试题ID</param>
        public void SaveOptions(string test, int questionId, int ComId)
        {
            string itemDec = "";
            if (test.IndexOf("A.") > 0 && test.IndexOf("B.") > 0)
            {
                string changeA = test.Substring(test.IndexOf("A."), test.IndexOf("B.") - test.IndexOf("A."));
                itemDec = changeA.Substring(changeA.Split('.')[0].Length + 1);
                new SZHL_KS_STItemB().SaveQuestionItem(questionId, changeA.Split('.')[0], itemDec, ComId);

                if (test.IndexOf("B.") > 0 && test.IndexOf("C.") > 0)
                {
                    string changeB = test.Substring(test.IndexOf("B."), test.IndexOf("C.") - test.IndexOf("B."));
                    itemDec = changeB.Substring(changeB.Split('.')[0].Length + 1);
                    new SZHL_KS_STItemB().SaveQuestionItem(questionId, changeB.Split('.')[0], itemDec, ComId);

                    if (test.IndexOf("C.") > 0 && test.IndexOf("D.") > 0)
                    {
                        string changeC = test.Substring(test.IndexOf("C."), test.IndexOf("D.") - test.IndexOf("C."));
                        itemDec = changeC.Substring(changeC.Split('.')[0].Length + 1);
                        new SZHL_KS_STItemB().SaveQuestionItem(questionId, changeC.Split('.')[0], itemDec, ComId);
                        if (test.IndexOf("D.") > 0 && test.IndexOf("E.") > 0)
                        {
                            string changeD = test.Substring(test.IndexOf("D."), test.IndexOf("E.") - test.IndexOf("D."));
                            itemDec = changeD.Substring(changeD.Split('.')[0].Length + 1);
                            new SZHL_KS_STItemB().SaveQuestionItem(questionId, changeD.Split('.')[0], itemDec, ComId);
                            if (test.IndexOf("E.") > 0 && test.IndexOf("F.") > 0)
                            {
                                string changeE = test.Substring(test.IndexOf("D."), test.IndexOf("E.") - test.IndexOf("D."));
                                itemDec = changeE.Substring(changeE.Split('.')[0].Length + 1);
                                new SZHL_KS_STItemB().SaveQuestionItem(questionId, changeD.Split('.')[0], itemDec, ComId);
                            }
                            else
                            {
                                string changeE = test.Substring(test.IndexOf("E."));
                                itemDec = changeE.Substring(changeE.Split('.')[0].Length + 1);
                                new SZHL_KS_STItemB().SaveQuestionItem(questionId, changeE.Split('.')[0], itemDec, ComId);
                            }
                        }
                        else
                        {
                            string changeE = test.Substring(test.IndexOf("D."));
                            itemDec = changeE.Substring(changeE.Split('.')[0].Length + 1);
                            new SZHL_KS_STItemB().SaveQuestionItem(questionId, changeE.Split('.')[0], itemDec, ComId);

                        }

                    }
                    else
                    {
                        string changeC = test.Substring(test.IndexOf("C."));
                        itemDec = changeC.Substring(changeC.Split('.')[0].Length + 1);
                        new SZHL_KS_STItemB().SaveQuestionItem(questionId, changeC.Split('.')[0], itemDec, ComId);
                    }
                }
                else
                {
                    string changeB = test.Substring(test.IndexOf("B."));
                    itemDec = changeB.Substring(changeB.Split('.')[0].Length + 1);
                    new SZHL_KS_STItemB().SaveQuestionItem(questionId, changeB.Split('.')[0], itemDec, ComId);
                }


            }

        }
        #endregion
        #region 文档上传保存
        /// <summary>
        /// 试题模板及图片保存
        /// </summary>
        /// <param name="fileName">文档名称</param>
        /// <returns></returns>
        private string DocSave(string fileName, string toUploadUrl, JH_Auth_UserB.UserInfo UserInfo)
        {
            string Htmlstring = "";
            using (MemoryStream ms = new MemoryStream())
            {

                HtmlSaveOptions saveOptions = new HtmlSaveOptions();
                saveOptions.ImagesFolder = HttpContext.Current.Request.MapPath("~/ViewV5/upload/");
                saveOptions.ImagesFolderAlias = "/ViewV5/upload";
                saveOptions.ExportHeadersFootersMode = ExportHeadersFootersMode.None;
                Aspose.Words.Document doc = new Aspose.Words.Document(HttpContext.Current.Request.MapPath("~/ViewV5/upload/" + fileName));

                doc.Save(ms, saveOptions);
                Htmlstring = Encoding.UTF8.GetString(ms.ToArray());
                ms.Close(); ;
            }
            //正则表达式匹配图片地址上传到图片服务器
            Regex reg = new Regex("<img\\ssrc=\\\"(?<name>/upload/Aspose.Words.\\S*)\\\"");
            MatchCollection mColl = reg.Matches(Htmlstring);
            foreach (Match match in mColl)
            {
                string filePath = match.Groups["name"].Value;//本地图片位置 toUploadUrl 文件服务器位置
                //上传图片到文件服务器，返回MD5值
                string MD5 = CommonHelp.PostFile(toUploadUrl, HttpContext.Current.Request.MapPath(filePath));
                FT_File newfile = new FT_File();
                newfile.ComId = UserInfo.User.ComId;
                newfile.Name = Path.GetFileName(HttpContext.Current.Request.MapPath(filePath));
                newfile.FileMD5 = MD5.Replace("\"", "");
                newfile.FileSize = "0";
                newfile.FileVersin = 0;
                newfile.CRDate = DateTime.Now;
                newfile.CRUser = UserInfo.User.UserName;
                newfile.UPDDate = DateTime.Now;
                newfile.UPUser = UserInfo.User.UserName;
                newfile.FileExtendName = Path.GetExtension(HttpContext.Current.Request.MapPath(filePath)).Split('.')[1];
                newfile.FolderID = 3;
                newfile.ISYL = "Y";
                new FT_FileB().Insert(newfile);
                //新图片地址
                string newUrl = "/View/Common/DownLoadFile.aspx?fileId=" + newfile.ID;
                //替换图片地址
                Htmlstring = Htmlstring.Replace(filePath, newUrl);
                File.Delete(HttpContext.Current.Request.MapPath(filePath));
            }
            return Htmlstring;
        }

        #endregion
        #region Word文档转html
        /// <summary>
        /// 文档转换成Html
        /// </summary>
        /// <param name="Htmlstring">文档内容</param>
        /// <returns></returns>
        private string WortToHtml(string Htmlstring)
        {
            //script
            Htmlstring = Regex.Replace(Htmlstring, @"<script[^>]*>[\s\S]*?<\/[^>]*script>", "", RegexOptions.IgnoreCase);
            //style 
            Htmlstring = Regex.Replace(Htmlstring, @"<style[^>]*>[\s\S]*?<\/[^>]*style>", "", RegexOptions.IgnoreCase);

            Htmlstring = Regex.Replace(Htmlstring, @"<(?!(img|br)\s+)[^<>]*?>", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"([\r\n])[\s]+", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"-->", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"<!--.*", "", RegexOptions.IgnoreCase);

            Htmlstring = Regex.Replace(Htmlstring, @"&(quot|#34);", "\"", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(amp|#38);", "&", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(lt|#60|#x3C);", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(gt|#62);", ">", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(nbsp|#160|#xa0);", " ", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(iexcl|#161);", "\xa1", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(cent|#162);", "\xa2", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(pound|#163);", "\xa3", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(copy|#169);", "\xa9", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&#(\d+);", "", RegexOptions.IgnoreCase);

            Htmlstring.Replace("<", "");
            Htmlstring.Replace(">", "");
            Htmlstring = Htmlstring.Substring(Htmlstring.IndexOf("[题型]"));

            return Htmlstring;

        }
        #endregion
        #endregion

        //获取知识点
        public void ADDTKST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_KS_ST stmodel = JsonConvert.DeserializeObject<SZHL_KS_ST>(P1);
            if (stmodel.ID == 0)
            {
                stmodel.ComId = UserInfo.User.ComId;
                stmodel.CRUser = UserInfo.User.UserName;
                stmodel.CRDate = DateTime.Now;
                new SZHL_KS_STB().Insert(stmodel);
                string[] typeArray = new string[] { "单选题", "多选题", "判断题" };
                if (typeArray.Contains(stmodel.Type))
                {
                    List<SZHL_KS_STItem> item = JsonConvert.DeserializeObject<List<SZHL_KS_STItem>>(P2);
                    foreach (SZHL_KS_STItem q in item)
                    {
                        if (!string.IsNullOrEmpty(q.ItemDesc))
                        {
                            q.STID = stmodel.ID;
                            q.ComId = UserInfo.User.ComId;
                            new SZHL_KS_STItemB().Insert(q);
                        }
                    }
                }
            }
            else
            {

                string[] typeArray = new string[] { "单选题", "多选题", "判断题" };
                if (typeArray.Contains(stmodel.Type))
                {
                    new SZHL_KS_STItemB().Delete(d => d.STID == stmodel.ID);
                    List<SZHL_KS_STItem> item = JsonConvert.DeserializeObject<List<SZHL_KS_STItem>>(P2);
                    foreach (SZHL_KS_STItem q in item)
                    {
                        if (!string.IsNullOrEmpty(q.ItemDesc))
                        {
                            q.STID = stmodel.ID;
                            q.ComId = UserInfo.User.ComId;
                            new SZHL_KS_STItemB().Insert(q);
                        }
                    }
                }
                new SZHL_KS_STB().Update(stmodel);
            }
        }
        public void DELTKST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int stId = 0;
            int.TryParse(P1, out stId);
            if (new SZHL_KS_STB().Delete(d => d.ID == stId))
            {
                new SZHL_KS_STItemB().Delete(d => d.STID == stId);
            }
        }
        public void GETTKSTMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int strId = 0;
            int.TryParse(P1, out strId);
            SZHL_KS_ST st = new SZHL_KS_STB().GetEntity(d => d.ComId == UserInfo.User.ComId && d.ID == strId);
            msg.Result = st;
            msg.Result1 = new SZHL_KS_STItemB().GetEntities(d => d.ComId == UserInfo.User.ComId && d.STID == strId);
        }
        #endregion

        #region 考试安排
        public void GETKSAPMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int Id = int.Parse(P1);
            SZHL_KS_KSAP ksap = new SZHL_KS_KSAPB().GetEntity(d => d.ID == Id);
            string status = "0";
            if (ksap.KSDate < DateTime.Now && ksap.KSDate.Value.AddMinutes(ksap.YCSY.Value) > DateTime.Now)//考试进行中
            {
                status = "1";
            }
            else if (ksap.KSDate.Value.AddMinutes(ksap.YCSY.Value) < DateTime.Now && ksap.Status == 0)//考试已结束,阅卷未结束
            {

                status = "2";
            }
            else if (ksap.Status == 1)//阅卷完成
            {
                status = "3";
            }
            msg.Result = ksap;
            SZHL_KS_SJ sjmodel = new SZHL_KS_SJB().GetEntity(d => d.ID == ksap.SJID);
            msg.Result2 = sjmodel == null ? "" : sjmodel.SJName;
            msg.Result1 = status;

        }
        public void ADDKSAP(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_KS_KSAP ksap = JsonConvert.DeserializeObject<SZHL_KS_KSAP>(P1);

            if (ksap != null)
            {
                if (string.IsNullOrWhiteSpace(ksap.KSUser))
                {
                    msg.ErrorMsg = "请选择考试人员";
                    return;
                }
                if (string.IsNullOrWhiteSpace(ksap.YJTeacher))
                {
                    msg.ErrorMsg = "请选择阅卷老师";
                    return;
                }
                List<SZHL_KS_KSAP> ksapsj = new SZHL_KS_KSAPB().GetEntities(d => d.SJID == ksap.SJID && d.ID != ksap.ID).ToList();
                if (ksapsj.Count == 0)
                {
                    if (ksap.ID == 0)
                    {
                        ksap.CRDate = DateTime.Now;
                        ksap.CRUser = UserInfo.User.UserName;
                        ksap.ComId = UserInfo.User.ComId;
                        ksap.Status = 0;
                        new SZHL_KS_KSAPB().Insert(ksap);

                    }
                    else
                    {
                        new SZHL_KS_KSAPB().Update(ksap);
                    }
                }
                else
                {
                    msg.ErrorMsg = "该试卷已被使用";
                }
            }
        }
        public void GETKSAPLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int page = 0;
            int.TryParse(context.Request.QueryString["p"] ?? "1", out page);//页码
            page = page == 0 ? 1 : page;
            int recordCount = 0;
            string strWhere = string.Format(" ComId={0} ", UserInfo.User.ComId);
            string sqlWhere = " 1=1 ";
            string orderby = " CRDate desc";
            if (P1 != "")
            {
                switch (P1)
                {
                    case "1":
                        strWhere += string.Format(" and YJTeacher='{0}'", UserInfo.User.UserName);
                        break;
                    case "2":
                        strWhere += string.Format(" and Status=1 ");
                        break;
                }
            }
            string content = context.Request["Content"] ?? "";
            if (content != "")
            {
                strWhere += string.Format(" and KSName like '%{0}%'", content);
            }
            string myks = context.Request["myks"] ?? "";
            if (myks != "")
            {
                strWhere += string.Format(" AND ',' + KSUser + ',' LIKE '%{0}%'", UserInfo.User.UserName);
            }
            if (P2 != "")
            {
                sqlWhere += string.Format(" and kszt={0} ", P2);
                orderby = " kszt,KSDate ";
            }
            string sql = @"(SELECT
	*, CASE
WHEN KSDate > GETDATE() THEN
	2
WHEN DATEADD(
	MINUTE,
	ISNULL(KSSC, 0)+ISNULL(YCSY, 0),
	KSDate
) > GETDATE()
AND KSDate < GETDATE() THEN
	1
WHEN DATEADD(
	MINUTE,
	ISNULL(KSSC, 0)+ISNULL(YCSY, 0),
	KSDate
) < GETDATE() THEN
	3
END AS kszt
FROM
SZHL_KS_KSAP where " + strWhere + ") AS newksap";
            int pagecount = 0;
            int.TryParse(context.Request.QueryString["pagecount"] ?? "1", out pagecount);//页码
            pagecount = pagecount == 0 ? 10 : pagecount;
            DataTable dt = new SZHL_KS_KSAPB().GetDataPager(sql, " * ", pagecount, page, orderby, sqlWhere, ref recordCount);
            dt.Columns.Add("ISCY", Type.GetType("System.String"));
            dt.Columns.Add("ISKS", Type.GetType("System.String"));
            dt.Columns.Add("ISLSYJ", Type.GetType("System.String"));
            DataTable userks = new SZHL_KS_USERKSB().GetDTByCommand("select * from SZHL_KS_USERKS where CRUser='" + UserInfo.User.UserName + "'  And KSType=1");
            foreach (DataRow row in dt.Rows)
            {
                if (row["KSUser"] != null)
                {
                    string[] ksuser = row["KSUser"].ToString().Split(',');
                    if (ksuser.Contains(UserInfo.User.UserName))
                    {
                        row["ISCY"] = "1";
                    }
                    else
                    {
                        row["ISCY"] = "0";
                    }
                }
                else
                {
                    row["ISCY"] = "0";
                }
                row["ISKS"] = "0";
                DataTable dtuks = userks.FilterTable("KSAPID=" + row["ID"]);
                if (dtuks != null && dtuks.Rows.Count > 0)
                {
                    row["ISLSYJ"] = dtuks.Rows[0]["YJTeacher"];
                    row["ISKS"] = "1";
                    if (dtuks.Rows[0]["ISJJ"].ToString() == "1")
                    {
                        row["ISKS"] = "2";
                    }
                }
            }
            msg.Result = dt;
            msg.Result1 = recordCount;
        }

        public void DELKSAP(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                int ID = int.Parse(P1);
                new SZHL_KS_KSAPB().Delete(d => d.ComId == UserInfo.User.ComId && d.ID == ID);
            }
            catch (Exception ex)
            {
                msg.ErrorMsg = "删除失败";
            }

        }
        #endregion



        #region 试卷管理
        public void GETSJGLMODEL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int kcId = 0;
            int.TryParse(P2, out kcId);
            if (kcId == 0)
            {
                int Id = int.Parse(P1);
                msg.Result = new SZHL_KS_SJB().GetEntity(d => d.ID == Id);
            }
            else
            {

                msg.Result = new SZHL_KS_SJB().GetEntity(d => d.kcId == kcId);
            }
        }

        #region 获取已发布试卷
        public void GETSJFBLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string sql = string.Format(@"SELECT * FROM SZHL_KS_SJ WHERE ComId={0} and Status=1 And (kcId is null or kcId=0) AND (ID NOT IN(SELECT SJID from SZHL_KS_KSAP ))", UserInfo.User.ComId);
            if (P1 != "")
            {
                sql = string.Format(@"SELECT * FROM SZHL_KS_SJ WHERE ComId={0} and Status=1 And (kcId is null or kcId=0) AND (ID NOT IN(SELECT SJID from SZHL_KS_KSAP WHERE ID<>{1}))", UserInfo.User.ComId, P1);
            }
            msg.Result = new SZHL_KS_SJB().GetDTByCommand(sql);
        }
        #endregion
        //考试获取试卷
        public void GETSJGLMODELVIEW(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int sjID = 0, ksapID = 0;
            int.TryParse(P1, out sjID);
            int.TryParse(P2, out ksapID);
            if (P2 != "")
            {
                SZHL_KS_KSAP ksap = new SZHL_KS_KSAPB().GetEntity(d => d.ID == ksapID);
                if (ksap != null)
                {
                    sjID = ksap.SJID.Value;
                    if (ksap.KSDate > DateTime.Now)
                    {
                        msg.ErrorMsg = "考试未开始";
                    }
                    TimeSpan timespan = DateTime.Now - ksap.KSDate.Value;
                    if ((timespan.TotalSeconds - (ksap.KSSC + ksap.YCSY) * 60) > 0)
                    {
                        msg.ErrorMsg = "考试已结束";
                    }
                    msg.Result2 = (int)((ksap.KSSC + ksap.YCSY) * 60 - timespan.TotalSeconds);
                    msg.Result3 = ksap.KSSC + ksap.YCSY;
                }
            }
            //获取试卷信息
            DataTable dt = new SZHL_KS_SJB().GetDTByCommand(string.Format("SELECT sj.ID,sj.SJName,sj.TotalRecord,sj.SJDescribe,sj.PassRecord,sj.KSSC,COUNT(DISTINCT sjst.STType) DTCount,COUNT(DISTINCT sjst.STID) XTCount  from  SZHL_KS_SJ sj inner join SZHL_KS_SJSTGL sjst on  sj.ID=sjst.SJID where  sj.ID={0} and sj.ComId={1}  GROUP by sj.ID,sj.SJName,sj.TotalRecord,sj.SJDescribe,sj.PassRecord,sj.KSSC", sjID, UserInfo.User.ComId));
            dt.Columns.Add("TXType", Type.GetType("System.Object"));
            foreach (DataRow row in dt.Rows)
            {
                //获取试卷的题型列表 strIds 题库试题Id 
                DataTable dtType = new SZHL_KS_SJB().GetDTByCommand(@"SELECT  DISTINCT STType,sum(isnull(Record,0)) totalRecord,COUNT(ID) totalCount,stuff((select ','+cast( sjst.STID as varchar) from SZHL_KS_SJSTGL sjst where sjst.SJID=SZHL_KS_SJSTGL.SJID and sjst.STType=SZHL_KS_SJSTGL.STType for xml path('')),1,1,'') stIds
                                                                    from SZHL_KS_SJSTGL where SJID=" + row["ID"] + " GROUP by STType,SJID");
                dtType.Columns.Add("STList", Type.GetType("System.Object"));
                foreach (DataRow rowType in dtType.Rows)
                {
                    //获取试卷的题列表
                    DataTable dtST = new SZHL_KS_SJB().GetDTByCommand(@"SELECT st.ID,st.STID,st.STType,cast(st.QContent as VARCHAR(MAX)) QContent,COUNT(item.UserKSID) ksCount FROM  SZHL_KS_SJSTGL st LEFT  JOIN SZHL_KS_USERKSItem item on st.STID=item.STID and item.SJID=" + sjID + "  and item.CRUser='" + UserInfo.User.UserName + "' where  st.STID in (" + rowType["stIds"] + ") and st.SJID=" + sjID + " GROUP by st.ID,st.STID,st.STType,cast(st.QContent as VARCHAR(MAX)) ");

                    dtST.Columns.Add("QItem", Type.GetType("System.Object"));
                    dtST.Columns.Add("Answer", Type.GetType("System.String"));
                    string strItemSql = string.Format(@"SELECT item.*,ksitem.ID isselect from SZHL_KS_STItem item inner join SZHL_KS_SJST sjst on item.STID=sjst.STID LEFT join SZHL_KS_USERKSItem ksitem on item.STID=ksitem.STID and ksitem.SJID=" + sjID + "  AND item.ItemName=CAST( ksitem.Answer as VARCHAR(50)) and ksitem.CRUser='{0}' where item.STID in ({1}) and sjst.SJID={2}", UserInfo.User.UserName, rowType["stIds"], sjID);
                    strItemSql = string.Format(@"SELECT item.*,ksitem.ID isselect from SZHL_KS_SJSTGLItem item inner join SZHL_KS_SJSTGL sjst 
                                            on item.STID=sjst.STID and item.SJID=sjst.SJID LEFT join SZHL_KS_USERKSItem ksitem on item.STID=ksitem.STID and item.SJID=ksitem.SJID and ksitem.SJID={2}  
                                            AND item.ItemName=CAST( ksitem.Answer as VARCHAR(50)) and ksitem.CRUser='{0}' where item.STID in ({1}) and item.SJID={2}", UserInfo.User.UserName, rowType["stIds"], sjID);
                    string sql = string.Format("SELECT * FROM SZHL_KS_USERKSItem WHERE CRUser='{0}' AND STID in ({1}) AND SJID={2}", UserInfo.User.UserName, rowType["stIds"], sjID);
                    DataTable questionItem = new SZHL_KS_STItemB().GetDTByCommand(strItemSql);
                    DataTable dtuser = new SZHL_KS_USERKSItemB().GetDTByCommand(sql);
                    foreach (DataRow rowST in dtST.Rows)
                    {
                        rowST["QItem"] = questionItem.FilterTable(" STID=" + rowST["STID"]);
                        DataTable dtuser2 = dtuser.FilterTable(" STID=" + rowST["STID"]);
                        if (dtuser2 != null && dtuser2.Rows.Count > 0)
                        {
                            rowST["Answer"] = dtuser2.Rows[0]["Answer"];
                        }
                    }
                    rowType["STList"] = dtST;
                }
                row["TXType"] = dtType;
            }
            SZHL_KS_USERKS userks = new SZHL_KS_USERKSB().GetEntity(d => d.CRUser == UserInfo.User.UserName && d.ComId == UserInfo.User.ComId && d.KSAPID == ksapID && d.KSType == 1);
            msg.Result1 = userks;

            msg.Result = dt;
        }
        //课程考试
        public void GETKCSJMODELVIEW(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int kcId = 0;
            int.TryParse(P1, out kcId);

            //获取试卷信息
            DataTable dt = new SZHL_KS_SJB().GetDTByCommand(string.Format("SELECT sj.ID,sj.SJName,sj.TotalRecord,sj.SJDescribe,sj.PassRecord,sj.KSSC,COUNT(DISTINCT sjst.STType) DTCount,COUNT(DISTINCT sjst.STID) XTCount  from  SZHL_KS_SJ sj inner join SZHL_KS_SJSTGL sjst on  sj.ID=sjst.SJID where  sj.kcId={0} and sj.ComId={1}  GROUP by sj.ID,sj.SJName,sj.TotalRecord,sj.SJDescribe,sj.PassRecord,sj.KSSC", kcId, UserInfo.User.ComId));
            if (dt.Rows.Count > 0)
            {
                int sjID = int.Parse(dt.Rows[0]["ID"].ToString());
                dt.Columns.Add("TXType", Type.GetType("System.Object"));
                foreach (DataRow row in dt.Rows)
                {
                    //获取试卷的题型列表 strIds 题库试题Id 
                    DataTable dtType = new SZHL_KS_SJB().GetDTByCommand(@"SELECT  DISTINCT STType,sum(isnull(Record,0)) totalRecord,COUNT(ID) totalCount,stuff((select ','+cast( sjst.STID as varchar) from SZHL_KS_SJSTGL sjst where sjst.SJID=SZHL_KS_SJSTGL.SJID and sjst.STType=SZHL_KS_SJSTGL.STType for xml path('')),1,1,'') stIds
                                                                    from SZHL_KS_SJSTGL where SJID=" + row["ID"] + " GROUP by STType,SJID");
                    dtType.Columns.Add("STList", Type.GetType("System.Object"));
                    foreach (DataRow rowType in dtType.Rows)
                    {
                        //获取试卷的题列表
                        DataTable dtST = new SZHL_KS_SJB().GetDTByCommand(@"SELECT st.ID,st.STID,st.STType,cast(st.QContent as VARCHAR(MAX)) QContent,COUNT(item.UserKSID) ksCount FROM  SZHL_KS_SJSTGL st LEFT JOIN SZHL_KS_USERKS userks on  userks.SJID=st.SJID AND userks.ISJJ!=1
                                                                            LEFT  JOIN SZHL_KS_USERKSItem item on userks.ID=item.UserKSID and st.STID=item.STID and item.SJID=st.SJID  and item.CRUser='" + UserInfo.User.UserName + "' where  st.STID in (" + rowType["stIds"] + ") and st.SJID=" + sjID + @" 
                                                                            GROUP by st.ID,st.STID,st.STType,cast(st.QContent as VARCHAR(MAX)) ");

                        dtST.Columns.Add("QItem", Type.GetType("System.Object"));
                        dtST.Columns.Add("Answer", Type.GetType("System.String"));
                        string strItemSql = string.Format(@"SELECT item.*,ksitem.ID isselect from SZHL_KS_STItem item inner join SZHL_KS_SJST sjst on item.STID=sjst.STID LEFT join SZHL_KS_USERKSItem ksitem on item.STID=ksitem.STID and ksitem.SJID=" + sjID + "  AND item.ItemName=CAST( ksitem.Answer as VARCHAR(50)) and ksitem.CRUser='{0}' where item.STID in ({1}) and sjst.SJID={2}", UserInfo.User.UserName, rowType["stIds"], sjID);
                        strItemSql = string.Format(@"SELECT item.*,ksitem.ID isselect from SZHL_KS_SJSTGLItem item inner join SZHL_KS_SJSTGL sjst 
                                            on item.STID=sjst.STID and item.SJID=sjst.SJID LEFT JOIN SZHL_KS_USERKS userks on item.SJID=userks.SJID and userks.ISJJ!=1 LEFT join SZHL_KS_USERKSItem ksitem on  ksitem.UserKSID=userks.ID and item.STID=ksitem.STID and item.SJID=ksitem.SJID 
                                            AND item.ItemName=CAST( ksitem.Answer as VARCHAR(50)) and ksitem.CRUser='{0}' where item.STID in ({1}) and item.SJID={2}", UserInfo.User.UserName, rowType["stIds"], sjID);
                        string sql = string.Format("SELECT * FROM SZHL_KS_USERKSItem WHERE CRUser='{0}' AND STID in ({1}) AND SJID={2}", UserInfo.User.UserName, rowType["stIds"], sjID);
                        DataTable questionItem = new SZHL_KS_STItemB().GetDTByCommand(strItemSql);
                        DataTable dtuser = new SZHL_KS_USERKSItemB().GetDTByCommand(sql);
                        foreach (DataRow rowST in dtST.Rows)
                        {
                            rowST["QItem"] = questionItem.FilterTable(" STID=" + rowST["STID"]);
                            DataTable dtuser2 = dtuser.FilterTable(" STID=" + rowST["STID"]);
                            if (dtuser2 != null && dtuser2.Rows.Count > 0)
                            {
                                rowST["Answer"] = dtuser2.Rows[0]["Answer"];
                            }
                        }
                        rowType["STList"] = dtST;
                    }
                    row["TXType"] = dtType;
                }
                SZHL_KS_USERKS userks = new SZHL_KS_USERKSB().GetEntity(d => d.CRUser == UserInfo.User.UserName && d.ComId == UserInfo.User.ComId && d.KSAPID == kcId && d.KSType == 2 && d.ISJJ != 1);
                msg.Result1 = userks;
                msg.Result = dt;
            }
            else
            {
                msg.ErrorMsg = "请确认该课程设置过试卷";
            }
        }
        //课程考试记录
        public void GETUSERKSLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strSql = string.Format("SELECT userks.*,sj.SJName from SZHL_KS_USERKS userks INNER JOIN  SZHL_KS_SJ sj on userks.SJID=sj.ID where KSType=2 and KSAPID=" + P1 + " AND userks.CRUser='" + UserInfo.User.UserName + "'");
            msg.Result = new SZHL_KS_USERKSB().GetDTByCommand(strSql);
        }
        //手动打分
        public void GETSJGLDFVIEW(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strSql = string.Format("SELECT * from SZHL_KS_SJSTGL  where SJID={0} and STType='{1}'", P1, P2);
            DataTable dt = new SZHL_KS_STB().GetDTByCommand(strSql);
            if (dt.Rows.Count > 0)
            {
                string Ids = "";
                foreach (DataRow row in dt.Rows)
                {
                    Ids += row["STID"] + ",";
                }
                Ids = Ids.Substring(0, Ids.Length - 1);
                string strItemSql = string.Format("SELECT * from SZHL_KS_SJSTGLItem where STID in ({0}) and SJID={1}", Ids, P1);
                DataTable questionItem = new SZHL_KS_SJSTGLItemB().GetDTByCommand(strItemSql);
                dt.Columns.Add("QItem", Type.GetType("System.Object"));
                foreach (DataRow row in dt.Rows)
                {
                    row["QItem"] = questionItem.FilterTable(" STID=" + row["STID"]);
                }
            }
            msg.Result = dt;
        }
        public void SJAUTODF(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int sjId = 0;
            int.TryParse(P1, out sjId);
            int record = 0;
            int.TryParse(context.Request["record"] ?? "0", out record);
            if (record == 0)
            {
                msg.Result = "请输入正确的分数";
                return;
            }
            List<SZHL_KS_SJSTGL> sjstlist = new SZHL_KS_SJSTGLB().GetEntities(d => d.SJID == sjId && d.STType == P2 && d.ComId == UserInfo.User.ComId).ToList();
            if (sjstlist.Count() > 0)
            {
                foreach (SZHL_KS_SJSTGL sjst in sjstlist)
                {
                    sjst.Record = record;
                    new SZHL_KS_SJSTGLB().Update(sjst);
                }
            }
        }
        public void ADDSJGL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_KS_SJ sjgl = JsonConvert.DeserializeObject<SZHL_KS_SJ>(P1);
            if (sjgl != null)
            {
                if (sjgl.ID == 0)
                {
                    sjgl.CRDate = DateTime.Now;
                    sjgl.CRUser = UserInfo.User.UserName;
                    sjgl.ComId = UserInfo.User.ComId;

                    new SZHL_KS_SJB().Insert(sjgl);

                }
                else
                {
                    if (new SZHL_KS_SJB().GetEntity(d => d.ID == sjgl.ID).Status == 1)
                    {
                        msg.ErrorMsg = "试卷已发布不能修改";
                        return;
                    }
                    new SZHL_KS_SJB().Update(sjgl);
                }
                msg.Result = sjgl;
            }
        }
        //获取试卷列表
        public void GETSJGLLIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int page = 0;
            int.TryParse(context.Request.QueryString["p"] ?? "1", out page);//页码
            page = page == 0 ? 1 : page;
            int recordCount = 0;
            string strWhere = string.Format(" ComId={0} ANd  (kcId is null or kcId=0) ", UserInfo.User.ComId);

            string content = context.Request["Content"] ?? "";
            if (content != "")
            {
                strWhere += string.Format(" and SJName like '%{0}%'", content);
            }

            string sjzt = context.Request["sjzt"] ?? "";//试卷状态 
            if (sjzt != "")
            {
                strWhere += string.Format(" and Status ={0} ", sjzt);
            }
            int pagecount = 0;
            int.TryParse(context.Request.QueryString["pagecount"] ?? "1", out pagecount);//页码
            pagecount = pagecount == 0 ? 10 : pagecount;
            DataTable dt = new SZHL_KS_SJB().GetDataPager("  SZHL_KS_SJ ", " *  ", pagecount, page, "CRDate desc", strWhere, ref recordCount);
            msg.Result = dt;
            msg.Result1 = recordCount;
        }
        public void DELSJGL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            try
            {
                int ID = int.Parse(P1);
                SZHL_KS_SJ sj = new SZHL_KS_SJB().GetEntity(d => d.ID == ID);
                if (sj != null && sj.Status == 1)
                {
                    msg.ErrorMsg = "此试卷已经发布，不能删除";
                }
                else
                {
                    new SZHL_KS_SJB().Delete(d => d.ComId == UserInfo.User.ComId && d.ID == ID);
                }
            }
            catch (Exception ex)
            {
                msg.ErrorMsg = "删除失败";
            }

        }
        //试卷添加试题
        public void ADDSJST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int sjId = 0;
            int.TryParse(P1, out sjId);
            if (P2 != "")
            {
                string type = context.Request["type"] ?? "";
                string strDelSql = string.Format(@"DELETE from SZHL_KS_SJSTGLItem where STID in (
                                                    SELECT STID from SZHL_KS_SJSTGL where SJID={0} and STType='{1}' )  and SJID={0} ;
                                                    DELETE from  SZHL_KS_SJSTGL where SJID={0} and STType='{1}'", sjId, type);
                //添加试题
                string strSql = string.Format(@"insert into  SZHL_KS_SJSTGL  (SJID,STID,KnowLedge,STType,Level,QContent,QAnswer,CRUser,CRDate,ComId)
                                                SELECT  {0},ID,KnowLedge,Type,Level,QContent,QAnswer,{1},GETDATE(),ComId from  SZHL_KS_ST  where  ID in ({2})", P1, UserInfo.User.UserName, P2);
                //添加试题选项
                strSql += string.Format(@"insert into  SZHL_KS_SJSTGLItem  (ItemName,ItemDesc,ComId,STID,SJID)
                                        SELECT ItemName,ItemDesc,ComId,STID,{0}  from  SZHL_KS_STItem  where  STID in ({1})", P1, P2);
                new SZHL_KS_SJSTGLB().ExsSql(strDelSql + strSql);
            }
        }
        public void GETSJTYPELIST(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strSql = string.Format(@"SELECT DISTINCT zidian.TypeName,COUNT(sjst.ID) STCount,sum(isnull(sjst.Record,0)) TotalRecord from JH_Auth_ZiDian zidian 
                                            LEFT join SZHL_KS_SJSTGL sjst on  zidian.TypeName=sjst.STType and  sjst.SJID={0}  and sjst.ComId={1} 
                                            where Class=22 {2} GROUP by zidian.TypeName", P1, UserInfo.User.ComId, P2 == "" ? "" : " And Remark1=1 ");
            msg.Result = new SZHL_KS_SJSTB().GetDTByCommand(strSql);
        }
        public void ADDSJRECORD(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string[] recordStr = P1.Split(',');
            foreach (string str in recordStr)
            {
                int Id = 0;
                int.TryParse(str.Split(':')[0], out Id);
                SZHL_KS_SJSTGL sjst = new SZHL_KS_SJSTGLB().GetEntity(d => d.ID == Id);
                if (sjst != null)
                {
                    sjst.Record = decimal.Parse(str.Split(':')[1]);
                    new SZHL_KS_SJSTGLB().Update(sjst);
                }
            }
        }
        #endregion

        #region 我的考试
        /// <summary>
        /// 我的考试
        ///</summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETMYKSDATA(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string strWhere = string.Format(" where ComId={0} AND (','+KSUser+',' like '%{1}%' OR ISNULL(KSUser,'') = '') AND dateadd(minute,KSSC+YCSY,KSDate) > GETDATE()", UserInfo.User.ComId, UserInfo.User.UserName);
            DataTable dt = new SZHL_KS_KSAPB().GetDTByCommand(@"select *, CASE
WHEN KSDate > GETDATE() THEN
	2
WHEN DATEADD(
	MINUTE,
	ISNULL(KSSC, 0)+ISNULL(YCSY, 0),
	KSDate
) > GETDATE()
AND KSDate < GETDATE() THEN
	1
WHEN DATEADD(
	MINUTE,
	ISNULL(KSSC, 0)+ISNULL(YCSY, 0),
	KSDate
) < GETDATE() THEN
	3
END AS kszt,dateadd(minute,ISNULL(KSSC,0),KSDate) AS ksEND from SZHL_KS_KSAP " + strWhere + " ORDER BY KSDate");
            dt.Columns.Add("ISCY", Type.GetType("System.String"));
            dt.Columns.Add("ISKS", Type.GetType("System.String"));
            dt.Columns.Add("ISLSYJ", Type.GetType("System.String"));
            DataTable userks = new SZHL_KS_USERKSB().GetDTByCommand("select * from SZHL_KS_USERKS where CRUser='" + UserInfo.User.UserName + "' And KSType=1");
            foreach (DataRow row in dt.Rows)
            {
                if (row["KSUser"] != null)
                {
                    string[] ksuser = row["KSUser"].ToString().Split(',');
                    if (ksuser.Contains(UserInfo.User.UserName))
                    {
                        row["ISCY"] = "1";
                    }
                    else
                    {
                        row["ISCY"] = "0";
                    }
                }
                else
                {
                    row["ISCY"] = "0";
                }
                row["ISKS"] = "0";
                DataTable dtuks = userks.FilterTable("KSAPID=" + row["ID"]);
                if (dtuks != null && dtuks.Rows.Count > 0)
                {
                    row["ISLSYJ"] = dtuks.Rows[0]["YJTeacher"];
                    row["ISKS"] = "1";
                    if (dtuks.Rows[0]["ISJJ"].ToString() == "1")
                    {
                        row["ISKS"] = "2";
                    }
                }
            }
            msg.Result = dt;
        }
        public void ADDUSERKS(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string type = context.Request["stType"] ?? "";
            SZHL_KS_USERKS userKs = JsonConvert.DeserializeObject<SZHL_KS_USERKS>(P1);
            SZHL_KS_USERKSItem userKSItem = JsonConvert.DeserializeObject<SZHL_KS_USERKSItem>(P2);
            if (userKs != null && userKs.ID == 0)
            {
                userKs.SJID = userKSItem.SJID;
                userKs.CRUser = UserInfo.User.UserName;
                userKs.CRDate = DateTime.Now;
                userKs.ISJJ = 0;
                userKs.ComId = UserInfo.User.ComId;
                new SZHL_KS_USERKSB().Insert(userKs);
            }
            int isExists = 0;
            if (new SZHL_KS_USERKSItemB().GetEntities(d => d.SJID == userKSItem.SJID && d.ComId == UserInfo.User.ComId && d.CRUser == UserInfo.User.UserName && d.STID == userKSItem.STID && d.UserKSID == userKs.ID).Count() > 0)
            {
                isExists = 1;
                if (type != "多选题")
                {
                    new SZHL_KS_USERKSItemB().Delete(d => d.CRUser == UserInfo.User.UserName && d.SJID == userKSItem.SJID && d.STID == userKSItem.STID && d.UserKSID == userKs.ID);

                }
            }

            userKSItem.UserKSID = userKs.ID;
            userKSItem.CRUser = UserInfo.User.UserName;
            userKSItem.ComId = UserInfo.User.ComId;
            userKSItem.CRDate = DateTime.Now;
            new SZHL_KS_USERKSItemB().Insert(userKSItem);
            msg.Result = userKs;
            msg.Result1 = userKSItem;
            msg.Result2 = isExists;
        }
        //删除考试选项
        public void DELKSITEM(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            SZHL_KS_USERKSItem userKSItem = JsonConvert.DeserializeObject<SZHL_KS_USERKSItem>(P1);
            if (new SZHL_KS_USERKSItemB().GetEntities(d => d.SJID == userKSItem.SJID && d.ComId == UserInfo.User.ComId && d.CRUser == UserInfo.User.UserName && d.STID == userKSItem.STID && d.UserKSID == userKSItem.UserKSID).Count() > 0)
            {
                new SZHL_KS_USERKSItemB().Delete(d => d.CRUser == UserInfo.User.UserName && d.SJID == userKSItem.SJID && d.STID == userKSItem.STID && d.Answer == userKSItem.Answer && d.UserKSID == userKSItem.UserKSID);
            }
        }
        public void SUBMITSJ(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int USERKSid = 0;
            int.TryParse(P1, out USERKSid);
            SZHL_KS_USERKS userKs = new SZHL_KS_USERKSB().GetEntity(d => d.ID == USERKSid);
            if (userKs != null && userKs.ID != 0 && userKs.ISJJ != 1)
            {
                string sql = string.Format(@"UPDATE SZHL_KS_USERKSItem
SET Record = ISNULL(
	(
		SELECT
			Record
		FROM
			SZHL_KS_SJSTGL
		WHERE
			SJID = SZHL_KS_USERKSItem.SJID
		AND STType IN ('单选题', '判断题')
		AND LTrim(RTrim(QAnswer)) = LTrim(
			RTrim(SZHL_KS_USERKSItem.Answer)
		)
		AND STID = SZHL_KS_USERKSItem.STID
        AND ComId = SZHL_KS_USERKSItem.ComId
	),
	0
)
WHERE
	UserKSID = {0}
AND SJID = {1}
AND CRUser = '{2}';", userKs.ID, userKs.SJID, UserInfo.User.UserName);
                new SZHL_KS_USERKSItemB().ExsSql(sql);
                #region 多选题计算分数
                string strSQL = "";
                DataTable dtDX = new SZHL_KS_SJSTB().GetDTByCommand("SELECT * FROM SZHL_KS_SJSTGL where SJID=" + userKs.SJID + "  AND STType='多选题'");
                foreach (DataRow row in dtDX.Rows)
                {
                    int stId = int.Parse(row["STID"].ToString());
                    List<SZHL_KS_USERKSItem> ksitem = new SZHL_KS_USERKSItemB().GetEntities(d => d.STID == stId && d.UserKSID == userKs.ID).ToList();
                    string QAnswer = row["QAnswer"].ToString().Trim().Replace("<br/>", "").Replace("<br />", "").Replace("&nbsp;", "");
                    bool flag = true;
                    if (ksitem.Count == QAnswer.Length)
                    {
                        foreach (SZHL_KS_USERKSItem item in ksitem)
                        {
                            if (QAnswer.IndexOf(item.Answer) < 0)
                            {
                                flag = false;
                            }
                        }
                    }
                    else
                    {
                        flag = false;
                    }
                    if (flag)
                    {
                        strSQL += string.Format("update SZHL_KS_USERKSItem set Record={0} where ID=(SELECT TOP 1 ID from SZHL_KS_USERKSItem WHERE  UserKSID={1} and STID={2});", row["Record"].ToString(), userKs.ID, stId);
                    }
                }
                if (!string.IsNullOrWhiteSpace(strSQL))
                {

                    new SZHL_KS_USERKSItemB().ExsSql(strSQL);
                }
                #endregion
                string sqlitem = string.Format("SELECT SUM(Record) FROM (SELECT Record FROM SZHL_KS_USERKSItem WHERE UserKSID = {0} AND SJID = {1} AND CRUser = '{2}' GROUP BY STID,Record) Total", userKs.ID, userKs.SJID, UserInfo.User.UserName);
                object record = new SZHL_KS_USERKSItemB().ExsSclarSql(sqlitem);
                decimal total = 0;
                decimal.TryParse(record.ToString(), out total);
                userKs.CRDate = DateTime.Now;
                userKs.ISJJ = 1;
                userKs.Record = total;
                new SZHL_KS_USERKSB().Update(userKs);
            }
            msg.Result = userKs;
        }
        #endregion

        #region 阅卷打分提交算总分

        public void YJENDTJ(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            List<StRecord> stfenshuList = JsonConvert.DeserializeObject<List<StRecord>>(P2);

            int USERKSid = 0;
            int.TryParse(P1, out USERKSid);
            SZHL_KS_USERKS userKs = new SZHL_KS_USERKSB().GetEntity(d => d.ID == USERKSid);
            if (userKs != null && userKs.ID != 0)
            {
                string sql = "";
                for (int i = 0; i < stfenshuList.Count; i++)
                {
                    sql += string.Format("UPDATE SZHL_KS_USERKSItem SET Record={0} WHERE UserKSID={1} AND STID={2} AND SJID={3} AND CRUser={4} AND ComId={5} ;", stfenshuList[i].Record, userKs.ID, stfenshuList[i].STID, userKs.SJID, userKs.CRUser, UserInfo.User.ComId);
                }
                new SZHL_KS_USERKSItemB().ExsSql(sql);
                msg.Result2 = sql;

                string sqlitem = string.Format("SELECT SUM(Record) FROM (SELECT Record FROM SZHL_KS_USERKSItem WHERE UserKSID = {0} AND SJID = {1} AND CRUser = '{2}' GROUP BY STID,Record) Total", userKs.ID, userKs.SJID, userKs.CRUser);
                object record = new SZHL_KS_USERKSItemB().ExsSclarSql(sqlitem);
                decimal total = 0;
                decimal.TryParse(record.ToString(), out total);
                userKs.Record = total;
                userKs.YJTeacher = UserInfo.User.UserName;
                new SZHL_KS_USERKSB().Update(userKs);
            }
            msg.Result = userKs;
        }
        #endregion

        #region 试卷发布
        /// <summary>
        /// 试卷发布
        ///</summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void KSRELEASE(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int sjID = 0;
            if (int.TryParse(P1, out sjID))
            {
                SZHL_KS_SJ kssj = new SZHL_KS_SJB().GetEntity(d => d.ID == sjID);
                if (kssj == null)
                {
                    msg.ErrorMsg = "试卷信息错误";
                }
                decimal sumrec = new SZHL_KS_SJSTGLB().GetEntities(d => d.SJID == sjID && d.ComId == UserInfo.User.ComId).Sum(d => d.Record).Value;
                if (Convert.ToDecimal(kssj.TotalRecord) > sumrec)
                {
                    msg.ErrorMsg = "已选择试题总分数小于试卷总分";
                }
                else if (Convert.ToDecimal(kssj.TotalRecord) < sumrec)
                {
                    msg.ErrorMsg = "已选择试题总分数大于试卷总分";
                }
                else if (Convert.ToDecimal(kssj.TotalRecord) == sumrec)
                {
                    kssj.Status = 1;
                    new SZHL_KS_SJB().Update(kssj);
                    msg.Result = kssj;
                }
            }
            else
            {
                msg.ErrorMsg = "试卷信息错误";
            }
        }
        #endregion

        #region 获取考试安排的考试人员
        /// <summary>
        /// 获取考试人员
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETKSUSER(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int page = 0;
            int.TryParse(context.Request.QueryString["p"] ?? "1", out page);//页码
            page = page == 0 ? 1 : page;
            int recordCount = 0;
            string strWhere = string.Format(" ku.ComId={0} ", UserInfo.User.ComId);

            string content = context.Request["Content"] ?? "";
            if (content != "")
            {
                strWhere += string.Format(" and ku.CRUser like '%{0}%'", content);
            }
            if (P1 != "")
            {
                int ksapid = 0;
                int.TryParse(P1, out ksapid);
                strWhere += string.Format(" and ku.KSAPID ={0} ", P1);
                DataTable dt = new SZHL_KS_SJB().GetDataPager("  SZHL_KS_USERKS ku LEFT JOIN SZHL_KS_SJ ksj ON ku.SJID=ksj.ID ", " ku.*,ksj.TotalRecord,ksj.PassRecord ", 8, page, " ku.CRDate ", strWhere, ref recordCount);
                msg.Result = dt;
                msg.Result1 = recordCount;
            }
        }

        /// <summary>
        /// 试卷打分
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void YJSJDF(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            string ST = context.Request["STID"] ?? "";
            decimal record = 0;
            decimal.TryParse(P1, out record);
            int STID = 0, USERKS = 0;
            int.TryParse(P2, out USERKS);
            int.TryParse(ST, out STID);

            SZHL_KS_USERKS ksuser = new SZHL_KS_USERKSB().GetEntity(d => d.ID == USERKS);
            if (ksuser == null)
            {
                return;
            }
            decimal strecord = new SZHL_KS_SJSTGLB().GetEntity(d => d.STID == STID && d.SJID == ksuser.SJID).Record.Value;
            if (record > strecord)
            {
                record = strecord;
            }
            string sql = string.Format("UPDATE SZHL_KS_USERKSItem SET Record={0} WHERE UserKSID={1} AND STID={2} AND SJID={3} AND CRUser={4} AND ComId={5}", record, ksuser.ID, STID, ksuser.SJID, ksuser.CRUser, UserInfo.User.ComId);
            new SZHL_KS_USERKSItemB().ExsSql(sql);
        }

        #region 阅卷获取人员填写答案
        /// <summary>
        /// 阅卷获取人员填写答案
        /// </summary>
        /// <param name="context"></param>
        /// <param name="msg"></param>
        /// <param name="P1"></param>
        /// <param name="P2"></param>
        /// <param name="UserInfo"></param>
        public void GETYJVIEW(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int sjID = 0, ksapID = 0;
            int.TryParse(P2, out ksapID);
            SZHL_KS_KSAP ksap = new SZHL_KS_KSAPB().GetEntity(d => d.ID == ksapID);
            if (ksap != null)
            {
                sjID = ksap.SJID.Value;
            }
            //获取试卷信息
            DataTable dt = new SZHL_KS_SJB().GetDTByCommand(string.Format("SELECT sj.ID,sj.SJName,sj.TotalRecord,sj.SJDescribe,sj.PassRecord,sj.KSSC,COUNT(DISTINCT sjst.STType) DTCount,COUNT(DISTINCT sjst.STID) XTCount from  SZHL_KS_SJ sj inner join SZHL_KS_SJSTGL sjst on  sj.ID=sjst.SJID where  sj.ID={0} and sj.ComId={1}  GROUP by sj.ID,sj.SJName,sj.TotalRecord,sj.SJDescribe,sj.PassRecord,sj.KSSC ", sjID, UserInfo.User.ComId));
            dt.Columns.Add("TXType", Type.GetType("System.Object"));
            foreach (DataRow row in dt.Rows)
            {
                //获取试卷的题型列表 strIds 题库试题Id 
                DataTable dtType = new SZHL_KS_SJB().GetDTByCommand(@"SELECT  DISTINCT STType,sum(isnull(Record,0)) totalRecord,COUNT(ID) totalCount,stuff((select ','+cast( sjst.STID as varchar) from SZHL_KS_SJSTGL sjst where sjst.SJID=SZHL_KS_SJSTGL.SJID and sjst.STType=SZHL_KS_SJSTGL.STType for xml path('')),1,1,'') stIds
                                                                    from SZHL_KS_SJSTGL where SJID=" + row["ID"] + " GROUP by STType,SJID");
                dtType.Columns.Add("STList", Type.GetType("System.Object"));
                foreach (DataRow rowType in dtType.Rows)
                {
                    //获取试卷的题列表
                    DataTable dtST = new SZHL_KS_SJB().GetDTByCommand(@"SELECT st.ID,st.STID,st.STType,cast(st.QContent as VARCHAR(MAX)) QContent,COUNT(item.UserKSID) ksCount,st.QAnswer,st.Record as stRecord  FROM  SZHL_KS_SJSTGL st LEFT  JOIN SZHL_KS_USERKSItem item on st.STID=item.STID and item.SJID=" + sjID + "  and item.CRUser='" + P1 + "' where  st.STID in (" + rowType["stIds"] + ") and st.SJID=" + sjID + " GROUP by st.ID,st.STID,st.STType,cast(st.QContent as VARCHAR(MAX)),st.QAnswer,st.Record ");

                    dtST.Columns.Add("QItem", Type.GetType("System.Object"));
                    dtST.Columns.Add("Answer", Type.GetType("System.String"));
                    dtST.Columns.Add("Record", Type.GetType("System.String"));
                    string strItemSql = string.Format(@"SELECT item.*,ksitem.ID isselect from SZHL_KS_STItem item inner join SZHL_KS_SJST sjst on item.STID=sjst.STID LEFT join SZHL_KS_USERKSItem ksitem on item.STID=ksitem.STID and ksitem.SJID=" + sjID + "  AND item.ItemName=CAST( ksitem.Answer as VARCHAR(50)) and ksitem.CRUser='{0}' where item.STID in ({1}) and sjst.SJID={2}", P1, rowType["stIds"], sjID);
                    strItemSql = string.Format(@"SELECT item.*,ksitem.ID isselect from SZHL_KS_SJSTGLItem item inner join SZHL_KS_SJSTGL sjst 
                                            on item.STID=sjst.STID and item.SJID=sjst.SJID LEFT join SZHL_KS_USERKSItem ksitem on item.STID=ksitem.STID and item.SJID=ksitem.SJID and ksitem.SJID={2}  
                                            AND item.ItemName=CAST( ksitem.Answer as VARCHAR(50)) and ksitem.CRUser='{0}' where item.STID in ({1}) and item.SJID={2}", P1, rowType["stIds"], sjID);
                    string sql = string.Format("SELECT STID,CAST( isnull(Record,0) as INT) Record,Answer FROM SZHL_KS_USERKSItem WHERE CRUser='{0}' AND STID in ({1}) AND SJID={2}", P1, rowType["stIds"], sjID);
                    DataTable questionItem = new SZHL_KS_STItemB().GetDTByCommand(strItemSql);
                    DataTable dtuser = new SZHL_KS_USERKSItemB().GetDTByCommand(sql);
                    foreach (DataRow rowST in dtST.Rows)
                    {
                        rowST["QItem"] = questionItem.FilterTable(" STID=" + rowST["STID"]);
                        DataTable dtuser2 = dtuser.FilterTable(" STID=" + rowST["STID"]);
                        if (dtuser2 != null && dtuser2.Rows.Count > 0)
                        {
                            rowST["Answer"] = dtuser2.Rows[0]["Answer"];
                            rowST["Record"] = dtuser2.Rows[0]["Record"];
                        }
                        else
                        {
                            rowST["Record"] = 0;
                        }
                    }
                    rowType["STList"] = dtST;
                }
                row["TXType"] = dtType;
            }
            SZHL_KS_USERKS userks = new SZHL_KS_USERKSB().GetEntity(d => d.CRUser == P1 && d.ComId == UserInfo.User.ComId && d.KSAPID == ksapID);
            msg.Result1 = userks;

            msg.Result = dt;
        }
        // 课程详细页面考试记录查看详细 
        public void GETKCYJVIEW(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int sjID = 0, ksId = 0;
            int.TryParse(P2, out ksId);
            SZHL_KS_USERKS userks = new SZHL_KS_USERKSB().GetEntity(d => d.ID == ksId);

            if (userks != null)
            {
                sjID = userks.SJID.Value;
            }

            //获取试卷信息
            DataTable dt = new SZHL_KS_SJB().GetDTByCommand(string.Format("SELECT sj.ID,sj.SJName,sj.TotalRecord,sj.SJDescribe,sj.PassRecord,sj.KSSC,COUNT(DISTINCT sjst.STType) DTCount,COUNT(DISTINCT sjst.STID) XTCount from  SZHL_KS_SJ sj inner join SZHL_KS_SJSTGL sjst on  sj.ID=sjst.SJID where  sj.ID={0} and sj.ComId={1}  GROUP by sj.ID,sj.SJName,sj.TotalRecord,sj.SJDescribe,sj.PassRecord,sj.KSSC ", sjID, UserInfo.User.ComId));
            dt.Columns.Add("TXType", Type.GetType("System.Object"));
            foreach (DataRow row in dt.Rows)
            {
                //获取试卷的题型列表 strIds 题库试题Id 
                DataTable dtType = new SZHL_KS_SJB().GetDTByCommand(@"SELECT  DISTINCT STType,sum(isnull(Record,0)) totalRecord,COUNT(ID) totalCount,stuff((select ','+cast( sjst.STID as varchar) from SZHL_KS_SJSTGL sjst where sjst.SJID=SZHL_KS_SJSTGL.SJID and sjst.STType=SZHL_KS_SJSTGL.STType for xml path('')),1,1,'') stIds
                                                                    from SZHL_KS_SJSTGL where SJID=" + row["ID"] + " GROUP by STType,SJID");
                dtType.Columns.Add("STList", Type.GetType("System.Object"));
                foreach (DataRow rowType in dtType.Rows)
                {
                    //获取试卷的题列表
                    DataTable dtST = new SZHL_KS_SJB().GetDTByCommand(@"SELECT st.ID,st.STID,st.STType,cast(st.QContent as VARCHAR(MAX)) QContent,COUNT(item.UserKSID) ksCount,st.QAnswer,st.Record as stRecord  FROM  SZHL_KS_SJSTGL st LEFT  JOIN SZHL_KS_USERKSItem item on st.STID=item.STID and item.SJID=" + sjID + "  and item.CRUser='" + P1 + "' where  st.STID in (" + rowType["stIds"] + ") and st.SJID=" + sjID + " GROUP by st.ID,st.STID,st.STType,cast(st.QContent as VARCHAR(MAX)),st.QAnswer,st.Record ");

                    dtST.Columns.Add("QItem", Type.GetType("System.Object"));
                    dtST.Columns.Add("Answer", Type.GetType("System.String"));
                    dtST.Columns.Add("Record", Type.GetType("System.String"));
                    string strItemSql = string.Format(@"SELECT item.*,ksitem.ID isselect from SZHL_KS_STItem item inner join SZHL_KS_SJST sjst on item.STID=sjst.STID LEFT join SZHL_KS_USERKSItem ksitem on item.STID=ksitem.STID and ksitem.SJID=" + sjID + "  AND item.ItemName=CAST( ksitem.Answer as VARCHAR(50)) and ksitem.CRUser='{0}' where item.STID in ({1}) and sjst.SJID={2}", P1, rowType["stIds"], sjID);
                    strItemSql = string.Format(@"SELECT item.*,ksitem.ID isselect from SZHL_KS_SJSTGLItem item inner join SZHL_KS_SJSTGL sjst 
                                            on item.STID=sjst.STID and item.SJID=sjst.SJID LEFT join SZHL_KS_USERKSItem ksitem on item.STID=ksitem.STID and item.SJID=ksitem.SJID and ksitem.SJID={2}  and ksitem.UserKSID={3}
                                            AND item.ItemName=CAST( ksitem.Answer as VARCHAR(50)) and ksitem.CRUser='{0}' where item.STID in ({1}) and item.SJID={2} ", P1, rowType["stIds"], sjID, ksId);
                    string sql = string.Format("SELECT STID,CAST( isnull(Record,0) as INT) Record,Answer FROM SZHL_KS_USERKSItem WHERE CRUser='{0}' AND STID in ({1}) AND SJID={2} and UserKSID={3}", P1, rowType["stIds"], sjID, ksId);
                    DataTable questionItem = new SZHL_KS_STItemB().GetDTByCommand(strItemSql);
                    DataTable dtuser = new SZHL_KS_USERKSItemB().GetDTByCommand(sql);
                    foreach (DataRow rowST in dtST.Rows)
                    {
                        rowST["QItem"] = questionItem.FilterTable(" STID=" + rowST["STID"]);
                        DataTable dtuser2 = dtuser.FilterTable(" STID=" + rowST["STID"]);
                        if (dtuser2 != null && dtuser2.Rows.Count > 0)
                        {
                            rowST["Answer"] = dtuser2.Rows[0]["Answer"];
                            rowST["Record"] = dtuser2.Rows[0]["Record"];
                        }
                        else
                        {
                            rowST["Record"] = 0;
                        }
                    }
                    rowType["STList"] = dtST;
                }
                row["TXType"] = dtType;
            }
            msg.Result1 = userks;

            msg.Result = dt;
        }
        #endregion

        #endregion

        #region 获取我的考试记录
        public void GETKSJL(HttpContext context, Msg_Result msg, string P1, string P2, JH_Auth_UserB.UserInfo UserInfo)
        {
            int page = 0;
            int.TryParse(context.Request.QueryString["p"] ?? "1", out page);//页码
            page = page == 0 ? 1 : page;
            int recordCount = 0;
            string userName = UserInfo.User.UserName;
            string user = context.Request["user"] ?? "";
            if (user != "")
            {
                userName = user;
            }
            string strWhere = string.Format("ku.CRUser='{0}' and ku.ComId={1} AND ksap.Status=1", userName, UserInfo.User.ComId);
            DataTable dt = new SZHL_KS_USERKSB().GetDataPager(" SZHL_KS_USERKS ku INNER JOIN SZHL_KS_KSAP ksap ON ksap.ID=ku.KSAPID INNER JOIN SZHL_KS_SJ sj ON sj.ID=ku.SJID", "ku.CRUser,ku.Record,ksap.KSName,sj.TotalRecord,sj.SJName,ksap.KSDate,ksap.KSSC,ksap.YCSY", 8, page, "ksap.KSDate DESC", strWhere, ref recordCount);
            msg.Result = dt;
            msg.Result1 = recordCount;
        }
        #endregion

    }

    public class StRecord
    {
        public int STID { get; set; }
        public string Record { get; set; }
    }

}