using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GodRay : MonoBehaviour
{
    public Material material;
    public MeshFilter meshFilter;
    public Mesh mesh;
    //public Light spot;
    public float dis = 3000;

    public Color color;

    public void Start()
    {
        material = new Material(material);
        Camera.onPostRender += OnPostRender;
    }

    void OnPostRender(Camera ca)
    {
        if (!Application.isPlaying)
            return;
        var dir = transform.InverseTransformDirection(transform.forward);
        var lightPos = new Vector4(dir.x, dir.y, -dir.z, 0);
        material.SetVector("litPos", lightPos);
        Mesh mesh = meshFilter.sharedMesh;
        material.SetPass(0);
        if (this.mesh)
        {
            mesh = this.mesh;
        }
        Graphics.DrawMeshNow(mesh, meshFilter.transform.localToWorldMatrix);

        //spot.color = color;
        material.SetColor("_Color", color);
        material.SetFloat("_Dis", dis);
    }

    private void OnDestroy()
    {
        Camera.onPostRender -= OnPostRender;
    }
}
