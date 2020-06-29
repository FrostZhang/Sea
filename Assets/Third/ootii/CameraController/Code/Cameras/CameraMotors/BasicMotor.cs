using UnityEngine;
using com.ootii.Base;

namespace com.ootii.Cameras
{
    /// <summary>
    /// Basic Motor that moves and rotates based on the Camera Controller's transform.
    /// </summary>
    [BaseName("Basic Motor")]
    [BaseDescription("Basic Motor that moves and rotates based on the Camera Controller's transform. It does not use the Anchor.")]
    public class BasicMotor : CameraMotor
    {
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

            mRigTransform.Position = RigController._Transform.position;
            mRigTransform.Rotation = RigController._Transform.rotation;

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

            return lIsDirty;
        }

#endif

    }
}
