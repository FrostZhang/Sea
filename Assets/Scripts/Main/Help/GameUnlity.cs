using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

// Copyright (C) 2019 All Rights Reserved.
// Detail：GameUnlity	MyChessboard	2019/8/27
// Version：1.0.0
public class GameUnlity
{
    public static void ChangeLayer(GameObject go,int layer,bool includeChild)
    {
        if (!includeChild)
        {
            go.layer = layer;
        }
        else
        {
            ChangeLayer(go.transform, layer);
        }
    }

    private static void ChangeLayer(Transform go, int layer)
    {
        go.gameObject.layer = layer;
        if (go.childCount>0)
        {
            for (int i = 0; i < go.childCount; i++)
            {
                ChangeLayer(go.GetChild(i), layer);
            }
        }
    }

    public static Button CreateBlocker(Canvas rootCanvas)
    {
        GameObject gameObject = new GameObject("Blocker");
        RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
        rectTransform.SetParent(rootCanvas.transform, false);
        rectTransform.anchorMin = Vector3.zero;
        rectTransform.anchorMax = Vector3.one;
        rectTransform.sizeDelta = Vector2.zero;
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        //Canvas component = m_Dropdown.GetComponent<Canvas>();
        canvas.sortingLayerID = rootCanvas.sortingLayerID;
        canvas.sortingOrder = rootCanvas.sortingOrder + 1000;
        gameObject.AddComponent<GraphicRaycaster>();
        Image image = gameObject.AddComponent<Image>();
        image.color = Color.clear;
        Button button = gameObject.AddComponent<Button>();
        return button;
    }

    public static string GetRootPath(Transform tr)
    {
        string path = tr.name;
        while (tr.parent)
        {
            path = tr.parent.name + "/" + path;
            tr = tr.parent;
        }
        return path;
    }
    //public static async Task<string> BuildFileMd5(Uri url)
    //{
    //    var request = UnityWebRequest.Get(url.AbsoluteUri);
    //    await request.SendWebRequest();
    //    if (request.isHttpError || request.isNetworkError)
    //        Debuger.Log(Color.red, url.AbsoluteUri);
    //    else
    //    {
    //        using (MD5 md5 = MD5.Create())
    //        {
    //            byte[] fileMd5Bytes = md5.ComputeHash(request.downloadHandler.data);  // 计算Stream 对象的哈希值
    //            return System.BitConverter.ToString(fileMd5Bytes).Replace("-", "").ToLower();
    //        }
    //    }
    //    request.Dispose();
    //    return string.Empty;
    //}

    //public static string BuildFileMd5(string localpath)
    //{
    //    string fileMd5 = string.Empty;
    //    try
    //    {
    //        using (StreamReader fs = new StreamReader(localpath))
    //        {
    //            MD5 md5 = MD5.Create();
    //            byte[] fileMd5Bytes = md5.ComputeHash(fs.BaseStream);  // 计算Stream 对象的哈希值
    //            fileMd5 = System.BitConverter.ToString(fileMd5Bytes).Replace("-", "").ToLower();
    //        }
    //    }
    //    catch (System.Exception e)
    //    {
    //        throw e;
    //    }
    //    return fileMd5;
    //}

    //public static async Task CreateTXT(string path, string content, System.Text.Encoding encoding)
    //{
    //    string dir = Path.GetDirectoryName(path);
    //    if (!System.IO.Directory.Exists(dir))
    //        System.IO.Directory.CreateDirectory(dir);
    //    using (StreamWriter sw = new StreamWriter(path, false, encoding))
    //    {
    //        try
    //        {
    //            await sw.WriteAsync(content);
    //            await sw.FlushAsync();
    //        }
    //        catch (IOException e)
    //        {
    //            throw e;
    //        }
    //    }
    //}

    //public static System.Uri GetstreamingAssets(params string[] filename)
    //{
    //    string[] ss = new string[filename.Length + 1];
    //    ss[0] = Application.streamingAssetsPath;
    //    filename.CopyTo(ss, 1);
    //    var url = new System.Uri(Combine(ss));
    //    return url;
    //}

    //public static System.Uri Getpersistentdata(params string[] filename)
    //{
    //    var ss = new string[filename.Length + 1];
    //    ss[0] = Application.persistentDataPath;
    //    filename.CopyTo(ss, 1);
    //    var url = new System.Uri(Combine(ss));
    //    return url;
    //}

    //static StringBuilder sb = new StringBuilder(100);
    //public static string Combine(string[] ss)
    //{
    //    sb.Clear();
    //    foreach (var item in ss)
    //    {
    //        sb.Append(item.Replace('\\', '/') + "/");
    //    }
    //    sb.Remove(sb.Length - 1, 1);
    //    return sb.ToString();
    //}

    //public static async Task<string> WebRequest(string url)
    //{
    //    var request = UnityWebRequest.Get(url);
    //    await request.SendWebRequest();
    //    if (request.isHttpError || request.isNetworkError)
    //        Debuger.Log(Color.red, request.error);
    //    else
    //        return request.downloadHandler.text;
    //    request.Dispose();
    //    return null;
    //}
}
