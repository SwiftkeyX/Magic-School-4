using UnityEngine;
using UnityEngine.UIElements;

namespace MagicSchool.UI
{
    // the overlay panel 
    [RequireComponent(typeof(UIDocument))]
    internal abstract class PanelController : MonoBehaviour
    {
        protected const string HiddenClass = "is-hidden";

        [SerializeField] private VisualTreeAsset _panelAsset;

        protected VisualElement Panel { get; private set; }

        protected VisualElement MainPanel { get; private set; }

        private void OnEnable()
        {
            UIDocument document = GetComponent<UIDocument>();

            MainPanel = document.rootVisualElement;
            if (MainPanel == null || _panelAsset == null) return;

            Panel = PanelMounter.MountInMainPanel(MainPanel, _panelAsset);
            if (Panel == null) return;

            OnMounted(Panel);
        }

        protected abstract void OnMounted(VisualElement panel);

        public void SetShown(bool shown)
        {
            if (Panel == null) return;

            Panel.EnableInClassList(HiddenClass, !shown);
        }
    }
}
