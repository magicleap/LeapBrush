// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2019-2024) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using System;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.NativeTypes;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif // UNITY_EDITOR

namespace MagicLeap.LeapBrush
{
#if UNITY_EDITOR
    [OpenXRFeature(UiName = "Leap Brush Premultiplied Alpha",
        Desc = "Enable pre-multiplied alpha for rendered openxr layers.",
        Company = "Magic Leap",
        Version = "1.0.0",
        BuildTargetGroups = new[] { BuildTargetGroup.Android, BuildTargetGroup.Standalone },
        FeatureId = FeatureId
    )]
#endif // UNITY_EDITOR
    public class LeapBrushPremultipliedAlphaFeature : OpenXRFeature
    {
        public const string FeatureId
            = "com.magicleap.openxr.feature.ml2_premultiplied_alpha";

        private static XrGetInstanceProcAddr _originalGetInstanceProcAddr;
        private static XrEndFrame _originalEndFrame;

        private static bool _usePremultipliedAlpha = true;

        internal delegate XrResult XrGetInstanceProcAddr(ulong instance, [MarshalAs(UnmanagedType.LPStr)] string name, ref IntPtr pointer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal unsafe delegate XrResult XrEndFrame(ulong session, XrFrameEndInfo* frameEndInfo);

        internal enum XrRenderingStructTypes : ulong
        {
            XrTypeFrameEndInfo = 12,
            XrTypeFrameEndInfoML = 1000135000U,
            XrTypeGlobalDimmerFrameEndInfo = 1000136000U,
            XrTypeCompositionLayerProjectionView = 48,
            XrTypeCompositionLayerProjection = 35,
        }

        [Flags]
        internal enum XrCompositionLayerFlags : ulong
        {
            CorrectChomaticAberrationBit = 0x00001,
            BlendTextureSourceAlpha = 2,
            UnPreMultipliedAlpha = 4,
        }

        internal struct XrCompositionLayerBaseHeader
        {
            internal XrRenderingStructTypes Type;
            internal IntPtr Next;
            internal XrCompositionLayerFlags LayerFlags;
            internal ulong Space;
        }

        internal unsafe struct XrFrameEndInfo
        {
            internal XrRenderingStructTypes Type;
            internal IntPtr Next;
            internal long DisplayTime;
            internal XrEnvironmentBlendMode EnvironmentBlendMode;
            internal uint LayerCount;
            internal XrCompositionLayerBaseHeader** Layers;
        }

        protected override IntPtr HookGetInstanceProcAddr(IntPtr func)
        {
            _originalGetInstanceProcAddr = Marshal.GetDelegateForFunctionPointer<XrGetInstanceProcAddr>(
                base.HookGetInstanceProcAddr(func));
            XrGetInstanceProcAddr interceptedDelegate = GetInstanceProcAddr;
            return Marshal.GetFunctionPointerForDelegate(interceptedDelegate);
        }

        [MonoPInvokeCallback(typeof(XrGetInstanceProcAddr))]
        private static XrResult GetInstanceProcAddr(ulong instance, [MarshalAs(UnmanagedType.LPStr)] string functionName, ref IntPtr funcAddr)
        {
            var result = _originalGetInstanceProcAddr(instance, functionName, ref funcAddr);
            switch (functionName)
            {
                case "xrEndFrame":
                {
                    _originalEndFrame = Marshal.GetDelegateForFunctionPointer<XrEndFrame>(funcAddr);
                    unsafe
                    {
                        XrEndFrame endFrame = EndFrame;
                        funcAddr = Marshal.GetFunctionPointerForDelegate(endFrame);
                    }
                    break;
                }
            }
            return result;
        }

        [MonoPInvokeCallback(typeof(XrGetInstanceProcAddr))]
        private unsafe static XrResult EndFrame(ulong session, XrFrameEndInfo* frameEndInfo)
        {
            if (_usePremultipliedAlpha && frameEndInfo->LayerCount > 0)
            {
                XrCompositionLayerBaseHeader* layer = *frameEndInfo->Layers;
                layer->LayerFlags &= (~XrCompositionLayerFlags.UnPreMultipliedAlpha);
            }

            return _originalEndFrame(session, frameEndInfo);
        }

        public void SetUsePremultipliedAlpha(bool usePremultipliedAlpha)
        {
            _usePremultipliedAlpha = usePremultipliedAlpha;
        }
    }
}
