using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.ComponentModel;
using UnityEngine.Events;
using Mono.Data.Sqlite;

namespace Asher.Extension
{
    public static class UIExtension
    {
        public static void Bingding(this Text ui, string name, INotifyPropertyChanged x)
        {
            PropertyChangedEventHandler foo = null;
            var prop = x.GetType().GetProperty(name);
            foo = delegate (object sender, PropertyChangedEventArgs e)
            {
                if (ui.IsDestroyed())
                {
                    x.PropertyChanged -= foo;
                    return;
                }
                if (e.PropertyName == name)
                {
                    ui.text = prop.GetValue(x, null).ToString();
                }
            };
            if (prop == null)
            {
                Debug.LogError("can't find property :" + name);
                return;
            }
            x.PropertyChanged += foo;
            ui.text = prop.GetValue(x, null).ToString();
        }

        public static void Bingding(this Slider ui, string name, INotifyPropertyChanged x, Bingdingway bdw = Bingdingway.one)
        {
            PropertyChangedEventHandler foo = null;
            var prop = x.GetType().GetProperty(name);
            foo = delegate (object sender, PropertyChangedEventArgs e)
            {
                if (ui.IsDestroyed())
                {
                    x.PropertyChanged -= foo;
                    return;
                }
                if (e.PropertyName == name)
                {
                    ui.value = (float)prop.GetValue(x, null);
                }
            };
            x.PropertyChanged += foo;
            if (prop == null || !prop.PropertyType.Equals(typeof(float)))
            {
                Debug.LogError("can't find property :" + name);
                return;
            }
            ui.value = (float)prop.GetValue(x, null);
            if (bdw == Bingdingway.two)
            {
                ui.onValueChanged.AddListener((_) =>
                {
                    prop.SetValue(x, System.Convert.ChangeType(ui.value, prop.PropertyType), null);
                });
            }
        }

        public static void Bingding(this InputField ui, string name, INotifyPropertyChanged x, Bingdingway bdw = Bingdingway.one)
        {
            PropertyChangedEventHandler foo = null;
            var prop = x.GetType().GetProperty(name);
            foo = delegate (object sender, PropertyChangedEventArgs e)
            {
                if (ui.IsDestroyed())
                {
                    x.PropertyChanged -= foo;
                    return;
                }
                if (e.PropertyName == name)
                {
                    ui.text = (string)System.Convert.ChangeType(prop.GetValue(x, null), typeof(string));
                }
            };
            if (prop == null)
            {
                Debug.LogError("can't find property :" + name);
                return;
            }
            x.PropertyChanged += foo;
            ui.text = (string)System.Convert.ChangeType(prop.GetValue(x, null), typeof(string));
            if (bdw == Bingdingway.two)
            {
                ui.onEndEdit.AddListener((_) =>
                {
                    if (!string.IsNullOrWhiteSpace(ui.text))
                    {
                        prop.SetValue(x, System.Convert.ChangeType(ui.text, prop.PropertyType), null);
                    }
                });
            }
        }

        //public static void BingdingColor(this Image ui, string name, INotifyPropertyChanged x)
        //{
        //    PropertyChangedEventHandler foo = null;
        //    var prop = x.GetType().GetProperty(name);
        //    foo = delegate(object sender, PropertyChangedEventArgs e)
        //    {
        //        if (ui.IsDestroyed())
        //        {
        //            x.PropertyChanged -= foo;
        //            return;
        //        }
        //        if (e.PropertyName == name)
        //        {
        //            ui.color = prop.GetValue(x, null);
        //        }
        //    };
        //}

        //public static void Bingding(this ToggleBASE ui, string name, INotifyPropertyChanged x, Bingdingway bdw = Bingdingway.one)
        //{
        //    PropertyChangedEventHandler foo = null;
        //    var prop = x.GetType().GetProperty(name);
        //    foo = delegate (object sender, PropertyChangedEventArgs e)
        //    {
        //        if (ui.IsDestroyed())
        //        {
        //            x.PropertyChanged -= foo;
        //            return;
        //        }
        //        if (e.PropertyName == name)
        //        {
        //            ui.Set((bool)System.Convert.ChangeType(prop.GetValue(x, null), typeof(bool)), false);
        //        }
        //    };
        //    if (prop == null)
        //    {
        //        Debug.LogError("can't find property :" + name);
        //        return;
        //    }
        //    ui.Set((bool)System.Convert.ChangeType(prop.GetValue(x, null), typeof(bool)), false);
        //    x.PropertyChanged += foo;
        //    if (bdw == Bingdingway.two)
        //    {
        //        ui.onValueChanged.AddListener((_) =>
        //        {
        //            prop.SetValue(x, System.Convert.ChangeType(_, prop.PropertyType), null);
        //        });
        //    }
        //}

        public static void Bingding(this Dropdown ui, string name, INotifyPropertyChanged x, Bingdingway bdw = Bingdingway.one)
        {
            PropertyChangedEventHandler foo = null;
            var prop = x.GetType().GetProperty(name);
            foo = delegate (object sender, PropertyChangedEventArgs e)
            {
                if (ui.IsDestroyed())
                {
                    x.PropertyChanged -= foo;
                    return;
                }
                if (e.PropertyName == name)
                {
                    ui.value = (int)System.Convert.ChangeType(prop.GetValue(x, null), typeof(int));
                }
            };
            if (prop == null)
            {
                Debug.LogError("can't find property :" + name);
                return;
            }
            ui.value = (int)System.Convert.ChangeType(prop.GetValue(x, null), typeof(int));
            x.PropertyChanged += foo;
            if (bdw == Bingdingway.two)
            {
                ui.onValueChanged.AddListener((_) =>
                {
                    prop.SetValue(x, System.Convert.ChangeType(_, prop.PropertyType), null);
                });
            }
        }

    }
    public enum Bingdingway
    {
        one, two
    }

}

public static class SqlitExtension
{
    public static List<string> GetData(this SqliteDataReader reader)
    {
        List<string> list = new List<string>();
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
}

public static class UnityExtension
{
    public static void SetRectPositionAndRotation(this Transform tr, Vector3 pos, Quaternion ro)
    {
        var re = tr as RectTransform;
        if (re)
        {
            re.anchoredPosition = pos;
            re.rotation = ro;
        }
    }

    public static Vector2 getanchoredPosition(this Transform tr)
    {
        var re = tr as RectTransform;
        if (re)
        {
            return re.anchoredPosition;
        }
        return tr.position;
    }

    public static void Set(this Image image)
    {
        image.SetNativeSize();
    }
}

public static class TransformExtensions
{
    public static void CopyFromWorld(this Transform t, Transform source)
    {
        t.position = source.position;
        t.rotation = source.rotation;
        // 注意：此处使用世界坐标系下的缩放系数赋予给局部坐标系下的缩放系数
        t.localScale = source.lossyScale;
    }

    public static void CopyFromLocal(this Transform t, Transform source)
    {
        t.localPosition = source.localPosition;
        t.localRotation = source.localRotation;
        t.localScale = source.localScale;
    }

    public static Bounds GetMaxBounds(this Transform t)
    {
        Bounds b = new Bounds();
        Renderer[] childRenderers = t.gameObject.GetComponentsInChildren<Renderer>();
        if (childRenderers.Length > 0)
        {
            b = childRenderers[0].bounds;
            for (int i = 1; i < childRenderers.Length; ++i)
            {
                b.Encapsulate(childRenderers[i].bounds);
            }
        }
        else
        {
            b.center = t.position;
        }

        return b;
    }

    public static void AutoScale(this Transform tr)
    {
        var bound = tr.GetMaxBounds();
        var max = Mathf.Max(bound.size.x, bound.size.y, bound.size.z);
        float scale = 1;
        while (max > 100)
        {
            scale *= 0.1f;
            max *= 0.1f;
        }
        scale = Mathf.Max(0.0001f, scale);
        //Vector3 v3 = new Vector3(bound.center.x, 0, bound.center.z);
        //foreach (Transform item in tr)
        //{
        //    item.position -= v3;
        //}
        tr.localScale = new Vector3(scale, scale, scale);
        //Debug.Log(bound.center);
    }

    public static T GetOrAddComponent<T>(this GameObject go) where T : UnityEngine.Component
    {
        var t = go.GetComponent<T>();
        if (null == t)
        {
            t = go.AddComponent<T>();
        }
        return t;
    }

    public static T GetOrAddComponent<T>(this Transform go) where T : UnityEngine.Component
    {
        var t = go.GetComponent<T>();
        if (null == t)
        {
            t = go.gameObject.AddComponent<T>();
        }
        return t;
    }
}

