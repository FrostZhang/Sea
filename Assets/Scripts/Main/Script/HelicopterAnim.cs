using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HelicopterAnim : MonoBehaviour
{
    public Transform target;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.RotateAround(target.position,Vector3.up, -Time.deltaTime *10);
    }
}
