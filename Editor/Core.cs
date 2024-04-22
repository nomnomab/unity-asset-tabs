using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Nomnom.AssetTabs {
    internal static class Core {
        // todo: make sure this doesn't get any weird edge case for caching
        private static EditorWindow _lastWindow;
        
        [InitializeOnLoadMethod]
        private static void OnLoad() {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }
        
        [MenuItem("Assets/Open as Tab", false, priority: Int32.MaxValue)]
        private static void Open() {
            OpenAssetAsTab(Selection.activeObject);
        }
        
        [MenuItem("Assets/Open as Tab", true)]
        private static bool OpenValidate() {
            return Selection.activeObject;
        }

        private static void OnUpdate() {
            if (DragAndDrop.objectReferences == null || DragAndDrop.objectReferences.Length <= 0) {
                return;
            }
            
            // are we dragging into a tab row
            var hoveringWindow = EditorWindow.mouseOverWindow;
            if (!hoveringWindow) {
                if (_lastWindow) {
                    var dockArea = _lastWindow.rootVisualElement.parent?.Children().First();
                    dockArea?.UnregisterCallback<DragPerformEvent>(OnDragPerform);
                }
                _lastWindow = null;
                return;
            }

            if (!_lastWindow || _lastWindow != hoveringWindow) {
                if (_lastWindow) {
                    var dockArea = _lastWindow.rootVisualElement.parent?.Children().First();
                    dockArea?.UnregisterCallback<DragPerformEvent>(OnDragPerform);
                }
                    
                _lastWindow = hoveringWindow;
                {
                    var dockArea = _lastWindow.rootVisualElement.parent?.Children().First();
                    dockArea?.UnregisterCallback<DragPerformEvent>(OnDragPerform);
                    dockArea?.RegisterCallback<DragPerformEvent>(OnDragPerform);
                }
            }

            var tabRect = hoveringWindow.position;
            tabRect.height = 30;

            var mousePosition = ReflectionUtility.GetCurrentMousePosition();
            if (tabRect.Contains(mousePosition)) {
                DragAndDrop.SetGenericData("valid-asset-tab", true);
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            }
        }

        private static void OnDragPerform(DragPerformEvent evt) {
            var dragObjects = DragAndDrop.objectReferences;
            if (dragObjects.Length == 0) return;
            if (DragAndDrop.GetGenericData("valid-asset-tab") == null) {
                return;
            }
            
            var dockArea = ReflectionUtility.GetEditorWindowParent(_lastWindow);
            
            EditorApplication.delayCall += () => {
                for (int i = 0; i < dragObjects.Length; i++) {
                    var assetPath = AssetDatabase.GetAssetPath(dragObjects[i]);
                    if (!string.IsNullOrEmpty(assetPath) && AssetDatabase.IsValidFolder(assetPath)) {
                        FolderTabWindow.Create(dragObjects[i], _lastWindow);
                        continue;
                    }
                    
                    var e = ReflectionUtility.OpenPropertyEditor(dragObjects[i]);
                    ReflectionUtility.DockAddTab(dockArea, e);
                }

                EditorApplication.delayCall += () => {
                    ReflectionUtility.ScrollToNewTab(dockArea, null);

                    foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>()) {
                        if (!window) continue;
                        window.Repaint();
                    }
                };
            };

            _lastWindow = null;
            
            evt.StopImmediatePropagation();
            evt.StopPropagation();
        }

        public static void FocusAsset(Object obj) {
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = obj;

            var assetPath = AssetDatabase.GetAssetPath(obj);
            if (AssetDatabase.IsValidFolder(assetPath)) {
                EditorApplication.delayCall += () => AssetDatabase.OpenAsset(obj);
                EditorApplication.QueuePlayerLoopUpdate();
                return;
            }
                
            EditorGUIUtility.PingObject(obj);
            EditorApplication.QueuePlayerLoopUpdate();
        }

        public static void OpenAsset(Object obj) {
            FocusAsset(obj);
            AssetDatabase.OpenAsset(obj);
        }
        
        public static void OpenAssetAsTab(Object obj) {
            var currentWindow = EditorWindow.mouseOverWindow;
            var dockArea = ReflectionUtility.GetEditorWindowParent(currentWindow);

            var assetPath = AssetDatabase.GetAssetPath(obj);
            if (AssetDatabase.IsValidFolder(assetPath)) {
                EditorApplication.delayCall += () => {
                    FolderTabWindow.Create(obj, currentWindow);
                };
                
                EditorApplication.QueuePlayerLoopUpdate();
                return;
            }
                
            var propertyEditor = ReflectionUtility.OpenPropertyEditor(obj);
            EditorApplication.delayCall += () => {
                ReflectionUtility.DockAddTab(dockArea, propertyEditor);
                ReflectionUtility.ScrollToNewTab(dockArea, null);
            };
            
            EditorApplication.QueuePlayerLoopUpdate();
        }
    }
}