using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using com.ootii.Actors;
using com.ootii.Geometry;
using com.ootii.Helpers;
using com.ootii.Input;
using com.ootii.Messages;
using com.ootii.Utilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Cameras
{
    /// <summary>
    /// Delegate to support camera controller events
    /// </summary>
    public delegate void CameraMotorEvent(CameraMotor rMotor);

    /// <summary>
    /// Camera rig used to hold the camera and move based on the motors
    /// </summary>
    [AddComponentMenu("ootii/Camera Rigs/Camera Controller")]
    public class CameraController : BaseCameraRig
    {
        // Floating point error
        public const float EPSILON = 0.0001f;

        // Keep us from going past the poles. The number is a bit
        // odd, but it matches Unity's limit for the LookRotation function.
        public const float MIN_PITCH = -87.4f;
        public const float MAX_PITCH = 87.4f;

        /// <summary>
        /// Character controller the camera is tied to (if it is)
        /// </summary>
        protected ICharacterController mCharacterController = null;
        public ICharacterController CharacterController
        {
            get { return mCharacterController; }
        }

        /// <summary>
        /// GameObject that owns the IInputSource we really want
        /// </summary>
        public GameObject _InputSourceOwner = null;
        public GameObject InputSourceOwner
        {
            get { return _InputSourceOwner; }

            set
            {
                _InputSourceOwner = value;

                // Object that will provide access to the keyboard, mouse, etc
                if (_InputSourceOwner != null) { mInputSource = InterfaceHelper.GetComponent<IInputSource>(_InputSourceOwner); }
            }
        }

        /// <summary>
        /// Determines if we'll auto find the input source if one doesn't exist
        /// </summary>
        public bool _AutoFindInputSource = true;
        public bool AutoFindInputSource
        {
            get { return _AutoFindInputSource; }
            set { _AutoFindInputSource = value; }
        }

        /// <summary>
        /// Transform that represents the anchor we want to follow
        /// </summary>
        public override Transform Anchor
        {
            get { return _Anchor; }

            set
            {
                if (_Anchor != null)
                {
                    // Stop listening to the old controller
                    ICharacterController lController = InterfaceHelper.GetComponent<ICharacterController>(_Anchor.gameObject);
                    if (lController != null)
                    {
                        lController.OnControllerPostLateUpdate -= OnControllerLateUpdate;
                    }
                    else
                    {
                        IBaseCameraAnchor lAnchor = _Anchor.GetComponent<IBaseCameraAnchor>();
                        if (lAnchor != null) { lAnchor.OnAnchorPostLateUpdate -= OnControllerLateUpdate; }
                    }
                }

                bool lIsAnchorSame = (_Anchor == value);

                _Anchor = value;
                if (_Anchor != null && this.enabled)
                {
                    // Start listening to the new controller
                    ICharacterController lController = InterfaceHelper.GetComponent<ICharacterController>(_Anchor.gameObject);
                    if (lController == null)
                    {
                        IBaseCameraAnchor lAnchor = _Anchor.GetComponent<IBaseCameraAnchor>();
                        if (lAnchor != null)
                        {
                            lAnchor.OnAnchorPostLateUpdate += OnControllerLateUpdate;
                        }
                        else
                        {
                            IsInternalUpdateEnabled = true;
                        }
                    }
                    else
                    {
                        IsInternalUpdateEnabled = false;
                        IsFixedUpdateEnabled = false;
                        lController.OnControllerPostLateUpdate += OnControllerLateUpdate;
                    }

                    // Reinitialize the motor to the new anchor. This way we can
                    // set things like the last anchor position
                    if (Application.isPlaying)
                    {
                        if (!lIsAnchorSame) { InstantiateMotors(); }

                        if (!lIsAnchorSame && ActiveMotor != null)
                        {
                            ActiveMotor.Initialize();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Offset from the anchor's position that represents the rig's target
        /// </summary>
        public Vector3 _AnchorOffset = new Vector3(0f, 2f, 0f);
        public Vector3 AnchorOffset
        {
            get { return _AnchorOffset; }
            set { _AnchorOffset = value; }
        }

        /// <summary>
        /// Determines if the anchor offset will rotate with the anchor. This can
        /// be disabled to support "always up" cameras.
        /// </summary>
        public bool _RotateAnchorOffset = true;
        public bool RotateAnchorOffset
        {
            get { return _RotateAnchorOffset; }
            set { _RotateAnchorOffset = value; }
        }

        /// <summary>
        /// Current position of the actual anchor
        /// </summary>
        public Vector3 AnchorPosition
        {
            get
            {
                if (_Anchor == null)
                {
                    return _AnchorOffset;
                }
                else
                {
                    return _Anchor.position + (_Anchor.rotation * _AnchorOffset);
                }
            }
        }

        /// <summary>
        /// Determines if we inverse the pitch on all 1st person and 3rd person cameras.
        /// </summary>
        public bool _InvertPitch = true;
        public bool InvertPitch
        {
            get { return _InvertPitch; }
            set { _InvertPitch = value; }
        }

        /// <summary>
        /// For this rig, "Mode" is actually the motor index.
        /// </summary>
        public override int Mode
        {
            get { return _ActiveMotorIndex; }
            set { ActivateMotor(value); }
        }

        /// <summary>
        /// Motor index that is currently active
        /// </summary>
        public int _ActiveMotorIndex = -1;
        public int ActiveMotorIndex
        {
            get { return _ActiveMotorIndex; }
        }

        /// <summary>
        /// Gets the active motor
        /// </summary>
        public CameraMotor ActiveMotor
        {
            get
            {
                if (_ActiveMotorIndex < 0 || _ActiveMotorIndex >= Motors.Count) { return null; }
                return Motors[_ActiveMotorIndex];
            }
        }

        /// <summary>
        /// Event for when a motor is testing to see if it should activate. Allows external callers to reject it
        /// </summary>
        public MessageEvent MotorTestActivateEvent = null;

        /// <summary>
        /// Event for when a motor is activated. Allows external callers to react to it.
        /// </summary>
        public MessageEvent MotorActivatedEvent = null;

        /// <summary>
        /// Event for when a motor is deactivated. Allows external callers to react to it.
        /// </summary>
        public MessageEvent MotorDeactivatedEvent = null;

        /// <summary>
        /// Event for a custom action that occurs dring a motor. Allows external callers to react to it.
        /// </summary>
        public MessageEvent ActionTriggeredEvent = null;

        /// <summary>
        /// Event for when a motion is activated. Allows external callers to tap into it.
        /// </summary>
        [NonSerialized]
        public CameraMotorEvent MotorActivated = null;

        /// <summary>
        /// Event for after the active motion has been updated.
        /// </summary>
        [NonSerialized]
        public CameraMotorEvent MotorUpdated = null;

        /// <summary>
        /// Event for some motors that travel on thier own.
        /// </summary>
        [NonSerialized]
        public CameraMotorEvent MotorArrived = null;

        /// <summary>
        /// Event for when a motion is deactivated. Allows external callers to tap into it.
        /// </summary>
        [NonSerialized]
        public CameraMotorEvent MotorDeactivated = null;

        /// <summary>
        /// We keep track of the tilt so we can make small changes to it as the actor rotates.
        /// This is safter than trying to do a full rotation all at once which can cause odd
        /// rotations as we hit 180 degrees.
        /// </summary>
        protected Quaternion mTilt = Quaternion.identity;
        public Quaternion Tilt
        {
            get { return mTilt; }
        }

        /// <summary>
        /// Determines if we'll use collisions
        /// </summary>
        public bool _IsCollisionsEnabled = true;
        public bool IsCollisionsEnabled
        {
            get { return _IsCollisionsEnabled; }
            set { _IsCollisionsEnabled = value; }
        }

        /// <summary>
        /// Layers the camera will collide with
        /// </summary>
        public int _CollisionLayers = 1;
        public int CollisionLayers
        {
            get { return _CollisionLayers; }
            set { _CollisionLayers = value; }
        }

        /// <summary>
        /// Radius of the camera's collider
        /// </summary>
        public float _CollisionRadius = 0.2f;
        public float CollisionRadius
        {
            get { return _CollisionRadius; }
            set { _CollisionRadius = value; }
        }

        /// <summary>
        /// Minimum distance we'll move the camera to if there's a collision
        /// </summary>
        public float _MinCollisionDistance = 0f;
        public float MinCollisionDistance
        {
            get { return _MinCollisionDistance; }
            set { _MinCollisionDistance = value; }
        }

        /// <summary>
        /// Speed at which the camera recovers from a collision
        /// </summary>
        public float _CollisionRecoverySpeed = 5f;
        public float CollisionRecoverySpeed
        {
            get { return _CollisionRecoverySpeed; }
            set { _CollisionRecoverySpeed = value; }
        }

        /// <summary>
        /// Used to help manage our collisions
        /// </summary>
        protected bool mHadCollided = false;

        /// <summary>
        /// Actual distance if we've collided or not
        /// </summary>
        protected float mActualDistance = 0f;

        /// <summary>
        /// Determines if we enable zooming
        /// </summary>
        public bool _IsZoomEnabled = true;
        public bool IsZoomEnabled
        {
            get { return _IsZoomEnabled; }
            set { _IsZoomEnabled = value; }
        }

        /// <summary>
        /// Action alias we use to test if we're zooming
        /// </summary>
        public string _ZoomActionAlias = "Camera Zoom";
        public string ZoomActionAlias
        {
            get { return _ZoomActionAlias; }
            set { _ZoomActionAlias = value; }
        }

        /// <summary>
        /// Deteremins if we'll reset the zoom when the action alias is released
        /// </summary>
        public bool _ZoomResetOnRelease = true;
        public bool ZoomResetOnRelease
        {
            get { return _ZoomResetOnRelease; }
            set { _ZoomResetOnRelease = value; }
        }

        /// <summary>
        /// Speed at which we zoom in and out
        /// </summary>
        public float _ZoomSpeed = 25f;
        public float ZoomSpeed
        {
            get { return _ZoomSpeed; }
            set { _ZoomSpeed = value; }
        }

        /// <summary>
        /// Smoothing we use to reach the target
        /// </summary>
        public float _ZoomSmoothing = 0.1f;
        public float ZoomSmoothing
        {
            get { return _ZoomSmoothing; }
            set { _ZoomSmoothing = value; }
        }

        /// <summary>
        /// Min amount we'll zoom into
        /// </summary>
        public float _ZoomMin = 20f;
        public float ZoomMin
        {
            get { return _ZoomMin; }
            set { _ZoomMin = value; }
        }

        /// <summary>
        /// Max amount we'll zoomout of. 0f means use the orignal FOV value
        /// </summary>
        public float _ZoomMax = 0f;
        public float ZoomMax
        {
            get { return _ZoomMax; }
            set { _ZoomMax = value; }
        }

        /// <summary>
        /// Field of view that the camera started with
        /// </summary>
        protected float mOriginalFOV = 60f;
        public float OriginalFOV
        {
            get { return mOriginalFOV; }
        }

        /// <summary>
        /// Field of view that the camera is moving towards
        /// </summary>
        protected float mTargetFOV = float.MaxValue;
        public float TargetFOV
        {
            get { return mTargetFOV; }
            set { mTargetFOV = value; }
        }

        /// <summary>
        /// Velocity at which we're zooming in and out
        /// </summary>
        private float mZoomVelocity = 0f;

        /// <summary>
        /// Determines if we fade the character when the camera is close
        /// </summary>
        public bool _IsFadeEnabled = true;
        public bool IsFadeEnabed
        {
            get { return _IsFadeEnabled; }

            set
            {
                _IsFadeEnabled = value;
                if (!_IsFadeEnabled) { SetAnchorAlpha(1f); }
            }
        }

        /// <summary>
        /// Distance at which we start the fading
        /// </summary>
        public float _FadeDistance = 0.4f;
        public float FadeDistance
        {
            get { return _FadeDistance; }
            set { _FadeDistance = value; }
        }

        /// <summary>
        /// Time it takes to fade
        /// </summary>
        public float _FadeSpeed = 0.25f;
        public float FadeSpeed
        {
            get { return _FadeSpeed; }
            set { _FadeSpeed = value; }
        }

        /// <summary>
        /// After fading is complete, determines if we disable the renderers. This is good
        /// when there are materials with no transparancies (which is required for fading)
        /// </summary>
        public bool _DisableRenderers = false;
        public bool DisableRenderers
        {
            get { return _DisableRenderers; }
            set { _DisableRenderers = value; }
        }

        /// <summary>
        /// Alpha values used to ease to the target alpha over time
        /// </summary>
        protected float mAlpha = 1f;
        protected float mAlphaStart = 0f;
        protected float mAlphaEnd = 1f;
        protected float mAlphaElapsed = 0f;

        /// <summary>
        /// Determines how strong the shake is over the duration
        /// </summary>
        public AnimationCurve _ShakeStrength = AnimationCurve.Linear(0f, 0f, 1f, 0f);
        public AnimationCurve ShakeStrength
        {
            get { return _ShakeStrength; }
            set { _ShakeStrength = value; }
        }

        /// <summary>
        /// Defines how long we've been shaking for
        /// </summary>
        protected float mShakeElapsed = 0f;
        protected float mShakeDuration = 0f;
        protected float mShakeSpeedFactor = 1f;
        protected float mShakeRange = 0.05f;
        protected float mShakeStrengthX = 1f;
        protected float mShakeStrengthY = 1f;

        /// <summary>
        /// List of motions the controller manages
        /// </summary>
        //[NonSerialized]
        public List<CameraMotor> Motors = new List<CameraMotor>();

        /// <summary>
        /// Provides access to the keyboard, mouse, etc.
        /// </summary>
        protected IInputSource mInputSource = null;
        public IInputSource InputSource
        {
            get { return mInputSource; }
        }

        /// <summary>
        /// Serialized motors since Unity can't serialized derived classes
        /// </summary>
        public List<string> MotorDefinitions = new List<string>();

        /// <summary>
        /// Serialized transforms that may be used by the motors. We have to store them
        /// here since Unity won't serialize them with and support polymorphism.
        /// DO NOT SHARE INDEXES - even if this mean re-storing the same transform between motors
        /// </summary>
        public List<Transform> StoredTransforms = new List<Transform>();

        /// <summary>
        /// Track the camera's last position
        /// </summary>
        protected Vector3 mLastPosition = Vector3.zero;
        public Vector3 LastPosition
        {
            get { return mLastPosition; }
        }

        /// <summary>
        /// Track the camera's last rotation
        /// </summary>
        protected Quaternion mLastRotation = Quaternion.identity;
        public Quaternion LastRotation
        {
            get { return mLastRotation; }
        }

        /// <summary>
        /// Called before any Start or Update functions are called.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            // Initialize the old positions
            mLastPosition = transform.position;
            mLastRotation = transform.rotation;

            // Object that will provide access to the keyboard, mouse, etc
            if (_InputSourceOwner != null) { mInputSource = InterfaceHelper.GetComponent<IInputSource>(_InputSourceOwner); }

            // If the input source is still null, see if we can grab a local input source
            if (_AutoFindInputSource && mInputSource == null) { mInputSource = InterfaceHelper.GetComponent<IInputSource>(gameObject); }

            // If that's still null, see if we can grab one from the scene. This may happen
            // if the MC was instanciated from a prefab which doesn't hold a reference to the input source
            if (_AutoFindInputSource && mInputSource == null)
            {
                IInputSource[] lInputSources = InterfaceHelper.GetComponents<IInputSource>();
                for (int i = 0; i < lInputSources.Length; i++)
                {
                    GameObject lInputSourceOwner = ((MonoBehaviour)lInputSources[i]).gameObject;
                    if (lInputSourceOwner.activeSelf && lInputSources[i].IsEnabled)
                    {
                        mInputSource = lInputSources[i];
                        _InputSourceOwner = lInputSourceOwner;
                    }
                }
            }

            // Setup the camera params based on the character controller
            if (_Anchor != null && this.enabled)
            {
                ICharacterController lController = InterfaceHelper.GetComponent<ICharacterController>(_Anchor.gameObject);
                if (lController == null)
                {
                    IBaseCameraAnchor lAnchor = _Anchor.GetComponent<IBaseCameraAnchor>();
                    if (lAnchor != null)
                    {
                        IsInternalUpdateEnabled = false;
                        IsFixedUpdateEnabled = false;
                        lAnchor.OnAnchorPostLateUpdate += OnControllerLateUpdate;
                    }
                }
                else
                {
                    IsInternalUpdateEnabled = false;
                    IsFixedUpdateEnabled = false;
                    lController.OnControllerPostLateUpdate += OnControllerLateUpdate;
                }

                mTilt = QuaternionExt.FromToRotation(Vector3.up, _Anchor.up);
            }

            // Grab the defaults
            if (_Camera != null)
            {
                mOriginalFOV = _Camera.fieldOfView;
            }

            // Default the curve
            if (_ShakeStrength.keys.Length == 2)
            {
                if (_ShakeStrength.keys[0].value == 0f && _ShakeStrength.keys[0].value == 0f)
                {
                    _ShakeStrength.AddKey(0.5f, 1f);
                }
            }

            // Create and initialize the motors
            InstantiateMotors();
        }

        /// <summary>
        /// Use this for initialization
        /// </summary>
        protected override void Start()
        {
            if (Anchor == null)
            {
                GameObject lPlayer = GameObject.FindGameObjectWithTag("Player");
                if (lPlayer != null) { Anchor = lPlayer.transform; }
            }
        }

        /// <summary>
        /// Called when the component is enabled. This is also called after awake. So,
        /// we need to ensure we're not doubling up on the assignment.
        /// </summary>
        protected void OnEnable()
        {
            if (_Anchor != null)
            {
                ICharacterController lController = InterfaceHelper.GetComponent<ICharacterController>(_Anchor.gameObject);
                if (lController != null)
                {
                    if (lController.OnControllerPostLateUpdate != null) { lController.OnControllerPostLateUpdate -= OnControllerLateUpdate; }
                    lController.OnControllerPostLateUpdate += OnControllerLateUpdate;
                }
                else
                {
                    IBaseCameraAnchor lAnchor = _Anchor.GetComponent<IBaseCameraAnchor>();
                    if (lAnchor != null)
                    {
                        if (lAnchor.OnAnchorPostLateUpdate != null) { lAnchor.OnAnchorPostLateUpdate -= OnControllerLateUpdate; }
                        lAnchor.OnAnchorPostLateUpdate += OnControllerLateUpdate;
                    }
                }
            }
        }

        /// <summary>
        /// Called when the component is disabled.
        /// </summary>
        protected void OnDisable()
        {
            if (_Anchor != null)
            {
                ICharacterController lController = InterfaceHelper.GetComponent<ICharacterController>(_Anchor.gameObject);
                if (lController != null && lController.OnControllerPostLateUpdate != null)
                {
                    lController.OnControllerPostLateUpdate -= OnControllerLateUpdate;
                }
                else
                {
                    IBaseCameraAnchor lAnchor = _Anchor.GetComponent<IBaseCameraAnchor>();
                    if (lAnchor != null)
                    {
                        lAnchor.OnAnchorPostLateUpdate -= OnControllerLateUpdate;
                    }
                }
            }
        }

        /// <summary>
        /// Works backwards from the camera to determine how the anchor should be positioned and rotated
        /// to reach the current camera position.
        /// </summary>
        /// <param name="rPosition">Expected anchor position.</param>
        /// <param name="rRotation">Expected anchor rotation.</param>
        public override void ExtrapolateAnchorPosition(out Vector3 rPosition, out Quaternion rRotation)
        {
            rPosition = _Anchor.position;
            rRotation = _Anchor.rotation;

            CameraMotor lMotor = ActiveMotor;
            if (lMotor == null) { return; }

            float lDistance = (mActualDistance > 0f ? mActualDistance : lMotor.Distance);
            Vector3 lFocusPoint = _Transform.position + (_Transform.forward * lDistance);

            YawPitchMotor lYawPitchMotor = lMotor as YawPitchMotor;
            if (lYawPitchMotor != null)
            {
                Quaternion lFinalRotation = _Transform.rotation * Quaternion.Inverse(Quaternion.Euler(lYawPitchMotor.LocalPitch, lYawPitchMotor.LocalYaw, 0f));

                lFocusPoint = lFocusPoint + (_Transform.right * -lMotor.Offset.x) + (_Transform.forward * -lMotor.Offset.z);
                Vector3 lFinalPosition = lFocusPoint - (lFinalRotation * lMotor.AnchorOffset);

                rPosition = lFinalPosition;
                rRotation = lFinalRotation;
            }
        }

        /// <summary>
        /// Enables (or disables) the motor based on the name. Note that this doesn't activate
        /// the motor. It just sets if the motor CAN be activated.
        /// </summary>   
        /// <param name="rName">Name of the motor to enable</param>
        /// <param name="rEnable">Determines if we're enabling or disabling the motor</param>
        /// <returns></returns>
        public void EnableMotor(string rName, bool rEnable)
        {
            for (int i = 0; i < Motors.Count; i++)
            {
                CameraMotor lMotor = Motors[i];
                if (lMotor.Name == rName)
                {
                    lMotor.IsEnabled = rEnable;
                }
            }
        }

        /// <summary>
        /// Enables (or disables) the motor based on the type and name. Note that this doesn't activate
        /// the motor. It just sets if the motor CAN be activated.
        /// </summary>
        /// <typeparam name="T">Type of motor to enable</typeparam>
        /// <param name="rEnable">Determines if we are enabling or disabling</param>
        /// <param name="rName">Optional parameter to specify a name to look for too</param>
        /// <returns></returns>
        public void EnableMotor<T>(bool rEnable, string rName = null) where T : CameraMotor
        {
            Type lBaseType = typeof(T);

            for (int i = 0; i < Motors.Count; i++)
            {
                CameraMotor lMotor = Motors[i];
                Type lType = lMotor.GetType();

                if (ReflectionHelper.IsSubclassOf(lType, lBaseType))
                {
                    if (rName == null || lMotor.Name == rName)
                    {
                        lMotor.IsEnabled = rEnable;
                    }
                }
            }
        }

        /// <summary>
        /// Activate the specified motor if it is enabled.
        /// </summary>
        /// <param name="rIndex">Index of the motor to be activated</param>
        public void ActivateMotor(int rIndex)
        {
            if (_ActiveMotorIndex == rIndex) { return; }

            if (rIndex < 0 || rIndex >= Motors.Count) { return; }
            if (!Motors[rIndex].IsEnabled) { return; }

            CameraMessage lMessage = null;
            CameraMotor lPrevActiveMotor = null;

            if (_ActiveMotorIndex >= 0 && _ActiveMotorIndex < Motors.Count)
            {
                lPrevActiveMotor = Motors[_ActiveMotorIndex];
                lPrevActiveMotor.Deactivate(Motors[rIndex]);

                // If somone has tapped into the event, fire it
                if (MotorDeactivated != null) { MotorDeactivated(lPrevActiveMotor); }

                // Send the message
                lMessage = CameraMessage.Allocate();
                lMessage.ID = EnumMessageID.MSG_CAMERA_MOTOR_DEACTIVATE;
                lMessage.Motor = lPrevActiveMotor;

                if (MotorDeactivatedEvent != null)
                {
                    MotorDeactivatedEvent.Invoke(lMessage);
                }

#if USE_MESSAGE_DISPATCHER || OOTII_MD
                MessageDispatcher.SendMessage(lMessage);
#endif

                CameraMessage.Release(lMessage);
            }

            Motors[rIndex].Activate(lPrevActiveMotor);
            _ActiveMotorIndex = rIndex;

            // If somone has tapped into the event, fire it
            if (MotorActivated != null) { MotorActivated(Motors[_ActiveMotorIndex]); }

            // Send the message
            lMessage = CameraMessage.Allocate();
            lMessage.ID = EnumMessageID.MSG_CAMERA_MOTOR_ACTIVATE;
            lMessage.Motor = Motors[_ActiveMotorIndex];

            if (MotorActivatedEvent != null)
            {
                MotorActivatedEvent.Invoke(lMessage);
            }

#if USE_MESSAGE_DISPATCHER || OOTII_MD
            MessageDispatcher.SendMessage(lMessage);
#endif

            CameraMessage.Release(lMessage);
        }

        /// <summary>
        /// Activate the specified motor if it is enabled.
        /// </summary>
        /// <param name="rMotor">Motor to be activated</param>
        public void ActivateMotor(CameraMotor rMotor)
        {
            if (rMotor == null) { return; }
            if (!rMotor.IsEnabled) { return; }

            int lIndex = Motors.IndexOf(rMotor);
            if (lIndex == _ActiveMotorIndex) { return; }

            CameraMessage lMessage = null;
            CameraMotor lPrevActiveMotor = null;

            if (_ActiveMotorIndex >= 0 && _ActiveMotorIndex < Motors.Count)
            {
                lPrevActiveMotor = Motors[_ActiveMotorIndex];
                lPrevActiveMotor.Deactivate(rMotor);

                // If somone has tapped into the event, fire it
                if (MotorDeactivated != null) { MotorDeactivated(lPrevActiveMotor); }

                // Send the message
                lMessage = CameraMessage.Allocate();
                lMessage.ID = EnumMessageID.MSG_CAMERA_MOTOR_DEACTIVATE;
                lMessage.Motor = Motors[_ActiveMotorIndex];

                if (MotorDeactivatedEvent != null)
                {
                    MotorDeactivatedEvent.Invoke(lMessage);
                }

#if USE_MESSAGE_DISPATCHER || OOTII_MD
                MessageDispatcher.SendMessage(lMessage);
#endif

                CameraMessage.Release(lMessage);
            }

            rMotor.Activate(lPrevActiveMotor);
            _ActiveMotorIndex = lIndex;

            // If somone has tapped into the event, fire it
            if (MotorActivated != null) { MotorActivated(Motors[_ActiveMotorIndex]); }

            // Send the message
            lMessage = CameraMessage.Allocate();
            lMessage.ID = EnumMessageID.MSG_CAMERA_MOTOR_ACTIVATE;
            lMessage.Motor = Motors[_ActiveMotorIndex];

            if (MotorActivatedEvent != null)
            {
                MotorActivatedEvent.Invoke(lMessage);
            }

#if USE_MESSAGE_DISPATCHER || OOTII_MD
            MessageDispatcher.SendMessage(lMessage);
#endif

            CameraMessage.Release(lMessage);
        }

        /// <summary>
        /// Deactivates the current motor, leaving no motor active
        /// </summary>
        public void DeactivateMotor()
        {
            if (_ActiveMotorIndex >= 0 && _ActiveMotorIndex < Motors.Count)
            {
                Motors[_ActiveMotorIndex].Deactivate(null);

                // If somone has tapped into the event, fire it
                if (MotorDeactivated != null) { MotorDeactivated(Motors[_ActiveMotorIndex]); }

                // Send the message
                CameraMessage lMessage = CameraMessage.Allocate();
                lMessage.ID = EnumMessageID.MSG_CAMERA_MOTOR_DEACTIVATE;
                lMessage.Motor = Motors[_ActiveMotorIndex];

                if (MotorDeactivatedEvent != null)
                {
                    MotorDeactivatedEvent.Invoke(lMessage);
                }

#if USE_MESSAGE_DISPATCHER || OOTII_MD
                MessageDispatcher.SendMessage(lMessage);
#endif

                CameraMessage.Release(lMessage);
            }

            _ActiveMotorIndex = -1;
        }

        /// <summary>
        /// Returns the first motor that is enabled and has the specified name
        /// </summary>        
        /// <param name="rName">Name of the motor we are trying to retrieve</param>
        /// <returns>Motor if a motor is found matching the name or null if not</returns>
        public CameraMotor GetMotor(string rName)
        {
            for (int i = 0; i < Motors.Count; i++)
            {
                CameraMotor lMotor = Motors[i];
                if (lMotor.Name == rName)
                {
                    return lMotor;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the first motor that is enabled and a subtype of the specified type
        /// </summary>
        /// <typeparam name="T">Type of motor we are looking for</typeparam>
        /// <param name="rName">Optional name we will look for as well.</param>
        /// <returns>Returns the first motor found matching the arguments or null if none are found.</returns>
        public T GetMotor<T>(string rName = null) where T : CameraMotor
        {
            Type lBaseType = typeof(T);

            for (int i = 0; i < Motors.Count; i++)
            {
                CameraMotor lMotor = Motors[i];
                Type lType = lMotor.GetType();

                if (ReflectionHelper.IsSubclassOf(lType, lBaseType))
                {
                    if (rName == null || lMotor.Name == rName)
                    {
                        return lMotor as T;
                    }
                }
            }

            return default(T);
        }

        /// <summary>
        /// Returns the first motor index that is enabled and has the specified name.
        /// </summary>        
        /// <param name="rName">Optional name we can look for as well.</param>
        /// <returns>Returns the first motor matching the arguments or null if none is found</returns>
        public int GetMotorIndex(string rName)
        {
            for (int i = 0; i < Motors.Count; i++)
            {
                CameraMotor lMotor = Motors[i];
                if (lMotor.Name == rName)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns the first motor's index that is enabled and a subtype of the specified type.
        /// </summary>
        /// <typeparam name="T">Type of motor we are looking for</typeparam>
        /// <param name="rName">Optional name we can look for as well.</param>
        /// <returns>Index of the first motor matching the arguments or -1 if none is found</returns>
        public int GetMotorIndex<T>(string rName = null) where T : CameraMotor
        {
            Type lBaseType = typeof(T);

            for (int i = 0; i < Motors.Count; i++)
            {
                CameraMotor lMotor = Motors[i];
                Type lType = lMotor.GetType();

                if (ReflectionHelper.IsSubclassOf(lType, lBaseType))
                {
                    if (rName == null || lMotor.Name == rName)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// Causes the camera to shake for the specified duration
        /// </summary>
        /// <param name="rRange">Distance the shake will apply</param>
        /// <param name="rDuration">Time (in seconds) to shake</param>
        public void Shake(float rRange, float rDuration)
        {
            mShakeElapsed = 0f;
            mShakeSpeedFactor = 1f;
            mShakeStrengthX = 1f;
            mShakeStrengthY = 1f;
            mShakeRange = rRange;
            mShakeDuration = rDuration;
        }

        /// <summary>
        /// Causes the camera to shake for the specified duration
        /// </summary>
        /// <param name="rRange">Distance the shake will apply</param>
        /// <param name="rStrengthX">Multiplier to the x-movement of the shake</param>
        /// <param name="rStrengthY">Multiplier to the y-movement of the shake</param>
        /// <param name="rDuration">Time (in seconds) to shake</param>
        public void Shake(float rRange, float rStrengthX, float rStrengthY, float rDuration)
        {
            mShakeElapsed = 0f;
            mShakeSpeedFactor = 1f;
            mShakeStrengthX = rStrengthX;
            mShakeStrengthY = rStrengthY;
            mShakeRange = rRange;
            mShakeDuration = rDuration;
        }

        /// <summary>
        /// Clears out any target we're moving to
        /// </summary>
        public override void ClearTargetYawPitch()
        {
            for (int i = 0; i < Motors.Count; i++)
            {
                YawPitchMotor lMotor = Motors[i] as YawPitchMotor;
                if (lMotor != null) { lMotor.ClearTargetYawPitch(); }
            }
        }

        /// <summary>
        /// Causes us to ignore user input and force the camera to the specified localangles
        /// </summary>
        /// <param name="rYaw">Target local yaw</param>
        /// <param name="rPitch">Target local pitch</param>
        /// <param name="rSpeed">Degrees per second we'll rotate. A value of -1 uses the current yaw speed.</param>
        /// <param name="rAutoClearTarget">Determines if we'll clear the target once we reach it.</param>
        public override void SetTargetYawPitch(float rYaw, float rPitch, float rSpeed = -1f, bool rAutoClearTarget = true)
        {
            for (int i = 0; i < Motors.Count; i++)
            {
                YawPitchMotor lMotor = Motors[i] as YawPitchMotor;
                if (lMotor != null && lMotor.IsActive) { lMotor.SetTargetYawPitch(rYaw, rPitch, rSpeed, rAutoClearTarget); }
            }
        }

        /// <summary>
        /// Clears the forward direction target we're trying to reach
        /// </summary>
        public override void ClearTargetForward()
        {
            for (int i = 0; i < Motors.Count; i++)
            {
                YawPitchMotor lMotor = Motors[i] as YawPitchMotor;
                if (lMotor != null) { lMotor.ClearTargetForward(); }
            }
        }

        /// <summary>
        /// Causes us to ignore user input and force the camera to a specific direction.
        /// </summary>
        /// <param name="rForward">Forward direction the camera should look.</param>
        /// <param name="rSpeed">Speed at which we get there. A value of -1 uses the current yaw speed.</param>
        /// <param name="rAutoClearTarget">Determines if we'll clear the target once we reach it.</param>
        public override void SetTargetForward(Vector3 rForward, float rSpeed = -1f, bool rAutoClearTarget = true)
        {
            for (int i = 0; i < Motors.Count; i++)
            {
                YawPitchMotor lMotor = Motors[i] as YawPitchMotor;
                if (lMotor != null) { lMotor.SetTargetForward(rForward, rSpeed, rAutoClearTarget); }
            }
        }

        /// <summary>
        /// Update logic that can be called during LateUpdate (default) or FixedUpdate
        /// </summary>
        protected override void InternalUpdate()
        {
            if (!_IsInternalUpdateEnabled) { return; }

            // Allow the internal update to continue
            base.InternalUpdate();

            // Track our position and rotation for the next frame
            mLastPosition = _Transform.position;
            mLastRotation = _Transform.rotation;
        }

        /// <summary>
        /// LateUpdate logic for the controller should be done here. This allows us
        /// to support dynamic and fixed update times
        /// </summary>
        /// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
        /// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
        public override void RigLateUpdate(float rDeltaTime, int rUpdateIndex)
        {
            CameraMotor lActiveMotor = ActiveMotor;

            // Update the tilt before we do anything and determine tilt changes
            float lTiltAngle = UpdateTilt();

            // Cycle through the motors and determine if one should auto-activate
            int lNextMotorIndex = -1;
            for (int i = 0; i < Motors.Count; i++)
            {
                if (i == _ActiveMotorIndex) { continue; }

                bool lActivate = Motors[i].TestActivate(lActiveMotor);

                // Send the message
                CameraMessage lMessage = CameraMessage.Allocate();
                lMessage.ID = EnumMessageID.MSG_CAMERA_MOTOR_TEST;
                lMessage.Motor = Motors[_ActiveMotorIndex];
                lMessage.Continue = true;

                if (lActivate && MotorTestActivateEvent != null)
                {
                    MotorActivatedEvent.Invoke(lMessage);
                    lActivate = lMessage.Continue;
                }

#if USE_MESSAGE_DISPATCHER || OOTII_MD
                if (lActivate)
                {
                    lActivate = lMessage.Continue;
                }
#endif

                CameraMessage.Release(lMessage);

                if (lActivate)
                {
                    if (lNextMotorIndex < 0 || Motors[i].Priority >= Motors[lNextMotorIndex].Priority)
                    {
                        lNextMotorIndex = i;
                    }
                }
            }

            // Activate the new motor if it's time
            if (lNextMotorIndex >= 0)
            {
                ActivateMotor(lNextMotorIndex);
                lActiveMotor = Motors[lNextMotorIndex];
            }

            // If we don't have an active motor, no need to continue
            if (lActiveMotor == null) { return; }

            // Grab the latest transform
            CameraTransform lTransform = lActiveMotor.RigLateUpdate(rDeltaTime, rUpdateIndex, lTiltAngle);

            //if (lActiveMotor is YawPitchMotor)
            //{
            //    YawPitchMotor lActiveYawPitch = lActiveMotor as YawPitchMotor;
            //    Utilities.Debug.Log.ScreenWrite("                    l-yaw:" + lActiveYawPitch.LocalYaw.ToString("f4") + " l-pitch:" + lActiveYawPitch.LocalPitch.ToString("f4"), 1);
            //}


            // Test for collisions and adjust based on them
            UpdateCollisions(rDeltaTime, ref lTransform);

            // Apply the results
            _Transform.position = lTransform.Position;
            _Transform.rotation = lTransform.Rotation;

            // Update the FOV zoom
            UpdateZoom(rDeltaTime);

            // Update character fading
            UpdateFade(rDeltaTime);

            // Update the main camera's position to handle the shake
            UpdateShake(rDeltaTime);

            // If somone has tapped into the event, fire it
            if (MotorUpdated != null) { MotorUpdated(lActiveMotor); }

            // If the internal update is enabled, than we need to run the post logic here
            if (_IsInternalUpdateEnabled)
            {
                // Call out to our events if needed
                if (mOnPostLateUpdate != null) { mOnPostLateUpdate(rDeltaTime, mUpdateIndex, this); }

                // Allow the active motor to responed
                lActiveMotor.PostRigLateUpdate();
            }

#if UNITY_EDITOR

            if (EditorShowDebug)
            {
                if (lActiveMotor != null)
                {
                    Graphics.GraphicsManager.DrawPoint(lActiveMotor.AnchorPosition, Color.magenta);
                }
            }

#endif
        }

        /// <summary>
        /// Delegate callback for handling the camera movement AFTER the character controller
        /// </summary>
        /// <param name="rController">Character controller who is the anchor</param>
        /// <param name="rDeltaTime">Delta time used by the character controller</param>
        /// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
        protected void OnControllerLateUpdate(ICharacterController rController, float rDeltaTime, int rUpdateIndex)
        {
            // Set the controller who owns the camera
            mCharacterController = rController;

            // Update the camera and its motoers
            RigLateUpdate(rDeltaTime, rUpdateIndex);

            // Call out to our events if needed
            if (mOnPostLateUpdate != null) { mOnPostLateUpdate(rDeltaTime, mUpdateIndex, this); }

            // Allow the active motor to responed
            if (_ActiveMotorIndex >= 0 && _ActiveMotorIndex < Motors.Count)
            {
                Motors[_ActiveMotorIndex].PostRigLateUpdate();
            }

            // Disable the force
            _FrameForceToFollowAnchor = false;

            // Track our position and rotation for the next frame
            mLastPosition = _Transform.position;
            mLastRotation = _Transform.rotation;
        }

        /// <summary>
        /// Processes the motor definitions and updates the motors to match the definitions.
        /// </summary>
        public void InstantiateMotors()
        {
            int lMotorDefCount = MotorDefinitions.Count;

            // First, remove any extra motors that may exist
            for (int i = Motors.Count - 1; i >= lMotorDefCount; i--)
            {
                Motors.RemoveAt(i);
            }

            // We need to match the motor definitions to the motors
            for (int i = 0; i < lMotorDefCount; i++)
            {
                string lDefinition = MotorDefinitions[i];
                JSONNode lDefinitionNode = JSONNode.Parse(lDefinition);
                if (lDefinitionNode == null) { continue; }

                CameraMotor lMotor = null;

                string lTypeString = lDefinitionNode["Type"].Value;

                Type lType = Type.GetType(lTypeString);
                if (lType == null) { continue; }

                // If don't have a motor matching the type, we need to create one
                if (Motors.Count <= i || lTypeString != Motors[i].GetType().AssemblyQualifiedName)
                {
                    lMotor = Activator.CreateInstance(lType) as CameraMotor;
                    lMotor.RigController = this;

                    if (Motors.Count <= i)
                    {
                        Motors.Add(lMotor);
                    }
                    else
                    {
                        Motors[i] = lMotor;
                    }
                }
                // Grab the matching motor
                else
                {
                    lMotor = Motors[i];
                }

                // Fill the motor with data from the definition
                if (lMotor != null)
                {
                    lMotor.DeserializeMotor(lDefinition);
                }
            }

            // Allow each motion to initialize now that his has been deserialized
            for (int i = 0; i < Motors.Count; i++)
            {
                Motors[i].Awake();
            }

            // If we're running, initialize the motors
            if (Application.isPlaying)
            {
                // Ensure we have the active motor
                if (_ActiveMotorIndex >= 0 && _ActiveMotorIndex < Motors.Count)
                {
                    Motors[_ActiveMotorIndex].Activate(null);
                }
            }
        }

        /// <summary>
        /// Grab the tilt that matches our anchor
        /// </summary>
        /// <returns>Float that is the tilt angle change</returns>
        private float UpdateTilt()
        {
            float lFrameTiltAngle = 0f;

            if (_Anchor != null)
            {
                // Update our tilt to match the anchor's tilt. We do the tilt in increments so we
                // don't have any crazy 180 degree tilts.
                Vector3 lTiltUp = mTilt.Up();
                Quaternion lTiltDelta = QuaternionExt.FromToRotation(lTiltUp, _Anchor.up);
                if (!QuaternionExt.IsIdentity(lTiltDelta))
                {
                    lFrameTiltAngle = Vector3.Angle(lTiltUp, _Anchor.up);

                    mTilt = lTiltDelta * mTilt;
                }

                // Grab the angular difference between our up and the natural up. If there
                // is no difference, we can clean up the tilt some.
                float lTiltAngle = Vector3.Angle(mTilt.Up(), Vector3.up);
                if (lTiltAngle < 0.0001f)
                {
                    if (!QuaternionExt.IsIdentity(mTilt))
                    {
                        mTilt = Quaternion.identity;
                    }
                }
            }

            return lFrameTiltAngle;
        }

        Vector3 zoomPos, targetZoomPos, currentVelocity;
        /// <summary>
        /// Manages the FOV zooming
        /// </summary>
        /// <param name="rDeltaTime">Delta time </param>
        private void UpdateZoom(float rDeltaTime)
        {
            if (_IsZoomEnabled && _ZoomActionAlias.Length > 0 && Anchor)
            {
                zoomPos = Anchor.position;
                if (_ZoomResetOnRelease && mInputSource.IsJustReleased(_ZoomActionAlias))
                {
                    mTargetFOV = mOriginalFOV;
                    targetZoomPos = zoomPos;
                }
                else
                {
                    float lZoomMax = (_ZoomMax > 0 ? _ZoomMax : mOriginalFOV);

                    float lFrameFieldOfView = -mInputSource.GetValue(_ZoomActionAlias) * _ZoomSpeed;
                    mTargetFOV = Mathf.Clamp(mTargetFOV + lFrameFieldOfView, _ZoomMin, lZoomMax);
                    targetZoomPos = zoomPos + _Camera.transform.forward.normalized * lFrameFieldOfView;
                }
            }
            else if (mTargetFOV != mOriginalFOV)
            {
                mTargetFOV = mOriginalFOV;
            }

            if (_IsZoomEnabled && Anchor)
            {
                CameraMotor activeMotor = ActiveMotor;
                if (activeMotor.Name == "1st Person View")
                {
                    Anchor.position = Vector3.SmoothDamp(Anchor.position, targetZoomPos, ref currentVelocity, _ZoomSmoothing);
                }
                else if (activeMotor.Name == "Targeting" || activeMotor.Name == "3rd Person Fixed")
                {

                }
                if (mInputSource.IsPressed(KeyCode.Mouse2))
                {
                    float deltaX = mInputSource.GetValue("Mouse X")* activeMotor.Distance*0.01f;
                    float deltaY = mInputSource.GetValue("Mouse Y") * activeMotor.Distance * 0.01f;
                    Anchor.Translate(new Vector3(-deltaX, -deltaY, 0), Space.Self);
                }
            }

            //if (_IsZoomEnabled && Mathf.Abs(mTargetFOV - _Camera.fieldOfView) > 0.001f)
            //{
            //    _Camera.fieldOfView = Mathf.SmoothDampAngle(_Camera.fieldOfView, mTargetFOV, ref mZoomVelocity, _ZoomSmoothing);
            //}
            //else
            //{
            //    mZoomVelocity = 0f;
            //}
        }

        /// <summary>
        /// Manage line of sight collisions
        /// </summary>
        /// <param name="rDeltaTime"></param>
        private void UpdateCollisions(float rDeltaTime, ref CameraTransform rTransform)
        {
            bool lIsColliding = false;

            CameraMotor lActiveMotor = ActiveMotor;

            float lFrameMovement = _CollisionRecoverySpeed * rDeltaTime;

            Vector3 lNewAnchorPosition;
            Vector3 lNewFocusPosition;
            if (lActiveMotor == null)
            {
                lNewAnchorPosition = _Anchor.position + (_Anchor.rotation * _AnchorOffset);
                lNewFocusPosition = _Anchor.position + (_Anchor.rotation * _AnchorOffset);
            }
            else
            {
                lNewAnchorPosition = lActiveMotor.AnchorPosition;
                lNewFocusPosition = lActiveMotor.GetFocusPosition(rTransform.Rotation);
            }

            // If our focus position isn't the same as our anchor position, we need to 
            // test collisions with that first. We may need to bring the focus position closer to the anchor.
            if (lNewFocusPosition != lNewAnchorPosition)
            {
                //Vector3 lToFocus = lNewFocusPosition - lNewAnchorPosition;
                //Vector3 lFocusDirection = lToFocus.normalized;
                //float lFocusDistance = lToFocus.magnitude;

                //RaycastHit lFocusHit;
                //if (RaycastExt.SafeSphereCast(lNewAnchorPosition, lFocusDirection, _CollisionRadius, out lFocusHit, lFocusDistance, _CollisionLayers, _Anchor))
                //{
                //    mHadCollided = true;
                //    lIsColliding = true;
                //    lNewFocusPosition = lNewAnchorPosition + (lFocusDirection * (lFocusHit.distance - 0.05f));
                //}
            }

            float lOldDistance = Vector3.Distance(_Transform.position, lNewFocusPosition);

            Vector3 lToCamera = rTransform.Position - lNewFocusPosition;
            Vector3 lNewDirection = lToCamera.normalized;
            float lNewDistance = lToCamera.magnitude;

            // Check if our view is obstructed from the head to the camera
            if (_IsCollisionsEnabled && (lActiveMotor == null || lActiveMotor.IsCollisionEnabled))
            {
                RaycastHit lViewHit;
                if (RaycastExt.SafeSphereCast(lNewFocusPosition, lNewDirection, _CollisionRadius, out lViewHit, lNewDistance, _CollisionLayers, _Anchor))
                {
                    mActualDistance = lViewHit.distance;

                    // When distance == 0, that means our sphere cast start with an intrusion. So, 
                    // we change it to a raycast
                    if (mActualDistance == 0f)
                    {
                        if (RaycastExt.SafeRaycast(lNewFocusPosition, lNewDirection, out lViewHit, lNewDistance, _CollisionLayers, _Anchor))
                        {
                            mActualDistance = lViewHit.distance;
                        }
                    }

                    if (mActualDistance > 0f)
                    {
                        float lDelta = mActualDistance - lOldDistance;
                        if (lDelta <= lFrameMovement)
                        {
                            mActualDistance = Mathf.Max(mActualDistance, _MinCollisionDistance);

                            mHadCollided = true;
                            lIsColliding = true;
                            rTransform.Position = lNewFocusPosition + (lNewDirection * mActualDistance);
                        }
                    }
                }
            }

            // If we haven't collided, we may need to move back to the desired position slowly
            if (!lIsColliding && mHadCollided)
            {
                // Determine if we need to recover using our speed
                float lDelta = lNewDistance - lOldDistance;
                if (lDelta - EPSILON > lFrameMovement)
                {
                    mActualDistance = lOldDistance + (_CollisionRecoverySpeed * rDeltaTime);
                    rTransform.Position = lNewFocusPosition + (lNewDirection * mActualDistance);
                }
                // No recovery needed
                else
                {
                    mHadCollided = false;
                }
            }

            // Store the final distance
            if (mActualDistance == 0f) { mActualDistance = lActiveMotor.Distance; }

#if UNITY_EDITOR

            if (EditorShowDebug)
            {
                Graphics.GraphicsManager.DrawCapsule(lNewFocusPosition, rTransform.Position, _CollisionRadius, Color.green);
            }

#endif
        }

        /// <summary>
        /// Manages the shake and returns the position offset for the camera inside the rig
        /// </summary>
        /// <returns></returns>
        private void UpdateShake(float rDeltaTime)
        {
            Vector3 lCameraPosition = Vector3.zero;

            if (mShakeDuration > 0f)
            {
                mShakeElapsed = mShakeElapsed + (rDeltaTime * mShakeSpeedFactor);

                float lDuration = Mathf.Clamp01(mShakeElapsed / mShakeDuration);
                if (lDuration < 1f)
                {
                    float lStrength = _ShakeStrength.Evaluate(lDuration);
                    lCameraPosition.x = (((float)NumberHelper.Randomizer.NextDouble() * 2f) - 1f) * mShakeRange * mShakeStrengthX * lStrength;
                    lCameraPosition.y = (((float)NumberHelper.Randomizer.NextDouble() * 2f) - 1f) * mShakeRange * mShakeStrengthY * lStrength;
                }
                else
                {
                    mShakeElapsed = 0f;
                    mShakeDuration = 0f;
                }

                _Camera.transform.localPosition = lCameraPosition;
            }
        }

        /// <summary>
        /// Causes the anchor to fade over time if the camera is too close
        /// </summary>
        /// <param name="rDelta"></param>
        private void UpdateFade(float rDelta)
        {
            Vector3 lCameraPosition = _Transform.position;
            Vector3 lFocusPosition;

            CameraMotor lActiveMotor = ActiveMotor;
            if (lActiveMotor == null)
            {
                lFocusPosition = _Anchor.position + (_Anchor.rotation * _AnchorOffset);
            }
            else
            {
                lFocusPosition = lActiveMotor.GetFocusPosition(_Transform.rotation);
            }

            // Determine the fade state
            float lCameraDistance = Vector3.Distance(lCameraPosition, lFocusPosition);
            if (lCameraDistance < _FadeDistance)
            {
                mAlphaStart = mAlpha;
                mAlphaEnd = 0f;
            }
            else
            {
                mAlphaStart = mAlpha;
                mAlphaEnd = 1f;
            }

            if (mAlpha != mAlphaEnd)
            {
                mAlphaElapsed = mAlphaElapsed + Time.deltaTime;
                mAlpha = NumberHelper.SmoothStep(mAlphaStart, mAlphaEnd, (_FadeSpeed > 0f ? mAlphaElapsed / _FadeSpeed : 1f));

                if (_IsFadeEnabled) { SetAnchorAlpha(mAlpha); }
            }
            else
            {
                mAlphaElapsed = 0f;
                mAlphaStart = mAlpha;
            }
        }

        /// <summary>
        /// Fades in/out the anchor
        /// </summary>
        /// <param name="rAlpha"></param>
        protected void SetAnchorAlpha(float rAlpha)
        {
            if (_Anchor == null) { return; }
            if (!Application.isPlaying) { return; }

            if (_IsFadeEnabled && (ActiveMotor == null || ActiveMotor.IsFadingEnabled))
            {
                Renderer[] lAllRenderers = _Anchor.gameObject.GetComponentsInChildren<Renderer>();

                for (int i = 0; i < lAllRenderers.Length; i++)
                {
                    float lAlpha = rAlpha;
                    bool lIsEnabled = (lAlpha > 0f ? true : false);

                    // Test if we should control this renderer
                    Renderer lRenderer = lAllRenderers[i];
                    if (!_DisableRenderers && !lRenderer.enabled) { continue; }

                    // If we're specifying renderers, check if this is part of the list.
                    // If not, we'll force it back on just incase another motor disaled it.
                    if (ActiveMotor.SpecifyFadeRenderers)
                    {
                        if (!ActiveMotor.IsFadeRenderer(lRenderer.transform))
                        {
                            lAlpha = 1f;
                            lIsEnabled = true;
                        }
                    }

                    // Change the alpha value
                    Material[] lMaterials = lRenderer.materials;
                    for (int j = 0; j < lMaterials.Length; j++)
                    {
                        if (lMaterials[j].HasProperty("_Color"))
                        {
                            Color lColor = lMaterials[j].color;
                            lColor.a = lAlpha;

                            lMaterials[j].color = lColor;
                        }
                    }

                    // Enable/disable the renderer as needed
                    if (_DisableRenderers)
                    {
                        lRenderer.enabled = lIsEnabled;
                    }
                }
            }
        }

        // **************************************************************************************************
        // Following properties and function only valid while editing
        // **************************************************************************************************

#if UNITY_EDITOR

        /// <summary>
        /// Determines if we show debug information
        /// </summary>
        public bool EditorShowDebug = false;

        /// <summary>
        /// Tab that the editor is on
        /// </summary>
        public int EditorTabIndex = 0;

        /// <summary>
        /// Allows us to re-open the last selected motor
        /// </summary>
        public int EditorMotorIndex = 0;

        /// <summary>
        /// Force the inspector to refresh
        /// </summary>
        public bool EditorRefresh = false;

        /// <summary>
        /// Tracks what adventure style was last chosen
        /// </summary>
        public int EditorCameraStyle = -1;

        /// <summary>
        /// Tracks if we should use adventure style (with 3rd person camera)
        /// </summary>
        public bool EditorUseAdventureStyle = true;

        /// <summary>
        /// Show the events section in the editor
        /// </summary>
        public bool EditorShowEvents = false;

        /// <summary>
        /// Allow the motor to render it's own GUI debug GUI
        /// </summary>
        public virtual bool OnDebugInspectorGUI()
        {
            bool lIsDirty = false;


            return lIsDirty;
        }

#endif

    }
}
