
using System;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

namespace openxr
{
#if UNITY_EDITOR
    [UnityEditor.XR.OpenXR.Features.OpenXRFeature(UiName = "Body tracking Extension",
        BuildTargetGroups = new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android },
        Company = "Ousttrue",
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
        public const int XR_TYPE_BODY_TRACKER_CREATE_INFO_FB = 1000076001;
        public const int XR_TYPE_BODY_JOINTS_LOCATE_INFO_FB = 1000076002;
        public const int XR_TYPE_BODY_JOINT_LOCATIONS_V1_FB = 1000076003;
        public const int XR_TYPE_SYSTEM_BODY_TRACKING_PROPERTIES_FB = 1000076004;
        public const int XR_TYPE_BODY_JOINT_LOCATIONS_FB = 1000076005;
        public const int XR_TYPE_BODY_SKELETON_FB = 1000076006;

        ulong instance_;
        ulong session_;

        internal enum XrBodyJointSetFB
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
        internal struct XrBodyTrackerCreateInfoFB
        {
            public int type;
            public IntPtr next;
            public XrBodyJointSetFB bodyJointSet;
        }

        /*
        XRAPI_ATTR XrResult XRAPI_CALL xrCreateBodyTrackerFB(
            XrSession session,
            const XrBodyTrackerCreateInfoFB* createInfo,
            XrBodyTrackerFB* bodyTracker);
        */
        internal delegate int PFN_xrCreateBodyTrackerFB(ulong session,
            in XrBodyTrackerCreateInfoFB createInfo,
            out ulong bodyTracker);

        /*
        XRAPI_ATTR XrResult XRAPI_CALL xrDestroyBodyTrackerFB(XrBodyTrackerFB bodyTracker);
        */
        internal delegate int PFN_xrDestroyBodyTrackerFB(ulong bodyTracker);

        /*
        typedef struct XrBodyJointsLocateInfoFB {
            XrStructureType type;
            const void* XR_MAY_ALIAS next;
            XrSpace baseSpace;
            XrTime time;
        } XrBodyJointsLocateInfoFB;
        */
        [StructLayout(LayoutKind.Sequential)]
        internal struct XrBodyJointsLocateInfoFB
        {
            public int type;
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
        internal struct XrBodyJointLocationFB
        {
            public HandTrackingFeature.XrSpaceLocationFlags locationFlags;
            public HandTrackingFeature.XrPosef pose;
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
        internal struct XrBodyJointLocationsFB
        {
            public int type;
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
        internal delegate int PFN_xrLocateBodyJointsFB(
            ulong bodyTracker,
            in XrBodyJointsLocateInfoFB locateInfo,
            ref XrBodyJointLocationsFB locations);

        PFN_xrCreateBodyTrackerFB xrCreateBodyTrackerFB_ = null;
        PFN_xrDestroyBodyTrackerFB xrDestroyBodyTrackerFB_ = null;
        PFN_xrLocateBodyJointsFB xrLocateBodyJointsFB_ = null;

        ulong handle_;
        public event Action SessionBegin;
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
            CloseHandTracker(ref handle_);
            instance_ = 0;
        }

        override protected void OnSessionBegin(ulong session)
        {
            session_ = session;
            Debug.Log($"{featureId}: {instance_}.{session_}");

            var getInstanceProcAddr = Marshal.GetDelegateForFunctionPointer<FrameTimeFeature.Type_xrGetInstanceProcAddr>(xrGetInstanceProcAddr);
            Func<string, IntPtr> getAddr = (string name) =>
            {
                IntPtr ptr;
                getInstanceProcAddr(instance_, name, out ptr);
                return ptr;
            };
            xrCreateBodyTrackerFB_ = Marshal.GetDelegateForFunctionPointer<PFN_xrCreateBodyTrackerFB>(getAddr("xrCreateBodyTrackerFB"));
            xrDestroyBodyTrackerFB_ = Marshal.GetDelegateForFunctionPointer<PFN_xrDestroyBodyTrackerFB>(getAddr("xrDestroyBodyTrackerFB"));
            xrLocateBodyJointsFB_ = Marshal.GetDelegateForFunctionPointer<PFN_xrLocateBodyJointsFB>(getAddr("xrLocateBodyJointsFB"));

            {
                var create = new XrBodyTrackerCreateInfoFB
                {
                    type = XR_TYPE_BODY_TRACKER_CREATE_INFO_FB
                };
                var retVal = xrCreateBodyTrackerFB_(session, create, out handle_);
                if (retVal != 0)
                {
                    Debug.Log("Couldn't open body hand tracker: Error " + retVal);
                    return;
                }
            }

            if (SessionBegin != null)
            {
                SessionBegin();
            }
        }

        override protected void OnSessionEnd(ulong session)
        {
            Debug.Log($"OnSessionEnd: {instance_}.{session_}");
            if (SessionEnd != null)
            {
                SessionEnd();
            }
            CloseHandTracker(ref handle_);
            session_ = 0;
        }

        override protected void OnSessionDestroy(ulong xrSession)
        {
            Debug.Log("OnSessionDestroy");
            CloseHandTracker(ref handle_);
        }

        void CloseHandTracker(ref ulong handle)
        {
            if (handle != 0)
            {
                xrDestroyBodyTrackerFB_(handle);
                handle = 0;
            }
        }

        public bool TryGetJoints(long frame_time, out Vector3[] positions, out Quaternion[] orientations)
        {
            if (handle_ == 0)
            {
                Debug.LogWarning("zero");
                positions = default;
                orientations = default;
                return false;
            }

            var allJoints = new XrBodyJointLocationFB[XR_BODY_JOINT_COUNT_FB];
            using (var pin = new ArrayPin(allJoints))
            {
                var jli = new XrBodyJointsLocateInfoFB
                {
                    type = XR_TYPE_BODY_JOINTS_LOCATE_INFO_FB,
                    baseSpace = OpenXRFeature.GetCurrentAppSpace(),
                    time = frame_time,
                };
                var joints = new XrBodyJointLocationsFB
                {
                    type = XR_TYPE_BODY_JOINT_LOCATIONS_FB,
                    jointCount = (uint)allJoints.Length,
                    jointLocations = pin.Ptr,
                };
                int retVal = xrLocateBodyJointsFB_(handle_, jli, ref joints);
                if (retVal != 0)
                {
                    Debug.Log($"false: {retVal}");
                    positions = default;
                    orientations = default;
                    return false;
                }
            }

            positions = new Vector3[allJoints.Length];
            orientations = new Quaternion[allJoints.Length];
            for (int c = 0; c < allJoints.Length; c++)
            {
                positions[c] = allJoints[c].pose.position.PosToUnity();
                orientations[c] = allJoints[c].pose.orientation.OrientationToUnity();
            }
            return true;
        }
    }
}