using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainUI : MonoBehaviour, IPanel
{
    public Transform Tr { get; set; }
    public DrapdownBASE menu;
    public InputField search;
    public Transform prefab;
    public TrPool<UIItem> pool;
    public List<UIItem> myuiitem;
    public Button sea;
    public class UIItem : ITrPoolItem
    {
        public Transform Tr { get; private set; }
        public Text title;
        public Button btn;
        public Transform target;
        public void OnEnterPool()
        {
            btn.onClick.RemoveAllListeners();
        }

        public void Start(Transform tr)
        {
            Tr = tr;
            title = tr.GetComponentInChildren<Text>();
            btn = tr.GetComponentInChildren<Button>();
        }
    }

    public void Start()
    {
        menu.Add("场景管理");
        menu.Set(0, false);
        menu.onValueChanged.AddListener(OnClickMenu);
        pool = new TrPool<UIItem>(prefab);
        prefab.gameObject.SetActive(false);
        myuiitem = new List<UIItem>();
        search.onValueChanged.AddListener(Search);
        sea.onClick.AddListener(() =>
        {
            var se = GameApp.Ins.GetControllerCompent<GameObject>("sea");
            if (se)
            {
                foreach (Transform item in se.transform)
                {
                    item.gameObject.SetActive(!item.gameObject.activeSelf);
                }
            }
        });
    }

    private void Search(string cus)
    {
        if (string.IsNullOrEmpty(cus))
        {
            foreach (var item in myuiitem)
            {
                item.Tr.gameObject.SetActive(true);
            }
        }
        else
        {
            cus = cus.ToLower();
            foreach (var item in myuiitem)
            {
                if (item.title.text.Contains(cus))
                {
                    item.Tr.gameObject.SetActive(true);
                }
                else
                {
                    item.Tr.gameObject.SetActive(false);
                }
            }
        }
    }


    public void Ini(List<Transform> li)
    {
        StartCoroutine(_Ini(li));
    }

    IEnumerator _Ini(List<Transform> li)
    {
        yield return new WaitForSeconds(1);
        for (int i = 0; i < li.Count; i++)
        {
            var pre = pool.Getprefab();
            pre.Item1.SetAsLastSibling();
            pre.Item2.title.text = li[i].name;
            pre.Item2.target = li[i];
            pre.Item2.btn.onClick.AddListener(() =>
            {
                GameApp.Ins.camera.SwitchToThirdPersonView(pre.Item2.target, new Vector3(80, 0, 0), 200, true, false);
            });
            myuiitem.Add(pre.Item2);
        }
    }

    public void Add(Transform target)
    {
        var pre = pool.Getprefab();
        pre.Item1.SetAsLastSibling();
        pre.Item2.title.text = target.name;
        pre.Item2.target = target;
        pre.Item2.btn.onClick.AddListener(() =>
        {
            GameApp.Ins.camera.SwitchToThirdPersonView(pre.Item2.target, new Vector3(80, 0, 0), 200, true, false);
        });
        myuiitem.Add(pre.Item2);
    }

    private void OnClickMenu(int arg0, string arg1)
    {

    }

    public void OnDestroy()
    {

    }
}