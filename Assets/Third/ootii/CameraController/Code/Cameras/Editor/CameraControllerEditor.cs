using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using com.ootii.Actors;
using com.ootii.Base;
using com.ootii.Helpers;
using com.ootii.Input;

namespace com.ootii.Cameras
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(com.ootii.Cameras.CameraController))]
    public class CameraControllerEditor : UnityEditor.Editor
    {
        // Helps us keep track of when the list needs to be saved. This
        // is important since some changes happen in scene.
        private bool mIsDirty;

        // The actual class we're storing
        private com.ootii.Cameras.CameraController mTarget;
        private SerializedObject mTargetSO;

        // List object for our motors
        private ReorderableList mMotorList;

        private int mMotorIndex = 0;
        private List<string> mMotorNames = new List<string>();
        private Dictionary<string, Type> mMotorTypes = new Dictionary<string, Type>();

        /// <summary>
        /// Called when the object is selected in the editor
        /// </summary>
        private void OnEnable()
        {
            // Grab the serialized objects
            mTarget = (com.ootii.Cameras.CameraController)target;
            mTargetSO = new SerializedObject(target);

            // Refresh the project layers
            EditorHelper.RefreshLayers();

            // Update the motors so they can update with the definitions.
            if (!UnityEngine.Application.isPlaying)
            {
                mTarget.InstantiateMotors();
            }

            // Create the list of motors to display
            InstantiateMotorList();

            // Dropdown values
            mMotorTypes.Clear();
            mMotorNames.Clear();

            // Generate the list of motions to display
            Assembly lAssembly = Assembly.GetAssembly(typeof(com.ootii.Cameras.CameraController));
            Type[] lMotionTypes = lAssembly.GetTypes();

            for (int i = 0; i < lMotionTypes.Length; i++)
            {
                Type lType = lMotionTypes[i];
                if (lType.IsAbstract) { continue; }
                if (typeof(CameraMotor).IsAssignableFrom(lType))
                {
                    mMotorNames.Add(GetFriendlyName(lType));
                    mMotorTypes.Add(GetFriendlyName(lType), lType);
                }
            }

            mMotorNames = mMotorNames.OrderBy(x => x).ToList<string>();

            // Setup the input
            if (!TestInputManagerSettings())
            {
                CreateInputManagerSettings();
            }
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

            EditorHelper.DrawInspectorTitle("ootii Camera Controller");

            EditorHelper.DrawInspectorDescription("Advanced camera rig that uses different motors to control the camera.", MessageType.None);

            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

            GameObject lNewInputSourceOwner = EditorHelper.InterfaceOwnerField<IInputSource>(new GUIContent("Input Source", "Input source we'll use to get key presses, mouse movement, etc. This GameObject should have a component implementing the IInputSource interface."), mTarget.InputSourceOwner, true);
            if (lNewInputSourceOwner != mTarget.InputSourceOwner)
            {
                mIsDirty = true;
                mTarget.InputSourceOwner = lNewInputSourceOwner;
            }

            EditorGUILayout.LabelField(new GUIContent("Find", "Determines if we attempt to automatically find the input source at startup if one isn't set."), GUILayout.Width(30));

            bool lNewAutoFindInputSource = EditorGUILayout.Toggle(mTarget.AutoFindInputSource, GUILayout.Width(16));
            if (lNewAutoFindInputSource != mTarget.AutoFindInputSource)
            {
                mIsDirty = true;
                mTarget.AutoFindInputSource = lNewAutoFindInputSource;
            }

            EditorGUILayout.EndHorizontal();

            if (EditorHelper.BoolField("Force Update", "Determines if we'll use our internal update or rely on something like a character controller to call update.", mTarget.IsInternalUpdateEnabled, mTarget))
            {
                mIsDirty = true;
                mTarget.IsInternalUpdateEnabled = EditorHelper.FieldBoolValue;
            }

            if (mTarget.IsInternalUpdateEnabled)
            {
                if (EditorHelper.BoolField("Use Fixed Update", "Determines if we'll use the FixedUpdate cycle to update the camera instead of the typical LateUpdate cycle. This is useful for Rigidbody based character controllers.", mTarget.UseFixedUpdate, mTarget))
                {
                    mIsDirty = true;
                    mTarget.UseFixedUpdate = EditorHelper.FieldBoolValue;
                }
            }

            EditorHelper.DrawLine();

            if (EditorHelper.ObjectField<Transform>("Anchor", "Transform the camera is meant to follow.", mTarget.Anchor, mTarget))
            {
                mIsDirty = true;
                mTarget.Anchor = EditorHelper.FieldObjectValue as Transform;
            }

            if (EditorHelper.Vector3Field("Anchor Offset", "Offset from the transform that represents the true anchor.", mTarget.AnchorOffset, mTarget))
            {
                mIsDirty = true;
                mTarget.AnchorOffset = EditorHelper.FieldVector3Value;
            }

            if (EditorHelper.BoolField("Rotate Anchor Offset", "Determines if the offset rotates as the anchor rotates. Disabling this creates an 'always up' camera.", mTarget.RotateAnchorOffset, mTarget))
            {
                mIsDirty = true;
                mTarget.RotateAnchorOffset = EditorHelper.FieldBoolValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.BoolField("Invert Pitch", "Determines if we invert the pitch on the 1st person and 3rd person motors.", mTarget.InvertPitch, mTarget))
            {
                mIsDirty = true;
                mTarget.InvertPitch = EditorHelper.FieldBoolValue;
            }

            EditorHelper.DrawLine();

            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();

            GUILayout.Label("", EditorHelper.BasicIcon, GUILayout.Width(16), GUILayout.Height(16));

            if (GUILayout.Button("Basic", EditorStyles.miniButton, GUILayout.Width(70)))
            {
                mTarget.EditorTabIndex = 0;
                mIsDirty = true;
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(20);

            EditorGUILayout.BeginHorizontal();

            GUILayout.Label("", EditorHelper.AdvancedIcon, GUILayout.Width(16), GUILayout.Height(16));

            if (GUILayout.Button("Advanced", EditorStyles.miniButton, GUILayout.Width(70)))
            {
                mTarget.EditorTabIndex = 1;
                mIsDirty = true;
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("", EditorHelper.DebugIcon, GUILayout.Width(16), GUILayout.Height(16)))
            {
                mTarget.EditorTabIndex = 2;
                mIsDirty = true;
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            if (mTarget.EditorTabIndex == 0)
            {
                bool lIsDirty = OnBasicInspector();
                if (lIsDirty) { mIsDirty = true; }
            }
            else if (mTarget.EditorTabIndex == 1)
            {
                bool lIsDirty = OnAdvancedInspector();
                if (lIsDirty) { mIsDirty = true; }
            }
            else if (mTarget.EditorTabIndex == 2)
            {
                bool lIsDirty = OnDebugInspector();
                if (lIsDirty) { mIsDirty = true; }
            }

            GUILayout.Space(10);

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

                // Update all the definitions
                for (int i = 0; i < mTarget.Motors.Count; i++)
                {
                    if (i >= mTarget.MotorDefinitions.Count) { mTarget.MotorDefinitions.Add(""); }
                    mTarget.MotorDefinitions[i] = mTarget.Motors[i].SerializeMotor();
                }

                // Pushes the values back to the runtime so it has the changes
                mTargetSO.ApplyModifiedProperties();

                // Clear out the dirty flag
                mIsDirty = false;
            }

            // Force the inspector to refresh
            if (mTarget.EditorRefresh)
            {
                Repaint();
                mTarget.EditorRefresh = false;
            }
        }

        /// <summary>
        /// Properties to display on the basic tab
        /// </summary>
        /// <returns></returns>
        private bool OnBasicInspector()
        {
            bool lIsDirty = false;

            EditorGUILayout.BeginVertical(EditorHelper.Box);

            EditorHelper.DrawSmallTitle("Camera Styles");

            EditorHelper.DrawInspectorDescription("Click to setup the motor, input, and settings to mimic the secified style.", MessageType.None);

            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(5);

            if (GUILayout.Button(new GUIContent("", "Setup 1st Person Style."), FirstPersonIcon, GUILayout.Width(32f), GUILayout.Height(32f)))
            {
                lIsDirty = true;
                EnableFirstPersonStyle();
            }

            EditorGUILayout.LabelField("1st Person Style", EditorHelper.OptionLabel, GUILayout.MinWidth(50), GUILayout.ExpandWidth(true));

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(5);

            if (GUILayout.Button(new GUIContent("", "Setup 3rd Person Style."), ThirdPersonIcon, GUILayout.Width(32f), GUILayout.Height(32f)))
            {
                lIsDirty = true;
                EnableThirdPersonStyle();
            }

            EditorGUILayout.LabelField("3rd Person Style", EditorHelper.OptionLabel, GUILayout.MinWidth(50), GUILayout.ExpandWidth(true));

            EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(5);

            if (GUILayout.Button(new GUIContent("", "Setup MOBA Style."), MOBAIcon, GUILayout.Width(32f), GUILayout.Height(32f)))
            {
                lIsDirty = true;
                EnableMOBAStyle();
            }

            EditorGUILayout.LabelField("MOBA Style", EditorHelper.OptionLabel, GUILayout.MinWidth(50), GUILayout.ExpandWidth(true));

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            EditorGUILayout.EndVertical();

            GUILayout.Space(5);

            EditorGUILayout.BeginVertical(EditorHelper.Box);

            EditorHelper.DrawSmallTitle("Options");

            EditorHelper.DrawInspectorDescription("Select the desired options for your style.", MessageType.None);
            GUILayout.Space(5);

            if (mTarget.EditorCameraStyle == 0)
            {
                ViewMotor lViewMotor = mTarget.GetMotor<ViewMotor>("1st Person View");
                if (lViewMotor != null)
                {
                    // Speeds
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(5);

                    if (EditorHelper.FloatField("Yaw Speed", "Speed to rotate around the camera's y-axis.", lViewMotor.YawSpeed, mTarget))
                    {
                        lIsDirty = true;
                        lViewMotor.YawSpeed = EditorHelper.FieldFloatValue;
                    }

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(5);

                    if (EditorHelper.FloatField("Pitch Speed", "Speed to rotate around the camera's x-axis.", lViewMotor.PitchSpeed, mTarget))
                    {
                        lIsDirty = true;
                        lViewMotor.PitchSpeed = EditorHelper.FieldFloatValue;
                    }

                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(5);
                }
            }
            else if (mTarget.EditorCameraStyle == 1)
            {
                YawPitchMotor lYawPitchMotor = mTarget.GetMotor("3rd Person Follow") as YawPitchMotor;
                if (!mTarget.EditorUseAdventureStyle) { lYawPitchMotor = mTarget.GetMotor("3rd Person Fixed") as YawPitchMotor; }

                if (lYawPitchMotor != null)
                {
                    // Colliding
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(5);

                    if (EditorHelper.BoolField(mTarget.IsCollisionsEnabled, "Is Collisions Enabled", mTarget, 16f))
                    {
                        lIsDirty = true;
                        mTarget.IsCollisionsEnabled = EditorHelper.FieldBoolValue;
                    }

                    EditorGUILayout.LabelField("Test Collisions", GUILayout.MinWidth(50), GUILayout.ExpandWidth(true));

                    if (mTarget.IsCollisionsEnabled)
                    {
                        int lNewCollisionLayers = EditorHelper.LayerMaskField(new GUIContent("", "Layers that identifies objects that will block the camera's movement."), mTarget.CollisionLayers, GUILayout.Width(100));
                        if (lNewCollisionLayers != mTarget.CollisionLayers)
                        {
                            lIsDirty = true;
                            mTarget.CollisionLayers = lNewCollisionLayers;
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(5);

                    // Following
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(5);

                    if (EditorHelper.BoolField(mTarget.EditorUseAdventureStyle, "Use 'follow' vs 'fixed' style", mTarget, 16f))
                    {
                        lIsDirty = true;
                        mTarget.EditorUseAdventureStyle = EditorHelper.FieldBoolValue;

                        if (mTarget.EditorUseAdventureStyle)
                        {
                            if (mTarget.GetMotorIndex<OrbitFollowMotor>() >= 0)
                            {
                                mTarget._ActiveMotorIndex = mTarget.GetMotorIndex<OrbitFollowMotor>("3rd Person Follow");
                            }
                        }
                        else
                        {
                            if (mTarget.GetMotorIndex<OrbitFixedMotor>() >= 0)
                            {
                                mTarget._ActiveMotorIndex = mTarget.GetMotorIndex<OrbitFixedMotor>("3rd Person Fixed");
                            }
                        }
                    }

                    EditorGUILayout.LabelField("Use 'adventure' style", GUILayout.MinWidth(50), GUILayout.ExpandWidth(true));

                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(5);


                    // Targeting
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(5);

                    bool lUseTargeting = (mTarget.GetMotor("Targeting In") != null && mTarget.GetMotor("Targeting In").IsEnabled);
                    if (EditorHelper.BoolField(lUseTargeting, "Use targeting", mTarget, 16f))
                    {
                        lIsDirty = true;

                        if (EditorHelper.FieldBoolValue)
                        {
                            if (mTarget.GetMotor("Targeting In") != null) { mTarget.GetMotor("Targeting In").IsEnabled = true; }
                            if (mTarget.GetMotor("Targeting Out") != null) { mTarget.GetMotor("Targeting Out").IsEnabled = true; }
                        }
                        else
                        {
                            if (mTarget.GetMotor("Targeting In") != null) { mTarget.GetMotor("Targeting In").IsEnabled = false; }
                            if (mTarget.GetMotor("Targeting Out") != null) { mTarget.GetMotor("Targeting Out").IsEnabled = false; }
                        }
                    }

                    EditorGUILayout.LabelField("Use targeting", GUILayout.MinWidth(50), GUILayout.ExpandWidth(true));

                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(5);

                    // Speeds
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(5);

                    if (EditorHelper.FloatField("Yaw Speed", "Speed to rotate around the camera's y-axis.", lYawPitchMotor.YawSpeed, mTarget))
                    {
                        lIsDirty = true;
                        lYawPitchMotor.YawSpeed = EditorHelper.FieldFloatValue;
                    }

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(5);

                    if (EditorHelper.FloatField("Pitch Speed", "Speed to rotate around the camera's x-axis.", lYawPitchMotor.PitchSpeed, mTarget))
                    {
                        lIsDirty = true;
                        lYawPitchMotor.PitchSpeed = EditorHelper.FieldFloatValue;
                    }

                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(5);
                }
            }
            else if (mTarget.EditorCameraStyle == 2)
            {
                WorldMotor lWorldMotor = mTarget.GetMotor<WorldMotor>("Top Down");
                if (lWorldMotor != null)
                {
                    // Follow
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(5);

                    if (EditorHelper.BoolField(lWorldMotor.FollowAnchor, "Follow Anchor", mTarget, 16f))
                    {
                        lIsDirty = true;
                        lWorldMotor.FollowAnchor = EditorHelper.FieldBoolValue;
                    }

                    EditorGUILayout.LabelField("Follow anchor", GUILayout.MinWidth(50), GUILayout.ExpandWidth(true));

                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(5);

                    // Grip
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(5);

                    if (EditorHelper.BoolField(lWorldMotor.GripPan, "Grip Pan", mTarget, 16f))
                    {
                        lIsDirty = true;
                        lWorldMotor.GripPan = EditorHelper.FieldBoolValue;
                    }

                    EditorGUILayout.LabelField("Use grip panning", GUILayout.MinWidth(50), GUILayout.ExpandWidth(true));

                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(5);

                    // Grip
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(5);

                    if (EditorHelper.BoolField(lWorldMotor.EdgePan, "Edge Pan", mTarget, 16f))
                    {
                        lIsDirty = true;
                        lWorldMotor.EdgePan = EditorHelper.FieldBoolValue;
                    }

                    EditorGUILayout.LabelField("Use edge panning", GUILayout.MinWidth(50), GUILayout.ExpandWidth(true));

                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(5);

                    // Grip
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(5);

                    if (EditorHelper.BoolField(lWorldMotor.InputPan, "Input Pan", mTarget, 16f))
                    {
                        lIsDirty = true;
                        lWorldMotor.InputPan = EditorHelper.FieldBoolValue;
                    }

                    EditorGUILayout.LabelField("Use input panning", GUILayout.MinWidth(50), GUILayout.ExpandWidth(true));

                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(5);
                }
            }

            EditorGUILayout.EndVertical();

            return lIsDirty;
        }

        /// <summary>
        /// Displays the properties for the advanced tab
        /// </summary>
        /// <returns></returns>
        private bool OnAdvancedInspector()
        {
            bool lIsDirty = false;

            // Show the Layers
            GUILayout.BeginVertical(EditorHelper.GroupBox);
            mMotorList.DoLayoutList();

            if (mMotorList.index >= 0)
            {
                GUILayout.Space(10f);
                GUILayout.BeginVertical(EditorHelper.Box);

                bool lListIsDirty = DrawMotorDetailItem(mTarget.Motors[mMotorList.index]);
                if (lListIsDirty) { lIsDirty = true; }

                GUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(1);

            EditorGUILayout.BeginVertical(EditorHelper.Box);
            EditorHelper.DrawSmallTitle("Collision Properties");

            if (EditorHelper.BoolField("Is Colliding Enabled", "Determines if we allow collisions.", mTarget.IsCollisionsEnabled, mTarget))
            {
                mIsDirty = true;
                mTarget.IsCollisionsEnabled = EditorHelper.FieldBoolValue;
            }

            if (mTarget.IsCollisionsEnabled)
            {
                int lNewCollisionLayers = EditorHelper.LayerMaskField(new GUIContent("Collision Layers", "Layers the camera can collide with."), mTarget.CollisionLayers);
                if (lNewCollisionLayers != mTarget._CollisionLayers)
                {
                    mIsDirty = true;
                    mTarget.CollisionLayers = lNewCollisionLayers;
                }

                if (EditorHelper.FloatField("Radius", "Radius of the collision sphere around the camera.", mTarget.CollisionRadius, mTarget))
                {
                    lIsDirty = true;
                    mTarget.CollisionRadius = EditorHelper.FieldFloatValue;
                }

                if (EditorHelper.FloatField("Min Distance", "When a collision occurs, the minimum distance the camera can be to the anchor.", mTarget.MinCollisionDistance, mTarget))
                {
                    lIsDirty = true;
                    mTarget.MinCollisionDistance = EditorHelper.FieldFloatValue;
                }

                if (EditorHelper.FloatField("Recovery Speed", "Units per second the camera recovers from a collision at.", mTarget.CollisionRecoverySpeed, mTarget))
                {
                    lIsDirty = true;
                    mTarget.CollisionRecoverySpeed = EditorHelper.FieldFloatValue;
                }
            }

            GUILayout.EndVertical();

            GUILayout.Space(5);

            EditorGUILayout.BeginVertical(EditorHelper.Box);
            EditorHelper.DrawSmallTitle("Zoom Properties");

            if (EditorHelper.BoolField("Is Zooming Enabled", "Determines if we allow changing the field-of-view zooming.", mTarget.IsZoomEnabled, mTarget))
            {
                mIsDirty = true;
                mTarget.IsZoomEnabled = EditorHelper.FieldBoolValue;
            }

            if (mTarget.IsZoomEnabled)
            {
                if (EditorHelper.TextField("Zoom Action Alias", "Input alias we'll use to determine if the zoom value is changing. Typically this is the mouse wheel, but it can be a button.", mTarget.ZoomActionAlias, mTarget))
                {
                    mIsDirty = true;
                    mTarget.ZoomActionAlias = EditorHelper.FieldStringValue;
                }

                if (EditorHelper.BoolField("Reset on Release", "When the zoom action alias is a button, releasing that button will reset the zoom.", mTarget.ZoomResetOnRelease, mTarget))
                {
                    mIsDirty = true;
                    mTarget.ZoomResetOnRelease = EditorHelper.FieldBoolValue;
                }

                GUILayout.Space(5f);

                EditorGUILayout.BeginHorizontal();

                float lLabelWidth = Mathf.Min(EditorGUIUtility.currentViewWidth - 218f - 23f, EditorGUIUtility.labelWidth);
                EditorGUILayout.LabelField(new GUIContent("Range", "Rotation around the actor's up vector."), GUILayout.Width(lLabelWidth));

                EditorGUILayout.LabelField(new GUIContent("Min", "Minimum zoom value."), GUILayout.Width(45f));
                if (EditorHelper.FloatField(mTarget.ZoomMin, "Min Zoom", mTarget, 40f))
                {
                    lIsDirty = true;
                    mTarget.ZoomMin = EditorHelper.FieldFloatValue;
                }

                GUILayout.Space(5f);

                EditorGUILayout.LabelField(new GUIContent("Max", "Minimum yaw angle -180 to 180."), GUILayout.Width(45f));
                if (EditorHelper.FloatField(mTarget.ZoomMax, "Max Zoom", mTarget, 40f))
                {
                    lIsDirty = true;
                    mTarget.ZoomMax = EditorHelper.FieldFloatValue;
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(new GUIContent("Speed", "Rotation around the actor's up vector."), GUILayout.Width(lLabelWidth));

                EditorGUILayout.LabelField(new GUIContent("Zoom", "Minimum zoom value."), GUILayout.Width(45f));
                if (EditorHelper.FloatField(mTarget.ZoomSpeed, "Zoom Speed", mTarget, 40f))
                {
                    lIsDirty = true;
                    mTarget.ZoomSpeed = EditorHelper.FieldFloatValue;
                }

                GUILayout.Space(5f);

                EditorGUILayout.LabelField(new GUIContent("Smooth", "Minimum yaw angle -180 to 180."), GUILayout.Width(45f));
                if (EditorHelper.FloatField(mTarget.ZoomSmoothing, "Zoom Smooth", mTarget, 40f))
                {
                    lIsDirty = true;
                    mTarget.ZoomSmoothing = EditorHelper.FieldFloatValue;
                }

                EditorGUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();

            GUILayout.Space(5);

            EditorGUILayout.BeginVertical(EditorHelper.Box);
            EditorHelper.DrawSmallTitle("Fade Properties");

            if (mTarget.IsFadeEnabed)
            {
                EditorGUILayout.HelpBox("True fading requires character shaders to use transparency." + (mTarget.DisableRenderers ? "" : "Disabling renderers works with opaque or transparent shaders."), MessageType.Warning);
            }

            bool lNewIsFadeEnabled = EditorGUILayout.Toggle(new GUIContent("Is Fade Enabled", "Determines if we fade the anchor when the camera is too close. This requires the anchor shaders to use transparencies."), mTarget.IsFadeEnabed);
            if (lNewIsFadeEnabled != mTarget.IsFadeEnabed)
            {
                mIsDirty = true;
                mTarget.IsFadeEnabed = lNewIsFadeEnabled;
            }

            if (mTarget.IsFadeEnabed)
            {
                float lNewFadeDistance = EditorGUILayout.FloatField(new GUIContent("Fade Distance", "Distance between the anchor and camera where we start fading out."), mTarget.FadeDistance);
                if (lNewFadeDistance != mTarget.FadeDistance)
                {
                    mIsDirty = true;
                    mTarget.FadeDistance = lNewFadeDistance;
                }

                float lNewFadeSpeed = EditorGUILayout.FloatField(new GUIContent("Fade Speed", "Time (in seconds) to fade the anchor in and out."), mTarget.FadeSpeed);
                if (lNewFadeSpeed != mTarget.FadeSpeed)
                {
                    mIsDirty = true;
                    mTarget.FadeSpeed = lNewFadeSpeed;
                }

                bool lNewDisableRenderers = EditorGUILayout.Toggle(new GUIContent("Disable Renderers", "Determines if the anchor's renderers are disabled when fading is complete. This works for non-transparent shaders too."), mTarget.DisableRenderers);
                if (lNewDisableRenderers != mTarget.DisableRenderers)
                {
                    mIsDirty = true;
                    mTarget.DisableRenderers = lNewDisableRenderers;
                }
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(5);

            EditorGUILayout.BeginVertical(EditorHelper.Box);
            EditorHelper.DrawSmallTitle("Shake Properties");

            // Determine how strength is applied
            EditorGUI.BeginChangeCheck();
            AnimationCurve lNewShakeStrength = EditorGUILayout.CurveField(new GUIContent("Shake Strength", "Determines how strong the shake is over the duration. (0 = none, 1 = 100%)"), mTarget.ShakeStrength);
            if (EditorGUI.EndChangeCheck())
            {
                if (mTarget != null) { Undo.RecordObject(mTarget, "Set Shake Strength"); }

                mIsDirty = true;
                mTarget.ShakeStrength = lNewShakeStrength;
            }

            GUILayout.Space(3);
            EditorGUILayout.EndVertical();

            GUILayout.Space(5f);

            // Show the events
            GUILayout.BeginHorizontal();

            if (GUILayout.Button(new GUIContent("Events"), EditorStyles.boldLabel))
            {
                mTarget.EditorShowEvents = !mTarget.EditorShowEvents;
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent(mTarget.EditorShowEvents ? "-" : "+"), EditorStyles.boldLabel))
            {
                mTarget.EditorShowEvents = !mTarget.EditorShowEvents;
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(EditorHelper.GroupBox);
            EditorHelper.DrawInspectorDescription("Assign functions to be called when specific events take place.", MessageType.None);

            if (mTarget.EditorShowEvents)
            {
                GUILayout.BeginVertical(EditorHelper.Box);

                SerializedProperty lActivatedEvent = mTargetSO.FindProperty("MotorTestActivateEvent");
                if (lActivatedEvent != null)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(lActivatedEvent);
                    if (EditorGUI.EndChangeCheck())
                    {
                        mIsDirty = true;
                    }
                }

                lActivatedEvent = mTargetSO.FindProperty("MotorActivatedEvent");
                if (lActivatedEvent != null)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(lActivatedEvent);
                    if (EditorGUI.EndChangeCheck())
                    {
                        mIsDirty = true;
                    }
                }

                SerializedProperty lDeactivatedEvent = mTargetSO.FindProperty("MotorDeactivatedEvent");
                if (lDeactivatedEvent != null)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(lDeactivatedEvent);
                    if (EditorGUI.EndChangeCheck())
                    {
                        mIsDirty = true;
                    }
                }

                SerializedProperty lCustomEvent = mTargetSO.FindProperty("ActionTriggeredEvent");
                if (lCustomEvent != null)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(lCustomEvent);
                    if (EditorGUI.EndChangeCheck())
                    {
                        mIsDirty = true;
                    }
                }

                GUILayout.EndVertical();
            }

            GUILayout.EndVertical();

            GUILayout.Space(5);

            return lIsDirty;
        }

        /// <summary>
        /// Displays the properties for the debug tab
        /// </summary>
        /// <returns></returns>
        private bool OnDebugInspector()
        {
            bool lIsDirty = false;

            if (EditorHelper.BoolField("Show Debug Info", "Determines if we render extra debug information about the camera.", mTarget.EditorShowDebug, mTarget))
            {
                lIsDirty = true;
                mTarget.EditorShowDebug = EditorHelper.FieldBoolValue;
            }

            EditorGUILayout.LabelField("Active Index", mTarget.ActiveMotorIndex.ToString());

            GUILayout.Space(5f);

            if (mTarget.ActiveMotorIndex >= 0 && mTarget.ActiveMotorIndex < mTarget.Motors.Count)
            {
                bool lIsMotorDirty = mTarget.Motors[mTarget.ActiveMotorIndex].OnDebugInspectorGUI();
                if (lIsMotorDirty) { lIsDirty = true; }
            }

            return lIsDirty;
        }

        /// <summary>
        /// Create the reorderable list
        /// </summary>
        private void InstantiateMotorList()
        {
            mMotorList = new ReorderableList(mTarget.Motors, typeof(CameraMotor), true, true, true, true);
            mMotorList.drawHeaderCallback = DrawMotorListHeader;
            mMotorList.drawFooterCallback = DrawMotorListFooter;
            mMotorList.drawElementCallback = DrawMotorListItem;
            mMotorList.onAddCallback = OnMotorListItemAdd;
            mMotorList.onRemoveCallback = OnMotorListItemRemove;
            mMotorList.onSelectCallback = OnMotorListItemSelect;
            mMotorList.onReorderCallback = OnMotorListReorder;
            mMotorList.footerHeight = 17f;

            if (mTarget.EditorMotorIndex >= 0 && mTarget.EditorMotorIndex < mMotorList.count)
            {
                mMotorList.index = mTarget.EditorMotorIndex;
            }
        }

        /// <summary>
        /// Header for the list
        /// </summary>
        /// <param name="rRect"></param>
        private void DrawMotorListHeader(Rect rRect)
        {
            EditorGUI.LabelField(rRect, "Camera Motors");
            if (GUI.Button(rRect, "", EditorStyles.label))
            {
                mMotorList.index = -1;
                OnMotorListItemSelect(mMotorList);
            }
        }

        /// <summary>
        /// Allows us to draw each item in the list
        /// </summary>
        /// <param name="rRect"></param>
        /// <param name="rIndex"></param>
        /// <param name="rIsActive"></param>
        /// <param name="rIsFocused"></param>
        private void DrawMotorListItem(Rect rRect, int rIndex, bool rIsActive, bool rIsFocused)
        {
            if (rIndex < mTarget.Motors.Count)
            {
                bool lIsDirty = false;
                CameraMotor lMotor = mTarget.Motors[rIndex];

                rRect.y += 2;

                float lHSpace = 5f;
                float lFlexVSpace = rRect.width - lHSpace - lHSpace - 16f - lHSpace - 16f;

                EditorGUILayout.BeginHorizontal();

                string lIndex = rIndex.ToString();

                Rect lTypeRect = new Rect(rRect.x, rRect.y, lFlexVSpace / 2f, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(lTypeRect, (lMotor != null ? "[" + lIndex + "] " + GetFriendlyName(lMotor.GetType()) : "null"));


                Rect lNameRect = new Rect(lTypeRect.x + lTypeRect.width + lHSpace, lTypeRect.y, lFlexVSpace / 2f, EditorGUIUtility.singleLineHeight);
                string lMotionName = EditorGUI.TextField(lNameRect, lMotor.Name);
                if (lMotionName != lMotor.Name)
                {
                    lIsDirty = true;
                    lMotor.Name = lMotionName;
                }

                Rect lIsEnabledRect = new Rect(lNameRect.x + lNameRect.width + lHSpace, lNameRect.y, 16f, 16f);
                bool lNewIsEnabled = EditorGUI.Toggle(lIsEnabledRect, lMotor.IsEnabled);
                if (lNewIsEnabled != lMotor.IsEnabled)
                {
                    lIsDirty = true;
                    lMotor.IsEnabled = lNewIsEnabled;
                }

                Rect lIsActiveRect = new Rect(lIsEnabledRect.x + lIsEnabledRect.width + lHSpace, lIsEnabledRect.y, 16f, 16f);

                bool lIsActive = (mTarget._ActiveMotorIndex == rIndex);
                bool lNewIsActive = EditorGUI.Toggle(lIsActiveRect, lIsActive);
                if (lNewIsActive != lIsActive)
                {
                    lIsDirty = true;

                    if (Application.isPlaying)
                    {
                        mTarget.ActivateMotor(rIndex);
                    }
                    else
                    {
                        mTarget._ActiveMotorIndex = (lNewIsActive ? rIndex : -1);
                    }
                }

                EditorGUILayout.EndHorizontal();

                // Update the motion if there's a change
                if (lIsDirty)
                {
                    mIsDirty = true;
                    mTarget.MotorDefinitions[rIndex] = lMotor.SerializeMotor();
                }
            }
        }

        /// <summary>
        /// Footer for the list
        /// </summary>
        /// <param name="rRect"></param>
        private void DrawMotorListFooter(Rect rRect)
        {
            Rect lMotorRect = new Rect(rRect.x, rRect.y + 1, rRect.width - 4 - 28 - 28, 16);
            mMotorIndex = EditorGUI.Popup(lMotorRect, mMotorIndex, mMotorNames.ToArray());

            Rect lAddRect = new Rect(lMotorRect.x + lMotorRect.width + 4, lMotorRect.y, 28, 15);
            if (GUI.Button(lAddRect, new GUIContent("+", "Add Motor."), EditorStyles.miniButtonLeft)) { OnMotorListItemAdd(mMotorList); }

            Rect lDeleteRect = new Rect(lAddRect.x + lAddRect.width, lAddRect.y, 28, 15);
            if (GUI.Button(lDeleteRect, new GUIContent("-", "Delete Motor."), EditorStyles.miniButtonRight)) { OnMotorListItemRemove(mMotorList); };
        }

        /// <summary>
        /// Allows us to add to a list
        /// </summary>
        /// <param name="rList"></param>
        private void OnMotorListItemAdd(ReorderableList rList)
        {
            CameraMotor lMotor = Activator.CreateInstance(mMotorTypes[mMotorNames[mMotorIndex]]) as CameraMotor;
            lMotor.RigController = mTarget;

            mTarget.Motors.Add(lMotor);
            mTarget.MotorDefinitions.Add(lMotor.SerializeMotor());

            mMotorList.index = mTarget.Motors.Count - 1;
            OnMotorListItemSelect(rList);

            mIsDirty = true;
        }

        /// <summary>
        /// Allows us process when a list is selected
        /// </summary>
        /// <param name="rList"></param>
        private void OnMotorListItemSelect(ReorderableList rList)
        {
            mTarget.EditorMotorIndex = rList.index;
        }

        /// <summary>
        /// Allows us to stop before removing the item
        /// </summary>
        /// <param name="rList"></param>
        private void OnMotorListItemRemove(ReorderableList rList)
        {
            if (EditorUtility.DisplayDialog("Warning!", "Are you sure you want to delete the item?", "Yes", "No"))
            {
                int rIndex = rList.index;

                rList.index--;

                mTarget.Motors[rIndex].Clear();

                mTarget.Motors.RemoveAt(rIndex);
                mTarget.MotorDefinitions.RemoveAt(rIndex);

                OnMotorListItemSelect(rList);

                mIsDirty = true;
            }
        }

        /// <summary>
        /// Allows us to process after the motions are reordered
        /// </summary>
        /// <param name="rList"></param>
        private void OnMotorListReorder(ReorderableList rList)
        {
            mTarget.MotorDefinitions.Clear();

            for (int i = 0; i < mTarget.Motors.Count; i++)
            {
                mTarget.MotorDefinitions.Add(mTarget.Motors[i].SerializeMotor());
            }

            mIsDirty = true;
        }

        /// <summary>
        /// Renders the currently selected step
        /// </summary>
        /// <param name="rStep"></param>
        private bool DrawMotorDetailItem(CameraMotor rMotor)
        {
            bool lIsDirty = false;

            float lLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 60f;

            Type lMotorType = rMotor.GetType();
            string lMotorTypeName = GetFriendlyName(lMotorType);

            EditorHelper.DrawSmallTitle(rMotor.Name.Length > 0 ? rMotor.Name : lMotorTypeName);

            string lDescription = GetDescription(rMotor.GetType());
            if (lDescription.Length > 0)
            {
                EditorHelper.DrawInspectorDescription(lDescription, MessageType.None);
            }

            if (EditorHelper.TextField("Name", "Name of the motor", rMotor.Name, mTarget))
            {
                lIsDirty = true;
                rMotor.Name = EditorHelper.FieldStringValue;
            }

            if (EditorHelper.BoolField("Enabled", "Determines if the motor is enabled and can activate.", rMotor.IsEnabled, mTarget))
            {
                lIsDirty = true;
                rMotor.IsEnabled = EditorHelper.FieldBoolValue;
            }

            EditorGUIUtility.labelWidth = lLabelWidth;

            EditorHelper.DrawLine();

            bool lIsMotorDirty = rMotor.OnInspectorGUI();
            if (lIsMotorDirty) { lIsDirty = true; }

            if (lIsDirty)
            {
                mTarget.MotorDefinitions[mMotorList.index] = rMotor.SerializeMotor();
            }

            return lIsDirty;
        }

        /// <summary>
        /// Sets up the first person style camera
        /// </summary>
        private void EnableFirstPersonStyle()
        {
            SetupScene();
            DisableAllMotors();

            mTarget.EditorCameraStyle = 0;
            mTarget.IsCollisionsEnabled = false;

            ViewMotor lMotor = mTarget.GetMotor<ViewMotor>("1st Person View");
            if (lMotor == null)
            {
                lMotor = new ViewMotor();
                mTarget.Motors.Add(lMotor);
                mTarget.MotorDefinitions.Add("");

                lMotor.Name = "1st Person View";
                lMotor.RigController = mTarget;
                lMotor.RotateAnchor = true;
                lMotor.RotateAnchorAlias = "";
            }

            lMotor.IsEnabled = true;
            mTarget.EditorMotorIndex = mTarget.Motors.IndexOf(lMotor);
            mTarget.MotorDefinitions[mTarget.EditorMotorIndex] = lMotor.SerializeMotor();

            mTarget._ActiveMotorIndex = mTarget.EditorMotorIndex;

            EditorUtility.DisplayDialog("Camera Controller", "The new style was setup", "Close");
        }

        /// <summary>
        /// Sets up the third person style camera
        /// </summary>
        private void EnableThirdPersonStyle()
        {
            SetupScene();
            DisableAllMotors();

            mTarget.EditorCameraStyle = 1;
            mTarget.IsCollisionsEnabled = true;

            OrbitFollowMotor lMotor = mTarget.GetMotor<OrbitFollowMotor>("3rd Person Follow");
            if (lMotor == null)
            {
                lMotor = new OrbitFollowMotor();
                mTarget.Motors.Add(lMotor);
                mTarget.MotorDefinitions.Add("");

                lMotor.Name = "3rd Person Follow";
                lMotor.RigController = mTarget;
                lMotor.MaxDistance = 3f;
            }

            lMotor.IsEnabled = true;
            mTarget.EditorMotorIndex = mTarget.Motors.IndexOf(lMotor);
            mTarget.MotorDefinitions[mTarget.EditorMotorIndex] = lMotor.SerializeMotor();

            mTarget._ActiveMotorIndex = mTarget.EditorMotorIndex;

            OrbitFixedMotor lMotor2 = mTarget.GetMotor<OrbitFixedMotor>("3rd Person Fixed");
            if (lMotor2 == null)
            {
                lMotor2 = new OrbitFixedMotor();
                mTarget.Motors.Add(lMotor2);
                mTarget.MotorDefinitions.Add("");

                lMotor2.Name = "3rd Person Fixed";
                lMotor2.RigController = mTarget;
                lMotor2.MaxDistance = 3f;
            }

            lMotor2.IsEnabled = true;
            mTarget.MotorDefinitions[mTarget.Motors.IndexOf(lMotor2)] = lMotor2.SerializeMotor();

            OrbitFixedMotor lMotor3 = mTarget.GetMotor<OrbitFixedMotor>("Targeting");
            if (lMotor3 == null)
            {
                lMotor3 = new OrbitFixedMotor();
                mTarget.Motors.Add(lMotor3);
                mTarget.MotorDefinitions.Add("");

                lMotor3.Name = "Targeting";
                lMotor3.RigController = mTarget;
                lMotor3.Offset = new Vector3(0.5f, 0f, 0f);
                lMotor3.MaxDistance = 0.5f;
                lMotor3.RotateAnchor = true;
                lMotor3.RotateAnchorAlias = "Camera Rotate Character";
            }

            lMotor3.IsEnabled = true;
            mTarget.MotorDefinitions[mTarget.Motors.IndexOf(lMotor3)] = lMotor3.SerializeMotor();

            TransitionMotor lTransition = mTarget.GetMotor<TransitionMotor>("Targeting In");
            if (lTransition == null)
            {
                lTransition = new TransitionMotor();
                mTarget.Motors.Add(lTransition);
                mTarget.MotorDefinitions.Add("");

                lTransition.Name = "Targeting In";
                lTransition.RigController = mTarget;
                lTransition.ActionAlias = "Camera Aim";
                lTransition.ActionAliasEventType = 0;
                lTransition.StartMotorIndex = 0;
                lTransition.EndMotorIndex = 2;
                lTransition.TransitionTime = 0.15f;
            }

            lTransition.IsEnabled = false;
            mTarget.MotorDefinitions[mTarget.Motors.IndexOf(lTransition)] = lTransition.SerializeMotor();

            TransitionMotor lTransition2 = mTarget.GetMotor<TransitionMotor>("Targeting Out");
            if (lTransition2 == null)
            {
                lTransition2 = new TransitionMotor();
                mTarget.Motors.Add(lTransition2);
                mTarget.MotorDefinitions.Add("");

                lTransition2.Name = "Targeting Out";
                lTransition2.RigController = mTarget;
                lTransition2.ActionAlias = "Camera Aim";
                lTransition2.ActionAliasEventType = 1;
                lTransition2.StartMotorIndex = 2;
                lTransition2.EndMotorIndex = 0;
                lTransition2.TransitionTime = 0.25f;
            }

            lTransition2.IsEnabled = false;
            mTarget.MotorDefinitions[mTarget.Motors.IndexOf(lTransition2)] = lTransition2.SerializeMotor();

            EditorUtility.DisplayDialog("Camera Controller", "The new style was setup", "Close");
        }

        /// <summary>
        /// Sets up the MOBA style camera
        /// </summary>
        private void EnableMOBAStyle()
        {
            SetupScene();
            DisableAllMotors();

            mTarget.EditorCameraStyle = 2;
            mTarget.IsCollisionsEnabled = false;
            if (mTarget.transform.rotation == Quaternion.identity) { mTarget.transform.rotation = Quaternion.Euler(25f, 0f, 0f); }

            WorldMotor lMotor = mTarget.GetMotor<WorldMotor>("Top Down");
            if (lMotor == null)
            {
                lMotor = new WorldMotor();
                mTarget.Motors.Add(lMotor);
                mTarget.MotorDefinitions.Add("");

                lMotor.Name = "Top Down";
                lMotor.RigController = mTarget;
            }

            lMotor.IsEnabled = true;
            mTarget.EditorMotorIndex = mTarget.Motors.IndexOf(lMotor);
            mTarget.MotorDefinitions[mTarget.EditorMotorIndex] = lMotor.SerializeMotor();

            mTarget._ActiveMotorIndex = mTarget.EditorMotorIndex;

            EditorUtility.DisplayDialog("Camera Controller", "The new style was setup", "Close");
        }

        /// <summary>
        /// Disable all the motors so we can reset them
        /// </summary>
        private void DisableAllMotors()
        {
            for (int i = 0; i < mTarget.Motors.Count; i++)
            {
                mTarget.Motors[i]._IsActive = false;
                mTarget.Motors[i].IsEnabled = false;

                if (i < mTarget.MotorDefinitions.Count)
                {
                    mTarget.MotorDefinitions[i] = mTarget.Motors[i].SerializeMotor();
                }
            }
        }

        /// <summary>
        /// Setup the basic scene elements
        /// </summary>
        private void SetupScene()
        {
            // Get or create the input source
            IInputSource lInputSource = CreateInputSource("com.ootii.Input.EasyInputSource, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
            if (lInputSource == null) { lInputSource = CreateInputSource<UnityInputSource>(); }
            lInputSource.IsEnabled = true;
            ReflectionHelper.SetProperty(lInputSource, "ViewActivator", 3);

            GameObject lInputSourceGO = null;
            if (lInputSource is MonoBehaviour)
            {
                lInputSourceGO = ((MonoBehaviour)lInputSource).gameObject;
            }

            mTarget.InputSourceOwner = lInputSourceGO;

            // Assign the character if we can find one
            if (mTarget.Anchor == null)
            {
                ICharacterController[] lCharacterControllers = InterfaceHelper.GetComponents<ICharacterController>();
                if (lCharacterControllers != null && lCharacterControllers.Length > 0)
                {
                    if (lCharacterControllers[0] is MonoBehaviour)
                    {
                        mTarget.Anchor = ((MonoBehaviour)lCharacterControllers[0]).gameObject.transform;
                    }
                }

                mTarget.AnchorOffset = new Vector3(0f, 1.8f, 0f);
            }

            // Check if the camera rig is setup
            GameObject lCameraGO = GameObject.FindGameObjectWithTag("MainCamera");
            if (lCameraGO == null)
            {
                Camera lCamera = Component.FindObjectOfType<Camera>();
                if (lCamera != null) { lCameraGO = lCamera.gameObject; }
            }

            if (lCameraGO == null)
            {
                EditorUtility.DisplayDialog("Warning!", "Unable to create the camera as no 'MainCamera' was found.", "Close");
                return;
            }

            // Move the camera down a layer
            if (lCameraGO.transform == mTarget.transform)
            {
                GameObject lMainCameraGO = UnityEngine.Object.Instantiate<GameObject>(mTarget.gameObject);
                lMainCameraGO.name = "Main Camera";
                lMainCameraGO.transform.parent = mTarget.transform;
                lMainCameraGO.transform.localScale = Vector3.one;
                lMainCameraGO.transform.localRotation = Quaternion.identity;
                lMainCameraGO.transform.localPosition = Vector3.zero;

                com.ootii.Cameras.CameraController lController = lMainCameraGO.GetComponent<com.ootii.Cameras.CameraController>();
                if (lController != null) { DestroyImmediate(lController); }

                Component[] lComponents = mTarget.gameObject.GetComponents(typeof(Component));
                for (int i = lComponents.Length - 1; i >= 0; i--)
                {
                    if (lComponents[i] is Transform) { continue; }
                    if (lComponents[i] is com.ootii.Cameras.CameraController) { continue; }
                    DestroyImmediate(lComponents[i]);
                }

                mTarget.gameObject.name = "Camera Rig";
            }
            // Move the camera under the rig
            else if (lCameraGO.transform.parent != mTarget.transform)
            {
                mTarget.transform.position = lCameraGO.transform.position;
                mTarget.transform.rotation = lCameraGO.transform.rotation;

                lCameraGO.transform.parent = mTarget.transform;
                lCameraGO.transform.localScale = Vector3.one;
                lCameraGO.transform.localRotation = Quaternion.identity;
                lCameraGO.transform.localPosition = Vector3.zero;
            }
        }

        /// <summary>
        /// Creates the camera rig if need and returns the GO
        /// </summary>
        /// <returns></returns>
        private IInputSource CreateInputSource<T>() where T : IInputSource
        {
            IInputSource[] lInputSources = InterfaceHelper.GetComponents<IInputSource>();
            if (lInputSources != null && lInputSources.Length > 0) { return lInputSources[0]; }

            // Create the input source
            GameObject lInputSourceGO = new GameObject("Input Source");
            T lInputSource = (T)((object)lInputSourceGO.AddComponent(typeof(T)));

            return lInputSource;
        }

        /// <summary>
        /// Creates the camera rig if need and returns the GO
        /// </summary>
        /// <returns></returns>
        private IInputSource CreateInputSource(string rType)
        {
            if (!ReflectionHelper.IsTypeValid(rType)) { return null; }

            Type lType = Type.GetType(rType);

            IInputSource[] lInputSources = InterfaceHelper.GetComponents<IInputSource>();
            if (lInputSources != null && lInputSources.Length > 0) { return lInputSources[0]; }

            // Create the input source
            GameObject lInputSourceGO = new GameObject("Input Source");
            IInputSource lInputSource = lInputSourceGO.AddComponent(lType) as IInputSource;

            return lInputSource;
        }

        /// <summary>
        /// Test if we need to setup input manager entries
        /// </summary>
        /// <returns></returns>
        private bool TestInputManagerSettings()
        {
            if (!InputManagerHelper.IsDefined("Camera Zoom")) { return false; }
            if (!InputManagerHelper.IsDefined("Camera Aim")) { return false; }
            if (!InputManagerHelper.IsDefined("Camera Follow")) { return false; }
            if (!InputManagerHelper.IsDefined("Camera Grip")) { return false; }
            if (!InputManagerHelper.IsDefined("Camera Forward")) { return false; }
            if (!InputManagerHelper.IsDefined("Camera Back")) { return false; }
            if (!InputManagerHelper.IsDefined("Camera Left")) { return false; }
            if (!InputManagerHelper.IsDefined("Camera Right")) { return false; }
            if (!InputManagerHelper.IsDefined("Camera Rotate Character")) { return false; }

            return true;
        }

        /// <summary>
        /// If the input manager entries don't exist, create them
        /// </summary>
        private void CreateInputManagerSettings()
        {
            if (!InputManagerHelper.IsDefined("Camera Zoom"))
            {
                InputManagerEntry lEntry = new InputManagerEntry();
                lEntry.Name = "Camera Zoom";
                lEntry.PositiveButton = "";
                lEntry.Gravity = 0f;
                lEntry.Dead = 0f;
                lEntry.Sensitivity = 0.1f;
                lEntry.Type = InputManagerEntryType.MOUSE_MOVEMENT;
                lEntry.Axis = 3;
                lEntry.JoyNum = 0;
                InputManagerHelper.AddEntry(lEntry);
            }

            if (!InputManagerHelper.IsDefined("Camera Aim"))
            {
                InputManagerEntry lEntry = new InputManagerEntry();
                lEntry.Name = "Camera Aim";
                lEntry.PositiveButton = "mouse 1";
                lEntry.Gravity = 100;
                lEntry.Dead = 0.3f;
                lEntry.Sensitivity = 100;
                lEntry.Type = InputManagerEntryType.KEY_MOUSE_BUTTON;
                lEntry.Axis = 0;
                lEntry.JoyNum = 0;
                InputManagerHelper.AddEntry(lEntry, true);

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX

            lEntry = new InputManagerEntry();
            lEntry.Name = "Camera Aim";
            lEntry.PositiveButton = "";
            lEntry.Gravity = 1;
            lEntry.Dead = 0.3f;
            lEntry.Sensitivity = 1;
            lEntry.Type = InputManagerEntryType.JOYSTICK_AXIS;
            lEntry.Axis = 6;
            lEntry.JoyNum = 0;
            InputManagerHelper.AddEntry(lEntry, true);

#else

                lEntry = new InputManagerEntry();
                lEntry.Name = "Camera Aim";
                lEntry.PositiveButton = "";
                lEntry.Gravity = 1;
                lEntry.Dead = 0.3f;
                lEntry.Sensitivity = 1;
                lEntry.Type = InputManagerEntryType.JOYSTICK_AXIS;
                lEntry.Axis = 10;
                lEntry.JoyNum = 0;
                InputManagerHelper.AddEntry(lEntry, true);

#endif
            }

            if (!InputManagerHelper.IsDefined("Camera Rotate Character"))
            {
                InputManagerEntry lEntry = new InputManagerEntry();
                lEntry.Name = "Camera Rotate Character";
                lEntry.PositiveButton = "mouse 1";
                lEntry.Gravity = 100;
                lEntry.Dead = 0.3f;
                lEntry.Sensitivity = 100;
                lEntry.Type = InputManagerEntryType.KEY_MOUSE_BUTTON;
                lEntry.Axis = 0;
                lEntry.JoyNum = 0;

                InputManagerHelper.AddEntry(lEntry, true);
            }

            if (!InputManagerHelper.IsDefined("Camera Follow"))
            {
                InputManagerEntry lEntry = new InputManagerEntry();
                lEntry.Name = "Camera Follow";
                lEntry.PositiveButton = "space";
                lEntry.Gravity = 100;
                lEntry.Dead = 0.3f;
                lEntry.Sensitivity = 100;
                lEntry.Type = InputManagerEntryType.KEY_MOUSE_BUTTON;
                lEntry.Axis = 0;
                lEntry.JoyNum = 0;

                InputManagerHelper.AddEntry(lEntry, true);
            }

            if (!InputManagerHelper.IsDefined("Camera Grip"))
            {
                InputManagerEntry lEntry = new InputManagerEntry();
                lEntry.Name = "Camera Grip";
                lEntry.PositiveButton = "mouse 0";
                lEntry.Gravity = 100;
                lEntry.Dead = 0.3f;
                lEntry.Sensitivity = 100;
                lEntry.Type = InputManagerEntryType.KEY_MOUSE_BUTTON;
                lEntry.Axis = 0;
                lEntry.JoyNum = 0;

                InputManagerHelper.AddEntry(lEntry, true);
            }

            if (!InputManagerHelper.IsDefined("Camera Forward"))
            {
                InputManagerEntry lEntry = new InputManagerEntry();
                lEntry.Name = "Camera Forward";
                lEntry.PositiveButton = "w";
                lEntry.Gravity = 100;
                lEntry.Dead = 0.3f;
                lEntry.Sensitivity = 100;
                lEntry.Type = InputManagerEntryType.KEY_MOUSE_BUTTON;
                lEntry.Axis = 0;
                lEntry.JoyNum = 0;

                InputManagerHelper.AddEntry(lEntry, true);
            }

            if (!InputManagerHelper.IsDefined("Camera Back"))
            {
                InputManagerEntry lEntry = new InputManagerEntry();
                lEntry.Name = "Camera Back";
                lEntry.PositiveButton = "s";
                lEntry.Gravity = 100;
                lEntry.Dead = 0.3f;
                lEntry.Sensitivity = 100;
                lEntry.Type = InputManagerEntryType.KEY_MOUSE_BUTTON;
                lEntry.Axis = 0;
                lEntry.JoyNum = 0;

                InputManagerHelper.AddEntry(lEntry, true);
            }

            if (!InputManagerHelper.IsDefined("Camera Left"))
            {
                InputManagerEntry lEntry = new InputManagerEntry();
                lEntry.Name = "Camera Left";
                lEntry.PositiveButton = "a";
                lEntry.Gravity = 100;
                lEntry.Dead = 0.3f;
                lEntry.Sensitivity = 100;
                lEntry.Type = InputManagerEntryType.KEY_MOUSE_BUTTON;
                lEntry.Axis = 0;
                lEntry.JoyNum = 0;

                InputManagerHelper.AddEntry(lEntry, true);
            }

            if (!InputManagerHelper.IsDefined("Camera Right"))
            {
                InputManagerEntry lEntry = new InputManagerEntry();
                lEntry.Name = "Camera Right";
                lEntry.PositiveButton = "d";
                lEntry.Gravity = 100;
                lEntry.Dead = 0.3f;
                lEntry.Sensitivity = 100;
                lEntry.Type = InputManagerEntryType.KEY_MOUSE_BUTTON;
                lEntry.Axis = 0;
                lEntry.JoyNum = 0;

                InputManagerHelper.AddEntry(lEntry, true);
            }
        }

        /// <summary>
        /// Returns a friendly name for the type
        /// </summary>
        /// <param name="rType"></param>
        /// <returns></returns>
        private string GetFriendlyName(Type rType)
        {
            string lTypeName = rType.Name;
            object[] lMotionAttributes = rType.GetCustomAttributes(typeof(BaseNameAttribute), true);
            if (lMotionAttributes != null && lMotionAttributes.Length > 0) { lTypeName = ((BaseNameAttribute)lMotionAttributes[0]).Value; }

            return lTypeName;
        }

        /// <summary>
        /// Returns a friendly name for the type
        /// </summary>
        /// <param name="rType"></param>
        /// <returns></returns>
        private string GetDescription(Type rType)
        {
            string lDescription = "";
            object[] lMotionAttributes = rType.GetCustomAttributes(typeof(BaseDescriptionAttribute), true);
            if (lMotionAttributes != null && lMotionAttributes.Length > 0) { lDescription = ((BaseDescriptionAttribute)lMotionAttributes[0]).Value; }

            return lDescription;
        }

        /// <summary>
        /// Box used to group standard GUI elements
        /// </summary>
        private static GUIStyle mFirstPersonIcon = null;
        private static GUIStyle FirstPersonIcon
        {
            get
            {
                if (mFirstPersonIcon == null)
                {
                    Texture2D lTexture = Resources.Load<Texture2D>(EditorGUIUtility.isProSkin ? "FirstPersonIcon" : "FirstPersonIcon");

                    mFirstPersonIcon = new GUIStyle(GUI.skin.box);
                    mFirstPersonIcon.normal.background = lTexture;
                    mFirstPersonIcon.padding = new RectOffset(0, 0, 0, 0);
                    mFirstPersonIcon.border = new RectOffset(0, 0, 0, 0);
                }

                return mFirstPersonIcon;
            }
        }

        /// <summary>
        /// Box used to group standard GUI elements
        /// </summary>
        private static GUIStyle mThirdPersonIcon = null;
        private static GUIStyle ThirdPersonIcon
        {
            get
            {
                if (mThirdPersonIcon == null)
                {
                    Texture2D lTexture = Resources.Load<Texture2D>(EditorGUIUtility.isProSkin ? "ThirdPersonIcon" : "ThirdPersonIcon");

                    mThirdPersonIcon = new GUIStyle(GUI.skin.box);
                    mThirdPersonIcon.normal.background = lTexture;
                    mThirdPersonIcon.padding = new RectOffset(0, 0, 0, 0);
                    mThirdPersonIcon.border = new RectOffset(0, 0, 0, 0);
                }

                return mThirdPersonIcon;
            }
        }

        /// <summary>
        /// Box used to group standard GUI elements
        /// </summary>
        private static GUIStyle mMOBAIcon = null;
        private static GUIStyle MOBAIcon
        {
            get
            {
                if (mMOBAIcon == null)
                {
                    Texture2D lTexture = Resources.Load<Texture2D>(EditorGUIUtility.isProSkin ? "MOBAIcon" : "MOBAIcon");

                    mMOBAIcon = new GUIStyle(GUI.skin.box);
                    mMOBAIcon.normal.background = lTexture;
                    mMOBAIcon.padding = new RectOffset(0, 0, 0, 0);
                    mMOBAIcon.border = new RectOffset(0, 0, 0, 0);
                }

                return mMOBAIcon;
            }
        }
    }
}