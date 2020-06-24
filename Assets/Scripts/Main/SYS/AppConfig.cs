using UnityEngine;

// Copyright (C) 2019 All Rights Reserved.
// Detail：AppConfig	MyChessboard	2019/8/28
// Version：1.0.0
[System.Serializable]
public class AppConfig
{
    public SystemLanguage CusLan = SystemLanguage.Unknown;
    public int fame = 30;
    public int vulume = 100;
    [Range(0, 2)]
    public int debugLv = 1;
    public string debugScene;

    //16位24位32位
    [HideInInspector] public string appAesKey = "*7956$1lo0@#Zxp;";
    [HideInInspector] public string appAesIV = "@-8523769lQovg%&";

    public string severforupdata = "http://127.0.0.1/iot/";

    public int UDPReceivePort = 8266;

    public int saveSerailNumber;
//    public const string BUNDLEPATN = "Windows";
    public const string BUNDLEPATN= "AndroidRes";

    public string DeAes(string data)
    {
        return AesCrypto.Decrypt(data, appAesKey, appAesIV);
    }

    public static string EnAes(string data)
    {
        string appAesKey = "*7956$1lo0@#Zxp;";
        string appAesIV = "@-8523769lQovg%&";
        return AesCrypto.Encrypt(data, appAesKey, appAesIV);
    }


    public const string HOST = "t0eff28.mqtt.iot.gz.baidubce.com";

}
