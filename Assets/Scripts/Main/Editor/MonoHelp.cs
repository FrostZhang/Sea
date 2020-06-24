using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[CustomEditor(typeof(MonoBehaviour))]
public class MonoHelp : Editor
{
    [MenuItem("GameObject/-- Copy Mono Rela --", false, 0)]
    [AddComponentMenu("GameObject/MonoHelp")]
    public static void CreateMediaPlayerWidthUnityAudioEditor()
    {
        // SceneModeUtility.GetSelectedObjectsOfType<>  ≤È’“ŒÔÃÂ
        var go = Selection.activeGameObject;
        //var asset = AssetDatabase.GetAssetOrScenePath(go);
        //var dps = AssetDatabase.GetDependencies(asset);
        //foreach (var item in dps)
        //{
        //    Debug.Log(item);
        //}
        if (go)
        {
            var script = go.GetComponent<MonoBehaviour>();
            if (script)
            {
                var field = script.GetType().GetFields();
                StringBuilder sb = new StringBuilder();
                foreach (var item in field)
                {
                    var va = item.GetValue(script) as UnityEngine.Object;
                    if (va)
                    {
                        Transform tr = va as Transform;
                        if (tr)
                        {
                            string path = GetTrString(go.transform, tr, string.Empty);
                            sb.AppendLine(item.Name + " = tr.Find(\"" + path + "\").GetComponent<Transform>();");
                        }
                        MonoBehaviour mono = va as MonoBehaviour;
                        if (mono)
                        {
                            string path = GetTrString(go.transform, mono.transform, string.Empty);
                            sb.AppendLine(item.Name + " = tr.Find(\"" + path + "\").GetComponent<" + item.GetValue(script).GetType().Name + ">();");
                        }
                    }
                }
                GUIUtility.systemCopyBuffer = sb.ToString();
            }
        }
    }

    public static string GetTrString(Transform pa, Transform self, string str)
    {
        str = "/" + self.name + str;
        if (self.parent && self.parent != pa)
        {
            return GetTrString(pa, self.parent, str);
        }
        else
        {
            return str.Remove(0, 1);
        }
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Reflect"))
        {

        }
    }
}
