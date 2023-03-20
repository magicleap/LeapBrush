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

        public static bool EpsilonEquals(Quaternion quaternion, QuaternionProto quaternionProto)
        {
            return EpsilonEquals(quaternion.x, quaternionProto.X)
                   && EpsilonEquals(quaternion.y, quaternionProto.Y)
                   && EpsilonEquals(quaternion.z, quaternionProto.Z)
                   && EpsilonEquals(quaternion.w, quaternionProto.W);
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

        private static bool EpsilonEquals(float a, float b)
        {
            return Mathf.Abs(a - b) < Mathf.Epsilon;
        }

        public static TransformProto ToProto(Transform transform)
        {
            return new TransformProto()
            {
                Position = ToProto(transform.localPosition),
                Rotation = ToProto(transform.localRotation),
                Scale = ToProto(transform.localScale)
            };
        }

        public static PoseProto ToProto(Pose pose)
        {
            return new PoseProto
            {
                Position = ToProto(pose.position),
                Rotation = ToProto(pose.rotation)
            };
        }

        private static Vector3Proto ToProto(Vector3 p)
        {
            return new Vector3Proto{ X = p.x, Y = p.y, Z = p.z };
        }

        private static QuaternionProto ToProto(Quaternion q)
        {
            return new QuaternionProto{ X = q.x, Y = q.y, Z = q.z, W = q.w };
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
    }
}