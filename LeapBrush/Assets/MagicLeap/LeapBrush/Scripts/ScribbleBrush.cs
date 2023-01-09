using System;
using System.Collections.Generic;
using MagicLeap.DesignToolkit.Audio;
using UnityEngine;

namespace MagicLeap.LeapBrush
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshCollider))]
    [RequireComponent(typeof(GenericAudioHandler))]
    public class ScribbleBrush : BrushBase
    {
        [SerializeField]
        private SoundDefinition _drawStartSound;

        [SerializeField]
        private SoundDefinition _drawEndSound;

        private const float BrushEndCapLength = .001f;
        private const float BrushHalfWidth = .01f;
        private const float MinDistanceAddPose = .0025f;

        private bool _initialized;
        private GenericAudioHandler _audioHandler;
        private DateTimeOffset _lastPoseChangeTime = DateTimeOffset.MinValue;
        private bool _playEndSoundAfterTimeout;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly TimeSpan DrawingPausedSoundTimeout = TimeSpan.FromSeconds(0.25f);

        public event Action OnPosesAdded;

        private void Awake()
        {
            EnsureInitialized();
            _audioHandler = GetComponent<GenericAudioHandler>();
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

            ApplyDrawingTipPoses();

            RebuildMesh(_poses);

            _initialized = true;
        }

        private void Update()
        {
            if (_drawing) {
                Pose lastPose = _poses[^1];
                Pose nextPose = new Pose(_brushControllerTransform.position,
                    _brushControllerTransform.rotation);
                if ((nextPose.position - lastPose.position).sqrMagnitude >= MinDistanceAddPose * MinDistanceAddPose)
                {
                    _poses.Add(nextPose);
                    RebuildMesh(_poses);

                    OnPosesAdded?.Invoke();
                }
            }
            else if (_brushControllerTransform != null)
            {
                transform.SetPositionAndRotation(_brushControllerTransform.position,
                    _brushControllerTransform.rotation);
            }

            if (_playEndSoundAfterTimeout && DateTimeOffset.Now
                > _lastPoseChangeTime + DrawingPausedSoundTimeout)
            {
                _audioHandler.PlaySoundSafe(_drawEndSound);
                _playEndSoundAfterTimeout = false;
            }
        }

        private void StartDrawing()
        {
            _drawing = true;

            Vector3 endCapStartPoint = transform.TransformPoint(new Vector3(-BrushEndCapLength, 0, 0));

            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            _poses.Clear();
            _poses.Add(new Pose(endCapStartPoint, _brushControllerTransform.rotation));
            _poses.Add(new Pose(_brushControllerTransform.position, _brushControllerTransform.rotation));
            RebuildMesh(_poses);

            OnPosesAdded?.Invoke();

            _audioHandler.PlaySoundSafe(_drawStartSound);
        }

        private void StopDrawing()
        {
            _drawing = false;
            DispatchOnDrawingCompleted();

            ApplyDrawingTipPoses();
            RebuildMesh(_poses);

            _audioHandler.PlaySoundSafe(_drawEndSound);
        }

        public override void SetPosesAndTruncate(int startIndex, IList<Pose> poses,
            bool receivedDrawing)
        {
            EnsureInitialized();

            var timeSpanSincePoseChanged = DateTimeOffset.Now - _lastPoseChangeTime;
            _lastPoseChangeTime = DateTimeOffset.Now;

            if (receivedDrawing && _audioHandler != null &&
                (startIndex == 0 || timeSpanSincePoseChanged > DrawingPausedSoundTimeout))
            {
                _audioHandler.PlaySoundSafe(_drawStartSound);
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

        public override void OnTriggerButtonDown()
        {
            StartDrawing();
        }

        public override void OnTriggerButtonUp()
        {
            if (_drawing)
            {
                StopDrawing();
            }
        }

        private void ApplyDrawingTipPoses()
        {
            _poses.Clear();
            _poses.Add(new Pose(new Vector3(-BrushEndCapLength, 0, 0), Quaternion.identity));
            _poses.Add(new Pose(new Vector3(0, 0, 0), Quaternion.identity));
        }

        private void RebuildMesh(IList<Pose> poses)
        {
            var mesh = GetComponent<MeshFilter>().mesh;
            mesh.Clear();

            if (poses.Count < 2)
            {
                return;
            }

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