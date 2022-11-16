using UnityEngine.XR.OpenXR.Features;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using UnityEngine.XR.OpenXR;


namespace openxr
{
#if UNITY_EDITOR
    [UnityEditor.XR.OpenXR.Features.OpenXRFeature(UiName = "Hand tracking Extension",
        BuildTargetGroups = new[] {
            UnityEditor.BuildTargetGroup.Standalone, UnityEditor.BuildTargetGroup.WSA, UnityEditor.BuildTargetGroup.Android },
        Company = "Joe M",
        Desc = "Enable hand tracking in unity",
        DocumentationLink = "https://docs.unity3d.com/Packages/com.unity.xr.openxr@0.1/manual/index.html",
        OpenxrExtensionStrings = xr_extension,
        Version = "0.0.1",
        FeatureId = featureId)]
#endif
    public class HandTrackingFeature : OpenXRFeature
    {
        public const string featureId = "com.joemarshall.handtracking";
        public const string xr_extension = "XR_EXT_hand_tracking";
        public const int XR_HAND_JOINT_COUNT_EXT = 26;

        public const int XR_TYPE_HAND_TRACKER_CREATE_INFO_EXT = 1000051001;
        public const int XR_TYPE_HAND_JOINTS_LOCATE_INFO_EXT = 1000051002;
        public const int XR_TYPE_HAND_JOINT_LOCATIONS_EXT = 1000051003;

        public enum Hand_Index { L, R };

        // get the address of the hand tracking functions using: OpenXRFeature.xrGetInstanceProcAddr

        /*XrResult xrCreateHandTrackerEXT(
            XrSession                                   session,
            const XrHandTrackerCreateInfoEXT*           createInfo,
            XrHandTrackerEXT*                           handTracker);*/
        internal delegate int Type_xrCreateHandTrackerEXT(ulong session, in XrHandTrackerCreateInfoEXT createInfo, out ulong tracker);

        /*
            XrResult xrDestroyHandTrackerEXT(XrHandTrackerEXT handTracker);
        */
        internal delegate int Type_xrDestroyHandTrackerEXT(ulong tracker);

        /*XrResult xrLocateHandJointsEXT(
            XrHandTrackerEXT                            handTracker,
            const XrHandJointsLocateInfoEXT*            locateInfo,
            XrHandJointLocationsEXT*                    locations);*/
        internal delegate int Type_xrLocateHandJointsEXT(ulong tracker, in XrHandJointsLocateInfoEXT locateInfoEXT, ref XrHandJointLocationsEXT locations);

        /*typedef struct XrHandJointsLocateInfoEXT {
            XrStructureType    type;
            const void*        next;
            XrSpace            baseSpace;
            XrTime             time;
        } XrHandJointsLocateInfoEXT;*/
        [StructLayout(LayoutKind.Sequential)]
        internal struct XrHandJointsLocateInfoEXT
        {
            public int stype;
            public IntPtr next;
            public ulong space;
            public long time;
        };

        /*
        typedef struct XrHandJointLocationEXT {
            XrSpaceLocationFlags    locationFlags;
            XrPosef                 pose;
            float                   radius;
        } XrHandJointLocationEXT;
        */
        [StructLayout(LayoutKind.Sequential)]
        internal struct XrHandJointLocationEXT
        {
            // TODO check size of enums and types
            public ulong locationFlags;
            public XrPosef pose;
            public float radius; // joint radius
        }
        static GCHandle pinnedJointArray;
        /*typedef struct XrHandJointLocationsEXT {
        XrStructureType            type;
        void*                      next;
        XrBool32                   isActive;
        uint32_t                   jointCount;
        XrHandJointLocationEXT*    jointLocations;
    } XrHandJointLocationsEXT;*/
        [StructLayout(LayoutKind.Sequential)]
        internal struct XrHandJointLocationsEXT
        {
            public int stype;
            public IntPtr next;
            public int isActive;
            public uint jointCount;
            public IntPtr jointLocations;
        };

        /*typedef struct XrHandTrackerCreateInfoEXT {
            XrStructureType      type;
            const void*          next;
            XrHandEXT            hand;
            XrHandJointSetEXT    handJointSet;
        } XrHandTrackerCreateInfoEXT;*/
        [StructLayout(LayoutKind.Sequential)]
        internal struct XrHandTrackerCreateInfoEXT
        {
            public int stype;
            public IntPtr next;
            public int hand;
            public int handJointSet;
        }

        Type_xrCreateHandTrackerEXT xrCreateHandTrackerEXT_;
        Type_xrDestroyHandTrackerEXT xrDestroyHandTrackerEXT_;
        Type_xrLocateHandJointsEXT xrLocateHandJointsEXT_;

        ulong instance_;
        ulong session_;

        ulong handle_left_ = 0;
        ulong handle_right_ = 0;

        public event Action SessionBegin;
        public event Action SessionEnd;

        public ulong GetHandle(Hand_Index hand)
        {
            switch (hand)
            {
                case Hand_Index.L:
                    return handle_left_;
                case Hand_Index.R:
                    return handle_right_;
                default:
                    return 0;
            }
        }

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
            CloseHandTracker(ref handle_left_);
            CloseHandTracker(ref handle_right_);
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
            xrCreateHandTrackerEXT_ = Marshal.GetDelegateForFunctionPointer<Type_xrCreateHandTrackerEXT>(getAddr("xrCreateHandTrackerEXT"));
            xrDestroyHandTrackerEXT_ = Marshal.GetDelegateForFunctionPointer<Type_xrDestroyHandTrackerEXT>(getAddr("xrDestroyHandTrackerEXT"));
            xrLocateHandJointsEXT_ = Marshal.GetDelegateForFunctionPointer<Type_xrLocateHandJointsEXT>(getAddr("xrLocateHandJointsEXT"));

            {
                XrHandTrackerCreateInfoEXT lh_create = new XrHandTrackerCreateInfoEXT
                {
                    stype = XR_TYPE_HAND_TRACKER_CREATE_INFO_EXT,
                    hand = 1,
                };
                var retVal = xrCreateHandTrackerEXT_(session, lh_create, out handle_left_);
                if (retVal != 0)
                {
                    Debug.Log("Couldn't open left  hand tracker: Error " + retVal);
                    return;
                }
            }

            {
                XrHandTrackerCreateInfoEXT rh_create = new XrHandTrackerCreateInfoEXT
                {
                    stype = XR_TYPE_HAND_TRACKER_CREATE_INFO_EXT,
                    hand = 2,
                };
                var retVal = xrCreateHandTrackerEXT_(session, rh_create, out handle_right_);
                if (retVal != 0)
                {
                    Debug.Log("Couldn't open right  hand tracker: Error " + retVal);
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
            CloseHandTracker(ref handle_left_);
            CloseHandTracker(ref handle_right_);
            session_ = 0;
        }

        override protected void OnSessionDestroy(ulong xrSession)
        {
            Debug.Log("OnSessionDestroy");
            CloseHandTracker(ref handle_left_);
            CloseHandTracker(ref handle_right_);
        }

        void CloseHandTracker(ref ulong handle)
        {
            if (handle != 0)
            {
                xrDestroyHandTrackerEXT_(handle);
                handle = 0;
            }
        }

        public bool TryGetJoints(long frame_time, ulong handle, out Vector3[] positions, out Quaternion[] orientations, out float[] radius)
        {
            if (handle == 0)
            {
                positions = default;
                orientations = default;
                radius = default;
                return false;
            }

            var allJoints = new XrHandJointLocationEXT[XR_HAND_JOINT_COUNT_EXT];
            var jli = new XrHandJointsLocateInfoEXT
            {
                stype = XR_TYPE_HAND_JOINTS_LOCATE_INFO_EXT,
                space = OpenXRFeature.GetCurrentAppSpace(),
                time = frame_time,
            };
            using (var Pin = new ArrayPin(allJoints))
            {
                XrHandJointLocationsEXT joints = new XrHandJointLocationsEXT
                {
                    stype = XR_TYPE_HAND_JOINT_LOCATIONS_EXT,
                    jointCount = (uint)allJoints.Length,
                    jointLocations = Pin.Ptr,
                };
                int retVal = xrLocateHandJointsEXT_(handle, jli, ref joints);
                if (retVal != 0)
                {
                    positions = default;
                    orientations = default;
                    radius = default;
                    return false;
                }
            }

            positions = new Vector3[allJoints.Length];
            orientations = new Quaternion[allJoints.Length];
            radius = new float[allJoints.Length];
            for (int c = 0; c < allJoints.Length; c++)
            {
                positions[c] = allJoints[c].pose.position.ToUnity();
                orientations[c] = allJoints[c].pose.orientation.ToUnity();
                if ((allJoints[c].locationFlags & 0x3) == 0)
                {
                    radius[c] = 0f;
                }
                else
                {
                    radius[c] = allJoints[c].radius;
                }
            }
            return true;
        }
    }
}