using System;
using System.Reflection;
using System.Text;
using UnityEngine;
using com.ootii.Base;
using com.ootii.Geometry;
using com.ootii.Helpers;
using com.ootii.Input;
using com.ootii.Timing;
using com.ootii.Utilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Cameras
{
    /// <summary>
    /// Base class that drives the camera using yaw and pitch values
    /// </summary>
    public abstract class YawPitchMotor : CameraMotor
    {
        /// <summary>
        /// Provides a value for "numerical error"
        /// </summary>
        public const float EPSILON = 0.0001f;

        /// <summary>
        /// Desired distance from the anchor's position
        /// </summary>
        public float _MaxDistance = 3f;
        public override float MaxDistance
        {
            get { return _MaxDistance; }
            set { _MaxDistance = value; }
        }

        /// <summary>
        /// Actual distance from the anchor's position
        /// </summary>
        protected float mDistance = 3f;
        public override float Distance
        {
            get { return mDistance; }
            set { mDistance = value; }
        }

        /// <summary>
        /// Determines if we can yaw
        /// </summary>
        public bool _IsYawEnabled = true;
        public virtual bool IsYawEnabled
        {
            get { return _IsYawEnabled; }
            set { _IsYawEnabled = value; }
        }

        /// <summary>
        /// Yaw of the camera relative to the anchor's forward.
        /// Looking to the anchor's left is < 0f
        /// Looking to the anchor's right is > 0f
        /// Looking in the direction of the anchor = 0f
        /// Looking against the direction of the anchor = 180f
        /// </summary>
        public virtual float LocalYaw
        {
            get
            {
                float lYaw = 0f;

                Transform lAnchor = Anchor;
                if (lAnchor == null)
                {
                    lYaw = RigController._Transform.rotation.eulerAngles.y;
                }
                else
                {
                    Quaternion lRotation = Quaternion.Inverse(lAnchor.rotation) * RigController._Transform.rotation;
                    lYaw = lRotation.eulerAngles.y;
                }

                if (lYaw > 180f) { lYaw = lYaw - 360f; }
                else if (lYaw < -180f) { lYaw = lYaw + 360f; }
                return lYaw;

                //Transform lAnchor = Anchor;
                //if (lAnchor != null)
                //{
                //    lYaw = Vector3Ext.HorizontalAngleTo(lAnchor.forward, RigController._Transform.forward, RigController.Tilt.Up());
                //}
                //else
                //{
                //    lYaw = Vector3Ext.HorizontalAngleTo(Vector3.forward, RigController._Transform.forward, RigController.Tilt.Up());
                //}

                //return lYaw;
            }

            //set
            //{
            //    float lYaw = LocalYaw;

            //    if (value != lYaw)
            //    {
            //        Vector3 lFrameEuler = new Vector3(0f, value - lYaw, 0f);
            //        Vector3 lCameraPosition = RigController._Transform.position;
            //        Quaternion lCameraRotation = RigController._Transform.rotation;

            //        if (Modes[mTransitionMode]._ForceForward)
            //        {
            //            UpdateAnchorRotation(0f, ref lCameraPosition, ref lCameraRotation);
            //        }
            //        //else if (Modes[_Mode]._AnchorDistance <= 0f)
            //        //{
            //        //    UpdateFirstPerson(0f, lFrameEuler);
            //        //}
            //        else
            //        {
            //            UpdateThirdPerson(0f, lFrameEuler, ref lCameraPosition, ref lCameraRotation);
            //        }

            //        _Transform.position = lCameraPosition;
            //        _Transform.rotation = lCameraRotation;
            //    }
            //}
        }

        /// <summary>
        /// Minimum yaw value allowed
        /// </summary>
        public float _MinYaw = -180f;
        public virtual float MinYaw
        {
            get { return _MinYaw; }
            set { _MinYaw = value; }
        }

        /// <summary>
        /// Maximum yaw value allowed
        /// </summary>
        public float _MaxYaw = 180f;
        public virtual float MaxYaw
        {
            get { return _MaxYaw; }
            set { _MaxYaw = value; }
        }

        /// <summary>
        /// Speed at which we yaw
        /// </summary>
        public float _YawSpeed = 120f;
        public virtual float YawSpeed
        {
            get { return _YawSpeed; }

            set
            {
                _YawSpeed = value;
                mDegreesYPer60FPSTick = _YawSpeed / 60f;
            }
        }

        /// <summary>
        /// Multiplier to the rotation speed if the anchor is rotating and a target is set
        /// </summary>
        public float _TargetRotationMultiplier = 3f;
        public float TargetRotationMultiplier
        {
            get { return _TargetRotationMultiplier; }
            set { _TargetRotationMultiplier = value; }
        }

        /// <summary>
        /// Determines if we can pitch
        /// </summary>
        public bool _IsPitchEnabled = true;
        public virtual bool IsPitchEnabled
        {
            get { return _IsPitchEnabled; }
            set { _IsPitchEnabled = value; }
        }

        /// <summary>
        /// Determines if we invert the pitch information we get from the input
        /// </summary>
        public bool _InvertPitch = true;
        public virtual bool InvertPitch
        {
            get { return _InvertPitch; }
            set { _InvertPitch = value; }
        }

        /// <summary>
        /// Pitch of the camera relative to the anchor's forward.
        /// Looking above the anchor (up) is < 0f
        /// Looking below the anchor (down) is > 0f
        /// Looking in the direction of the anchor = 0f
        /// </summary>
        public virtual float LocalPitch
        {
            get
            {
                float lPitch = 0f;

                Transform lAnchor = Anchor;
                if (lAnchor == null)
                {
                    lPitch = RigController._Transform.rotation.eulerAngles.x;
                }
                else
                {
                    Quaternion lRotation = Quaternion.Inverse(lAnchor.rotation) * RigController._Transform.rotation;
                    lPitch = lRotation.eulerAngles.x;
                }

                if (lPitch > 180f) { lPitch = lPitch - 360f; }
                else if (lPitch < -180f) { lPitch = lPitch + 360f; }
                return lPitch;

                //Quaternion lRotation = Quaternion.Inverse(RigController.Tilt) * RigController._Transform.rotation;

                //float lPitch = lRotation.eulerAngles.x;
                //if (lPitch > 180f) { lPitch = lPitch - 360f; }

                //return lPitch;
            }

            //set
            //{
            //    float lPitch = LocalPitch;

            //    if (value != lPitch)
            //    {
            //        Vector3 lFrameEuler = new Vector3(value - lPitch, 0f, 0f);
            //        Vector3 lCameraPosition = Vector3.zero;
            //        Quaternion lCameraRotation = Quaternion.identity;

            //        if (Modes[mTransitionMode]._ForceForward)
            //        {
            //            UpdateAnchorRotation(0f, ref lCameraPosition, ref lCameraRotation);
            //        }
            //        //else if (Modes[_Mode]._AnchorDistance <= 0f)
            //        //{
            //        //    UpdateFirstPerson(0f, lFrameEuler);
            //        //}
            //        else
            //        {
            //            UpdateThirdPerson(0f, lFrameEuler, ref lCameraPosition, ref lCameraRotation);
            //        }

            //        _Transform.position = lCameraPosition;
            //        _Transform.rotation = lCameraRotation;
            //    }
            //}
        }

        /// <summary>
        /// Minimum pitch value allowed
        /// </summary>
        public float _MinPitch = -60f;
        public virtual float MinPitch
        {
            get { return _MinPitch; }
            set { _MinPitch = Mathf.Max(value, CameraController.MIN_PITCH); }
        }

        /// <summary>
        /// Maximum pitch value allowed
        /// </summary>
        public float _MaxPitch = 60f;
        public virtual float MaxPitch
        {
            get { return _MaxPitch; }
            set { _MaxPitch = Mathf.Min(value, CameraController.MAX_PITCH); }
        }

        /// <summary>
        /// Speed at which we pitch
        /// </summary>
        public float _PitchSpeed = 45f;
        public virtual float PitchSpeed
        {
            get { return _PitchSpeed; }

            set
            {
                _PitchSpeed = value;
                mDegreesXPer60FPSTick = _PitchSpeed / 60f;
            }
        }

        /// <summary>
        /// Amount of smoothing we apply to the input
        /// </summary>
        public float _Smoothing = 0.05f;
        public float Smoothing
        {
            get { return _Smoothing; }
            set { _Smoothing = value; }
        }


        /// <summary>
        /// Holds the yaw and pitch values for us
        /// </summary>
        protected Vector3 _Euler = Vector3.zero;
        public Vector3 Euler
        {
            get { return _Euler; }
        }

        /// <summary>
        /// Holds the yaw and pitch values we're trying to get to
        /// </summary>
        protected Vector3 _EulerTarget = Vector3.zero;
        public Vector3 EulerTarget
        {
            get { return _EulerTarget; }
        }
        
        /// <summary>
        /// Speed we'll actually apply to the rotation. This is essencially the
        /// number of degrees per tick assuming we're running at 60 FPS
        /// </summary>
        protected float mDegreesYPer60FPSTick = 1f;
        protected float mDegreesXPer60FPSTick = 1f;

        /// <summary>
        /// Track the euler change this frame (mostly for debugging)
        /// </summary>
        protected Vector3 mFrameEuler = Vector3.zero;

        /// <summary>
        /// Track the last rotation of the anchor
        /// </summary>
        protected Vector3 mAnchorLastPosition = Vector3.zero;
        protected Quaternion mAnchorLastRotation = Quaternion.identity;
        protected Vector3 mFocusLastPosition = Vector3.zero;
        protected bool mWasAnchorRotating = false;

        /// <summary>
        /// Target values we'll move to and ignore input
        /// </summary>
        protected float mTargetYaw = float.MaxValue;
        protected float mTargetPitch = float.MaxValue;
        protected Vector3 mTargetForward = Vector3.zero;

        protected float mTargetYawSpeed = 0f;
        protected float mTargetPitchSpeed = 0f;

        protected bool mAutoClearTarget = true;

        /// <summary>
        /// Smooth out the orbiting of the camera
        /// </summary>
        protected float mViewVelocityY = 0f;
        protected float mViewVelocityX = 0f;

        /// <summary>
        /// Called when the motor is instantiated
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            mDistance = _MaxDistance;
            mDegreesYPer60FPSTick = _YawSpeed / 60f;
            mDegreesXPer60FPSTick = _PitchSpeed / 60f;

            if (Application.isPlaying)
            {
                Transform lAnchorTransform = Anchor;
                Transform lCameraTransform = RigController.Transform;

                if (lAnchorTransform != null)
                {
                    mAnchorLastPosition = lAnchorTransform.position;
                    mAnchorLastRotation = lAnchorTransform.rotation;
                }

                _Euler = lCameraTransform.eulerAngles;
                NormalizeEuler(ref _Euler);

                _EulerTarget = _Euler;

                mFocusLastPosition = GetFocusPosition(lCameraTransform.rotation);
            }
        }

        /// <summary>
        /// Called to initialize the motor. This can be done multiple times to
        /// reset and prepare the motor for activation.
        /// </summary>
        public override bool Initialize()
        {
            Transform lAnchorTransform = Anchor;
            Transform lCameraTransform = RigController.transform;

            if (lAnchorTransform != null)
            {
                mAnchorLastPosition = lAnchorTransform.position;
                mAnchorLastRotation = lAnchorTransform.rotation;
            }

            _Euler = lCameraTransform.eulerAngles;
            NormalizeEuler(ref _Euler);

            _EulerTarget = _Euler;

            mFocusLastPosition = GetFocusPosition(lCameraTransform.rotation);

            return base.Initialize();
        }

        /// <summary>
        /// Clears out any target we're moving to
        /// </summary>
        public void ClearTargetYawPitch()
        {
            mTargetYaw = float.MaxValue;
            mTargetPitch = float.MaxValue;
        }

        /// <summary>
        /// Causes us to ignore user input and force the camera to the specified localangles
        /// </summary>
        /// <param name="rYaw">Target local yaw</param>
        /// <param name="rPitch">Target local pitch</param>
        /// <param name="rSpeed">Degrees per second we'll rotate</param>
        public void SetTargetYawPitch(float rYaw, float rPitch, float rSpeed = -1f, bool rAutoClearTarget = true)
        {
            ClearTargetForward();

            mTargetYaw = rYaw;
            mTargetPitch = rPitch;
            mAutoClearTarget = rAutoClearTarget;

            float lDeltaYaw = Mathf.Abs(mTargetYaw - LocalYaw);
            float lDeltaPitch = Mathf.Abs(mTargetPitch - LocalPitch);

            if (rSpeed > 0f)
            {
                float lTime = (lDeltaYaw >= lDeltaPitch ? lDeltaYaw / rSpeed : lDeltaPitch / rSpeed);
                if (lTime > 0f)
                {
                    mTargetYawSpeed = lDeltaYaw / lTime;
                    mTargetPitchSpeed = lDeltaPitch / lTime;
                }
            }
            else if (rSpeed == 0f)
            {
                mTargetYawSpeed = 0f;
                mTargetPitchSpeed = 0f;
            }
            else
            {
                float lAnchorRotatingMultiplier = 1f;
                if (mWasAnchorRotating) { lAnchorRotatingMultiplier = _TargetRotationMultiplier; }
                else if (Anchor != null && !QuaternionExt.IsEqual(mAnchorLastRotation, Anchor.rotation)) { lAnchorRotatingMultiplier = _TargetRotationMultiplier; }

                mTargetYawSpeed = _YawSpeed * lAnchorRotatingMultiplier;
                mTargetPitchSpeed = _PitchSpeed * lAnchorRotatingMultiplier;
            }
        }

        /// <summary>
        /// Clears the forward direction we're trying to reach
        /// </summary>
        public void ClearTargetForward()
        {
            mTargetForward = Vector3.zero;
        }

        /// <summary>
        /// Causes us to ignore user input and force the camera to a specific direction.
        /// </summary>
        /// <param name="rForward">Forward direction the camera should look</param>
        /// <param name="rSpeed">Speed at which we get there</param>
        public void SetTargetForward(Vector3 rForward, float rSpeed = -1f, bool rAutoClearTarget = true)
        {
            if (rForward.sqrMagnitude == 0f) { return; }
            if (Anchor == null) { return; }

            ClearTargetYawPitch();
            mTargetForward = rForward;
            mAutoClearTarget = rAutoClearTarget;

            Quaternion lTargetRotation = Quaternion.LookRotation(mTargetForward, Anchor.up);

            Quaternion lLocalRotation = Quaternion.Inverse(Anchor.rotation) * RigController._Transform.rotation;
            Quaternion lLocalTargetRotation = Quaternion.Inverse(Anchor.rotation) * lTargetRotation;

            float lDeltaYaw = Mathf.Abs(lLocalTargetRotation.eulerAngles.y - lLocalRotation.eulerAngles.y);
            float lDeltaPitch = Mathf.Abs(lLocalTargetRotation.eulerAngles.x - lLocalRotation.eulerAngles.x);

            if (rSpeed > 0f)
            {
                float lTime = (lDeltaYaw >= lDeltaPitch ? lDeltaYaw / rSpeed : lDeltaPitch / rSpeed);
                if (lTime > 0f)
                {
                    mTargetYawSpeed = lDeltaYaw / lTime;
                    mTargetPitchSpeed = lDeltaPitch / lTime;
                }
            }
            else if (rSpeed == 0f)
            {
                mTargetYawSpeed = 0f;
                mTargetPitchSpeed = 0f;
            }
            else
            {
                mTargetYawSpeed = _YawSpeed;
                mTargetPitchSpeed = _PitchSpeed;
            }
        }

        /// <summary>
        /// Grabs the euler changes that happen this frame.
        /// </summary>
        /// <param name="rAnchorYaw">Anchor yaw that should be added</param>
        /// <returns></returns>
        public virtual Vector3 GetFrameEuler(bool rUseYawLimits, bool rUsePitchLimits = true)
        {
            Vector3 lFrameEuler = Vector3.zero;

            // If the camera was moved externally, we need to recalculate the last focus position
            if (RigController.LastPosition != RigController._Transform.position)
            {
                mFocusLastPosition = GetFocusPosition(RigController._Transform.rotation);
            }

            // Now we can grab the movement
            if (mTargetForward.sqrMagnitude > 0f)
            {
                Quaternion lTargetRotation = Quaternion.LookRotation(mTargetForward, Anchor.up);

                Quaternion lLocalRotation = Quaternion.Inverse(Anchor.rotation) * RigController._Transform.rotation;
                Quaternion lLocalTargetRotation = Quaternion.Inverse(Anchor.rotation) * lTargetRotation;

                float lDeltaYaw = lLocalTargetRotation.eulerAngles.y - lLocalRotation.eulerAngles.y;
                if (lDeltaYaw > 180f) { lDeltaYaw = lDeltaYaw - 360f; }
                else if (lDeltaYaw < -180f) { lDeltaYaw = lDeltaYaw + 360f; }

                float lDeltaPitch = lLocalTargetRotation.eulerAngles.x - lLocalRotation.eulerAngles.x;
                if (lDeltaPitch > 180f) { lDeltaPitch = lDeltaPitch - 360f; }
                else if (lDeltaPitch < -180f) { lDeltaPitch = lDeltaPitch + 360f; }

                if (mTargetYawSpeed <= 0f)
                {
                    lFrameEuler.y = lDeltaYaw;
                }
                else
                {
                    lFrameEuler.y = Mathf.Sign(lDeltaYaw) * Mathf.Min(mTargetYawSpeed * Time.deltaTime, Mathf.Abs(lDeltaYaw));
                }

                if (mTargetPitchSpeed <= 0f)
                {
                    lFrameEuler.x = lDeltaPitch;
                }
                else
                {
                    lFrameEuler.x = Mathf.Sign(lDeltaPitch) * Mathf.Min(mTargetPitchSpeed * Time.deltaTime, Mathf.Abs(lDeltaPitch));
                }

                if (Mathf.Abs(lFrameEuler.y) < EPSILON && Mathf.Abs(lFrameEuler.x) < EPSILON)
                {
                    _Euler.y = LocalYaw;
                    _Euler.x = LocalPitch;
                    _EulerTarget = _Euler;

                    if (mAutoClearTarget) { mTargetForward = Vector3.zero; }
                }
            }
            else if (mTargetYaw < float.MaxValue || mTargetPitch < float.MaxValue)
            {
                if (mTargetYaw < float.MaxValue)
                {
                    float lDeltaYaw = mTargetYaw - LocalYaw;

                    if (mTargetYawSpeed <= 0f)
                    {
                        lFrameEuler.y = lDeltaYaw;
                    }
                    else
                    {
                        lFrameEuler.y = Mathf.Sign(lDeltaYaw) * Mathf.Min(mTargetYawSpeed * Time.deltaTime, Mathf.Abs(lDeltaYaw));
                    }

                    // TRT 04/09/2017 - Added to stop trying to reach the target when the character is rotating
                    Transform lAnchorTransform = Anchor;
                    float lAnchorRootYawDelta = Vector3Ext.HorizontalAngleTo(mAnchorLastRotation.Forward(), lAnchorTransform.forward, lAnchorTransform.up);
                    if (Mathf.Abs(lFrameEuler.y) - Mathf.Abs(lAnchorRootYawDelta * 2f) < EPSILON)
                    {
                        _Euler.y = mTargetYaw;
                        _EulerTarget.y = mTargetYaw;

                        //if (mAutoClearTarget) { mTargetYaw = float.MaxValue; }
                    }

                    if (mAutoClearTarget && Mathf.Abs(lDeltaYaw) < EPSILON) { mTargetYaw = float.MaxValue; }
                }

                if (mTargetPitch < float.MaxValue)
                {
                    float lDeltaPitch = mTargetPitch - LocalPitch;

                    if (mTargetPitchSpeed <= 0f)
                    {
                        lFrameEuler.x = lDeltaPitch;
                    }
                    else
                    {
                        lFrameEuler.x = Mathf.Sign(lDeltaPitch) * Mathf.Min(mTargetPitchSpeed * Time.deltaTime, Mathf.Abs(lDeltaPitch));
                    }

                    if (Mathf.Abs(lFrameEuler.x) < EPSILON)
                    {
                        _Euler.x = mTargetPitch;
                        _EulerTarget.x = mTargetPitch;

                        //if (mAutoClearTarget) { mTargetPitch = float.MaxValue; }
                    }

                    if (mAutoClearTarget && Mathf.Abs(lDeltaPitch) < EPSILON) { mTargetPitch = float.MaxValue; }
                }
            }
            else
            {
                IInputSource lInputSource = RigController.InputSource;

                if (lInputSource.IsViewingActivated)
                {
                    if (_IsYawEnabled && lFrameEuler.y == 0f) { lFrameEuler.y = lInputSource.ViewX * mDegreesYPer60FPSTick; }
                    if (_IsPitchEnabled && lFrameEuler.x == 0f) { lFrameEuler.x = (RigController._InvertPitch || _InvertPitch ? -1f : 1f) * lInputSource.ViewY * mDegreesXPer60FPSTick; }
                }

                // Grab the smoothed yaw
                _EulerTarget.y = (rUseYawLimits && (_MinYaw > -180f || _MaxYaw < 180f) ? Mathf.Clamp(_EulerTarget.y + lFrameEuler.y, _MinYaw, _MaxYaw) : _EulerTarget.y + lFrameEuler.y);
                lFrameEuler.y = (_Smoothing <= 0f ? _EulerTarget.y : SmoothDamp(_Euler.y, _EulerTarget.y, _Smoothing * 0.001f, Time.deltaTime)) - _Euler.y;
                _Euler.y = _Euler.y + lFrameEuler.y;

                // Grab the smoothed pitch
                _EulerTarget.x = (rUsePitchLimits && (_MinPitch > -180f || _MaxPitch < 180f) ? Mathf.Clamp(_EulerTarget.x + lFrameEuler.x, _MinPitch, _MaxPitch) : _EulerTarget.x + lFrameEuler.x);
                lFrameEuler.x = (_Smoothing <= 0f ? _EulerTarget.x : SmoothDamp(_Euler.x, _EulerTarget.x, _Smoothing * 0.001f, Time.deltaTime)) - _Euler.x;
                _Euler.x = _Euler.x + lFrameEuler.x;
            }

            return lFrameEuler;
        }

        /// <summary>
        /// Allows the motor to process after the camera and controller have completed
        /// </summary>
        public override void PostRigLateUpdate()
        {
            Transform lAnchor = Anchor;
            if (lAnchor != null)
            {
                mWasAnchorRotating = !QuaternionExt.IsEqual(mAnchorLastRotation, lAnchor.rotation);

                mAnchorLastPosition = lAnchor.position;
                mAnchorLastRotation = lAnchor.rotation;
            }

            mFocusLastPosition = GetFocusPosition(RigController._Transform.rotation);

            // Reset our internal rotation targets
            if (Mathf.Abs(_EulerTarget.y - _Euler.y) < EPSILON)
            {
                _Euler.y = LocalYaw;
                _EulerTarget.y = _Euler.y;
                mViewVelocityY = 0f;
            }

            // Reset our internal rotation targets
            if (Mathf.Abs(_EulerTarget.x - _Euler.x) < EPSILON)
            {
                _Euler.x = LocalPitch;
                _EulerTarget.x = _Euler.x;
                mViewVelocityX = 0f;
            }

            base.PostRigLateUpdate();
        }

        /// <summary>
        /// Provides framerate independent smoothing
        /// </summary>
        /// <param name="rSource">Cureent value</param>
        /// <param name="rTarget">TargetValue</param>
        /// <param name="rSmoothing">Smoothing rate dictates the proportion of source remaining after one second</param>
        /// <param name="rDeltaTime">Delta time</param>
        /// <returns></returns>
        public float SmoothDamp(float rSource, float rTarget, float rSmoothing, float rDeltaTime)
        {
            return Mathf.Lerp(rSource, rTarget, 1 - Mathf.Pow(rSmoothing, rDeltaTime));
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

            lIsDirty = base.OnInspectorGUI();

            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();

            if (EditorHelper.BoolField(IsYawEnabled, "Yaw Enabled", RigController, 16f))
            {
                lIsDirty = true;
                IsYawEnabled = EditorHelper.FieldBoolValue;
            }

            EditorGUILayout.LabelField(new GUIContent("Yaw", "Manages the rotation around the character's up axis."));

            EditorGUILayout.EndHorizontal();

            if (IsYawEnabled)
            {
                if (EditorHelper.FloatField("Speed", "Degrees per second to rotate.", YawSpeed, RigController))
                {
                    lIsDirty = true;
                    YawSpeed = EditorHelper.FieldFloatValue;
                }

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(new GUIContent("Range", "Angular range (min & max) for the rotation from -180 to 180"), GUILayout.Width(EditorGUIUtility.labelWidth - (EditorGUIUtility.currentViewWidth < 292 ? 18f : 4f)));

                if (EditorHelper.FloatField(MinYaw, "Min Yaw", RigController))
                {
                    lIsDirty = true;
                    MinYaw = EditorHelper.FieldFloatValue;
                }

                if (EditorHelper.FloatField(MaxYaw, "Max Yaw", RigController))
                {
                    lIsDirty = true;
                    MaxYaw = EditorHelper.FieldFloatValue;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();

            if (EditorHelper.BoolField(IsPitchEnabled, "Pitch Enabled", RigController, 16f))
            {
                lIsDirty = true;
                IsPitchEnabled = EditorHelper.FieldBoolValue;
            }

            EditorGUILayout.LabelField(new GUIContent("Pitch", "Manages the rotation around the character's up axis."));

            EditorGUILayout.EndHorizontal();

            if (IsPitchEnabled)
            {
                if (EditorHelper.BoolField("Invert", "Determines if we invert the pitch", InvertPitch, RigController))
                {
                    lIsDirty = true;
                    InvertPitch = EditorHelper.FieldBoolValue;
                }

                if (EditorHelper.FloatField("Speed", "Degrees per second to rotate.", PitchSpeed, RigController))
                {
                    lIsDirty = true;
                    PitchSpeed = EditorHelper.FieldFloatValue;
                }

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(new GUIContent("Range", "Angular range (min & max) for the rotation from -180 to 180"), GUILayout.Width(EditorGUIUtility.labelWidth - (EditorGUIUtility.currentViewWidth < 292 ? 18f : 4f)));

                if (EditorHelper.FloatField(MinPitch, "Min Pitch", RigController))
                {
                    lIsDirty = true;
                    MinPitch = EditorHelper.FieldFloatValue;
                }

                if (EditorHelper.FloatField(MaxPitch, "Max Pitch", RigController))
                {
                    lIsDirty = true;
                    MaxPitch = EditorHelper.FieldFloatValue;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            if (EditorHelper.FloatField("Smoothing", "Smoothing factor applied to the rotations. Use 0 to have no smoothing and 1 for lots of smoothing. 0.05 to 0.2 is usually good.", Smoothing, RigController))
            {
                lIsDirty = true;
                Smoothing = EditorHelper.FieldFloatValue;
            }

            return lIsDirty;
        }

        /// <summary>
        /// Allow the motor to render it's own GUI debug GUI
        /// </summary>
        public override bool OnDebugInspectorGUI()
        {
            bool lIsDirty = false;

            EditorGUILayout.LabelField("Frame Euler", StringHelper.ToSimpleString(mFrameEuler));

            return lIsDirty;
        }

#endif
    }
}
