using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MySqlTest : MonoBehaviour
{
    private IEnumerator Start()
    {
        //MySqlManager.Instance.AutoTranslate("You are welcome",null);

        MySqlManager.Instance.Init("129.204.192.19", "3306", "unitytest", "LgWindow", "lzk123");
        MySqlManager.Instance.Connect();
        while (!MySqlManager.Instance.IsConnected())
        {
            Debug.Log("未连接");
            yield return null;
        }

        List<string> tables = MySqlManager.Instance.GetTableColumnsByTableName("test");
        for (int i = 0; i < tables.Count; i++)
        {
            Debug.Log(tables[i]);
        }
        //MySqlManager.Instance.ExcuteSql("select * from test",ExcuteType.Select);
        //MySqlManager.Instance.ExcuteSql("create table tb_tmp01 ", ExcuteType.Insert);

        //MySqlManager.Instance.Insert("test", new string[] { "id", "name" }, new string[] { "4", "lzy" });
        //MySqlManager.Instance.Update("test", new string[] { "name" }, new string[] { "lxh" }, "id=5");
        //MySqlManager.Instance.PrintDataSet(MySqlManager.Instance.Query("test", new string[] { "id", "name" }, "id<=10"));

        //MySqlManager.Instance.CreateTable("test2", new ColumnData[]
        //    {
        //        new ColumnData(DataType.INT,"id",true,false,0),
        //        new ColumnData(DataType.CHAR,"name",false,false,255)
        //    });
        MySqlManager.Instance.AlterTable("test2", AlterType.Add, new ColumnData[]
            {
                new ColumnData(DataType.CHAR,"sex",false,true,0),
            });
        MySqlManager.Instance.AlterTable("test2", AlterType.Drop, new ColumnData[]
            {
                new ColumnData(DataType.CHAR,"newSex",false,true,0),
            });
        MySqlManager.Instance.AlterTable("test2", AlterType.Change, new ColumnData[]
            {
                new ColumnData(DataType.CHAR,"sex","newSex",false,false,5),
            });
    }
    
    private void OnApplicationQuit()
    {
        MySqlManager.Instance.Disconnect();
    }
}
