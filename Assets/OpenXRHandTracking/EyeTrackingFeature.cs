using UnityEngine.XR.OpenXR.Features;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using UnityEngine.XR.OpenXR;


namespace openxr
{
#if UNITY_EDITOR
    [UnityEditor.XR.OpenXR.Features.OpenXRFeature(UiName = "Eye tracking Extension",
        BuildTargetGroups = new[] {
            UnityEditor.BuildTargetGroup.Standalone, UnityEditor.BuildTargetGroup.WSA, UnityEditor.BuildTargetGroup.Android },
        Company = "ousttrue",
        Desc = "Enable eye tracking in unity",
        DocumentationLink = "https://developer.oculus.com/documentation/native/android/move-eye-tracking/",
        OpenxrExtensionStrings = xr_extension,
        Version = "0.0.1",
        FeatureId = featureId)]
#endif
    public class EyeTrackingFeature : OpenXRFeature
    {
        public const string featureId = "com.ousttrue.eyetracking";
        public const string xr_extension = "XR_FB_eye_tracking_social";
        public const int XR_TYPE_EYE_TRACKER_CREATE_INFO_FB = 1000202001;
        public const int XR_TYPE_EYE_GAZES_INFO_FB = 1000202002;
        public const int XR_TYPE_EYE_GAZES_FB = 1000202003;
        public const int XR_TYPE_SYSTEM_EYE_TRACKING_PROPERTIES_FB = 1000202004;

        /*
        typedef struct XrEyeTrackerCreateInfoV2FB {
            XrStructureType type;
            const void* XR_MAY_ALIAS next;
        } XrEyeTrackerCreateInfoV2FB;
        */
        [StructLayout(LayoutKind.Sequential)]
        internal struct XrEyeTrackerCreateInfoV2FB
        {
            public int type;
            public IntPtr next;
        }

        /*
        typedef XrResult(XRAPI_PTR* PFN_xrCreateEyeTrackerFB)(
            XrSession session,
            const XrEyeTrackerCreateInfoV2FB* createInfo,
            XrEyeTrackerFB* eyeTracker);
        */
        internal delegate int PFN_xrCreateEyeTrackerFB(
            ulong session,
            in XrEyeTrackerCreateInfoV2FB createInfo,
            out ulong eyeTracker);


        /*
        typedef XrResult(XRAPI_PTR* PFN_xrDestroyEyeTrackerFB)(XrEyeTrackerFB eyeTracker);
        */
        internal delegate int PFN_xrDestroyEyeTrackerFB(ulong eyeTracker);

        /*
        typedef struct XrEyeGazesInfoFB {
            XrStructureType type;
            const void* XR_MAY_ALIAS next;
            XrSpace baseSpace;
            XrTime time;
        } XrEyeGazesInfoFB;
        */
        [StructLayout(LayoutKind.Sequential)]
        internal struct XrEyeGazesInfoFB
        {
            public int type;
            public IntPtr next;
            public ulong baseSpace;
            public long time;
        }

        /*
        typedef struct XrEyeGazeV2FB {
            XrBool32 isValid;
            XrPosef gazePose;
            float gazeConfidence;
        } XrEyeGazeV2FB;
        */
        [StructLayout(LayoutKind.Sequential)]
        internal struct XrEyeGazeV2FB
        {
            public int isValid;
            public XrPosef gazePose;
            public float gazeConfidence;
        }

        /*
        typedef struct XrEyeGazesV2FB {
            XrStructureType type;
            void* XR_MAY_ALIAS next;
            XrEyeGazeV2FB gaze[2];
            XrTime time;
        } XrEyeGazesV2FB;
        */
        [StructLayout(LayoutKind.Sequential)]
        internal struct XrEyeGazesV2FB
        {
            public int type;
            public IntPtr next;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public XrEyeGazeV2FB[] gaze;
            public long time;
        }

        /*
        typedef XrResult(XRAPI_PTR* PFN_xrGetEyeGazesFB)(
            XrEyeTrackerFB eyeTracker,
            const XrEyeGazesInfoFB* gazeInfo,
            XrEyeGazesV2FB* eyeGazes);
        */
        internal delegate int PFN_xrGetEyeGazesFB(
            ulong eyeTracker,
            in XrEyeGazesInfoFB gazeInfo,
            ref XrEyeGazesV2FB eyeGazes);

        PFN_xrCreateEyeTrackerFB xrCreateEyeTrackerFB_ = null;
        PFN_xrDestroyEyeTrackerFB xrDestroyEyeTrackerFB_ = null;
        PFN_xrGetEyeGazesFB xrGetEyeGazesFB_ = null;

        ulong instance_;
        ulong session_;
        public event Action SessionBegin;
        public event Action SessionEnd;
        ulong handle_;

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
            xrCreateEyeTrackerFB_ = Marshal.GetDelegateForFunctionPointer<PFN_xrCreateEyeTrackerFB>(getAddr("xrCreateEyeTrackerFB"));
            xrDestroyEyeTrackerFB_ = Marshal.GetDelegateForFunctionPointer<PFN_xrDestroyEyeTrackerFB>(getAddr("xrDestroyEyeTrackerFB"));
            xrGetEyeGazesFB_ = Marshal.GetDelegateForFunctionPointer<PFN_xrGetEyeGazesFB>(getAddr("xrGetEyeGazesFB"));

            {
                var create = new XrEyeTrackerCreateInfoV2FB
                {
                    type = XR_TYPE_EYE_TRACKER_CREATE_INFO_FB,
                };
                var retVal = xrCreateEyeTrackerFB_(session, create, out handle_);
                if (retVal != 0)
                {
                    Debug.LogWarning("Couldn't open left  hand tracker: Error " + retVal);
                    return;
                }
                Debug.Log($"Create EyeTracker: {handle_}");
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
            xrDestroyEyeTrackerFB_(handle_);
            handle_ = 0;
            session_ = 0;
        }

        public bool TryGetGaze(out Vector3 lp, out Quaternion lr, out Vector3 rp, out Quaternion rr)
        {
            if (handle_ == 0)
            {
                lp = default;
                lr = default;
                rp = default;
                rr = default;
                return false;
            }

            var eyeGazes = new XrEyeGazesV2FB
            {
                type = XR_TYPE_EYE_GAZES_FB
            };

            var gazesInfo = new XrEyeGazesInfoFB
            {
                type = XR_TYPE_EYE_GAZES_INFO_FB,
                baseSpace = OpenXRFeature.GetCurrentAppSpace(),
            };

            var ret = xrGetEyeGazesFB_(handle_, in gazesInfo, ref eyeGazes);
            if (ret != 0)
            {
                lp = default;
                lr = default;
                rp = default;
                rr = default;
                return false;
            }

            lp = eyeGazes.gaze[0].gazePose.position.ToUnity();
            lr = eyeGazes.gaze[0].gazePose.orientation.ToUnity();
            rp = eyeGazes.gaze[1].gazePose.position.ToUnity();
            rr = eyeGazes.gaze[1].gazePose.orientation.ToUnity();

            return true;
        }
    }
}
