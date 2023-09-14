using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    /// <summary>
    /// The scribble brush tool and brush stroke.
    /// </summary>
    /// <remarks>
    /// The scribble brush allows the user to draw while the select input action is active in a
    /// free-form drawing mode. Any motion by the controller while drawing will be picked up as new brush
    /// pose.
    ///
    /// <para>The algorithm will only append new brush poses if the user moves the tool
    /// far enough from the previous pose.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshCollider))]
    public class ScribbleBrush : BrushBase
    {
        /// <summary>
        /// Event fired when poses have been added to this brush.
        /// </summary>
        public event Action<ScribbleBrush> OnPosesAdded;

        [SerializeField]
        private AudioSource _drawStartSound;

        [SerializeField]
        private AudioSource _drawEndSound;

        private const float BrushHalfWidth = .01f;

        private bool _initialized;
        private DateTimeOffset _lastPoseChangeTime = DateTimeOffset.MinValue;
        private bool _playEndSoundAfterTimeout;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly TimeSpan DrawingPausedSoundTimeout = TimeSpan.FromSeconds(0.25f);

        public void AddPose(Pose pose)
        {
            _poses.Add(pose);
            RebuildMesh(_poses);
        }

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

        private void Update()
        {
            if (_playEndSoundAfterTimeout && DateTimeOffset.Now
                > _lastPoseChangeTime + DrawingPausedSoundTimeout)
            {
                _drawEndSound.Play();
                _playEndSoundAfterTimeout = false;
            }
        }

        public override void SetPosesAndTruncate(int startIndex, IList<Pose> poses,
            bool receivedDrawing)
        {
            EnsureInitialized();

            var timeSpanSincePoseChanged = DateTimeOffset.Now - _lastPoseChangeTime;
            _lastPoseChangeTime = DateTimeOffset.Now;

            if (receivedDrawing &&
                (startIndex == 0 || timeSpanSincePoseChanged > DrawingPausedSoundTimeout))
            {
                _drawStartSound.Play();
                _playEndSoundAfterTimeout = true;
            }

            if (startIndex < _poses.Count)
            {
                _poses.RemoveRange(startIndex, _poses.Count - startIndex);
            }

            _poses.AddRange(poses);
            RebuildMesh(_poses);

            var meshCollider = GetComponent<MeshCollider>();
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = GetComponent<MeshFilter>().mesh;
        }

        public override void SetColors(Color32 strokeColor, Color32 fillColor,
            float fillDimmerAlpha)
        {
            Material material = GetComponent<MeshRenderer>().material;
            material.SetColor(BaseColorId, strokeColor);
            material.SetColor(EmissionColorId, (Color)(strokeColor) / 4.0f);
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
                return;
            }

            // Create a ribbon strip connecting the poses that make up the brush stroke.

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
        }
    }
}