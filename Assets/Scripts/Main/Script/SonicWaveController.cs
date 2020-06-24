using OceanEnv_ydf.Terrain.Grd;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SonicWaveController : MonoBehaviour, IGameController
{
    public GameObject obj;

    public Projector saomiaoyi;
    public float invert = 120;
    public float depth;
    public float height;
    public List<Transform> targets;
    public GameObject sea;
    public LineRenderer line;
    void Start()
    {
        GameApp.Ins.GameController = this;
        LayerManager_Grd.GetInstance().start();
        LayerManager_Grd.GetInstance().add("terrain", Application.streamingAssetsPath + "/GMRTv3_6_20190430topo1.tif", 109.41f, 18.25f);

        Debug.Log(Application.streamingAssetsPath + "/GMRTv3_6_20190430topo1.tif");

        LayerManager_Grd.GetInstance().update(new Vector2(obj.transform.position.x, obj.transform.position.z));
        oldpos = obj.transform.position;

        GameApp.Ins.camera.SwitchToThirdPersonView(GameObject.Find("全局").transform, new Vector3(80, 0, 0), 500, true, false);

        var main = GameApp.Ins.ui.LoadPanel<MainUI>(CanvasType.Main);
        main.Ini(targets);
        Vector3 pos = obj.transform.position;
        line.SetPosition(0, new Vector3(pos.x, pos.y - 10, pos.z - 2500));
    }


    public void OnDrawGizmos()
    {
        if (saomiaoyi)
        {
            saomiaoyi.farClipPlane = depth;
            saomiaoyi.material.SetFloat("_H", height / depth * 2);
        }
    }


    Vector3 oldpos;
    float t = 0;
    private void Update()
    {
        if (Vector3.SqrMagnitude(obj.transform.position - oldpos) < 8)
        {
            obj.transform.position += new Vector3(0, 0, 20 * Time.deltaTime);
            Vector3 pos = obj.transform.position;
            line.SetPosition(line.positionCount - 1, new Vector3(pos.x, pos.y - 10, pos.z - 3));
        }
        else
        {
            LayerManager_Grd.GetInstance().update(new Vector2(obj.transform.position.x, obj.transform.position.z));
            oldpos = obj.transform.position;
        }
        if ((t -= Time.deltaTime) < 0)
        {
            var s = GameObject.Instantiate(saomiaoyi);
            s.transform.position = saomiaoyi.transform.position;
            t = invert;
            line.positionCount += 1;
        }
    }
}
