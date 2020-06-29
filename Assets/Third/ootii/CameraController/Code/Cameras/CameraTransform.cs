using UnityEngine;

namespace com.ootii.Cameras
{
    /// <summary>
    /// Used to describe the physical state of the camera
    /// </summary>
    public struct CameraTransform
    {
        public Vector3 Position;

        public Quaternion Rotation;

        public float FieldOfView;

        /// <summary>
        /// Returns the lerped value from one transform to another
        /// </summary>
        /// <param name="rFrom"></param>
        /// <param name="rTo"></param>
        /// <param name="rTime"></param>
        public void Lerp(CameraTransform rFrom, CameraTransform rTo, float rTime)
        {
            Position = Vector3.Lerp(rFrom.Position, rTo.Position, rTime);
            Rotation = Quaternion.Slerp(rFrom.Rotation, rTo.Rotation, rTime);
        }
    }
}
