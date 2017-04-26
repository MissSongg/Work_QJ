using QJY.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using System.Web.UI.WebControls;
using System.Text;
namespace QJY.API
{
    #region 文档管理模块
    public class FT_FolderB : BaseEFDao<FT_Folder>
    {


        public FoldFile GetWDTREE(int FolderID, ref List<FoldFileItem> ListID, int comId, string strUserName = "")
        {
            List<FT_Folder> ListAll = new FT_FolderB().GetEntities(d => d.ComId == comId).ToList();
            FT_Folder Folder = new FT_FolderB().GetEntity(d => d.ID == FolderID);
            FT_FolderB.FoldFile Model = new FT_FolderB.FoldFile();
            Model.Name = Folder.Name;
            Model.FolderID = Folder.ID;
            Model.CRUser = Folder.CRUser;
            Model.PFolderID = Folder.PFolderID.Value;
            ListID.Add(new FoldFileItem() { ID = Folder.ID, Type = "folder" });
            if (strUserName != "")
            {
                Model.SubFileS = new FT_FileB().GetEntities(d => d.FolderID == Folder.ID && d.CRUser == strUserName && d.ComId == comId).ToList();
            }
            else
            {
                Model.SubFileS = new FT_FileB().GetEntities(d => d.FolderID == Folder.ID && d.ComId == comId).ToList();
            }
            foreach (var item in Model.SubFileS)
            {
                ListID.Add(new FoldFileItem() { ID = item.ID, Type = "file" });

            }
            Model.SubFolder = new FT_FolderB().GETNEXTFLODER(Folder.ID, ListAll, ref ListID, comId, strUserName);
            return Model;
        }


        private List<FoldFile> GETNEXTFLODER(int FolderID, List<FT_Folder> ListAll, ref List<FoldFileItem> ListID, int comId, string strUserName = "")
        {
            List<FoldFile> ListData = new List<FoldFile>();
            var list = ListAll.Where(d => d.PFolderID == FolderID && d.ComId == comId);
            if (strUserName != "")
            {
                list = list.Where(d => d.CRUser == strUserName);
            }
            foreach (var item in list)
            {
                FoldFile FolderNew = new FoldFile();
                FolderNew.FolderID = item.ID;
                FolderNew.Name = item.Name;
                FolderNew.CRUser = item.CRUser;
                FolderNew.PFolderID = item.PFolderID.Value;
                if (strUserName != "")
                {
                    FolderNew.SubFileS = new FT_FileB().GetEntities(d => d.FolderID == item.ID && d.CRUser == strUserName && d.ComId == comId).ToList();
                }
                else
                {
                    FolderNew.SubFileS = new FT_FileB().GetEntities(d => d.FolderID == item.ID && d.ComId == comId).ToList();
                }
                foreach (var SubFile in FolderNew.SubFileS)
                {
                    ListID.Add(new FoldFileItem() { ID = SubFile.ID, Type = "file" });
                }
                FolderNew.SubFolder = GETNEXTFLODER(item.ID, ListAll, ref ListID, comId, strUserName);
                ListData.Add(FolderNew);
                ListID.Add(new FoldFileItem() { ID = item.ID, Type = "folder" });
            }
            return ListData;

        }


        /// <summary>
        /// 复制树状结构
        /// </summary>
        /// <param name="FloderID"></param>
        /// <param name="PID"></param>
        public void CopyFloderTree(int FloderID, int PID, int comId)
        {
            List<FoldFileItem> ListID = new List<FoldFileItem>();
            FoldFile Model = new FT_FolderB().GetWDTREE(FloderID, ref ListID, comId);
            FT_Folder Folder = new FT_FolderB().GetEntity(d => d.ID == Model.FolderID && d.ComId == comId);
            Folder.PFolderID = PID;
            new FT_FolderB().Insert(Folder);

            //更新文件夹路径Code
            FT_Folder PFolder = new FT_FolderB().GetEntity(d => d.ID == PID && d.ComId == comId);
            Folder.Remark = PFolder.Remark + "-" + Folder.ID;
            new FT_FolderB().Update(Folder);

            foreach (FT_File file in Model.SubFileS)
            {
                file.FolderID = Folder.ID;
                new FT_FileB().Insert(file);
            }
            GreateWDTree(Model.SubFolder, Folder.ID, comId);
        }

        /// <summary>
        /// 根据父ID创建树装结构文档
        /// </summary>
        /// <param name="ListFoldFile"></param>
        private void GreateWDTree(List<FoldFile> ListFoldFile, int newfolderid, int comId)
        {

            foreach (FoldFile item in ListFoldFile)
            {

                FT_Folder PModel = new FT_FolderB().GetEntity(d => d.ID == item.FolderID && d.ComId == comId);
                PModel.PFolderID = newfolderid;
                new FT_FolderB().Insert(PModel);

                //更新文件夹路径Code
                FT_Folder PFolder = new FT_FolderB().GetEntity(d => d.ID == newfolderid && d.ComId == comId);
                PModel.Remark = PFolder.Remark + "-" + PModel.ID;
                new FT_FolderB().Update(PModel);

                foreach (FT_File file in item.SubFileS)
                {
                    file.FolderID = PModel.ID;
                    new FT_FileB().Insert(file);
                }

                GreateWDTree(item.SubFolder, PModel.ID, comId);



            }
        }



        /// <summary>
        /// 判断用户是否有当前文件件的管理权限
        /// </summary>
        /// <param name="FloderID"></param>
        /// <param name="strUserName"></param>
        /// <returns></returns>
        public string isHasManage(string FloderID, string strUserName)
        {
            string strReturn = "N";
            FT_Folder Model = new FT_FolderB().GetEntities("ID=" + FloderID).SingleOrDefault();
            if (!string.IsNullOrEmpty(Model.Remark))
            {
                string str = Model.Remark.ToFormatLike();
                DataTable dt = new FT_FolderB().GetDTByCommand("SELECT ID FROM FT_Folder WHERE ','+UploadaAuthUsers+','  like '%," + strUserName + ",%'  AND ID IN ('" + Model.Remark.ToFormatLike('-') + "')");
                if (dt.Rows.Count > 0)
                {
                    strReturn = "Y";
                }
            }
            return strReturn;
        }


        public void DelWDTree(int FolderID, int comId)
        {
            List<FoldFileItem> ListID = new List<FoldFileItem>();
            new FT_FolderB().GetWDTREE(FolderID, ref ListID, comId);
            foreach (FoldFileItem listitem in ListID)
            {
                if (listitem.Type == "file")
                {
                    new FT_FileB().Delete(d => d.ID == listitem.ID && d.ComId == comId);
                }
                else
                {
                    new FT_FolderB().Delete(d => d.ID == listitem.ID && d.ComId == comId);
                }
            }

        }



        public class FoldFile
        {
            public int FolderID { get; set; }
            public string Name { get; set; }
            public string CRUser { get; set; }
            public int PFolderID { get; set; }
            public string Remark { get; set; }

            public List<FoldFile> SubFolder { get; set; }
            public List<FT_File> SubFileS { get; set; }

        }
        public class FoldFileItem
        {
            public int ID { get; set; }
            public string Type { get; set; }

        }
    }
    public class FT_FileB : BaseEFDao<FT_File>
    {
        public void AddVersion(FT_File oldmodel, string strMD5, string strSIZE)
        {
            FT_File_Vesion Vseion = new FT_File_Vesion();
            Vseion.FileMD5 = oldmodel.FileMD5;
            Vseion.RFileID = oldmodel.ID;
            new FT_File_VesionB().Insert(Vseion);
            //添加新版本

            oldmodel.FileVersin = oldmodel.FileVersin + 1;
            oldmodel.FileMD5 = strMD5;
            oldmodel.FileSize = strSIZE;
            new FT_FileB().Update(oldmodel);
            //修改原版本

        }



        /// <summary>
        /// 判断同一目录下是否有相同文件(不判断应用文件夹)
        /// </summary>
        /// <param name="strMD5"></param>
        /// <param name="strFileName"></param>
        /// <param name="FolderID"></param>
        /// <returns></returns>
        public FT_File GetSameFile(string strFileName, int FolderID, int ComId)
        {
            int[] folders = { 1, 2, 3 };
            if (!folders.Contains(FolderID))
            {
                return new FT_FileB().GetEntities(d => d.ComId == ComId && (d.Name + "." + d.FileExtendName) == strFileName && d.FolderID == FolderID).FirstOrDefault();
            }
            return null;

        }

        /// <summary>
        /// 获取文件在服务器上的预览文件路径
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>


        /// <summary>
        /// 更新企业空间占用
        /// </summary>
        /// <param name="FileSize"></param>
        /// <returns></returns>
        public int AddSpace(int ComId, int FileSize)
        {
            JH_Auth_QY qymodel = new JH_Auth_QYB().GetEntity(d => d.ComId == ComId);
            if (qymodel != null)
            {
                qymodel.QyExpendSpace = qymodel.QyExpendSpace + FileSize;
            }
            new JH_Auth_QYB().Update(qymodel);
            return qymodel.QyExpendSpace.Value;
        }
    }



    public class FT_File_DownhistoryB : BaseEFDao<FT_File_Downhistory>
    {

    }


    public class FT_File_ShareB : BaseEFDao<FT_File_Share>
    {

    }


    public class FT_File_UserAuthB : BaseEFDao<FT_File_UserAuth>
    {

    }


    public class FT_File_UserTagB : BaseEFDao<FT_File_UserTag>
    {

    }

    public class FT_File_VesionB : BaseEFDao<FT_File_Vesion>
    {

    }

    #endregion




    #region 业务模块

    public class JH_Auth_TLB : BaseEFDao<JH_Auth_TL>
    {
        public DataTable GetTL(string strMsgType, string MSGTLYID)
        {
            DataTable dtList = new DataTable();
            dtList = new JH_Auth_TLB().GetDTByCommand("  SELECT *  FROM JH_Auth_TL WHERE MSGType='" + strMsgType + "' AND  MSGTLYID='" + MSGTLYID + "'");
            dtList.Columns.Add("FileList", Type.GetType("System.Object"));
            foreach (DataRow dr in dtList.Rows)
            {
                if (dr["MSGisHasFiles"] != null && dr["MSGisHasFiles"].ToString() != "")
                {
                    int[] fileIds = dr["MSGisHasFiles"].ToString().SplitTOInt(',');
                    dr["FileList"] = new FT_FileB().GetEntities(d => fileIds.Contains(d.ID));
                }
            }
            return dtList;
        }
    }
    #endregion
    public class SZHL_XXFBTypeB : BaseEFDao<SZHL_XXFBType>
    {
    }
    public class SZHL_XXFBB : BaseEFDao<SZHL_XXFB>
    {
    }
    public class SZHL_XXFB_ITEMB : BaseEFDao<SZHL_XXFB_ITEM>
    {
    }
    public class SZHL_LCSPB : BaseEFDao<SZHL_LCSP>
    {
    }


    public class SZHL_CCXJB : BaseEFDao<SZHL_CCXJ>
    {
    }
    public class SZHL_WQQDB : BaseEFDao<SZHL_WQQD>
    {
    }
    public class SZHL_HYGLB : BaseEFDao<SZHL_HYGL>
    {
    }
    public class SZHL_HYGL_ROOMB : BaseEFDao<SZHL_HYGL_ROOM>
    {
    }
    public class SZHL_HYGL_QRB : BaseEFDao<SZHL_HYGL_QR>
    {
    }
    public class SZHL_HYGL_QDB : BaseEFDao<SZHL_HYGL_QD>
    {
    }

    public class SZHL_YCGLB : BaseEFDao<SZHL_YCGL>
    {
    }
    public class SZHL_YCGL_CARB : BaseEFDao<SZHL_YCGL_CAR>
    {
    }
    public class SZHL_JFBXB : BaseEFDao<SZHL_JFBX>
    {
        /// <summary>
        /// 获取编号
        /// </summary> 
        public string GetFormCode()
        {
            string strSql = string.Format("select top 1 formcode from SZHL_JFBX where CRDate>'{0}' order by CRDate DESC", DateTime.Now.ToShortDateString());
            object obj = new SZHL_JFBXB().ExsSclarSql(strSql);
            string formCode = DateTime.Now.ToString("yyyyMMdd");
            if (obj == null || obj.ToString() == "")
            {
                formCode = formCode + "001";
            }
            else
            {
                string preFormCode = obj.ToString();
                int code = 0;
                int.TryParse(preFormCode.Substring(preFormCode.Length - 3), out code);
                formCode = formCode + (code + 1).ToString("000");
            }
            return formCode;
        }
    }
    public class SZHL_JFBXITEMB : BaseEFDao<SZHL_JFBXITEM>
    {
    }
    public class SZHL_RWGLB : BaseEFDao<SZHL_RWGL>
    {
    }

    public class SZHL_RWGL_ITEMB : BaseEFDao<SZHL_RWGL_ITEM>
    {
    }
    public class SZHL_QYHDB : BaseEFDao<SZHL_QYHD>
    {
    }
    public class SZHL_QYHD_ITEMB : BaseEFDao<SZHL_QYHD_ITEM>
    {
    }

    #region 企业活动（new）
    public class SZHL_QYHDNB : BaseEFDao<SZHL_QYHDN>
    {
    }
    public class SZHL_QYHD_OptionB : BaseEFDao<SZHL_QYHD_Option>
    {
    }
    public class SZHL_QYHD_ResultB : BaseEFDao<SZHL_QYHD_Result>
    {
    }
    #endregion

    public class JH_Auth_CommonB : BaseEFDao<JH_Auth_Common>
    {
    }
    public class SZHL_KDGLB : BaseEFDao<SZHL_KDGL>
    {
    }
    public class SZHL_KDDY_LISTB : BaseEFDao<SZHL_KDDY_LIST>
    {
    }
    public class SZHL_KDDY_PZB : BaseEFDao<SZHL_KDDY_PZ>
    {
    }
    public class SZHL_KDDY_CYDZB : BaseEFDao<SZHL_KDDY_CYDZ>
    {
    }
    public class SZHL_DCGLB : BaseEFDao<SZHL_DCGL>
    {
    }
    public class SZHL_DCGL_HEADERB : BaseEFDao<SZHL_DCGL_HEADER>
    {
    }
    public class SZHL_DCGL_ITEMB : BaseEFDao<SZHL_DCGL_ITEM>
    {
    }
    public class SZHL_TXSXB : BaseEFDao<SZHL_TXSX>
    {
    }
    public class SZHL_DXGLB : BaseEFDao<SZHL_DXGL>
    {
    }
    public class SZHL_TXLB : BaseEFDao<SZHL_TXL>
    {
    }
    public class SZHL_DDGLB : BaseEFDao<SZHL_DDGL>
    {
    }
    public class SZHL_XXFB_SCKB : BaseEFDao<SZHL_XXFB_SCK>
    {
    }
    public class SZHL_GZBGB : BaseEFDao<SZHL_GZBG>
    {
    }
    public class SZHL_HelpMenuB : BaseEFDao<SZHL_HelpMenu>
    {
    }
    public class SZHL_NOTEB : BaseEFDao<SZHL_NOTE>
    {
    }
    public class SZHL_WTFKB : BaseEFDao<SZHL_WTFK>
    {
    }
    public class SZHL_XMGLB : BaseEFDao<SZHL_XMGL>
    {
    }
    public class SZHL_CRM_KHGLB : BaseEFDao<SZHL_CRM_KHGL>
    {
    }
    public class SZHL_CRM_CONTACTB : BaseEFDao<SZHL_CRM_CONTACT>
    {
    }
    public class SZHL_CRM_HTGLB : BaseEFDao<SZHL_CRM_HTGL>
    {
    }
    public class SZHL_TSSQB : BaseEFDao<SZHL_TSSQ>
    {
    }
    public class SZHL_CRM_CPGLB : BaseEFDao<SZHL_CRM_CPGL>
    {
    }
    public class SZHL_CRM_GJJLB : BaseEFDao<SZHL_CRM_GJJL>
    {
    }
    public class SZHL_CRM_CARDB : BaseEFDao<SZHL_CRM_CARD>
    {
    }
    public class SZHL_QYIMB : BaseEFDao<SZHL_QYIM>
    {
    }
    public class SZHL_QYIM_ITEMB : BaseEFDao<SZHL_QYIM_ITEM>
    {
    }
    public class SZHL_QYIM_LISTB : BaseEFDao<SZHL_QYIM_LIST>
    {
    }
    public class SZHL_CRM_KDB : BaseEFDao<SZHL_CRM_KD>
    {
    }
    public class SZHL_KQBCB : BaseEFDao<SZHL_KQBC>
    { }
    public class SZHL_KQJLB : BaseEFDao<SZHL_KQJL>
    { }

    public class SZHL_XZ_JLB : BaseEFDao<SZHL_XZ_JL> { }
    public class SZHL_XZ_GZDB : BaseEFDao<SZHL_XZ_GZD> { }
    public class SZHL_CHAT_MSGB : BaseEFDao<SZHL_CHAT_MSG> { }
    public class SZHL_CHAT_GROUPB : BaseEFDao<SZHL_CHAT_GROUP> { }
    public class SZHL_CHAT_GROUPUSERB : BaseEFDao<SZHL_CHAT_GROUPUSER> { }
    public class SZHL_CHAT_MSG_GROUPB : BaseEFDao<SZHL_CHAT_MSG_GROUP> { }
    public class SZHL_CHAT_MSG_GROUP_USERB : BaseEFDao<SZHL_CHAT_MSG_GROUP_USER> { }

    public class SZHL_DRAFTB : BaseEFDao<SZHL_DRAFT> { }
    public class SZHL_DDGL_ITEMB : BaseEFDao<SZHL_DDGL_ITEM> { }
    public class SZHL_GZGLB : BaseEFDao<SZHL_GZGL> { }
    public class SZHL_GZGL_JCSZB : BaseEFDao<SZHL_GZGL_JCSZ> { }
    public class SZHL_GZGL_FLB : BaseEFDao<SZHL_GZGL_FL> { }
    public class SZHL_GZGL_WXYJB : BaseEFDao<SZHL_GZGL_WXYJ> { }
    public class SZHL_KS_KSAPB : BaseEFDao<SZHL_KS_KSAP>
    {
    }
    public class SZHL_KS_SJB : BaseEFDao<SZHL_KS_SJ>
    {
    }
    public class SZHL_KS_SJSTB : BaseEFDao<SZHL_KS_SJST>
    {
    }
    public class SZHL_KS_STB : BaseEFDao<SZHL_KS_ST>
    {
    }
    public class SZHL_KS_STItemB : BaseEFDao<SZHL_KS_STItem>
    {
        public void SaveQuestionItem(int questionId, string itemName, string itemDec, int ComId)
        {

            SZHL_KS_STItem questionItem = new SZHL_KS_STItem();
            questionItem.STID = questionId;
            questionItem.ItemName = itemName;
            questionItem.ItemDesc = itemDec.Trim();
            questionItem.ComId = ComId;
            new SZHL_KS_STItemB().Insert(questionItem);
        }
    }
    public class SZHL_KS_SJSTGLB : BaseEFDao<SZHL_KS_SJSTGL>
    {
    }
    public class SZHL_KS_SJSTGLItemB : BaseEFDao<SZHL_KS_SJSTGLItem>
    {
    }
    public class SZHL_KS_TKB : BaseEFDao<SZHL_KS_TK>
    {
    }
    public class SZHL_KS_TKFLB : BaseEFDao<SZHL_KS_TKFL>
    {
    }
    public class SZHL_KS_USERKSB : BaseEFDao<SZHL_KS_USERKS>
    {
    }
    public class SZHL_KS_USERKSItemB : BaseEFDao<SZHL_KS_USERKSItem>
    {
    }
    public class SZHL_PX_KCGLB : BaseEFDao<SZHL_PX_KCGL>
    {
    }
    public class SZHL_PX_KJGLB : BaseEFDao<SZHL_PX_KJGL>
    {
    }
    public class SZHL_DBGLB : BaseEFDao<SZHL_DBGL>
    {
    }
    public class SZHL_PX_SeeTimeB : BaseEFDao<SZHL_PX_SeeTime>
    {
    }


    #region 营销模块
    public class SZHL_YX_HDB : BaseEFDao<SZHL_YX_HD>
    {
    }
    public class SZHL_YX_HD_ITEMB : BaseEFDao<SZHL_YX_HD_ITEM>
    {
    }

    public class SZHL_YX_HD_GMB : BaseEFDao<SZHL_YX_HD_GM>
    {
    }
    public class SZHL_YX_HD_CYB : BaseEFDao<SZHL_YX_HD_CY>
    {
    }
     
    #endregion
}