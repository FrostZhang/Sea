using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Unity通用帮助功能类函数
/// </summary>
public static class CommonUnityUtility
{
    /// <summary>
    /// 通过给定的名称，找到第一个符合名称的子物体
    /// </summary>
    /// <param name="go">待查找的父物体</param>
    /// <param name="childObjName">要查找的子物体的名称</param>
    /// <param name="includeInactive">是否包含未激活的物体</param>
    /// <returns>子物体</returns>
    public static GameObject FindObj(this GameObject go, string childObjName, bool includeInactive = true)
    {
        if ((includeInactive || !includeInactive && go.activeInHierarchy) && go.name.Equals(childObjName))
            return go;

        return FindFirstLevelChildObjByName(go, childObjName, includeInactive);
    }

    private static GameObject FindFirstLevelChildObjByName(GameObject go, string childObjName, bool includeInactive = true)
    {
        int childCount = go.transform.childCount;

        for (int i = 0; i < childCount; i++)
        {
            Transform t = go.transform.GetChild(i);
            if ((includeInactive || !includeInactive && t.gameObject.activeInHierarchy) && t.name.Equals(childObjName))
                return t.gameObject;

            GameObject result = FindFirstLevelChildObjByName(t.gameObject, childObjName, includeInactive);
            if (result)
                return result;
        }

        return null;
    }

    public static void SetParent(this GameObject go,GameObject parentObj)
    {
        if (parentObj==null)
        {
            go.transform.SetParent(null);
        }
        else
        {
            go.transform.SetParent(parentObj.transform);
        }
        
    }

    public static string Substr(this string str,int count)
    {
        string[] strs = str.Split('.');
        string newstr;
        if (strs.Length==2)
        {
            if (strs[1].Length>count)
            {
                newstr = strs[0] +'.' + strs[1].Substring(0, count);
            }
            else
            {
                newstr = str;
            }
        }
        else
        {
            newstr = str;
        }
        return newstr;
    }
    /// <summary>
    /// 获取角度 -180-180
    /// </summary>
    /// <param name="angle"></param>
    /// <returns></returns>
    public static float GetAngle(this float angle)
    {
        if (angle>180)
        {
            angle -= 360;
        }
        else if(angle<-180)
        {
            angle += 360;
        }
        return angle;
    }

    public static void ChangeAllLayer(this GameObject targetObj,int layerIndex)
    {
        targetObj.layer = layerIndex;
        if (targetObj.transform.childCount>0)
        {
            for (int i = 0; i < targetObj.transform.childCount; i++)
            {
                targetObj.transform.GetChild(i).gameObject.ChangeAllLayer(layerIndex);
            }
        }
    }
    /// <summary>
    /// 改变材质球的透明度
    /// </summary>
    /// <param name="material"></param>
    /// <param name="alphaValue"></param>
    public static void ChangeMaterialAlpha(this Material material,float alphaValue)
    {
        Color color = material.color;
        material.color = new Color(color.a, color.g, color.b, alphaValue);
    }
    /// <summary>
    /// 改变某个对象整体的透明度
    /// </summary>
    /// <param name="targetObj"></param>
    /// <param name="alphaValue"></param>
    public static void ChangeAllObjMaterialAlpha(this GameObject targetObj,float alphaValue)
    {
        if (targetObj.GetComponent<MeshRenderer>())
        {
            Material material = targetObj.GetComponent<MeshRenderer>().material;
            material.ChangeMaterialAlpha(alphaValue);
        }
        if (targetObj.transform.childCount>0)
        {
            for (int i = 0; i < targetObj.transform.childCount; i++)
            {
                targetObj.transform.GetChild(i).gameObject.ChangeAllObjMaterialAlpha(alphaValue);
            }
        }
    }

    ///// <summary>
    ///// 通过给定的名称在指定的场景中查找对象
    ///// </summary>
    ///// <param name="goName">对象的名称</param>
    ///// <param name="includeInactive">是否包含未激活的物体</param>
    ///// <param name="scenes">查找的范围（如果为空，则在所有场景中查找）</param>
    ///// <returns>查找后的结果</returns>
    //public static GameObject FindObjByScenes(string goName, bool includeInactive = true, params Scene[] scenes)
    //{
    //    SceneMgr sceneMgr = ProjectContext.Instance.Container.Resolve<SceneMgr>();

    //    if (scenes.Length == 0)
    //        scenes = sceneMgr.GetAllLoadedScenes();

    //    foreach (Scene scene in scenes)
    //    {
    //        GameObject[] rootGameObjects = scene.GetRootGameObjects();
    //        foreach (GameObject rootGo in rootGameObjects)
    //        {
    //            GameObject result = rootGo.FindObj(goName, includeInactive);
    //            if (result)
    //                return result;
    //        }
    //    }

    //    return null;
    //}

    ///// <summary>
    ///// 通过给定的名称在指定的名称场景中查找对象
    ///// </summary>
    ///// <param name="goName">对象的名称</param>
    ///// <param name="includeInactive">是否包含未激活的物体</param>
    ///// <param name="scenesName">查找的范围（如果为空，则在所有场景中查找）</param>
    ///// <returns>查找后的结果</returns>
    //public static GameObject FindObjByScenesName(string goName, bool includeInactive = true, params string[] scenesName)
    //{
    //    SceneMgr sceneMgr = ProjectContext.Instance.Container.Resolve<SceneMgr>();

    //    if (scenesName.Length == 0)
    //        scenesName = sceneMgr.GetAllLoadedScenesName();

    //    foreach (string sceneName in scenesName)
    //    {
    //        Scene scene = SceneManager.GetSceneByName(sceneName);
    //        if (!scene.IsValid() || !scene.isLoaded)
    //            continue;

    //        GameObject[] rootGameObjects = scene.GetRootGameObjects();
    //        foreach (GameObject rootGo in rootGameObjects)
    //        {
    //            GameObject result = rootGo.FindObj(goName, includeInactive);
    //            if (result)
    //                return result;
    //        }
    //    }

    //    return null;
    //}
}
