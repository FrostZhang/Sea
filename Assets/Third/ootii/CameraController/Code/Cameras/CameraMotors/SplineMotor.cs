using System;
using UnityEngine;
using com.ootii.Base;
using com.ootii.Helpers;
using com.ootii.Geometry;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Cameras
{
    /// <summary>
    /// Motor that stays at the fixed offset from the anchor
    /// </summary>
    [BaseName("Spline Motor")]
    [BaseDescription("Rig Motor will follow along a spline and look at a target or forward.")]
    public class SplineMotor : CameraMotor
    {
        /// <summary>
        /// Determines if we use the rig's anchor
        /// </summary>
        public bool _UseAnchor = true;
        public bool UseAnchor
        {
            get { return _UseAnchor; }
            set { _UseAnchor = value; }
        }

        /// <summary>
        /// INTERNAL ONLY: Index into the Camera Rig Controller's stored gameobjects
        /// </summary>
        public int _PathOwnerIndex = -1;
        public virtual int PathOwnerIndex
        {
            get { return _PathOwnerIndex; }

            set
            {
                _PathOwnerIndex = value;

                if (_PathOwnerIndex >= 0)
                {
                    if (RigController != null && _PathOwnerIndex < RigController.StoredTransforms.Count)
                    {
                        _PathOwner = RigController.StoredTransforms[_PathOwnerIndex];
                    }
                }
            }
        }

        /// <summary>
        /// Contains the spline we're going to follow
        /// </summary>
        [NonSerialized]
        public Transform _PathOwner = null;
        public virtual Transform PathOwner
        {
            get { return _PathOwner; }

            set
            {
                _PathOwner = value;

                if (RigController != null)
                {
                    if (_PathOwner == null)
                    {
                        if (_PathOwnerIndex >= 0 && _PathOwnerIndex < RigController.StoredTransforms.Count)
                        {
                            RigController.StoredTransforms[_PathOwnerIndex] = null;

                            if (_PathOwnerIndex == RigController.StoredTransforms.Count - 1)
                            {
                                RigController.StoredTransforms.RemoveAt(_PathOwnerIndex);
                                _PathOwnerIndex = -1;
                            }
                        }

                        mPath = null;
                        mPathLength = 0f;
                    }
                    else
                    {
                        if (_PathOwnerIndex == -1)
                        {
                            _PathOwnerIndex = RigController.StoredTransforms.Count;
                            RigController.StoredTransforms.Add(null);
                        }

                        RigController.StoredTransforms[_PathOwnerIndex] = _PathOwner;

                        mPath = _PathOwner.gameObject.GetComponent<BezierSpline>();
                        mPathLength = (mPath != null ? mPath.Length : 0f);
                    }
                }
            }
        }

        /// <summary>
        /// Length of the path
        /// </summary>
        private float mPathLength = 0f;
        public float PathLength
        {
            get { return mPathLength; }
        }

        /// <summary>
        /// Determines how strong the shake is over the duration
        /// </summary>
        public AnimationCurve _Speed = AnimationCurve.Linear(0f, 5f, 1f, 5f);
        public AnimationCurve Speed
        {
            get { return _Speed; }
            set { _Speed = value; }
        }

        /// <summary>
        /// Determines if the motor loops when it hits the end of the spline
        /// </summary>
        public bool _Loop = false;
        public bool Loop
        {
            get { return _Loop; }
            set { _Loop = value; }
        }

        /// <summary>
        /// Determines if we face the movement direction
        /// </summary>
        public bool _RotateToMovementDirection = false;
        public bool RotateToMovementDirection
        {
            get { return _RotateToMovementDirection; }
            set { _RotateToMovementDirection = value; }
        }

        /// <summary>
        /// Determines if we face the anchor
        /// </summary>
        public bool _RotateToAnchor = false;
        public bool RotateToAnchor
        {
            get { return _RotateToAnchor; }
            set { _RotateToAnchor = value; }
        }

        /// <summary>
        /// Determines if we activate another motor when we're done
        /// </summary>
        public bool _ActivateMotorOnComplete = false;
        public bool ActivateMotorOnComplete
        {
            get { return _ActivateMotorOnComplete; }
            set { _ActivateMotorOnComplete = value; }
        }

        /// <summary>
        /// Motor index we'll activate when done
        /// </summary>
        public int _EndMotorIndex = 0;
        public int EndMotorIndex
        {
            get { return _EndMotorIndex; }
            set { _EndMotorIndex = value; }
        }

        /// <summary>
        /// How much of the path we've travelled
        /// </summary>
        public float mDistanceTravelled = 0f;
        public float DistanceTravelled
        {
            get { return mDistanceTravelled; }
            set { mDistanceTravelled = value; }
        }

        /// <summary>
        /// How much of the path we've travelled (0 to 1)
        /// </summary>
        public float DistanceTravelledNormalized
        {
            get
            {
                if (mPathLength == 0f) { return 0f; }
                return mDistanceTravelled / mPathLength;
            }

            set
            {
                mDistanceTravelled = mPathLength * value;
            }
        }

        /// <summary>
        /// Determines if the motor auto starts when activated
        /// </summary>
        public bool mAutoStart = true;
        public bool AutoStart
        {
            get { return mAutoStart; }
            set { mAutoStart = value; }
        }

        /// <summary>
        /// Determines if the motor is running
        /// </summary>
        private bool mHasStarted = false;

        /// <summary>
        /// Determines if the motor is currently paused
        /// </summary>
        private bool mIsPaused = false;
        public bool IsPaused
        {
            get { return mIsPaused; }
            set { mIsPaused = value; }
        }

        /// <summary>
        /// Track the last position to help with rotation direciton
        /// </summary>
        private Vector3 mLastPosition = Vector3.zero;

        /// <summary>
        /// Spline that we'll follow
        /// </summary>
        protected BezierSpline mPath = null;

        /// <summary>
        /// Called when the motor is instantiated
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            if (PathOwner != null)
            {
                mPath = PathOwner.gameObject.GetComponent<BezierSpline>();
                mPathLength = (mPath != null ? mPath.Length : 0f);
            }

            if (Application.isPlaying && Anchor == null)
            {
                mRigTransform.Position = RigController._Transform.position;
                mRigTransform.Rotation = RigController._Transform.rotation;
            }
        }

        /// <summary>
        /// Starts movement on the motor as the specified point
        /// </summary>
        /// <param name="rNormalizedDistance">Distance from 0 to 1 where 1 represents the path length</param>
        public void Start(float rNormalizedDistance = 0f)
        {
            mHasStarted = true;
            mIsPaused = false;

            if (mPath != null)
            {
                mDistanceTravelled = rNormalizedDistance * mPathLength;

                Vector3 lPosition = mPath.GetPoint(rNormalizedDistance);
                if (mLastPosition == Vector3.zero) { mLastPosition = lPosition; }

                mRigTransform.Position = lPosition;
            }
        }

        /// <summary>
        /// Stops movement at the current point
        /// </summary>
        public void Stop()
        {
            mHasStarted = false;
        }

        /// <summary>
        /// Tells the motor to activated
        /// </summary>
        /// <param name="rOldMotor"></param>
        public override void Activate(CameraMotor rOldMotor)
        {
            base.Activate(rOldMotor);

            if (mAutoStart)
            {
                Start();
            }
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
            if (mPath == null) { return mRigTransform; }
            if (RigController == null) { return mRigTransform; }

            bool lHasArrived = false;

            if (mHasStarted && !mIsPaused)
            {
                float lSpeed = _Speed.Evaluate(DistanceTravelledNormalized);
                mDistanceTravelled += lSpeed * rDeltaTime;
            }

            if (mDistanceTravelled >= mPathLength)
            {
                if (_Loop)
                {
                    mDistanceTravelled -= mPathLength;
                }
                else
                {
                    mDistanceTravelled = mPathLength;
                }

                lHasArrived = true;
            }

            float lPercent = mDistanceTravelled / mPathLength;

            Vector3 lPosition = mPath.GetPoint(lPercent);
            if (mLastPosition == Vector3.zero) { mLastPosition = lPosition; }

            mRigTransform.Position = lPosition;

            if (_RotateToAnchor)
            {
                mRigTransform.Rotation = Quaternion.LookRotation(AnchorPosition - lPosition, Vector3.up);
            }
            else if (_RotateToMovementDirection)
            {
                if ((lPosition - mLastPosition).sqrMagnitude != 0f)
                {
                    mRigTransform.Rotation = Quaternion.LookRotation(lPosition - mLastPosition, Vector3.up);
                }
            }

            mLastPosition = lPosition;

            if (lHasArrived && mHasStarted)
            {
                if (_ActivateMotorOnComplete)
                {
                    if (_EndMotorIndex >= 0 && _EndMotorIndex < RigController.Motors.Count)
                    {
                        RigController.ActivateMotor(_EndMotorIndex);
                    }
                }

                if (RigController.MotorArrived != null) { RigController.MotorArrived(this); }

                if (!_Loop) { mHasStarted = false; }
            }

            return mRigTransform;
        }

        /// <summary>
        /// Gieven a JSON string that is the definition of the object, we parse
        /// out the properties and set them.
        /// </summary>
        /// <param name="rDefinition">JSON string</param>
        public override void DeserializeMotor(string rDefinition)
        {
            base.DeserializeMotor(rDefinition);

            if (_PathOwnerIndex >= 0)
            {
                if (_PathOwnerIndex < RigController.StoredTransforms.Count)
                {
                    _PathOwner = RigController.StoredTransforms[_PathOwnerIndex];
                }
                else
                {
                    _PathOwner = null;
                    _PathOwnerIndex = -1;
                }
            }

            if (_PathOwner != null)
            {
                mPath = _PathOwner.gameObject.GetComponent<BezierSpline>();
                mPathLength = (mPath != null ? mPath.Length : 0f);
            }

            _IsCollisionEnabled = false;
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

            if (EditorHelper.ObjectField<Transform>("Path", "Path that the camera will follow.", PathOwner, RigController))
            {
                lIsDirty = true;
                PathOwner = EditorHelper.FieldObjectValue as Transform;
            }

            if (EditorHelper.BoolField("Auto Start", "Determines if start the path as soon as the motor activates.", AutoStart, RigController))
            {
                lIsDirty = true;
                AutoStart = EditorHelper.FieldBoolValue;
            }

            // Determine how gravity is applied
            EditorGUI.BeginChangeCheck();
            AnimationCurve lNewSpeed = EditorGUILayout.CurveField(new GUIContent("Speed", "Determines the speed over the duration of the path."), Speed);
            if (EditorGUI.EndChangeCheck())
            {
                if (RigController != null) { Undo.RecordObject(RigController, "Set Speed"); }

                lIsDirty = true;
                Speed = lNewSpeed;
            }

            GUILayout.Space(5f);

            if (EditorHelper.BoolField("Look To Anchor", "Rotates us to always look at the anchor.", RotateToAnchor, RigController))
            {
                lIsDirty = true;
                RotateToAnchor = EditorHelper.FieldBoolValue;

                if (RotateToAnchor) { RotateToMovementDirection = !RotateToAnchor; }
            }

            if (RotateToAnchor)
            {
                if (EditorHelper.BoolField("Use Rig Anchor", "Determines if we use the controller's default anchor our a custom one", UseRigAnchor, RigController))
                {
                    lIsDirty = true;
                    UseRigAnchor = EditorHelper.FieldBoolValue;
                }

                if (!UseRigAnchor)
                {
                    if (EditorHelper.ObjectField<Transform>("Anchor", "Anchor to use instead of the controller's default value", _Anchor, RigController))
                    {
                        lIsDirty = true;
                        Anchor = EditorHelper.FieldObjectValue as Transform;
                    }

                    if (EditorHelper.Vector3Field("Anchor Offset", "Anchor Offset to use instead of the controller's default value", _AnchorOffset, RigController))
                    {
                        lIsDirty = true;
                        AnchorOffset = EditorHelper.FieldVector3Value;
                    }
                }

                GUILayout.Space(5f);
            }

            if (EditorHelper.BoolField("Look To Path", "Rotates us to always look in the direction we're moving.", RotateToMovementDirection, RigController))
            {
                lIsDirty = true;
                RotateToMovementDirection = EditorHelper.FieldBoolValue;

                if (RotateToMovementDirection) { RotateToAnchor = !RotateToMovementDirection; }
            }

            GUILayout.Space(5f);

            if (EditorHelper.BoolField("Loop", "Determines if loop once we reach the end.", Loop, RigController))
            {
                lIsDirty = true;
                Loop = EditorHelper.FieldBoolValue;
            }

            EditorGUILayout.BeginHorizontal();

            if (EditorHelper.BoolField("Activate End Motor", "Determines if activate the specified motor when we reach the end.", ActivateMotorOnComplete, RigController))
            {
                lIsDirty = true;
                ActivateMotorOnComplete = EditorHelper.FieldBoolValue;
            }

            if (ActivateMotorOnComplete)
            {
                EditorHelper.LabelField("Index", "Index of the motor to activate.", 43f);
                if (EditorHelper.IntField(EndMotorIndex, "Motor Index", RigController, 35f))
                {
                    lIsDirty = true;
                    EndMotorIndex = EditorHelper.FieldIntValue;
                }

                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.EndHorizontal();

            return lIsDirty;
        }

#endif

    }
}
