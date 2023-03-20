using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityGLTF;
using UnityGLTF.Loader;

namespace MagicLeap
{
    /// <summary>
    /// Manager for fetching the list of available 3D models to load, gltf model loader, etc.
    /// </summary>
    public class External3DModelManager : MonoBehaviour
    {
        public bool Multithreaded = false;
        public int MaximumLod = 300;
        public int Timeout = 8;
        public GLTFSceneImporter.ColliderType Collider = GLTFSceneImporter.ColliderType.Mesh;
        public GameObject _external3DModelPrefab_;

        public ImporterFactory Factory = null;

        [SerializeField]
        private Shader _shaderOverride = null;

        [SerializeField]
        private GameObject[] _builtIn3DModels = Array.Empty<GameObject>();

        private class ModelCacheEntry
        {
            public GameObject Model;
            public LinkedList<External3DModel> ModelLoadQueue = new();
        }

        private Dictionary<String, ModelCacheEntry> _modelCache = new();

        public event Action OnModelsListUpdated;

        private ModelInfo[] _models = Array.Empty<ModelInfo>();

        public class ModelInfo
        {
            public string FileName;
            public string Path;
            public GameObject Prefab;
        }

        public ModelInfo[] Models => _models;

        public void RefreshModelList()
        {
            string parentDir = Application.persistentDataPath;

            Dictionary<string, ModelInfo> builtInModelInfos = GetBuiltIn3DModelInfos();

            List<ModelInfo> newModels = new();
            newModels.AddRange(builtInModelInfos.Values);

            ThreadDispatcher.ScheduleWork(() =>
            {
                try
                {
                    foreach (string path in Directory.GetFiles(parentDir))
                    {
                        if (!path.EndsWith(".gltf") && !path.EndsWith(".glb"))
                        {
                            continue;
                        }

                        string fileName = Path.GetFileName(path);
                        if (builtInModelInfos.ContainsKey(fileName))
                        {
                            continue;
                        }

                        newModels.Add(new ModelInfo()
                        {
                            FileName = fileName,
                            Path = path
                        });
                    }
                }
                catch (IOException exception)
                {
                    Debug.LogErrorFormat("Error loading list of 3d models from {0}: {1}",
                        parentDir, exception);
                }

                ThreadDispatcher.ScheduleMain(() =>
                {
                    _models = newModels.ToArray();
                    Array.Sort(_models, (ModelInfo a, ModelInfo b) =>
                        string.CompareOrdinal(a.FileName, b.FileName));
                    OnModelsListUpdated?.Invoke();
                });
            });
        }

        public External3DModel LoadModelAsync(string fileName, Transform parentTransform)
        {
            External3DModel externalModel = Instantiate(_external3DModelPrefab_, parentTransform)
                .GetComponent<External3DModel>();
            externalModel.Initialize(fileName);

            StartCoroutine(LoadGltfCoroutine(fileName, externalModel));

            return externalModel;
        }

        private IEnumerator LoadGltfCoroutine(string fileName, External3DModel externalModel)
        {
            // TODO(ghazen): This is a hack: Instead, alter External3DModel to support immediate
            // load completion callbacks
            yield return new WaitForEndOfFrame();

            LoadGltfSafe(fileName, externalModel);
        }

        private async Task LoadGltfSafe(string fileName, External3DModel external3DModel)
        {
            try
            {
                await LoadGltf(fileName, external3DModel);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private async Task LoadGltf(string fileName, External3DModel external3DModel)
        {
            Factory = Factory ?? ScriptableObject.CreateInstance<DefaultImporterFactory>();

            Debug.LogFormat("Trying to load file {0}", fileName);

            var importOptions = new ImportOptions
            {
                AsyncCoroutineHelper = gameObject.GetComponent<AsyncCoroutineHelper>() ??
                                       gameObject.AddComponent<AsyncCoroutineHelper>()
            };

            GLTFSceneImporter sceneImporter = null;
            ModelCacheEntry cacheEntry = null;
            try
            {
                if (_modelCache.TryGetValue(fileName, out cacheEntry))
                {
                    if (cacheEntry.Model != null)
                    {
                        Debug.LogFormat("Loaded model {0} from cache", fileName);
                        Instantiate(cacheEntry.Model, external3DModel.transform).SetActive(true);
                        external3DModel.OnLoadCompleted();
                        return;
                    }

                    Debug.LogFormat("Waiting for model {0} to load", fileName);
                    cacheEntry.ModelLoadQueue.AddLast(external3DModel);
                    return;
                }

                cacheEntry = new ModelCacheEntry();
                _modelCache[fileName] = cacheEntry;

                if (GetBuiltIn3DModelInfos().TryGetValue(fileName, out ModelInfo modelInfo))
                {
                    cacheEntry.Model = Instantiate(modelInfo.Prefab, transform);
                }

                if (cacheEntry.Model == null)
                {
                    string gltfPath = Path.Join(Application.persistentDataPath, fileName);

                    importOptions.DataLoader = new FileLoader(Path.GetDirectoryName(gltfPath));

                    sceneImporter = Factory.CreateSceneImporter(fileName, importOptions);

                    sceneImporter.SceneParent = transform;
                    sceneImporter.Collider = Collider;
                    sceneImporter.MaximumLod = MaximumLod;
                    sceneImporter.Timeout = Timeout;
                    sceneImporter.IsMultithreaded = Multithreaded;
                    sceneImporter.CustomShaderName = _shaderOverride ? _shaderOverride.name : null;

                    await sceneImporter.LoadSceneAsync();

                    cacheEntry.Model = sceneImporter.LastLoadedScene;
                }

                // Override the shaders on all materials if a shader is provided
                if (_shaderOverride != null)
                {
                    Renderer[] renderers = cacheEntry.Model.GetComponentsInChildren<Renderer>();
                    foreach (Renderer renderer in renderers)
                    {
                        renderer.sharedMaterial.shader = _shaderOverride;
                    }
                }

                Animation[] animations = cacheEntry.Model.GetComponents<Animation>();
                if (animations.Length > 0)
                {
                    animations[0].Play();
                }

                MeshCollider[] colliders = cacheEntry.Model.GetComponentsInChildren<MeshCollider>();
                foreach (MeshCollider collider in colliders)
                {
                    Rigidbody rigidbody = collider.gameObject.AddComponent<Rigidbody>();
                    rigidbody.useGravity = false;
                    rigidbody.isKinematic = true;
                    rigidbody.constraints = RigidbodyConstraints.FreezeAll;
                }

                cacheEntry.Model.SetActive(false);

                Instantiate(cacheEntry.Model, external3DModel.transform).SetActive(true);
                external3DModel.OnLoadCompleted();

                foreach (External3DModel otherModel in cacheEntry.ModelLoadQueue)
                {
                    Instantiate(cacheEntry.Model, otherModel.transform).SetActive(true);
                    otherModel.OnLoadCompleted();
                }
                cacheEntry.ModelLoadQueue.Clear();
            }
            catch (FileNotFoundException e)
            {
                Debug.LogWarningFormat("File {0} not found", e.FileName);
                external3DModel.OnLoadFailed(true);

                if (cacheEntry != null)
                {
                    foreach (External3DModel otherModel in cacheEntry.ModelLoadQueue)
                    {
                        otherModel.OnLoadFailed(true);
                    }

                    _modelCache.Remove(fileName);
                }
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("Exception while trying to load model: {0}", e);
                external3DModel.OnLoadFailed(false);

                if (cacheEntry != null)
                {
                    foreach (External3DModel otherModel in cacheEntry.ModelLoadQueue)
                    {
                        otherModel.OnLoadFailed(false);
                    }

                    _modelCache.Remove(fileName);
                }
            }
            finally
            {
                if (importOptions.DataLoader != null)
                {
                    sceneImporter?.Dispose();
                    importOptions.DataLoader = null;
                }
            }
        }

        private Dictionary<string, ModelInfo> GetBuiltIn3DModelInfos()
        {
            Dictionary<string, ModelInfo> modelInfos = new();
            foreach (GameObject gameObject in _builtIn3DModels)
            {
                if (gameObject == null)
                {
                    Debug.LogError("Built in 3D Model asset missing!");
                    continue;
                }
                ModelInfo modelInfo = new();
                modelInfo.FileName = gameObject.name + ".glb";
                modelInfo.Prefab = gameObject;
                modelInfos.Add(modelInfo.FileName, modelInfo);
            }

            return modelInfos;
        }
    }
}