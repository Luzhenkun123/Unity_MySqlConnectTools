using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEditor;
using UnityEngine;

public enum WindowType
{
    Login,Menu
}

public class SqlLoginWindow : EditorWindow
{
    public static SqlLoginWindow currentWindow;

    public const int LoginWidth = 300,LoginHeight=230;
    public const int MenuWidth = 300, MenuHeight = 500;

    WindowType windowType;
    #region 登录窗口UI组件
    string ip, port, database, uid,password;
    bool isAutoLogin;
    #endregion
    #region 菜单窗口UI组件
    string[] optionContents=new string[] { "增加","删除","查询","修改"};
    string[] actionContents = new string[] { "使用MySql语句", "可视化操作" };
    int optionSelect,actionSelect;
    string currentState="";
    #endregion

    #region 增加操作的UI组件
    string[] addContents = new string[] { "增加数据", "增加表" };
    int addSelect;

    //增加数据相关
    string addDataTableName;//要添加的数据的所在表
    string currentChooseTableName;//当前选择的表是哪个
    List<string> currentChooseTableColumnNames;//当前选择的表有哪些属性
    string[] addDataInputs;
    Vector2 addDataScrollViewPos;
    Rect addDataScrollViewOutRect = new Rect(0, 110, 300, 400), addDataScrollViewInRect;
    string addDataState;

    //增加表相关
    string addNewTableName;//新表名字
    int newTableFieldCount;//增加域
    Vector2 addTableScrollViewPos;
    Rect addTableScrollViewOutRect=new Rect(0,110,300,400), addTableScrollViewInRect;
    List<VariableData> variableDatasList = new List<VariableData>();
    string addTableState;
    #endregion
    #region 删除操作的UI组件
    string[] deleteContents = new string[] { "删除数据", "删除表" };
    int deleteSelect;
    string deleteDataState;
    //删除表相关
    List<string> tableNameList = null;
    string dropTableState;
    #endregion
    #region 查询操作
    string[] quaryContents = new string[] { "查询数据", "查询表" };
    int quarySelect;
    #endregion
    #region 修改操作
    string[] alterContents = new string[] { "修改数据", "修改表" };
    int alterSelect;
    string alterDataState;
    #endregion


    string sqlContent;//MySQL 语句
    string excuteResult;//执行结果



    [MenuItem("MySQL工具/打开面板")]
    public static void OpenWindow()
    {
        currentWindow = EditorWindow.GetWindowWithRect<SqlLoginWindow>(new Rect(0,0,LoginWidth,LoginHeight));
        currentWindow.titleContent =new GUIContent(MySqlManager.Instance.IsConnected()? "MySQL 管理器首页": "MySQL 管理器入口");
        currentWindow.Show();
    }

    private void OnGUI()
    {
        switch (windowType)
        {
            case WindowType.Login:
                DrawLoginWindow();
                break;
            case WindowType.Menu:
                DrawMenuWindow();
                break;
        }
    }

    /// <summary>
    /// 绘制登录页面
    /// </summary>
    private void DrawLoginWindow()
    {
        if(currentWindow)
            currentWindow.minSize = new Vector2(LoginWidth, LoginHeight);

        if (MySqlManager.Instance.IsConnected())
        {
            GUI.Label(new Rect(10, 30, 60, 20), "地址:");
            GUI.Label(new Rect(50, 30, 150, 20), ip);
            GUI.Label(new Rect(155, 30, 10, 20), ":");
            GUI.Label(new Rect(165, 30, 50, 20), port);

            GUI.Label(new Rect(10, 60, 60, 20), "库名:");
            GUI.Label(new Rect(50, 60, 150, 20), database);

            GUI.Label(new Rect(10, 90, 60, 20), "账号:");
            GUI.Label(new Rect(50, 90, 150, 20), uid);

            GUI.Label(new Rect(10, 120, 60, 20), "密码:");
            GUI.Label(new Rect(50, 120, 150, 20), "******");

            if (GUI.Button(new Rect(40, 175, 85, 30), "断开数据库"))
            {
                MySqlManager.Instance.Disconnect();
                password = "";//密码显示置空，需要重新输入
                currentWindow.titleContent.text = "MySQL 管理器入口";
                return;
            }
            if (GUI.Button(new Rect(170, 175, 85, 30), "进入管理工具"))
            {
                windowType = WindowType.Menu;
                currentWindow.titleContent.text = "MySQL 菜单页面";
            }

            currentState = "已连接";
            //显示连接状态
            GUI.Label(new Rect(130-(currentState.Length>=3? (currentState.Length-3)*5:0), 140, 150, 30), currentState);
        }
        else
        {
            GUI.Label(new Rect(10, 30, 60, 20), "地址:");
            ip = GUI.TextField(new Rect(50, 30, 150, 20), ip);
            GUI.Label(new Rect(205, 30, 10, 20), ":");
            port = GUI.TextField(new Rect(220, 30, 50, 20), port);

            GUI.Label(new Rect(30, 60, 60, 20), "库名:");
            database = GUI.TextField(new Rect(70, 60, 150, 20), database);

            GUI.Label(new Rect(30, 90, 60, 20), "账号:");
            uid = GUI.TextField(new Rect(70, 90, 150, 20), uid);

            GUI.Label(new Rect(30, 120, 60, 20), "密码:");
            password = GUI.TextField(new Rect(70, 120, 150, 20), password);

            isAutoLogin = GUI.Toggle(new Rect(230, 120, 100, 20), isAutoLogin, "自动登录");

            if (GUI.Button(new Rect(75, 175, 150, 30), "连接数据库"))
            {
                if (ip==null || ip == "" || 
                    port==null || port == "" || 
                    database==null || database == "" || 
                    uid==null || uid == "" ||
                    password==null || password == "")
                {
                    //Debug.Log("登录表单数据不能为空");
                    currentState = "表单不完整";
                    return;
                }
                currentState = "连接中";

                MySqlManager.Instance.Init(ip, port, database, uid, password);
                //MySqlManager.Instance.Connect();
                MySqlManager.Instance.ConnectAsync((state)=>
                {
                    if(state)
                    {
                        if (MySqlManager.Instance.IsConnected())//登陆成功则记录信息
                        {
                            PlayerPrefs.SetString("IP", ip);
                            PlayerPrefs.SetString("Port", port);
                            PlayerPrefs.SetString("Database", database);
                            PlayerPrefs.SetString("Uid", uid);
                            if (isAutoLogin)
                            {
                                PlayerPrefs.SetString("Password", password);
                                PlayerPrefs.SetInt("AutoLogin", 1);
                            }
                            else
                            {
                                PlayerPrefs.SetInt("AutoLogin", 0);
                            }
                            PlayerPrefs.Save();

                            currentWindow.titleContent.text = "MySQL 管理器首页";

                            //初始化一些窗口
                            InitChooseTableMenu();
                        }
                    }
                    else
                    {
                        currentState = "连接失败";
                    }
                });
            }
            //显示连接状态
            GUI.Label(new Rect(130 - (currentState.Length >= 3 ? (currentState.Length - 3) * 5 : 0), 140, 150, 30), currentState);
        }
    }
    /// <summary>
    /// 绘制菜单页面
    /// </summary>
    private void DrawMenuWindow()
    {
        currentWindow.minSize = new Vector2(MenuWidth, MenuHeight);
        if(GUI.Button(new Rect(250,0,50,20),"返回"))
        {
            windowType = WindowType.Login;
            currentWindow.titleContent.text= "MySQL 管理器首页";
        }
        actionSelect = GUI.Toolbar(new Rect(0, 0, 250, 20), actionSelect, actionContents);
        if(actionSelect==0)
        {
            //绘制文本域
            DrawInputSQL();
        }
        else
        {
            optionSelect = GUI.Toolbar(new Rect(10, 30, 280, 20), optionSelect, optionContents);
        }


        if (actionSelect == 1)//如果选择了可视化，才需要绘制，SQL语句清一色使用输入框
        {
            switch (optionSelect)
            {
                case 0:
                    DrawInsert();
                    break;
                case 1:
                    DrawDelete();
                    break;
                case 2:
                    DrawQuary();
                    break;
                case 3:
                    DrawAlter();
                    break;
            }
        }
    }
    /// <summary>
    /// 绘制插入数据或者插入表
    /// </summary>
    private void DrawInsert()
    {
        addSelect = GUI.Toolbar(new Rect(10, 60, 280, 20), addSelect, addContents);
        if (addSelect == 0)//绘制增加数据的UI组件
        {
            //清空新增表的数据
            newTableFieldCount = 0;
            if(variableDatasList!=null)
                variableDatasList.Clear();
            currentPressTypeBtnIndex = 0;
            addTableState = "";
            addNewTableName = "";


            if (GUI.Button(new Rect(10, 85, 280, 20), "选择数据表"))
            {
                InitChooseTableMenu();
            }

            if (currentChooseTableColumnNames != null)
            {
                addDataScrollViewInRect = new Rect(0, 0, 280, 60 + currentChooseTableColumnNames.Count * 25 + 100);
            }
            else
            {
                addDataScrollViewInRect = new Rect(0, 0, 280, 60);
            }

            addDataScrollViewPos = GUI.BeginScrollView(addDataScrollViewOutRect, addDataScrollViewPos, addDataScrollViewInRect);
            GUI.Label(new Rect(10, 0, 100, 20), "当前选择表:");
            GUI.Label(new Rect(75, 0, 180, 20), currentChooseTableName);
            if (currentChooseTableName != null && currentChooseTableName != "")
            {
                for (int i = 0; i < currentChooseTableColumnNames.Count; i++)
                {
                    int curY = 20 + (25 * i);
                    int txFieldX = 7 * currentChooseTableColumnNames[i].Length + 18;
                    GUI.Label(new Rect(10, curY, 180, 20), currentChooseTableColumnNames[i]);
                    addDataInputs[i] = GUI.TextField(new Rect(txFieldX, curY, currentWindow.minSize.x - 20 - txFieldX, 20), addDataInputs[i]);
                }
                if (GUI.Button(new Rect(10, addDataScrollViewInRect.height - 140, 280, 20), "增加数据"))
                {
                    MySqlManager.Instance.Insert(currentChooseTableName, currentChooseTableColumnNames.ToArray(), addDataInputs, ref addDataState);
                }
                if (addDataState != null && addDataState != "")
                {
                    GUI.TextArea(new Rect(10, addDataScrollViewInRect.height - 120, 280, 100), "执行结果:" + addDataState);
                }
            }
            GUI.EndScrollView();
        }
        else//绘制增加表的UI组件
        {
            //清空增加数据的数据
            addDataTableName = "";
            currentChooseTableName = "";
            if(currentChooseTableColumnNames!=null)
                currentChooseTableColumnNames.Clear();
            addDataInputs=null;
            addDataState="";

            GUI.Label(new Rect(10, 85, 100, 20), "表名:");
            addNewTableName = GUI.TextField(new Rect(50, 85, 215, 20), addNewTableName);
            if(GUI.Button(new Rect(270, 85, 20, 20), "+"))
            {
                newTableFieldCount++;
                variableDatasList.Add(new VariableData(variableDatasList));
            }

            int unitHeight = 60;//单位高度
            if(addTableState != null && addTableState !="")
            {
                addTableScrollViewInRect = new Rect(0, 0, 280, newTableFieldCount * unitHeight * 1.15f + 150);
            }

            else
            {
                addTableScrollViewInRect = new Rect(0, 0, 280, newTableFieldCount * unitHeight * 1.15f + 40);
            }

            addTableScrollViewPos=GUI.BeginScrollView(addTableScrollViewOutRect, addTableScrollViewPos, addTableScrollViewInRect);

            VariableData varData = null;
            for (int i = 0; i < newTableFieldCount; i++)
            {
                varData = variableDatasList[i];
                //1.15倍间距
                GUI.BeginGroup(new Rect(0, i * unitHeight*1.15f, 290, unitHeight));
                GUI.DrawTexture(new Rect(10, 0, 280, unitHeight),Resources.Load<Texture2D>("White"),ScaleMode.StretchToFill,true,0,new Color(0.25f,0.25f,0.25f),0,5);

                GUI.Label(new Rect(10, 0, 60, 20), "属性名称：");
                varData.varName=GUI.TextField(new Rect(70,0, 195, 20), varData.varName);

                GUI.Label(new Rect(10, 20, 60, 20), "属性类型：");
                GUI.Label(new Rect(70, 20, 100, 20), varData.varType);

                //字符类型才能设置长度
                if(varData.varType == "CHAR"|| varData.varType == "VARCHAR")
                    varData.length= GUI.TextField(new Rect(150, 20, 50, 20), varData.length);
                if (GUI.Button(new Rect(205, 20, 85, 20), "选择属性类型"))
                {
                    currentPressTypeBtnIndex = i;
                    InitChooseVaribleTypeMenu();
                }
                if(varData.varType!=null)
                {
                    DataType varType = (DataType)Enum.Parse(typeof(DataType), varData.varType);
                    switch (varType)
                    {
                        case DataType.TINYINT:
                        case DataType.SMALLINT:
                        case DataType.MEDIUMINT:
                        case DataType.INT:
                        case DataType.BIGINT:
                            varData.isAutoIncrement = GUI.Toggle(new Rect(150, 20, 50, 20), varData.isAutoIncrement, "自增");
                            break;
                    }
                }

                //GUI.Label(new Rect(10, 40, 60, 20), "主键");
                varData.IsPrimaryKey=GUI.Toggle(new Rect(15, 40, 50, 20), varData.IsPrimaryKey, "主键");
                varData.canNull=GUI.Toggle(new Rect(100, 40, 50, 20), varData.canNull, "可空");

                if (i==newTableFieldCount-1&&GUI.Button(new Rect(270,0, 20, 20), "-"))
                {
                    newTableFieldCount--;
                    variableDatasList.RemoveAt(newTableFieldCount);
                }
                GUI.EndGroup();
            }
            if (GUI.Button(new Rect(10, (newTableFieldCount) * unitHeight * 1.15f, 270, 20), "增加新表"))
            {
                AddNewTable(addNewTableName);
                chooseTableMenu = null;
                InitChooseTableMenu(false);//重新获取表
            }
            //绘制结果
            if(addTableState!=null&&addTableState!="")
            {
                GUI.TextArea(new Rect(10, (newTableFieldCount) * unitHeight * 1.15f+30,280, 100), "执行结果:" + addTableState);
            }
            GUI.EndScrollView();
        }
    }
    /// <summary>
    /// 绘制删除数据或者删除表
    /// </summary>
    private void DrawDelete()
    {
        deleteSelect = GUI.Toolbar(new Rect(10, 60, 280, 20), deleteSelect, deleteContents);
        if(deleteSelect==0)
        {
            if (GUI.Button(new Rect(10, 85, 280, 20), "选择数据表"))
            {
                InitChooseTableMenu();
            }
            if(currentChooseTableName!=null&&currentChooseTableName!="")
            {
                int unitWidth = 100;
                float mutiple = 1.05f;
                GUI.Label(new Rect(10, 110, 280, 20), "数据表：" + currentChooseTableName);

                DataSet dt= MySqlManager.Instance.Query(currentChooseTableName);
                DataTable dc = dt.Tables[0];
                float height = 20;
                if (dt.Tables[0] != null)
                {
                    height = dt.Tables[0].Rows.Count * 20 + 40;
                }

                Rect outRect = new Rect(0, 0, currentChooseTableColumnNames.Count * unitWidth* mutiple+30, height);
                addDataScrollViewPos = GUI.BeginScrollView(new Rect(0, 110, 280, 240), addDataScrollViewPos, outRect);

                //绘制列表属性名称
                for (int i = 0; i < currentChooseTableColumnNames.Count; i++)
                {
                    GUI.TextField(new Rect(20+i * unitWidth* mutiple, 20, unitWidth, 20), currentChooseTableColumnNames[i]);
                }
                //绘制数据
                for (int row = 0; row < dc.Rows.Count; row++)
                {
                    if(GUI.Button(new Rect(0, row * 20 + 40, 20, 20), "-"))
                    {
                        bool select=EditorUtility.DisplayDialog("删除数据", "是否确认删除该数据", "确认", "取消");
                        if(select)
                        {
                            MySqlManager.Instance.Delete(currentChooseTableName, currentChooseTableColumnNames[0]+"='"+dc.Rows[row][0]+"'",ref deleteDataState);
                            ChangeTableName(currentChooseTableName);
                        }
                    }
                    for (int column = 0; column < dc.Columns.Count; column++)
                    {
                        GUI.TextField(new Rect(20+column * unitWidth * mutiple, row*20+40, unitWidth, 20), dc.Rows[row][column].ToString());
                    };
                }

                GUI.EndScrollView();
                if (deleteDataState != null && deleteDataState != "")
                {
                    GUI.TextArea(new Rect(10, 350, 280, 150), "执行结果:" + deleteDataState);
                }
            }

        }
        else
        {
            if (GUI.Button(new Rect(10, 85, 280, 20), "显示数据表"))
            {
                tableNameList = MySqlManager.Instance.GetTables();
            }
            if(tableNameList != null)
            {
                Rect outRect = new Rect(0, 0, 280, 20 * tableNameList.Count + 10);
                if (dropTableState != null && dropTableState != "")
                {
                    outRect.height += 10;
                    GUI.TextArea(new Rect(10, tableNameList.Count * 20 + 120, 280, 100), "执行结果:" + dropTableState);
                }
                addTableScrollViewPos =GUI.BeginScrollView(new Rect(0, 110, 300, 400),addTableScrollViewPos, outRect);
                for (int i = 0; i < tableNameList.Count; i++)
                {
                    GUI.TextField(new Rect(10, i * 20, 250, 20), tableNameList[i]);
                    if(GUI.Button(new Rect(260, i * 20, 20, 20), "-"))
                    {
                        bool select =EditorUtility.DisplayDialog("删除表", "是否确认删除表"+tableNameList[i], "确认", "取消");
                        if(select)
                        {
                            MySqlManager.Instance.DropTable(tableNameList[i],ref dropTableState);
                            tableNameList = MySqlManager.Instance.GetTables();
                            chooseTableMenu = null;
                            InitChooseTableMenu(false);//重新获取表
                        }
                    }
                }
                GUI.EndScrollView();
            }
        }
    }
    /// <summary>
    /// 绘制查询数据或者查询表
    /// </summary>
    private void DrawQuary()
    {
        quarySelect = GUI.Toolbar(new Rect(10, 60, 280, 20), quarySelect, quaryContents);
        if (quarySelect == 0)
        {
            //清空
            if(tableNameList!=null)
                tableNameList.Clear();
            dropTableState = "";

            if (GUI.Button(new Rect(10, 85, 280, 20), "选择数据表"))
            {
                InitChooseTableMenu();
            }
            if (currentChooseTableName != null && currentChooseTableName != "")
            {
                int unitWidth = 100;
                float mutiple = 1.05f;
                GUI.Label(new Rect(10, 110, 280, 20), "数据表：" + currentChooseTableName);

                DataSet dt = MySqlManager.Instance.Query(currentChooseTableName);
                DataTable dc = dt.Tables[0];
                float height = 20;
                if (dt.Tables[0] != null)
                {
                    height = dt.Tables[0].Rows.Count * 20 + 40;
                }

                Rect outRect = new Rect(0, 0, currentChooseTableColumnNames.Count * unitWidth * mutiple+30  , height);
                addDataScrollViewPos = GUI.BeginScrollView(new Rect(0, 110, 280, 240), addDataScrollViewPos, outRect);

                //绘制列表属性名称
                for (int i = 0; i < currentChooseTableColumnNames.Count; i++)
                {
                    GUI.TextField(new Rect(20 + i * unitWidth * mutiple, 20, unitWidth, 20), currentChooseTableColumnNames[i]);
                }
                //绘制数据
                for (int row = 0; row < dc.Rows.Count; row++)
                {
                    for (int column = 0; column < dc.Columns.Count; column++)
                    {
                        GUI.TextField(new Rect(20 + column * unitWidth * mutiple, row * 20 + 40, unitWidth, 20), dc.Rows[row][column].ToString());
                    };
                }

                GUI.EndScrollView();
                if (deleteDataState != null && deleteDataState != "")
                {
                    GUI.TextArea(new Rect(10, 350, 280, 150), "执行结果:" + deleteDataState);
                }
            }

        }
        else
        {
            //清空
            if(currentChooseTableColumnNames!=null)
                currentChooseTableColumnNames.Clear();
            currentChooseTableName = "";

            if (GUI.Button(new Rect(10, 85, 280, 20), "显示数据表"))
            {
                tableNameList = MySqlManager.Instance.GetTables();
            }
            if (tableNameList != null)
            {
                Rect outRect = new Rect(0, 0, 280, 20 * tableNameList.Count + 10);
                if (dropTableState != null && dropTableState != "")
                {
                    outRect.height += 10;
                    GUI.TextArea(new Rect(10, tableNameList.Count * 20 + 120, 280, 100), "执行结果:" + dropTableState);
                }
                addTableScrollViewPos = GUI.BeginScrollView(new Rect(0, 110, 300, 400), addTableScrollViewPos, outRect);
                for (int i = 0; i < tableNameList.Count; i++)
                {
                    Debug.Log(tableNameList[i]);
                    List<string> columnList = MySqlManager.Instance.GetTableColumnsByTableName(tableNameList[i]);
                    int listCount = 0;
                    if(columnList!=null)
                    {
                        listCount = columnList.Count;
                    }
                    GUI.TextField(new Rect(10, i * 20, 280, 20),"表名:"+tableNameList[i]+"   |   字段数:"+ listCount);
                }
                GUI.EndScrollView();
            }
        }
    }


    int lastAlterSelect;
    string[,] allDatas;
    /// <summary>
    /// 绘制修改数据或者修改表结构
    /// </summary>
    private void DrawAlter()
    {
        lastAlterSelect = alterSelect;
        alterSelect = GUI.Toolbar(new Rect(10, 60, 280, 20), alterSelect, alterContents);
        if (lastAlterSelect != alterSelect)
        {
            if(lastAlterSelect!=0)
            {
                currentChooseTableName = "";
                //清空
                if (tableNameList != null)
                    tableNameList.Clear();
                dropTableState = "";
            }
            else
            {            //清空
                if (currentChooseTableColumnNames != null)
                    currentChooseTableColumnNames.Clear();
                //currentChooseTableName = "";
                allDatas = null;
            }
        }
        if (alterSelect == 0)
        {

            if (GUI.Button(new Rect(10, 85, 280, 20), "选择数据表"))
            {
                InitChooseTableMenu();
            }
            if (currentChooseTableName != null && currentChooseTableName != "")
            {
                int unitWidth = 100;
                float mutiple = 1.05f;
                GUI.Label(new Rect(10, 110, 280, 20), "数据表：" + currentChooseTableName);

                float outWidth = 0;
                if(currentChooseTableColumnNames!=null)
                {
                    outWidth = currentChooseTableColumnNames.Count * unitWidth * mutiple;
                }

                DataSet dt = MySqlManager.Instance.Query(currentChooseTableName);//查询
                float height = 20;
                if(dt.Tables[0]!=null)
                {
                    height = dt.Tables[0].Rows.Count*20+40;
                }

                Rect outRect = new Rect(0, 0, outWidth + 30, height);
                addDataScrollViewPos = GUI.BeginScrollView(new Rect(0, 110, 280, 240), addDataScrollViewPos, outRect);

                //绘制列表属性名称
                for (int i = 0; i < currentChooseTableColumnNames.Count; i++)
                {
                    GUI.TextField(new Rect(20 + i * unitWidth * mutiple, 20, unitWidth, 20), currentChooseTableColumnNames[i]);
                }
                if (dt.Tables.Count <= 0)
                {
                    currentChooseTableName = "";
                    currentChooseTableColumnNames.Clear();
                    return;
                }
                DataTable dc = dt.Tables[0];
                //绘制数据
                if(allDatas==null||
                    allDatas.GetLength(0)<dc.Rows.Count||
                    allDatas.GetLength(1) < dc.Columns.Count
                   )
                    allDatas = new string[dc.Rows.Count,dc.Columns.Count];
                for (int row = 0; row < dc.Rows.Count; row++)
                {
                    for (int column = 0; column < dc.Columns.Count; column++)
                    {
                        if(allDatas[row,column]==null)
                        {
                            allDatas[row,column] = dc.Rows[row][column].ToString();
                        }
                        allDatas[row, column] = GUI.TextField(new Rect(20 + column * unitWidth * mutiple, row * 20 + 40, unitWidth, 20), allDatas[row, column]);
                    }
                    if (GUI.Button(new Rect(0, row * 20 + 40, 20, 20), "√"))
                    {
                        bool select = EditorUtility.DisplayDialog("修改数据", "是否确认修改该数据", "确认", "取消");
                        if (select)
                        {
                            //获得所有值
                            string[] allValues = new string[currentChooseTableColumnNames.Count];
                            for (int i = 0; i < allValues.Length; i++)
                            {
                                string value = allDatas[row, i];
                                if (value.Contains("上午") || value.Contains("下午"))
                                {
                                    DateTime dateTime = new DateTime();
                                    if (DateTime.TryParse(value, out dateTime))
                                    {
                                        value = dateTime.ToString("yyyy-MM-dd");
                                        Debug.Log(value);
                                    }
                                }

                                allValues[i] = value;
                            }
                            //获得条件
                            string condition = currentChooseTableColumnNames[0] + "='" + allValues[0] + "'";
                            MySqlManager.Instance.Update(currentChooseTableName, currentChooseTableColumnNames.ToArray(), allValues, condition, ref alterDataState);
                            ChangeTableName(currentChooseTableName);
                        }
                    }
                }

                GUI.EndScrollView();
                if (alterDataState != null && alterDataState != "")
                {
                    GUI.TextArea(new Rect(10, 350, 280, 150), "执行结果:" + alterDataState);
                }
            }

        }
        else
        {

            if (GUI.Button(new Rect(10, 85, 280, 20), "选择数据表"))
            {
                InitChooseTableMenu();
            }
            if(currentChooseTableName!=null&&currentChooseTableName!="")
            {
                GUI.Label(new Rect(10, 110, 100, 20), "表名:");
                GUI.Label(new Rect(50, 110, 215, 20), currentChooseTableName);
                GUI.TextArea(new Rect(10, 130, 280, 40), "当前功能暂不支持，建议使用Mysql语句或者删除再重新添加表");
            }


        }
    }

    /// <summary>
    /// 绘制输入SQL语句的文本域
    /// </summary>
    private void DrawInputSQL()
    {
        GUI.Label(new Rect(10, 30, 100, 20), "输入MySql语句");
        sqlContent = GUI.TextArea(new Rect(10, 60, 280, 220), sqlContent);
        if (GUI.Button(new Rect(10, 295, 280, 20), "执行"))
        {
            ExcuteType excuteType = ExcuteType.Insert;

            //根据语句判断操作类型
            if (sqlContent != null && sqlContent.Length >= 6)
            {
                string exType = sqlContent.Substring(0, 6).ToUpper();
                if (exType.Contains("INSERT"))
                    excuteType = ExcuteType.Insert;
                else if (exType.Contains("DELETE"))
                    excuteType = ExcuteType.Delete;
                else if (exType.Contains("SELECT") || exType.Contains("SHOW"))
                    excuteType = ExcuteType.Select;
                else if (exType.Contains("UPDATE"))
                    excuteType = ExcuteType.Update;
                else if (exType.Contains("CREATE"))
                    excuteType = ExcuteType.CreateTable;
                else if (exType.Contains("ALTER"))
                    excuteType = ExcuteType.AlterTable;
                else if (exType.Contains("DROP"))
                    excuteType = ExcuteType.DropTable;
                else if (exType.Contains("USE"))
                {
                    chooseTableMenu = null;
                    excuteType = ExcuteType.ChangeDatabase;
                }
            }
            if(excuteType==ExcuteType.ChangeDatabase)
            {
                database = sqlContent.Substring(4, sqlContent.Length - 4);
                database=database.Replace(' ', '\0');
            }
            MySqlManager.Instance.ExcuteSql(sqlContent, excuteType, ref excuteResult);
            if(excuteType==ExcuteType.ChangeDatabase)
            {
                InitChooseTableMenu(false);
                currentChooseTableName = "";
                if(currentChooseTableColumnNames!=null)
                currentChooseTableColumnNames.Clear();

            }
        }
        if (excuteResult != null && excuteResult != "")
        {
            GUI.Label(new Rect(10, 320, 100, 20), "执行结果");
            GUI.TextArea(new Rect(10, 340, 280, 140), excuteResult);
            if (GUI.Button(new Rect(10, 485, 50, 20), "清除"))
            {
                excuteResult = " ";
            }
        }
    }

    void ChangeTableName(object tableName)
    {
        string lastTableName = this.currentChooseTableName;
        this.currentChooseTableName = tableName.ToString();
        if (lastTableName != currentChooseTableName)
        {
            //选择了不同的表格，更新表格数据列表
            currentChooseTableColumnNames = MySqlManager.Instance.GetTableColumnsByTableName(currentChooseTableName);
            addDataInputs = new string[currentChooseTableColumnNames.Count];
            addDataState = " ";
        }
        Debug.Log("当前选择" + currentChooseTableName);
    }//选择表名的菜单回调
    void ChangeTypeName(object typeName)
    {
        variableDatasList[currentPressTypeBtnIndex].varType = typeName.ToString();
    }//修改变量类型

    GenericMenu chooseTableMenu;
    void InitChooseTableMenu(bool isShow=true)
    {
        if(chooseTableMenu==null||chooseTableMenu.GetItemCount()==0)
        {
            chooseTableMenu = new GenericMenu(); //初始化GenericMenu
            List<string> allTablesList = MySqlManager.Instance.GetTables();
            for (int i = 0; i < allTablesList.Count; i++)
            {
                int index = i;
                chooseTableMenu.AddItem(new GUIContent(allTablesList[i]), false,ChangeTableName,allTablesList[i]); //向菜单中添加菜单项
            }
        }
        if(isShow)
        chooseTableMenu.ShowAsContext(); //显示菜单
    }//初始化/显示选择数据表的菜单
    GenericMenu chooseTypeMenu;
    int currentPressTypeBtnIndex;
    void InitChooseVaribleTypeMenu()//选择数据类型的菜单
    {
        if (chooseTypeMenu == null || chooseTypeMenu.GetItemCount() == 0)
        {
            chooseTypeMenu = new GenericMenu(); //初始化GenericMenu
            foreach (var item in Enum.GetValues(typeof(DataType)))
            {
                string typeName = Enum.GetName(typeof(DataType), item);
                chooseTypeMenu.AddItem(new GUIContent(typeName), false, ChangeTypeName, typeName); //向菜单中添加菜单项
            }
        }
        chooseTypeMenu.ShowAsContext(); //显示菜单
    }
    private bool AddNewTable(string tableName)
    {
        if(variableDatasList==null||variableDatasList.Count==0)
        {
            addTableState = "创建表" + tableName + "失败，数据未填写完整";
            return false;
        }
        ColumnData[] columnDatas = new ColumnData[variableDatasList.Count];
        try
        {
            for (int i = 0; i < variableDatasList.Count; i++)
            {
                columnDatas[i] = variableDatasList[i].ConvertToColumnData();
            }
        }
        catch (Exception e)
        {
            addTableState = "创建表" + tableName + "失败,错误信息:"+e.Message;
            return false;
        }

        return MySqlManager.Instance.CreateTable(tableName, columnDatas,ref addTableState);
    }

    private void OnEnable()
    {
        //读取看是否有上次保存的登陆记录
        ip = PlayerPrefs.GetString("IP");
        port = PlayerPrefs.GetString("Port");
        database = PlayerPrefs.GetString("Database");
        uid = PlayerPrefs.GetString("Uid");
        if(PlayerPrefs.GetInt("AutoLogin")==1)
        {
            password = PlayerPrefs.GetString("Password");
            Debug.Log("自动登录");

            //登录
            MySqlManager.Instance.Init(ip, port, database, uid, password);
            MySqlManager.Instance.Connect();
        }

        windowType = WindowType.Login;
    }
    private void OnDestroy()
    {
        //断开连接
        MySqlManager.Instance.Disconnect();
        currentWindow = null;
    }
}
/// <summary>
/// 新增表的变量数据
/// </summary>
public class VariableData
{
    public List<VariableData> otherDatasList;
    public VariableData() { }
    public VariableData(List<VariableData> list)
    {
        otherDatasList = list;
    }
    public string varName, varType;
    private bool isPrimaryKey;
    public bool canNull;
    public bool isAutoIncrement;
    public bool IsPrimaryKey
    {
        get
        {
            return isPrimaryKey;
        }
        set
        {
            if(value)
            {
                //防止两个主键,如果当前要设置为主键，那么本来的主键就要去勾
                if (otherDatasList != null && otherDatasList.Count > 1)
                {
                    for (int i = 0; i < otherDatasList.Count; i++)
                    {
                        if (otherDatasList[i].IsPrimaryKey&&otherDatasList[i]!=this)
                        {
                            otherDatasList[i].IsPrimaryKey = false;
                        }
                    }
                }
            }
            isPrimaryKey = value;
        }
    }
    public string length="0";

    public ColumnData ConvertToColumnData()
    {
        DataType dtType = (DataType)Enum.Parse(typeof(DataType), varType);
        int charLength = 0;
        int.TryParse(length,out charLength);
        ColumnData columnData = new ColumnData(dtType, varName, isPrimaryKey, canNull, isAutoIncrement, charLength);
        return columnData;
    }
}