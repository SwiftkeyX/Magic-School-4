using UnityEngine;
using UnityEngine.UIElements;

namespace MagicSchool.UI
{
    // the overlay panel 
    [RequireComponent(typeof(UIDocument))]
    internal abstract class PanelController : MonoBehaviour
    {
        // .uss for hiding the panel 
        protected const string HiddenClass = "is-hidden";

        // the panel
        [SerializeField] private VisualTreeAsset _panelAsset;

        // this panel's own root, once it is in the tree
        protected VisualElement Panel { get; private set; }

        // the main panel that was going to be inserted to
        protected VisualElement MainPanel { get; private set; }

        // the mount the panel into the main panel
        private void OnEnable()
        {
            UIDocument document = GetComponent<UIDocument>();

            MainPanel = document.rootVisualElement;
            if (MainPanel == null || _panelAsset == null) return;

            Panel = PanelMounter.MountInMainPanel(MainPanel, _panelAsset);
            if (Panel == null) return;

            OnMounted(Panel);
        }

        // The panel is still in the tree: find what panel this tree holds, wire that panel inside main panel.
        protected abstract void OnMounted(VisualElement panel);

        // show/hide this panel. 
        public void SetShown(bool shown)
        {
            if (Panel == null) return;

            Panel.EnableInClassList(HiddenClass, !shown);
        }
    }
}
