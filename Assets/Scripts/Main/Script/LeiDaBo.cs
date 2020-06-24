using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeiDaBo : MonoBehaviour
{
    public Material nor;
    public Material wire;
    public GameObject qipao;
    public GameObject chencuan;
    IEnumerator Start()
    {
        yield return new WaitForSeconds(2);
        GetComponent<MeshCollider>().sharedMesh = GetComponent<MeshFilter>().sharedMesh;
    }

    RaycastHit hit;
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(other);
        if (other.gameObject.layer.Equals(9))
        {
            if (Random.Range(0, 101) > 90)
            {
                if (Physics.Raycast(transform.position, -Vector3.up, out hit, 3000))
                {
                    if (Random.Range(0, 101) > 25)
                    {
                        Instant(qipao);
                    }
                    else
                    {
                        Instant(chencuan);
                    }
                }
                // var pos = transform.position - Vector3.up * 1500 + UnityEngine.Random.onUnitSphere * 50;
            }
        }
    }

    private void Instant(GameObject go)
    {
        var q = Instantiate(go, hit.point - Vector3.up * 38.5f, go.transform.rotation);
        q.name = go.name + q.GetInstanceID().ToString();
        GameApp.Ins.ui.GetPanel<MainUI>().Add(q.transform);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer.Equals(9))
        {

        }
    }
}
