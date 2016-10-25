/**
 * 网页即时通讯客户端程序
 * CreateBy: David Ma
 * CreateOn: 2016-07-20
 * Version: 0.1
 */
'use strict';
var WebChat = {
    //后台处理地址
    serviceUrl: '/API/VIEWAPI.ashx?Action=CHAT_',
    //当前会话
    curChat: null,
    //当前登录人
    loginUser: {},
    //最近会话列表
    recentChatList: [],
    curRecentIndex: -1,
    //群组列表
    groupList: [],
    curGroupIndex: null,
    //组织结构列表
    organizationList: [],
    //是否停止获取消息
    isStopGetMsg: true,
    searchTimeOut: null,
    msgAudio: document.getElementById('msgAudio'),
    isAudioPlay: true,
    //初始化
    init: function () {
        //获取初始化数据
        $.getJSON(WebChat.serviceUrl + 'INITCHAT', {}, function (resultData) {
            if (resultData.ErrorMsg == "") {
                console.log(resultData);

                //--------------初始化聊天信息----开始
                //绑定当前人信息
                WebChat.loginUser = resultData.Result;
                $('#loginUserName').text(WebChat.loginUser.UserRealName);

                //群组列表
                WebChat.groupList = resultData.Result1;
                var groupHtml = '';
                $.each(WebChat.groupList, function (index, el) {
                    groupHtml += '<li class="group-li" id="g_' + index + '" >'
                        + '<div class="chart-div">'
                        + '<div class="pull-left img-ridus ml5"><img src="/ViewV5/images/IM/user2.jpg"></div>'
                        + '<div class="pull-left ml10 chat-info grap">'
                        + '<h4>' + el.GroupName + '</h4>'
                        + '</div></div></li>';
                });
                $('#groupUl').html(groupHtml);

                $('.group-li').click(function () {
                    var index = $(this).attr('id').substr(2);
                    WebChat.showGroupDetail(index);
                });

                //组织机构
                WebChat.organizationTreeList = resultData.Result2;
                WebChat.organizationList = [];
                WebChat.organizationHtml = '';
                WebChat.buildOrganizationTree(WebChat.organizationTreeList);
                $('#divDepart').html(WebChat.organizationHtml);

                //点击部门显示部门人员
                $('.list-box-bt').click(function () {
                    var deptCode = $(this).attr('id').substr(2);
                    WebChat.showDeptPerson(deptCode);
                });

                //添加群聊
                $('#divDepartForGroup').html(WebChat.organizationHtml);
                $('#divDepartForGroup').find('.list-box-bt').click(function () {
                    var deptCode = $(this).attr('id').substr(2);
                    WebChat.showDeptPersonForGroup(deptCode);
                });

                // 组织结构 展开收起
                $(".gs-box .down-open").click(function () {
                    $(this).next(".children-list").toggle();
                })

                //最近会话
                WebChat.recentChatList = resultData.Result3;
                WebChat.showRecentChatList();
                WebChat.setTotalNoMsgCount();
                //--------------初始化聊天信息----结束

                //-----------绑定事件----开始
                //搜索
                $('#txtSearch').keyup(function () {
                    if (!$('#txtSearch').val()) {
                        console.log('no value');
                        $('#ulSearchPersonList').html('');
                        $('#ulSearchGroupList').html('');
                        return;
                    }

                    //延迟500毫秒后进行搜索    
                    clearTimeout(WebChat.searchTimeOut);
                    WebChat.searchTimeOut = setTimeout("WebChat.searchPerson($('#txtSearch').val());", 500);
                });

                //发起群聊事件
                $('#btnCreateGroup').click(function () {
                    WebChat.showGroupEdit();
                });

                //保存群组
                $('#btnSaveGroup').click(function () {
                    //验证群组名称
                    var groupName = $.trim($('#addGroupName').val());
                    if (groupName.length == 0) {
                        alert('请输入群组名称');
                        return;
                    }

                    if (!WebChat.editGroup.personList || WebChat.editGroup.personList.length == 0) {
                        alert('请至少选择一个联系人');
                        return;
                    }

                    var personCodeString = '';
                    $.each(WebChat.editGroup.personList, function (index, el) {
                        personCodeString += el.UserName + ',';
                    });
                    personCodeString = personCodeString.substr(0, personCodeString.length - 1);

                    $.getJSON(WebChat.serviceUrl + 'CREATEGROUP', { P1: groupName, P2: personCodeString }, function (resultData) {
                        if (resultData.ErrorMsg == "") {
                            //重新获取群组列表
                            WebChat.rebindGroup();

                            //关闭编辑窗口
                            $('#divAddGroupPerson').html('');
                            $('#divGroupEdit').hide();

                            //创建会话
                            WebChat.startChat(1, resultData.Result);
                        }
                        else {
                            alert('添加群组失败');
                        }
                    });
                });

                //退出群组/解散群组
                $('#btnExitGroup').click(function () {
                    if (window.confirm("确定要" + $(this).text() + "吗")) {

                        var tag = $(this).attr('tag');

                        if (tag) {
                            $.getJSON(WebChat.serviceUrl + 'EXITGROUP', { P1: WebChat.groupList[WebChat.curGroupIndex].ID, P2: tag }, function (deleteResult) {
                                if (deleteResult.ErrorMsg == "") {
                                    $('#toGroupName').text('');
                                    $('#groupUserUl').html('');
                                    $('#btnExitGroup').hide();
                                    $('#btnStartGroupChat').hide();

                                    WebChat.rebindGroup();
                                }
                            });
                        }
                    }
                });

                //开始群组聊天
                $('#btnStartGroupChat').click(function () {
                    WebChat.startChat(1, WebChat.groupList[WebChat.curGroupIndex]);
                });

                //切换聊天列表事件
                var left_ul = $('#left_ul').children();
                var left_b = $('#left_b').children();
                var panel_r = $('#panel-r').children();

                left_ul.each(function (index, el) {
                    el.click(function () {
                        for (var i = 0; i < left_ul.length; i++) {
                            if (i == index) {
                                left_b[i].style.display = 'block';
                                left_ul[i].className = 'active';
                                panel_r[i].style.display = 'block';
                                panel_r[i].className = 'panel-right';

                            } else {
                                left_b[i].style.display = 'none';
                                left_ul[i].className = '';
                                panel_r[i].style.display = 'none';
                            }
                        }
                    });
                });

                //发送消息事件
                $('#btnSend').click(function () {
                    WebChat.sendMsg();
                });

                //输入框ctrl+enter事件
                $('#imgEditor').keydown(function (event) {
                    if (event.keyCode == 13 && event.ctrlKey) {
                    //if (event.keyCode == 13) {
                        WebChat.sendMsg();
                    }
                });

                //关闭窗口
                $('.hd-right-close:not(.pull-right)').click(function () {
                    if (window.parent && window.parent.closeim) {
                        window.parent.closeim();
                    };
                });
                //-----------绑定事件----结束

                //切换到会话列表
                change(0);

                //开始获取未读消息
                WebChat.getNoReadMsg();
            }
            else {
                console.log('初始化失败:' + resultData.ErrorMsg);
            }
        });

        var $btnOpenSound = $('#btnOpenSound');
        if (window.localStorage.chat_audio) {
            WebChat.isAudioPlay = window.localStorage.chat_audio == "1" ? true : false;
            $btnOpenSound.text(WebChat.isAudioPlay ? "关闭声音" : "打开声音");
        }
        else {
            window.localStorage.chat_audio = "1";
            $btnOpenSound.text("关闭声音");
        }

        $btnOpenSound.click(function () {
            WebChat.isAudioPlay = !WebChat.isAudioPlay;
            window.localStorage.chat_audio = WebChat.isAudioPlay ? "1" : "0";
            $btnOpenSound.text(WebChat.isAudioPlay ? "关闭声音" : "打开声音");

            if (WebChat.isAudioPlay) {
                WebChat.playMsgAudio();
            }
        });

        $('.img-icon-face').qqFace({
            assign: 'imgEditor',    //给输入框赋值 
            path: 'qqface/face/'    //表情图片存放的路径 
        });

        //$('#imgEditor').change(function () {
        //    var msg = $(this).val();

        //    if (/\[em_([0-9]*)\]/.test(msg))
        //    {
        //        msg = msg.replace(/\[em_([0-9]*)\]/g, '<img src="qqface/face/$1.gif" border="0" />');
        //        $(this).val(msg);
        //    }
        //});
    },
    //绑定组织机构树
    buildOrganizationTree: function (organizationList) {
        $.each(organizationList, function (index, el) {
            WebChat.organizationList.push(el);
            if (el.DeptRoot == -1) {
                WebChat.organizationHtml += '<h6 style="padding:0 20px">' + el.DeptName + '</h6>'
                    + '<ul class="gs-box">';

                if (el.ChildBranch && el.ChildBranch.length > 0) {
                    WebChat.buildOrganizationTree(el.ChildBranch);
                }

                WebChat.organizationHtml += '</ul>';
            }
            else if (el.ChildBranch && el.ChildBranch.length > 0) {
                WebChat.organizationHtml += '<li><div class="down-open list-box-bt" id="o_' + el.DeptCode + '"><i class="font20 bg grow-list"></i><span>' + el.DeptName + '</span></div>'
                    + '<ul class="children-list" style="display:none">';

                WebChat.buildOrganizationTree(el.ChildBranch);

                WebChat.organizationHtml += '</ul></li>'
            }
            else {
                WebChat.organizationHtml += '<li><div class="down-open list-box-bt" id="o_' + el.DeptCode + '"><span>' + el.DeptName + '</span></div></li>';
            }
        });
    },
    //根据部门编码查询部门人员
    showDeptPerson: function (deptCode) {
        var dept = null;
        $.each(WebChat.organizationList, function (index, el) {
            if (el.DeptCode == deptCode) {
                dept = el;
                if (!el.personList) {
                    el.personList = [];
                    $.getJSON('/API/VIEWAPI.ashx?Action=XTGL_GETYUSERBYCODE', { P1: deptCode }, function (resultData) {
                        if (resultData.ErrorMsg == "") {
                            if (resultData.Result && resultData.Result.length > 0) {
                                var personHtml = '<ul>';
                                $.each(resultData.Result, function (index, person) {
                                    personHtml += '<li id="p_' + index + '"><div class="clearfix">'
                                        + '<img src="/ViewV5/images/IM/user2.jpg" class="img-30 pull-left">'
                                        + '<div class="pull-left" style="margin-left:15px">'
                                        + '<h5>' + person.UserRealName + '</h5><p>' + person.zhiwu + '</p>'
                                        + '</div></div></li>';

                                    el.personList.push(person);
                                });

                                el.personHtml = personHtml + '</ul>';
                                $('#divDeptPerson').html(el.personHtml);
                                $('li[id^="p_"]').click(function () {
                                    var person = el.personList[parseInt($(this).attr('id').substr(2))];
                                    var chat = WebChat.startChat(0, person);
                                });
                            }
                            else {
                                $('#divDeptPerson').html('');
                            }
                        }
                    });
                }
                else {
                    $('#divDeptPerson').html(el.personHtml);
                    $('li[id^="p_"]').click(function () {
                        var person = el.personList[parseInt($(this).attr('id').substr(2))];
                        var chat = WebChat.startChat(0, person);
                    });
                }

                return false;
            }
        });
    },
    showDeptPersonForGroup: function (deptCode) {
        var dept = null;
        $.each(WebChat.organizationList, function (index, el) {
            if (el.DeptCode == deptCode) {
                dept = el;
                if (!el.personHtmlForGroup || !el.personList) {
                    el.personList = [];
                    $.getJSON('/API/VIEWAPI.ashx?Action=XTGL_GETYUSERBYCODE', { P1: deptCode }, function (resultData) {
                        if (resultData.ErrorMsg == "") {
                            if (resultData.Result && resultData.Result.length > 0) {
                                var personHtml = '<ul>';
                                $.each(resultData.Result, function (index, person) {
                                    personHtml += '<li id="pg_' + index + '"><div class="clearfix">'
                                        + '<img src="/ViewV5/images/IM/user2.jpg" class="img-30 pull-left">'
                                        + '<div class="pull-left" style="margin-left:15px">'
                                        + '<h5>' + person.UserRealName + '</h5><p>' + person.zhiwu + '</p>'
                                        + '</div></div></li>';

                                    el.personList.push(person);
                                });

                                el.personHtmlForGroup = personHtml + '</ul>';
                                $('#divDepartPersonForGroup').html(el.personHtmlForGroup);
                                $('li[id^="pg_"]').click(function () {
                                    var person = el.personList[parseInt($(this).attr('id').substr(3))];
                                    WebChat.addPersonToGroup(person);
                                });
                            }
                            else {
                                $('#divDeptPerson').html('');
                            }
                        }
                    });
                }
                else {
                    $('#divDepartPersonForGroup').html(el.personHtmlForGroup);
                    $('li[id^="pg_"]').click(function () {
                        var person = el.personList[parseInt($(this).attr('id').substr(3))];
                        WebChat.addPersonToGroup(person);
                    });
                }

                return false;
            }
        });
    },
    //发送消息
    sendMsg: function () {
        var msgContent = $('#imgEditor').val();

        //如果消息内容为空，返回
        if (!msgContent)
            return;

        //组建消息
        var msgType = WebChat.curChat.MsgType;
        var msg = {
            MsgContentType: 0,
            MsgContent: msgContent
        };

        if (msgType == 0) {
            msg.ToUser = WebChat.curChat.chatUser.UserName;
            msg.ConverId = WebChat.curChat.ConverId;
        }
        else {
            msg.GroupId = WebChat.curChat.chatUser.GroupId;
        }

        //发送消息
        $.post(WebChat.serviceUrl + 'SENDMSG', { P1: JSON.stringify(msg), P2: msgType }, function (resultData) {
            resultData = JSON.parse(resultData);
            if (resultData.ErrorMsg == "") {
                var sendMsg = resultData.Result;
                sendMsg.FromUserRealName = WebChat.loginUser.UserRealName;

                WebChat.curChat.msgList.unshift(resultData.Result);
                WebChat.curChat = WebChat.appendMsgHtml(1, [resultData.Result], WebChat.curChat);

                $('#msgList').html(WebChat.curChat.msgListHtml);

                //if (type == 1) {
                $('#window-box-t').scrollTop($('#msgList').height());
                //}

                $('#imgEditor').val('').focus();
            }
        });
    },
    //设置当前会话
    setCurChat: function (index) {
        WebChat.curChat = WebChat.recentChatList[index];
        WebChat.curRecentIndex = index;

        //显示聊天对象名称
        if (WebChat.curChat.MsgType == 1) {
            $('#toUserName').text(WebChat.curChat.chatUser.GroupName);
        }
        else {
            $('#toUserName').text(WebChat.curChat.chatUser.UserRealName);
        }

        //显示消息列表
        WebChat.loadMsgList();
    },
    //显示消息列表
    loadMsgList: function () {
        if (WebChat.curChat) {
            //已经组装好html，直接显示
            if (WebChat.curChat.msgListHtml) {
                $('#msgList').html(WebChat.curChat.msgListHtml);
                $('#window-box-t').scrollTop($('#msgList').height());
            }
                //未组装html，但是有消息，组装html并显示
            else if (WebChat.curChat.msgList && WebChat.curChat.msgList.length > 0) {
                WebChat.curChat.msgListHtml = WebChat.createMsgListHtml(WebChat.curChat.msgList);
                $('#msgList').html(WebChat.curChat.msgListHtml);
                $('#window-box-t').scrollTop($('#msgList').height());
            }
                //根据会话id查询消息列表
            else if (WebChat.curChat.ConverId || WebChat.curChat.GroupId) {
                $.getJSON(WebChat.serviceUrl + 'GETCONVERSATION', {
                    P1: WebChat.curChat.ConverId || WebChat.curChat.GroupId,
                    P2: WebChat.curChat.MsgType
                }, function (resultData) {
                    if (resultData.ErrorMsg == "") {
                        WebChat.curChat.msgList = resultData.Result;
                        WebChat.curChat.msgListHtml = WebChat.createMsgListHtml(WebChat.curChat.msgList);
                        $('#msgList').html(WebChat.curChat.msgListHtml);
                        $('#window-box-t').scrollTop($('#msgList').height());
                    }
                });
            }
            else {
                $('#msgList').html('');
            }
        }
    },
    //创建消息列表html
    createMsgListHtml: function (msgList) {
        var msgHtml = '';
        $.each(msgList, function (index, el) {
            msgHtml = '<div class="clearfix chat-msg">'
                + '<div class="' + (el.FromUser == WebChat.loginUser.UserName ? 'msg-right' : 'msg-left') + ' message-content"><i class="message-sj"></i>'
                + '<div class="chat-box-detail msg-lt">' + WebChat.replaceEm(el.MsgContent) + '</div></div></div>'
                + msgHtml;
        });

        return msgHtml;
    },
    //追加消息列表html
    appendMsgHtml: function (type, msgList, chat) {
        if (!chat.msgListHtml) {
            chat.msgListHtml = "";
        }

        $.each(msgList, function (index, el) {
            var msgHtml = '<div class="clearfix chat-msg">'
                + '<div class="' + (el.FromUser == WebChat.loginUser.UserName ? 'msg-right' : 'msg-left') + ' message-content"><i class="message-sj"></i>'
                + '<div class="chat-box-detail msg-lt">' + WebChat.replaceEm(el.MsgContent) + '</div></div></div>'

            if (type == 0) {
                chat.msgListHtml = msgHtml + chat.msgListHtml;
            } else {
                chat.msgListHtml = chat.msgListHtml + msgHtml;
            }
        });

        return chat;
    },
    //显示群组明细
    showGroupDetail: function (index) {
        if (WebChat.curGroupIndex == index)
            return;
        change(1);

        $('g_' + index).addClass('active').siblings('.active').removeClass('active');

        WebChat.curGroupIndex = index;
        var group = WebChat.groupList[WebChat.curGroupIndex];

        $.getJSON(WebChat.serviceUrl + 'GETGROUPUSER', { P1: group.ID }, function (resultData) {
            if (resultData.ErrorMsg == "") {
                var groupUserHtml = '';
                if (resultData.Result.length > 0) {
                    $.each(resultData.Result, function (index, el) {
                        groupUserHtml += '<li id="gu_' + el.UserName + '"><div class="clearfix">'
                            + '<img src="/ViewV5/images/IM/user2.jpg" class="img-30 pull-left">'
                            + '<div class="pull-left" style="margin-left:15px">'
                            + '<h5>' + el.UserRealName + '</h5>'
                            + '<p>' + el.zhiwu + '</p>'
                            + '</div></div></li>';
                    });
                }
                $('#toGroupName').text(group.GroupName);
                $('#groupUserUl').html(groupUserHtml);

                if (group.CRUser == WebChat.loginUser.UserName) {
                    $('#btnExitGroup').html('解散群组').attr('tag', 'delete');
                }
                else {
                    $('#btnExitGroup').html('退出群组').attr('tag', 'exit');
                }
                $('#btnExitGroup').show();
                $('#btnStartGroupChat').show();

                group.personList = resultData.Result;
                WebChat.groupList[WebChat.curGroupIndex] = group;
            }
        });
    },
    //获取未读消息
    getNoReadMsg: function () {
        if (WebChat.isStopGetMsg) {
            setTimeout('WebChat.getNoReadMsg()', 100);
            return;
        }

        WebChat.isStopGetMsg = true;
        $.getJSON(WebChat.serviceUrl + 'GETNOREADMSG', {}, function (resultData) {
            if (resultData.ErrorMsg == "") {
                var lastChat = null;
                var lastMsg = null;
                var noReadMsgList = [];
                //遍历普通消息
                $.each(resultData.Result, function (index, el) {
                    if (lastChat && el.ConverId != lastChat.lastConverId) {
                        WebChat.updateChatByNoReadMsg(lastChat, lastMsg, noReadMsgList);
                        lastChat = null;
                        lastMsg = null;
                        noReadMsgList = [];
                    }

                    lastMsg = el;
                    noReadMsgList.push(el);

                    if (!lastChat) {
                        lastChat = WebChat.findChatByConverIdAndType(0, el.ConverId);

                        if (lastChat == null) {
                            lastChat = WebChat.createChat(el, el.FromUser, el.FromUserRealName);
                        }
                    }
                });
                if (lastChat) {
                    WebChat.updateChatByNoReadMsg(lastChat, lastMsg, noReadMsgList);
                }

                lastChat = null;
                lastMsg = null;
                noReadMsgList = [];

                //遍历群组消息
                $.each(resultData.Result1, function (index, el) {
                    if (lastChat && el.GroupId != lastChat.GroupId) {
                        WebChat.updateChatByNoReadMsg(lastChat, lastMsg, noReadMsgList);
                        lastChat = null;
                        lastMsg = null;
                        noReadMsgList = [];
                    }

                    lastMsg = el;
                    lastMsg.ConverId = lastMsg.GroupId;
                    lastMsg.MsgType = 1;
                    noReadMsgList.push(el);

                    if (!lastChat) {
                        lastChat = WebChat.findChatByConverIdAndType(1, el.GroupId);

                        if (lastChat == null) {
                            lastChat = WebChat.createChat(el, el.FromUser, el.FromUserRealName);
                        }
                    }
                });
                if (lastChat) {
                    WebChat.updateChatByNoReadMsg(lastChat, lastMsg, noReadMsgList);
                }

                if ((resultData.Result && resultData.Result.length > 0) || (resultData.Result1 && resultData.Result1.length > 0)) {
                    //重新对会话列表进行排序
                    if (WebChat.recentChatList) {
                        WebChat.recentChatList.sort(function (a, b) {
                            return b.CRDate.localeCompare(a.CRDate);
                        });
                    }

                    //播放提示音
                    WebChat.playMsgAudio();

                    //更新会话列表html
                    WebChat.showRecentChatList();

                    //显示未读消息数量
                    WebChat.setTotalNoMsgCount();
                }

                WebChat.isStopGetMsg = false;
                setTimeout('WebChat.getNoReadMsg()', 500);
            }
            else {
                WebChat.isStopGetMsg = false;
                setTimeout('WebChat.getNoReadMsg()', 500);
            }
        });
    },
    //根据会话类型和会话ID查询会话
    findChatByConverIdAndType: function (msgType, converId) {
        var chat = null;
        $.each(WebChat.recentChatList, function (index, el) {
            if (el.MsgType == msgType && el.ConverId == converId) {
                chat = el;
                return false;
            }
        });

        return chat;
    },
    //根据会话类型和对方联系人信息查询会话
    findChatByTypeAndPerson: function (msgType, userCodeOrGroupId) {
        //从最近会话列表中查询会话
        var chat = null;
        var chatIndex = null;
        $.each(WebChat.recentChatList, function (index, el) {
            if (el.MsgType == msgType) {
                if ((el.MsgType == 0 && (el.ToUser == userCodeOrGroupId || el.FromUser == userCodeOrGroupId))
                    || (el.MsgType == 1 && el.GroupId == userCodeOrGroupId)) {
                    chat = el;
                    chatIndex = index;
                    return false;
                }
            }
        });
        if (chat)
            return { chat: chat, index: chatIndex };
        else
            return null;
    },
    //根据未读消息创建会话
    createChat: function (msg, toUser, toUserRealName) {
        var chat = {};

        chat.ComId = WebChat.loginUser.ComId;
        chat.MsgType = msg.MsgType;
        chat.GroupId = msg.GroupId;
        chat.GroupName = msg.GroupName;
        chat.FromUser = toUser;
        chat.FromUserRealName = toUserRealName;
        chat.ToUser = msg.FromUser;
        chat.ToUserRealName = msg.FromUserRealName;
        chat.MsgCount = 0;
        chat.MsgContent = msg.MsgContent;
        chat.MsgContentType = msg.MsgContentType;
        chat.CRDate = msg.CRDate;
        chat.ConverId = msg.ConverId;

        return chat;
    },
    //根据对方联系人信息创建会话
    createNewChat: function (msgType, personOrGroup) {
        var chat = {};

        chat.ComId = WebChat.loginUser.ComId;
        chat.MsgType = msgType;

        if (msgType == 0) {
            chat.GroupId = null;
            chat.GroupName = null;
            chat.FromUser = WebChat.loginUser.UserName,
            chat.FromUserRealName = WebChat.loginUser.UserRealName;
            chat.ToUser = personOrGroup.UserName;
            chat.ToUserRealName = personOrGroup.UserRealName;
            chat.ConverId = -1;
        }
        else {
            chat.GroupId = personOrGroup.ID;
            chat.GroupName = personOrGroup.GroupName;
            chat.ConverId = personOrGroup.ID;
        }

        chat.MsgCount = 0;
        chat.MsgContent = '';
        chat.MsgContentType = 0;
        chat.CRDate = new Date();

        return chat;
    },
    //根据对方联系人开始会话
    startChat: function (msgType, personOrGroup) {
        var result = WebChat.findChatByTypeAndPerson(msgType, msgType == 0 ? personOrGroup.UserName : personOrGroup.ID);

        if (result == null) {
            result = { chat: WebChat.createNewChat(msgType, personOrGroup) };
        }
        else {

            WebChat.recentChatList.splice(result.index, 1);
        }

        //将对话置顶并选中
        WebChat.recentChatList.unshift(result.chat);
        WebChat.showRecentChatList();
        change(0);
        $('.recent-li')[0].click();
    },
    //根据未读消息更新会话
    updateChatByNoReadMsg: function (chat, msg, msgList) {
        chat.GroupId = msg.GroupId;
        chat.GroupName = msg.GroupName;
        chat.FromUser = msg.FromUser;
        chat.FromUserRealName = msg.FromUserRealName;
        chat.ToUser = WebChat.loginUser.UserName;
        chat.ToUserRealName = WebChat.loginUser.UserRealName;
        chat.MsgCount += msgList.length;
        chat.MsgContent = msg.MsgContent;
        chat.MsgContentType = msg.MsgContentType;
        chat.CRDate = msg.CRDate;
        chat.ConverId = msg.ConverId;
        chat = WebChat.appendMsgHtml(1, msgList, chat);
        chat.msgList = msgList.concat(chat.msgList);

        $.each(WebChat.recentChatList, function (index, el) {
            if (WebChat.curChat && WebChat.curChat.MsgType == el.MsgType && WebChat.curChat.ConverId == el.ConverId) {
                el.MsgCount = chat.MsgCount = 0;
                var lastMsgId = (WebChat.curChat.msgList && WebChat.curChat.msgList.length > 0) ? WebChat.curChat.msgList[0].ID : 0;
                $.getJSON(WebChat.serviceUrl + 'GETCONVERSATION', {
                    p: lastMsgId,
                    t: 1,
                    P1: WebChat.curChat.ConverId,
                    P2: WebChat.curChat.MsgType
                }, function (resultData) {
                    if (resultData.ErrorMsg == "") {
                        //将新消息显示在列表框
                        WebChat.curChat = WebChat.appendMsgHtml(1, resultData.Result, WebChat.curChat);
                        $('#msgList').html(WebChat.curChat.msgListHtml);
                        $('#window-box-t').scrollTop($('#msgList').height());

                        //将新的消息加入数组
                        if (WebChat.curChat.msgList) {
                            WebChat.curChat.msgList = resultData.Result.concat(WebChat.curChat.msgList);
                        }
                        else {
                            WebChat.curChat.msgList = resultData.Result;
                        }

                        el = WebChat.curChat;
                    }
                });

                return false;
            }

            if (el.MsgType == chat.MsgType && el.ConverId == chat.ConverId) {
                el = chat;
                return false;
            }
        });

        return chat;
    },
    //显示最近会话列表
    showRecentChatList: function () {
        var recentHtml = '';
        $.each(WebChat.recentChatList, function (index, el) {
            el.chatUser = {};
            if (el.MsgType == 1) {
                el.chatUser.GroupId = el.GroupId;
                el.chatUser.GroupName = el.GroupName;
                el.chatUser.ConverId = el.GroupId;
            }
            else {
                el.chatUser.UserName = el.FromUser == WebChat.loginUser.UserName ? el.ToUser : el.FromUser;
                el.chatUser.UserRealName = el.FromUser == WebChat.loginUser.UserName ? el.ToUserRealName : el.FromUserRealName;
            }

            recentHtml += '<li class="recent-li" id="r_' + index + '" >'
                + '<div class="chart-div"><i class="icon-close-chat bg font20"></i>'
                + '<div class="chat-time pull-right"><span class="no-read-count">' + (el.MsgCount > 0 ? el.MsgCount : '') + '</span></div>'
                + '<div class="pull-left img-ridus ml5"><img src="/ViewV5/images/IM/user2.jpg"></div>'
                + '<div class="pull-left ml10 chat-info">'
                + '<h4>' + (el.MsgType == 1 ? el.chatUser.GroupName : el.chatUser.UserRealName) + '</h4>'
                + '<p>' + el.MsgContent + '</p>'
                + '</div></div> </li>';
        });

        //显示会话列表
        $('#recentUl').html(recentHtml);

        //显示会话内容
        $('.recent-li').click(function () {
            $(this).addClass('active').siblings('.active').removeClass('active');
            $(this).find('.no-read-count').text('');
            var index = $(this).attr('id').substr(2);
            WebChat.setCurChat(index);
        });

        //删除会话
        $('.icon-close-chat').click(function () {
            var index = $(this).parent().parent().attr('id').substr(2);
            WebChat.recentChatList.splice(index, 1);
            WebChat.showRecentChatList();

            if (WebChat.curRecentIndex == index) {
                $('#toUserName').text('');
                $('#msgList').html('');
                $('#imgEditor').val('');
            }
        });
    },
    showGroupEdit: function (groupIndex) {
        if (groupIndex) {
            var group = WebChat.groupList[groupIndex];
            var groupUserHtml = '';
            if (group.personList && group.personList.length > 0) {
                $.each(group.personList, function (index, el) {
                    groupUserHtml += '<li id="gp_' + el.UserName + '">'
                        + '<img src="/ViewV5/images/IM/user2.jpg" class="pic-40">'
                        + '<span style="display:block;font-size:12px;">' + el.UserRealName + '</span>'
                        + '</li>';
                });
            }
            $('#addGroupName').val(group.GroupName);
            $('#divAddGroupPersonCount').text(resultData.Result.length);
            $('#divAddGroupPerson').html(groupUserHtml);

            WebChat.editGroup = group;
        }
        else {
            $('#addGroupName').val('');
            $('#divAddGroupPersonCount').text('0');
            $('#divAddGroupPerson').html('');

            WebChat.editGroup = {};
            WebChat.editGroup.personList = [];
        }

        $('#divGroupEdit').show();
    },
    addPersonToGroup: function (person) {
        if (person.UserName == WebChat.loginUser.UserName)
            return;

        var isExist = false;
        $.each(WebChat.editGroup.personList, function (index, el) {
            if (el.UserName == person.UserName) {
                isExist = true;
                return false;
            }
        });

        if (!isExist) {
            WebChat.editGroup.personList.push(person);
            $('#divAddGroupPersonCount').text(WebChat.editGroup.personList.length);
            var personHtml = '<li id="gp_' + person.UserName + '">'
                            + '<img src="/ViewV5/images/IM/user2.jpg" class="pic-40">'
                            + '<span style="display:block;font-size:12px;">' + person.UserRealName + '</span>'
                            + '</li>';
            $('#divAddGroupPerson').html($('#divAddGroupPerson').html() + personHtml);
        }
    },
    rebindGroup: function () {
        $.getJSON(WebChat.serviceUrl + 'GETGROUPLIST', {}, function (resultData) {
            if (resultData.ErrorMsg == "") {
                WebChat.groupList = resultData.Result;
                var groupHtml = '';
                $.each(WebChat.groupList, function (index, el) {
                    groupHtml += '<li class="group-li" id="g_' + index + '" >'
                        + '<div class="chart-div">'
                        + '<div class="pull-left img-ridus ml5"><img src="/ViewV5/images/IM/user2.jpg"></div>'
                        + '<div class="pull-left ml10 chat-info grap">'
                        + '<h4>' + el.GroupName + '</h4>'
                        + '</div></div></li>';
                });
                $('#groupUl').html(groupHtml);

                $('.group-li').click(function () {
                    $(this).addClass('active').siblings('.active').removeClass('active');
                    var index = $(this).attr('id').substr(2);
                    WebChat.showGroupDetail(index);
                });
            }
        });
    },
    searchPerson: function (name) {
        if (name) {
            console.log('search', name);
            $.getJSON(WebChat.serviceUrl + 'SEARCHPERSON', { P1: name }, function (resultData) {
                if (resultData.ErrorMsg == "") {
                    var personList = resultData.Result;
                    var groupList = resultData.Result1;

                    var personHtml = '';
                    $.each(personList, function (index, el) {
                        personHtml = personHtml + '<li id="sp_' + index + '"><div class="select2-result-label"><img src="/ViewV5/images/IM/user2.jpg" class="img-30">'
                            + el.UserRealName + '</div></li>';

                    });
                    $('#ulSearchPersonList').html(personHtml);
                    $('#ulSearchPersonList li').click(function () {
                        var index = $(this).attr('id').substr(3);
                        WebChat.startChat(0, personList[index]);
                    });

                    var groupHtml = '';
                    $.each(groupList, function (index, el) {
                        groupHtml = groupHtml + '<li id="sg_' + index + '"><div class="select2-result-label"><img src="/ViewV5/images/IM/user2.jpg" class="img-30">'
                            + el.GroupName + '</div></li>';
                    });
                    $('#ulSearchGroupList').html(groupHtml);
                    $('#ulSearchGroupList li').click(function () {
                        var index = $(this).attr('id').substr(3);
                        WebChat.startChat(1, groupList[index]);
                    });
                }
            });
        }
        else {
            $('#ulSearchPersonList').html('');
            $('#ulSearchGroupList').html('');
        }
    },
    setTotalNoMsgCount: function () {
        if (window.parent && window.parent.setNoReadMsgCount) {
            var totalMsgCount = 0;
            $.each(WebChat.recentChatList, function (index, el) {
                totalMsgCount += el.MsgCount > 0 ? el.MsgCount : 0;
            });
            window.parent.setNoReadMsgCount(totalMsgCount);
        }
    },
    playMsgAudio: function () {
        if (WebChat.isAudioPlay) {
            WebChat.msgAudio.play();
        }
    },
    replaceEm: function (str) {
        str = str.replace(/\</g, '&lt;');
        str = str.replace(/\>/g, '&gt;');
        str = str.replace(/\n/g, '<br/>');
        str = str.replace(/\[em_([0-9]*)\]/g, '<img src="qqface/face/$1.gif" border="0" />');
        return str;
    }
};

$(function () {
    WebChat.init();
});


//-------------------left_ul点击切换------------------
function change(num) {
    for (var i = 0; i < left_ul.length; i++) {
        if (i == num) {
            left_b[i].style.display = 'block';
            left_ul[i].className = 'active';
            panel_r[i].style.display = 'block';
            panel_r[i].className = 'panel-right';

        } else {
            left_b[i].style.display = 'none';
            left_ul[i].className = '';
            panel_r[i].style.display = 'none';
        }
    }
}

//弹出个人信息 关闭 展示使用
$(".look-zl").click(function () {
    if (WebChat.curChat.MsgType == 1) {
        var groupId = WebChat.curChat.chatUser.GroupId;
        $.each(WebChat.groupList, function (index, el) {
            if (el.ID == groupId) {
                WebChat.showGroupDetail(index);
                return;
            }
        });
    }
    else {
        var userName = WebChat.curChat.chatUser.UserName;
        $.getJSON(WebChat.serviceUrl + 'GETPERSON', { P1: userName }, function (resultData) {
            if (resultData.ErrorMsg == "" && resultData.Result) {
                var person = resultData.Result;
                person.dept = resultData.Result1;

                $('#personDetailImage').attr('src', person.txurl);
                $('#personRealName').html(person.UserRealName);
                $('#personZhiwu').html(person.zhiwu);
                $('#personDept').html(person.dept.DeptName);
                $('#personMobile').html(person.mobphone);
                $('#personTel').html('');
                $('#personEmail').html(person.mailbox);
                $('#personInDate').html(person.EntryDate);
                $('#personBirthday').html(person.Birthday);
                $('#personQQ').html(person.QQ);
                $('#personIntro').html(person.Usersign);

                $(".personage").show();
            }
        });
    }
})
$(".close-zl").click(function () {
    $(".personage").hide();
})
// 搜索框鼠标点击显示 仅为做效果展示 可删除
$(".input-search").focus(function () {
    $(".search-resul").show();
})
$(".input-search").blur(function () {
    setTimeout(function () { $(".search-resul").hide(); }, 500);
});