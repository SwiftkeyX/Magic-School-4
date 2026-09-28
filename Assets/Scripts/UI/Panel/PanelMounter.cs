using UnityEngine;
using UnityEngine.UIElements;

namespace MagicSchool.UI
{
    internal static class PanelMounter
    {
        public static VisualElement CloneTemplateRoot(VisualTreeAsset asset)
        {
            VisualElement wrapper = asset.CloneTree();
            VisualElement root = wrapper[0];

            for (int i = 0; i < wrapper.styleSheets.count; i++)
            {
                root.styleSheets.Add(wrapper.styleSheets[i]);
            }

            root.RemoveFromHierarchy();
            return root;
        }

        public static VisualElement MountInMainPanel(VisualElement mainPanel, VisualTreeAsset panelTree)
        {
            VisualElement panel = CloneTemplateRoot(panelTree);

            // use this panel's name - to find where it should be put inside the "main panel"
            VisualElement slot = mainPanel.Q<VisualElement>(panel.name);
            if (slot == null)
            {
                Debug.LogWarning($"PanelMounter: no element named '{panel.name}' found in the main document.");
                return null;
            }

            slot.Clear();
            slot.Add(panel);

            return panel;
        }
    }
}
