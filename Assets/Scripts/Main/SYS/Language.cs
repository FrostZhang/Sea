//using System.Collections;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using UnityEngine;
//using UnityEngine.Networking;

//// Copyright (C) 2019 All Rights Reserved.
//// Detail：Language	MyChessboard	2019/8/12
//// Version：1.0.0
//public class Language
//{
//    public static SortedDictionary<int, string> LanDic { get; private set; }

//    public Language()
//    {
//        LanDic = new SortedDictionary<int, string>();
//    }

//    public async Task Start()
//    {
//        string landata = string.Empty;
//        SystemLanguage language = GameApp.Ins.appConfig.CusLan;
//        if (language.Equals(SystemLanguage.Unknown))
//        {
//            language = Application.systemLanguage;
//            GameApp.Ins.appConfig.CusLan = language;
//        }
//        string url = null;
//        switch (language)
//        {
//            case SystemLanguage.Afrikaans:
//                break;
//            case SystemLanguage.Arabic:
//                break;
//            case SystemLanguage.Basque:
//                break;
//            case SystemLanguage.Belarusian:
//                break;
//            case SystemLanguage.Bulgarian:
//                break;
//            case SystemLanguage.Catalan:
//                break;
//            case SystemLanguage.Chinese:
//            case SystemLanguage.ChineseSimplified:
//                url = GameUnlity.GetstreamingAssets(AppConfig.BUNDLEPATN, "Lan", "Chinese.txt").AbsoluteUri;
//                break;
//            case SystemLanguage.Czech:
//                break;
//            case SystemLanguage.Danish:
//                break;
//            case SystemLanguage.Dutch:
//                break;
//            case SystemLanguage.English:
//                url = GameUnlity.GetstreamingAssets(AppConfig.BUNDLEPATN, "Lan", "English.txt").AbsoluteUri;
//                break;
//            case SystemLanguage.Estonian:
//                break;
//            case SystemLanguage.Faroese:
//                break;
//            case SystemLanguage.Finnish:
//                break;
//            case SystemLanguage.French:
//                break;
//            case SystemLanguage.German:
//                break;
//            case SystemLanguage.Greek:
//                break;
//            case SystemLanguage.Hebrew:
//                break;
//            case SystemLanguage.Icelandic:
//                break;
//            case SystemLanguage.Indonesian:
//                break;
//            case SystemLanguage.Italian:
//                break;
//            case SystemLanguage.Japanese:
//                break;
//            case SystemLanguage.Korean:
//                break;
//            case SystemLanguage.Latvian:
//                break;
//            case SystemLanguage.Lithuanian:
//                break;
//            case SystemLanguage.Norwegian:
//                break;
//            case SystemLanguage.Polish:
//                break;
//            case SystemLanguage.Portuguese:
//                break;
//            case SystemLanguage.Romanian:
//                break;
//            case SystemLanguage.Russian:
//                break;
//            case SystemLanguage.SerboCroatian:
//                break;
//            case SystemLanguage.Slovak:
//                break;
//            case SystemLanguage.Slovenian:
//                break;
//            case SystemLanguage.Spanish:
//                break;
//            case SystemLanguage.Swedish:
//                break;
//            case SystemLanguage.Thai:
//                break;
//            case SystemLanguage.Turkish:
//                break;
//            case SystemLanguage.Ukrainian:
//                break;
//            case SystemLanguage.Vietnamese:
//                break;
//            case SystemLanguage.ChineseTraditional:
//                break;
//            case SystemLanguage.Unknown:
//                break;
//            case SystemLanguage.Hungarian:
//                break;
//            default:
//                break;
//        }
//        if (null != url)
//        {
//            using (var request = UnityWebRequest.Get(url))
//            {
//                await request.SendWebRequest();
//                if (request.isHttpError || request.isNetworkError)
//                    Debuger.Log(Color.red, request.error);
//                else
//                {
//                    using (var dow = request.downloadHandler)
//                    {
//                        landata = dow.text;
//                    }
//                }
//            }
//        }
//        if (!string.IsNullOrEmpty(landata))
//        {
//            landata = landata.Remove(0, 1);
//            try
//            {
//                landata = GameApp.Ins.appConfig.DeAes(landata);
//                var datas = landata.Split(new string[] { "\n" }, System.StringSplitOptions.RemoveEmptyEntries);
//                for (int i = 0; i < datas.Length; i++)
//                {
//                    var lan = datas[i].Split(':');
//                    if (lan != null && lan.Length > 1)
//                    {
//                        LanDic.Add(System.Convert.ToInt32(lan[0]), lan[1]);
//                    }
//                }
//                Debuger.Log(Color.white, language.ToString() + " Success " + LanDic.Count);
//            }
//            catch (System.Exception e)
//            {
//#if UNITY_EDITOR
//                Debuger.Log(Color.red, e);
//#else
//                MessageBox.Ins.ShowMessage(new MesSetting()
//                {
//                    btnType = BtnType.Ok,
//                    content = "语言加载失败，请重启尝试修复。",
//                    btnoktext = "好的",
//                    action = (x) => { Application.Quit(); }
//                });
//#endif
//            }

//        }
//    }

//    public static string GetLan(int lanid)
//    {
//        string ss = string.Empty;
//        LanDic.TryGetValue(lanid, out ss);
//        return ss;
//    }

//}
