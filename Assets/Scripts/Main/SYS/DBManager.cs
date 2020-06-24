using Mono.Data.Sqlite;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DBManager : SingleBase<DBManager>
{

    private string dbName = "data";     //数据库名称

    //建立数据库连接
    SqliteConnection connection;
    //数据库命令
    SqliteCommand command;
    //数据库阅读器
    SqliteDataReader reader;

    public void Awake()
    {
        //连接数据库
        OpenConnect();
        Start();
    }

    private void OnDestroy()
    {
        //断开数据库连接
        CloseDB();
    }

    public void Start()
    {
        ////创建表
        //string[] colNames = new string[] { "key", "value" };
        //string[] colTypes = new string[] { "string", "string" };
        //CreateTable("settingConfig", colNames, colTypes);

        ////使用泛型创建数据表
        //CreateTable<T4>();

        ////根据条件查找特定的字段
        //foreach (var item in SelectData("user", new string[] { "name" }, new string[] { "password", "123456" }))
        //{
        //    Debug.Log(item);
        //}

        ////更新数据
        //UpdataData("user", new string[] {"name", "yyy"}, new string[] { "name" ,"wxy" });

        ////删除数据
        //DeleteValues("user", new string[] { "name","wxyqq" });


        ////查询数据
        
        ////foreach (var item in GetDataBySqlQuery("user",new string[] { "name"}))
        ////{
        ////    Debug.Log(item);
        ////}

        ////插入数据
        //string[] values = new string[] { "3", "33" };
        //insertvalues("user", values);

        //foreach (var item in getdatabysqlquery("user", new string[] { "name" }))
        //{
        //    debug.log(item);
        //}

        ////使用泛型插入对象
        //T4 t = new T4(2, "22", "222");
        //Insert<T4>(t);

    }

    /// <summary>
    /// 删除表
    /// </summary>
    /// <param name="tableName"></param>
    /// <returns></returns>
    public SqliteDataReader DeleteTable(string tableName)
    {
        string sql = "DROP TABLE " + tableName;
        return ExecuteQuery(sql);
    }

    /// <summary>
    /// 查询整张表的数据
    /// </summary>
    /// <param name="tableName"></param>
    /// <returns></returns>
    public SqliteDataReader SelectFullTableData(string tableName)
    {
        string queryString = "select * from " + tableName;
        return ExecuteQuery(queryString);
    }

    /// <summary>
    /// 查询数据
    /// </summary>
    /// <param name="tableName">表名</param>
    /// <param name="fields">需要查找数据</param>
    /// <returns></returns>
    public List<String> GetDataBySqlQuery(string tableName, string[] fields)
    {
        //string queryString = "select " + fields[0];
        //for (int i = 1; i < fields.Length; i++)
        //{
        //    queryString += " , " + fields[i];
        //}
        //queryString += " from " + tableName;
        //return ExecuteQuery(queryString);

        List<string> list = new List<string>();
        //string queryString = "SELECT * FROM " + tableName;
        string queryString = "select " + fields[0];
        for (int i = 1; i < fields.Length; i++)
        {
            queryString += " , " + fields[i];
        }
        queryString += " from " + tableName;
        reader = ExecuteQuery(queryString);
        while (reader.Read())
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                object obj = reader.GetValue(i);
                list.Add(obj.ToString());
            }
        }
        return list;
    }

    /// <summary>
    /// 查询数据
    /// </summary>
    /// <param name="tableName">数据表名</param>
    /// <param name="values">需要查询的数据</param>
    /// <param name="fields">查询的条件</param>
    /// <returns></returns>
    public SqliteDataReader SelectData(string tableName, string[] values, string[] fields)
    {
        string sql = "select " + values[0];
        for (int i = 1; i < values.Length; i++)
        {
            sql += " , " + values[i];
        }
        sql += " from " + tableName + " where( ";
        for (int i = 0; i < fields.Length - 1; i += 2)
        {
            sql += fields[i] + " =' " + fields[i + 1] + " 'and ";
        }
        sql = sql.Substring(0, sql.Length - 4) + ");";
        return ExecuteQuery(sql);


        //用于查看打印
        //List<string> list = new List<string>();
        //reader = ExecuteQuery(sql);

        //for (int i = 0; i < reader.FieldCount; i++)
        //{
        //    object obj = reader.GetValue(i);
        //    list.Add(obj.ToString());
        //}
        //return list;
    }


    /// <summary>
    /// 执行SQL命令
    /// </summary>
    /// <param name="queryString">SQL命令字符串</param>
    /// <returns></returns>
    public SqliteDataReader ExecuteQuery(string queryString)
    {
        command = connection.CreateCommand();
        command.CommandText = queryString;
        reader = command.ExecuteReader();
        return reader;
    }

    /// <summary>
    /// 创建表(使用泛型)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public void CreateTable<T>()
    {
        var type = typeof(T);
        string sql = "create Table " + type.Name + "( ";
        var fields = type.GetFields();
        for (int i = 0; i < fields.Length; i++)
        {
            sql += " [ " + fields[i].Name + "] " + CS2DB(fields[i].FieldType) + ",";
        }
        sql = sql.TrimEnd(',') + ")";
        ExecuteQuery(sql);
    }

    /// <summary>
    /// CS转化为DB类别
    /// </summary>
    /// <param name="type">c#中字段的类别</param>
    /// <returns></returns>
    string CS2DB(Type type)
    {
        string result = "Text";
        if (type == typeof(Int32))
        {
            result = "Int";
        }
        else if (type == typeof(String))
        {
            result = "Text";
        }
        else if (type == typeof(Single))
        {
            result = "FLOAT";
        }
        else if (type == typeof(Boolean))
        {
            result = "Bool";
        }
        return result;
    }


    /// <summary>
    /// 创建数据库
    /// </summary>
    /// <param name="tableName">数据库名</param>
    /// <param name="colNames">字段名</param>
    /// <param name="colTypes">字段名类型</param>
    /// <returns></returns>
    public SqliteDataReader CreateTable(string tableName, string[] colNames, string[] colTypes)
    {
        string queryString = "create table " + tableName + "(" + colNames[0] + " " + colTypes[0];
        for (int i = 1; i < colNames.Length; i++)
        {
            queryString += ", " + colNames[i] + " " + colTypes[i];
        }
        queryString += " )";

        Debug.Log("添加成功");
        return ExecuteQuery(queryString);
    }

    /// <summary>
    /// 向指定数据表中插入数据
    /// </summary>
    /// <param name="tableName"></param>
    /// <param name="values"></param>
    /// <returns></returns>
    public SqliteDataReader InsertValues(string tableName, string[] values)
    {
        string sql = "INSERT INTO " + tableName + " values (";
        foreach (var item in values)
        {
            sql += "'" + item + "',";
        }
        sql = sql.TrimEnd(',') + ")";

        Debug.Log("插入成功");
        return ExecuteQuery(sql);
    }

    /// <summary>
    /// 插入数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="t"></param>
    /// <returns></returns>
    public SqliteDataReader Insert<T>(T t)
    {
        var type = typeof(T);
        var fields = type.GetFields();
        string sql = "INSERT INTO " + type.Name + " values (";

        foreach (var field in fields)
        {
            //通过反射得到对象的值
            sql += "'" + type.GetField(field.Name).GetValue(t) + "',";
        }
        sql = sql.TrimEnd(',') + ");";

        Debug.Log("插入成功");
        return ExecuteQuery(sql);
    }


    /// <summary>
    /// 更新数据
    /// </summary>
    /// <param name="tableName"></param>
    /// <param name="values">需要修改的数据</param>
    /// <param name="conditions">修改的条件</param>
    /// <returns></returns>
    public SqliteDataReader UpdataData(string tableName, string[] values, string[] conditions)
    {
        string sql = "update " + tableName + " set ";
        for (int i = 0; i < values.Length - 1; i += 2)
        {
            sql += values[i] + "='" + values[i + 1] + "',";
        }
        sql = sql.TrimEnd(',') + " where (";
        for (int i = 0; i < conditions.Length - 1; i += 2)
        {
            sql += conditions[i] + "='" + conditions[i + 1] + "' and ";
        }
        sql = sql.Substring(0, sql.Length - 4) + ");";
        Debug.Log("更新成功");
        return ExecuteQuery(sql);
    }


    /// <summary>
    /// 删除数据
    /// </summary>
    /// <param name="tableName"></param>
    /// <param name="conditions">查询条件</param>
    /// <returns></returns>
    public SqliteDataReader DeleteValues(string tableName, string[] conditions)
    {
        string sql = "delete from " + tableName + " where (";
        for (int i = 0; i < conditions.Length - 1; i += 2)
        {
            sql += conditions[i] + "='" + conditions[i + 1] + "' and ";
        }
        sql = sql.Substring(0, sql.Length - 4) + ");";
        return ExecuteQuery(sql);
    }

    bool isopen;
    //打开数据库
    public void OpenConnect()
    {
        if (isopen)
            return;
        try
        {
            //数据库存放在 Asset/StreamingAssets
            string path = Application.streamingAssetsPath + "/" + dbName + ".db";
            //新建数据库连接
            connection = new SqliteConnection(@"Data Source = " + path);
            //打开数据库
            connection.Open();
            Debug.Log("打开数据库");
        }
        catch (Exception ex)
        {
            Debug.Log(ex.ToString());
        }
        isopen = true;
    }

    //关闭数据库
    public void CloseDB()
    {
        if (command != null)
        {
            command.Cancel();
        }
        command = null;

        if (reader != null)
        {
            reader.Close();
        }
        reader = null;

        if (connection != null)
        {
            connection.Close();
        }
        connection = null;
        isopen = false;
    }
}