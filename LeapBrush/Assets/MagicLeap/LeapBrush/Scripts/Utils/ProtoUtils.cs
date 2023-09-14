using System;
using Google.Protobuf.Collections;
using MagicLeap.LeapBrush;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap
{
    /// <summary>
    /// Utilities for working with Protocol Buffers in the Leap Brush api.
    /// </summary>
    public class ProtoUtils
    {
        public static bool EpsilonEquals(Transform transform, TransformProto transformProto)
        {
            return EpsilonEquals(transform.localPosition, transformProto.Position)
                   && EpsilonEquals(transform.localRotation, transformProto.Rotation)
                   && EpsilonEquals(transform.localScale, transformProto.Scale);
        }

        public static bool EpsilonEquals(Transform transform, PoseAndScale poseAndScale)
        {
            return EpsilonEquals(transform.localPosition, poseAndScale.Pose.position)
                   && EpsilonEquals(transform.localRotation, poseAndScale.Pose.rotation)
                   && EpsilonEquals(transform.localScale, poseAndScale.Scale);

        }

        public static bool EpsilonEquals(Pose pose, PoseProto poseProto)
        {
            return EpsilonEquals(pose.position, poseProto.Position)
                   && EpsilonEquals(pose.rotation, poseProto.Rotation);
        }

        public static bool EpsilonEquals(Vector3 vector, Vector3Proto vectorProto)
        {
            return EpsilonEquals(vector.x, vectorProto.X)
                   && EpsilonEquals(vector.y, vectorProto.Y)
                   && EpsilonEquals(vector.z, vectorProto.Z);
        }

        public static bool EpsilonEquals(Vector3 a, Vector3 b)
        {
            return EpsilonEquals(a.x, b.x)
                   && EpsilonEquals(a.y, b.y)
                   && EpsilonEquals(a.z, b.z);
        }

        public static bool EpsilonEquals(Quaternion quaternion, QuaternionProto quaternionProto)
        {
            return EpsilonEquals(quaternion.x, quaternionProto.X)
                   && EpsilonEquals(quaternion.y, quaternionProto.Y)
                   && EpsilonEquals(quaternion.z, quaternionProto.Z)
                   && EpsilonEquals(quaternion.w, quaternionProto.W);
        }

        public static bool EpsilonEquals(Quaternion a, Quaternion b)
        {
            return EpsilonEquals(a.x, b.x)
                   && EpsilonEquals(a.y, b.y)
                   && EpsilonEquals(a.z, b.z)
                   && EpsilonEquals(a.w, b.w);
        }

        public static bool EpsilonEquals(PoseProto a, PoseProto b)
        {
            return EpsilonEquals(a.Position, b.Position)
                   && EpsilonEquals(a.Rotation, b.Rotation);
        }

        public static bool EpsilonEquals(Vector3Proto a, Vector3Proto b)
        {
            return EpsilonEquals(a.X, b.X)
                   && EpsilonEquals(a.Y, b.Y)
                   && EpsilonEquals(a.Z, b.Z);
        }

        public static bool EpsilonEquals(QuaternionProto a, QuaternionProto b)
        {
            return EpsilonEquals(a.X, b.X)
                   && EpsilonEquals(a.Y, b.Y)
                   && EpsilonEquals(a.Z, b.Z)
                   && EpsilonEquals(a.W, b.W);
        }

        public static bool EpsilonEquals(float a, float b)
        {
            return Mathf.Abs(a - b) < Mathf.Epsilon;
        }

        public static TransformProto ToProto(Transform transform, TransformProto outProto = null)
        {
            if (outProto == null)
            {
                outProto = new ();
            }

            outProto.Position = ToProto(transform.localPosition, outProto.Position);
            outProto.Rotation = ToProto(transform.localRotation, outProto.Rotation);
            outProto.Scale = ToProto(transform.localScale, outProto.Scale);
            return outProto;
        }

        public static PoseProto ToProto(Pose pose, PoseProto outProto = null)
        {
            if (outProto == null)
            {
                outProto = new ();
            }

            outProto.Position = ToProto(pose.position, outProto.Position);
            outProto.Rotation = ToProto(pose.rotation, outProto.Rotation);
            return outProto;
        }

        private static Vector3Proto ToProto(Vector3 p, Vector3Proto outProto = null)
        {
            if (outProto == null)
            {
                outProto = new ();
            }

            outProto.X = p.x;
            outProto.Y = p.y;
            outProto.Z = p.z;
            return outProto;
        }

        private static QuaternionProto ToProto(Quaternion q, QuaternionProto outProto = null)
        {
            if (outProto == null)
            {
                outProto = new ();
            }

            outProto.X = q.x;
            outProto.Y = q.y;
            outProto.Z = q.z;
            outProto.W = q.w;
            return outProto;
        }

        public struct PoseAndScale
        {
            public Pose Pose;
            public Vector3 Scale;
        }

        public static PoseAndScale FromProto(TransformProto transformProto)
        {
            PoseAndScale poseAndScale = new PoseAndScale();
            poseAndScale.Pose = new Pose(ProtoUtils.FromProto(transformProto.Position),
                ProtoUtils.FromProto(transformProto.Rotation));
            poseAndScale.Scale = ProtoUtils.FromProto(transformProto.Scale);
            return poseAndScale;
        }

        public static Pose FromProto(PoseProto poseProto)
        {
            return new Pose(ProtoUtils.FromProto(poseProto.Position),
                ProtoUtils.FromProto(poseProto.Rotation));
        }

        public static Vector3 FromProto(Vector3Proto p)
        {
            return new Vector3{ x = p.X, y = p.Y, z = p.Z };
        }

        public static Quaternion FromProto(QuaternionProto q)
        {
            return new Quaternion{ x = q.X, y = q.Y, z = q.Z, w = q.W };
        }

        public static Pose[] FromProto(RepeatedField<PoseProto> posesProto)
        {
            Pose[] poses = new Pose[posesProto.Count];
            for (int i = 0; i < posesProto.Count; ++i)
            {
                poses[i] = FromProto(posesProto[i]);
            }
            return poses;
        }

        public static SpaceInfoProto.Types.MappingMode ToProto(MLAnchors.MappingMode mappingMode)
        {
            switch (mappingMode)
            {
                case MLAnchors.MappingMode.OnDevice:
                    return SpaceInfoProto.Types.MappingMode.OnDevice;
                case MLAnchors.MappingMode.ARCloud:
                    return SpaceInfoProto.Types.MappingMode.ArCloud;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(mappingMode), mappingMode, null);
            }
        }

        public static MLAnchors.MappingMode FromProto(SpaceInfoProto.Types.MappingMode mappingMode)
        {
            switch (mappingMode)
            {
                case SpaceInfoProto.Types.MappingMode.OnDevice:
                    return MLAnchors.MappingMode.OnDevice;
                case SpaceInfoProto.Types.MappingMode.ArCloud:
                    return MLAnchors.MappingMode.ARCloud;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(mappingMode), mappingMode, null);
            }
        }

        /// <summary>
        /// Convert a tool enum value to the protocol buffer equivalent.
        /// </summary>
        public static UserStateProto.Types.ToolState ToProto(ToolType currentTool)
        {
            switch (currentTool)
            {
                case ToolType.Eraser:
                    return UserStateProto.Types.ToolState.Eraser;
                case ToolType.Laser:
                    return UserStateProto.Types.ToolState.Laser;
                case ToolType.BrushScribble:
                    return UserStateProto.Types.ToolState.BrushScribble;
                case ToolType.BrushPoly:
                    return UserStateProto.Types.ToolState.BrushPoly;
                default:
                    throw new ArgumentOutOfRangeException(nameof(currentTool), currentTool, null);
            }
        }

        public static BatteryStatusProto.Types.BatteryState ToProto(BatteryStatus batteryStatus)
        {
            switch (batteryStatus)
            {
                case BatteryStatus.Unknown:
                    return BatteryStatusProto.Types.BatteryState.Unknown;
                case BatteryStatus.Charging:
                    return BatteryStatusProto.Types.BatteryState.Charging;
                case BatteryStatus.Discharging:
                    return BatteryStatusProto.Types.BatteryState.Discharging;
                case BatteryStatus.NotCharging:
                    return BatteryStatusProto.Types.BatteryState.NotCharging;
                case BatteryStatus.Full:
                    return BatteryStatusProto.Types.BatteryState.Full;
                default:
                    throw new ArgumentOutOfRangeException(nameof(batteryStatus), batteryStatus, null);
            }
        }

        public static void ToProto(Vector3[] vectors, RepeatedField<Vector3Proto> vectorsProto)
        {
            while (vectorsProto.Count > vectors.Length)
            {
                vectorsProto.RemoveAt(vectorsProto.Count - 1);
            }

            for (int i = 0; i < vectorsProto.Count; i++)
            {
                ToProto(vectors[i], vectorsProto[i]);
            }

            for (int i = vectorsProto.Count; i < vectors.Length; i++)
            {
                vectorsProto.Add(ToProto(vectors[i]));
            }
        }

        public static bool EpsilonEquals(
            RepeatedField<Vector3Proto> a, RepeatedField<Vector3Proto> b)
        {
            if (a.Count != b.Count)
            {
                return false;
            }

            for (int i = 0; i < a.Count; i++)
            {
                if (!EpsilonEquals(a[i], b[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}