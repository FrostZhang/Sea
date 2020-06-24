
using UnityEngine;
using UnityEditor;

public class EditorHelp : EditorWindow
{
    static EditorHelp help;
    [MenuItem("Tools/MyTool/EditorHelp")]
    static void DoIt()
    {
        //EditorUtility.DisplayDialog("MyTool", "Do It in C# !", "OK", "");
        help = GetWindow<EditorHelp>();
    }

    Transform tr; Material ma;
    private void OnGUI()
    {
        tr = EditorGUILayout.ObjectField("target",tr, typeof(Transform), true) as Transform;
        ma = EditorGUILayout.ObjectField("Material", ma, typeof(Material), false) as Material;
        if (GUILayout.Button("制作"))
        {
            var render =  tr.GetComponentsInChildren<MeshRenderer>();

            foreach (var item in render)
            {
                var n = item.materials.Length;
                Material[] mas = new Material[n];
                for (int i = 0; i < mas.Length; i++)
                {
                    mas[i] = ma;
                }
                item.materials = mas;
            }
        }
    }
}