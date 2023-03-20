#if UNITY_STANDALONE_OSX
#define ENABLE_USD
#endif

using System;
using System.IO;
using Unity.Formats.USD;
using UnityEditor;
using UnityEngine;
using USD.NET;

namespace MagicLeap.LeapBrush
{
    [CustomEditor(typeof(SpaceMeshManager))]
    public class SpaceMeshManagerEditor : Editor
    {
        private const string ConvertedPrefabName = "Assets/ConvertUSD.prefab";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            SpaceMeshManager spaceMeshManager = (SpaceMeshManager) target;

#if ENABLE_USD
            if (GUILayout.Button("Convert USD to Prefab..."))
            {
                HandleConvertUsdToPrefab(spaceMeshManager);
            }
#endif

#if UNITY_ANDROID || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_WIN
            if (GUILayout.Button("Export converted prefab as UnityBundles..."))
            {
                HandleExportPrefabAsUnityBundles(spaceMeshManager);
            }
#endif

            if (GUILayout.Button("Test load AssetBundles..."))
            {
                TestLoadAssetBundle(spaceMeshManager);
            }
        }

        private void TestLoadAssetBundle(SpaceMeshManager spaceMeshManager)
        {
            string bundlePath = EditorUtility.OpenFilePanel(
                "Open Bundle File", null, ".unitybundle");

            var myLoadedAssetBundle = AssetBundle.LoadFromFile(bundlePath);
            try
            {
                Debug.Log(string.Join(", ", myLoadedAssetBundle.GetAllAssetNames()));

                GameObject loaded = myLoadedAssetBundle.LoadAsset<GameObject>(
                    myLoadedAssetBundle.GetAllAssetNames()[0]);
                Instantiate(loaded, spaceMeshManager.SpaceMeshContainer.transform);
            }
            finally
            {
                myLoadedAssetBundle.Unload(false);
            }
        }

#if ENABLE_USD
        private static void HandleConvertUsdToPrefab(SpaceMeshManager spaceMeshManager)
        {
            InitUsd.Initialize();

            foreach (Transform childTransform in spaceMeshManager.SpaceMeshContainer.transform)
            {
                DestroyImmediate(childTransform.gameObject);
            }
            string usdPath = EditorUtility.OpenFilePanel(
                "Open USD Mesh File", null, ".usd");

            Debug.LogFormat("Converting usd file {0}...", usdPath);

            Scene scene = Scene.Open(usdPath);
            if (scene == null)
            {
                Debug.LogError("Failed to import mesh usd");
                return;
            }

            scene.Time = 0;

            SceneImportOptions importOptions = new();
            importOptions.changeHandedness = BasisTransformation.FastWithNegativeScale;
            importOptions.materialMap.MetallicWorkflowMaterial = spaceMeshManager.Material;
            importOptions.materialMap.SpecularWorkflowMaterial = spaceMeshManager.Material;
            importOptions.materialMap.DisplayColorMaterial = spaceMeshManager.Material;
            importOptions.enableGpuInstancing = false;
            importOptions.materialImportMode = MaterialImportMode.ImportPreviewSurface;

            PrimMap primMap = new PrimMap();

            var usdSceneRoot = new GameObject("UsdConvertRoot");
            usdSceneRoot.transform.SetParent(spaceMeshManager.SpaceMeshContainer.transform,
                worldPositionStays: false);
            SceneImporter.BuildScene(scene,
                usdSceneRoot,
                importOptions,
                primMap,
                composingSubtree: false);

            AssetDatabase.DeleteAsset(ConvertedPrefabName);

            // TODO: Determine why meshes are coming in with -90 x-axis rotation.
            usdSceneRoot.transform.localRotation = Quaternion.identity;

            try
            {
                bool prefabSaveSuccess;
                var prefabGameObject = PrefabUtility.SaveAsPrefabAsset(
                    usdSceneRoot, ConvertedPrefabName, out prefabSaveSuccess);
                if (!prefabSaveSuccess)
                {
                    Debug.LogError("Failed to save prefab");
                    return;
                }

                MeshFilter[] sourceMeshFilters = usdSceneRoot
                    .GetComponentsInChildren<MeshFilter>();
                MeshFilter[] prefabMeshFilters = prefabGameObject
                    .GetComponentsInChildren<MeshFilter>();
                if (sourceMeshFilters.Length != prefabMeshFilters.Length)
                {
                    Debug.LogError("Mesh filter count mismatch");
                    return;
                }

                for (int i = 0; i < sourceMeshFilters.Length; i++)
                {
                    MeshFilter sourceMeshFilter = sourceMeshFilters[i];
                    MeshFilter prefabMeshFilter = prefabMeshFilters[i];
                    MeshRenderer sourceMeshRenderer = sourceMeshFilter.GetComponent<MeshRenderer>();
                    MeshRenderer prefabMeshRenderer = prefabMeshFilter.GetComponent<MeshRenderer>();

                    prefabMeshFilter.sharedMesh = sourceMeshFilter.sharedMesh;
                    prefabMeshFilter.sharedMesh.RecalculateBounds();
                    AssetDatabase.AddObjectToAsset(sourceMeshFilter.sharedMesh,
                        prefabGameObject);

                    var usdPrimSource = sourceMeshRenderer.GetComponent<UsdPrimSource>();
                    if (usdPrimSource == null)
                    {
                        Debug.LogError("MeshRenderer missing UsdPrimSource");
                        continue;
                    }

                    string texturePath = spaceMeshManager.GetTexturePathFromUsdPrim(
                        usdPrimSource, Directory.GetParent(usdPath).FullName);
                    if (texturePath == null)
                    {
                        Debug.LogErrorFormat(
                            "Unable to determine texture path from UsdPrimSource ({0})",
                            usdPrimSource.m_usdPrimPath);
                        continue;
                    }

                    var bytes = File.ReadAllBytes(texturePath);
                    Texture2D texture = new Texture2D(
                        SpaceMeshManager.TextureWidthHeight, SpaceMeshManager.TextureWidthHeight);
                    texture.LoadImage(bytes);
                    texture.anisoLevel = SpaceMeshManager.TextureAnisoLevel;
                    AssetDatabase.AddObjectToAsset(texture, prefabGameObject);

                    prefabMeshRenderer.sharedMaterial = sourceMeshRenderer.sharedMaterial;
                    prefabMeshRenderer.sharedMaterial.SetTexture("_BaseMap", texture);

                    AssetDatabase.AddObjectToAsset(sourceMeshRenderer.sharedMaterial,
                        prefabGameObject);
                }

                AssetDatabase.SaveAssets();

                Debug.LogFormat("Usd file {0} converted", usdPath);
            }
            finally
            {
                primMap.DestroyAll();

                DestroyImmediate(usdSceneRoot);
            }
        }
#endif

        private static void HandleExportPrefabAsUnityBundles(SpaceMeshManager spaceMeshManager)
        {
            string usdPath = EditorUtility.OpenFilePanel(
                "Select USD Mesh File", null, ".usd");

#if UNITY_ANDROID
            BuildBundleForPlatform(BuildTarget.Android, usdPath, ConvertedPrefabName);
#elif UNITY_STANDALONE_LINUX
            BuildBundleForPlatform(BuildTarget.StandaloneLinux64, usdPath, ConvertedPrefabName);
#elif UNITY_STANDALONE_WIN
            BuildBundleForPlatform(BuildTarget.StandaloneWindows64, usdPath, ConvertedPrefabName);
#else
            throw new Exception("Unexpected platform");
#endif
        }

        private static void BuildBundleForPlatform(
            BuildTarget buildTarget, string usdPath, string prefabAssetName)
        {
            string bundleExtension;
            switch (buildTarget)
            {
                case BuildTarget.Android:
                    bundleExtension = SpaceMeshManager.AndroidBundleExtension;
                    break;
                case BuildTarget.StandaloneLinux64:
                    bundleExtension = SpaceMeshManager.Linux64BundleExtension;
                    break;
                case BuildTarget.StandaloneWindows64:
                    bundleExtension = SpaceMeshManager.Windows64BundleExtension;
                    break;
                default:
                    throw new Exception("Unexpected build target " + buildTarget);
            }

            AssetBundleBuild bundleBuild = new();
            bundleBuild.assetBundleName = Path.GetFileName(usdPath) + bundleExtension;
            bundleBuild.assetNames = new[] {prefabAssetName};

            BuildPipeline.BuildAssetBundles(
                Path.GetDirectoryName(usdPath),
                new[] {bundleBuild}, BuildAssetBundleOptions.None, buildTarget);
        }
    }
}
