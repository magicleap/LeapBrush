#if UNITY_STANDALONE_OSX
#define ENABLE_USD
#endif

using System;
using System.Collections;
using System.IO;
using pxr;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

#if ENABLE_USD
using Unity.Formats.USD;
using USD.NET;
#endif

namespace MagicLeap
{
    /// <summary>
    /// Manager for displaying a visualization of the Space RGG mesh if present.
    /// </summary>
    public class SpaceMeshManager : MonoBehaviour
    {
        [FormerlySerializedAs("_material")]
        [SerializeField]
        public Material Material;

        [FormerlySerializedAs("_spaceMeshContainer")]
        [SerializeField]
        public GameObject SpaceMeshContainer;

        [SerializeField]
        private GameObject _loadingSpinner;

        private IEnumerator _loaderCoroutine;
        private string _spaceId;
        private bool _isShown;
#if ENABLE_USD
        private PrimMap _primMap;
#endif

        public const int TextureWidthHeight = 2048;
        public const int TextureAnisoLevel = 2;

        public const string AndroidBundleExtension = ".android.unitybundle";
        public const string Linux64BundleExtension = ".linux64.unitybundle";
        public const string Windows64BundleExtension = ".windows64.unitybundle";

        private class TextureLoadRequest
        {
            public string TexturePath;
            public Texture2D Texture;
            public bool Completed;

            public TextureLoadRequest(string texturePath)
            {
                TexturePath = texturePath;
            }
        }

        private void Start()
        {
#if ENABLE_USD
            InitUsd.Initialize();
#endif
        }

        public void SetShown(bool isShown)
        {
            if (_isShown == isShown)
            {
                return;
            }

            _isShown = isShown;

            if (!isShown)
            {
                SpaceMeshContainer.SetActive(false);
            }
            else
            {
                LoadSpaceMesh();
            }
        }

        public void UpdateSpaceMesh(string spaceId, Pose spacePose)
        {
            SpaceMeshContainer.transform.SetWorldPose(spacePose);

            if (spaceId == _spaceId)
            {
                return;
            }

            _spaceId = spaceId;

            if (_isShown)
            {
                LoadSpaceMesh();
            }
        }

        private void LoadSpaceMesh()
        {
#if ENABLE_USD
            if (_primMap != null)
            {
                _primMap.DestroyAll();
                _primMap = null;
            }
#endif

            foreach (Transform childTransform in SpaceMeshContainer.transform)
            {
                Destroy(childTransform.gameObject);
            }

            if (_loaderCoroutine != null)
            {
                StopCoroutine(_loaderCoroutine);
            }

            SpaceMeshContainer.SetActive(false);

            string usdDir = Path.Join(
                Application.persistentDataPath,
                "SpaceMeshes",
                _spaceId);

#if ENABLE_USD
            _loaderCoroutine = LoadSpaceMeshFromUsdCoroutine(usdDir);
#else
            _loaderCoroutine = LoadSpaceMeshFromBundleCoroutine(usdDir);
#endif
            StartCoroutine(_loaderCoroutine);
        }

#if ENABLE_USD
        private IEnumerator LoadSpaceMeshFromUsdCoroutine(string usdDir)
        {
            _loadingSpinner.SetActive(true);
            try
            {
                string meshUsdPath = Path.Join(usdDir, "mesh.usd");
                if (!File.Exists(meshUsdPath))
                {
                    Debug.LogErrorFormat("No space mesh found at {0}", meshUsdPath);
                    yield break;
                }

                Debug.LogFormat("Loading space mesh from {0}...", meshUsdPath);

                Scene scene = Scene.Open(meshUsdPath);
                if (scene == null)
                {
                    Debug.LogError("Failed to import mesh usd");
                    yield break;
                }

                scene.Time = 0;

                // Disable texture importing into the asset database
                MaterialImporter.OnResolveTexture = ResolveTextureDoNotImport;

                SceneImportOptions importOptions = new();
                importOptions.changeHandedness = BasisTransformation.FastWithNegativeScale;
                importOptions.materialMap.MetallicWorkflowMaterial = Material;
                importOptions.materialMap.SpecularWorkflowMaterial = Material;
                importOptions.materialMap.DisplayColorMaterial = Material;
                importOptions.enableGpuInstancing = false;
                importOptions.materialImportMode = MaterialImportMode.ImportPreviewSurface;

                _primMap = new PrimMap();

                var usdSceneRoot = new GameObject("UsdRoot");
                usdSceneRoot.transform.SetParent(SpaceMeshContainer.transform,
                    worldPositionStays: false);
                IEnumerator buildSceneEnumerator = SceneImporter.BuildScene(scene,
                    usdSceneRoot,
                    importOptions,
                    _primMap,
                    5.0f,
                    composingSubtree: false);

                int buildSceneFrameCount = 0;
                while (buildSceneEnumerator.MoveNext())
                {
                    yield return null;
                    buildSceneFrameCount++;
                }

                // Restore default texture import behavior
                MaterialImporter.OnResolveTexture = null;

                int loadTexturesFrameCount = 0;
                foreach (MeshRenderer meshRenderer in SpaceMeshContainer
                             .GetComponentsInChildren<MeshRenderer>())
                {
                    var usdPrimSource = meshRenderer.GetComponent<UsdPrimSource>();
                    if (usdPrimSource == null)
                    {
                        Debug.LogError("MeshRenderer missing UsdPrimSource");
                        continue;
                    }

                    string texturePath = GetTexturePathFromUsdPrim(usdPrimSource, usdDir);
                    if (texturePath == null)
                    {
                        Debug.LogErrorFormat(
                            "Unable to determine texture path from UsdPrimSource ({0})",
                            usdPrimSource.m_usdPrimPath);
                        continue;
                    }

                    TextureLoadRequest textureLoadRequest = new(texturePath);
                    LoadTextureFromFileAsync(textureLoadRequest);
                    while (!textureLoadRequest.Completed)
                    {
                        yield return null;
                        loadTexturesFrameCount++;
                    }

                    if (textureLoadRequest.Texture != null)
                    {
                        meshRenderer.material.SetTexture("_BaseMap", textureLoadRequest.Texture);
                    }

                    meshRenderer.receiveShadows = false;
                    meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                }

                // TODO: Determine why meshes are needing a 180 degree X-axis rotation
                usdSceneRoot.transform.localRotation = Quaternion.Euler(180, 0, 0);

                SpaceMeshContainer.SetActive(_isShown);

                Debug.LogFormat("Loaded USD scene. {0} scene build frames, {1} load texture frames",
                    buildSceneFrameCount, loadTexturesFrameCount);
            }
            finally
            {
                _loadingSpinner.SetActive(false);
                _loaderCoroutine = null;
            }
        }

        private Texture2D ResolveTextureDoNotImport(SdfAssetPath textureAssetPath, bool isNormalMap,
            SceneImportOptions importOptions)
        {
            return null;
        }
#else
        private IEnumerator LoadSpaceMeshFromBundleCoroutine(string usdDir)
        {
            _loadingSpinner.SetActive(true);
            try
            {
#if UNITY_ANDROID
                string bundleExtension = SpaceMeshManager.AndroidBundleExtension;
#elif UNITY_STANDALONE_LINUX
                string bundleExtension = SpaceMeshManager.Linux64BundleExtension;
#elif UNITY_STANDALONE_WIN
                string bundleExtension = SpaceMeshManager.Windows64BundleExtension;
#endif

                string meshBundlePath = Path.Join(
                    usdDir, "mesh.usd" + bundleExtension);
                if (!File.Exists(meshBundlePath))
                {
                    Debug.LogErrorFormat("No space bundle found at {0}", meshBundlePath);
                    yield break;
                }

                Debug.LogFormat("Loading space mesh from {0}...", meshBundlePath);

                int bundleLoadFrames = 0;
                int assetLoadFrames = 0;

                var bundleLoadRequest = AssetBundle.LoadFromFileAsync(meshBundlePath);
                while (!bundleLoadRequest.isDone)
                {
                    bundleLoadFrames++;
                    yield return null;
                }

                try
                {
                    AssetBundleRequest loadRequest =
                        bundleLoadRequest.assetBundle.LoadAssetAsync<GameObject>(
                            bundleLoadRequest.assetBundle.GetAllAssetNames()[0]);
                    while (!loadRequest.isDone)
                    {
                        assetLoadFrames++;
                        yield return null;
                    }

                    Instantiate(loadRequest.asset, SpaceMeshContainer.transform);
                }
                finally
                {
                    bundleLoadRequest.assetBundle.Unload(false);
                }

                foreach (MeshRenderer meshRenderer in SpaceMeshContainer
                             .GetComponentsInChildren<MeshRenderer>())
                {
                    meshRenderer.sharedMaterial.shader = Material.shader;
                    meshRenderer.receiveShadows = false;
                    meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                }

                SpaceMeshContainer.SetActive(_isShown);

                Debug.LogFormat(
                    "Loaded bundle scene. {0} bundle load frames, {1} asset load frames.",
                    bundleLoadFrames, assetLoadFrames);
            }
            finally
            {
                _loadingSpinner.SetActive(false);
                _loaderCoroutine = null;
            }
        }
#endif

        private static void LoadTextureFromFileAsync(TextureLoadRequest request)
        {
            ThreadDispatcher.ScheduleWork(() =>
            {
                try
                {
                    var bytes = File.ReadAllBytes(request.TexturePath);
                    ThreadDispatcher.ScheduleMain(() =>
                    {
                        try
                        {
                            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                            texture.LoadImage(bytes);
                            request.Texture = texture;
                        }
                        finally
                        {
                            request.Completed = true;
                        }
                    });
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    ThreadDispatcher.ScheduleMain(() =>
                    {
                        request.Completed = true;
                    });
                }
            });
        }

#if ENABLE_USD
        public string GetTexturePathFromUsdPrim(UsdPrimSource usdPrimSource, string usdDir)
        {
            if (string.IsNullOrEmpty(usdPrimSource.m_usdPrimPath))
            {
                return null;
            }

            string[] pieces = usdPrimSource.m_usdPrimPath.Split("/");
            if (pieces.Length != 4)
            {
                return null;
            }

            return Path.Join(usdDir, pieces[2] + ".png");
        }
#endif
    }
}
