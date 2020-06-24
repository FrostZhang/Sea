using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrPool<T> where T : ITrPoolItem, new()
{
    public Queue<Transform> pool;
    Transform prefab;
    Dictionary<int, T> prefabScripts;

    public TrPool(Transform t, int count = 10)
    {
        prefab = t;
        pool = new Queue<Transform>(count);
        prefabScripts = new Dictionary<int, T>(30);
    }

    public Tuple<Transform, T> Getprefab()
    {
        if (pool.Count > 0)
        {
            var t = pool.Dequeue();
            t.gameObject.SetActive(true);
            return new Tuple<Transform, T>(t, prefabScripts[t.GetInstanceID()]);
        }
        else
        {
            var t = UnityEngine.Object.Instantiate(prefab, prefab.parent);
            t.gameObject.SetActive(true);
            var sp = new T();
            sp.Start(t);
            prefabScripts.Add(t.GetInstanceID(), sp);
            return new Tuple<Transform, T>(t, sp);
        }
    }

    public void Reprefab(Transform pre)
    {
        prefabScripts[pre.GetInstanceID()].OnEnterPool();
        pre.gameObject.SetActive(false);
        pool.Enqueue(pre);
    }

    public void Clear()
    {
        prefabScripts.Clear();
        foreach (var item in pool)
            UnityEngine.Object.Destroy(item);
        pool.Clear();
        pool = null;
        prefab = null;
    }

    public T Script(int trname)
    {
        return prefabScripts[trname];
    }
}