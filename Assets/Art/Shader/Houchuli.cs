using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Houchuli : MonoBehaviour {

    public Material ma;
    public Camera ca;
    public bool enable;
    //public Shader shader;
	void Start () {
        ca = GetComponent<Camera>();
        ca.depthTextureMode = DepthTextureMode.DepthNormals;
        //if (shader)
        //{
        //    ma = new Material(shader);
        //}
	}


    public void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        ma.SetMatrix("_CamToWorld", ca.cameraToWorldMatrix);
        Graphics.Blit(source, destination, ma);
    }
    
}
