using UnityEngine;
using com.ootii.Base;
using com.ootii.Helpers;
using com.ootii.Geometry;
using com.ootii.Input;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Cameras
{
    /// <summary>
    /// Motor that stays at the fixed offset from the anchor
    /// </summary>
    [BaseName("Top-Down View Motor")]
    [BaseDescription("Camera Motor for strategy games and MOBAs that allow for a top-down view.")]
    public class WorldMotor : YawPitchMotor
    {
        public Vector3 _MinBounds = new Vector3(-100, -100, -100);
        public Vector3 MinBounds
        {
            get { return _MinBounds; }
            set { _MinBounds = value; }
        }

        public Vector3 _MaxBounds = new Vector3(100, 100, 100);
        public Vector3 MaxBounds
        {
            get { return _MaxBounds; }
            set { _MaxBounds = value; }
        }

        public bool _FollowGround = false;
        public bool FollowGround
        {
            get { return _FollowGround; }
            set { _FollowGround = value; }
        }

        public float _GroundDistance = 0f;
        public float GroundDistance
        {
            get { return _GroundDistance; }
            set { _GroundDistance = value; }
        }

        public float _GroundSmoothing = 0.05f;
        public float GroundSmoothing
        {
            get { return _GroundSmoothing; }
            set { _GroundSmoothing = Mathf.Clamp(value, 0f, 0.8f); }
        }

        public int _GroundLayers = 1;
        public int GroundLayers
        {
            get { return _GroundLayers; }
            set { _GroundLayers = value; }
        }

        public bool _FollowAnchor = false;
        public bool FollowAnchor
        {
            get { return _FollowAnchor; }
            set { _FollowAnchor = value; }
        }

        public string _FollowAlias = "Camera Follow";
        public string FollowAlias
        {
            get { return _FollowAlias; }
            set { _FollowAlias = value; }
        }

        public bool _FollowElevation = true;
        public bool FollowElevation
        {
            get { return _FollowElevation; }
            set { _FollowElevation = value; }
        }

        public bool _FollowFromView = true;
        public bool FollowFromView
        {
            get { return _FollowFromView; }
            set { _FollowFromView = value; }
        }

        public bool _AllowDisconnect = true;
        public bool AllowDisconnect
        {
            get { return _AllowDisconnect; }
            set { _AllowDisconnect = value; }
        }


        public bool _GripPan = true;
        public bool GripPan
        {
            get { return _GripPan; }
            set { _GripPan = value; }
        }

        public string _GripAlias = "Camera Grip";
        public string GripAlias
        {
            get { return _GripAlias; }
            set { _GripAlias = value; }
        }

        public float _GripPanSpeed = 50f;
        public float GripPanSpeed
        {
            get { return _GripPanSpeed; }
            set { _GripPanSpeed = value; }
        }


        public bool _EdgePan = true;
        public bool EdgePan
        {
            get { return _EdgePan; }
            set { _EdgePan = value; }
        }

        public float _EdgePanBorder = 20f;
        public float EdgePanBorder
        {
            get { return _EdgePanBorder; }
            set { _EdgePanBorder = value; }
        }

        public float _EdgePanSpeed = 10f;
        public float EdgePanSpeed
        {
            get { return _EdgePanSpeed; }
            set { _EdgePanSpeed = value; }
        }


        public bool _InputPan = true;
        public bool InputPan
        {
            get { return _InputPan; }
            set { _InputPan = value; }
        }

        public bool _UseViewInput = false;
        public bool UseViewInput
        {
            get { return _UseViewInput; }
            set { _UseViewInput = value; }
        }

        public string _ForwardAlias = "Camera Forward";
        public string ForwardAlias
        {
            get { return _ForwardAlias; }
            set { _ForwardAlias = value; }
        }

        public string _BackAlias = "Camera Back";
        public string BackAlias
        {
            get { return _BackAlias; }
            set { _BackAlias = value; }
        }

        public string _LeftAlias = "Camera Left";
        public string LeftAlias
        {
            get { return _LeftAlias; }
            set { _LeftAlias = value; }
        }

        public string _RightAlias = "Camera Right";
        public string RightAlias
        {
            get { return _RightAlias; }
            set { _RightAlias = value; }
        }

        public float _InputPanSpeed = 10f;
        public float InputPanSpeed
        {
            get { return _InputPanSpeed; }
            set { _InputPanSpeed = value; }
        }

        /// <summary>
        /// Determines if follow is connected or not
        /// </summary>
        protected bool mIsFollowConnected = true;
        public bool IsFollowConnected
        {
            get { return mIsFollowConnected; }
            set { mIsFollowConnected = value; }
        }

        /// <summary>
        /// Consistant speed values
        /// </summary>
        public float mGripUnitsPerTick = 0f;
        public float mEdgeUnitsPerTick = 0f;
        public float mInputUnitsPerTick = 0f;

        /// <summary>
        /// Target that we're heading to
        /// </summary>
        protected Vector3 mPositionTarget = Vector3.zero;
        protected Vector3 mPositionVelocity = Vector3.zero; 

        /// <summary>
        /// Default constructor
        /// </summary>
        public WorldMotor() :base ()
        {
            _MaxDistance = 8f;
            mDistance = 8f;
        }

        /// <summary>
        /// Called to initialize the motor. This can be done multiple times to
        /// reset and prepare the motor for activation.
        /// </summary>
        public override bool Initialize()
        {
            base.Initialize();

            mGripUnitsPerTick = _GripPanSpeed * Time.deltaTime;
            mEdgeUnitsPerTick = _EdgePanSpeed * Time.deltaTime;
            mInputUnitsPerTick = _InputPanSpeed * Time.deltaTime;

            mRigTransform.Position = RigController.Transform.position;
            mRigTransform.Rotation = RigController.Transform.rotation;

            mPositionTarget = mRigTransform.Position;

            if (_MaxDistance == 0f)
            {
                MaxDistance = RigController._Transform.position.y - AnchorPosition.y;
            }

            if (_GroundDistance == 0f)
            {
                GroundDistance = GetGroundDistance();
            }

            return true;
        }

        /// <summary>
        /// Updates the motor over time. This is called by the controller
        /// every update cycle so movement can be updated. 
        /// </summary>
        /// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
        /// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
        /// <param name="rTiltAngle">Amount of tilting the camera needs to do to match the anchor</param>
        public override CameraTransform RigLateUpdate(float rDeltaTime, int rUpdateIndex, float rTiltAngle = 0f)
        {
            bool lSmooth = true;
            bool lContinue = true;
            Vector3 lMovement = Vector3.zero;
            IInputSource lInputSource = RigController.InputSource;

            // Camera rotation we'll use to modify the input
            Quaternion lCameraYaw = Quaternion.Euler(0f, RigController._Transform.rotation.eulerAngles.y, 0f);

            // Follow anchor logic
            if (_FollowAnchor && Anchor != null && lMovement.sqrMagnitude == 0f)
            {
                if (!_AllowDisconnect)
                {
                    mIsFollowConnected = true;
                }
                else if (_FollowAlias.Length == 0)
                {
                    mIsFollowConnected = true;
                }
                else if (_FollowAlias.Length > 0 && lInputSource != null && lInputSource.IsPressed(_FollowAlias))
                {
                    mIsFollowConnected = true;
                }

                if (mIsFollowConnected)
                {
                    lSmooth = false;
                    lContinue = false;
                    //Vector3 lCameraPosition = RigController._Transform.position;
                    Vector3 lAnchorPosition = AnchorPosition;

                    if (_FollowFromView)
                    {
                        //float lAngle = 90f - RigController._Transform.eulerAngles.x;
                        //float lDistance = _MaxDistance / Mathf.Cos(lAngle * Mathf.Deg2Rad);
                        Vector3 lLastPositionTarget = mPositionTarget;

                        mPositionTarget = lAnchorPosition - (RigController._Transform.forward * _MaxDistance);
                        //lMovement = lNewCameraPosition - lCameraPosition;

                        if (!_FollowElevation) { mPositionTarget.y = lLastPositionTarget.y; }
                    }
                    else
                    {
                        lMovement.x = lAnchorPosition.x - RigController._Transform.position.x;
                        lMovement.y = (_FollowElevation ? lAnchorPosition.y - RigController._Transform.position.y : 0f);
                        lMovement.z = lAnchorPosition.z - RigController._Transform.position.z;
                    }
                }
            }
            else
            {
                mIsFollowConnected = false;
                mPositionTarget = RigController._Transform.position;
            }

            // Grip logic
            if (_GripPan && lInputSource != null && lMovement.sqrMagnitude == 0f)
            {
                if (_GripAlias.Length == 0 || lInputSource.IsPressed(_GripAlias))
                {
                    if (lInputSource.ViewX != 0f || lInputSource.ViewY != 0f)
                    {
                        mIsFollowConnected = false;

                        if (lContinue)
                        {
                            lContinue = false;
                            lMovement.x = lInputSource.ViewX * -mGripUnitsPerTick;
                            lMovement.z = lInputSource.ViewY * -mGripUnitsPerTick;

                            lMovement = lCameraYaw * lMovement;
                        }
                    }
                }
            }

            // Input logic
            if (_InputPan && lInputSource != null && lMovement.sqrMagnitude == 0f)
            {
                float lZ = (_ForwardAlias.Length > 0 && lInputSource.IsPressed(_ForwardAlias) ? 1f : 0f);
                lZ = lZ - (_BackAlias.Length > 0 && lInputSource.IsPressed(_BackAlias) ? 1f : 0f);

                float lX = (_RightAlias.Length > 0 && lInputSource.IsPressed(_RightAlias) ? 1f : 0f);
                lX = lX - (_LeftAlias.Length > 0 && lInputSource.IsPressed(_LeftAlias) ? 1f : 0f);

                if (lX != 0f || lZ != 0f)
                {
                    mIsFollowConnected = false;

                    if (lContinue)
                    {
                        float lMagnitude = 1f;
                        InputManagerHelper.ConvertToRadialInput(ref lX, ref lZ, ref lMagnitude);

                        lContinue = false;
                        lMovement.x = lX;
                        lMovement.z = lZ;

                        lMovement = (lCameraYaw * lMovement) * mInputUnitsPerTick;
                    }
                }
            }

            // Edge logic
            if (_EdgePan && lMovement.sqrMagnitude == 0f)
            {
                Vector3 lEdgeMovement = Vector3.zero;

                float lScreenWidth = Screen.width;
                float lScreenHeight = Screen.height;

                Vector3 lMousePosition = UnityEngine.Input.mousePosition;
                lMousePosition.x = Mathf.Clamp(lMousePosition.x, 0f, lScreenWidth - 1f);
                lMousePosition.y = Mathf.Clamp(lMousePosition.y, 0f, lScreenHeight - 1f);

                float lEdgePanBorder = (_EdgePanBorder > 1f ? _EdgePanBorder : 1f);

                if (lMousePosition.x < lEdgePanBorder)
                {
                    float lPercent = (lEdgePanBorder - lMousePosition.x) / lEdgePanBorder;
                    lEdgeMovement.x = -mEdgeUnitsPerTick * lPercent;
                }
                else if (lMousePosition.x >= lScreenWidth - _EdgePanBorder)
                {
                    float lPercent = (lEdgePanBorder - (lScreenWidth - 1f - lMousePosition.x)) / lEdgePanBorder;
                    lEdgeMovement.x = mEdgeUnitsPerTick * lPercent;
                }

                if (lMousePosition.y < _EdgePanBorder)
                {
                    float lPercent = (lEdgePanBorder - lMousePosition.y) / lEdgePanBorder;
                    lEdgeMovement.z = -mEdgeUnitsPerTick * lPercent;
                }
                else if (lMousePosition.y >= lScreenHeight - _EdgePanBorder)
                {
                    float lPercent = (lEdgePanBorder - (lScreenHeight - 1f - lMousePosition.y)) / lEdgePanBorder;
                    lEdgeMovement.z = mEdgeUnitsPerTick * lPercent;
                }

                if (lEdgeMovement.sqrMagnitude > 0f)
                {
                    mIsFollowConnected = false;

                    if (lContinue)
                    {
                        lContinue = false;
                        lMovement.x = lEdgeMovement.x;
                        lMovement.z = lEdgeMovement.z;

                        lMovement = lCameraYaw * lMovement;
                    }
                }
            }

            // Set the final results and apply any bounds
            //Vector3 lNewPosition = mRigTransform.Position + lMovement;
            mPositionTarget = mPositionTarget + lMovement;

            if (!mIsFollowConnected || !_FollowElevation)
            {
                Vector3 lGroundTarget = GetGroundTarget();
                if (lGroundTarget != Vector3Ext.Null)
                {
                    mPositionTarget.y = SmoothDamp(mPositionTarget.y, lGroundTarget.y + _GroundDistance, _GroundSmoothing, rDeltaTime);
                }
            }

            if (_MinBounds.x != 0f || _MaxBounds.x != 0f)
            {
                mPositionTarget.x = Mathf.Clamp(mPositionTarget.x, _MinBounds.x, _MaxBounds.x);
            }

            if (_MinBounds.y != 0f || _MaxBounds.y != 0f)
            {
                mPositionTarget.y = Mathf.Clamp(mPositionTarget.y, _MinBounds.y, _MaxBounds.y);
            }

            if (_MinBounds.z != 0f || _MaxBounds.z != 0f)
            {
                mPositionTarget.z = Mathf.Clamp(mPositionTarget.z, _MinBounds.z, _MaxBounds.z);
            }

            lMovement = (!lSmooth ? mPositionTarget : Vector3.SmoothDamp(RigController._Transform.position, mPositionTarget, ref mPositionVelocity, rDeltaTime)) - RigController._Transform.position;
            mRigTransform.Position = RigController._Transform.position + lMovement;

            mRigTransform.Rotation = RigController._Transform.rotation;

            return mRigTransform;
        }

        /// <summary>
        /// Allows the motor to process after the camera and controller have completed
        /// </summary>
        public override void PostRigLateUpdate()
        {
            Transform lAnchor = Anchor;
            if (lAnchor != null)
            {
                mAnchorLastPosition = Anchor.position;
                mAnchorLastRotation = Anchor.rotation;
            }

            // Reset our internal rotation targets
            if (Vector3.Distance(mPositionTarget, mRigTransform.Position) < EPSILON)
            {
                //mPosition = RigController.Transform.position;
                mPositionTarget = mRigTransform.Position;
                mPositionVelocity.x = 0f;
                mPositionVelocity.y = 0f;
                mPositionVelocity.z = 0f;
            }

            mAnchorOffsetDistance = Mathf.Min(mAnchorOffsetDistance + (1f * Time.deltaTime), AnchorOffset.magnitude);
        }

        /// <summary>
        /// Determines the cameras vertical target on the ground
        /// </summary>
        /// <returns></returns>
        public Vector3 GetGroundTarget()
        {
            Vector3 lStart = RigController._Transform.position;
            float lDistance = (_GroundDistance > 0f ? _GroundDistance * 20f : 100f);

            RaycastHit lHitInfo;
            if (RaycastExt.SafeRaycast(lStart, Vector3.down, out lHitInfo, lDistance, _GroundLayers, _Anchor))
            {
                return lHitInfo.point;
            }

            return Vector3Ext.Null;
        }

        /// <summary>
        /// Determines the cameras vertical distance to the ground
        /// </summary>
        /// <returns></returns>
        public float GetGroundDistance()
        {
            Vector3 lStart = RigController._Transform.position;
            float lDistance = (_GroundDistance > 0f ? _GroundDistance * 20f : 100f);

            RaycastHit lHitInfo;
            if (RaycastExt.SafeRaycast(lStart, Vector3.down, out lHitInfo, lDistance, _GroundLayers, _Anchor))
            {
                return lHitInfo.distance;
            }

            return 0f;
        }

        // **************************************************************************************************
        // Following properties and function only valid while editing
        // **************************************************************************************************

#if UNITY_EDITOR

        /// <summary>
        /// Allow the motion to render it's own GUI
        /// </summary>
        public override bool OnInspectorGUI()
        {
            bool lIsDirty = false;

            if (EditorHelper.Vector3Field("Min Bound", "Minimum world bounds the camera can move to. Setting min and max to 0 disables.", MinBounds, RigController))
            {
                lIsDirty = true;
                MinBounds = EditorHelper.FieldVector3Value;
            }

            if (EditorHelper.Vector3Field("Max Bound", "Maximum world bounds the camera can move to. Setting min and max to 0 disables.", MaxBounds, RigController))
            {
                lIsDirty = true;
                MaxBounds = EditorHelper.FieldVector3Value;
            }

            GUILayout.Space(5f);

            if (EditorHelper.BoolField("Follow Ground", "Determines if move the camera vertically with the ground distance. Checking 'Follow Anchor' + 'Use Anchor Y' overrides the vertical distance.", FollowGround, RigController))
            {
                lIsDirty = true;
                FollowGround = EditorHelper.FieldBoolValue;
            }

            if (FollowGround)
            {
                if (EditorHelper.FloatField("Vertical Distance", "Vertical distance from the ground. '0' means use camera's starting distance.", GroundDistance, RigController))
                {
                    lIsDirty = true;
                    GroundDistance = EditorHelper.FieldFloatValue;
                }

                if (EditorHelper.FloatField("Vertical Smoothing", "Determines how much smoothing is applied to vertical movement (0 = no smoothing, 0.8 = max smoothing)", GroundSmoothing, RigController))
                {
                    lIsDirty = true;
                    GroundSmoothing = EditorHelper.FieldFloatValue;
                }

                int lNewCollisionLayers = EditorHelper.LayerMaskField(new GUIContent("Ground Layers", "Layers that we'll collide with when determining camera height."), GroundLayers);
                if (lNewCollisionLayers != GroundLayers)
                {
                    lIsDirty = true;
                    GroundLayers = lNewCollisionLayers;
                }
            }

            GUILayout.Space(5f);

            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();

            if (EditorHelper.BoolField(FollowAnchor, "Follow Anchor", RigController, 14f))
            {
                lIsDirty = true;
                FollowAnchor = EditorHelper.FieldBoolValue;
            }

            EditorGUILayout.LabelField(new GUIContent("Follow Anchor", "Determines if we'll pan as the anchor moves."));

            EditorGUILayout.EndHorizontal();

            if (FollowAnchor)
            {
                if (EditorHelper.TextField("Action Alias", "Input alias that activates the panning. If no input is set, we'll always follow the anchor.", FollowAlias, RigController))
                {
                    lIsDirty = true;
                    FollowAlias = EditorHelper.FieldStringValue;
                }

                GUILayout.Space(5f);

                if (EditorHelper.BoolField("Use View Direction", "Determines if we follow using our view direction or just the x,z position.", FollowFromView, RigController))
                {
                    lIsDirty = true;
                    FollowFromView = EditorHelper.FieldBoolValue;
                }

                if (EditorHelper.BoolField("Use Anchor Y", "Determines if we follow the anchor's y-position and force the vertical distance from it.", FollowElevation, RigController))
                {
                    lIsDirty = true;
                    FollowElevation = EditorHelper.FieldBoolValue;
                }

                if (EditorHelper.FloatField("Distance", "Distance from the anchor the camera will follow", MaxDistance, RigController))
                {
                    lIsDirty = true;
                    MaxDistance = EditorHelper.FieldFloatValue;
                }

                GUILayout.Space(5f);

                if (EditorHelper.BoolField("Allow Disconnecting", "Determines if one of the other pan options will disable follow until the alias is pressed.", AllowDisconnect, RigController))
                {
                    lIsDirty = true;
                    AllowDisconnect = EditorHelper.FieldBoolValue;
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();

            if (EditorHelper.BoolField(GripPan, "Grip Pan", RigController, 14f))
            {
                lIsDirty = true;
                GripPan = EditorHelper.FieldBoolValue;
            }

            EditorGUILayout.LabelField(new GUIContent("Grip Pan", "Determines if we'll pan the camera using the mouse."));

            EditorGUILayout.EndHorizontal();

            if (GripPan)
            {
                if (EditorHelper.TextField("Action Alias", "Input alias that activates the panning", GripAlias, RigController))
                {
                    lIsDirty = true;
                    GripAlias = EditorHelper.FieldStringValue;
                }

                if (EditorHelper.FloatField("Speed", "Units per second that the camera will pan", GripPanSpeed, RigController))
                {
                    lIsDirty = true;
                    GripPanSpeed = EditorHelper.FieldFloatValue;
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();

            if (EditorHelper.BoolField(EdgePan, "Edge Pan", RigController, 14f))
            {
                lIsDirty = true;
                EdgePan = EditorHelper.FieldBoolValue;
            }

            EditorGUILayout.LabelField(new GUIContent("Edge Pan", "Determines if we'll pan the camera when the cursor reaches the edge."));

            EditorGUILayout.EndHorizontal();

            if (EdgePan)
            {
                if (EditorHelper.FloatField("Border", "Pixels width/height of the border we'll pan in", EdgePanBorder, RigController))
                {
                    lIsDirty = true;
                    EdgePanBorder = EditorHelper.FieldFloatValue;
                }

                if (EditorHelper.FloatField("Speed", "Units per second that the camera will pan", EdgePanSpeed, RigController))
                {
                    lIsDirty = true;
                    EdgePanSpeed = EditorHelper.FieldFloatValue;
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();

            if (EditorHelper.BoolField(InputPan, "Input Pan", RigController, 14f))
            {
                lIsDirty = true;
                InputPan = EditorHelper.FieldBoolValue;
            }

            EditorGUILayout.LabelField(new GUIContent("Input Pan", "Determines if we'll pan the camera when when input is pressed."));

            EditorGUILayout.EndHorizontal();

            if (InputPan)
            {
                if (EditorHelper.TextField("Forward Alias", "Input alias that causes the camera to move forward", ForwardAlias, RigController))
                {
                    lIsDirty = true;
                    ForwardAlias = EditorHelper.FieldStringValue;
                }

                if (EditorHelper.TextField("Back Alias", "Input alias that causes the camera to move forward", BackAlias, RigController))
                {
                    lIsDirty = true;
                    BackAlias = EditorHelper.FieldStringValue;
                }

                if (EditorHelper.TextField("Left Alias", "Input alias that causes the camera to move forward", LeftAlias, RigController))
                {
                    lIsDirty = true;
                    LeftAlias = EditorHelper.FieldStringValue;
                }

                if (EditorHelper.TextField("Right Alias", "Input alias that causes the camera to move forward", RightAlias, RigController))
                {
                    lIsDirty = true;
                    RightAlias = EditorHelper.FieldStringValue;
                }

                if (EditorHelper.FloatField("Speed", "Units per second that the camera will pan", InputPanSpeed, RigController))
                {
                    lIsDirty = true;
                    InputPanSpeed = EditorHelper.FieldFloatValue;
                }
            }

            EditorGUILayout.EndVertical();

            return lIsDirty;
        }

#endif

    }
}
