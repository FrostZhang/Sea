using System;
using UnityEngine;
using com.ootii.Base;
using com.ootii.Helpers;

namespace com.ootii.Cameras
{
    /// <summary>
    /// Simple motor used to always look at a specific object
    /// </summary>
    [BaseName("Look At Motor")]
    [BaseDescription("Simple motor used to always look at a specific object.")]
    public class LookAtTargetMotor : CameraMotor
    {
        /// <summary>
        /// Determines if we use the current position as the camera position vs. the anchor
        /// </summary>
        public bool _UseCurrentPosition = true;
        public bool UseCurrentPosition
        {
            get { return _UseCurrentPosition; }
            set { _UseCurrentPosition = value; }
        }

        /// <summary>
        /// Explicit position to move the camera to in code
        /// </summary>
        public Vector3 _Position = Vector3.zero;
        public Vector3 Position
        {
            get { return _Position; }

            set
            {
                _Position = value;
                mIsPositionSet = (_Position.sqrMagnitude > 0f);
            }
        }

        /// <summary>
        /// INTERNAL ONLY: Index into the Camera Rig Controller's stored gameobjects
        /// </summary>
        public int _TargetIndex = -1;
        public virtual int TargetIndex
        {
            get { return _TargetIndex; }

            set
            {
                _TargetIndex = value;

                if (_TargetIndex >= 0)
                {
                    if (RigController != null && _TargetIndex < RigController.StoredTransforms.Count)
                    {
                        _Target = RigController.StoredTransforms[_TargetIndex];
                    }
                }
            }
        }
        
        /// <summary>
        /// Anchor that we can use instead of the default
        /// </summary>
        [NonSerialized]
        public Transform _Target = null;
        public virtual Transform Target
        {
            get { return _Target; }

            set
            {
                _Target = value;

#if UNITY_EDITOR

                if (!Application.isPlaying)
                {
                    if (RigController != null)
                    {
                        if (_Target == null)
                        {
                            if (_TargetIndex >= 0 && _TargetIndex < RigController.StoredTransforms.Count)
                            {
                                RigController.StoredTransforms[_TargetIndex] = null;
                            }
                        }
                        else
                        {
                            if (_TargetIndex == -1)
                            {
                                _TargetIndex = RigController.StoredTransforms.Count;
                                RigController.StoredTransforms.Add(null);
                            }

                            RigController.StoredTransforms[_TargetIndex] = _Target;
                        }
                    }
                }

#endif

            }
        }

        /// <summary>
        /// Anchor offset we can use instead of the default
        /// </summary>
        public Vector3 _TargetOffset = Vector3.zero;
        public virtual Vector3 TargetOffset
        {
            get { return _TargetOffset; }

            set { _TargetOffset = value; }
        }

        /// <summary>
        /// Determines if a position was set for the motor
        /// </summary>
        protected bool mIsPositionSet = false;

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

            Vector3 lUp = Vector3.up;

            if (mIsPositionSet)
            {
                mRigTransform.Position = _Position;
            }
            else if (_UseCurrentPosition)
            {
                mRigTransform.Position = RigController._Transform.position;
            }
            else
            {
                if (Anchor != null)
                {
                    lUp = Anchor.up;
                    mRigTransform.Position = AnchorPosition + (Anchor.rotation * _Offset);
                }
                else
                {
                    mRigTransform.Position = _Offset;
                }
            }

            Vector3 lDirection = Vector3.forward;
            if (_Target == null)
            {
                lDirection = (_TargetOffset - mRigTransform.Position).normalized;
            }
            else
            { 
                lDirection = ((_Target.position + _TargetOffset) - mRigTransform.Position).normalized;
            }

            mRigTransform.Rotation = Quaternion.LookRotation(lDirection, lUp);

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

            if (_TargetIndex >= 0)
            {
                if (_TargetIndex < RigController.StoredTransforms.Count)
                {
                    _Target = RigController.StoredTransforms[_TargetIndex];
                }
                else
                {
                    _Target = null;
                    _TargetIndex = -1;
                }
            }

            IsCollisionEnabled = false;
            IsFadingEnabled = false;
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

            if (EditorHelper.BoolField("Use Static Position", "Determines if we use the transforms current position as the camera position (and don't move it with the anchor).", UseCurrentPosition, RigController))
            {
                lIsDirty = true;
                UseCurrentPosition = EditorHelper.FieldBoolValue;
            }

            if (!UseCurrentPosition)
            {
                GUILayout.Space(5f);


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

                if (EditorHelper.Vector3Field("Offset", "Offset that applied after the rig's AnchorOffset is applied", Offset, RigController))
                {
                    lIsDirty = true;
                    Offset = EditorHelper.FieldVector3Value;
                }
            }

            GUILayout.Space(5f);

            if (EditorHelper.ObjectField<Transform>("Target", "Base position the camera will look at.", _Target, RigController))
            {
                lIsDirty = true;
                Target = EditorHelper.FieldObjectValue as Transform;
            }

            if (EditorHelper.Vector3Field("Target Offset", "Offset from the Target position that the camera will look at.", _TargetOffset, RigController))
            {
                lIsDirty = true;
                TargetOffset = EditorHelper.FieldVector3Value;
            }

            return lIsDirty;
        }

#endif

    }
}
