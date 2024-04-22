using UnityEditor;
using UnityEngine;

namespace Nomnom.AssetTabs {
    internal class AssetPostProcessorWatcher: AssetModificationProcessor {
        private static void OnWillCreateAsset(string assetName) {
            RepaintWindows();
        }

        private static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options) {
            RepaintWindows();
            return AssetDeleteResult.DidNotDelete;
        }

        private static AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath) {
            RepaintWindows();
            return AssetMoveResult.DidNotMove;
        }

        private static string[] OnWillSaveAssets(string[] paths) {
            if (paths.Length > 0) {
                RepaintWindows();
            }
            return paths;
        }

        private static void RepaintWindows() {
            // todo: repaint windows that have changed paths
            EditorApplication.delayCall += () => {
                foreach (var window in Resources.FindObjectsOfTypeAll<FolderTabWindow>()) {
                    if (!window) continue;
                    window.Refresh();
                    window.Repaint();
                }
            };
        }
    }
}