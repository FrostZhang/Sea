using System;
using UnityEngine;
using com.ootii.Base;
using com.ootii.Helpers;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Cameras
{
    /// <summary>
    /// Simple motor used to always look at a specific object
    /// </summary>
    [BaseName("Free Flight")]
    [BaseDescription("Editor style motor that allows the player to move the camera around the scene without an anchor.")]
    public class FreeFlightMotor : YawPitchMotor
    {
        public float _Speed = 5f;
        public float Speed
        {
            get { return _Speed; }
            set { _Speed = value; }
        }

        public float _VerticalSpeed = 5f;
        public float VerticalSpeed
        {
            get { return _VerticalSpeed; }
            set { _VerticalSpeed = value; }
        }

        public string _UpActionAlias = "";
        public string UpActionAlias
        {
            get { return _UpActionAlias; }
            set { _UpActionAlias = value; }
        }

        public string _DownActionAlias = "";
        public string DownActionAlias
        {
            get { return _DownActionAlias; }
            set { _DownActionAlias = value; }
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
            if (RigController.InputSource == null) { return mRigTransform; }

            Transform lCameraTransform = RigController._Transform;

            // Grab any euler changes this frame
            mFrameEuler = GetFrameEuler(false, true);

            // Determine the new rotation
            Quaternion lNewRotation = Quaternion.AngleAxis(mFrameEuler.y, Vector3.up) * lCameraTransform.rotation;
            lNewRotation = lNewRotation * Quaternion.AngleAxis(mFrameEuler.x, Vector3.right);

            // Determine the new movement
            Vector3 lMovement = new Vector3(RigController.InputSource.MovementX, 0f, RigController.InputSource.MovementY);
            Vector3 lNewPosition = lCameraTransform.position + (lNewRotation * (lMovement * _Speed * rDeltaTime));

            // Finally, handle any verticla movement
            if (_UpActionAlias.Length > 0 && RigController.InputSource.IsPressed(_UpActionAlias))
            {
                lNewPosition = lNewPosition + (Vector3.up * (_VerticalSpeed * rDeltaTime));
            }

            if (_DownActionAlias.Length > 0 && RigController.InputSource.IsPressed(_DownActionAlias))
            {
                lNewPosition = lNewPosition + (Vector3.down * (_VerticalSpeed * rDeltaTime));
            }

            // Return the results
            mRigTransform.Position = lNewPosition;
            mRigTransform.Rotation = lNewRotation;
            return mRigTransform;
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

            GUILayout.Space(5f);

            if (EditorHelper.FloatField("Speed", "Speed at which the camera moves forward.", Speed, RigController))
            {
                lIsDirty = true;
                Speed = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.FloatField("Vertical Speed", "Speed at which the camera moves up/down.", VerticalSpeed, RigController))
            {
                lIsDirty = true;
                VerticalSpeed = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.TextField("Up Action Alias", "Action alias that has the camera move up.", UpActionAlias, RigController))
            {
                lIsDirty = true;
                UpActionAlias = EditorHelper.FieldStringValue;
            }

            if (EditorHelper.TextField("Down Action Alias", "Action alias that has the camera move down.", DownActionAlias, RigController))
            {
                lIsDirty = true;
                DownActionAlias = EditorHelper.FieldStringValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.FloatField("Yaw Speed", "Degrees per second to rotate.", YawSpeed, RigController))
            {
                lIsDirty = true;
                YawSpeed = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.FloatField("Pitch Speed", "Degrees per second to rotate.", PitchSpeed, RigController))
            {
                lIsDirty = true;
                PitchSpeed = EditorHelper.FieldFloatValue;
            }

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(new GUIContent("Pitch Range", "Angular range (min & max) for the rotation from -180 to 180"), GUILayout.Width(EditorGUIUtility.labelWidth - (EditorGUIUtility.currentViewWidth < 292 ? 18f : 4f)));

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

            GUILayout.Space(5f);

            if (EditorHelper.FloatField("Smoothing", "Smoothing factor applied to the rotations. Use 0 to have no smoothing and 1 for lots of smoothing. 0.05 to 0.2 is usually good.", Smoothing, RigController))
            {
                lIsDirty = true;
                Smoothing = EditorHelper.FieldFloatValue;
            }

            return lIsDirty;
        }

#endif

    }
}
