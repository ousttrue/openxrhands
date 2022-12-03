using System;
using UnityEngine;
using UnityEngine.XR.OpenXR;


namespace openxr
{
    public class BodyGetter : MonoBehaviour
    {
        FrameTimeFeature frameTime_;
        BodyTrackingFeature bodyTracking_;

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
        }

        void BodyEnd()
        {
            Debug.Log("BodyEnd");
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
        }
    }
}