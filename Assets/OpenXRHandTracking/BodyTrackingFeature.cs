
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;


namespace openxr
{
#if UNITY_EDITOR
    [UnityEditor.XR.OpenXR.Features.OpenXRFeature(UiName = "Body tracking Extension",
        BuildTargetGroups = new[] {
            UnityEditor.BuildTargetGroup.Standalone, UnityEditor.BuildTargetGroup.Android },
        Company = "ousttrue",
        Desc = "Enable body tracking in unity",
        DocumentationLink = "https://developer.oculus.com/documentation/native/android/move-body-tracking/",
        OpenxrExtensionStrings = xr_extension,
        Version = "0.0.1",
        FeatureId = featureId)]
#endif
    public class BodyTrackingFeature : OpenXRFeature
    {
        public const string featureId = "com.ousttrue.bodytracking";
        public const string xr_extension = "XR_FB_body_tracking";
        public const int XR_BODY_JOINT_COUNT_FB = 70;

        public enum XrBodyJointSetFB
        {
            XR_BODY_JOINT_SET_DEFAULT_FB = 0,
            XR_BODY_JOINT_SET_MAX_ENUM_FB = 0x7FFFFFFF
        };

        /*
        typedef struct XrBodyTrackerCreateInfoFB {
            XrStructureType type;
            const void* XR_MAY_ALIAS next;
            XrBodyJointSetFB bodyJointSet;
        } XrBodyTrackerCreateInfoFB;
        */
        [StructLayout(LayoutKind.Sequential)]
        public struct XrBodyTrackerCreateInfoFB
        {
            public XrStructureType type;
            public IntPtr next;
            public XrBodyJointSetFB bodyJointSet;
        }

        /*
        XRAPI_ATTR XrResult XRAPI_CALL xrCreateBodyTrackerFB(
            XrSession session,
            const XrBodyTrackerCreateInfoFB* createInfo,
            XrBodyTrackerFB* bodyTracker);
        */
        public delegate XrResult PFN_xrCreateBodyTrackerFB(ulong session,
            in XrBodyTrackerCreateInfoFB createInfo,
            out ulong bodyTracker);
        PFN_xrCreateBodyTrackerFB xrCreateBodyTrackerFB_;
        public PFN_xrCreateBodyTrackerFB XrCreateBodyTrackerFB => xrCreateBodyTrackerFB_;

        /*
        XRAPI_ATTR XrResult XRAPI_CALL xrDestroyBodyTrackerFB(XrBodyTrackerFB bodyTracker);
        */
        public delegate XrResult PFN_xrDestroyBodyTrackerFB(ulong bodyTracker);
        PFN_xrDestroyBodyTrackerFB xrDestroyBodyTrackerFB_;
        public PFN_xrDestroyBodyTrackerFB XrDestroyBodyTrackerFB => xrDestroyBodyTrackerFB_;

        /*
        typedef struct XrBodyJointsLocateInfoFB {
            XrStructureType type;
            const void* XR_MAY_ALIAS next;
            XrSpace baseSpace;
            XrTime time;
        } XrBodyJointsLocateInfoFB;
        */
        [StructLayout(LayoutKind.Sequential)]
        public struct XrBodyJointsLocateInfoFB
        {
            public XrStructureType type;
            public IntPtr next;
            public ulong baseSpace;
            public long time;
        }

        /*
        typedef struct XrBodyJointLocationFB {
            XrSpaceLocationFlags locationFlags;
            XrPosef pose;
        } XrBodyJointLocationFB;
        */
        [StructLayout(LayoutKind.Sequential)]
        public struct XrBodyJointLocationFB
        {
            public XrSpaceLocationFlags locationFlags;
            public XrPosef pose;
        }

        /*
        typedef struct XrBodyJointLocationsFB {
            XrStructureType type;
            void* XR_MAY_ALIAS next;
            XrBool32 isActive;
            float confidence;
            uint32_t jointCount;
            XrBodyJointLocationFB* jointLocations;
            uint32_t skeletonChangedCount;
            XrTime time;
        } XrBodyJointLocationsFB;
        */
        [StructLayout(LayoutKind.Sequential)]
        public struct XrBodyJointLocationsFB
        {
            public XrStructureType type;
            public IntPtr next;
            public int isActive;
            public float confidence;
            public uint jointCount;
            public IntPtr jointLocations;
            public uint skeletonChangedCount;
            public long time;
        }

        /*
        XRAPI_ATTR XrResult XRAPI_CALL xrLocateBodyJointsFB(
            XrBodyTrackerFB bodyTracker,
            const XrBodyJointsLocateInfoFB* locateInfo,
            XrBodyJointLocationsFB* locations);
        */
        public delegate XrResult PFN_xrLocateBodyJointsFB(
            ulong bodyTracker,
            in XrBodyJointsLocateInfoFB locateInfo,
            ref XrBodyJointLocationsFB locations);
        PFN_xrLocateBodyJointsFB xrLocateBodyJointsFB_;
        public PFN_xrLocateBodyJointsFB XrLocateBodyJointsFB => xrLocateBodyJointsFB_;

        Type_xrCreateReferenceSpace xrCreateReferenceSpace_;
        public Type_xrCreateReferenceSpace XrCreateReferenceSpace => xrCreateReferenceSpace_;

        ulong instance_;
        ulong session_;

        public event Action<BodyTrackingFeature, ulong> SessionBegin;
        public event Action SessionEnd;


        override protected bool OnInstanceCreate(ulong xrInstance)
        {
            instance_ = xrInstance;
            if (!OpenXRRuntime.IsExtensionEnabled(xr_extension))
            {
                Debug.LogWarning($"{xr_extension} is not enabled.");
                // Return false here to indicate the system should disable your feature for this execution.  
                // Note that if a feature is marked required, returning false will cause the OpenXRLoader to abort and try another loader.
                return false;
            }

            return true;
        }

        override protected void OnInstanceDestroy(ulong xrInstance)
        {
            instance_ = 0;
        }

        override protected void OnSessionBegin(ulong session)
        {
            session_ = session;
            Debug.Log($"{featureId}: {instance_}.{session_}");

            var getInstanceProcAddr = Marshal.GetDelegateForFunctionPointer<PFN_xrGetInstanceProcAddr>(xrGetInstanceProcAddr);
            Func<string, IntPtr> getAddr = (string name) =>
            {
                IntPtr ptr;
                getInstanceProcAddr(instance_, name, out ptr);
                return ptr;
            };
            xrCreateBodyTrackerFB_ = Marshal.GetDelegateForFunctionPointer<PFN_xrCreateBodyTrackerFB>(getAddr("xrCreateBodyTrackerFB"));
            xrDestroyBodyTrackerFB_ = Marshal.GetDelegateForFunctionPointer<PFN_xrDestroyBodyTrackerFB>(getAddr("xrDestroyBodyTrackerFB"));
            xrLocateBodyJointsFB_ = Marshal.GetDelegateForFunctionPointer<PFN_xrLocateBodyJointsFB>(getAddr("xrLocateBodyJointsFB"));
            xrCreateReferenceSpace_ = Marshal.GetDelegateForFunctionPointer<Type_xrCreateReferenceSpace>(getAddr("xrCreateReferenceSpace"));

            if (SessionBegin != null)
            {
                SessionBegin(this, session_);
            }
        }

        override protected void OnSessionEnd(ulong session)
        {
            Debug.Log($"{featureId}: OnSessionEnd: {instance_}.{session_}");
            if (SessionEnd != null)
            {
                SessionEnd();
            }
            session_ = 0;
        }

        override protected void OnSessionDestroy(ulong xrSession)
        {
        }
    }
}