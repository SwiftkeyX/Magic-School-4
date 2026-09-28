using UnityEditor;
using UnityEngine;

namespace MagicSchool.Core
{
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
            // Rows are laid out as "height + standardVerticalSpacing", so a hidden field has to
            // give back the spacing too or it leaves a blank gap where it used to be.
            if (!IsShown(property)) return -EditorGUIUtility.standardVerticalSpacing;

            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        private bool IsShown(SerializedProperty property)
        {
            SerializedProperty sibling = FindSibling(property);

            // named a field that does not exist - show it rather than hide it forever with no clue why
            if (sibling == null) return true;

            // a list or array sibling counts as filled in once it holds something
            if (sibling.isArray && sibling.propertyType != SerializedPropertyType.String)
                return sibling.arraySize > 0;

            switch (sibling.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    return sibling.boolValue;

                // a condition picked from the <None> dropdown
                case SerializedPropertyType.ManagedReference:
                    return sibling.managedReferenceValue != null;

                case SerializedPropertyType.ObjectReference:
                    return sibling.objectReferenceValue != null;

                default:
                    return true;
            }
        }

        /// The sibling lives in the same object as this field, so swap the last element of this
        /// field's path for the sibling's name - that keeps working however deeply nested the
        /// owning object is (Steps > Element > Action Groups > Element > Effects > Element).
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
