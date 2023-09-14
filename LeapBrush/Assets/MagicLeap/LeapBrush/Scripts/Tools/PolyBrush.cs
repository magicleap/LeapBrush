using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// The polygon brush stroke.
    /// </summary>
    /// <remarks>
    /// The polygon brush allows the user to precisely place a series of control points which are
    /// connected by brush strokes. The user can click on the previous or first control point
    /// to stop the drawing or create a loop. The drawing can optionally have a fill and/or
    /// segmented dimming fill value.
    ///
    /// <para>Similar to the ScribbleBrush, this algorithm creates a ribbon between the control
    /// points, however if a fill is selected, it also uses a simple triangle fan to attempt
    /// to draw a reasonable fill. This algorithm could be improved.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshCollider))]
    public class PolyBrush : BrushBase
    {
        [SerializeField]
        private GameObject _fillGameObject;

        [SerializeField]
        private GameObject _fillDimmerGameObject;

        [SerializeField]
        private Material _opaqueFillMaterial;

        [SerializeField]
        private Material _transparentFillMaterial;

        [SerializeField]
        private AudioSource _drawPointSound;

        private const float BrushHalfWidth = .01f;

        private bool _initialized;
        private Color32 _fillColor;
        private float _fillDimmerAlpha;
        private bool _fillMaterialIsOpaque = true;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            var meshFilter = GetComponent<MeshFilter>();

            Mesh mesh = meshFilter.mesh;
            if (mesh == null) {
                mesh = meshFilter.mesh = new Mesh() {
                    name = "Quad strip mesh"
                };
            };
            mesh.Clear();
            mesh.MarkDynamic();

            _initialized = true;
        }

        private void OnEnable()
        {
            RebuildMesh(_poses);
        }

        public override void SetPosesAndTruncate(int startIndex, IList<Pose> poses,
            bool receivedDrawing)
        {
            EnsureInitialized();

            if (receivedDrawing && startIndex >= _poses.Count)
            {
                _drawPointSound.Play();
            }

            if (startIndex < _poses.Count)
            {
                _poses.RemoveRange(startIndex, _poses.Count - startIndex);
            }

            _poses.AddRange(poses);
            RebuildMesh(_poses);

            if (_poses.Count > 1 &&
                !(_poses.Count == 2 && _poses[0].position == _poses[1].position))
            {
                var meshCollider = GetComponent<MeshCollider>();
                meshCollider.sharedMesh = null;
                meshCollider.sharedMesh = GetComponent<MeshFilter>().mesh;
            }

            if (_poses.Count > 2)
            {
                var fillMeshCollider = _fillGameObject.GetComponent<MeshCollider>();
                fillMeshCollider.sharedMesh = null;
                fillMeshCollider.sharedMesh = _fillGameObject.GetComponent<MeshFilter>().mesh;
            }
        }

        public override void SetColors(Color32 strokeColor, Color32 fillColor,
            float fillDimmerAlpha)
        {
            Material strokeMaterial = GetComponent<MeshRenderer>().material;
            strokeMaterial.SetColor(BaseColorId, strokeColor);
            strokeMaterial.SetColor(EmissionColorId, (Color)(strokeColor) / 4.0f);

            _fillColor = fillColor;
            _fillDimmerAlpha = fillDimmerAlpha;

            if (_fillGameObject != null)
            {
                _fillGameObject.SetActive(_fillColor != Color.clear);

                bool newFillMaterialIsOpaque = _fillColor.a == Byte.MaxValue;
                if (_fillMaterialIsOpaque != newFillMaterialIsOpaque)
                {
                    _fillMaterialIsOpaque = newFillMaterialIsOpaque;
                    _fillGameObject.GetComponent<MeshRenderer>().material =
                        _fillMaterialIsOpaque ? _opaqueFillMaterial : _transparentFillMaterial;
                }

                Material fillMaterial = _fillGameObject.GetComponent<MeshRenderer>().material;
                fillMaterial.SetColor(BaseColorId, fillColor);
                fillMaterial.SetColor(EmissionColorId, (Color) (fillColor) / 4.0f);
            }

            if (_fillDimmerGameObject != null)
            {
                _fillDimmerGameObject.SetActive(_fillDimmerAlpha > 0);
                _fillDimmerGameObject.GetComponent<MeshRenderer>().material
                    .SetColor(BaseColorId, new Color(
                        fillDimmerAlpha, fillDimmerAlpha, fillDimmerAlpha,
                        fillDimmerAlpha));
            }
        }

        public void AddPose(Pose pose)
        {
            _poses.Add(pose);
            RebuildMesh(_poses);
        }

        public void RemoveLastPose()
        {
            _poses.RemoveAt(_poses.Count - 1);
            RebuildMesh(_poses);
        }

        public void SetLastPose(Pose pose)
        {
            _poses[^1] = pose;
            RebuildMesh(_poses);
        }

        /// <summary>
        /// Rebuild the current mesh of this brush stroke with an updated set of poses.
        /// </summary>
        /// <param name="poses">The new poses to use for the mesh.</param>
        private void RebuildMesh(IList<Pose> poses)
        {
            var mesh = GetComponent<MeshFilter>().mesh;
            mesh.Clear();

            if (poses.Count < 2)
            {
                if (_fillGameObject != null)
                {
                    _fillGameObject.SetActive(false);
                }
                if (_fillDimmerGameObject != null)
                {
                    _fillDimmerGameObject.SetActive(false);
                }
                return;
            }

            // Create a ribbon strip for the lines (strokes) between control points.

            var vertices = new Vector3[poses.Count * 2];
            var triangles = new int[poses.Count * 6 - 6];

            for (int poseIdx = 0; poseIdx < poses.Count; ++poseIdx)
            {
                Pose pose = poses[poseIdx];
                Matrix4x4 trs = Matrix4x4.TRS(pose.position, pose.rotation, Vector3.one);
                vertices[poseIdx * 2] = trs.MultiplyPoint3x4(new Vector3(0, BrushHalfWidth, 0));
                vertices[poseIdx * 2 + 1] = trs.MultiplyPoint3x4(new Vector3(0, -BrushHalfWidth, 0));
            }

            for (int quadIdx = 0; quadIdx < poses.Count - 1; ++quadIdx)
            {
                triangles[quadIdx * 6] = quadIdx * 2;
                triangles[quadIdx * 6 + 1] = quadIdx * 2 + 1;
                triangles[quadIdx * 6 + 2] = quadIdx * 2 + 2;
                triangles[quadIdx * 6 + 3] = quadIdx * 2 + 2;
                triangles[quadIdx * 6 + 4] = quadIdx * 2 + 1;
                triangles[quadIdx * 6 + 5] = quadIdx * 2 + 3;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;

            mesh.RecalculateNormals();

            // Update the fill mesh if there are enough poses and colors were selected.

            if (_fillGameObject != null && _fillDimmerGameObject != null)
            {
                _fillGameObject.SetActive(poses.Count > 2 && _fillColor != Color.clear);
                _fillDimmerGameObject.SetActive(poses.Count > 2 && _fillDimmerAlpha > 0);
                if (poses.Count > 2)
                {
                    RebuildFillMesh(poses);
                }
            }
        }

        /// <summary>
        /// Rebuild the fill and dimmer meshes with new poses.
        /// </summary>
        /// <param name="poses">The new poses that should make up the fill mesh</param>
        private void RebuildFillMesh(IList<Pose> poses)
        {
            var mesh = _fillGameObject.GetComponent<MeshFilter>().mesh;
            mesh.Clear();
            mesh.MarkDynamic();

            var vertices = new Vector3[poses.Count];
            var triangles = new int[poses.Count * 3 - 6];

            for (int poseIdx = 0; poseIdx < poses.Count; ++poseIdx)
            {
                vertices[poseIdx] = poses[poseIdx].position;
            }

            for (int triangleIdx = 0; triangleIdx < poses.Count - 2; ++triangleIdx)
            {
                triangles[triangleIdx * 3] = 0;
                triangles[triangleIdx * 3 + 1] = triangleIdx + 1;
                triangles[triangleIdx * 3 + 2] = triangleIdx + 2;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;

            mesh.RecalculateNormals();

            _fillDimmerGameObject.GetComponent<MeshFilter>().mesh = mesh;
        }
    }
}