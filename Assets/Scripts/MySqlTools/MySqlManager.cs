using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Text;
using UnityEngine.Networking;
using System.Threading.Tasks;

/// <summary>
/// 执行操作的类型
/// </summary>
public enum ExcuteType
{
    Insert,Delete,Select,Update,
    CreateTable, AlterTable,DropTable,
    ChangeDatabase
}
/// <summary>
/// 修改表的操作类型
/// </summary>
public enum AlterType
{
    Add,Drop,Change
}

/// <summary>
/// 数据类型
/// </summary>
public enum DataType
{
    TINYINT, SMALLINT, MEDIUMINT, INT, BIGINT,
    FLOAT, DOUBLE, DECIMAL,
    YEAR, TIME, DATE, DATETIME, TIMESTAMP,
    CHAR, VARCHAR, TINYTEXT, TEXT, MEDIUMTEXT, LONGTEXT, ENUM, SET,
    BIT, BINARY, VARBINARY, TINYBLOB, BLOB, MEDIUMBLOB, LONGBLOB
}
/// <summary>
/// 建表的数据类型
/// </summary>
public class ColumnData
{
    public DataType columnType;
    public string originColumnName;
    public string newColumnName;
    public bool isPrimaryKey, canNull,autoIncrement;
    public int length;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="columnType">属性类型</param>
    /// <param name="columnName">属性名</param>
    /// <param name="isPrimaryKey">是否为主键</param>
    /// <param name="autoIncrement">是否自增</param>
    /// <param name="canNull">是否可以为空</param>
    /// <param name="length">类型长度</param>
    public ColumnData(DataType columnType, string columnName, bool isPrimaryKey, bool canNull, int length)
    {
        this.columnType = columnType;
        this.newColumnName = columnName;
        this.isPrimaryKey = isPrimaryKey;
        this.canNull = canNull;
        this.length = length;
    }

    public ColumnData(DataType columnType, string originColumnName, string newColumnName, bool isPrimaryKey, bool canNull, int length)
    {
        this.columnType = columnType;
        this.originColumnName = originColumnName;
        this.newColumnName = newColumnName;
        this.isPrimaryKey = isPrimaryKey;
        this.canNull = canNull;
        this.length = length;
    }

    public ColumnData(DataType columnType, string newColumnName, bool isPrimaryKey, bool canNull, bool autoIncrement, int length)
    {
        this.columnType = columnType;
        this.newColumnName = newColumnName;
        this.isPrimaryKey = isPrimaryKey;
        this.canNull = canNull;
        this.autoIncrement = autoIncrement;
        this.length = length;
    }
}

/// <summary>
/// MySql管理器，支持增删查改等数据库操作
/// </summary>
public class MySqlManager : BaseManager<MySqlManager>
{
    /// <summary>
    /// 析构函数自动关闭数据库连接
    /// </summary>
    ~MySqlManager()
    {
        Disconnect();
    }

    private MySqlConnection sqlConnection;    //连接类对象
    #region MySql参数

    private string ip;    //地址
    private string port;    //地址
    private string dataBase;    //数据库名称
    private string uid;    //用户名
    //封装属性
    public string Ip { get => ip; private set => ip = value; }
    public string DataBase { get => dataBase; private set => dataBase = value; }
    public string Uid { get => uid; private set => uid = value; }
    public string Port { get => port; private set => port = value; }

    //格式化链接
    private string connectionString;


    #endregion

    #region 数据库状态接口(连接与断连)
    /// <summary>
    /// 初始化MySql参数
    /// </summary>
    /// <param name="ip">IP地址</param>
    /// <param name="port">端口号</param>
    /// <param name="dataBase">数据库名</param>
    /// <param name="uid">账号</param>
    /// <param name="psw">密码</param>
    /// <param name="sslMode">是否使用SSL</param>
    public void Init(string ip, string port, string dataBase, string uid, string psw, string sslMode = "none")
    {
        this.Ip = ip;
        this.Port = port;
        this.DataBase = dataBase;
        this.Uid = uid;

        connectionString = $"SERVER={Ip};PORT={Port};DATABASE={dataBase};UID={Uid};PASSWORD={psw};SslMode={sslMode}";
    }
    public bool IsConnected()
    {
        if (sqlConnection == null) return false;
        return (sqlConnection.State != ConnectionState.Closed && sqlConnection.State != ConnectionState.Broken&&sqlConnection.State!=ConnectionState.Connecting);
    }//判断是否连接
    public void Connect()
    {
        if (connectionString == null || connectionString == "")
        {
            Debug.Log("MySql管理器未初始化，请先使用Init方法初始化管理器信息");
            return;
        }
        if (sqlConnection != null && IsConnected())
        {
            Debug.Log("数据库已连接或正在试图连接，请稍等，如需重新连接，请调用Disconnect手动释放");
        }
        else
        {
            try
            {
                sqlConnection = new MySqlConnection(connectionString);
                sqlConnection.Open();
            }
            catch (Exception e)
            {
                Debug.Log("MySql服务器连接失败，报错信息:" + e.Message);
            }
        }

    }//连接数据库
    public void Disconnect()
    {
        if (sqlConnection == null) return;
        if (IsConnected())
        {
            sqlConnection.Close();
            sqlConnection.Dispose();
            sqlConnection = null;
            Debug.Log("断开MySql连接");
        }
    }//断开数据库
    public async void ConnectAsync(Action<bool> overAction=null)
    {
        if (connectionString == null || connectionString == "")
        {
            Debug.Log("MySql管理器未初始化，请先使用Init方法初始化管理器信息");
            return;
        }
        if (sqlConnection != null && IsConnected())
        {
            Debug.Log("数据库已连接或正在试图连接，请稍等，如需重新连接，请调用Disconnect手动释放");
        }
        else
        {
            try
            {
                sqlConnection = new MySqlConnection(connectionString);
                await Task.Run(sqlConnection.OpenAsync);
                Debug.Log("数据库异步连接成功");
                overAction?.Invoke(true);
            }
            catch (Exception e)
            {
                Debug.Log("MySql服务器连接失败，报错信息:" + e.Message);
                overAction?.Invoke(false);
            }
        }
    }
    #endregion

    #region 数据库操作
    /// <summary>
    /// //数据库操作增删查改
    /// </summary>
    /// <param name="sqlCommand">SQL语句</param>
    /// <param name="excuteType">操作类型</param>
    /// <returns></returns>
    public bool ExcuteSql(string sqlCommand, ExcuteType excuteType,ref string result)
    {
        if (!IsConnected())
        {
            result =result+ "数据库未连接\n";
            Debug.Log(result);
            return false;
        }
        MySqlCommand mycmd = new MySqlCommand(sqlCommand, sqlConnection);
        try
        {
            switch (excuteType)
            {
                case ExcuteType.Select:
                    MySqlDataAdapter dataAdapter = new MySqlDataAdapter(mycmd);
                    DataSet dataSet = new DataSet();
                    dataAdapter.Fill(dataSet);
                    if (dataSet.Tables.Count >= 0)
                    {
                        result = result +  "查询成功，共查询到" + dataSet.Tables[0].Rows.Count + "条结果\n";
                        Debug.Log(result);
                        result= result+PrintDataSet(dataSet)+"\n";
                    }

                    break;
                case ExcuteType.Insert:
                case ExcuteType.Update:
                case ExcuteType.Delete:
                    int effectLine = mycmd.ExecuteNonQuery();
                    if (effectLine > 0)
                    {
                        result =result+ "执行成功,受影响的行数有" + effectLine+"行\n";
                        Debug.Log(result);
                        return true;
                    }
                    return false;
                case ExcuteType.CreateTable:
                    mycmd.ExecuteNonQuery();
                    result =result+ "创建表成功\n";
                    Debug.Log(result);
                    break;
                case ExcuteType.AlterTable:
                    mycmd.ExecuteNonQuery();
                    result =result+ "修改表成功\n";
                    Debug.Log(result);
                    break;
                case ExcuteType.ChangeDatabase:
                    mycmd.ExecuteNonQuery();
                    result = result + "切换数据库成功\n";
                    Debug.Log(result);
                    break;
                case ExcuteType.DropTable:
                    mycmd.ExecuteNonQuery();
                    result = result + "删除表成功\n";
                    Debug.Log(result);
                    break;
            }
        }
        catch (Exception e)
        {
            string actionName = "";
            switch (excuteType)
            {
                case ExcuteType.Select:
                    actionName = "查询";
                    break;
                case ExcuteType.Insert:
                    actionName = "插入";
                    break;
                case ExcuteType.Update:
                    actionName = "更新";
                    break;
                case ExcuteType.Delete:
                    actionName = "删除";
                    break;
                case ExcuteType.CreateTable:
                    actionName = "创建表";
                    break;
                case ExcuteType.AlterTable:
                    actionName = "修改表";
                    break;
            }
            result=result+ OutException(actionName, e)+"\n";
        }
        return false;
    }

    #region 数据操作
    /// <summary>
    /// 插入数据
    /// </summary>
    /// <param name="tableName">表名</param>
    /// <param name="itemsName">属性名列表</param>
    /// <param name="values">对应的值</param>
    /// <returns></returns>
    public bool Insert(string tableName, string[] itemsName, string[] values,ref string outMsg)
    {
        try
        {
            string sql = $"insert into {tableName}({GetConcat(itemsName)}) values({GetConcat(values, true)})";
            Debug.Log(sql);
            MySqlCommand sqlCmd = new MySqlCommand(sql, sqlConnection);
            int effectLine = sqlCmd.ExecuteNonQuery();
            if (effectLine > 0)
            {
                outMsg = "插入数据成功,共影响" + effectLine + "行数据";
                Debug.Log(outMsg);
                return true;
            }
            return false;
        }
        catch (Exception e)
        {
            outMsg= OutException("插入", e);
            return true;
        }
    }
    /// <summary>
    /// 删除数据
    /// </summary>
    /// <param name="tableName">表名</param>
    /// <param name="condition">删除条件</param>
    /// <returns></returns>
    public bool Delete(string tableName, string condition,ref string outMsg)
    {
        try
        {
            string sql = $"delete from {tableName} where {condition}";
            MySqlCommand sqlCmd = new MySqlCommand(sql, sqlConnection);
            int effectLine = sqlCmd.ExecuteNonQuery();
            if (effectLine > 0)
            {
                outMsg="删除数据成功,共删除" + effectLine + "行数据";
                Debug.Log(outMsg);
                return true;
            }
            return false;
        }
        catch (Exception e)
        {
            outMsg = OutException("删除数据", e);
            return false;
        }
    }
    /// <summary>
    /// 查询数据
    /// </summary>
    /// <param name="tableName">表名</param>
    /// <param name="itemsName">属性名，为空则为查询所有</param>
    /// <param name="condition">查询条件</param>
    /// <returns></returns>
    public DataSet Query(string tableName, string[] itemsName = null, string condition = "")
    {
        DataSet dataSet = new DataSet();
        try
        {
            string itemName = itemsName == null ? "*" : GetConcat(itemsName);
            string cdition = condition == "" ? "" : $"where {condition}";
            string sql = $"select {itemName}  from {tableName}  {cdition}";
            //Debug.Log("sql语句："+sql);
            MySqlCommand sqlCmd = new MySqlCommand(sql, sqlConnection);
            MySqlDataAdapter sqlAdpt = new MySqlDataAdapter(sqlCmd);
            sqlAdpt.Fill(dataSet);
        }
        catch (Exception e)
        {
            OutException("查询", e);
        }
        return dataSet;
    }
    /// <summary>
    /// 修改数据
    /// </summary>
    /// <param name="tableName">表名</param>
    /// <param name="itemsName">属性名列表</param>
    /// <param name="values">对应的值</param>
    /// <returns></returns>
    public bool Update(string tableName, string[] itemsName, string[] values, string condition,ref string outMsg)
    {
        try
        {
            List<string> updateDatas = new List<string>();
            for (int i = 0; i < itemsName.Length; i++)
            {
                if(values[i]!=null&&values[i]!="")
                    updateDatas.Add(itemsName[i] + "='" + values[i] + "'");
            }

            string sql = $"update {tableName} set {GetConcat(updateDatas.ToArray())}  where {condition}";
            Debug.Log(sql);
            MySqlCommand sqlCmd = new MySqlCommand(sql, sqlConnection);
            int effectLine = sqlCmd.ExecuteNonQuery();
            if (effectLine > 0)
            {
                outMsg="修改数据成功,共影响" + effectLine + "行数据";
                Debug.Log(outMsg);
                return true;
            }
            return false;
        }
        catch (Exception e)
        {
            outMsg=OutException("插入", e);
            return true;
        }
    }
    #endregion
    #region 表操作
    /// <summary>
    /// 创建表
    /// </summary>
    /// <param name="tableName">表名</param>
    /// <param name="datas">表属性参数</param>
    /// <returns></returns>
    public bool CreateTable(string tableName, ColumnData[] datas,ref string tableState)
    {
        try
        {
            string dataParamsStr = GetCreateDatas(datas);
            //Debug.Log(dataParamsStr);
            if (dataParamsStr != "ERROR")
            {
                string sql = $"create table {tableName}({dataParamsStr})";
                Debug.Log(sql);
                MySqlCommand sqlCmd = new MySqlCommand(sql, sqlConnection);
                sqlCmd.ExecuteNonQuery();

                tableState = "创建" + tableName + "表成功";
                Debug.Log(tableState);
                return true;
            }
            else
            {
                tableState = "语句有误，大概率是因为尝试设置两个主键,需要设置外键请前往Navicat进行设置";
                Debug.Log(tableState);
                return false;
            }

        }
        catch (Exception e)
        {
            tableState=OutException("创建表", e);
            return false;
        }
    }
    public bool AlterTable(string tableName,AlterType alterType ,ColumnData[] datas)
    {
        string actionName = Enum.GetName(typeof(AlterType), alterType);
        try
        {
            string dataParamsStr = GetAlterDatas(datas, alterType);
            if (dataParamsStr != "ERROR")
            {
                string sql = $"alter table {tableName} {actionName} {dataParamsStr}";
                //Debug.Log(sql);
                MySqlCommand sqlCmd = new MySqlCommand(sql, sqlConnection);
                sqlCmd.ExecuteNonQuery();
                Debug.Log($"修改{tableName}表执行{actionName}操作成功");
                return true;
            }
            else
            {
                Debug.Log("语句有误，大概率是因为尝试设置两个主键,需要设置外键请前往Navicat进行设置");
                return false;
            }

        }
        catch (Exception e)
        {
            OutException("修改表进行"+actionName, e);
            return false;
        }
    }
    public bool DropTable(string tableName,ref string tableState)
    {
        try
        {
                string sql = $"drop table {tableName}";
                MySqlCommand sqlCmd = new MySqlCommand(sql, sqlConnection);
                sqlCmd.ExecuteNonQuery();

                tableState = "删除" + tableName + "表成功";
                Debug.Log(tableState);
                return true;

        }
        catch (Exception e)
        {
            tableState = OutException("删除表", e);
            return false;
        }
    }
    public List<string> GetTables()
    {
        DataSet dataSet = new DataSet();
        try
        {
            MySqlCommand sqlCmd = new MySqlCommand("show tables", sqlConnection);
            MySqlDataAdapter sqlAdpt = new MySqlDataAdapter(sqlCmd);
            sqlAdpt.Fill(dataSet);

            List<string> result = new List<string>();
            DataTable dc = dataSet.Tables[0];
            for (int row = 0; row < dc.Rows.Count; row++)
            {
                for (int column = 0; column < dc.Columns.Count; column++)
                {
                    result.Add(dc.Rows[row][column].ToString());
                }
            }
            return result;
        }
        catch (Exception e)
        {
            OutException("查询", e);
        }
        return null;
    }
    public List<string> GetTableColumnsByTableName(string tableName)
    {
        DataSet dataSet = new DataSet();
        try
        {
            MySqlCommand sqlCmd = new MySqlCommand("show fields from " + tableName, sqlConnection);
            MySqlDataAdapter sqlAdpt = new MySqlDataAdapter(sqlCmd);
            sqlAdpt.Fill(dataSet);

            List<string> result = new List<string>();
            DataTable dc = dataSet.Tables[0];
            for (int row = 0; row < dc.Rows.Count; row++)
            {
                    result.Add(dc.Rows[row][0].ToString());
            }//只要第一列的数据
            return result;
        }
        catch (Exception e)
        {
            OutException("查询", e);
        }
        return null;
    }
    #endregion

    #endregion

    #region 会被调用到的一些方法
    /// <summary>
    /// 获得字符串拼接，使用逗号隔开
    /// </summary>
    /// <param name="itemsName">字符串数组</param>
    /// <param name="isAddQuotes">是否需要引号包裹</param>
    /// <returns></returns>
    string GetConcat(string[] itemsName, bool isAddQuotes = false)
    {
        StringBuilder result = new StringBuilder();
        for (int i = 0; i < itemsName.Length; i++)
        {
            if (isAddQuotes)
                result.Append("'" + itemsName[i] + "'");
            else
                result.Append(itemsName[i]);
            if (i < itemsName.Length - 1)
            {
                result.Append(",");
            }
        }
        return result.ToString();
    }

    /// <summary>
    /// 通用获取表数据的方法
    /// </summary>
    /// <param name="datas"></param>
    /// <returns></returns>
    string[] GetTableParams(ColumnData[] datas)
    {
        string[] resultArray = new string[datas.Length];

        int primaryCount = 0;

        string dataLength = "";
        string dataType = "";
        string isPrimaryKey = "";
        string canNull = "";
        string autoIncrement = "";
        for (int i = 0; i < datas.Length; i++)
        {
            if (datas[i].isPrimaryKey)
            {
                primaryCount++;
                if (primaryCount >= 2)
                {
                    return new string[] { "ERROR" };
                }
            }

            dataType = Enum.GetName(typeof(DataType), datas[i].columnType);
            dataLength = datas[i].length > 0 ? "(" + datas[i].length + ")" : "";
            isPrimaryKey = datas[i].isPrimaryKey ? "PRIMARY KEY" : "";
            canNull = datas[i].canNull ? "" : "NOT NULL";
            autoIncrement = datas[i].autoIncrement ? "AUTO_INCREMENT":"";
            resultArray[i] = $"{datas[i].newColumnName} {dataType}{dataLength} {isPrimaryKey} {canNull} {autoIncrement}";
        }
        return resultArray;
    }

    /// <summary>
    /// 获得创建表的数据的字符串
    /// </summary>
    /// <param name="datas">数据参数们</param>
    /// <returns></returns>
    string GetCreateDatas(ColumnData[] datas)
    {
        return GetConcat(GetTableParams(datas));
    }

    /// <summary>
    /// 获得修改表的语句
    /// </summary>
    /// <param name="datas">语句参数</param>
    /// <param name="alterType">修改类型</param>
    /// <returns></returns>
    string GetAlterDatas(ColumnData[] datas,AlterType alterType)
    {
        switch (alterType)
        {
            case AlterType.Add:
                return GetCreateDatas(datas);
            case AlterType.Drop:
                StringBuilder dropList = new StringBuilder();
                for (int i = 0; i < datas.Length; i++)
                {
                    dropList.Append(datas[i].newColumnName);
                    if(i<datas.Length-1)
                    {
                        dropList.Append(",");
                    }
                }
                return dropList.ToString();
            case AlterType.Change:
                string[] temp= GetTableParams(datas);
                for (int i = 0; i < temp.Length; i++)
                {
                    temp[i] = datas[i].originColumnName + " " + temp[i];
                }
                return GetConcat(temp);
            default:
                return "none";
        }
    }

    /// <summary>
    /// 异常处理
    /// </summary>
    /// <param name="actionName">执行操作的名称</param>
    /// <param name="e">错误信息</param>
    string OutException(string actionName, Exception e)
    {
        string originMsg = $"执行{actionName}操作时发生错误\n错误信息:" + e.Message;

        if(Application.isPlaying)
        {
            string waitTranlateStr = e.Message.Replace('_', ' ');
            waitTranlateStr = waitTranlateStr.Replace('\'', ' ');//待翻译的语句里不能存在单引号

            AutoTranslate(waitTranlateStr, (str) =>
            {
                Debug.Log(originMsg + "(" + str + ")");
            });
        }
        else
        {
            Debug.Log(originMsg);
        }

        return originMsg;
    }
    public string PrintDataSet(DataSet dataSet)
    {
        if (dataSet.Tables.Count <= 0) return "";
        DataTable dc = null;
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < dataSet.Tables.Count; i++)
        {
            dc = dataSet.Tables[i];

            for (int column = 0; column < dc.Columns.Count; column++)
            {
                sb.Append(dc.Columns[column] + "\t");
            }
            sb.Append("\n");
            for (int column = 0; column < dc.Columns.Count; column++)
            {
                sb.Append("------");
            }
            sb.Append("\n");


            for (int row = 0; row < dc.Rows.Count; row++)
            {
                for (int column = 0; column < dc.Columns.Count; column++)
                {
                    sb.Append(dc.Rows[row][column] + "\t");
                }
                sb.Append("\n");
                //Debug.Log(sb.ToString());
                //sb.Clear();
            }
            Debug.Log(sb.ToString());
            return sb.ToString();
        }
        return "";
    }//在控制台打印出查询结果
    #endregion

    #region 自动翻译
    //有道翻译api
    const string translateApi = "https://v.api.aa1.cn/api/api-fanyi-yd/index.php";
    /// <summary>
    /// 自动翻译 英-》中
    /// </summary>
    /// <param name="str">待翻译的句子</param>
    /// <param name="overAction">翻译完成的操作</param>
    public void AutoTranslate(string str, Action<string> overAction)
    {
        string targetUrl = $"{translateApi}?msg={str}&type=2";
        MonoMgr.Instance.StartCoroutine(TranslateRequest(targetUrl, overAction));
    }
    IEnumerator TranslateRequest(string targetUrl, Action<string> overAction)
    {
        using (UnityWebRequest req = UnityWebRequest.Get(targetUrl))
        {
            yield return req.SendWebRequest();
            if (req.downloadHandler != null)
            {
                TranslateData translateData = JsonUtility.FromJson<TranslateData>(req.downloadHandler.text);
                overAction.Invoke(translateData.text);
            }
            else
            {
                overAction.Invoke("none");
            }
        }
    }
    #endregion
}

/// <summary>
/// 翻译返回的参数类型
/// </summary>
[System.Serializable]
public class TranslateData
{
    public string type;
    public string desc;
    public string text;
}