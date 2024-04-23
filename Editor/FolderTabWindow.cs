using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Nomnom.AssetTabs {
    internal sealed class FolderTabWindow: EditorWindow {
        [SerializeField] private Object _folderAsset;
        [SerializeField] private float _zoomLevel = 84;
        [SerializeField] private float _scrollValue;

        private readonly List<VisualElement> _elements = new();
        private ScrollView _scrollView;
        private Image _selectionIcon;
        private Label _selectionName;
        
        public static FolderTabWindow Create(Object folder, EditorWindow? dockTo) {
            var window = dockTo != null ? CreateWindow<FolderTabWindow>(dockTo.GetType()) : CreateWindow<FolderTabWindow>();
            window._folderAsset = folder;
            window.titleContent = new GUIContent(folder.name, AssetPreview.GetMiniThumbnail(folder));
            window.Show();
            EditorApplication.delayCall += () => {
                window.Refresh();
            };
            return window;
        }

        private void CreateGUI() {
            rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("AssetTabsStyles"));
            
            Refresh();
        }

        public void Refresh() {
            _elements.Clear();
            _scrollView = null;
            _selectionIcon = null;
            _selectionName = null;
            
            if (_folderAsset == null) return;

            var assetPath = AssetDatabase.GetAssetPath(_folderAsset);
            if (string.IsNullOrEmpty(assetPath) || !AssetDatabase.IsValidFolder(assetPath)) {
                Close();
                return;
            }
            
            titleContent = new GUIContent(_folderAsset.name, AssetPreview.GetMiniThumbnail(_folderAsset));

            var root = rootVisualElement;
            root.Clear();

            var header = new VisualElement {
                name = "folder-tab--header"
            };

            // var headerLabel = new Label(assetPath) {
            //     name = "folder-tab--label"
            // };

            var breadcrumbs = new ScrollView(ScrollViewMode.Horizontal) {
                name = "folder-tab--breadcrumbs",
            };
            var inner = new VisualElement {
                name = "folder-tab--breadcrumbs__inner"
            };
            breadcrumbs.Add(inner);

            var splitPath = assetPath.Split('/');
            for (var i = 0; i < splitPath.Length; i++) {
                var item = splitPath[i];
                if (string.IsNullOrEmpty(item)) continue;
                var breadcrumb = new VisualElement {
                    name = "folder-tab--breadcrumb",
                    focusable = true
                };
                var breadcrumbLabel = new Label(item);
                breadcrumb.Add(breadcrumbLabel);
                
                var tmpI = i;
                breadcrumb.RegisterCallback<ClickEvent>(e => {
                    openBreadcrumb();
                });

                breadcrumb.RegisterCallback<NavigationSubmitEvent>(e => {
                    openBreadcrumb();
                });

                breadcrumb.RegisterCallback<MouseUpEvent>(e => {
                    if (e.button == 1) {
                        var menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Open"), false, openBreadcrumb);
                        menu.AddItem(new GUIContent("Open as Tab"), false, openBreadcrumbAsTab);
                        menu.AddItem(new GUIContent("Open as Floating"), false, openBreadcrumbAsFloating);
                        menu.ShowAsContext();
                        e.StopPropagation();
                    }
                });
                
                void openBreadcrumb() {
                    var path = string.Join("/", splitPath.Take(tmpI + 1));
                    var folderAsset = AssetDatabase.LoadMainAssetAtPath(path);
                    Core.FocusAsset(folderAsset);
                }
                
                void openBreadcrumbAsTab() {
                    var path = string.Join("/", splitPath.Take(tmpI + 1));
                    var folderAsset = AssetDatabase.LoadMainAssetAtPath(path);
                    Core.OpenAssetAsTab(folderAsset);
                }
                
                void openBreadcrumbAsFloating() {
                    var path = string.Join("/", splitPath.Take(tmpI + 1));
                    var folderAsset = AssetDatabase.LoadMainAssetAtPath(path);
                    Core.OpenAssetAsFloating(folderAsset);
                }
                
                inner.Add(breadcrumb);
                
                if (i < splitPath.Length - 1) {
                    var separator = new Label("/") {
                        focusable = false
                    };
                    separator.AddToClassList("folder-tab--label__separator");
                    inner.Add(separator);
                }
            }
            header.Add(breadcrumbs);
            
            root.Add(header);
            
            var gridScroller = new ScrollView(ScrollViewMode.Vertical) {
                name = "folder-tab--grid-scroller"
            };
            gridScroller.Q("unity-slider").focusable = false;

            gridScroller.verticalScroller.slider.RegisterValueChangedCallback(e => {
                var obj = new SerializedObject(this);
                obj.FindProperty(nameof(_scrollValue)).floatValue = e.newValue;
                _scrollValue = e.newValue;
                obj.ApplyModifiedPropertiesWithoutUndo();
            });
            
            // gridScroller.RegisterCallback<MouseUpEvent>(e => {
            //     if (e.button != 1) return;
            //     
            //     var r = GUILayoutUtility.GetRect(EditorGUIUtility.IconContent("CreateAddNew"), ReflectionUtility.toolbarCreateAddNewDropDownStyle);
            //     r.position = e.mousePosition;
            //     
            //     Selection.activeObject = _folderAsset;
            //     EditorUtility.DisplayPopupMenu(r, "Assets", new MenuCommand(_folderAsset));
            //     
            //     e.StopPropagation();
            // });
            
            _scrollView = gridScroller;
            root.Add(gridScroller);

            var grid = new VisualElement {
                name = "folder-tab--grid",
            };
            gridScroller.Add(grid);
            grid.EnableInClassList("list-view", false);
            
            _scrollView.RegisterCallback<DragUpdatedEvent>(e => {
                var refs = DragAndDrop.objectReferences;
                if (refs == null || refs.Length == 0) return;
                
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                e.StopPropagation();
            });
            
            _scrollView.RegisterCallback<DragPerformEvent>(e => {
                var refs = DragAndDrop.objectReferences;
                if (refs == null || refs.Length == 0) return;

                var folderPath = AssetDatabase.GetAssetPath(_folderAsset);
                
                foreach (var obj in refs) {
                    if (AssetDatabase.Contains(obj)) {
                        // move the asset
                        var assetPath = AssetDatabase.GetAssetPath(obj);
                        AssetDatabase.MoveAsset(assetPath, folderPath + "/" + Path.GetFileName(assetPath));
                        AssetDatabase.Refresh();
                        continue;
                    }

                    if (obj is GameObject gameObject) {
                        // is this already connected to a prefab?
                        if (PrefabUtility.GetPrefabAssetType(gameObject) != PrefabAssetType.NotAPrefab) {
                            var variantName = $"{gameObject.name} Variant";
                            var assetPath = $"{folderPath}/{variantName}";
                            
                            // get the next available number
                            var existingVariants = AssetDatabase.FindAssets($"t:GameObject {variantName}", new[] { folderPath })
                                .Select(AssetDatabase.GUIDToAssetPath)
                                .Select(path => Path.GetFileNameWithoutExtension(path))
                                .Select(name => int.TryParse(name.Substring(variantName.Length).Trim(), out var num) ? num : 0)
                                .OrderBy(num => num)
                                .ToList();
                            
                            if (existingVariants.Count > 0) {
                                assetPath += $" {existingVariants.Last() + 1}.prefab";
                            } else {
                                assetPath += ".prefab";
                            }
                            
                            // check if asset path exists already
                            if (AssetDatabase.AssetPathToGUID(assetPath) != string.Empty) {
                                Debug.LogWarning($"Asset already exists at path: {assetPath}");
                                continue;
                            }
                            
                            PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, assetPath, InteractionMode.UserAction);
                            AssetDatabase.Refresh();
                            continue;
                        }

                        {
                            var assetPath = $"{folderPath}/{gameObject.name}.prefab";
                            PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, assetPath, InteractionMode.UserAction);
                            AssetDatabase.Refresh();
                        }
                        continue;
                    }
                }
                
                e.StopPropagation();
            });

            // gather all assets in the folder
            var childFolders = Directory.GetDirectories(assetPath)
                .Where(x => !x.EndsWith(".meta"));
            var childAssets = Directory.GetFiles(assetPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(x => !x.EndsWith(".meta"))
                .GroupBy(Path.GetExtension);

            foreach (var folder in childFolders) {
                GenerateButton(folder, grid);
            }

            foreach (var group in childAssets) {
                foreach (var path in group) {
                    GenerateButton(path, grid);
                }
            }

            var footer = new VisualElement {
                name = "folder-tab--footer"
            };

            var selectionIcon = new Image {
                name = "folder-tab--selection-icon",
            };
            footer.Add(selectionIcon);
            _selectionIcon = selectionIcon;
            
            var selectionLabel = new Label {
                name = "folder-tab--selection-label",
            };
            footer.Add(selectionLabel);
            _selectionName = selectionLabel;
            
            SetSelection(null);

            var scaleSlider = new Slider {
                name = "folder-tab--scale-slider",
            };

            scaleSlider.style.width = 100;
            scaleSlider.lowValue = 36;
            scaleSlider.highValue = 120;
            scaleSlider.value = 0;
            footer.Add(scaleSlider);
            
            scaleSlider.RegisterValueChangedCallback(evt => {
                var obj = new SerializedObject(this);
                obj.FindProperty(nameof(_zoomLevel)).floatValue = evt.newValue;
                _zoomLevel = evt.newValue;
                obj.ApplyModifiedPropertiesWithoutUndo();

                updateSize();
            });
            
            scaleSlider.RegisterCallback<MouseUpEvent>(e => {
                if (e.button == 1) {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Reset Zoom"), false, () => {
                        scaleSlider.value = 84;
                    });
                    menu.ShowAsContext();
                    e.StopPropagation();
                }
            });
            
            scaleSlider.value = _zoomLevel;
            updateSize();

            void updateSize() {
                var items = grid.Children();
                var inListView = _zoomLevel <= scaleSlider.lowValue;
                
                grid.EnableInClassList("list-view", inListView);
                
                foreach (var item in items) {
                    if (!inListView) {
                        item.style.width = _zoomLevel;
                        item.style.height = _zoomLevel;
                    } else {
                        item.style.width = new StyleLength(StyleKeyword.Auto);
                        item.style.height = 20;
                    }
                }
            }

            root.Add(footer);

            _scrollView.verticalScroller.slider.value = _scrollValue;
            
            Repaint();
        }

        private void GenerateButton(string path, VisualElement parent) {
            var childAsset = AssetDatabase.LoadMainAssetAtPath(path);
            if (childAsset == null) return;

            var button = new VisualElement {
                focusable = true,
                userData = childAsset
            };
            button.AddToClassList("folder-tab--button");
            
            var elementsIndex = _elements.Count;
            _elements.Add(button);

            var img = new Image {
                name = "folder-tab--image"
            };

            var instanceId = childAsset.GetInstanceID();
            
            //? waits until the preview is loaded before assigning
            // todo: maybe limit this to some max amount of executors at a given time?
            img.schedule.Execute(() => {
                if (AssetPreview.IsLoadingAssetPreview(instanceId)) {
                    return;
                }
                
                // kind of weird but it seems to work fine
                Texture2D thumbnail = null;
                if (AssetDatabase.IsValidFolder(path)) {
                    thumbnail = AssetPreview.GetMiniThumbnail(childAsset);
                } else {
                    thumbnail = AssetPreview.GetAssetPreview(childAsset);
                }

                // if the above somehow fails, try to get the mini thumbnail
                if (thumbnail == null) {
                    thumbnail = AssetPreview.GetMiniThumbnail(childAsset);
                }

                img.image = thumbnail;
            }).Until(() => img.image && !AssetPreview.IsLoadingAssetPreview(instanceId));
            
            button.Add(img);

            var labelRow = new VisualElement {
                name = "folder-tab--label-row"
            };

            var labelIcon = new Image {
                name = "folder-tab--label-icon",
                image = AssetPreview.GetMiniThumbnail(childAsset),
                
            };

            var label = new Label(childAsset.name) {
                name = "folder-tab--label"
            };

            button.Add(img);
            
            labelRow.Add(labelIcon);
            labelRow.Add(label);
            
            button.Add(labelRow);

            // button.text = childAsset.name;
            button.RegisterCallback<ClickEvent>(e => {
                if (e.button != 0) return;
                if (e.clickCount < 2) return;
                Core.OpenAsset(childAsset);
                e.StopPropagation();
            });

            button.RegisterCallback<NavigationSubmitEvent>(e => {
                Core.OpenAsset(childAsset);
                e.StopPropagation();
            });

            var isDown = false;
            var isDragging = false;
            button.RegisterCallback<MouseDownEvent>(e => {
                if (e.button != 0) return;
                isDown = true;
            });
            
            button.RegisterCallback<MouseUpEvent>(e => {
                if (e.button != 0) {
                    if (e.button == 1) {
                        var menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Focus"), false, () => Core.FocusAsset(childAsset));
                        menu.AddSeparator(string.Empty);
                        menu.AddItem(new GUIContent("Open"), false, () => Core.OpenAsset(childAsset));
                        menu.AddItem(new GUIContent("Open as Tab"), false, () => Core.OpenAssetAsTab(childAsset));
                        menu.AddItem(new GUIContent("Open as Floating"), false, () => Core.OpenAssetAsFloating(childAsset));
                        menu.AddSeparator(string.Empty);
                        menu.AddItem(new GUIContent("Duplicate %D"), false, () => Core.DuplicateAsset(childAsset, this));
                        menu.AddItem(new GUIContent("Delete _DEL"), false, () => Core.DestroyAsset(childAsset, this));
                        menu.ShowAsContext();
                        e.StopPropagation();
                    }
                    return;
                }
                isDown = false;
            });
            
            button.RegisterCallback<MouseMoveEvent>(e => {
                if (e.button != 0) return;
                if (!isDown) return;
                if (isDragging) return;

                isDragging = true;
                
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new[] { childAsset };
                DragAndDrop.StartDrag("Drag " + childAsset.name);
            });
            
            button.RegisterCallback<FocusInEvent>(e => {
                _scrollView?.ScrollTo(button);
                SetSelection(childAsset);
            });
            
            button.RegisterCallback<FocusOutEvent>(e => {
                SetSelection(null);
            });
            
            button.RegisterCallback<KeyUpEvent>(e => {
                if (e.ctrlKey && e.keyCode == KeyCode.D) {
                    Core.DuplicateAsset(childAsset, this);
                    e.StopPropagation();
                    return;
                }
                
                if (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.X) {
                    Core.DestroyAsset(childAsset, this);
                    e.StopPropagation();
                    return;
                }
            });
            
            button.AddManipulator(new KeyboardNavigationManipulator((op, e) => {
                var inListView = parent.ClassListContains("list-view");
                
                switch (op) {
                    case KeyboardNavigationOperation.Submit:
                        Core.OpenAsset(childAsset);
                        e.StopPropagation();
                        break;
                    // left
                    case KeyboardNavigationOperation.MoveLeft:
                        if (inListView) return;
                        navigatePrevious();
                        break;
                    // right
                    case KeyboardNavigationOperation.MoveRight:
                        if (inListView) return;
                        navigateNext();
                        break;
                    // up
                    case KeyboardNavigationOperation.Previous:
                        if (!inListView) {
                            // navigateVertical(new Vector2(0, -button.layout.height));
                            navigateUp();
                        } else {
                            navigatePrevious();
                        }
                        break;
                    // down
                    case KeyboardNavigationOperation.Next:
                        if (!inListView) {
                            // navigateVertical(new Vector2(0, button.layout.height));
                            navigateDown();
                        } else {
                            navigateNext();
                        }
                        break;
                }
                
                int getRowCount() {
                    var rowCount = (int)(button.parent.layout.width / button.layout.width);
                    Debug.Log($"{_scrollView.layout.width}/{button.layout.width} = {rowCount}");
                    return rowCount;
                }

                void navigatePrevious() {
                    var leftIndex = elementsIndex - 1;
                    if (leftIndex >= 0) {
                        _elements[leftIndex].Focus();
                        _scrollView?.ScrollTo(_elements[leftIndex]);
                    }
                    e.StopPropagation();
                }
                
                void navigateNext() {
                    var rightIndex = elementsIndex + 1;
                    if (rightIndex < _elements.Count) {
                        _elements[rightIndex].Focus();
                        _scrollView?.ScrollTo(_elements[rightIndex]);
                    }
                    e.StopPropagation();
                }
                
                void navigateUp() {
                    var upIndex = elementsIndex - getRowCount();
                    if (upIndex >= 0) {
                        _elements[upIndex].Focus();
                        _scrollView?.ScrollTo(_elements[upIndex]);
                    }
                    e.StopPropagation();
                }
                
                void navigateDown() {
                    var downIndex = elementsIndex + getRowCount();
                    if (downIndex < _elements.Count) {
                        _elements[downIndex].Focus();
                        _scrollView?.ScrollTo(_elements[downIndex]);
                    }
                    e.StopPropagation();
                }

                void navigateVertical(Vector2 direction) {
                    using var _ = ListPool<VisualElement>.Get(out var found);
                    var pos = button.worldBound.center + direction;
                    button.panel.PickAll(pos, found);
                        
                    foreach (var element in found) {
                        if (!element.ClassListContains("folder-tab--button")) {
                            continue;
                        }
                        element.Focus();
                        _scrollView?.ScrollTo(element);
                        break;
                    }
                        
                    e.StopPropagation();
                }
            }));
            
            button.RegisterCallback<MouseLeaveEvent>(e => {
                isDown = false;
                isDragging = false;
            });
            
            parent.Add(button);
        }

        private void SetSelection(Object obj) {
            if (_selectionIcon == null || _selectionName == null) return;
            if (!obj) {
                _selectionIcon.style.display = DisplayStyle.None;
                _selectionName.style.display = DisplayStyle.None;
                return;
            }
            
            _selectionIcon.style.display = DisplayStyle.Flex;
            _selectionName.style.display = DisplayStyle.Flex;
            _selectionIcon.image = AssetPreview.GetMiniThumbnail(obj);
            _selectionName.text = AssetDatabase.GetAssetPath(obj);
        }
    }
}