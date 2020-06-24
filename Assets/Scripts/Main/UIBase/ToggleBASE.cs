using System;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.UI
{
    /// <summary>
    ///   <para>A standard toggle that has an on / off state.</para>
    /// </summary>
    [AddComponentMenu("UI/ToggleTest", 41), RequireComponent(typeof(RectTransform))]
    public class ToggleBASE : Selectable, IPointerClickHandler, ISubmitHandler, ICanvasElement, IEventSystemHandler
    {
        /// <summary>
        ///   <para>Display settings for when a toggle is activated or deactivated.</para>
        /// </summary>
        public enum ToggleTransition
        {
            /// <summary>
            ///   <para>Show / hide the toggle instantly.</para>
            /// </summary>
            None,
            Fade
        }

        [Serializable]
        public class ToggleEvent : UnityEvent<bool>
        {
        }

        /// <summary>
        ///   <para>Transition mode for the toggle.</para>
        /// </summary>
        public ToggleTransition toggleTransition = ToggleTransition.Fade;

        /// <summary>
        ///   <para>Graphic affected by the toggle.</para>
        /// </summary>
        public Graphic graphic;

        [SerializeField]
        private ToggleBASEGroup m_Group;

        public ToggleEvent onValueChanged = new ToggleEvent();

        [FormerlySerializedAs("m_IsActive"), SerializeField, Tooltip("Is the toggle currently on or off?")]
        private bool m_IsOn;

        public ToggleBASEGroup group
        {
            get
            {
                return m_Group;
            }
            set
            {
                m_Group = value;
                if (Application.isPlaying)
                {
                    SetToggleGroup(m_Group, true);
                    PlayEffect(true);
                }
            }
        }

        public bool isOn { get { return m_IsOn; } }

        protected ToggleBASE()
        {

        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            PrefabType prefabType = PrefabUtility.GetPrefabType(this);
            if (prefabType != PrefabType.Prefab && !Application.isPlaying)
            {
                CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
            }
        }
#endif

        public virtual void Rebuild(CanvasUpdate executing)
        {
            if (executing == CanvasUpdate.Prelayout)
            {
                onValueChanged.Invoke(m_IsOn);
            }
        }

        public virtual void LayoutComplete()
        {
        }

        public virtual void GraphicUpdateComplete()
        {
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetToggleGroup(m_Group, false);
            PlayEffect(true);
        }

        protected override void OnDisable()
        {
            SetToggleGroup(null, false);
            base.OnDisable();
        }

        protected override void OnDidApplyAnimationProperties()
        {
            if (graphic != null)
            {
                bool flag = !Mathf.Approximately(graphic.canvasRenderer.GetColor().a, 0f);
                if (m_IsOn != flag)
                {
                    m_IsOn = flag;
                    Set(!flag);
                }
            }
            base.OnDidApplyAnimationProperties();
        }

        private void SetToggleGroup(ToggleBASEGroup newGroup, bool setMemberValue)
        {
            ToggleBASEGroup group = m_Group;
            if (m_Group != null)
            {
                m_Group.UnregisterToggle(this);
            }
            if (setMemberValue)
            {
                m_Group = newGroup;
            }
            if (newGroup != null && IsActive())
            {
                newGroup.RegisterToggle(this);
            }
            if (newGroup != null && newGroup != group && isOn && IsActive())
            {
                newGroup.NotifyToggleOn(this);
            }
        }

        private void Set(bool value)
        {
            Set(value, true);
        }

        public void Set(bool value, bool sendCallback = true)
        {
            if (m_IsOn != value)
            {
                m_IsOn = value;
                if (m_Group != null && IsActive())
                {
                    if (m_IsOn || (!m_Group.AnyTogglesOn() && !m_Group.allowSwitchOff))
                    {
                        m_IsOn = true;
                        m_Group.NotifyToggleOn(this, sendCallback);
                    }
                }
                PlayEffect(toggleTransition == ToggleTransition.None);
                if (sendCallback)
                {
                    UISystemProfilerApi.AddMarker("Toggle.value", this);
                    onValueChanged.Invoke(m_IsOn);
                }
            }
        }

        private void PlayEffect(bool instant)
        {
            if (!(graphic == null))
            {
                if (!Application.isPlaying)
                {
                    graphic.canvasRenderer.SetAlpha((!m_IsOn) ? 0f : 1f);
                }
                else
                {
                    graphic.CrossFadeAlpha((!m_IsOn) ? 0f : 1f, (!instant) ? 0.1f : 0f, true);
                }
            }
            EffectExt();
        }

        protected virtual void EffectExt()
        {

        }

        protected override void Start()
        {
            //PlayEffect(true);
        }

        private void InternalToggle()
        {
            if (IsActive() && IsInteractable())
            {
                Set(!isOn);
            }
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                InternalToggle();
            }
        }

        public virtual void OnSubmit(BaseEventData eventData)
        {
            InternalToggle();
        }
    }
}
