using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class SectorProgressBar : MonoBehaviour
{
    public float radius = 2;
    public float startAngleDegree = 0;
    public float angleDegree = 100;
    public int segments = 10;
    public int angleDegreePrecision = 1000;
    public int radiusPrecision = 1000;

    private MeshFilter meshFilter;

    private SectorMeshCreator creator = new SectorMeshCreator();

    [ExecuteInEditMode]
    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
    }
    private void Start()
    {
        meshFilter.mesh = creator.CreateMesh(radius, startAngleDegree, angleDegree, segments, angleDegreePrecision, radiusPrecision);
    }

    //在Scene界面画辅助线
    void OnDrawGizmos()
    {
        Gizmos.color = Color.gray;
        DrawMesh();
    }

    //在Scene界面画辅助线
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        DrawMesh();
    }

    private void DrawMesh()
    {
        Mesh mesh = creator.CreateMesh(radius, startAngleDegree, angleDegree, segments, angleDegreePrecision, radiusPrecision);
        int[] tris = mesh.triangles;
        for (int i = 0; i < tris.Length; i += 3)
        {
            Gizmos.DrawLine(convert2World(mesh.vertices[tris[i]]), convert2World(mesh.vertices[tris[i + 1]]));
            Gizmos.DrawLine(convert2World(mesh.vertices[tris[i]]), convert2World(mesh.vertices[tris[i + 2]]));
            Gizmos.DrawLine(convert2World(mesh.vertices[tris[i + 1]]), convert2World(mesh.vertices[tris[i + 2]]));
        }
    }

    private Vector3 convert2World(Vector3 src)
    {
        return transform.TransformPoint(src);
    }

    private class SectorMeshCreator
    {
        private float radius;
        private float startAngleDegree;
        private float angleDegree;
        private int segments;

        private Mesh cacheMesh;

        /// <summary>  
        /// 创建一个扇形Mesh  
        /// </summary>  
        /// <param name="radius">扇形半径</param>  
        /// <param name="startAngleDegree">扇形开始角度</param> 
        /// <param name="angleDegree">扇形角度</param>  
        /// <param name="segments">扇形弧线分段数</param>  
        /// <param name="angleDegreePrecision">扇形角度精度（在满足精度范围内，认为是同个角度）</param>  
        /// <param name="radiusPrecision">  
        /// <pre>  
        /// 扇形半价精度（在满足半价精度范围内，被认为是同个半价）。  
        /// 比如：半价精度为1000，则：1.001和1.002不被认为是同个半径。因为放大1000倍之后不相等。  
        /// 如果半价精度设置为100，则1.001和1.002可认为是相等的。  
        /// </pre>  
        /// </param>  
        /// <returns></returns>  
        public Mesh CreateMesh(float radius, float startAngleDegree, float angleDegree, int segments, int angleDegreePrecision, int radiusPrecision)
        {
            if (checkDiff(radius, startAngleDegree, angleDegree, segments, angleDegreePrecision, radiusPrecision))
            {//参数有改变才需要重新画mesh
                Mesh newMesh = Create(radius, startAngleDegree, angleDegree, segments);
                if (newMesh != null)
                {
                    cacheMesh = newMesh;
                    this.radius = radius;
                    this.startAngleDegree = startAngleDegree;
                    this.angleDegree = angleDegree;
                    this.segments = segments;
                }
            }
            return cacheMesh;
        }

        private Mesh Create(float radius, float startAngleDegree, float angleDegree, int segments)
        {
            if (segments == 0)
            {
                segments = 1;
#if UNITY_EDITOR
                Debug.Log("segments must be larger than zero.");
#endif
            }

            Mesh mesh = new Mesh();
            Vector3[] vertices = new Vector3[3 + segments - 1];
            vertices[0] = new Vector3(0, 0, 0);//第一个点是圆心点

            //uv是网格上的点对应到纹理上的某个位置的像素, 纹理是一张图片, 所以是二维
            //理解以后才发现, 之前显示出错的原因是原来的代码uv很随意的拿了顶点的计算结果
            Vector2[] uvs = new Vector2[vertices.Length];
            uvs[0] = new Vector2(0.5f, 0.5f);//纹理的圆心在中心

            float angle = Mathf.Deg2Rad * angleDegree;
            float startAngle = Mathf.Deg2Rad * startAngleDegree;
            float currAngle = angle + startAngle; //第一个三角形的起始角度
            float deltaAngle = angle / segments; //根据分段数算出每个三角形在圆心的角的角度
            for (int i = 1; i < vertices.Length; i++)
            {
                //圆上一点的公式: x = r*cos(angle), y = r*sin(angle)
                //根据半径和角度算出弧度上的点的位置
                float x = Mathf.Cos(currAngle);
                float y = Mathf.Sin(currAngle);
                //这里为了我的需求改到了把点算到了(x,y,0), 如果需要其他平面, 可以改成(x,0,y)或者(0,x,y)
                vertices[i] = new Vector3(x * radius, y * radius, 0);
                //纹理的半径就是0.5, 圆心在0.5f, 0.5f的位置
                uvs[i] = new Vector2(x * 0.5f + 0.5f, y * 0.5f + 0.5f);
                currAngle -= deltaAngle;
            }

            int[] triangles = new int[segments * 3];
            for (int i = 0, vi = 1; i < triangles.Length; i += 3, vi++)
            {//每个三角形都是由圆心点+两个相邻弧度上的点构成的
                triangles[i] = 0;
                triangles[i + 1] = vi;
                triangles[i + 2] = vi + 1;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;

            return mesh;
        }

        private bool checkDiff(float radius, float startAngleDegree, float angleDegree, int segments, int angleDegreePrecision, int radiusPrecision)
        {
            return segments != this.segments || (int)(startAngleDegree - this.startAngleDegree) != 0 || (int)((angleDegree - this.angleDegree) * angleDegreePrecision) != 0 ||
                   (int)((radius - this.radius) * radiusPrecision) != 0;
        }
    }
}