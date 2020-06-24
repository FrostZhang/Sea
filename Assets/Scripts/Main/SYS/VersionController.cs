//using System.Collections.Generic;
//using System.IO;
//using System.Threading.Tasks;
//using UnityEngine;
//using UnityEngine.Networking;

//// Copyright (C) 2019 All Rights Reserved.
//// Detail：VersionController	MyChessboard	2019/8/26
//// Version：1.0.0
//public class VersionController
//{
//    UpdatePanel up;

//    public async Task Start()
//    {
//        up = await GameApp.Ins.ui.LoadPanel<UpdatePanel>(CanvasType.Top);
//        up.slider.Set(0, 100, false);
//        up.Show("检查版本", null);
//        await Check();
//    }

//    List<string> downloadfiles = new List<string>();
//    Dictionary<string, string> localmd5, severmd5;
//    private async Task Check()
//    {
//        int flag = 0;
//        Dictionary<string, string> local = await LondLocalMD5();
//        Dictionary<string, string> sever = await LondSeverMD5();
//        if (null == sever)
//        {
//            Debuger.Log(Color.red, "cont connect sever");
//            return;
//        }
//        foreach (var item in sever)
//        {
//            if (null != local && local.ContainsKey(item.Key))
//            {
//                if (local[item.Key] != item.Value)
//                {
//                    flag++;
//                    downloadfiles.Add(item.Key);
//                    //MD5比对失败 下载
//                }
//                else
//                {
//                    var url = GameUnlity.Getpersistentdata(item.Key);
//                    if (!File.Exists(url.AbsolutePath))
//                    {
//                        url = GameUnlity.GetstreamingAssets(item.Key);
//                    }
//                    var md5 = await GameUnlity.BuildFileMd5(url);
//                    if (md5 != item.Value)
//                    {
//                        flag++;
//                        downloadfiles.Add(item.Key);
//                        //本地文件丢失或不完整 下载覆盖
//                    }
//                }
//            }
//            else
//            {
//                flag++;
//                downloadfiles.Add(item.Key);
//                //下载
//            }
//        }
//        if (flag > 0)
//        {
//            //启动下载器
//            Debuger.Log(Color.magenta, "start downloader. file num " + flag);
//            await BeginDownload();
//        }
//        else
//        {
//            Debuger.Log(Color.white, "its new version" + flag);
//        }
//        up.Show("更新成功", null);
//    }

//    private async Task BeginDownload()
//    {
//        using (System.Net.WebClient web = new System.Net.WebClient())
//        {
//            web.DownloadProgressChanged += (x, y) =>
//            {
//                up.slider.Set(y.ProgressPercentage, false);
//                //Debug.Log(web.BaseAddress + y.ProgressPercentage);
//            };

//            try
//            {
//                for (int i = 0; i < downloadfiles.Count; i++)
//                {
//                    up.Show("准备下载 " + (i + 1) + "/" + downloadfiles.Count, null);
//                    var p = downloadfiles[i];
//                    System.Uri localpath = null;
//#if UNITY_EDITOR
//                    localpath = GameUnlity.GetstreamingAssets(p);
//#else
//            localpath = GameUnlity.Getpersistentdata(p);
//#endif
//                    var dir = Path.GetDirectoryName(localpath.AbsolutePath);
//                    if (!Directory.Exists(dir))
//                    {
//                        Directory.CreateDirectory(dir);
//                    }

//                    var addess = GameApp.Ins.appConfig.severforupdata + p;
//                    Debuger.Log(Color.magenta, "start download " + addess);
//                    await web.DownloadFileTaskAsync(addess, localpath.AbsolutePath);
//                }
//                Debuger.Log(Color.white, "download files ok. begin update version file");
//                //替换version文件
//                System.Uri url = null;
//#if UNITY_EDITOR
//                url = GameUnlity.GetstreamingAssets(AppConfig.BUNDLEPATN, "version.txt");
//#else
//            url = GameUnlity.Getpersistentdata(AppConfig.BUNDLEPATN, "version.txt");
//#endif
//                await web.DownloadFileTaskAsync(GameApp.Ins.appConfig.severforupdata + AppConfig.BUNDLEPATN + "/version.txt",
//                    url.AbsolutePath);

//            }
//            catch (System.Exception e)
//            {
//                Debug.LogError(e + " update failed");
//            }
//            finally
//            {
//                web.Dispose();
//            }
//        }
//    }

//    private async Task<Dictionary<string, string>> LondLocalMD5()
//    {
//        string localversion = null;
//        UnityWebRequest request = null;
//        var url = GameUnlity.Getpersistentdata(AppConfig.BUNDLEPATN, "version.txt");
//        if (File.Exists(url.AbsolutePath))
//            request = UnityWebRequest.Get(url.AbsoluteUri);
//        else
//            request = UnityWebRequest.Get(GameUnlity.GetstreamingAssets(AppConfig.BUNDLEPATN, "version.txt").AbsoluteUri);
//        await request.SendWebRequest();
//        if (request.isHttpError || request.isNetworkError)
//        {
//            return null;
//        }
//        else
//        {
//            using (var dow = request.downloadHandler)
//            {
//                localversion = dow.text;
//            }
//        }
//        request.Dispose();
//        if (!string.IsNullOrEmpty(localversion))
//            return JiexiVersion(localversion);
//        return null;
//    }

//    private async Task<Dictionary<string, string>> LondSeverMD5()
//    {
//        string severversion = null;
//        using (UnityWebRequest request = UnityWebRequest.Get(
//            GameApp.Ins.appConfig.severforupdata + AppConfig.BUNDLEPATN + "/version.txt"))
//        {
//            await request.SendWebRequest();
//            if (request.isHttpError || request.isNetworkError)
//            {
//                return null;
//            }
//            else
//            {
//                using (var dow = request.downloadHandler)
//                {
//                    severversion = dow.text;
//                }
//            }
//            if (!string.IsNullOrEmpty(severversion))
//                return JiexiVersion(severversion);
//            return null;
//        }
//    }

//    private Dictionary<string, string> JiexiVersion(string text)
//    {
//        try
//        {
//            Dictionary<string, string> temp = new Dictionary<string, string>();
//            string[] files = text.Remove(0, 1).Trim()
//               .Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
//            foreach (var item in files)
//            {
//                string[] ss = item.Split(':');
//                if (ss != null && ss.Length > 1)
//                {
//                    temp.Add(ss[0], ss[1]);
//                }
//            }
//            return temp;
//        }
//        catch (System.Exception e)
//        {
//            Debuger.Log(Color.red, e);
//        }
//        return null;
//    }
//}