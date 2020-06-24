using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeiDaKong : MonoBehaviour
{
    public List<Transform> targets;

    Transform prefab;
    List<GodRayTr> ps;

    TrPool<GodRayTr> pool;

    public class GodRayTr : ITrPoolItem
    {
        public GodRay godray;
        public Transform Tr { get; private set; }
        public void OnEnterPool()
        {

        }

        public void Start(Transform tr)
        {
            Tr = tr;
            godray = tr.GetComponent<GodRay>();
        }
    }

    void Start()
    {
        prefab = transform.GetChild(0);
        prefab.gameObject.SetActive(false);
        pool = new TrPool<GodRayTr>(prefab);
        ps = new List<GodRayTr>();
    }

    void Update()
    {
        for (int i = 0; i < ps.Count; i++)
        {
            pool.Reprefab(ps[i].Tr);
        }
        ps.Clear();
        for (int i = 0; i < targets.Count; i++)
        {
            var pre = pool.Getprefab();
            pre.Item1.LookAt(targets[i]);
            pre.Item2.godray.dis = Vector3.Distance(targets[i].position, pre.Item1.position);
            ps.Add(pre.Item2);
        }
    }
}
