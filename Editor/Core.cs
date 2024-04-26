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

        [MenuItem("Assets/Open as Tab", false, priority: 500)]
        private static void Open() {
            OpenAssetAsTab(Selection.activeObject);
        }

        [MenuItem("Assets/Open as Tab", true)]
        private static bool OpenValidate() {
            return Selection.activeObject;
        }

        [MenuItem("Assets/Open as Floating", false, priority: 501)]
        private static void OpenFloating() {
            OpenAssetAsFloating(Selection.activeObject);
        }

        [MenuItem("Assets/Open as Floating", true)]
        private static bool OpenFloatingValidate() {
            // return Selection.activeObject && AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject));
            return Selection.activeObject;
        }

        private static void OnUpdate() {
            if (DragAndDrop.objectReferences == null || DragAndDrop.objectReferences.Length <= 0) {
                if (_lastWindow) {
                    UnRegisterEnterDrag(_lastWindow);
                    _lastWindow = null;
                }
                return;
            }

            // are we dragging into a tab row
            var hoveringWindow = EditorWindow.mouseOverWindow;
            if (!hoveringWindow) {
                if (_lastWindow) {
                    UnRegisterEnterDrag(_lastWindow);
                }
                _lastWindow = null;
                return;
            }

            if (!_lastWindow || _lastWindow != hoveringWindow) {
                if (_lastWindow) {
                    UnRegisterEnterDrag(_lastWindow);
                }

                _lastWindow = hoveringWindow;
                RegisterEnterDrag(hoveringWindow);
            }

            // Debug.Log($"{_lastWindow} vs {hoveringWindow}");

            var tabRect = hoveringWindow.position;
            tabRect.height = 30;

            var mousePosition = ReflectionUtility.GetCurrentMousePosition();
            if (tabRect.Contains(mousePosition)) {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            }
        }

        private static void RegisterEnterDrag(EditorWindow editorWindow) {
            if (!editorWindow) return;
            if (editorWindow is SceneView) {
                RegisterSceneViewEnterDrag(editorWindow);
                return;
            }
            
            var dockArea = editorWindow.rootVisualElement.parent?.Children().First();
            // Debug.Log($"dockArea enter: {dockArea}");
            dockArea?.UnregisterCallback<DragPerformEvent>(OnDragPerform);
            dockArea?.RegisterCallback<DragPerformEvent>(OnDragPerform);
        }

        private static void UnRegisterEnterDrag(EditorWindow editorWindow) {
            if (!editorWindow) return;
            if (editorWindow is SceneView) {
                UnRegisterSceneViewEnterDrag(editorWindow);
                return;
            }
            
            var dockArea = editorWindow.rootVisualElement.parent?.Children().First();
            // Debug.Log($"dockArea exit: {dockArea}");
            dockArea?.UnregisterCallback<DragPerformEvent>(OnDragPerform);
        }
        
        private static void RegisterSceneViewEnterDrag(EditorWindow editorWindow) {
            var sceneView = editorWindow as SceneView;
            if (!sceneView) return;
            
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }
        
        private static void UnRegisterSceneViewEnterDrag(EditorWindow editorWindow) {
            var sceneView = editorWindow as SceneView;
            if (!sceneView) return;
            
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView sceneView) {
            if (!_lastWindow) {
                SceneView.duringSceneGui -= OnSceneGUI;
                return;
            }
            
            var e = Event.current;
            if (e.type == EventType.DragExited) {
                OnDragPerform(default);
                e.Use();
            }
        }

        private static void OnDragPerform(DragPerformEvent evt) {
            UnRegisterEnterDrag(_lastWindow);
            _lastWindow = null;
            
            var dragObjects = DragAndDrop.objectReferences;
            if (dragObjects.Length == 0) {
                // Debug.Log("No drag objects");
                return;
            }
            
            var currentWindow = EditorWindow.mouseOverWindow;
            if (!currentWindow) return;
            
            var tabRect = currentWindow.position;
            tabRect.height = 30;
            
            var mousePosition = ReflectionUtility.GetCurrentMousePosition();
            if (!tabRect.Contains(mousePosition)) {
                // Debug.Log($"{mousePosition} not in {tabRect}");
                return;
            }
            
            // Debug.Log($"{tabRect} contains {mousePosition}");
            var dockArea = ReflectionUtility.GetEditorWindowParent(currentWindow);

            EditorApplication.delayCall += () => {
                for (int i = 0; i < dragObjects.Length; i++) {
                    var assetPath = AssetDatabase.GetAssetPath(dragObjects[i]);
                    if (!string.IsNullOrEmpty(assetPath) && AssetDatabase.IsValidFolder(assetPath)) {
                        FolderTabWindow.Create(dragObjects[i], currentWindow);
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

            if (evt == null) return;
            
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

        public static void OpenAssetAsFloating(Object obj) {
            var assetPath = AssetDatabase.GetAssetPath(obj);
            if (AssetDatabase.IsValidFolder(assetPath)) {
                EditorApplication.delayCall += () => {
                    FolderTabWindow.Create(obj, null);
                };

                EditorApplication.QueuePlayerLoopUpdate();
                return;
            }

            ReflectionUtility.OpenPropertyEditor(obj, true);
            EditorApplication.QueuePlayerLoopUpdate();
        }

        public static void DuplicateAsset(Object obj, FolderTabWindow window) {
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = obj;
            if (EditorApplication.ExecuteMenuItem("Edit/Duplicate")) {
                EditorApplication.delayCall += () => window.Refresh();
            }
            EditorApplication.delayCall += () => window.Focus();
        }

        public static void DestroyAsset(Object obj, FolderTabWindow window) {
            Selection.activeObject = obj;
            if (EditorApplication.ExecuteMenuItem("Assets/Delete")) {
                EditorApplication.delayCall += () => window.Refresh();
            }
            EditorApplication.delayCall += () => window.Focus();
        }
    }
}