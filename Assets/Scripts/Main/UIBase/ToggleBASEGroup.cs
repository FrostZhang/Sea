using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    /// <summary>
    ///   <para>A component that represents a group of UI.Toggles.</para>
    /// </summary>
    [AddComponentMenu("UI/Toggle Group Test", 42), DisallowMultipleComponent]
    public class ToggleBASEGroup : UIBehaviour
    {
        [SerializeField]
        private bool m_AllowSwitchOff = false;

        private List<ToggleBASE> m_Toggles = new List<ToggleBASE>();

        /// <summary>
        ///   <para>Is it allowed that no toggle is switched on?</para>
        /// </summary>
        public bool allowSwitchOff
        {
            get
            {
                return m_AllowSwitchOff;
            }
            set
            {
                m_AllowSwitchOff = value;
            }
        }

        protected ToggleBASEGroup()
        {
        }

        private void ValidateToggleIsInGroup(ToggleBASE toggle)
        {
            if (toggle == null || !m_Toggles.Contains(toggle))
            {
                throw new ArgumentException(string.Format("Toggle {0} is not part of ToggleGroup {1}", new object[]
                {
                    toggle,

                }));
            }
        }

        public void NotifyToggleOn(ToggleBASE toggle, bool sendCallback = true)
        {
            ValidateToggleIsInGroup(toggle);
            for (int i = 0; i < m_Toggles.Count; i++)
            {
                if (!(m_Toggles[i] == toggle))
                {
                    m_Toggles[i].Set(false, sendCallback);
                }
            }
        }

        public void UnregisterToggle(ToggleBASE toggle)
        {
            if (m_Toggles.Contains(toggle))
            {
                m_Toggles.Remove(toggle);
            }
        }

        public void RegisterToggle(ToggleBASE toggle)
        {
            if (!m_Toggles.Contains(toggle))
            {
                m_Toggles.Add(toggle);
            }
        }

        /// <summary>
        ///   <para>Are any of the toggles on?</para>
        /// </summary>
        public bool AnyTogglesOn()
        {
            return m_Toggles.Find((ToggleBASE x) => x.isOn) != null;
        }

        /// <summary>
        ///   <para>Returns the toggles in group that are active.</para>
        /// </summary>
        /// <returns>
        ///   <para>The active toggles in the group.</para>
        /// </returns>
        public IEnumerable<ToggleBASE> ActiveToggles()
        {
            return from x in m_Toggles
                   where x.isOn
                   select x;
        }

        public void SetAllTogglesOff(bool sendCallback = true)
        {
            bool allowSwitchOff = m_AllowSwitchOff;
            m_AllowSwitchOff = true;
            for (int i = 0; i < m_Toggles.Count; i++)
            {
                m_Toggles[i].Set(false, sendCallback);
            }
            m_AllowSwitchOff = allowSwitchOff;
        }
    }
}
