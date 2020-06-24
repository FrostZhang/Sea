using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[AddComponentMenu("UI/DrapdownTest", 45), RequireComponent(typeof(RectTransform)), RequireComponent(typeof(ToggleBASEGroup))]
public class DrapdownBASE : UIBehaviour
{
    private int m_Value;
    private bool iniValueForUI;

    public class DropdownEvent : UnityEvent<int, string> { }
    [SerializeField, Space]
    public DropdownEvent onValueChanged = new DropdownEvent();

    public Transform prefab;

    List<ToggleBASE> toggles;
    ToggleBASEGroup groupTest;
    Queue<Transform> pool;
    protected override void Awake()
    {
        groupTest = GetComponent<ToggleBASEGroup>();
        pool = new Queue<Transform>();
        toggles = new List<ToggleBASE>();
        if (!prefab)
            prefab = transform.GetChild(0);
        prefab.gameObject.SetActive(false);
        var tgs = GetComponentsInChildren<ToggleBASE>();
        foreach (var item in tgs)
        {
            if (item != prefab)
            {
                Add(item.GetComponentInChildren<Text>().text);
            }
        }
    }

    private void ChangeChoose(int c, bool arg0, string na)
    {
        if (arg0)
        {
            onValueChanged.Invoke(c, na);
            m_Value = c;
        }
        else if (toggles.TrueForAll((x) => !x.isOn))
        {
            onValueChanged.Invoke(-1, string.Empty);
        }
    }

    public void Set(int input, bool sendCallback = true)
    {
        if (input < toggles.Count && input > -1)
        {
            toggles[input].Set(true, sendCallback);
        }
        else
        {
            foreach (var item in toggles)
            {
                item.Set(false, sendCallback);
            }
        }
    }

    public Transform Getprefab()
    {
        if (pool.Count > 0)
        {
            return pool.Dequeue();
        }
        else
        {
            return Instantiate(prefab, prefab.parent);
        }
    }

    public void Reprefab(Transform pre)
    {
        pre.gameObject.SetActive(false);
        pool.Enqueue(pre);
    }

    public ToggleBASE Add(string content)
    {
        if (toggles.Exists((x) => x.name == content))
            return null;
        var t = Getprefab();
        t.SetAsLastSibling();
        t.gameObject.SetActive(true);
        if (!string.IsNullOrEmpty(content))
        {
            t.GetComponentInChildren<Text>().text = content;
            t.name = content;
        }
        var tog = t.GetComponent<ToggleBASE>();
        tog.onValueChanged.AddListener((x) =>
        {
            int i = toggles.FindIndex((a) => a == tog);
            if (i != -1)
            {
                ChangeChoose(i, x, tog.name);
            }
        });
        toggles.Add(tog);
        return tog;
    }

    public void Remove(string name)
    {
        var t = toggles.Find(x => x.name == name);
        if (t)
        {
            t.onValueChanged.RemoveAllListeners();
            toggles.Remove(t);
            Reprefab(t.transform);
        }
    }

    public void RemoveAll()
    {
        for (int i = 0; i < toggles.Count; i++)
        {
            toggles[i].onValueChanged.RemoveAllListeners();
            Reprefab(toggles[i].transform);
        }
        toggles.Clear();
    }
}
