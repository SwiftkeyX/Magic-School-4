using UnityEditor;
using UnityEngine;

namespace MagicSchool.Core
{
    // A hidden field must give back the standardVerticalSpacing too, or it leaves a gap. A condition naming a field
    // that does not exist shows the field rather than hiding it with no clue why. The sibling is found by swapping the
    // last part of this field's path, which works however deeply it is nested.
    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    public class ShowIfDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!IsShown(property)) return;

            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!IsShown(property)) return -EditorGUIUtility.standardVerticalSpacing;

            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        private bool IsShown(SerializedProperty property)
        {
            SerializedProperty sibling = FindSibling(property);

            if (sibling == null) return true;

            if (sibling.isArray && sibling.propertyType != SerializedPropertyType.String)
                return sibling.arraySize > 0;

            switch (sibling.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    return sibling.boolValue;

                case SerializedPropertyType.ManagedReference:
                    return sibling.managedReferenceValue != null;

                case SerializedPropertyType.ObjectReference:
                    return sibling.objectReferenceValue != null;

                default:
                    return true;
            }
        }

        private SerializedProperty FindSibling(SerializedProperty property)
        {
            ShowIfAttribute showIf = (ShowIfAttribute)attribute;

            string path = property.propertyPath;
            int lastDot = path.LastIndexOf('.');

            string siblingPath = lastDot < 0
                ? showIf.SiblingField
                : path.Substring(0, lastDot + 1) + showIf.SiblingField;

            return property.serializedObject.FindProperty(siblingPath);
        }
    }
}
