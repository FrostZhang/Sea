using System;
using UnityEngine;
using UnityEditor;
using com.ootii.Geometry;

[CanEditMultipleObjects]
[CustomEditor(typeof(BezierSpline))]
public class BezierSpineInspector : Editor
{
    /// <summary>
    /// Used to track if the scene needs to be saved
    /// </summary>
    private bool mIsDirty = false;

    /// <summary>
    /// Spine that we're editing
    /// </summary>
    private BezierSpline mSpline = null;

    /// <summary>
    /// Scriptable object that can be saved
    /// </summary>
    private SerializedObject mSplineSO = null;

    /// <summary>
    /// Track the selected point
    /// </summary>
    private int mSelectedControlPointIndex = -1;
    private int mSelectedTangentPointIndex = -1;

    /// <summary>
    /// Tracks the local/global rotation
    /// </summary>
    private Quaternion mRotation = Quaternion.identity;

    /// <summary>
    /// Called when the script object is loaded
    /// </summary>
    private void OnEnable()
    {
        mSpline = target as BezierSpline;
        mSplineSO = new SerializedObject(target);
    }

    /// <summary>
    /// Renders content to the inspector panel
    /// </summary>
    public override void OnInspectorGUI()
    {
        // Pulls variables from runtime so we have the latest values.
        mSplineSO.Update();

        // Allow the loop
        EditorGUI.BeginChangeCheck();
        bool lNewLoop = EditorGUILayout.Toggle(new GUIContent("Loop", ""), mSpline.Loop);
        if (EditorGUI.EndChangeCheck() && lNewLoop != mSpline.Loop)
        {
            mIsDirty = true;
            Undo.RecordObject(mSpline, "Spine Loop");

            mSpline.Loop = lNewLoop;
        }
        
        // Allow the segments to change
        EditorGUI.BeginChangeCheck();
        int lNewSegments = EditorGUILayout.IntField(new GUIContent("Segments", ""), mSpline.Segments);
        if (EditorGUI.EndChangeCheck() && lNewSegments != mSpline.Segments)
        {
            mIsDirty = true;
            Undo.RecordObject(mSpline, "Spine Segments");

            mSpline.Segments = lNewSegments;
        }

        // Draw all the curve points
        for (int i = 0; i < mSpline.ControlPointCount; i++)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();

            Vector3 lOldPoint = mSpline.GetControlPoint(i);
            Vector3 lNewPoint = EditorGUILayout.Vector3Field(new GUIContent("Control Point " + i.ToString(), ""), lOldPoint);
            if (EditorGUI.EndChangeCheck() && lNewPoint != lOldPoint)
            {
                mIsDirty = true;
                Undo.RecordObject(mSpline, "Spline Point Move");

                mSpline.SetControlPoint(i, lNewPoint);
            }

            GUILayout.Space(5);

            int lOldConstraint = mSpline.GetControlPointConstraint(i);
            int lNewConstraint = EditorGUILayout.IntField(new GUIContent("", ""), lOldConstraint, GUILayout.Width(20));
            if (lOldConstraint != lNewConstraint)
            {
                mIsDirty = true;
                Undo.RecordObject(mSpline, "Spline Point Constraint");

                mSpline.SetControlPointConstraint(i, lNewConstraint);
            }

            EditorGUILayout.EndHorizontal();
        }

        // Create a button for adding a new point
        if (GUILayout.Button("Add Point"))
        {
            mIsDirty = true;
            mSpline.AddControlPoint();

            mSelectedControlPointIndex = mSpline.ControlPointCount - 1;
        }

        // Create a button for adding a new point
        if (GUILayout.Button("Insert Point"))
        {
            mIsDirty = true;
            mSpline.InsertControlPoint(mSelectedControlPointIndex);
        }

        // Create a button for deleting a point
        if (GUILayout.Button("Delete Point"))
        {
            mIsDirty = true;
            mSpline.DeleteControlPoint(mSelectedControlPointIndex);

            mSelectedControlPointIndex = -1;
        }

        // If there is a change... update.
        if (mIsDirty)
        {
            // Flag the object as needing to be saved
            EditorUtility.SetDirty(mSpline);

#if UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
            EditorApplication.MarkSceneDirty();
#else
            if (!EditorApplication.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }
#endif

            // Pushes the values back to the runtime so it has the changes
            mSplineSO.ApplyModifiedProperties();

            // Clear out the dirty flag
            mIsDirty = false;
        }
    }

    /// <summary>
    /// Renders content to the scene editor
    /// </summary>
    public void OnSceneGUI()
    {
        mRotation = (Tools.pivotRotation == PivotRotation.Local ? mSpline.transform.rotation : Quaternion.identity);

        Color lHandleColor = Handles.color;

        // Render the control points
        for (int i = 0; i < mSpline.ControlPointCount; i++)
        {
            DrawControlPoint(i);
            DrawTangentPoints(i);
        }

        // Render the line tangents
        Handles.color = Color.gray;
        for (int i = 0; i < mSpline.ControlPointCount; i++)
        {
            Vector3 lWorldP0 = mSpline.transform.TransformPoint(mSpline.GetControlPoint(i));

            if (i > 0)
            {
                Vector3 lWorldP1 = mSpline.transform.TransformPoint(mSpline.GetBackwardTangentPoint(i));
                Handles.DrawLine(lWorldP0, lWorldP1);
            }

            if (i < mSpline.ControlPointCount - 1)
            {
                Vector3 lWorldP2 = mSpline.transform.TransformPoint(mSpline.GetForwardTangentPoint(i));
                Handles.DrawLine(lWorldP0, lWorldP2);
            }
        }

        // Render the curve
        Handles.color = Color.white;
        for (int i = 1; i < mSpline.ControlPointCount; i++)
        {
            Vector3 lWorldP0 = mSpline.transform.TransformPoint(mSpline.GetControlPoint(i - 1));
            Vector3 lWorldP1 = mSpline.transform.TransformPoint(mSpline.GetForwardTangentPoint(i - 1));
            Vector3 lWorldP2 = mSpline.transform.TransformPoint(mSpline.GetBackwardTangentPoint(i));
            Vector3 lWorldP3 = mSpline.transform.TransformPoint(mSpline.GetControlPoint(i));
            Handles.DrawBezier(lWorldP0, lWorldP3, lWorldP1, lWorldP2, Handles.color, null, 2f);
        }

        // Render the direction 
        Handles.color = Color.green;

        Vector3 lStart = mSpline.GetPoint(0f);
        Handles.DrawLine(lStart, lStart + mSpline.GetDirection(0f));

        int lSegments = mSpline.Segments * mSpline.CurveCount;
        for (int i = 1; i <= lSegments; i++)
        {
            float lTime = i / (float)lSegments;

            Vector3 lEnd = mSpline.GetPoint(lTime);
            Handles.DrawLine(lEnd, lEnd + mSpline.GetDirection(lTime));
        }

        // Clean up
        Handles.color = lHandleColor;
    }

    /// <summary>
    /// Draws a control point so that it can be moved in the scene
    /// </summary>
    /// <param name="rIndex"></param>
    /// <returns></returns>
    private void DrawControlPoint(int rIndex)
    {
        Handles.color = Color.white;

        Vector3 lWorldPoint = mSpline.transform.TransformPoint(mSpline.GetControlPoint(rIndex));

        float lHandleSize = HandleUtility.GetHandleSize(lWorldPoint);

        // Allow for selecting points to move
#if UNITY_4 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4 || UNITY_5_5
        if (Handles.Button(lWorldPoint, Quaternion.identity, 0.06f * lHandleSize, 0.08f * lHandleSize, Handles.DotCap))
#else
        if (Handles.Button(lWorldPoint, Quaternion.identity, 0.06f * lHandleSize, 0.08f * lHandleSize, Handles.DotHandleCap))
#endif
        {
            mSelectedTangentPointIndex = -1;
            mSelectedControlPointIndex = rIndex;
        }

        // Render the selected point handles
        if (mSelectedControlPointIndex == rIndex)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 lNewWorldPoint = Handles.DoPositionHandle(lWorldPoint, mRotation);
            if (EditorGUI.EndChangeCheck() && lNewWorldPoint != lWorldPoint)
            {
                mIsDirty = true;
                Undo.RecordObject(mSpline, "Spline Control Point Move");

                lWorldPoint = lNewWorldPoint;
                mSpline.SetControlPoint(rIndex, mSpline.transform.InverseTransformPoint(lWorldPoint));
            }
        }
    }

    /// <summary>
    /// Draws specific tangent points on the line
    /// </summary>
    /// <param name="rIndex"></param>
    private void DrawTangentPoints(int rIndex)
    {
        Handles.color = Color.cyan;

        Vector3 lWorldPoint = Vector3.zero;
        float lHandleSize = 1f;

        // Backward Tangent Point
        if (rIndex > 0)
        {
            lWorldPoint = mSpline.transform.TransformPoint(mSpline.GetBackwardTangentPoint(rIndex));
            lHandleSize = HandleUtility.GetHandleSize(lWorldPoint);

            // Allow for selecting points to move
#if UNITY_4 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4 || UNITY_5_5
            if (Handles.Button(lWorldPoint, Quaternion.identity, 0.04f * lHandleSize, 0.06f * lHandleSize, Handles.DotCap))
#else
            if (Handles.Button(lWorldPoint, Quaternion.identity, 0.04f * lHandleSize, 0.06f * lHandleSize, Handles.DotHandleCap))
#endif
            {
                mSelectedControlPointIndex = -1;
                mSelectedTangentPointIndex = (rIndex * 3) - 1;
            }

            // Render the selected point handles
            if (mSelectedTangentPointIndex == (rIndex * 3) - 1)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 lNewWorldPoint = Handles.DoPositionHandle(lWorldPoint, mRotation);
                if (EditorGUI.EndChangeCheck() && lNewWorldPoint != lWorldPoint)
                {
                    mIsDirty = true;
                    Undo.RecordObject(mSpline, "Spline Tangent Point Move");

                    lWorldPoint = lNewWorldPoint;
                    mSpline.SetBackwardTangentPoint(rIndex, mSpline.transform.InverseTransformPoint(lWorldPoint));
                }
            }
        }

        // Forward Tangent Point
        if (rIndex < mSpline.ControlPointCount - 1)
        {
            lWorldPoint = mSpline.transform.TransformPoint(mSpline.GetForwardTangentPoint(rIndex));
            lHandleSize = HandleUtility.GetHandleSize(lWorldPoint);

            // Allow for selecting points to move
#if UNITY_4 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4 || UNITY_5_5
            if (Handles.Button(lWorldPoint, Quaternion.identity, 0.04f * lHandleSize, 0.06f * lHandleSize, Handles.DotCap))
#else
            if (Handles.Button(lWorldPoint, Quaternion.identity, 0.04f * lHandleSize, 0.06f * lHandleSize, Handles.DotHandleCap))
#endif
            {
                mSelectedControlPointIndex = -1;
                mSelectedTangentPointIndex = (rIndex * 3) + 1;
            }

            // Render the selected point handles
            if (mSelectedTangentPointIndex == (rIndex * 3) + 1)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 lNewWorldPoint = Handles.DoPositionHandle(lWorldPoint, mRotation);
                if (EditorGUI.EndChangeCheck() && lNewWorldPoint != lWorldPoint)
                {
                    mIsDirty = true;
                    Undo.RecordObject(mSpline, "Spline Tangent Point Move");

                    lWorldPoint = lNewWorldPoint;
                    mSpline.SetForwardTangentPoint(rIndex, mSpline.transform.InverseTransformPoint(lWorldPoint));
                }
            }
        }
    }
}
