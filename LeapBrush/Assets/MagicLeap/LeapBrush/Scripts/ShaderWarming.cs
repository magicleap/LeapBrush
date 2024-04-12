using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MixedReality.Toolkit;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Scene = UnityEngine.SceneManagement.Scene;

namespace MagicLeap.LeapBrush
{
    public class ShaderWarming : MonoBehaviour
    {
        [Serializable]
        public class VertexAttributeDescriptorSerializable
        {
            public VertexAttribute attribute;
            public VertexAttributeFormat format;
            public int dimension;
            public int stream;

            public VertexAttributeDescriptorSerializable(VertexAttributeDescriptor vertexAttribute)
            {
                attribute = vertexAttribute.attribute;
                format = vertexAttribute.format;
                dimension = vertexAttribute.dimension;
                stream = vertexAttribute.stream;
            }

            private VertexAttributeDescriptorSerializable()
            {
            }

            public VertexAttributeDescriptor ToDescriptor()
            {
                return new VertexAttributeDescriptor
                {
                    attribute = attribute,
                    format = format,
                    dimension = dimension,
                    stream = stream
                };
            }

            public string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(attribute).Append("-");
                sb.Append(dimension).Append("-");
                sb.Append(format).Append("-");
                sb.Append(stream);
                return sb.ToString();
            }

            public static VertexAttributeDescriptorSerializable FromString(string str)
            {
                string[] pieces = str.Split("-");
                if (pieces.Length != 4)
                {
                    return null;
                }

                return new VertexAttributeDescriptorSerializable
                {
                    attribute = Enum.Parse<VertexAttribute>(pieces[0]),
                    dimension = int.Parse(pieces[1]),
                    format = Enum.Parse<VertexAttributeFormat>(pieces[2]),
                    stream = int.Parse(pieces[3])
                };
            }

            protected bool Equals(VertexAttributeDescriptorSerializable other)
            {
                return attribute == other.attribute && format == other.format &&
                       dimension == other.dimension && stream == other.stream;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((VertexAttributeDescriptorSerializable) obj);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine((int) attribute, (int) format, dimension, stream);
            }

            public static VertexAttributeDescriptorSerializable[] FromArray(
                VertexAttributeDescriptor[] attributes)
            {
                var result = new VertexAttributeDescriptorSerializable[attributes.Length];
                for (int i = 0; i < attributes.Length; i++)
                {
                    result[i] = new VertexAttributeDescriptorSerializable(attributes[i]);
                }

                return result;
            }

            public static VertexAttributeDescriptor[] ToArray(
                IList<VertexAttributeDescriptorSerializable> attributes)
            {
                var result = new VertexAttributeDescriptor[attributes.Count];
                for (int i = 0; i < attributes.Count; i++)
                {
                    result[i] = attributes[i].ToDescriptor();
                }

                return result;
            }
        }

        [Serializable]
        public class ShaderWarmingInfo
        {
            public Shader shader;

            public List<string> keywords = new();

            public List<VertexAttributeDescriptorSerializable> vertexDescriptors = new();

            public String ToString()
            {
                StringBuilder sb = new();
                sb.Append(shader.name);
                sb.Append(";");
                sb.Append(string.Join(",", keywords));
                sb.Append(";");
                for (int i = 0; i < vertexDescriptors.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(",");
                    }

                    sb.Append(vertexDescriptors[i].ToString());
                }

                return sb.ToString();
            }
        }

        [FormerlySerializedAs("_shaderWarmingInfos")]
        public List<ShaderWarmingInfo> ShaderWarmingInfos = new();

        [SerializeField]
        [Tooltip("Whether to capture all used shaders from the scene at runtime. Shaders that " +
            "were not yet in the prewarm collection will be logged an can then be imported into " +
            "this component.")]
        private bool _captureUsedShaders;

        private SortedSet<string> _shaderWarmedStrs = new();
        private SortedSet<string> _shaderUsedStrs = new();
        private HashSet<GameObject> _warmingGameObjects = new();

        private void Awake()
        {
            Debug.Log($"ShaderWarming.Awake Warming {ShaderWarmingInfos.Count()} shaders");

            foreach (ShaderWarmingInfo warmingInfo in ShaderWarmingInfos)
            {
                GameObject warmingGameObject = new GameObject(warmingInfo.ToString());
                _warmingGameObjects.Add(warmingGameObject);
                warmingGameObject.transform.SetParent(gameObject.transform);
                warmingGameObject.transform.localScale = Vector3.one * 0.00001f;

                MeshRenderer meshRenderer = warmingGameObject.AddComponent<MeshRenderer>();

                Material material = new Material(warmingInfo.shader);
                HashSet<String> expectedKeywords = new();
                foreach (String keyword in warmingInfo.keywords)
                {
#if !UNITY_ANDROID || UNITY_EDITOR
                    if (keyword == "STEREO_MULTIVIEW_ON")
                    {
                        continue;
                    }
#endif
                    expectedKeywords.Add(keyword);
                    material.EnableKeyword(keyword.Trim());
                }

                foreach (String presentKeyword in material.shaderKeywords)
                {
                    if (!expectedKeywords.Contains(presentKeyword))
                    {
                        material.DisableKeyword(presentKeyword);
                    }
                }

                meshRenderer.material = material;

                MeshFilter meshFilter = warmingGameObject.AddComponent<MeshFilter>();
                meshFilter.mesh = CreateMeshWithVertexAttributes(warmingInfo.vertexDescriptors);

                AddUsedShadersToSet(meshRenderer, _shaderWarmedStrs);
            }
        }

        void Start()
        {
            var mainCameraTransform = Camera.main.transform;
            transform.position = mainCameraTransform.position
                                 + mainCameraTransform.forward * 1.0f;
            transform.LookAt(mainCameraTransform);

            if (_captureUsedShaders)
            {
                StartCoroutine(CaptureShaderUseCoroutine());
                StartCoroutine(LogShaderUsePeriodically());
            }

            StartCoroutine(DeactivatePrewarmsAfterDelay());
        }

        private IEnumerator DeactivatePrewarmsAfterDelay()
        {
            // Wait for a number of frames and a timed delay before disabling the prewarm objects.
            for (int i = 0; i < ShaderWarmingInfos.Count; i++)
            {
                yield return null;
            }
            yield return new WaitForSeconds(5);

            Debug.Log("ShaderWarming.DeactivatePrewarmsAfterDelay");

            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
            }
        }

        private IEnumerator CaptureShaderUseCoroutine()
        {
            while (true)
            {
                yield return new WaitForEndOfFrame();

                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    foreach (GameObject rootGameObject in scene.GetRootGameObjects())
                    {
                        AddUsedShadersToSetRecursive(rootGameObject, _shaderUsedStrs,
                            _warmingGameObjects);
                    }
                }
            }
        }

        private IEnumerator LogShaderUsePeriodically()
        {
            while (true)
            {
                yield return new WaitForSeconds(5);

                DumpShadersToLog();
            }
        }

        private static void AddUsedShadersToSetRecursive(GameObject gameObject,
            SortedSet<string> shaderUsedStrs, HashSet<GameObject> ignoreGameObjects)
        {
            if (ignoreGameObjects.Contains(gameObject))
            {
                return;
            }

            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                AddUsedShadersToSet(renderer, shaderUsedStrs);
            }

            CanvasRenderer canvasRenderer = gameObject.GetComponent<CanvasRenderer>();
            if (canvasRenderer != null)
            {
                AddUsedShadersToSet(canvasRenderer, shaderUsedStrs);
            }

            foreach (Transform childTransform in gameObject.transform)
            {
                AddUsedShadersToSetRecursive(childTransform.gameObject, shaderUsedStrs,
                    ignoreGameObjects);
            }
        }

        private static void AddUsedShadersToSet(Renderer renderer, SortedSet<string> shaderUsedStrs)
        {
            Mesh mesh;
            if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
            {
                mesh = skinnedMeshRenderer.sharedMesh;
            }
            else if (renderer is LineRenderer lineRenderer)
            {
                mesh = new Mesh();
                lineRenderer.BakeMesh(mesh);
            }
            else
            {
                var meshFilter = renderer.GetComponent<MeshFilter>();
                if (!meshFilter)
                {
                    return;
                }

                mesh = meshFilter.sharedMesh;
            }

            if (mesh == null)
            {
                return;
            }

            AddUsedShadersToSet(mesh, renderer.sharedMaterials, shaderUsedStrs, false);
        }

        private static void AddUsedShadersToSet(CanvasRenderer renderer,
            SortedSet<string> shaderUsedStrs)
        {
            Mesh mesh = renderer.GetMesh();
            if (mesh == null)
            {
                return;
            }

            bool underRectMask2D = renderer.GetComponent<IClippable>() != null &&
                                   renderer.FindAncestorComponent<RectMask2D>();

            Material[] materials = new Material[renderer.materialCount];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = renderer.GetMaterial(i);
            }

            AddUsedShadersToSet(mesh, materials, shaderUsedStrs, underRectMask2D);
        }

        private static void AddUsedShadersToSet(Mesh mesh, Material[] materials,
            SortedSet<string> shaderUsedStrs, bool underRectMask2D)
        {
            VertexAttributeDescriptor[]
                vertexAttributes = mesh.GetVertexAttributes();

            foreach (Material material in materials)
            {
                SortedSet<string> keywords = new();
                foreach (string keyword in material.shaderKeywords)
                {
                    keywords.Add(keyword);
                }

                if (underRectMask2D)
                {
                    keywords.Add("UNITY_UI_CLIP_RECT");
                }

                {
                    StringBuilder sb = new();
                    sb.Append(material.shader.name);
                    sb.Append(";");
                    sb.Append(string.Join(",", keywords));
                    sb.Append(";");
                    for (int i = 0; i < vertexAttributes.Length; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append(",");
                        }

                        sb.Append(new VertexAttributeDescriptorSerializable(vertexAttributes[i])
                            .ToString());
                    }

                    shaderUsedStrs.Add(sb.ToString());
                }
            }
        }

        private Mesh CreateMeshWithVertexAttributes(
            IList<VertexAttributeDescriptorSerializable> vertexAttributes)
        {
            Mesh mesh = new Mesh();
            mesh.SetVertexBufferParams(
                4, VertexAttributeDescriptorSerializable.ToArray(vertexAttributes));

            foreach (VertexAttributeDescriptorSerializable vertexAttributeDescriptor in
                     vertexAttributes)
            {
                if (vertexAttributeDescriptor.attribute == VertexAttribute.Position)
                {
                    mesh.vertices = new Vector3[]
                    {
                        new(0, 0, 0),
                        new(1, 0, 0),
                        new(0, 1, 0),
                        new(1, 1, 0)
                    };

                    mesh.triangles = new[]
                    {
                        0, 2, 1,
                        2, 3, 1
                    };
                }
            }

            bool matchesExpected = true;

            VertexAttributeDescriptorSerializable[]
                actualVertexAttributes = VertexAttributeDescriptorSerializable.FromArray(
                    mesh.GetVertexAttributes());
            if (vertexAttributes.Count != actualVertexAttributes.Length)
            {
                matchesExpected = false;
            }
            else
            {
                for (int i = 0; i < actualVertexAttributes.Length; i++)
                {
                    if (!vertexAttributes[i].Equals(actualVertexAttributes[i]))
                    {
                        matchesExpected = false;
                        break;
                    };
                }
            }

            if (!matchesExpected)
            {
                StringBuilder sb = new StringBuilder(
                    "Generated mesh for warming does not have expected vertex attributes:");
                sb.Append(" Expected: [");
                for (int i = 0; i < vertexAttributes.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(",");
                    }

                    sb.Append(vertexAttributes[i].ToString());
                }
                sb.Append("], Actual: [");
                for (int i = 0; i < actualVertexAttributes.Length; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(",");
                    }

                    sb.Append(actualVertexAttributes[i].ToString());
                }
                sb.Append("]");
                throw new Exception(sb.ToString());
            }

            return mesh;
        }

        public void DumpShadersToLog()
        {
            int countUsedWarmed = 0;
            int countUsedNotWarmed = 0;
            foreach (string shaderStrUsed in _shaderUsedStrs)
            {
                if (_shaderWarmedStrs.Contains(shaderStrUsed))
                {
                    countUsedWarmed++;
                }
                else
                {
                    countUsedNotWarmed++;
                }
            }

            Debug.Log($"ShaderWarming.DumpShaders: {_shaderWarmedStrs.Count} warmed, " +
                      $"{countUsedWarmed} used (warmed), {countUsedNotWarmed} used (not warmed):");

            foreach (string shaderStr in _shaderWarmedStrs)
            {
                if (!_shaderUsedStrs.Contains(shaderStr))
                {
                    Debug.Log($"ShaderWarming.Shader WarmedNotUsed;{shaderStr}");
                }
            }

            foreach (string shaderStr in _shaderUsedStrs)
            {
                if (!_shaderWarmedStrs.Contains(shaderStr))
                {
                    Debug.Log($"ShaderWarming.Shader UsedNotWarmed;{shaderStr}");
                }
            }
        }
    }
}
