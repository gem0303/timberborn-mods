using Timberborn.BaseComponentSystem;
using Timberborn.CoreUI;
using Timberborn.EntityPanelSystem;
using Timberborn.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Local.IdleWorkerAlert
{
    public sealed class IdleWorkerFragment : IEntityPanelFragment
    {
        private const string PanelLocKey = "Local.IdleWorkerAlert.Panel";

        private readonly ILoc _loc;
        private Label _root;
        private IdleWorkerMonitor _monitor;

        public IdleWorkerFragment(ILoc loc)
        {
            _loc = loc;
        }

        public VisualElement InitializeFragment()
        {
            _root = new Label();
            _root.AddToClassList("entity-sub-panel");
            _root.AddToClassList("entity-panel__text");
            _root.style.backgroundColor = new StyleColor(new Color(159f / 255f, 92f / 255f, 8f / 255f, 1f));
            _root.style.color = new StyleColor(Color.white);
            Color borderColor = new Color(188f / 255f, 162f / 255f, 108f / 255f, 1f);
            _root.style.borderBottomColor = new StyleColor(borderColor);
            _root.style.borderLeftColor = new StyleColor(borderColor);
            _root.style.borderRightColor = new StyleColor(borderColor);
            _root.style.borderTopColor = new StyleColor(borderColor);
            _root.style.borderBottomWidth = 1f;
            _root.style.borderLeftWidth = 1f;
            _root.style.borderRightWidth = 1f;
            _root.style.borderTopWidth = 1f;
            _root.ToggleDisplayStyle(false);
            return _root;
        }

        public void ShowFragment(BaseComponent entity)
        {
            _monitor = entity.GetComponent<IdleWorkerMonitor>();
            UpdateDisplay();
        }

        public void ClearFragment()
        {
            _monitor = null;
            _root.ToggleDisplayStyle(false);
        }

        public void UpdateFragment()
        {
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            bool show = _monitor != null &&
                _monitor.IsSupportedWorkplace &&
                _monitor.IdleWorkerCount > 0;
            _root.ToggleDisplayStyle(show);

            if (!show)
            {
                return;
            }

            _root.text = string.Format(
                _loc.T(PanelLocKey),
                _monitor.IdleWorkerCount,
                _monitor.AssignedWorkerCount);
        }
    }
}
