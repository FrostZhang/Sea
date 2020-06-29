using System.Collections.Generic;
using UnityEngine;
using com.ootii.Actors;
using com.ootii.Base;
using com.ootii.Geometry;
using com.ootii.Helpers;
using com.ootii.Input;
using com.ootii.Timing;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Cameras
{
    /// <summary>
    /// Orbit motor
    /// </summary>
    [BaseName("Transition")]
    [BaseDescription("Motor that transitions from one motor to another. The 'Transition' motor does the transition while the 'End' motor is the result.")]
    public class TransitionMotor : CameraMotor
    {
        /// <summary>
        /// Used for the dropdown
        /// </summary>
        private static string[] ActionAliasEventTypes = new string[] { "Key down", "Key up" };

        /// <summary>
        /// Determines how we blend positions and rotations
        /// </summary>
        public static string[] BlendTypes = new string[] { "Active Start", "Static Start", "Start Only", "End Only" };

        /// <summary>
        /// Comparitors for distanced
        /// </summary>
        public static string[] NumericComparisonTypes = new string[] { "<", ">", "=" };

        /// <summary>
        /// Index of the motor that will do the transition
        /// </summary>
        public int _StartMotorIndex = 0;
        public int StartMotorIndex
        {
            get { return _StartMotorIndex; }
            set { _StartMotorIndex = value; }
        }

        /// <summary>
        /// Index of the motor we will end with (once the transition completes)
        /// </summary>
        public int _EndMotorIndex = 0;
        public int EndMotorIndex
        {
            get { return _EndMotorIndex; }
            set { _EndMotorIndex = value; }
        }

        /// <summary>
        /// Once we're done transitioning to EndMotorIndex, we can use this override to jump to a new idnex
        /// </summary>
        public int _EndMotorIndexOverride = -1;
        public int EndMotorIndexOverride
        {
            get { return _EndMotorIndexOverride; }
            set { _EndMotorIndexOverride = value; }
        }

        /// <summary>
        /// Determines how we'll blend the position
        /// </summary>
        public int _PositionBlend = 0;
        public int PositionBlend
        {
            get { return _PositionBlend; }
            set { _PositionBlend = value; }
        }

        /// <summary>
        /// Determines how we'll blend the rotation
        /// </summary>
        public int _RotationBlend = 0;
        public int RotationBlend
        {
            get { return _RotationBlend; }
            set { _RotationBlend = value; }
        }

        /// <summary>
        /// Time that it takes to transition
        /// </summary>
        public float _TransitionTime = 0.5f;
        public float TransitionTime
        {
            get { return _TransitionTime; }
            set { _TransitionTime = value; }
        }

        /// <summary>
        /// Determines how we process the input
        /// </summary>
        public int _ActionAliasEventType = 0;
        public int ActionAliasEventType
        {
            get { return _ActionAliasEventType; }
            set { _ActionAliasEventType = value; }
        }

        /// <summary>
        /// Determines if the transition will only occur when the start motor is active
        /// </summary>
        public bool _LimitToStart = true;
        public bool LimitToStart
        {
            get { return _LimitToStart; }
            set { _LimitToStart = value; }
        }

        /// <summary>
        /// Determines if we'll test the distance
        /// </summary>
        public bool _TestDistance = false;
        public bool TestDistance
        {
            get { return _TestDistance; }
            set { _TestDistance = value; }
        }

        /// <summary>
        /// Comma delimited string of stance IDs that the camera will work for. An empty string means all.
        /// </summary>
        public string _ActorStances = "";
        public string ActorStances
        {
            get { return _ActorStances; }
            
            set
            {
                _ActorStances = value;

                if (_ActorStances.Length == 0)
                {
                    if (mActorStances != null)
                    {
                        mActorStances.Clear();
                    }
                }
                else
                {
                    if (mActorStances == null) { mActorStances = new List<int>(); }
                    mActorStances.Clear();

                    int lStanceID = 0;
                    string[] lStanceIDs = _ActorStances.Split(',');
                    for (int i = 0; i < lStanceIDs.Length; i++)
                    {
                        if (int.TryParse(lStanceIDs[i], out lStanceID))
                        {
                            if (!mActorStances.Contains(lStanceID))
                            {
                                mActorStances.Add(lStanceID);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determines if we'll verify and auto-activate based on not being in the actor stances
        /// </summary>
        public bool _InvertVerifyActorStances = false;
        public bool InvertVerifyActorStances
        {
            get { return _InvertVerifyActorStances; }
            set { _InvertVerifyActorStances = value; }
        }

        /// <summary>
        /// Determines how we'll compare distance
        /// </summary>
        public int _DistanceCompareType = 0;
        public int DistanceCompareType
        {
            get { return _DistanceCompareType; }
            set { _DistanceCompareType = value; }
        }

        /// <summary>
        /// Determines how we'll compare distance
        /// </summary>
        public float _DistanceValue = 0.5f;
        public float DistanceValue
        {
            get { return _DistanceValue; }
            set { _DistanceValue = value; }
        }

        /// <summary>
        /// Motor we are transitioning from
        /// </summary>
        protected CameraMotor mStartMotor = null;

        /// <summary>
        /// Motor we are transitioning to
        /// </summary>
        protected CameraMotor mEndMotor = null;

        /// <summary>
        /// Track the position when the tranistion starts
        /// </summary>
        protected Vector3 mStartPosition = Vector3.zero;

        /// <summary>
        /// Track the rotation when the tranistion starts
        /// </summary>
        protected Quaternion mStartRotation = Quaternion.identity;

        /// <summary>
        /// We have to toggle this way for axis
        /// </summary>
        protected bool mIsActionAliasInUse = false;
        protected bool mWasActionAliasInUse = false;

        /// <summary>
        /// Actor stances we'll check to see if we can transition
        /// </summary>
        protected List<int> mActorStances = null;

        /// <summary>
        /// Elapsed time of the transition
        /// </summary>
        protected float mTransitionElapsedTime = 0f;
        public float TransitionElapsedTime
        {
            get { return mTransitionElapsedTime; }
        }

        /// <summary>
        /// Called when the motor is instantiated
        /// </summary>
        public override void Awake()
        {
            base.Awake();
        }

        /// <summary>
        /// Called to initialize the motor. This can be done multiple times to
        /// reset and prepare the motor for activation.
        /// </summary>
        public override bool Initialize()
        {
            // Set the transition motors
            mStartMotor = null;
            if (_StartMotorIndex < 0)
            {
                mStartMotor = RigController.ActiveMotor;
            }
            else if (_StartMotorIndex < RigController.Motors.Count)
            {
                mStartMotor = RigController.Motors[_StartMotorIndex];
            }

            if (mStartMotor == this) { mStartMotor = null; }

            mEndMotor = null;
            if (_EndMotorIndex >= 0 && _EndMotorIndex < RigController.Motors.Count)
            {
                mEndMotor = RigController.Motors[_EndMotorIndex];
            }

            if (mEndMotor == this) { mEndMotor = null; }

            // Determine the current time
            float lPrevElapsedPercent = 1f;

            if (RigController.ActiveMotor is TransitionMotor)
            {
                TransitionMotor lMotor = RigController.ActiveMotor as TransitionMotor;
                lPrevElapsedPercent = (lMotor.TransitionTime > 0f ? lMotor.TransitionElapsedTime / lMotor.TransitionTime : 1f);
            }
            else if (mStartMotor is YawPitchMotor)
            {
                YawPitchMotor lMotor = mStartMotor as YawPitchMotor;
                if (lMotor.MaxDistance == 0)
                {
                    lPrevElapsedPercent = 1f;
                }
                else
                {
                    lPrevElapsedPercent = NumberHelper.SmoothStepTime(0f, lMotor.MaxDistance, lMotor.Distance);
                }
            }
            else if (mEndMotor is YawPitchMotor)
            {
                YawPitchMotor lMotor = mEndMotor as YawPitchMotor;
                if (lMotor.MaxDistance == 0)
                {
                    lPrevElapsedPercent = 1f;
                }
                else
                {
                    lPrevElapsedPercent = NumberHelper.SmoothStepTime(0f, lMotor.MaxDistance, lMotor.Distance);
                }
            }

            mTransitionElapsedTime = _TransitionTime * (1f - lPrevElapsedPercent);

            // Initialize the motors
            if (mStartMotor != null && mEndMotor != null)
            {
                mStartMotor.Initialize();
                mEndMotor.Initialize();

                mStartPosition = RigController._Transform.position;
                mStartRotation = RigController._Transform.rotation;

                return base.Initialize();
            }

            return false;
        }

        /// <summary>
        /// Used to determine if the motor should activate
        /// </summary>
        /// <param name="rActiveMotor">Current active motor</param>
        /// <returns></returns>
        public override bool TestActivate(CameraMotor rActiveMotor)
        {
            UpdateToggle();

            if (!_IsEnabled) { return false; }

            if (_LimitToStart && RigController.ActiveMotorIndex != _StartMotorIndex) { return false; }

            bool lActivate = false;

            // Priority activation
            //if (!_LimitToStart || RigController.ActiveMotorIndex == _StartMotorIndex)
            {
                if (_ActionAlias.Length > 0 && RigController.InputSource != null)
                {
                    if (_ActionAliasEventType == 0)
                    {
                        if (RigController.InputSource.IsJustPressed(_ActionAlias) || (mIsActionAliasInUse && !mWasActionAliasInUse))
                        {
                            lActivate = true;
                        }
                        else if (_LimitToStart && RigController.InputSource.IsPressed(_ActionAlias))
                        {
                            lActivate = true;
                        }
                    }
                    else if (_ActionAliasEventType == 1)
                    {
                        if (RigController.InputSource.IsJustReleased(_ActionAlias) || (!mIsActionAliasInUse && mWasActionAliasInUse))
                        {
                            lActivate = true;
                        }
                    }
                }
            }

            // Priority activation
            if (!lActivate && TestDistance && RigController.ActiveMotor != null)
            {
                if (DistanceCompareType == 0)
                {
                    if (RigController.ActiveMotor.Distance < DistanceValue)
                    {
                        lActivate = true;
                    }
                }
                else if (DistanceCompareType == 1)
                {
                    if (RigController.ActiveMotor.Distance > DistanceValue)
                    {
                        lActivate = true;
                    }
                }
                else
                {
                    if (Mathf.Abs(RigController.ActiveMotor.Distance - DistanceValue) < 0.001f)
                    {
                        lActivate = true;
                    }
                }
            }

#if OOTII_MC

            // Activation limiters
            if (lActivate)
            {
                if (mActorStances != null && mActorStances.Count > 0 && RigController.CharacterController != null)
                {
                    lActivate = false;

                    GameObject lGameObject = RigController.CharacterController.gameObject;

                    com.ootii.Actors.AnimationControllers.MotionController lMotionController = lGameObject.GetComponent<com.ootii.Actors.AnimationControllers.MotionController>();
                    if (lMotionController != null)
                    {
                        int lStance = lMotionController.Stance;
                        if (mActorStances.Contains(lStance))
                        {
                            lActivate = true;
                        }
                    }
                }
            }
            // See if we need to activate because we are no longer in a valid actor stance we should be in
            else if (_InvertVerifyActorStances && mActorStances != null && mActorStances.Count > 0 && RigController.CharacterController != null)
            {
                // Only do it if we are in the start motor
                //if (!_LimitToStart || RigController.ActiveMotorIndex == _StartMotorIndex)
                {
                    GameObject lGameObject = RigController.CharacterController.gameObject;

                    // Only do it if we are not in a required stance
                    com.ootii.Actors.AnimationControllers.MotionController lMotionController = lGameObject.GetComponent<com.ootii.Actors.AnimationControllers.MotionController>();
                    if (lMotionController != null)
                    {
                        int lStance = lMotionController.Stance;
                        if (!mActorStances.Contains(lStance))
                        {
                            lActivate = true;
                        }
                    }
                }
            }

#endif

            return lActivate;
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
            UpdateToggle();

            if (mStartMotor == null || mEndMotor == null) { return mRigTransform; }

            mTransitionElapsedTime += rDeltaTime;
            float lElapsedPercent = mTransitionElapsedTime / _TransitionTime;

            CameraTransform lStartResult = mStartMotor.RigLateUpdate(rDeltaTime, rUpdateIndex, rTiltAngle);
            CameraTransform lEndResult = mEndMotor.RigLateUpdate(rDeltaTime, rUpdateIndex, rTiltAngle);

            if (_PositionBlend == 1)
            {
                mRigTransform.Position = Vector3.Lerp(mStartPosition, lEndResult.Position, lElapsedPercent);
            }
            else if (_PositionBlend == 2)
            {
                mRigTransform.Position = lStartResult.Position;
            }
            else if (_PositionBlend == 3)
            {
                mRigTransform.Position = lEndResult.Position;
            }
            else
            {
                mRigTransform.Position = Vector3.Lerp(lStartResult.Position, lEndResult.Position, lElapsedPercent);
            }

            if (_RotationBlend == 1)
            {
                mRigTransform.Rotation = Quaternion.Slerp(mStartRotation, lEndResult.Rotation, lElapsedPercent);
            }
            else if (_RotationBlend == 2)
            {
                mRigTransform.Rotation = lStartResult.Rotation;
            }
            else if (_RotationBlend == 3)
            {
                mRigTransform.Rotation = lEndResult.Rotation;
            }
            else
            {
                mRigTransform.Rotation = Quaternion.Slerp(lStartResult.Rotation, lEndResult.Rotation, lElapsedPercent);
            }

            return mRigTransform;
        }

        /// <summary>
        /// Allows the motor to process after the camera and controller have completed
        /// </summary>
        public override void PostRigLateUpdate()
        {
            base.PostRigLateUpdate();

            if (mStartMotor != null) { mStartMotor.PostRigLateUpdate(); }
            if (mEndMotor != null) { mEndMotor.PostRigLateUpdate(); }

            if (mTransitionElapsedTime >= _TransitionTime)
            {
                int lNewMotorIndex = (_EndMotorIndexOverride >= 0 && _EndMotorIndex < RigController.Motors.Count ? _EndMotorIndexOverride : _EndMotorIndex);
                RigController.ActivateMotor(lNewMotorIndex);
            }
        }

        /// <summary>
        /// Update the toggle this way so we can handle axis values
        /// </summary>
        protected virtual void UpdateToggle()
        {
            if (_ActionAlias.Length > 0 && RigController.InputSource != null)
            {
                mWasActionAliasInUse = mIsActionAliasInUse;
                mIsActionAliasInUse = (RigController.InputSource.GetValue(_ActionAlias) != 0f);
            }
        }

        /// <summary>
        /// Given a JSON string that is the definition, we parse out the properties and set them
        /// </summary>
        /// <param name="rDefinition"></param>
        public override void DeserializeMotor(string rDefinition)
        {
            base.DeserializeMotor(rDefinition);

            ActorStances = _ActorStances;
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

            EditorGUILayout.BeginVertical(EditorHelper.Box);

            if (EditorHelper.IntField("Start Index", "Index of the motor we will use to transition from (-1 means current).", StartMotorIndex, RigController))
            {
                lIsDirty = true;
                StartMotorIndex = EditorHelper.FieldIntValue;
            }

            if (EditorHelper.IntField("End Index", "Index of the motor we will transition to.", EndMotorIndex, RigController))
            {
                lIsDirty = true;
                EndMotorIndex = EditorHelper.FieldIntValue;
            }

            if (EditorHelper.IntField("   Override", "If set, the motor index we'll jump to once the transition is complete. The transition itself will still use 'End Index'. Set to -1 to clear.", EndMotorIndexOverride, RigController))
            {
                lIsDirty = true;
                EndMotorIndexOverride = EditorHelper.FieldIntValue;
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorHelper.Box);

            if (EditorHelper.PopUpField("Position Blend", "Determines how we'll blend from the previous camera to the new camera.", PositionBlend, BlendTypes, RigController))
            {
                lIsDirty = true;
                PositionBlend = EditorHelper.FieldIntValue;
            }

            if (EditorHelper.PopUpField("Rotation Blend", "Determines how we'll blend from the previous camera to the new camera.", RotationBlend, BlendTypes, RigController))
            {
                lIsDirty = true;
                RotationBlend = EditorHelper.FieldIntValue;
            }

            if (EditorHelper.FloatField("Transition Time", "Time in seconds to transition to the new motor.", TransitionTime, RigController))
            {
                lIsDirty = true;
                TransitionTime = EditorHelper.FieldFloatValue;
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorHelper.Box);

            if (EditorHelper.BoolField("Test Only In Start", "Transition only activates while in the start motor.", LimitToStart, RigController))
            {
                lIsDirty = true;
                LimitToStart = EditorHelper.FieldBoolValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.TextField("Action Alias", "Input alias that will determine if this motor activates", ActionAlias, RigController))
            {
                lIsDirty = true;
                ActionAlias = EditorHelper.FieldStringValue;
            }

            if (EditorHelper.PopUpField("Input Type", "Determines how we'll test the input.", ActionAliasEventType, ActionAliasEventTypes, RigController))
            {
                lIsDirty = true;
                ActionAliasEventType = EditorHelper.FieldIntValue;
            }

#if OOTII_MC

            GUILayout.Space(5f);

            if (EditorHelper.TextField("Valid Actor Stances", "Comma delimited list of stance IDs that the transition will work in. Leave empty to ignore this condition.", ActorStances, RigController))
            {
                lIsDirty = true;
                ActorStances = EditorHelper.FieldStringValue;
            }

            if (EditorHelper.BoolField("Invert Validation", "Check if we're NOT in one of the valid stances to activate.", InvertVerifyActorStances, RigController))
            {
                lIsDirty = true;
                InvertVerifyActorStances = EditorHelper.FieldBoolValue;
            }

#endif

            EditorGUILayout.EndVertical();

            //GUILayout.Space(5f);

            //if (EditorHelper.BoolField("Test Distance", "Determines if we'll test the distance in activate the transition.", TestDistance, RigController))
            //{
            //    lIsDirty = true;
            //    TestDistance = EditorHelper.FieldBoolValue;
            //}

            //if (TestDistance)
            //{
            //    if (EditorHelper.PopUpField("Compare Type", "Determines how we'll compare the distance to the value below.", DistanceCompareType, NumericComparisonTypes, RigController))
            //    {
            //        lIsDirty = true;
            //        DistanceCompareType = EditorHelper.FieldIntValue;
            //    }

            //    if (EditorHelper.FloatField("Compare Value", "Value we're comparing the distance to.", DistanceValue, RigController))
            //    {
            //        lIsDirty = true;
            //        DistanceValue = EditorHelper.FieldFloatValue;
            //    }
            //}

            return lIsDirty;
        }

#endif

    }
}
