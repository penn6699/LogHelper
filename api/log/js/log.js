/// <reference path="jquery.min.js" />
/// <reference path="bootstrap/bootstrap.min.js" />
/// <reference path="bootstrap-select/bootstrap-select.min.js" />
/// <reference path="bootstrap-table/bootstrap-table.min.js" />
/// <reference path="json-viewer/jquery.json-viewer.js" />
/// <reference path="laydate/laydate.js" />
/// <reference path="layer/layer.js" />

//获取来自地址栏（URL）中的一个参数值
function getQueryString(key) {
    /// <summary>获取来自地址栏（URL）中的某个参数值</summary>
    /// <param name="key" type="String">参数名称</param>
    /// <returns type="String" />
    var regExp = new RegExp("[\?|\&]" + key + "=([^&]+)"); //  new RegExp("[\?\&]" + key + "=([^\&]+)(&|$)");
    var matchStrings = window.location.search.match(regExp);
    return matchStrings && decodeURIComponent(matchStrings[1]);
}

//字符串的format 调用方法如："第一名是{0}, 第二名是{1}".format("张三", "王五")); 'name={name},sex={sex}'.format({name:'xxx',sex:1});
String.prototype.format = function () {
    var args = arguments;
    //如果第一个参数是对象
    var isObject = typeof (args[0]) === 'object';
    var re = isObject ? /\{(\w+)\}/g : /\{(\d+)\}/g;
    var isNull = function (value) {
        if (typeof (value) === 'object') {
            return JSON.stringify(value) || "";
        }
        return value == null ? "" : value;
    };
    return this.replace(re, function (m, key) {
        if ((isObject && args[0][key] === undefined) || (!isObject && args[key] === undefined)) {
            return "{" + key + "}";
        }
        return isObject ? isNull(args[0][key]) : isNull(args[key]);
    });
};

//去除字符串两端的空格
String.prototype.trim = function () {
    return this.replace(/(^\s*)|(\s*$)/g, '');
};

//数字的四舍五入
Number.prototype.round = function (number) {
    return Math.round(this * Math.pow(10, number)) / Math.pow(10, number);
};

//全局唯一标识符GUID,类似.net中的NewID();
function guid() {
    function S4() {
        return (((1 + Math.random()) * 0x10000) | 0).toString(16).substring(1);
    }
    return (S4() + S4() + "-" + S4() + "-" + S4() + "-" + S4() + "-" + S4() + S4() + S4());
}
//数据容量单位转换(kb,mb,gb,tb)
function getFileSize(fileSize, withUnit) {

    if (fileSize === 0) return '0 B';
    var k = 1024,
        sizes = ['B', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'],
        i = Math.floor(Math.log(fileSize) / Math.log(k));

    if (!Number.prototype.round) {
        Number.prototype.round = function (number) {
            return Math.round(this * Math.pow(10, number)) / Math.pow(10, number);
        };
    }

    withUnit = typeof (withUnit) === "undefined" ? true : withUnit;
    if (withUnit) {
        return (fileSize / Math.pow(k, i)).round(2) + ' ' + sizes[i];
    }
    else {
        return (fileSize / Math.pow(k, i)).round(2);
    }

}

//检测步骤
var LogHelper = (function () {
    var _log_action = "", $table;
    var _ApiHost = '';
    var _dal = {
        "GetLogList": function (par, callback) {
            $.ajax({
                url: _ApiHost + "/Log/SearchLogDatabase",
                type: "get",
                data: par,
                dataType: "json",
                success: function (res) {
                    callback(res || []);
                }
            });
        }
        , "GetLogDataList": function (par, callback) {
            $.ajax({
                url: _ApiHost + "/Log/GetLogs",
                type: "post",
                data: par,
                dataType: "json",
                success: function (res) {
                    callback(res || []);
                }
            });
        }
        , "GetLogData": function (path, id, callback) {
            $.ajax({
                url: _ApiHost + "/Log/GetLogData",
                type: "get",
                data: { "path": path, "id": id },
                dataType: "json",
                success: function (res) {
                    callback(res || []);
                }
            });
        }
    };


    function init(callback) {
        var _this = this;
        _ApiHost = location.href.split('/log/index.html')[0];
        _log_action = getQueryString("log_action") || "";
        $table = $("#search_table");

        var now = new Date();


        var monthMaxDate = new Date(now.getFullYear() + '-' + fillDate_0((now.getMonth() + 1)) + '-01');
        monthMaxDate.setMonth(now.getMonth() + 1);
        monthMaxDate.setDate(monthMaxDate.getDate() - 1);

        function fillDate_0(str) {
            var _str = '00' + str;
            return _str.substr(_str.length - 2, 2);
        }


        laydate.render({
            elem: '#log_db_search [name="bDate"]'
            , format: 'yyyy-MM-dd'
            , value: now.getFullYear() + '-' + fillDate_0((now.getMonth() + 1)) + '-01'
        });
        laydate.render({
            elem: '#log_db_search [name="eDate"]'
            , format: 'yyyy-MM-dd'
            , value: monthMaxDate.getFullYear() + '-' + fillDate_0((monthMaxDate.getMonth() + 1)) + '-' + fillDate_0(monthMaxDate.getDate())
        });


        $('#log_db_search [name="Level"]')
            .attr('multiple', 'multiple')
            .selectpicker({
                'actionsBox': true
            })
            .selectpicker('val', [])
            .trigger("change");


        $table.bootstrapTable({
            toolbar: '#toolbar',
            striped: true,
            cache: false,
            pagination: true,
            sidePagination: "client",
            pageNumber: 1,
            pageSize: 10,
            pageList: [10, 15, 20, 30, 40, 50, 75, 100],

            sortable: true,
            sortName: 'date',
            sortOrder:'desc',
            search: false,
            showColumns: true,
            showRefresh: true,
            minimumCountColumns: 2,
            uniqueId: "fileName",
            onRefresh: function (params) { refresh(); },
            onSearch: function (text) { },
            columns: [
                { checkbox: true },

                { title: '序号', width: '40px',align: 'center', formatter: function (value, row, index) {return index + 1; } },

                { field: 'date', title: '日期', align: 'center', width: '60px', sortable: true },
                { field: 'level', title: '等级', width: '100px', sortable: true },
                { field: 'fileName', title: '文件名', sortable: true },


                {
                    field: 'fileSize', title: '文件大小', align: "center", width: '150px', sortable: true
                    , formatter: function (value, row, index) {
                        return getFileSize(value);
                    }
                },
                { field: 'fileUpdateTime', title: '更新时间', align: "center", width: '150px', sortable: true },
                { field: 'filePath', title: '文件Path', sortable: true, visible: false }
            ]

        });

        if (callback) {
            callback.call(_this);
        }

    }

    //刷新
    function refresh() {
        var par = { bDate: "", eDate: "", level: "" };
        par.bDate = $('#log_db_search [name="bDate"]').val();
        par.eDate = $('#log_db_search [name="eDate"]').val();

        par.level = $('#log_db_search [name="Level"]').selectpicker('val');
        par.level = Array.isArray(par.level) ? par.level.join(',') : '';

        _dal.GetLogList(par, function (rData) {
            //console.log(rData);
            $table.bootstrapTable("load", rData);
        });
    }

    //显示
    function showLogsList() {
        var _rows = $table.bootstrapTable('getSelections');
        if (_rows.length < 1) {
            layer.alert('请至少选择一个日志数据库'); return false;
        }


        var tableID = guid();
        var toolID = guid();
        //tmpl
        {
            var tmpl = ['<div class="labware-list-wrap" style="padding:10px 10px 80px 10px;">'];

            tmpl.push('<div class="panel panel-default" style="margin-bottom:10px;">');
            tmpl.push('<div class="panel-heading"><span class="panel-title">查询</span>');
            tmpl.push('<span class="pull-right" role="button" data-toggle="collapse" data-target="#sample_search_{0}" style="color:#ababab;padding-top: 1px;">'.format(toolID));
            tmpl.push('<i class="glyphicon glyphicon-chevron-up" title="收起"></i><i class="glyphicon glyphicon-chevron-down" title="展开"></i></span></div>');
            tmpl.push('<div class="panel-body collapse in search-wrap " id="sample_search_{0}">'.format(toolID));
            tmpl.push('<div class="container" style="width:100%;"><div class="row">');

            tmpl.push('<div class="col-sm-8 col-md-8"><div class="input-group"><div class="input-group-addon">日期</div>');
            tmpl.push('<input type="text" class="form-control" name="bTime" value="" placeholder="开始日期" title="开始日期" readonly="readonly" style="cursor: pointer;background-color: #ffffff;" />');
            tmpl.push('<div class="input-group-addon" style="border-left:none;border-right:none;padding:0 5px;">至</div>');
            tmpl.push('<input type="text" class="form-control" name="eTime" value="" placeholder="结束日期" title="结束日期" readonly="readonly" style="cursor: pointer;background-color: #ffffff;" />');
            tmpl.push('</div></div>');

            tmpl.push('<div class="col-sm-4 col-md-4"><div class="input-group"><div class="input-group-addon">等级</div>');
            tmpl.push('<select class="form-control" name="Level"></select></div></div>');

            tmpl.push('<div class="col-sm-4 col-md-4"><div class="input-group"><div class="input-group-addon">类型</div>');
            tmpl.push('<input type="text" class="form-control" name="Type" value="" /></div></div>');

            tmpl.push('<div class="col-sm-4 col-md-4"><div class="input-group"><div class="input-group-addon">访问IP</div>');
            tmpl.push('<input type="text" class="form-control" name="UserIP" value="" /></div></div>');

            tmpl.push('<div class="col-sm-4 col-md-4"><div class="input-group"><div class="input-group-addon">操作人ID</div>');
            tmpl.push('<input type="text" class="form-control" name="UserID" value="" /></div></div>');

            tmpl.push('<div class="col-sm-4 col-md-4"><div class="input-group"><div class="input-group-addon">操作人</div>');
            tmpl.push('<input type="text" class="form-control" name="UserName" value="" /></div></div>');

            tmpl.push('<div class="col-sm-4 col-md-4"><div class="input-group"><div class="input-group-addon">信息</div>');
            tmpl.push('<input type="text" class="form-control" name="Message" value="" /></div></div>');

            tmpl.push('<div class="col-sm-4 col-md-4"><div class="input-group"><div class="input-group-addon">数据</div>');
            tmpl.push('<input type="text" class="form-control" name="Data" value="" /></div></div>');

            tmpl.push('<div class="col-sm-12 col-md-12"><div class="btn-group">');
            tmpl.push('<button class="btn btn-success btn-logs-search">查询</button></div></div>');

            tmpl.push('</div></div></div></div>');

            tmpl.push('<div id="{0}" class="tool-bar"><div class="btn-group">'.format(toolID));
            //tmpl.push('<button class="btn btn-success btn-logs-search">查询</button>');
            tmpl.push('</div></div><table id="{0}"></table>'.format(tableID));


            tmpl.push('</div>');
        }


        layer.open({
            title: '<b>日志列表</b>'
            , type: 1
            , maxmin: true
            , shade: 0
            , scrollbar: false

            , content: tmpl.join("")
            , area: ["99%", "99%"]
            , success: function (layero, layerIndex) {
                //layer.full(layerIndex);

                laydate.render({
                    elem: layero.find('[name="bTime"]')[0]
                    , type: 'datetime'
                    , format: 'yyyy-MM-dd HH:mm:ss'
                });
                laydate.render({
                    elem: layero.find('[name="eTime"]')[0]
                    , type: 'datetime'
                    , format: 'yyyy-MM-dd HH:mm:ss'
                });


                var _levelObj = {};
                $.each(_rows, function (ind, row) {
                    _levelObj[row.level] = row.level;
                });
                var _levelHtml = [];
                $.each(_levelObj, function (key, val) {
                    _levelHtml.push('<option value="{0}">{0}</option>'.format(key));
                });
                layero.find('[name="Level"]')
                    .html(_levelHtml.join(''))
                    .attr('multiple', 'multiple')
                    .selectpicker({
                        'actionsBox': true
                    })
                    .selectpicker('val', [])
                    .trigger("change");


                var $logTable = $("#" + tableID);
                var _toolbarMoreSelect = "#" + toolID;

                $logTable.bootstrapTable({

                    toolbar: _toolbarMoreSelect,
                    striped: true,
                    cache: false,
                    pagination: true,
                    sidePagination: "client",
                    pageNumber: 1,
                    pageSize: 8,
                    pageList: [3, 5, 6, 7, 8, 9, 10, 15, 20, 25, 30, 40, 50, 60, 70, 100, 150, 200, 250, 300, 400, 500],

                    sortable: true,

                    search: false,
                    showColumns: true,
                    showRefresh: true,
                    minimumCountColumns: 2,
                    uniqueId: "ID",
                    onRefresh: function (params) {
                        loadLogs();
                    },
                    onSearch: function (text) { },
                    onPostBody: function () {

                    },
                    columns: [
                        //{ checkbox: true },
                        {
                            align: 'center', width: '80',
                            events: {
                                "click .btn-msg": function (event, value, row, index) {

                                    layer.open({
                                        title: (index + 1) + '_信息'+ (row.fileName ? '_【' + row.fileName + '】' : '') 
                                        , type: 1 //iframe
                                        , area: ['40%', '60%']
                                        , shade: 0
                                        , moveOut: true
                                        , maxmin: true
                                        , success: function (layero, layerIndex) {
                                            // layer.full(index);
                                        }
                                        , content: '<div style="padding:10px;">' + (row.Message + "").replace(/\<br\/\>/g, '<br/><br/>') + '</div>'
                                    });

                                }
                                , "click .btn-data": function (event, value, row, index) {

                                    _dal.GetLogData(row.filePath, row.ID, function (rDatas) {
                                        var _dataStr = (((rDatas[0] || {}).Data || "") + "").replace(/\<br\/\>/g, '<br/><br/>');
                                        if (!_dataStr) {
                                            layer.msg("没有相关数据", { time: 600 }); return false;
                                        }

                                        layer.open({
                                            title: (index + 1) + '_数据'+ (row.fileName ? '_【' + row.fileName + '】' : '') 
                                            , type: 1
                                            , area: ['80%', '80%']
                                            , scrollbar: false
                                            , shade: 0
                                            , moveOut: true
                                            , maxmin: true
                                            , content: '<div class="content" style="padding:10px;">' + _dataStr + '</div>'
                                            , success: function (layero, layerIndex) {
                                                // layer.full(index);
                                                try {
                                                    var dataJson = JSON.parse(_dataStr);
                                                    layero.find('.content').html('<pre class="json-renderer" style="padding:10px;" ></pre>');

                                                    layero.find('.json-renderer').jsonViewer(dataJson, { collapsed: false, withQuotes: true, withLinks: false });

                                                }
                                                catch (e) {
                                                    layero.find('.content').html(_dataStr);
                                                }



                                            }

                                        });
                                    });



                                }

                            },
                            formatter: function (value, row, index) {
                                var btnTmpl = [];

                                btnTmpl.push('<div class="btn-group">');
                                btnTmpl.push('<button type="button" class="btn btn-success btn-xs btn-msg" >信息</button>');
                                btnTmpl.push('<button type="button" class="btn btn-info btn-xs btn-data" >数据</button>');
                                btnTmpl.push('</div>');

                                return btnTmpl.join("");
                            }
                            , cellStyle: function (value, row, index) {
                                return {
                                    css: {
                                        "min-width": "80px"
                                    }
                                };
                            }

                        },
                        { field: 'index', width: '40px', align: 'center', formatter: function (value, row, index) { return index + 1; } },
                        
                        {
                            field: 'fileName', title: '数据库', width: '80px', sortable: true, visible: false
                            , cellStyle: function (value, row, index) {
                                return {
                                    css: {
                                        "min-width": "80px"
                                    }
                                };
                            }
                        },
                        {
                            field: 'Time', title: '时间', width: '150px', align: 'center', sortable: true
                            , cellStyle: function (value, row, index) {
                                return {
                                    css: {
                                        "min-width": "150px"
                                    }
                                };
                            }
                        },
                        {
                            field: 'Level', title: '等级', width: '85px', align: 'center', sortable: true
                            , cellStyle: function (value, row, index) {
                                return {
                                    css: {
                                        "min-width": "85px"
                                    }
                                };
                            }
                        },
                        {
                            field: 'Type', title: '类型', width: '160px', sortable: true
                            , cellStyle: function (value, row, index) {
                                return {
                                    css: {
                                        "min-width": "160px"
                                    }
                                };
                            }
                        },
                        {
                            field: 'UserIP', title: '访问IP', width: '90px', sortable: true
                            , cellStyle: function (value, row, index) {
                                return {
                                    css: {
                                        "min-width": "90px"
                                    }
                                };
                            }
                        },
                        {
                            field: 'UserID', title: '操作人ID', width: '90px', sortable: true
                            , cellStyle: function (value, row, index) {
                                return {
                                    css: {
                                        "min-width": "90px"
                                    }
                                };
                            }
                        },
                        {
                            field: 'UserName', title: '操作人', width: '120px', sortable: true
                            , cellStyle: function (value, row, index) {
                                return {
                                    css: {
                                        "min-width": "120px"
                                    }
                                };
                            }
                        }
                        //信息
                        , {
                            field: 'Message', title: '信息', width: '300px', sortable: true,
                            formatter: function (value, row, index) {
                                return '<div style="max-width:280px;white-space: nowrap;overflow: hidden;text-overflow: ellipsis;">' + (value + "").replace(/\<br\/\>/g, ' ') + '</div>';
                            }
                            , cellStyle: function (value, row, index) {
                                return {
                                    css: {
                                        "min-width": "300px"
                                    }
                                };
                            }
                        }

                    ]


                });

                //加载数据
                var loadLogs = function () {
                    var _par = {
                        'bTime': layero.find('[name="bTime"]').val()
                        , 'eTime': layero.find('[name="eTime"]').val()
                        , 'Level': ''
                        , 'path': ''
                        , 'Type': layero.find('[name="Type"]').val()
                        , 'UserID': layero.find('[name="UserID"]').val()
                        , 'UserName': layero.find('[name="UserName"]').val()
                        , 'UserIP': layero.find('[name="UserIP"]').val()
                        , 'Message': layero.find('[name="Message"]').val()
                        , 'Data': layero.find('[name="Data"]').val()
                    };

                    _par.Level = layero.find('[name="Level"]').selectpicker('val');
                    _par.Level = Array.isArray(_par.Level) ? _par.Level.join(',') : '';


                    var paths = [];
                    $.each(_rows, function (ind, row) {
                        var _Level = (row.fileName.split('_')[1] || '').split('.')[0] || '';
                        if (!_par.Level || (_par.Level && _par.Level.indexOf(_Level) > -1)) {
                            paths.push(row.filePath);
                        }
                    });
                    _par.path = paths.join(',');





                    if (!_par.path) {
                        $logTable.bootstrapTable('load', []);
                        return false;
                    }

                    _dal.GetLogDataList(_par, function (resRows) {
                        $logTable.bootstrapTable('load', resRows);
                    });

                };
                loadLogs();

                //查询
                layero.find('.btn-logs-search').click(function () {
                    loadLogs();
                });


            }

        });

    }


    return {
        init: init,
        refresh: refresh,
        showLogsList: showLogsList
    };


}());


















