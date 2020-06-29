using UnityEngine;
using com.ootii.Actors;

namespace com.ootii.Cameras
{
    /// <summary>
    /// Provides a simple interface for all camera anchors
    /// </summary>
    public interface IBaseCameraAnchor
    {
        /// <summary>
        /// Determines if the anchor is actively following the root
        /// </summary>
        bool IsFollowingEnabled { get; set; }

        /// <summary>
        /// Transform that represents the camera rig
        /// </summary>
        Transform Transform { get; }

        /// <summary>
        /// Transform that represents the root that we want to follow or look at
        /// </summary>
        Transform Root { get; set; }

        /// <summary>
        /// Offset from the root that the anchor will be positioned.
        /// </summary>
        Vector3 RootOffset { get; set; }

        /// <summary>
        /// Taps into the controller's late update cycle (if it exists)
        /// </summary>
        ControllerLateUpdateDelegate OnAnchorPostLateUpdate { get; set; }

        /// <summary>
        /// Clears any target or position that we were moving to
        /// </summary>
        /// <param name="rFollowRoot">Determines if we go back to following the root</param>
        void ClearTarget(bool rFollowRoot = false);

        /// <summary>
        /// Used to have the anchor move back to the root and root offset
        /// </summary>
        /// <param name="rSpeed">Units per second to move to the position.</param>
        /// <param name="rLerp">Lerp value used for smoothing (0 to disable).</param>
        void ClearTarget(float rSpeed = 0f, float rLerp = 0f);

        /// <summary>
        /// Moves the anchor to the specified position over time.
        /// </summary>
        /// <param name="rTarget">Transform to follow or null for a static world position.</param>
        /// <param name="rPosition">Position relative to the rTarget.</param>
        /// <param name="rSpeed">Units per second to move to the position.</param>
        /// <param name="rLerp">Lerp value used for smoothing (0 to disable).</param>
        /// <param name="rClear">Determines if we clear the target once we reach it.</param>
        void SetTargetPosition(Transform rTarget, Vector3 rPosition, float rSpeed, float rLerp = 0f, bool rClear = true);
    }
}
