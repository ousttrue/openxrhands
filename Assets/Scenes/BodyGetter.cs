using System;
using UnityEngine;
using UnityEngine.XR.OpenXR;


namespace openxr
{
    public class BodyGetter : MonoBehaviour
    {
        FrameTimeFeature frameTime_;
        BodyTrackingFeature bodyTracking_;

        ulong space_;
        BodyTrackingTracker tracker_;

        static bool TryGetFeature<T>(out T feature) where T : UnityEngine.XR.OpenXR.Features.OpenXRFeature
        {
            feature = OpenXRSettings.Instance.GetFeature<T>();
            if (feature == null || feature.enabled == false)
            {
                return false;
            }
            return true;
        }

        void Start()
        {
            if (!TryGetFeature(out frameTime_))
            {
                Debug.LogError("FrameTimeFeature required");
                this.enabled = false;
                return;
            }
            if (!TryGetFeature(out bodyTracking_))
            {
                this.enabled = false;
                Debug.LogError("You need to enable the openXR hand tracking support extension");
                return;
            }

            bodyTracking_.SessionBegin += BodyBegin;
            bodyTracking_.SessionEnd += BodyEnd;
        }

        void BodyBegin(BodyTrackingFeature feature, ulong session)
        {
            var info = new XrReferenceSpaceCreateInfo
            {
                type = XrStructureType.XR_TYPE_REFERENCE_SPACE_CREATE_INFO,
                referenceSpaceType = XrReferenceSpaceType.XR_REFERENCE_SPACE_TYPE_STAGE,
                poseInReferenceSpace = new XrPosef
                {
                    position = new XrVector3f
                    {
                        x = 0,
                        y = 0,
                        z = 0,
                    },
                    orientation = new XrVector4f
                    {
                        x = 0,
                        y = 0,
                        z = 0,
                        w = 1,
                    },
                },
            };
            var retValue = feature.XrCreateReferenceSpace(session, info, ref space_);
            Debug.Log($"XrCreateReferenceSpace: {retValue}");

            tracker_ = BodyTrackingTracker.CreateTracker(feature, session);
        }

        void BodyEnd()
        {
            Debug.Log("BodyEnd");
            tracker_.Dispose();
            tracker_ = null;
        }

        void Update()
        {
            if (!frameTime_.enabled)
            {
                return;
            }
            var time = frameTime_.FrameTime;
            var space = frameTime_.CurrentAppSpace;
            if (!bodyTracking_.enabled)
            {
                return;
            }
            if (tracker_ != null)
            {
                if (tracker_.TryGetJoints(time, space, out var joints))
                {

                }
            }
        }
    }
}