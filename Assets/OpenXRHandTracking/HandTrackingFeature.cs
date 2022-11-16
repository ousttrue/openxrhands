using UnityEngine.XR.OpenXR.Features;
using UnityEngine;
using UnityEditor;
using System.Runtime.InteropServices;
using System;
using UnityEngine.XR.OpenXR;

namespace openxr
{
#if UNITY_EDITOR
    [UnityEditor.XR.OpenXR.Features.OpenXRFeature(UiName = "Hand tracking Extension",
        BuildTargetGroups = new[] { BuildTargetGroup.Standalone, BuildTargetGroup.WSA, BuildTargetGroup.Android },
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
        FrameTimeFeature.Type_xrGetInstanceProcAddr xrGetInstanceProcAddr_;

        public enum Hand_Index { L, R };
        ulong instance_;
        ulong session_;

        ulong handle_left_ = 0;
        ulong handle_right_ = 0;

        public ulong GetHandle(Hand_Index hand)
        {
            switch (hand)
            {
                case Hand_Index.L:
                    if (handle_left_ == 0)
                    {
                        Debug.LogError("handle_left==0");
                    }
                    return handle_left_;
                case Hand_Index.R:
                    if (handle_right_ == 0)
                    {
                        Debug.LogError("handle_right==0");
                    }
                    return handle_right_;
                default:
                    return 0;
            }
        }

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
            public XrHandJointsLocateInfoEXT(ulong space, long time)
            {
                stype = 1000051002;
                this.space = space;
                this.time = time;
                this.next = IntPtr.Zero;
            }
            int stype;
            IntPtr next;
            ulong space;
            long time;
        };

        enum XrSpaceLocationFlags
        {
            XR_SPACE_LOCATION_ORIENTATION_VALID_BIT = 0x00000001,
            XR_SPACE_LOCATION_POSITION_VALID_BIT = 0x00000002,
            XR_SPACE_LOCATION_ORIENTATION_TRACKED_BIT = 0x00000004,
            XR_SPACE_LOCATION_POSITION_TRACKED_BIT = 0x00000008
        };

        /*typedef struct XrVector2f {
            float    x;
            float    y;
        } XrVector2f;*/
        [StructLayout(LayoutKind.Sequential)]
        internal struct XrVector2f
        {
            public float x;
            public float y;
        }

        /*typedef struct XrVector3f {
            float    x;
            float    y;
            float    z;
        } XrVector3f;*/
        [StructLayout(LayoutKind.Sequential)]
        internal struct XrVector3f
        {
            public float x;
            public float y;
            public float z;

            public Vector3 PosToUnity()
            {
                return new Vector3(x, y, -z);
            }
        }

        /*typedef struct XrVector4f {
            float    x;
            float    y;
            float    z;
            float    w;
        } XrVector4f;*/
        [StructLayout(LayoutKind.Sequential)]
        internal struct XrVector4f
        {
            public float x;
            public float y;
            public float z;
            public float w;

            public Quaternion OrientationToUnity()
            {
                return new Quaternion(x, y, -z, -w);
            }
        }

        /*typedef struct XrPosef {
            XrQuaternionf    orientation;
            XrVector3f       position;
        } XrPosef;*/
        [StructLayout(LayoutKind.Sequential)]
        internal struct XrPosef
        {
            public XrVector4f orientation;
            public XrVector3f position;
        }

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
            public XrHandJointLocationsEXT(ref XrHandJointLocationEXT[] jointArray)
            {
                pinnedJointArray = GCHandle.Alloc(jointArray, GCHandleType.Pinned);
                jointCount = (uint)jointArray.Length;
                stype = 1000051003;
                next = IntPtr.Zero;
                isActive = 0;
                jointLocations = pinnedJointArray.AddrOfPinnedObject();
            }

            public void Unpin()
            {
                pinnedJointArray.Free();
            }
            int stype;
            IntPtr next;
            int isActive;
            uint jointCount;

            IntPtr jointLocations;
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
            public XrHandTrackerCreateInfoEXT(int hand)
            {
                this.stype = 1000051001;
                this.next = IntPtr.Zero;
                this.hand = hand;
                this.handJointSet = 0; // standard set of joints
            }
            int stype;
            IntPtr next;
            int hand;
            int handJointSet;
        }

        Type_xrCreateHandTrackerEXT xrCreateHandTrackerEXT_;
        Type_xrDestroyHandTrackerEXT xrDestroyHandTrackerEXT_;
        Type_xrLocateHandJointsEXT xrLocateHandJointsEXT_;

        public event Action SessionBegin;

        public HandTrackingFeature()
        {
            Debug.Log("new HandTrackingFeature");
        }

        ~HandTrackingFeature()
        {
            Debug.Log("delete HandTrackingFeature");
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

            xrGetInstanceProcAddr_ = Marshal.GetDelegateForFunctionPointer<FrameTimeFeature.Type_xrGetInstanceProcAddr>(xrGetInstanceProcAddr);
            return true;
        }

        override protected void OnInstanceDestroy(ulong xrInstance)
        {
            closeHandTracker();
            instance_ = 0;
        }

        override protected void OnSessionBegin(ulong session)
        {
            session_ = session;
            Debug.Log($"OnSessionBegin: {instance_}.{session_}");

            Func<string, IntPtr> getAddr = (string name) =>
            {
                IntPtr ptr;
                xrGetInstanceProcAddr_(instance_, name, out ptr);
                return ptr;
            };
            xrCreateHandTrackerEXT_ = Marshal.GetDelegateForFunctionPointer<Type_xrCreateHandTrackerEXT>(getAddr("xrCreateHandTrackerEXT"));
            xrDestroyHandTrackerEXT_ = Marshal.GetDelegateForFunctionPointer<Type_xrDestroyHandTrackerEXT>(getAddr("xrDestroyHandTrackerEXT"));
            xrLocateHandJointsEXT_ = Marshal.GetDelegateForFunctionPointer<Type_xrLocateHandJointsEXT>(getAddr("xrLocateHandJointsEXT"));

            {
                XrHandTrackerCreateInfoEXT lh_create = new XrHandTrackerCreateInfoEXT(1);
                var retVal = xrCreateHandTrackerEXT_(session, lh_create, out handle_left_);
                if (retVal != 0)
                {
                    Debug.Log("Couldn't open left  hand tracker: Error " + retVal);
                    return;
                }
            }

            {
                XrHandTrackerCreateInfoEXT rh_create = new XrHandTrackerCreateInfoEXT(2);
                var retVal = xrCreateHandTrackerEXT_(session, rh_create, out handle_right_);
                if (retVal != 0)
                {
                    Debug.Log("Couldn't open right  hand tracker: Error " + retVal);
                    return;
                }
            }

            SessionBegin();
        }

        public event Action SessionEnd;

        override protected void OnSessionEnd(ulong session)
        {
            Debug.Log($"OnSessionEnd: {instance_}.{session_}");
            SessionEnd();
            closeHandTracker();
            session_ = 0;
        }

        override protected void OnSessionDestroy(ulong xrSession)
        {
            Debug.Log("OnSessionDestroy");
            closeHandTracker();
        }

        void closeHandTracker()
        {
            if (handle_left_ != 0)
            {
                // Type_xrDestroyHandTrackerEXT fp = GetInstanceProc<Type_xrDestroyHandTrackerEXT>("xrDestroyHandTrackerEXT");
                // if (fp != null)
                // {
                //     fp(handle_left);
                // }
                xrDestroyHandTrackerEXT_(handle_left_);
                handle_left_ = 0;
            }
            if (handle_right_ != 0)
            {
                // Type_xrDestroyHandTrackerEXT fp = GetInstanceProc<Type_xrDestroyHandTrackerEXT>("xrDestroyHandTrackerEXT");
                // if (fp != null)
                // {
                //     fp(handle_right);
                // }
                xrDestroyHandTrackerEXT_(handle_right_);
                handle_right_ = 0;
            }
        }

        public bool TryGetHandJoints(long frame_time, ulong handle, out Vector3[] positions, out Quaternion[] orientations, out float[] radius)
        {
            if (handle == 0)
            {
                positions = default;
                orientations = default;
                radius = default;
                return false;
            }

            XrHandJointLocationEXT[] allJoints = new XrHandJointLocationEXT[26];
            XrHandJointsLocateInfoEXT jli = new XrHandJointsLocateInfoEXT(OpenXRFeature.GetCurrentAppSpace(), frame_time);
            XrHandJointLocationsEXT joints = new XrHandJointLocationsEXT(ref allJoints);
            int retVal = xrLocateHandJointsEXT_(handle, jli, ref joints);
            joints.Unpin();
            if (retVal != 0)
            {
                positions = default;
                orientations = default;
                radius = default;
                return false;
            }

            positions = new Vector3[allJoints.Length];
            orientations = new Quaternion[allJoints.Length];
            radius = new float[allJoints.Length];
            for (int c = 0; c < allJoints.Length; c++)
            {
                positions[c] = allJoints[c].pose.position.PosToUnity();
                orientations[c] = allJoints[c].pose.orientation.OrientationToUnity();
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