using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = System.Object;

namespace Nomnom.AssetTabs {
    internal static class ReflectionUtility {
        // mmmm reflection
        private static readonly Type _editorType = typeof(Editor);
        private static readonly Type _dockAreaType = _editorType.Assembly.GetType("UnityEditor.DockArea");
        private static readonly Type _propertyEditorType = _editorType.Assembly.GetType("UnityEditor.PropertyEditor");
        
        private static readonly MethodInfo _getCurrentMousePositionMethod = _editorType.GetMethod("GetCurrentMousePosition", BindingFlags.NonPublic | BindingFlags.Static);
        
        private static readonly MethodInfo _openPropertyEditorMethod = _propertyEditorType.GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .First(x => x.Name == "OpenPropertyEditor" && x.GetParameters().Length == 2);
        
        private static readonly MethodInfo _addTabMethod = _dockAreaType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .First(x => x.Name == "AddTab" && x.GetParameters().Length == 2 && x.GetParameters()[0].ParameterType == typeof(EditorWindow));
        
        private static readonly FieldInfo _getEditorWindowParentField = typeof(EditorWindow).GetField("m_Parent", BindingFlags.NonPublic | BindingFlags.Instance);
        
        private static readonly FieldInfo _dockScrollOffsetField = _dockAreaType.GetField("m_ScrollOffset", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo _dockTotalTabWidthField = _dockAreaType.GetField("m_TotalTabWidth", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo _dockTabAreaRectField = _dockAreaType.GetField("m_TabAreaRect", BindingFlags.NonPublic | BindingFlags.Instance);

        internal static Vector2 GetCurrentMousePosition() {
            return (Vector2)_getCurrentMousePositionMethod.Invoke(null, null);
        }

        internal static object OpenPropertyEditor(Object obj, bool showWindow = false) {
            return _openPropertyEditorMethod.Invoke(null, new object[] { obj, showWindow });
        }
        
        internal static void DockAddTab(object dock, object tab) {
            _addTabMethod.Invoke(dock, new object[] { tab, false });
        }
        
        internal static object GetEditorWindowParent(object child) {
            return _getEditorWindowParentField.GetValue(child);
        }
        
        internal static void ScrollToNewTab(object dock, object tab) {
            var scrollOffset = (float)_dockScrollOffsetField.GetValue(dock);
            var totalTabWidth = (float)_dockTotalTabWidthField.GetValue(dock);
            var tabAreaRect = (Rect)_dockTabAreaRectField.GetValue(dock);

            if (totalTabWidth < tabAreaRect.width) return;
            
            scrollOffset = totalTabWidth - tabAreaRect.width;
            _dockScrollOffsetField.SetValue(dock, scrollOffset);
        }
    }
}