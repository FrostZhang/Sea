using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class CashData
{
    public static bool IsSever = true;
    private static int signnetid;
    public string visitor = SystemInfo.deviceUniqueIdentifier;

    const string MODULEDATA = "MODULEDATA";
    const string TIEHUADATA = "TIEHUADATA";
    const string SAVESERAILUMBER = "SAVESERAILUMBER";

    public string equipmentModelPath = Application.streamingAssetsPath + "/Equipments/";

    public CashData()
    {
        if (PlayerPrefs.GetInt(SAVESERAILUMBER, 0) != GameApp.Ins.appConfig.saveSerailNumber)
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.SetInt(SAVESERAILUMBER, GameApp.Ins.appConfig.saveSerailNumber);
        }
        MVC.OnSceneLoad += OnSceneLoad;
    }

    private void OnSceneLoad(float obj)
    {
        if (obj > 1)
        {
            Debug.Log("CashData Clear"); 
            System.GC.Collect();
        }
    }

    public static int SignNetID()
    {
        if (IsSever)
            return signnetid++;
        return -1;
    }

    public static T LoadCash<T>(string path, bool cash = true) where T : Object
    {
        Object t;
        t =  Resources.Load<T>(path);
        return t as T;
    }

    public static T LoadCashNow<T>(string path, bool cash = true) where T : Object
    {
        Object t;
        t = UnityEngine.Resources.Load<T>(path);
        return t as T;
    }
}