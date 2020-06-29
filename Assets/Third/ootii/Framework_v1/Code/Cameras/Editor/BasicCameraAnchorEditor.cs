using UnityEngine;
using UnityEditor;
using com.ootii.Cameras;
using com.ootii.Helpers;

[CanEditMultipleObjects]
[CustomEditor(typeof(BaseCameraAnchor))]
public class BaseCameraAnchorEditor : Editor
{
    // Helps us keep track of when the list needs to be saved. This
    // is important since some changes happen in scene.
    private bool mIsDirty;

    // The actual class we're storing
    private BaseCameraAnchor mTarget;
    private SerializedObject mTargetSO;

    /// <summary>
    /// Called when the object is selected in the editor
    /// </summary>
    private void OnEnable()
    {
        // Grab the serialized objects
        mTarget = (BaseCameraAnchor)target;
        mTargetSO = new SerializedObject(target);
    }

    /// <summary>
    /// This function is called when the scriptable object goes out of scope.
    /// </summary>
    private void OnDisable()
    {
    }

    /// <summary>
    /// Called when the inspector needs to draw
    /// </summary>
    public override void OnInspectorGUI()
    {
        // Pulls variables from runtime so we have the latest values.
        mTargetSO.Update();

        GUILayout.Space(5);

        EditorHelper.DrawInspectorTitle("ootii Basic Camera Anchor");

        EditorHelper.DrawInspectorDescription("This anchor can be used to control how we follow a target. The Camera Controller would then use this object as the 'anchor'.", MessageType.None);

        GUILayout.Space(5);

        if (EditorHelper.BoolField("Is Following Enabled", "Determines if we're actively following the target.", mTarget.IsFollowingEnabled, mTarget))
        {
            mIsDirty = true;
            mTarget.IsFollowingEnabled = EditorHelper.FieldBoolValue;
        }

        if (EditorHelper.ObjectField<Transform>("Target", "Transform the camera is meant to follow.", mTarget.Root, mTarget))
        {
            mIsDirty = true;
            mTarget.Root = EditorHelper.FieldObjectValue as Transform;
        }

        if (EditorHelper.Vector3Field("Target Offset", "Offset from the target's root that the anchor will follow", mTarget.RootOffset, mTarget))
        {
            mIsDirty = true;
            mTarget.RootOffset = EditorHelper.FieldVector3Value;
        }

        GUILayout.Space(5);

        if (EditorHelper.BoolField("Rotate with Target", "Determines if we rotate with the target.", mTarget.RotateWithTarget, mTarget))
        {
            mIsDirty = true;
            mTarget.RotateWithTarget = EditorHelper.FieldBoolValue;
        }

        if (EditorHelper.ObjectField<Transform>("Rotation Target", "Alternate target that will determine the anchor's rotation.", mTarget.RotationRoot, mTarget))
        {
            mIsDirty = true;
            mTarget.RotationRoot = EditorHelper.FieldObjectValue as Transform;
        }

        GUILayout.Space(5);

        if (EditorHelper.FloatField("Movement Lerp", "Lerp applied to the position to smooth movement.", mTarget.MovementLerp, mTarget))
        {
            mIsDirty = true;
            mTarget.MovementLerp = EditorHelper.FieldFloatValue;
        }

        GUILayout.Space(5);

        EditorGUILayout.LabelField("Limits", EditorStyles.boldLabel, GUILayout.Height(16f));

        EditorGUILayout.BeginVertical(EditorHelper.GroupBox);

        EditorHelper.DrawInspectorDescription("Locks movement and rotation along specific axes.", MessageType.None);

        EditorGUILayout.BeginVertical(EditorHelper.Box);

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(new GUIContent("Freeze Position", "Prevents movement on the specified axis."), GUILayout.Width(EditorGUIUtility.labelWidth));

        EditorGUILayout.LabelField(new GUIContent("x"), GUILayout.Width(10));
        bool lNewFreezePositionX = EditorGUILayout.Toggle(mTarget.FreezePositionX, GUILayout.Width(10));
        if (lNewFreezePositionX != mTarget.FreezePositionX)
        {
            mIsDirty = true;
            mTarget.FreezePositionX = lNewFreezePositionX;
        }

        GUILayout.Space(5);
        EditorGUILayout.LabelField(new GUIContent("y"), GUILayout.Width(10));
        bool lNewFreezePositionY = EditorGUILayout.Toggle(mTarget.FreezePositionY, GUILayout.Width(10));
        if (lNewFreezePositionY != mTarget.FreezePositionY)
        {
            mIsDirty = true;
            mTarget.FreezePositionY = lNewFreezePositionY;
        }

        GUILayout.Space(5);
        EditorGUILayout.LabelField(new GUIContent("z"), GUILayout.Width(10));
        bool lNewFreezePositionZ = EditorGUILayout.Toggle(mTarget.FreezePositionZ, GUILayout.Width(10));
        if (lNewFreezePositionZ != mTarget.FreezePositionZ)
        {
            mIsDirty = true;
            mTarget.FreezePositionZ = lNewFreezePositionZ;
        }

        GUILayout.FlexibleSpace();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndVertical();

        // If there is a change... update.
        if (mIsDirty)
        {
            // Flag the object as needing to be saved
            EditorUtility.SetDirty(mTarget);

#if UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
            EditorApplication.MarkSceneDirty();
#else
            if (!EditorApplication.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }
#endif

            // Pushes the values back to the runtime so it has the changes
            mTargetSO.ApplyModifiedProperties();

            // Clear out the dirty flag
            mIsDirty = false;
        }
    }
}
