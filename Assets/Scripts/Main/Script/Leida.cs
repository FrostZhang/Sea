using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Leida : MonoBehaviour
{
    Material ma;
    // Start is called before the first frame update
    void Start()
    {
        ma = GetComponent<MeshRenderer>().sharedMaterial;
        colliders = new List<Collider>();
        v4s = new List<Vector4>();
    }

    List<Collider> colliders;
    List<Vector4> v4s;

    private void OnTriggerEnter(Collider other)
    {
        if (!colliders.Contains(other))
        {
            colliders.Add(other);
        }
    }

    private void Update()
    {
        v4s.Clear();
        for (int i = 0; i < colliders.Count; i++)
        {
            if (i > 9)
            {
                break;
            }
            var localPos = transform.InverseTransformPoint(colliders[i].transform.position);
            float x = localPos.x / 5;   //？为什么是5
            float z = localPos.z / 5;

            x = -0.5f * x + 0.5f;
            z = -0.5f * z + 0.5f;
            v4s.Add(new Vector4(x, z, 0, 0));
        }
        if (v4s.Count<10)
        {
            for (int i = v4s.Count; i < 10; i++)
            {
                v4s.Add(new Vector4(-1, -1, 0, 0));
            }
        }
        ma.SetVectorArray("_Targets", v4s);
    }


    private void OnTriggerExit(Collider other)
    {
        colliders.Remove(other);
    }
}
