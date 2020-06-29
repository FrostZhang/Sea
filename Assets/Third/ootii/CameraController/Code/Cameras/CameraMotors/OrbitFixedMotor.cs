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
    [BaseName("3rd Person Fixed")]
    [BaseDescription("Motor that allows the rig to orbit the anchor and anchor offset. This rig follows the anchor as if attached by a hard pole.")]
    public class OrbitFixedMotor : YawPitchMotor
    {
        /// <summary>
        /// Anchor that we can use instead of the default
        /// </summary>
        public override Transform Anchor
        {
            get { return base.Anchor; }

            set
            {
                base.Anchor = value;

                if (Anchor != null)
                {
                    mCharacterController = InterfaceHelper.GetComponent<ICharacterController>(Anchor.gameObject);
                }
            }
        }

        /// <summary>
        /// Actual distance from the anchor's position
        /// </summary>
        public override float Distance
        {
            get { return mDistance; }
            set
            {
                mDistance = value;
                _MaxDistance = value;
            }
        }

        /// <summary>
        /// Determines if the camera rotates with the anchor
        /// </summary>
        public bool _RotateWithAnchor = false;
        public bool RotateWithAnchor
        {
            get { return _RotateWithAnchor; }
            set { _RotateWithAnchor = value; }
        }

        /// <summary>
        /// Determines if we'll rotate the character
        /// </summary>
        public bool _RotateAnchor = false;
        public bool RotateAnchor
        {
            get { return _RotateAnchor; }
            set { _RotateAnchor = value; }
        }

        /// <summary>
        /// Action alias that causes us to rotate the anchor
        /// </summary>
        public string _RotateAnchorAlias = "Camera Rotate Character";
        public string RotateAnchorAlias
        {
            get { return _RotateAnchorAlias; }
            set { _RotateAnchorAlias = value; }
        }

        /// <summary>
        /// Character controller that may be being used
        /// </summary>
        protected ICharacterController mCharacterController = null;

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
            Transform lAnchorTransform = Anchor;
            if (lAnchorTransform == null) { return mRigTransform; }

            if (RigController == null) { return mRigTransform; }
            //Quaternion lTilt = RigController.Tilt;
            Transform lCameraTransform = RigController._Transform;

            // Determine how much the anchor's yaw changed (we want to stay relative to the anchor direction)
            float lAnchorRootYawDelta = Vector3Ext.HorizontalAngleTo(mAnchorLastRotation.Forward(), lAnchorTransform.forward, lAnchorTransform.up);
            float lAnchorYaw = (Mathf.Abs(rTiltAngle) >= 2f ? lAnchorRootYawDelta : 0f);

            if (!RigController.RotateAnchorOffset)
            {
                lAnchorRootYawDelta = 0f;
                lAnchorYaw = 0f;
            }

            // Grab any euler changes this frame
            mFrameEuler = GetFrameEuler(true, true); // RigController.RotateAnchorOffset);

            // Get our predicted based on the frame and anchor changes. We want this to get the right-vector
            Quaternion lNewCameraRotation = Quaternion.AngleAxis(mFrameEuler.y + lAnchorYaw, (RigController.RotateAnchorOffset ? RigController.Tilt.Up() : Vector3.up)) * lCameraTransform.rotation;

            // Now grab the full (vertical + horizontal) position of our final focus.
            //Vector3 lNewFocusPosition = lAnchorTransform.position + (lAnchorTransform.up * lAnchorOffset.y);
            //lNewFocusPosition = lNewFocusPosition + (lNewCameraRotation.Right() * lAnchorOffset.x);
            Vector3 lNewFocusPosition = GetFocusPosition(lNewCameraRotation);

            // Default the value
            Vector3 lNewCameraPosition = lCameraTransform.position;
            Vector3 lToFocusPosition = lCameraTransform.forward;

            // If we're tilting, act like a fixed
            if (_RotateWithAnchor || RigController.FrameForceToFollowAnchor || Mathf.Abs(rTiltAngle) >= 2f)
            {
                // Get the local position and the right vector of the camera relative to the last frame
                Matrix4x4 lOldFocusMatrix = Matrix4x4.TRS(mFocusLastPosition, (RigController.RotateAnchorOffset ? mAnchorLastRotation : Quaternion.identity), Vector3.one);
                Matrix4x4 lNewFocusMatrix = Matrix4x4.TRS(lNewFocusPosition, (RigController.RotateAnchorOffset ? lAnchorTransform.rotation : Quaternion.identity), Vector3.one);

                // The matrix will add our anchor delta. But, we also added it when we use the LocalYaw
                // We'll remove it so we don't double up.
                if (mTargetYaw < float.MaxValue)
                {
                    mFrameEuler.y = mFrameEuler.y - lAnchorRootYawDelta;
                }

                // If nothing has changed, we won't update. This is important due to the fact
                // that the inverse of the matix causes a small floating point error to move us.
                if (mFrameEuler.sqrMagnitude != 0f || lOldFocusMatrix != lNewFocusMatrix)
                {
                    Vector3 lLocalPosition = lOldFocusMatrix.inverse.MultiplyPoint(lCameraTransform.position);
                    Vector3 lLocalCameraRight = lOldFocusMatrix.inverse.MultiplyVector(lCameraTransform.right);

                    // Rotate the old local position based on the frame's rotation changes
                    Quaternion lLocalRotation = Quaternion.AngleAxis(mFrameEuler.y, Vector3.up) * Quaternion.AngleAxis(mFrameEuler.x, lLocalCameraRight);
                    lLocalPosition = lLocalRotation * lLocalPosition;

                    // Grab the new position based on the updated matrix
                    lNewCameraPosition = lNewFocusMatrix.MultiplyPoint(lLocalPosition);

                    // The problem is that we may not be allowing the camera to pull back to the desired distance.
                    // So, apply the distance to the direction and that's our new position.
                    Vector3 lDirection = (lNewCameraPosition - lNewFocusPosition).normalized;
                    lNewCameraPosition = lNewFocusPosition + (lDirection * mDistance);
                }

                // Ensure our camera isn't inbetween the focus and the anchor
                float lNewCameraDistance = Vector3.Distance(AnchorPosition, lNewCameraPosition);
                float lNewFocusDistance = Vector3.Distance(AnchorPosition, lNewFocusPosition);
                if (lNewCameraDistance < lNewFocusDistance)
                {
                    lNewCameraPosition = lNewFocusPosition - (lNewCameraRotation.Forward() * mDistance);
                }
            }
            else
            {
                lNewCameraRotation = lNewCameraRotation * Quaternion.AngleAxis(mFrameEuler.x, Vector3.right);
                lNewCameraPosition = lNewFocusPosition - (lNewCameraRotation.Forward() * mDistance);
            }

            // Determine our rotation
            lToFocusPosition = lNewFocusPosition - lNewCameraPosition;
            if (lToFocusPosition.sqrMagnitude < 0.0001f) { lToFocusPosition = lCameraTransform.forward; }

            // Ensure we have the right rotation
            lNewCameraRotation = Quaternion.LookRotation(lToFocusPosition.normalized, (RigController.RotateAnchorOffset ? lAnchorTransform.up : Vector3.up));

            // Ensure we're the correct distance from the focus point
            if (lToFocusPosition.magnitude != mDistance)
            {
                lNewCameraPosition = lNewFocusPosition - (lToFocusPosition.normalized * mDistance);
            }

            // Return the results
            mRigTransform.Position = lNewCameraPosition;
            mRigTransform.Rotation = lNewCameraRotation;
            return mRigTransform;
        }

        /// <summary>
        /// Allows the motor to process after the camera and controller have completed
        /// </summary>
        public override void PostRigLateUpdate()
        {
            base.PostRigLateUpdate();

            // Determine if we rotate the anchor to match our rotation
            if (_RotateAnchor && Anchor != null)
            {
                if (_RotateAnchorAlias.Length == 0 || RigController.InputSource == null || RigController.InputSource.IsPressed(_RotateAnchorAlias))
                {
                    if (mCharacterController != null)
                    {
                        float lToCameraAngle = Vector3Ext.HorizontalAngleTo(Anchor.forward, RigController.Transform.forward, Anchor.up);

                        Quaternion lRotation = Quaternion.AngleAxis(lToCameraAngle, Vector3.up);
                        mCharacterController.Yaw = mCharacterController.Yaw * lRotation;
                        Anchor.rotation = mCharacterController.Tilt * mCharacterController.Yaw;
                    }
                    else
                    {
                        float lToCameraAngle = Vector3Ext.HorizontalAngleTo(Anchor.forward, RigController.Transform.forward, Anchor.up);

                        Quaternion lRotation = Quaternion.AngleAxis(lToCameraAngle, Vector3.up);
                        Anchor.rotation = Anchor.rotation * lRotation;
                    }
                }
            }
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

            if (base.OnInspectorGUI())
            {
                lIsDirty = true;
            }

            GUILayout.Space(5f);

            if (EditorHelper.FloatField("Distance", "Distance from the anchor that we orbit", MaxDistance, RigController))
            {
                lIsDirty = true;
                MaxDistance = EditorHelper.FieldFloatValue;

                if (Application.isPlaying) { mDistance = MaxDistance; }
            }

            GUILayout.Space(5f);

            if (EditorHelper.BoolField("Rotate With Anchor", "Determines we'll rotate the camera as the anchor rotates.", RotateWithAnchor, RigController))
            {
                lIsDirty = true;
                RotateWithAnchor = EditorHelper.FieldBoolValue;

                if (RotateWithAnchor) { RotateAnchor = false; }
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

                if (RotateAnchor) { RotateWithAnchor = false; }
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
