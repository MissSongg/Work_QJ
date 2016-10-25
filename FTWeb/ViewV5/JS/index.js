var model = avalon.define({
    $id: "IndexV5",
    userName: ComFunJS.getnowuser(),
    CommonData: [],//消息中心
    yytype: "WORK",
    UserData: {},//用户信息
    UserInfo: {},//用户缓存数据
    CompanyData: {},//企业信息
    isshowload: true,
    XXCount: 0,//消息数量
    QYGGData: [],//企业公告
    QYHDData: [], //企业活动
    wigetdata: [],//工作台组件数据
    initobj: "",//初始化要传给组件的数据
    ishasLeft: true,//是否要隐藏左侧菜单
    FunData: [],//选中模块
    isnull: false,//是否有数据
    SelModelMenu: function (item) {
        model.FunData.clear();
        if (item) {
            model.SelModel = item;
            model.FunData.pushArray(item.FunData.$model);
            model.ishasLeft = false;
        } else {
            model.SelModel = null;
            if (localStorage.getItem("WIGETDATAV5")) {
                model.FunData.pushArray(JSON.parse(localStorage.getItem("WIGETDATAV5")));
            } else {
                // model.FunData = [{ code: "RWGL", name: "任务管理", wigetpath: "RWGL/RWGLLIST", issel: true, isshow: true, order: 0 }, { code: "LCSP", name: "流程管理", wigetpath: "RWGL/RWGLLIST", issel: false, isshow: true, order: 2 }, { code: "NOTE", name: "记事本", wigetpath: "RWGL/RWGLLIST", issel: false, isshow: true, order: 3 }, { code: "KJFS", name: "快捷网址", wigetpath: "RWGL/RWGLLIST", issel: false, isshow: true, order: 4 }];
                model.FunData.pushArray([
                    { PageCode: "/ViewV5/AppPage/RWGL/RWGLLIST", ExtData: "", code: "RWGL", PageName: "任务管理", issel: true, isshow: true, order: 0 },
                    { PageCode: "/ViewV5/TempWiget/KJFS", ExtData: "", code: "KJFS", PageName: "快捷网址", issel: true, isshow: true, order: 0 },
                    { PageCode: "/ViewV5/TempWiget/NOTE", ExtData: "", PageName: "记事本", code: "NOTE", issel: true, isshow: true, order: 0 },
                    { PageCode: "/ViewV5/TempWiget/LCSP", ExtData: "", code: "LCSP", PageName: "流程审批", issel: true, isshow: true, order: 0 }]);
            }
            model.ishasLeft = true;
        }
        model.selmenulev2(model.FunData[0]);
        $('body,html').animate({ scrollTop: 0 }, '500');
    },//选中最左侧事件
    SelModelXX: function () {
        model.FunData.clear();
        model.SelModel = null;
        model.FunData.pushArray([{ PageCode: "/ViewV5/AppPage/XXZX/XXZXLIST", PageName: "消息管理", issel: true, isshow: true, order: 0 }]);
        model.ishasLeft = false;
        model.selmenulev2(model.FunData[0]);
    },//选中最左侧事件
    SelUserInfo: function () {
        model.FunData.clear();
        model.SelModel = null;
        model.FunData.pushArray([{ PageCode: "/ViewV5/AppPage/XTGL/UserCenter", PageName: "个人中心", issel: true, isshow: true, order: 0 }]);
        model.ishasLeft = false;
        model.selmenulev2(model.FunData[0]);
    },//选中消息的事件
    SelModelBZ: function () {
        model.FunData.clear();
        model.SelModel = null;
        model.FunData.pushArray([{ PageCode: "/ViewV5/AppPage/BZZX/BZZXLIST", PageName: "帮助中心", issel: true, isshow: true, order: 0 }]);
        model.ishasLeft = false;
        model.selmenulev2(model.FunData[0]);
    },
    SelModelWTFK: function () {
        model.FunData.clear();
        model.SelModel = null;
        model.FunData.pushArray([{ PageCode: "/ViewV5/AppPage/WTFK/WTFKLIST", PageName: "问题反馈", issel: true, isshow: true, order: 0 }]);
        model.ishasLeft = false;
        model.selmenulev2(model.FunData[0]);
    },
    selmenulev2: function (item) {
        model.isnull = false;
        model.initobj = null;//先清空数据
        model.PageCode = item.PageCode;
        model.initobj = item.ExtData;
        model.rdm = Math.random();
        model.PageCodeRdm = model.PageCode + '.html?r=' + model.rdm;
        model.page.pageindex = 1;
        model.page.total = 0;
        model.ShowColumns = [];
        model.TypeData = [];
        model.ListData = [];
        model.search.seartype = '1';
        model.search.searchcontent = '';

        if (localStorage.getItem(model.PageCode + "pagecount")) {
            model.page.pagecount = localStorage.getItem(model.PageCode + "pagecount");
        } else {
            model.page.pagecount = 10;
        }
        //清除日历样式
        $(".datetimepicker").remove()

    },//选中二级菜单事件
    refpage: function (pagecode) {
        if (pagecode) {
            for (var i = 0; i < model.FunData.length; i++) {
                if (model.FunData[i].PageCode.indexOf(pagecode) > -1) {
                    model.selmenulev2(model.FunData[i]);
                    return;
                }
            }
        } else {
            model.refpage(model.PageCode)
        }

    },//刷新页面
    PageCode: "/ViewV5/Base/Loading",//需要加载的模板
    PageCodeRdm: "/ViewV5/Base/Loading.html",//需要加载的模板
    rdm: Math.random(),//随机数

    Temprender: function () {
        if (typeof (tempindex) != "undefined") {
            tempindex.InitWigetData(model.initobj);

            if (model.ShowColumns.size() > 0) {
                setTimeout("model.GetExtColumns(" + model.rdm + ")", 1500);
            }
        }
    },//组件加载完成事件
    exit: function () {
        ComFunJS.winconfirm("确认要退出吗？", function () {
            ComFunJS.delCookie('szhlcode');

            location.href = "/login.html"
        })
    },//退出事件
    refiframe: function () {
        location.reload();

    }, //刷新当前页面
    selyyType: function (type, dom) {
        $(".yytype").removeClass("active")
        $(dom).children("a").addClass("active")
        model.yytype = type;

        $(".nav-list ul li:visible").eq(0).click();
    },//应用类别
    menutype: "WORK",
    GetXXZXList: function () {
        $.getJSON('/API/VIEWAPI.ashx?Action=XTGL_GETXXZXIST', function (resultData) {
            if (resultData.ErrorMsg == "") {
                model.CommonData = resultData.Result;
                model.XXCount = resultData.Result1;
            }
        })
    },//加载消息中心
    GetUserData: function () {
        $.getJSON('/API/VIEWAPI.ashx?Action=XTGL_GETUSERBYUSERNAME', { P1: ComFunJS.getnowuser() }, function (resultData) {
            if (resultData.ErrorMsg == "" && resultData.Result) {
                model.UserInfo = resultData.Result;
                model.UserData = resultData.Result.User;
                model.CompanyData = resultData.Result.QYinfo;
                $(document).attr("title", model.CompanyData.QYName + "-企捷微信办公平台");//修改title值
                model.GetCompanyAuth();
                ComFunJS.setCookie('fileapi', resultData.Result.QYinfo.FileServerUrl);
            }
        })
        $.getJSON('/API/VIEWAPI.ashx?Action=QYHD_GETQYHDLIST_PAGE', { p: 1, listType: 0 }, function (resultData) {
            if (resultData.ErrorMsg == "") {
                model.QYHDData = resultData.Result;
            }
        })
        $.getJSON('/API/VIEWAPI.ashx?Action=XXFB_GETXXFBLIST', { "type": "1" }, function (resultData) {
            if (resultData.ErrorMsg == "") {
                model.QYGGData = resultData.Result;
            }
        })
    }, //获取用户信息
    AuthList: [],//用户有权限的菜单
    KJFSData: [],//可以设置快捷方式的功能
    GetCompanyAuth: function () {
        $.getJSON('/API/VIEWAPI.ashx?Action=INIT_GETINDEXMENU', { P1: "PCINDEX" }, function (resultData) {
            if (resultData.ErrorMsg == "") {
                if (resultData.Result) {
                    resultData.Result.forEach(function (val) {
                        val.isshow = true;
                    })
                }
                model.AuthList = resultData.Result;
                if (resultData.Result1) {
                    resultData.Result1.forEach(function (val) {
                        val.issel = val.issel == "True";
                        val.isshow = true;
                    })
                    model.KJFSData = resultData.Result1;
                }

            }
        })
    }, //用户权限信息

    SETKJFS: function (item, event) {
        if (event) {
            event.stopPropagation();
        }
        $.post('/API/VIEWAPI.ashx?Action=INIT_SETAPPINDEX', { "P1": item.ModelCode, "Status": item.issel ? "N" : "Y", type: "PCKJFS", name: item.ModelName }, function (result) {
            var result = $.parseJSON(result);
            if (result.ErrorMsg == "") {
                item.issel = item.issel ? false : true;
                ComFunJS.winsuccess("设置成功")
                model.refiframe();
            }
        })


    },//设置快捷方式
    AddView: function (code, Name, ID, pcode, event) {
        if (event) {
            event.stopPropagation();
        }
        if (code == "QYTX" || code == "DXGL") {
            ComFunJS.winviewform("/View/Base/APP_ADD_WF.html?FormCode=" + code, Name, "1000");
        }
        else {
            if (!ID) {
                ID = "";
            }
            if (pcode == "CRM") {
                code = pcode + "_" + code;
            }
            ComFunJS.winviewform("/ViewV5/AppPage/APP_ADD_WF.html?FormCode=" + code + "&ID=" + ID, Name, "1000");

        }
    },//添加表格防范
    ViewForm: function (code, ID, PIID, event) {
        event = event ? event : window.event
        var obj = event.srcElement ? event.srcElement : event.target;
        if ($(obj).hasClass("icon-check") || $(obj).attr("type") == "checkbox") {
            return;
        } else {
            ComFunJS.winviewform("/ViewV5/AppPage/APPVIEW.html?FormCode=" + code + "&ID=" + ID + "&PIID=" + PIID + "&r=" + Math.random(), "查看");

        }
    },//查看表格方法
    ViewFormNew: function (code, ID, PIID, event) {
        event = event ? event : window.event
        var obj = event.srcElement ? event.srcElement : event.target;
        if ($(obj).hasClass("lk")) {
            ComFunJS.winviewform("/ViewV5/AppPage/APPVIEW.html?FormCode=" + code + "&ID=" + ID + "&PIID=" + PIID + "&r=" + Math.random(), "查看");

        }
    },//查看表格方法
    EditForm: function (code, ID, PIID, event) {
        if (event) {
            event.stopPropagation();
        }
        ComFunJS.winviewform("/ViewV5/AppPage/APP_ADD_WF.html?FormCode=" + code + "&ID=" + ID + "&PIID=" + PIID + "&r=" + Math.random(), "修改", "1000");
    },
    UseYYList: [],
    GetYYList: function () {
        $.getJSON('/API/VIEWAPI.ashx?Action=INIT_GETINDEXMENUNEW', { P1: "PCINDEX" }, function (resultData) {
            if (resultData.ErrorMsg == "") {
                resultData.Result.forEach(function (val, i) {
                    val.issel = val.issel == "True";
                })
                if (resultData.Result) {
                    resultData.Result.forEach(function (val) {
                        val.FunData.forEach(function (c) {
                            c.isshow = true;
                        })
                    })
                }
                model.UseYYList = resultData.Result;
                if (resultData.Result1 == "Y") {

                    $(".nav-list ul li:visible").eq(1).click();
                } else {
                    model.SelModelMenu('');
                }
                if (resultData.Result2 == "Y") {//超级管理员进行模块初始化
                    model.InitData();
                }
            }
        })
    },
    SaveYY: function (item) {
        var strContent = "";
        if (item.ISSY == 1) {
            strContent += item.ModelCode + ":N";
        }
        else {
            strContent += item.ModelCode + ":Y";
        }
        if (item.ISSY == 1) {
            item.ISSY = 0;
        }
        else {
            item.ISSY = 1;
        }
        if (strContent) {
            $.post("/API/VIEWAPI.ashx?Action=INIT_SETAPPINDEX", { P1: strContent, type: "PCINDEX" }, function (result) {
                var jsonresult = $.parseJSON(result)
                if ($.trim(jsonresult.ErrorMsg) != "") {
                    ComFunJS.winwarning(jsonresult.ErrorMsg);
                }
            });
        }
    },
    UploadHeadImage: function () {
        ComFunJS.winviewform("/ViewV5/Base/UploadTX.html", "头像上传", "700", "570");
    },  //上传头像
    ModifyPwd: function (dom) {
        var pwd = $("#newPwd").val();
        var pwd2 = $("#newPwd2").val();
        var retmsg = "";
        if ($("#UpdatePDModal .szhl_require")) {
            $("#UpdatePDModal .szhl_require").each(function () {
                if ($(this).val() == "") {
                    retmsg = $(this).parent().find("label").text() + "不能为空";
                }
            })
        }
        if (retmsg !== "") {
            top.layer.tips(retmsg, dom);
            return;
        }
        if (pwd != pwd2) {
            retmsg = "确认密码不一致";
            top.layer.tips(retmsg, dom);
            return;
        }
        $.post("/API/VIEWAPI.ashx?Action=XTGL_MODIFYPWD", { P1: pwd, P2: pwd2 }, function (result) {
            var jsonresult = $.parseJSON(result)
            $(dom).removeClass("disabled").find("i").hide();
            if ($.trim(jsonresult.ErrorMsg) != "") {
                top.ComFunJS.winwarning(jsonresult.ErrorMsg);
            } else {
                top.ComFunJS.winsuccess("操作成功");
                $('#UpdatePDModal').modal('hide');
            }
        });
        $("#newPwd").val("");
        $("#newPwd2").val("");
    },  //修改密码  待完善
    SaveWigetdata: function () {
        localStorage.setItem("WIGETDATAV5", JSON.stringify(model.FunData.$model));
        model.SelModelMenu('');
    },
    ChangeModelIsShow: function (item, dom, event) {
        if (event) {
            event.stopPropagation();
        }
        item.isshow = !item.isshow;
        model.SaveWigetdata();
    },

    //***通用列表页需要的数据***//
    TypeData: [], //类型数据
    GetTypeData: function (P1, callback) {//P1:字典类别，callback:回调函数,p2:字典类别ID
        $.getJSON('/API/VIEWAPI.ashx?Action=XTGL_GETZIDIANSLIST', { P1: P1 }, function (resultData) {
            if (resultData.ErrorMsg == "") {
                if (callback) {
                    return callback.call(this, resultData.Result);
                }
                else {
                    model.TypeData = resultData.Result;
                }
            }
        })
    },
    UserCustomData: [],
    GetCustomData: function (P1, callback) {//P1:类型，callback:回调函数
        $.getJSON('/API/VIEWAPI.ashx?Action=XTGL_GETUSERGROUP', { P1: P1 }, function (resultData) {
            if (resultData.ErrorMsg == "") {
                if (callback) {
                    return callback.call(this, resultData.Result);
                }
                else {
                    model.UserCustomData = resultData.Result;
                }
            }
        })
    },
    DelCustomData: function (item, event) {
        if (event.stopPropagation) {
            event.stopPropagation();
        }
        top.ComFunJS.winconfirm("确认要删除自定义搜索“" + item.DataContent + "”吗？", function () {
            $.post("/API/VIEWAPI.ashx", { Action: "XTGL_DELUSERGROUP", P1: item.ID }, function (result) {
                var jsonresult = $.parseJSON(result)
                if ($.trim(jsonresult.ErrorMsg) == "") {
                    model.UserCustomData.remove(item);
                    top.ComFunJS.winsuccess("删除成功");
                }
            })
        })
    },
    ShowColumns: [],  //显示的列名，数据在模板中填充
    ListData: [], //列表页数据
    page: { pageindex: 1, pagecount: 10, total: 0 }, //分页参数
    pageNum: [{ "num": 10 }, { "num": 20 }, { "num": 30 }, { "num": 50 }, { "num": 100 }],
    GetExtColumns: function (str) {  //获取扩展字段

        if (model.SelModel.ModelCode && model.rdm == str) {
            $.getJSON('/API/VIEWAPI.ashx?Action=XTGL_GETEXTENDFIELD', { P1: model.SelModel.ModelCode }, function (resultData) {
                if (resultData.ErrorMsg == "") {
                    $(resultData.Result).each(function (idx, itm) {
                        model.ShowColumns.push({ "ColName": itm.TableFiledName, "ColText": itm.TableFiledName, "IsSel": false, "format": "" });
                    })

                    var ext = localStorage.getItem(model.SelModel.ModelCode + "ShowColumns");
                    if (ext) {
                        $(model.ShowColumns).each(function (idx, itm) {
                            $(JSON.parse(ext)).each(function (index, ele) {
                                if (itm.ColName == ele.ColName) {
                                    itm.IsSel = ele.IsSel;
                                }
                            })
                        })

                    }
                }
            })
        }
    },
    ReSetShow: function (el) { //控制扩展字段展示
        if (el) {
            if (model.SelModel.ModelCode != 'LCSP' && el.type != "link") {
                el.IsSel = !el.IsSel;
                localStorage.setItem(model.SelModel.ModelCode + "ShowColumns", JSON.stringify(model.ShowColumns));
            }
            else {
                if (typeof (tempindex) != "undefined") {
                    tempindex.ReSetShow(el);
                }
            }
        }
    },
    search: { seartype: "1", searchcontent: "" },
    FnFormat: function (str, fmt) { //格式化
        str = str + "";
        if ((str || fmt.format == "gzstatus") && fmt.format) {

            switch (fmt.format) {
                case "shstate": //审核状态转换成文字
                    {
                        if (str == "0") {
                            str = "未审核";
                        } else if (str == "-1") {
                            str = "审核不通过";
                        } else if (str == "1") {
                            str = "审核通过";
                        }
                    }
                    break;

                case "statename": //审核流程，-1时不需要流程
                    {
                        if (str == "-1") str = "";
                    }
                    break;
                case "rwstate": //任务状态
                    {
                        if (str == "0") str = "待办任务";
                        else if (str == "1") str = "已办任务";
                        else if (str == "2") str = "过期任务";
                    }
                    break;
                case "dateformat": //日期格式，默认yyyy-mm-dd
                    {
                        str = ComFunJS.getnowdate("yyyy-mm-dd", str);
                    }
                    break;
                case "timeformat": //日期格式，默认yyyy-mm-dd
                    {
                        str = ComFunJS.getnowdate("yyyy-mm-dd hh:mm", str);
                    }
                    break;
                case "username": //用户id转成为用户名
                    {
                        str = ComFunJS.convertuser(str);
                    }
                    break;
                case "qrcode": //二维码图片展示
                    {
                        str = "<img src='" + str + "' style='width:60px;height:60px;' />"
                    }
                    break;
                case "bqh"://表情转换
                    {
                        return ComFunJS.bqhContent(str);
                    }
                    break;
                case "text"://截取字符串
                    {
                        str = ComFunJS.convstr(str);
                    }
                    break;
                case "txfs"://提醒方式
                    {
                        switch (str) {
                            case "0": str = '短信和微信'; break;
                            case "1": str = '短信'; break;
                            case "2": str = '微信'; break;
                        }

                    }
                    break;
                case "hdlx"://活动类型
                    {
                        switch (str) {
                            case "0": str = '活动报名'; break;
                            case "1": str = '企业投票'; break;
                        }
                    }
                    break;
                case "hdzt"://活动状态
                    {
                        switch (str) {
                            case "0": str = '已结束'; break;
                            case "1": str = '未开始'; break;
                            case "2": str = '正在进行'; break;
                        }
                    }
                    break;
                case "sr":     //收入明细
                    {
                        if (str * 1 >= 0)
                            str = "￥" + str;
                        else
                            str = "--";
                    }
                    break;
                case "zc":    //支出明细
                    {
                        if (str * 1 >= 0)
                            str = "--";
                        else
                            str = "￥" + str;
                    }
                    break;
                case "dyxx":    //快递json转换
                    {
                        if (str) {
                            var jsonresult = $.parseJSON(str);
                            var txtmsg = '';
                            jsonresult.forEach(function (item) {
                                txtmsg = txtmsg + item.title + ":" + item.text + "，";
                            });
                            str = txtmsg;
                        }
                    }
                    break;
                case "clzt":    //车辆状态
                    {
                        switch (str) {
                            case "0": str = '可用'; break;
                            case "1": str = '报废'; break;
                            case "2": str = '维修'; break;
                        }
                    }
                    break;
                case "hyszt":     //会议室状态
                    {
                        switch (str) {
                            case "0": str = '启用'; break;
                            case "1": str = '禁用'; break;
                        }
                    }
                    break;
                case "xxshzt":     //信息发布审核状态
                    {
                        switch (str) {
                            case "0": str = "未审核"; break;
                            case "1": str = "已审核"; break;
                            case "2": str = "正常"; break;
                            case "-1": str = "<label class='text-danger'>退回</label>"; break;
                        }
                    }
                    break;
                case "gzstatus":     //信息发布审核状态
                    {
                        if (str == undefined || !str || str != 1) {
                            str = "未关注";
                        } else
                            if (str == "1") {
                                str = "已关注";
                            }
                    }
                    break;

                default: {

                }
            }


        }
        if (fmt.len) {
            str = str.length > fmt.len ? str.substring(0, fmt.len) + '...' : str;
        }
        return str;

    },
    ViewQYHD: function (item) {
        if (item.Type == 1) {
            ComFunJS.winviewform("/ViewV5/AppPage/QYHD/QYTPVIEW.html?ID=" + item.ID + "&r=" + Math.random(), "投票信息");
        }
        else {
            ComFunJS.winviewform("/ViewV5/AppPage/QYHD/QYHDVIEW.html?ID=" + item.ID + "&r=" + Math.random(), "活动信息");
        }
    },
    ViewXXFB: function (xxitem) {
        ComFunJS.winviewform("/ViewV5/AppPage/XXFB/XXFBVIEW.html?ID=" + xxitem.ID + "&r=" + Math.random(), "新闻公告");
    },
    //***通用列表页需要的数据***//
    mouseover: function () {
        $(this).find(".tool").css("display", "block");
    },
    mouseout: function () {
        $(this).find(".tool").css("display", "none");
    },//鼠标移动控制显示和隐藏
    KQGZData: {},
    QDData: { QDDate: "未签到", Status: -1 },
    QTData: { QTDate: "未签退", Status: -1 },
    QDStatus: 0,
    GetKQGZ: function () {
        $.getJSON('/API/VIEWAPI.ashx?Action=KQGL_GETKQGZ', function (resultData) {
            if (resultData.ErrorMsg == "") {
                if (resultData.Result.length > 0) {
                    model.KQGZData = resultData.Result[0];
                }
                if (resultData.Result1.length > 0) {
                    model.QDData.QDDate = resultData.Result1[0].KQDate;
                    model.QDData.Status = resultData.Result1[0].Status;
                    model.QDStatus = 1;
                }
                if (resultData.Result2.length > 0) {
                    model.QTData.QTDate = resultData.Result2[0].KQDate;
                    model.QTData.Status = resultData.Result2[0].Status;
                    model.QDStatus = 2;
                }
            }
        })
    }, GetQDStausName: function () {
        var strName = "";
        switch (model.QDStatus) {
            case 0:
                strName = "签到";
                break;
            case 1:
                strName = "签退";
                break;
            case 2:
                strName = "考勤结束";
                break;
        }
        return strName;
    },
    SaveQD: function () {
        if (model.QDStatus == 2) {
            return;
        }
        $.post("/API/VIEWAPI.ashx", { Action: "KQGL_ADDKQJL", P1: model.QDStatus }, function (result) {
            var jsonresult = $.parseJSON(result)
            if ($.trim(jsonresult.ErrorMsg) == "") {
                if (model.QDStatus == 0) {
                    model.QDData.QDDate = jsonresult.Result.KQDate;
                    model.QDData.Status = jsonresult.Result.Status;
                    model.QDStatus = 1;
                } else if (model.QDStatus == 1) {
                    model.QTData.QTDate = jsonresult.Result.KQDate;
                    model.QTData.Status = jsonresult.Result.Status;
                    model.QDStatus = 2;
                }
                top.ComFunJS.winsuccess((model.QDStatus == 1 ? "签到" : "签退") + "成功");
            }
        })
    }, InitData: function () {
        $.post("/API/VIEWAPI.ashx", { Action: "INIT_INITMODELTYPE" }, function (result) {

        })

    }, OpenSet: function (event) {
        if (event) {
            event.stopPropagation();
        }
        $("#myModal").modal("show");
    }
})
avalon.ready(function () {
    ComFunJS.initsetajax(true);//初始化数据
    model.GetYYList();
    model.GetUserData();
    model.GetXXZXList();
    model.GetKQGZ();
})

model.page.$watch("pagecount", function () {
    localStorage.setItem(model.PageCode + "pagecount", model.page.pagecount);
})

