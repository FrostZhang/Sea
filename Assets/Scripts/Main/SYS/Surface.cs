using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// Copyright (C) 2019 All Rights Reserved.
// Detail：UI	Red	2019/10/12
// Version：1.0.0
public class Surface
{
    public Canvas[] canvas;

    List<IPanel> panels;

    public Surface(Transform pa)
    {
        canvas = new Canvas[4];
        canvas[0] = pa.Find("UI/World").GetComponent<Canvas>();
        canvas[1] = pa.Find("UI/Main").GetComponent<Canvas>();
        canvas[2] = pa.Find("UI/Top").GetComponent<Canvas>();
        panels = new List<IPanel>();
    }

    // 不加载两个相同的Panel
    //public async Task<T> LoadPanel<T>(T panel, CanvasType ct) where T : IPanel
    //{
    //    string n = typeof(T).Name;
    //    var c = canvas[(int)ct];
    //    var t = panels.Find(x => x is T);
    //    if (t != null)
    //    {
    //        return (T)t;
    //    }
    //    else
    //    {
    //        var p = await CashData.LoadCash<Transform>("UI/" + n, false);
    //        Transform tr = Object.Instantiate(p, c.transform);
    //        T va = tr.GetComponent<T>();
    //        if (va != null)
    //        {
    //            va.tr = tr;
    //            va.Start();
    //            panels.Add(va);
    //            return va;
    //        }
    //        if (panel is IAppUpdate)
    //        {
    //            GameApp.Ins.Updates.Add(panel as IAppUpdate);
    //        }
    //        panel.tr = tr;
    //        panel.Start();
    //        panels.Add(panel);
    //        return panel;
    //    }
    //}

    public T LoadPanel<T>(CanvasType ct) where T : IPanel, new()
    {
        string n = typeof(T).Name;
        var c = canvas[(int)ct];
        var t = panels.Find(x => x is T);
        if (t != null)
        {
            return (T)t;
        }
        else
        {
            var p = CashData.LoadCash<Transform>("UI/" + n, false);
            Transform tr = Object.Instantiate(p, c.transform);
            if (typeof(T).IsSubclassOf(typeof(MonoBehaviour)))
            {
                T va = tr.GetComponent<T>();
                if (va != null)
                {
                    va.Tr = tr;
                    //va.Start();
                    panels.Add(va);
                    return va;
                }
            }
            T panel = new T();
            if (panel is IAppUpdate)
            {
                GameApp.Ins.Updates.Add(panel as IAppUpdate);
            }
            panel.Tr = tr;
            panel.Start();
            panels.Add(panel);
            return panel;
        }
    }

    public T LoadPanelNow<T>(CanvasType ct) where T : IPanel, new()
    {
        string n = typeof(T).Name;
        var c = canvas[(int)ct];
        var t = panels.Find(x => x is T);
        if (t != null)
        {
            return (T)t;
        }
        else
        {
            var p = CashData.LoadCashNow<Transform>("UI/" + n, false);
            Transform tr = Object.Instantiate(p, c.transform);
            if (typeof(T).IsSubclassOf(typeof(MonoBehaviour)))
            {
                T va = tr.GetComponent<T>();
                if (va != null)
                {
                    va.Tr = tr;
                    //va.Start();
                    panels.Add(va);
                    return va;
                }
            }
            T panel = new T();
            if (panel is IAppUpdate)
            {
                GameApp.Ins.Updates.Add(panel as IAppUpdate);
            }
            panel.Tr = tr;
            panel.Start();
            panels.Add(panel);
            return panel;
        }
    }

    public void RemovePanel<T>() where T : IPanel
    {
        var t = panels.Find(x => x is T);
        if (t != null)
        {
            panels.Remove(t);
            t.OnDestroy();
            if (t is IAppUpdate)
            {
                GameApp.Ins.Updates.Remove(t as IAppUpdate);
            }
            Object.Destroy(t.Tr.gameObject);
        }
    }

    public T GetPanel<T>() where T : IPanel
    {
        var p = panels.Find(x => x is T);
        if (p != null)
        {
            return (T)p;
        }
        else
        {
            return default(T);
        }
    }


    public Canvas GetCanvas(CanvasType ct)
    {
        return canvas[(int)ct];
    }
}

public enum CanvasType
{
    World, Main, Top
}