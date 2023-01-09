using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MagicLeap.LeapBrush
{
    // TODO(ghazen): Remove this editor script once MLDevice Singleton objects are no longer
    // created in edit mode.
    public class MLDeviceSingletonEditorCleanup : MonoBehaviour
    {
        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            CleanUpMLDeviceSingletons();
        }

        public class PostProcessor : AssetPostprocessor
        {
            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
            {
                CleanUpMLDeviceSingletons();
            }
        }

        private static void CleanUpMLDeviceSingletons()
        {
            foreach (GameObject gameObject in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (gameObject.name == "(MLDevice Singleton) ")
                {
                    Debug.Log("MLDeviceSingletonEditorCleanup: Destroying (MLDevice Singleton)");
                    DestroyImmediate(gameObject);
                }
            }
        }
    }
}