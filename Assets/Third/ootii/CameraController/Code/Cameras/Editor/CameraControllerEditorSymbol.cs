using System.Linq;
using UnityEditor;

namespace com.ootii.Cameras
{
	[InitializeOnLoad]
    public class CameraControllerEditorSymbol : UnityEditor.Editor
	{
	    /// <summary>
	    /// Symbol that will be added to the editor
	    /// </summary>
	    private static string _EditorSymbol = "OOTII_CC";
	
	    /// <summary>
	    /// Add a new symbol as soon as Unity gets done compiling.
	    /// </summary>
	    static CameraControllerEditorSymbol()
	    {
	        string lSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            if (!lSymbols.Split(';').Contains(_EditorSymbol))
            {
	            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, lSymbols + ";" + _EditorSymbol);
            }
        }
    }
}
