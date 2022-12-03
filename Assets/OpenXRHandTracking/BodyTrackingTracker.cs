using System;
using UnityEngine;

namespace openxr
{
    public class BodyTrackingTracker : IDisposable
    {
        BodyTrackingFeature feature_;
        ulong handle_;
        BodyTrackingFeature.XrBodyJointLocationFB[] joints_;
        ArrayPin pin_;
        public BodyTrackingTracker(BodyTrackingFeature feature, ulong handle)
        {
            feature_ = feature;
            handle_ = handle;
            joints_ = new BodyTrackingFeature.XrBodyJointLocationFB[BodyTrackingFeature.XR_BODY_JOINT_COUNT_FB];
            pin_ = new ArrayPin(joints_);
        }

        public static BodyTrackingTracker CreateTracker(BodyTrackingFeature feature, ulong session)
        {
            var create = new BodyTrackingFeature.XrBodyTrackerCreateInfoFB
            {
                type = XrStructureType.XR_TYPE_BODY_TRACKER_CREATE_INFO_FB
            };

            ulong handle;
            var retVal = feature.XrCreateBodyTrackerFB(session, create, out handle);
            if (retVal != 0)
            {
                Debug.Log($"Couldn't open body hand tracker: {retVal}");
                return null;
            }

            return new BodyTrackingTracker(feature, handle);
        }

        public void Dispose()
        {
            pin_.Dispose();
            pin_ = null;
            if (handle_ != 0)
            {
                feature_.XrDestroyBodyTrackerFB(handle_);
                handle_ = 0;
            }
        }

        public bool TryGetJoints(long frame_time, ulong space, out BodyTrackingFeature.XrBodyJointLocationFB[] values)
        {
            if (handle_ == 0)
            {
                Debug.LogWarning("zero");
                values = default;
                return false;
            }

            var jli = new BodyTrackingFeature.XrBodyJointsLocateInfoFB
            {
                type = XrStructureType.XR_TYPE_BODY_JOINTS_LOCATE_INFO_FB,
                baseSpace = space,
                time = frame_time,
            };
            var joints = new BodyTrackingFeature.XrBodyJointLocationsFB
            {
                type = XrStructureType.XR_TYPE_BODY_JOINT_LOCATIONS_FB,
                jointCount = (uint)joints_.Length,
                jointLocations = pin_.Ptr,
                time = frame_time,
            };
            var retVal = feature_.XrLocateBodyJointsFB(handle_, jli, ref joints);
            if (retVal != 0)
            {
                Debug.LogError($"XrLocateBodyJointsFB: {retVal}");
                values = default;
                return false;
            }

            values = joints_;
            return true;
        }
    }
}
