using System;
using System.Collections;
using UnityEngine;
using com.ootii.Actors;
using com.ootii.Helpers;

namespace com.ootii.Cameras
{
    /// <summary>
    /// Camera Anchor is a component that typicaly follows the player. Then,
    /// the camera follows the anchor. This provides a way to detach the camera
    /// from the player or shift the view if needed.
    /// </summary>
    public class BaseCameraAnchor : MonoBehaviour, IBaseCameraAnchor
    {
        /// <summary>
        /// This transform. We cache this so we're not actually doing a Get everytime we access 'transform'
        /// </summary>
        [NonSerialized]
        public Transform _Transform = null;
        public virtual Transform Transform
        {
            get { return _Transform; }
        }

        /// <summary>
        /// Determines if the anchor is following the root or not
        /// </summary>
        public bool _IsFollowingEnabled = true;
        public virtual bool IsFollowingEnabled
        {
            get { return _IsFollowingEnabled; }
            set { _IsFollowingEnabled = value; }
        }

        /// <summary>
        /// Determines if the anchor will rotate with the target
        /// </summary>
        public bool _RotateWithTarget = true;
        public virtual bool RotateWithTarget
        {
            get { return _RotateWithTarget; }
            set { _RotateWithTarget = value; }
        }

        /// <summary>
        /// Transform that represents the anchor we want to follow.
        /// </summary>
        public Transform _Root = null;
        public virtual Transform Root
        {
            get { return _Root; }

            set
            {
                if (_Root != null)
                {
                    OnDisable();
                }

                _Root = value;
                if (_Root != null && this.enabled)
                {
                    OnEnable();
                }
            }
        }

        /// <summary>
        /// Alternate transform used to determine the anchor's rotation
        /// </summary>
        public Transform _RotationRoot = null;
        public virtual Transform RotationRoot
        {
            get { return _RotationRoot; }
            set { _RotationRoot = value; }
        }

        /// <summary>
        /// Offset from the root that the anchor will be positioned.
        /// </summary>
        public Vector3 _RootOffset = new Vector3(0f, 0f, 0f);
        public virtual Vector3 RootOffset
        {
            get { return _RootOffset; }
            set { _RootOffset = value; }
        }

        /// <summary>
        /// Lerp applied to movement to smooth it out
        /// </summary>
        public float _MovementLerp = 1f;
        public float MovementLerp
        {
            get { return _MovementLerp; }
            set { _MovementLerp = Mathf.Clamp01(value); }
        }

        /// <summary>
        /// Determines if we prevent movement on the x axis
        /// </summary>
        public bool _FreezePositionX = false;
        public bool FreezePositionX
        {
            get { return _FreezePositionX; }
            set { _FreezePositionX = value; }
        }

        /// <summary>
        /// Determines if we prevent movement on the y axis
        /// </summary>
        public bool _FreezePositionY = false;
        public bool FreezePositionY
        {
            get { return _FreezePositionY; }
            set { _FreezePositionY = value; }
        }

        /// <summary>
        /// Determines if we prevent movement on the z axis
        /// </summary>
        public bool _FreezePositionZ = false;
        public bool FreezePositionZ
        {
            get { return _FreezePositionZ; }
            set { _FreezePositionZ = value; }
        }

        /// <summary>
        /// Determines if we prevent rotation on the x axis
        /// </summary>
        public bool _FreezeRotationX = false;
        public bool FreezeRotationX
        {
            get { return _FreezeRotationX; }
            set { _FreezeRotationX = value; }
        }

        /// <summary>
        /// Determines if we prevent rotation on the y axis
        /// </summary>
        public bool _FreezeRotationY = false;
        public bool FreezeRotationY
        {
            get { return _FreezeRotationY; }
            set { _FreezeRotationY = value; }
        }

        /// <summary>
        /// Determines if we prevent rotation on the z axis
        /// </summary>
        public bool _FreezeRotationZ = false;
        public bool FreezeRotationZ
        {
            get { return _FreezeRotationZ; }
            set { _FreezeRotationZ = value; }
        }

        /// <summary>
        /// Allows for external processing after this anchor processes. This is important
        /// so we can keep the right position from the character/root.
        /// </summary>
        protected ControllerLateUpdateDelegate mOnAnchorPostLateUpdate = null;
        public virtual ControllerLateUpdateDelegate OnAnchorPostLateUpdate
        {
            get { return mOnAnchorPostLateUpdate; }
            set { mOnAnchorPostLateUpdate = value; }
        }

        // Determines if the camera is attached to an ICharacterController
        protected bool mIsAttachedToCharacterController = false;

        // Determines if a target is set
        protected bool mIsTargetPostionSet = false;

        // Determines the target object (or null for world)
        protected Transform mTarget = null;

        // Determines target position (relative to the mTarget)
        protected Vector3 mTargetOffset = Vector3.zero;

        // Speed to reach the target
        protected float mTargetSpeed = 1f;

        // Determines if we use smoothing to bring the movement in and out
        protected float mTargetLerp = 0f;

        // Determines if we clear the target once reached
        protected bool mTargetClear = true;

        // Determines if we start following the root once the target is reached
        protected bool mTargetRoot = false;

        /// <summary>
        /// Use this for initialization
        /// </summary>
        protected virtual void Awake()
        {
            _Transform = gameObject.transform;

            if (_Root != null && this.enabled)
            {
                OnEnable();
            }
        }

        /// <summary>
        /// Called when the component is enabled. This is also called after awake. So,
        /// we need to ensure we're not doubling up on the assignment.
        /// </summary>
        protected virtual void OnEnable()
        {
            if (_Root != null)
            {
                ICharacterController lController = InterfaceHelper.GetComponent<ICharacterController>(_Root.gameObject);
                if (lController != null)
                {
                    mIsAttachedToCharacterController = true;

                    if (lController.OnControllerPostLateUpdate != null) { lController.OnControllerPostLateUpdate -= OnControllerLateUpdate; }
                    lController.OnControllerPostLateUpdate += OnControllerLateUpdate;
                }
            }
        }

        /// <summary>
        /// Clears any target or position that we were moving to
        /// </summary>
        /// <param name="rFollowRoot">Determines if we go back to following the root.</param>
        public virtual void ClearTarget(bool rFollowRoot = false)
        {
            mTarget = null;
            mTargetOffset = Vector3.zero;
            mTargetSpeed = 0f;
            mTargetLerp = 0f;
            mTargetClear = false;
            mTargetRoot = false;
            mIsTargetPostionSet = false;

            IsFollowingEnabled = rFollowRoot;
        }

        /// <summary>
        /// Used to have the anchor move back to the root and root offset
        /// </summary>
        /// <param name="rSpeed">Units per second to move to the position.</param>
        /// <param name="rLerp">Lerp value used for smoothing (0 to disable).</param>
        public virtual void ClearTarget(float rSpeed = 0f, float rLerp = 0f)
        {
            mTarget = null;
            mTargetOffset = Vector3.zero;
            mTargetSpeed = rSpeed;
            mTargetLerp = rLerp;
            mTargetClear = true;
            mTargetRoot = true;
            mIsTargetPostionSet = (rSpeed > 0f);
        }

        /// <summary>
        /// Moves the anchor to the specified position over time.
        /// </summary>
        /// <param name="rTarget">Transform to follow (or null for a static world position).</param>
        /// <param name="rPosition">Position relative to the rTarget.</param>
        /// <param name="rSpeed">Units per second to move to the position.</param>
        /// <param name="rLerp">Lerp value used for smoothing (0 to disable).</param>
        /// <param name="rClear">Determines if we clear the target once we reach it.</param>
        public virtual void SetTargetPosition(Transform rTarget, Vector3 rPosition, float rSpeed, float rLerp = 0f, bool rClearTargetOnArrival = true)
        {
            IsFollowingEnabled = false;

            mTarget = rTarget;
            mTargetOffset = rPosition;
            mTargetSpeed = rSpeed;
            mTargetLerp = rLerp;
            mTargetClear = rClearTargetOnArrival;
            mTargetRoot = false;
            mIsTargetPostionSet = true;
        }

        /// <summary>
        /// Called when the component is disabled.
        /// </summary>
        protected virtual void OnDisable()
        {
            if (_Root != null)
            {
                ICharacterController lController = InterfaceHelper.GetComponent<ICharacterController>(_Root.gameObject);
                if (lController != null && lController.OnControllerPostLateUpdate != null)
                {
                    lController.OnControllerPostLateUpdate -= OnControllerLateUpdate;
                }
            }
        }

        /// <summary>
        /// Updates after each frame. We may not use this if we're attached to an ICharacterController
        /// </summary>
        protected virtual void LateUpdate()
        {
            if (mIsAttachedToCharacterController) { return; }

            AnchorLateUpdate(Time.deltaTime, 1);

            // Allow hooks to process after we do our update this frame
            if (OnAnchorPostLateUpdate != null)
            {
                OnAnchorPostLateUpdate(null, Time.deltaTime, 1);
            }
        }

        /// <summary>
        /// LateUpdate logic for the controller should be done here. This allows us
        /// to support dynamic and fixed update times
        /// </summary>
        /// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
        /// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
        protected virtual void AnchorLateUpdate(float rDeltaTime, int rUpdateIndex)
        {
            if (_Root == null) { return; }

            if (mIsTargetPostionSet)
            {
                Vector3 lTargetPosition = mTargetOffset;

                if (mTargetRoot)
                {
                    lTargetPosition = _Root.position + ((_RotationRoot != null ? _RotationRoot.right : _Root.right) * _RootOffset.x);
                }
                else if (mTarget != null)
                {
                    lTargetPosition = mTarget.position + (mTarget.rotation * mTargetOffset);
                }

                Vector3 lDirection = lTargetPosition - _Transform.position;
                float lDistance = lDirection.magnitude;

                if (lDistance <= 0.001f)
                {
                    if (mTargetClear) { ClearTarget(mTargetRoot); }
                    if (mTargetRoot) { IsFollowingEnabled = true; }
                }
                else
                {
                    if (mTargetSpeed > 0f) { lDistance = Mathf.Min(lDistance, mTargetSpeed * Time.deltaTime); }

                    Vector3 lMovement = lDirection.normalized * lDistance;
                    if (_FreezePositionX) { lMovement.x = 0f; }
                    if (_FreezePositionY) { lMovement.y = 0f; }
                    if (_FreezePositionZ) { lMovement.z = 0f; }

                    lTargetPosition = _Transform.position + lMovement;

                    if (mTargetLerp <= 0f || mTargetLerp >= 1f)
                    {
                        _Transform.position = lTargetPosition;
                    }
                    else
                    {
                        _Transform.position = Vector3.Lerp(_Transform.position, lTargetPosition, mTargetLerp);
                    }
                }
            }

            if (_IsFollowingEnabled)
            {
                Vector3 lNewAnchorPosition = _Root.position + ((_RotationRoot != null ? _RotationRoot.right : _Root.right) * _RootOffset.x);

                Vector3 lMovement = lNewAnchorPosition - _Transform.position;
                if (_FreezePositionX) { lMovement.x = 0f; }
                if (_FreezePositionY) { lMovement.y = 0f; }
                if (_FreezePositionZ) { lMovement.z = 0f; }

                _Transform.position = _Transform.position + Vector3.Lerp(Vector3.zero, lMovement, _MovementLerp);

                if (_RotateWithTarget)
                {
                    _Transform.rotation = _Root.rotation;
                }
            }
        }

        /// <summary>
        /// Delegate callback for handling the camera movement AFTER the character controller
        /// </summary>
        /// <param name="rController"></param>
        /// <param name="rDeltaTime"></param>
        /// <param name="rUpdateIndex"></param>
        protected virtual void OnControllerLateUpdate(ICharacterController rController, float rDeltaTime, int rUpdateIndex)
        {
            // Update this component
            AnchorLateUpdate(rDeltaTime, rUpdateIndex);

            // Allow hooks to process after we do our update this frame
            if (OnAnchorPostLateUpdate != null)
            {
                OnAnchorPostLateUpdate(rController, rDeltaTime, rUpdateIndex);
            }
        }
    }
}
