using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;
using UnityEngine.Rendering;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// Manager for fetching the list of available 3D models to load, gltf model loader, etc.
    /// </summary>
    public class External3DModelManager : MonoBehaviour
    {
        [SerializeField]
        private GameObject _external3DModelPrefab;

        [Serializable]
        public class BuiltIn3DModel
        {
            public GameObject Prefab;
            public float Scale = 1.0f;
        }

        [SerializeField]
        private BuiltIn3DModel[] _builtIn3DModels = Array.Empty<BuiltIn3DModel>();

        private class ModelCacheEntry
        {
            public GameObject Model;
            public LinkedList<External3DModel> ModelLoadQueue = new();
        }

        private Dictionary<String, ModelCacheEntry> _modelCache = new();

        private LinkedList<Action> _modelLoadQueue = new();

        public event Action OnModelsListUpdated;

        private ModelInfo[] _models = Array.Empty<ModelInfo>();

        public class ModelInfo
        {
            public string FileName;
            public string Path;
            public GameObject Prefab;
            public float Scale = 1.0f;
        }

        public ModelInfo[] Models => _models;

        private void Update()
        {
            if (_modelLoadQueue.Count > 0)
            {
                _modelLoadQueue.First.Value();
                _modelLoadQueue.RemoveFirst();
            }
        }

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
            External3DModel externalModel = Instantiate(_external3DModelPrefab, parentTransform)
                .GetComponent<External3DModel>();
            externalModel.Initialize(fileName);

            _modelLoadQueue.AddLast(() =>
            {
                if (externalModel != null)
                {
                    LoadGltfSafe(fileName, externalModel);
                }
            });

            return externalModel;
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
            Debug.LogFormat("Trying to load file {0}", fileName);

            GltfImport gltfImporter = new GltfImport(logger: new ConsoleLogger());
            ModelCacheEntry cacheEntry = null;
            try
            {
                if (_modelCache.TryGetValue(fileName, out cacheEntry))
                {
                    if (cacheEntry.Model != null)
                    {
                        Debug.LogFormat("Loaded model {0} from cache", fileName);
                        InstantiateModelInstanceAndAttach(external3DModel, cacheEntry.Model);
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
                    cacheEntry.Model.transform.localScale = Vector3.one * modelInfo.Scale;
                }

                if (cacheEntry.Model == null)
                {
                    string gltfPath = Path.Join(Application.persistentDataPath, fileName);

                    if (!File.Exists(gltfPath))
                    {
                        throw new FileNotFoundException();
                    }

                    bool result = await gltfImporter.Load(new Uri(new Uri("file://"), gltfPath));
                    if (!result)
                    {
                        throw new Exception("Gltf Load Failed");
                    }

                    GameObject instantiateParent = new GameObject("InstantiateTemp");
                    instantiateParent.transform.SetParent(transform);

                    bool instantiateResult = gltfImporter.InstantiateMainScene(
                        instantiateParent.transform);
                    if (!instantiateResult)
                    {
                        throw new Exception("Gltf InstantiateMainScene failed");
                    }

                    if (instantiateParent.transform.childCount == 0)
                    {
                        throw new Exception("Gltf InstantiateMainScene created no content");
                    }

                    if (instantiateParent.transform.childCount > 1)
                    {
                        Debug.LogWarning($"More than one scene loaded for {fileName}");
                    }

                    cacheEntry.Model = instantiateParent.transform.GetChild(0).gameObject;
                    cacheEntry.Model.transform.SetParent(transform);
                    Destroy(instantiateParent);
                }

                Animation[] animations = cacheEntry.Model.GetComponents<Animation>();
                if (animations.Length > 0)
                {
                    animations[0].Play();
                }

                foreach (Renderer renderer in
                         cacheEntry.Model.GetComponentsInChildren<Renderer>())
                {
                    if (renderer is MeshRenderer
                        && !renderer.gameObject.GetComponent<MeshCollider>())
                    {
                        renderer.gameObject.AddComponent<MeshCollider>();
                    }

                    renderer.receiveShadows = false;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                }

                cacheEntry.Model.SetActive(false);

                InstantiateModelInstanceAndAttach(external3DModel, cacheEntry.Model);

                foreach (External3DModel otherModel in cacheEntry.ModelLoadQueue)
                {
                    InstantiateModelInstanceAndAttach(otherModel, cacheEntry.Model);
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
        }

        private void InstantiateModelInstanceAndAttach(
            External3DModel external3DModel, GameObject cachedModel)
        {
            GameObject modelInstance = Instantiate(cachedModel, external3DModel.transform);
            modelInstance.SetActive(true);
            external3DModel.OnLoadCompleted(modelInstance);
        }

        private Dictionary<string, ModelInfo> GetBuiltIn3DModelInfos()
        {
            Dictionary<string, ModelInfo> modelInfos = new();
            foreach (BuiltIn3DModel builtIn3dModel in _builtIn3DModels)
            {
                if (builtIn3dModel.Prefab == null)
                {
                    Debug.LogError("Built in 3D Model asset missing!");
                    continue;
                }
                ModelInfo modelInfo = new();
                modelInfo.FileName = builtIn3dModel.Prefab.name + ".glb";
                modelInfo.Prefab = builtIn3dModel.Prefab;
                modelInfo.Scale = builtIn3dModel.Scale;
                modelInfos.Add(modelInfo.FileName, modelInfo);
            }

            return modelInfos;
        }
    }
}