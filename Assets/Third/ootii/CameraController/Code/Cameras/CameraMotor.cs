using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using com.ootii.Base;
using com.ootii.Geometry;
using com.ootii.Helpers;
using com.ootii.Input;
using com.ootii.Timing;
using com.ootii.Utilities;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace com.ootii.Cameras
{
    /// <summary>
    /// Base class that drives the camera rig. The rig is responsible for
    /// holding the camera. Motors are used to move and rotate the rig.
    /// </summary>
    public abstract class CameraMotor : BaseObject
    {
        /// <summary>
        /// Tracks the qualified type of the motion
        /// </summary>
        public string _Type = "";
        public virtual string Type
        {
            get { return _Type; }
            set { _Type = value; }
        }

        /// <summary>
        /// Determines if the motion is enabled. If it is
        /// running and then disabled, the motion will finish
        /// </summary>
        public bool _IsEnabled = true;
        public virtual bool IsEnabled
        {
            get { return _IsEnabled; }
            set { _IsEnabled = value; }
        }

        /// <summary>
        /// Determines how important this motor is to other
        /// motors. The higher the priority, the higher the
        /// importance.
        /// </summary>
        public float _Priority = 0;
        public float Priority
        {
            get { return _Priority; }
            set { _Priority = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public string _ActionAlias = "";
        public string ActionAlias
        {
            get { return _ActionAlias; }
            set { _ActionAlias = value; }
        }

        /// <summary>
        /// Determines if the motion is currently active
        /// </summary>
        [NonSerialized]
        public bool _IsActive = false;
        public virtual bool IsActive
        {
            get { return _IsActive; }
        }

        /// <summary>
        /// Determines if we use the controller's default anchor our a custom one
        /// </summary>
        public bool _UseRigAnchor = true;
        public bool UseRigAnchor
        {
            get { return _UseRigAnchor; }
            set { _UseRigAnchor = value; }
        }
        
        /// <summary>
        /// INTERNAL ONLY: Index into the Camera Rig Controller's stored gameobjects
        /// </summary>
        public int _AnchorIndex = -1;
        public virtual int AnchorIndex
        {
            get { return _AnchorIndex; }

            set
            {
                _AnchorIndex = value;

                if (_AnchorIndex >= 0)
                {
                    if (RigController != null && _AnchorIndex < RigController.StoredTransforms.Count)
                    {
                        _Anchor = RigController.StoredTransforms[_AnchorIndex];
                    }
                }
            }
        }

        /// <summary>
        /// Anchor that we can use instead of the default
        /// </summary>
        [NonSerialized]
        public Transform _Anchor = null;
        public virtual Transform Anchor
        {
            get
            {
                if (_UseRigAnchor)
                {
                    if (RigController != null)
                    {
                        return RigController._Anchor;
                    }
                }
                else
                {
                    return _Anchor;
                }

                return null;
            }

            set
            {
                _Anchor = value;

#if UNITY_EDITOR

                if (!Application.isPlaying)
                {
                    if (RigController != null)
                    {
                        if (_Anchor == null)
                        {
                            if (_AnchorIndex >= 0 && _AnchorIndex < RigController.StoredTransforms.Count)
                            {
                                RigController.StoredTransforms[_AnchorIndex] = null;

                                // TRT 10/18/2016 - Can't actually do this because someone may reference it
                                //if (_AnchorIndex == RigController.StoredTransforms.Count - 1)
                                //{
                                //    RigController.StoredTransforms.RemoveAt(_AnchorIndex);
                                //    _AnchorIndex = -1;
                                //}
                            }
                        }
                        else
                        {
                            if (_AnchorIndex == -1)
                            {
                                _AnchorIndex = RigController.StoredTransforms.Count;
                                RigController.StoredTransforms.Add(null);
                            }

                            RigController.StoredTransforms[_AnchorIndex] = _Anchor;
                        }
                    }
                }

#endif

            }
        }

        /// <summary>
        /// Anchor that we can use instead of the default
        /// </summary>
        public virtual Transform AnchorRoot
        {
            get
            {
                Transform lAnchor = null;

                if (_UseRigAnchor)
                {
                    if (RigController != null)
                    {
                        lAnchor = RigController._Anchor;
                    }
                }
                else
                {
                    lAnchor = _Anchor;
                }

                if (lAnchor != null)
                {
                    IBaseCameraAnchor lCameraAnchor = lAnchor.gameObject.GetComponent<IBaseCameraAnchor>();
                    if (lCameraAnchor != null) { lAnchor = lCameraAnchor.Root; }
                }

                return lAnchor;
            }
        }

        /// <summary>
        /// Anchor offset we can use instead of the default
        /// </summary>
        public Vector3 _AnchorOffset = Vector3.zero;
        public virtual Vector3 AnchorOffset
        {
            get
            {
                if (_UseRigAnchor)
                {
                    if (RigController != null)
                    {
                        return RigController._AnchorOffset;
                    }
                }
                else
                {
                    return _AnchorOffset;
                }

                return Vector3.zero;
            }

            set { _AnchorOffset = value; }
        }

        /// <summary>
        /// Current position of the actual anchor
        /// </summary>
        public virtual Vector3 AnchorPosition
        {
            get
            {
                Transform lAnchor = Anchor;
                if (lAnchor == null)
                {
                    return AnchorOffset;
                }
                else
                {
                    if (RigController.RotateAnchorOffset)
                    {
                        return lAnchor.position + (lAnchor.rotation * AnchorOffset);
                    }
                    else
                    {
                        return lAnchor.position + AnchorOffset;
                    }
                }
            }
        }

        /// <summary>
        /// Offset that is applied after the RigController's AnchorOffset
        /// </summary>
        public Vector3 _Offset = Vector3.zero;
        public virtual Vector3 Offset
        {
            get { return _Offset; }
            set { _Offset = value; }
        }

        /// <summary>
        /// Max distance the camera can be from the focus
        /// </summary>
        public virtual float MaxDistance
        {
            get { return 20f; }
            set { }
        }

        /// <summary>
        /// Actual distance the camera is from the focus
        /// </summary>
        public virtual float Distance
        {
            get { return 20f; }
            set { }
        }

        /// <summary>
        /// Determines if this motor allows collisions
        /// </summary>
        public bool _IsCollisionEnabled = true;
        public virtual bool IsCollisionEnabled
        {
            get { return _IsCollisionEnabled; }
            set { _IsCollisionEnabled = value; }
        }

        /// <summary>
        /// Determines if this motor allows fading
        /// </summary>
        public bool _IsFadingEnabled = true;
        public virtual bool IsFadingEnabled
        {
            get { return _IsFadingEnabled; }
            set { _IsFadingEnabled = value; }
        }

        /// <summary>
        /// Determines if specific fade renderers are used
        /// </summary>
        public bool _SpecifyFadeRenderers = false;
        public bool SpecifyFadeRenderers
        {
            get { return _SpecifyFadeRenderers; }
            set { _SpecifyFadeRenderers = value; }
        }

        /// <summary>
        /// INTERNAL ONLY: Renderers that will be faded when fading is enabled
        /// </summary>       
        public List<int> _FadeTransformIndexes = new List<int>();
        public List<int> FadeTransformIndexes
        {
            get { return _FadeTransformIndexes; }
            set { _FadeTransformIndexes = value; }
        }
        
        /// <summary>
        /// Camera rig controller
        /// </summary>
        [NonSerialized]
        public CameraController RigController;

        /// <summary>
        /// Transform that defines the motor's resulting state
        /// </summary>
        [NonSerialized]
        protected CameraTransform mRigTransform;

        /// <summary>
        /// Collisions distance of the anchor offset
        /// </summary>
        protected float mAnchorOffsetDistance = 0f;

        /// <summary>
        /// Called when the motor is deserialized
        /// </summary>
        public virtual void Awake()
        {
            if (RigController != null && RigController.Camera != null)
            {
                mRigTransform.FieldOfView = RigController.Camera.fieldOfView;
            }

            // Ensure our offset distance starts off at the default
            mAnchorOffsetDistance = AnchorOffset.magnitude;

#if UNITY_EDITOR

            // Recreate our list for rendering
            InstantiateRendererList();

#endif
        }

        /// <summary>
        /// Called to initialize the motor. This can be done multiple times to
        /// reset and prepare the motor for activation.
        /// </summary>
        public virtual bool Initialize()
        {
            // Ensure our offset distance starts off at the default
            mAnchorOffsetDistance = AnchorOffset.magnitude;

            return true;
        }

        /// <summary>
        /// Used to determine if the motor should activate
        /// </summary>
        /// <param name="rActiveMotor">Current active motor</param>
        /// <returns></returns>
        public virtual bool TestActivate(CameraMotor rActiveMotor)
        {
            if (!_IsEnabled) { return false; }

            if (_ActionAlias.Length > 0 && RigController.InputSource != null)
            {
                if (RigController.InputSource.IsJustPressed(_ActionAlias))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Called when the motor is activated
        /// </summary>
        public virtual void Activate(CameraMotor rOldMotor)
        {
            bool lIsValid = Initialize();
            if (lIsValid) { _IsActive = true; }
        }

        /// <summary>
        /// Called when the motor is deactivated
        /// </summary>
        public virtual void Deactivate(CameraMotor rNewMotor)
        {
            _IsActive = false;
        }

        /// <summary>
        /// Clears all values and references stored with the motor
        /// </summary>
        public virtual void Clear()
        {
            Anchor = null;
            _FadeTransformIndexes.Clear();
        }

        /// <summary>
        /// Updates the motor over time. This is called by the controller
        /// every update cycle so movement can be updated. 
        /// </summary>
        /// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
        /// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
        /// <param name="rTiltAngle">Amount of tilting the camera needs to do to match the anchor</param>
        public virtual CameraTransform RigLateUpdate(float rDeltaTime, int rUpdateIndex, float rTiltAngle = 0f)
        {
            return mRigTransform;
        }

        /// <summary>
        /// Allows the motor to process after the camera and controller have completed
        /// </summary>
        public virtual void PostRigLateUpdate()
        {
            mAnchorOffsetDistance = Mathf.Min(mAnchorOffsetDistance + (1f * Time.deltaTime), AnchorOffset.magnitude);
        }

        /// <summary>
        /// Gets the focus position given the new/predicted camera rotation
        /// </summary>
        /// <param name="rCameraRotation"></param>
        /// <returns></returns>
        public virtual Vector3 GetFocusPosition(Quaternion rCameraRotation)
        {
            Transform lAnchorTransform = Anchor;

            Vector3 lAnchorOffset = AnchorOffset;
            Vector3 lAnchorPosition = AnchorPosition;
            Vector3 lNewFocusPosition = (rCameraRotation.Right() * _Offset.x) + (rCameraRotation.Forward() * _Offset.z);

            if (lAnchorTransform != null)
            {
                // We need to see if the AnchorPosition is reachable from the Anchor
                if (RigController.IsCollisionsEnabled && _IsCollisionEnabled && lAnchorOffset.sqrMagnitude > 0f)
                {
                    Vector3 lToAnchor = lAnchorPosition - lAnchorTransform.position;
                    Vector3 lAnchorDirection = lToAnchor.normalized;
                    float lAnchorDistance = lToAnchor.magnitude;

                    RaycastHit lFocusHit;
                    float lDistanceOffset = lAnchorDistance * 0.5f;
                    if (RaycastExt.SafeSphereCast(lAnchorTransform.position + (lAnchorDirection * lDistanceOffset), lAnchorDirection, RigController.CollisionRadius * 1.1f, out lFocusHit, lAnchorDistance - lDistanceOffset, RigController.CollisionLayers, AnchorRoot))
                    {
                        if (lFocusHit.distance > 0f)
                        {
                            mAnchorOffsetDistance = lFocusHit.distance + lDistanceOffset;
                        }
                    }

                    lAnchorOffset = lAnchorDirection * mAnchorOffsetDistance;
                    lNewFocusPosition = lAnchorTransform.position + lAnchorOffset + lNewFocusPosition;
                }
                else
                {
                    lNewFocusPosition = lAnchorTransform.position + ((RigController.RotateAnchorOffset ? lAnchorTransform.rotation : Quaternion.identity) * lAnchorOffset) + lNewFocusPosition;
                }

#if UNITY_EDITOR
                Vector3 lSegmentEnd = lNewFocusPosition;
#endif

                 lNewFocusPosition = lNewFocusPosition + ((RigController.RotateAnchorOffset ? lAnchorTransform.up : Vector3.up) * _Offset.y);

#if UNITY_EDITOR
                if (RigController.EditorShowDebug)
                {
                    Graphics.GraphicsManager.DrawCapsule(lAnchorTransform.position, lAnchorPosition, RigController.CollisionRadius * 1.1f, Color.white);
                    Graphics.GraphicsManager.DrawCapsule(lAnchorPosition, lSegmentEnd, RigController.CollisionRadius * 1.1f, Color.gray);
                    Graphics.GraphicsManager.DrawCapsule(lSegmentEnd, lNewFocusPosition, RigController.CollisionRadius * 1.1f, Color.gray);
                }
#endif
                //float lCameraDistance = Vector3.Distance(lNewFocusPosition, RigController._Transform.position);
                //float lCameraDistancePercent = Mathf.Clamp01(MaxDistance > 0f ? lCameraDistance / MaxDistance : 1f);
                //if (lCameraDistancePercent < 1f)
                //{
                //    Vector3 lToFocus = lNewFocusPosition - lAnchorPosition;
                //    lNewFocusPosition = lAnchorPosition + (lToFocus.normalized * (lToFocus.magnitude * lCameraDistancePercent));
                //}

                // We need to see if the focus position is reachable. If there
                // is a collision, we need to pull it in.
                if (RigController.IsCollisionsEnabled && _IsCollisionEnabled && Offset.sqrMagnitude > 0f)
                {
                    Vector3 lToFocus = lNewFocusPosition - lAnchorPosition;
                    Vector3 lFocusDirection = lToFocus.normalized;
                    float lFocusDistance = lToFocus.magnitude;

                    RaycastHit lFocusHit;
                    if (RaycastExt.SafeSphereCast(lAnchorPosition, lFocusDirection, RigController.CollisionRadius * 1.1f, out lFocusHit, lFocusDistance, RigController.CollisionLayers, AnchorRoot))
                    {
                        lNewFocusPosition = lAnchorPosition + (lFocusDirection * lFocusHit.distance);
                    }
                }
            }

#if UNITY_EDITOR
            if (RigController.EditorShowDebug)
            {
                Graphics.GraphicsManager.DrawPoint(lNewFocusPosition, Color.green);
            }
#endif

            return lNewFocusPosition;
        }

        /// <summary>
        /// Normalize the angle values
        /// </summary>
        /// <param name="rEuler"></param>
        public void NormalizeEuler(ref Vector3 rEuler)
        {
            if (rEuler.x < -180f) { rEuler.x = rEuler.x + 360f; }
            else if (rEuler.x > 180f) { rEuler.x = rEuler.x - 360f; }

            if (rEuler.y < -180f) { rEuler.y = rEuler.y + 360f; }
            else if (rEuler.y > 180f) { rEuler.y = rEuler.y - 360f; }
        }

        /// <summary>
        /// Determines if the transform is one of the stored transforms
        /// </summary>
        /// <param name="rTransform">Transform that holds the renderer to test</param>
        /// <returns>True if the transform is specified as a fade renderer or false if not</returns>
        public bool IsFadeRenderer(Transform rTransform)
        {
            if (!IsFadingEnabled) { return false; }
            if (!SpecifyFadeRenderers) { return false; }

            for (int i = 0; i < _FadeTransformIndexes.Count; i++)
            {
                int lIndex = _FadeTransformIndexes[i];
                if (lIndex >= 0 && lIndex < RigController.StoredTransforms.Count)
                {
                    if (RigController.StoredTransforms[lIndex] == rTransform)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Return the transform associated with the index
        /// </summary>
        /// <param name="rIndex">Index in the fade renderer array</param>
        /// <returns>Transform associated with the index or null</returns>
        public Transform GetFadeRenderer(int rIndex)
        {
            if (rIndex < 0 || rIndex >= _FadeTransformIndexes.Count) { return null; }

            int lIndex = _FadeTransformIndexes[rIndex];

            if (RigController.StoredTransforms == null) { return null; }
            if (lIndex < 0 || lIndex >= RigController.StoredTransforms.Count) { return null; }

            return RigController.StoredTransforms[lIndex];
        }

        /// <summary>
        /// Adds the transform to our list of renderers to fade
        /// </summary>
        /// <param name="rTransform">Transform whose renderers we will fade</param>
        public void AddFadeRenderer(Transform rTransform)
        {
            int lIndex = -1;

            // Check if the rig has stored the transform
            for (int i = 0; i < RigController.StoredTransforms.Count; i++)
            {
                if (RigController.StoredTransforms[i] == rTransform)
                {
                    lIndex = i;
                    break;
                }
            }

            // If not, store it
            if (lIndex < 0)
            {
                RigController.StoredTransforms.Add(rTransform);
                lIndex = RigController.StoredTransforms.Count - 1;
            }

            // If we don't include the transform, include it
            if (!_FadeTransformIndexes.Contains(lIndex))
            {
                _FadeTransformIndexes.Add(lIndex);
            }
        }

        /// <summary>
        /// Sets the renderer at the specified index
        /// </summary>
        /// <param name="rIndex">Index of the item to set</param>
        /// <param name="rTransform">Transform to set</param>
        public void SetFadeRenderer(int rIndex, Transform rTransform)
        {
            int lIndex = -1;

            // Ensure we have room
            while (rIndex >= _FadeTransformIndexes.Count)
            {
                _FadeTransformIndexes.Add(-1);
            }

            // Check if the rig has stored the transform
            for (int i = 0; i < RigController.StoredTransforms.Count; i++)
            {
                if (RigController.StoredTransforms[i] == rTransform)
                {
                    lIndex = i;
                    break;
                }
            }

            // If not, store it
            if (lIndex < 0)
            {
                RigController.StoredTransforms.Add(rTransform);
                lIndex = RigController.StoredTransforms.Count - 1;
            }

            // Set the index
            _FadeTransformIndexes[rIndex] = lIndex;
        }

        /// <summary>
        /// Removes the renderer from our list to fade
        /// </summary>
        /// <param name="rTransform"></param>
        public void RemoveFadeRenderer(Transform rTransform)
        {
            int lIndex = -1;

            // Check if the rig has stored the transform
            for (int i = 0; i < RigController.StoredTransforms.Count; i++)
            {
                if (RigController.StoredTransforms[i] == rTransform)
                {
                    lIndex = i;
                    break;
                }
            }

            // If it has, remove it from this list
            if (lIndex >= 0)
            {
                _FadeTransformIndexes.Remove(lIndex);
            }
        }

        /// <summary>
        /// Removes the index from our list to fade
        /// </summary>
        /// <param name="rIndex">Index in our list of fade renderers</param>
        public void RemoveFadeRenderer(int rIndex)
        {
            if (rIndex < 0 || rIndex >= _FadeTransformIndexes.Count) { return; }
            _FadeTransformIndexes.RemoveAt(rIndex);
        }

        /// <summary>
        /// Creates a JSON string that represents the motor's serialized state. We
        /// do this since Unity can't handle putting lists of derived objects into
        /// prefabs.
        /// </summary>
        /// <returns>JSON string representing the object</returns>
        public virtual string SerializeMotor()
        {
            if (_Type.Length == 0) { _Type = this.GetType().AssemblyQualifiedName; }

            StringBuilder lStringBuilder = new StringBuilder();
            lStringBuilder.Append("{");

            // These four properties are important from the base MotionControllerMotion
            lStringBuilder.Append(", \"Type\" : \"" + _Type + "\"");
            lStringBuilder.Append(", \"Name\" : \"" + _Name + "\"");
            lStringBuilder.Append(", \"IsEnabled\" : \"" + _IsEnabled.ToString() + "\"");
            lStringBuilder.Append(", \"ActionAlias\" : \"" + _ActionAlias.ToString() + "\"");
            lStringBuilder.Append(", \"UseRigAnchor\" : \"" + _UseRigAnchor.ToString() + "\"");
            lStringBuilder.Append(", \"AnchorIndex\" : \"" + _AnchorIndex.ToString() + "\"");
            lStringBuilder.Append(", \"AnchorOffset\" : \"" + _AnchorOffset.ToString("G8") + "\"");
            lStringBuilder.Append(", \"Offset\" : \"" + _Offset.ToString("G8") + "\"");
            lStringBuilder.Append(", \"Distance\" : \"" + Distance.ToString("f5") + "\"");
            lStringBuilder.Append(", \"MaxDistance\" : \"" + MaxDistance.ToString("f5") + "\"");
            lStringBuilder.Append(", \"IsCollisionEnabled\" : \"" + _IsCollisionEnabled.ToString() + "\"");
            lStringBuilder.Append(", \"IsFadingEnabled\" : \"" + _IsFadingEnabled.ToString() + "\"");
            lStringBuilder.Append(", \"SpecifyFadeRenderers\" : \"" + _SpecifyFadeRenderers.ToString() + "\"");
            lStringBuilder.Append(", \"FadeTransformIndexes\" : \"" + string.Join(",", _FadeTransformIndexes.Select(n => n.ToString()).ToArray()) + "\"");

            // Cycle through all the properties. 
            // Unfortunately Binding flags don't seem to be working. So,
            // we need to ensure we don't include base properties
            PropertyInfo[] lBaseProperties = typeof(CameraMotor).GetProperties();
            PropertyInfo[] lProperties = this.GetType().GetProperties();
            foreach (PropertyInfo lProperty in lProperties)
            {
                if (!lProperty.CanWrite) { continue; }

                bool lAdd = true;
                for (int i = 0; i < lBaseProperties.Length; i++)
                {
                    if (lProperty.Name == lBaseProperties[i].Name)
                    {
                        lAdd = false;
                        break;
                    }
                }

                if (!lAdd || lProperty.GetValue(this, null) == null) { continue; }

                object lValue = lProperty.GetValue(this, null);
                if (lProperty.PropertyType == typeof(Vector2))
                {
                    lStringBuilder.Append(", \"" + lProperty.Name + "\" : \"" + ((Vector2)lValue).ToString("G8") + "\"");
                }
                else if (lProperty.PropertyType == typeof(Vector3))
                {
                    lStringBuilder.Append(", \"" + lProperty.Name + "\" : \"" + ((Vector3)lValue).ToString("G8") + "\"");
                }
                else if (lProperty.PropertyType == typeof(Vector4))
                {
                    lStringBuilder.Append(", \"" + lProperty.Name + "\" : \"" + ((Vector4)lValue).ToString("G8") + "\"");
                }
                else if (lProperty.PropertyType == typeof(Quaternion))
                {
                    lStringBuilder.Append(", \"" + lProperty.Name + "\" : \"" + ((Quaternion)lValue).ToString("G8") + "\"");
                }
                else if (lProperty.PropertyType == typeof(Transform))
                {
                    // We use the _StoredTransformIndex for serialization
                }
                else if (lProperty.PropertyType == typeof(List<int>))
                {
                    List<int> lList = lValue as List<int>;
                    lStringBuilder.Append(", \"" + lProperty.Name + "\" : \"" + string.Join(",", lList.Select(n => n.ToString()).ToArray()) + "\"");
                }
                else if (lProperty.PropertyType == typeof(AnimationCurve))
                {
                    lStringBuilder.Append(", \"" + lProperty.Name + "\" : \"");

                    AnimationCurve lCurve = lValue as AnimationCurve;
                    for (int i = 0; i < lCurve.keys.Length; i++)
                    {
                        Keyframe lKey = lCurve.keys[i];
                        lStringBuilder.Append(lKey.time.ToString("f5") + "|" + lKey.value.ToString("f5") + "|" + lKey.tangentMode.ToString() + "|" + lKey.inTangent.ToString("f5") + "|" + lKey.outTangent.ToString("f5"));

                        if (i < lCurve.keys.Length - 1) { lStringBuilder.Append(";"); }
                    }

                    lStringBuilder.Append("\"");
                }
                else
                {
                    lStringBuilder.Append(", \"" + lProperty.Name + "\" : \"" + lValue.ToString() + "\"");
                }
            }

            lStringBuilder.Append("}");

            return lStringBuilder.ToString();
        }

        /// <summary>
        /// Gieven a JSON string that is the definition of the object, we parse
        /// out the properties and set them.
        /// </summary>
        /// <param name="rDefinition">JSON string</param>
        public virtual void DeserializeMotor(string rDefinition)
        {
            JSONNode lDefinitionNode = JSONNode.Parse(rDefinition);
            if (lDefinitionNode == null) { return; }

            // Cycle through the properties and load the values we can
            PropertyInfo[] lProperties = this.GetType().GetProperties();
            foreach (PropertyInfo lProperty in lProperties)
            {
                if (!lProperty.CanWrite) { continue; }
                if (lProperty.GetValue(this, null) == null) { continue; }

                JSONNode lValueNode = lDefinitionNode[lProperty.Name];
                if (lValueNode == null)
                {
                    if (lProperty.PropertyType == typeof(string))
                    {
                        lProperty.SetValue(this, "", null);
                    }

                    continue;
                }

                if (lProperty.PropertyType == typeof(string))
                {
                    lProperty.SetValue(this, lValueNode.Value, null);
                }
                else if (lProperty.PropertyType == typeof(int))
                {
                    lProperty.SetValue(this, lValueNode.AsInt, null);
                }
                else if (lProperty.PropertyType == typeof(float))
                {
                    lProperty.SetValue(this, lValueNode.AsFloat, null);
                }
                else if (lProperty.PropertyType == typeof(bool))
                {
                    lProperty.SetValue(this, lValueNode.AsBool, null);
                }
                else if (lProperty.PropertyType == typeof(Vector2))
                {
                    Vector2 lVector2Value = Vector2.zero;
                    lVector2Value = lVector2Value.FromString(lValueNode.Value);

                    lProperty.SetValue(this, lVector2Value, null);
                }
                else if (lProperty.PropertyType == typeof(Vector3))
                {
                    Vector3 lVector3Value = Vector3.zero;
                    lVector3Value = lVector3Value.FromString(lValueNode.Value);

                    lProperty.SetValue(this, lVector3Value, null);
                }
                else if (lProperty.PropertyType == typeof(Vector4))
                {
                    Vector4 lVector4Value = Vector4.zero;
                    lVector4Value = lVector4Value.FromString(lValueNode.Value);

                    lProperty.SetValue(this, lVector4Value, null);
                }
                else if (lProperty.PropertyType == typeof(Quaternion))
                {
                    Quaternion lQuaternionValue = Quaternion.identity;
                    lQuaternionValue = lQuaternionValue.FromString(lValueNode.Value);

                    lProperty.SetValue(this, lQuaternionValue, null);
                }
                else if (lProperty.PropertyType == typeof(Transform))
                {
                    // We use the _StoredTransformIndex for serialization
                }
                else if (lProperty.PropertyType == typeof(List<int>))
                {
                    if (lValueNode.Value.Length > 0)
                    {
                        List<int> lValues = lValueNode.Value.Split(',').Select(x => int.Parse(x)).ToList();
                        lProperty.SetValue(this, lValues, null);
                    }
                }
                else if (lProperty.PropertyType == typeof(AnimationCurve))
                {
                    if (lValueNode.Value.Length > 0)
                    {
                        AnimationCurve lCurve = new AnimationCurve();

                        string[] lItems = lValueNode.Value.Split(';');
                        for (int i = 0; i < lItems.Length; i++)
                        {
                            string[] lElements = lItems[i].Split('|');
                            if (lElements.Length == 5)
                            {
                                int lIntValue = 0;
                                float lFloatValue = 0f;

                                Keyframe lKey = new Keyframe();
                                if (float.TryParse(lElements[0], out lFloatValue)) { lKey.time = lFloatValue; }
                                if (float.TryParse(lElements[1], out lFloatValue)) { lKey.value = lFloatValue; }
                                if (int.TryParse(lElements[2], out lIntValue)) { lKey.tangentMode = lIntValue; }
                                if (float.TryParse(lElements[3], out lFloatValue)) { lKey.inTangent = lFloatValue; }
                                if (float.TryParse(lElements[4], out lFloatValue)) { lKey.outTangent = lFloatValue; }

                                lCurve.AddKey(lKey);
                            }
                        }

                        lProperty.SetValue(this, lCurve, null);
                    }
                }
                else
                {
                    //JSONClass lObject = lValueNode.AsObject;
                }
            }

            if (_AnchorIndex >= 0)
            {
                if (_AnchorIndex < RigController.StoredTransforms.Count)
                {
                    _Anchor = RigController.StoredTransforms[_AnchorIndex];
                }
                else
                {
                    _Anchor = null;
                    _AnchorIndex = -1;
                }
            }
        }

        // **************************************************************************************************
        // Following properties and function only valid while editing
        // **************************************************************************************************

#if UNITY_EDITOR

        // List object for our motors
        private ReorderableList mRendererList;

        // Index of the selected item in the list
        private int EditorListIndex = -1;

        // Used to determine if the list is dirty
        private bool mIsDirty = false;

        /// <summary>
        /// Allow the motion to render it's own GUI
        /// </summary>
        public virtual bool OnInspectorGUI()
        {
            mIsDirty = false;
            bool lIsDirty = false;

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

            if (Offset.y == 0f && Offset.z == 0f)
            {
                if (EditorHelper.FloatField("X-Axis Offset", "Offset on the relative x-axis that becomes the focus point.", Offset.x, RigController))
                {
                    lIsDirty = true;
                    _Offset.x = EditorHelper.FieldFloatValue;
                }
            }
            else
            {
                if (EditorHelper.Vector3Field("Offset", "Offset that applied after the rig's AnchorOffset is applied", Offset, RigController))
                {
                    lIsDirty = true;
                    Offset = EditorHelper.FieldVector3Value;
                }
            }


            GUILayout.Space(5f);

            EditorGUILayout.BeginVertical(GUI.skin.box);

            if (EditorHelper.BoolField("Is Collision Enabled", "Determines if this motor allows colliding.", IsCollisionEnabled, RigController))
            {
                lIsDirty = true;
                IsCollisionEnabled = EditorHelper.FieldBoolValue;
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUI.skin.box);

            if (EditorHelper.BoolField("Is Fade Enabled", "Determines if this motor allows fading.", IsFadingEnabled, RigController))
            {
                lIsDirty = true;
                IsFadingEnabled = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.BoolField("Set Fade Renderers", "Determines if we use the specified fade renderers when fading.", SpecifyFadeRenderers, RigController))
            {
                lIsDirty = true;
                SpecifyFadeRenderers = EditorHelper.FieldBoolValue;
            }

            if (SpecifyFadeRenderers)
            {
                //InstantiateRendererList();

                // Show the Renderers
                GUILayout.BeginVertical(EditorHelper.GroupBox);

                mRendererList.DoLayoutList();

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();

            if (mIsDirty)
            {
                mIsDirty = false;
                lIsDirty = true;
            }

            return lIsDirty;
        }

        /// <summary>
        /// Allow the motor to render it's own GUI debug GUI
        /// </summary>
        public virtual bool OnDebugInspectorGUI()
        {
            bool lIsDirty = false;

            return lIsDirty;
        }

        /// <summary>
        /// Create the reorderable list
        /// </summary>
        private void InstantiateRendererList()
        {
            if (mRendererList != null)
            {
                mRendererList.drawHeaderCallback -= DrawListHeader;
                mRendererList.drawFooterCallback -= DrawListFooter;
                mRendererList.drawElementCallback -= DrawListItem;
                mRendererList.onAddCallback -= OnListItemAdd;
                mRendererList.onRemoveCallback -= OnListItemRemove;
                mRendererList.onSelectCallback -= OnListItemSelect;
                mRendererList.onReorderCallback -= OnListReorder;
                mRendererList.list.Clear();
            }

            mRendererList = new ReorderableList(_FadeTransformIndexes, typeof(int), true, true, true, true);
            mRendererList.drawHeaderCallback += DrawListHeader;
            mRendererList.drawFooterCallback += DrawListFooter;
            mRendererList.drawElementCallback += DrawListItem;
            mRendererList.onAddCallback += OnListItemAdd;
            mRendererList.onRemoveCallback += OnListItemRemove;
            mRendererList.onSelectCallback += OnListItemSelect;
            mRendererList.onReorderCallback += OnListReorder;
            mRendererList.footerHeight = 17f;

            if (EditorListIndex >= 0 && EditorListIndex < _FadeTransformIndexes.Count)
            {
                mRendererList.index = EditorListIndex;
            }
        }

        /// <summary>
        /// Header for the list
        /// </summary>
        /// <param name="rRect"></param>
        private void DrawListHeader(Rect rRect)
        {
            EditorGUI.LabelField(rRect, "Renderers");
            if (GUI.Button(rRect, "", EditorStyles.label))
            {
                mRendererList.index = -1;
                OnListItemSelect(mRendererList);
            }
        }

        /// <summary>
        /// Allows us to draw each item in the list
        /// </summary>
        /// <param name="rRect"></param>
        /// <param name="rIndex"></param>
        /// <param name="rIsActive"></param>
        /// <param name="rIsFocused"></param>
        private void DrawListItem(Rect rRect, int rIndex, bool rIsActive, bool rIsFocused)
        {
            if (rIndex < _FadeTransformIndexes.Count)
            {
                bool lIsDirty = false;

                rRect.y += 2;

                EditorGUILayout.BeginHorizontal();

                Rect lTransformRect = new Rect(rRect.x, rRect.y, rRect.width, EditorGUIUtility.singleLineHeight);

                Transform lTransform = GetFadeRenderer(rIndex);
                Transform lNewTransform = EditorGUI.ObjectField(lTransformRect, lTransform, typeof(Transform), true) as Transform;
                if (lTransform != lNewTransform)
                {
                    lIsDirty = true;

                    if (lNewTransform == null)
                    {
                        _FadeTransformIndexes[rIndex] = -1;
                    }
                    else
                    {
                        SetFadeRenderer(rIndex, lNewTransform);
                    }
                }

                EditorGUILayout.EndHorizontal();

                // Update the motion if there's a change
                if (lIsDirty)
                {
                    mIsDirty = true;
                }
            }
        }

        /// <summary>
        /// Footer for the list
        /// </summary>
        /// <param name="rRect"></param>
        private void DrawListFooter(Rect rRect)
        {
            Rect lMotorRect = new Rect(rRect.x, rRect.y + 1, rRect.width - 4 - 28 - 28, 16);
            //mMotorIndex = EditorGUI.Popup(lMotorRect, mMotorIndex, mMotorNames.ToArray());

            Rect lAddRect = new Rect(lMotorRect.x + lMotorRect.width + 4, lMotorRect.y, 28, 15);
            if (GUI.Button(lAddRect, new GUIContent("+", "Add Motor."), EditorStyles.miniButtonLeft)) { OnListItemAdd(mRendererList); }

            Rect lDeleteRect = new Rect(lAddRect.x + lAddRect.width, lAddRect.y, 28, 15);
            if (GUI.Button(lDeleteRect, new GUIContent("-", "Delete Motor."), EditorStyles.miniButtonRight)) { OnListItemRemove(mRendererList); };
        }

        /// <summary>
        /// Allows us to add to a list
        /// </summary>
        /// <param name="rList"></param>
        private void OnListItemAdd(ReorderableList rList)
        {
            _FadeTransformIndexes.Add(-1);

            mRendererList.index = _FadeTransformIndexes.Count - 1;
            OnListItemSelect(rList);

            mIsDirty = true;
        }

        /// <summary>
        /// Allows us process when a list is selected
        /// </summary>
        /// <param name="rList"></param>
        private void OnListItemSelect(ReorderableList rList)
        {
            EditorListIndex = rList.index;
        }

        /// <summary>
        /// Allows us to stop before removing the item
        /// </summary>
        /// <param name="rList"></param>
        private void OnListItemRemove(ReorderableList rList)
        {
            if (EditorUtility.DisplayDialog("Warning!", "Are you sure you want to delete the item?", "Yes", "No"))
            {
                int rIndex = rList.index;

                rList.index--;

                RemoveFadeRenderer(rIndex);

                OnListItemSelect(rList);

                mIsDirty = true;
            }
        }

        /// <summary>
        /// Allows us to process after the motions are reordered
        /// </summary>
        /// <param name="rList"></param>
        private void OnListReorder(ReorderableList rList)
        {
            mIsDirty = true;
        }

#endif
    }
}
