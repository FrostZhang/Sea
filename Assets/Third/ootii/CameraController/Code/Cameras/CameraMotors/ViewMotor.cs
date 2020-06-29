using System;
using UnityEngine;
using com.ootii.Actors;
using com.ootii.Base;
using com.ootii.Geometry;
using com.ootii.Helpers;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Cameras
{
    /// <summary>
    /// Orbit motor
    /// </summary>
    [BaseName("1st Person View")]
    [BaseDescription("Motor that allows the rig to rotate for 1st person views.")]
    public class ViewMotor : YawPitchMotor
    {
        ///// <summary>
        ///// Anchor that we can use instead of the default
        ///// </summary>
        //public override Transform Anchor
        //{
        //    get
        //    {
        //        // Return the rig anchor always... the motor anchor is just additive
        //        return RigController.Anchor;
        //    }

        //    set
        //    {
        //        base.Anchor = value;

        //        if (Anchor != null)
        //        {
        //            mCharacterController = InterfaceHelper.GetComponent<ICharacterController>(Anchor.gameObject);
        //        }
        //    }
        //}

        ///// <summary>
        ///// Anchor offset we can use instead of the default
        ///// </summary>
        //public override Vector3 AnchorOffset
        //{
        //    get
        //    {
        //        // Return the rig anchor offset always... the motor anchor offset is just additive
        //        return RigController.AnchorOffset;
        //    }

        //    set
        //    {
        //        base.AnchorOffset = value;
        //    }
        //}

        ///// <summary>
        ///// Current position of the actual anchor
        ///// </summary>
        //public override Vector3 AnchorPosition
        //{
        //    get
        //    {
        //        // Return the rig anchor position always... the motor anchor position is just additive
        //        return RigController.AnchorPosition;
        //    }
        //}

        /// <summary>
        /// Determines if we'll rotate the character
        /// </summary>
        public bool _RotateAnchor = true;
        public bool RotateAnchor
        {
            get { return _RotateAnchor; }
            set { _RotateAnchor = value; }
        }

        /// <summary>
        /// Action alias that causes us to rotate the anchor
        /// </summary>
        public string _RotateAnchorAlias = "";
        public string RotateAnchorAlias
        {
            get { return _RotateAnchorAlias; }
            set { _RotateAnchorAlias = value; }
        }

        /// <summary>
        /// If the actor is matching our rotation, we'll need to process slightly
        /// different to match the rotations.
        /// </summary>
        public bool _IsActorMatchingRotation = false;
        public bool IsActorMatchingRotation
        {
            get { return _IsActorMatchingRotation; }
            set { _IsActorMatchingRotation = value; }
        }

        /// <summary>
        /// Character controller that may be being used
        /// </summary>
        protected ICharacterController mCharacterController = null;

        /// <summary>
        /// Determines if we were rotating the anchor
        /// </summary>
        protected bool mWasRotatingAnchor = false;

        /// <summary>
        /// Default constructor
        /// </summary>
        public ViewMotor() : base()
        {
            _MaxDistance = 0f;
            mDistance = 0f;
        }

        /// <summary>
        /// Called when the motor is instantiated
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            if (Anchor != null)
            {
                mCharacterController = InterfaceHelper.GetComponent<ICharacterController>(Anchor.gameObject);
            }

            if (Application.isPlaying && Anchor == null)
            {
                mRigTransform.Position = RigController._Transform.position;
                mRigTransform.Rotation = RigController._Transform.rotation;
            }
        }

        /// <summary>
        /// Clears all values and references stored with the motor
        /// </summary>
        public override void Clear()
        {
            mCharacterController = null;
            base.Clear();
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
            if (RigController == null) { return mRigTransform; }
            if (RigController.Anchor == null) { return mRigTransform; }

            Quaternion lTilt = RigController.Tilt;
            Transform lAnchorTransform = Anchor;
            Transform lCameraTransform = RigController._Transform;

            if (_RotateAnchor && lAnchorTransform != null)
            {
                if (_RotateAnchorAlias.Length == 0 || RigController.InputSource == null || RigController.InputSource.IsPressed(_RotateAnchorAlias))
                {
                    mWasRotatingAnchor = true;
                }
            }

            // Determine how much the anchor's yaw changed (we want to stay relative to the anchor direction)
            //float lAnchorYaw = 0f;
            //float lAnchorRootYawDelta = 0f;

            //if (!_RotateAnchor)
            //{
            //    lAnchorRootYawDelta = Vector3Ext.HorizontalAngleTo(mAnchorLastRotation.Forward(), lAnchorTransform.forward, lAnchorTransform.up);
            //    lAnchorYaw = (Mathf.Abs(rTiltAngle) >= 2f ? lAnchorRootYawDelta - mFrameEuler.y : 0f);
            //}

            // Grab any euler changes this frame
            mFrameEuler = GetFrameEuler(!mWasRotatingAnchor);

            // Determine the position and rotation
            Vector3 lUp = lTilt.Up();
            Vector3 lRight = lCameraTransform.right;

            // Set the position
            Vector3 lNewCameraPosition = lAnchorTransform.position + (lAnchorTransform.rotation * AnchorOffset) + (lAnchorTransform.rotation * _Offset);

            // Get the local position and the right vector of the camera relative to the last frame
            Matrix4x4 lOldMatrix = Matrix4x4.TRS(mAnchorLastPosition, mAnchorLastRotation, Vector3.one);
            Matrix4x4 lNewMatrix = Matrix4x4.TRS(lAnchorTransform.position, lAnchorTransform.rotation, Vector3.one);

            // Set the rotation
            Vector3 lLocalForward = lOldMatrix.inverse.MultiplyVector(lCameraTransform.forward);
            Vector3 lForward = Quaternion.AngleAxis(mFrameEuler.x, lRight) * Quaternion.AngleAxis(mFrameEuler.y, lUp) * lNewMatrix.MultiplyVector(lLocalForward);

            Quaternion lNewCameraRotation = Quaternion.LookRotation(lForward, lUp);

            // We use the motor anchor to modify our position, not rotation.
            Vector3 lOffset = Vector3.zero;
            if (!_UseRigAnchor && _Anchor != null)
            {
                Vector3 lNewAnchorPosition = (_Anchor.position + (_Anchor.rotation * _AnchorOffset));
                lOffset = lNewAnchorPosition - AnchorPosition;
            }

            // Return the results
            mRigTransform.Position = lNewCameraPosition + lOffset;
            mRigTransform.Rotation = lNewCameraRotation;
            return mRigTransform;
        }

        /// <summary>
        /// Allows the motor to process after the camera and controller have completed
        /// </summary>
        public override void PostRigLateUpdate()
        {
            base.PostRigLateUpdate();

            Transform lAnchorTransform = Anchor;

            // If the anchor updates its rotation based on the rig, we need to get the final position of the rig
            if (_IsActorMatchingRotation && (RigController.ActiveMotor == this))
            {
                RigController._Transform.position = lAnchorTransform.position + (lAnchorTransform.rotation * AnchorOffset) + (lAnchorTransform.rotation * _Offset);
                RigController._Transform.rotation = Quaternion.Euler(RigController._Transform.rotation.eulerAngles.x, lAnchorTransform.rotation.eulerAngles.y, 0f);
            }

            if (_RotateAnchor && lAnchorTransform != null)
            {
                bool lIsRotatingAnchor = _RotateAnchorAlias.Length == 0 || RigController.InputSource == null || RigController.InputSource.IsPressed(_RotateAnchorAlias);

                if (lIsRotatingAnchor || mWasRotatingAnchor)
                {
                    if (mCharacterController != null)
                    {
                        float lToCameraAngle = Vector3Ext.HorizontalAngleTo(lAnchorTransform.forward, RigController.Transform.forward, lAnchorTransform.up);

                        Quaternion lRotation = Quaternion.AngleAxis(lToCameraAngle, Vector3.up);
                        mCharacterController.Yaw = mCharacterController.Yaw * lRotation;
                        lAnchorTransform.rotation = mCharacterController.Tilt * mCharacterController.Yaw;
                    }
                    else
                    {
                        float lToCameraAngle = Vector3Ext.HorizontalAngleTo(lAnchorTransform.forward, RigController.Transform.forward, lAnchorTransform.up);

                        Quaternion lRotation = Quaternion.AngleAxis(lToCameraAngle, Vector3.up);
                        lAnchorTransform.rotation = lAnchorTransform.rotation * lRotation;
                    }

                    mAnchorLastRotation = lAnchorTransform.rotation;

                    // Since we are no longer rotating, we need to clear our out values
                    if (!lIsRotatingAnchor)
                    {
                        _Euler.y = LocalYaw;
                        _Euler.x = LocalPitch;
                        _EulerTarget = _Euler;

                        mViewVelocityY = 0f;
                        mViewVelocityX = 0f;

                        mWasRotatingAnchor = false;
                    }
                }
            }
            else
            {
                mWasRotatingAnchor = false;
            }

            //Debug.Log("Char Yaw:" + RigController.Anchor.rotation.eulerAngles.y.ToString("f5") + " Anchor Yaw:" + Anchor.rotation.eulerAngles.y.ToString("f5") + " Cam Yaw:" + RigController._Transform.eulerAngles.y.ToString("f5"));
        }

        /// <summary>
        /// Gieven a JSON string that is the definition of the object, we parse
        /// out the properties and set them.
        /// </summary>
        /// <param name="rDefinition">JSON string</param>
        public override void DeserializeMotor(string rDefinition)
        {
            base.DeserializeMotor(rDefinition);

            if (Anchor != null)
            {
                mCharacterController = InterfaceHelper.GetComponent<ICharacterController>(Anchor.gameObject);
            }
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

            GUILayout.Space(5f);

            if (EditorHelper.BoolField("Anchor Rotates", "Indicates that the anchor rotates on it's own and the camera should rotate with it.", IsActorMatchingRotation, RigController))
            {
                lIsDirty = true;
                IsActorMatchingRotation = EditorHelper.FieldBoolValue;
            }

            if (RotateAnchor)
            {
                GUILayout.Space(5f);
                EditorHelper.DrawInspectorDescription("Rotate Anchor should not be set with the Motion Controller as individual motions include a 'Rotate With Camera' option. This prevents the camera from rotating the character when it shouldn't (ie during climbs).", MessageType.None);
            }

            if (EditorHelper.BoolField("Rotate Anchor", "Determines we'll rotate the anchor to face the direction of the camera.", RotateAnchor, RigController))
            {
                lIsDirty = true;
                RotateAnchor = EditorHelper.FieldBoolValue;
            }

            if (RotateAnchor)
            {
                if (EditorHelper.TextField("Rotate Anchor Alias", "Action alias we can use to trigger the character rotation or leave blank to always rotate.", RotateAnchorAlias, RigController))
                {
                    lIsDirty = true;
                    RotateAnchorAlias = EditorHelper.FieldStringValue;
                }
            }

            return lIsDirty;
        }

#endif

    }
}
