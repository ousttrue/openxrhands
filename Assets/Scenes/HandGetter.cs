using System;
using UnityEngine;
using UnityEngine.XR.OpenXR;


namespace openxr
{
    public class HandGetter : MonoBehaviour
    {
        FrameTimeFeature frameTime_;
        HandTrackingFeature handTracking_;
        HandTrackingMeshFeature handTrackingMesh_;

        HandTracker leftTracker_;
        HandTracker rightTracker_;

        [SerializeField]
        public Material HandMaterial;
        HandObject leftHand_;
        HandObject rightHand_;

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
            if (!TryGetFeature(out handTracking_))
            {
                this.enabled = false;
                Debug.LogError("You need to enable the openXR hand tracking support extension");
                return;
            }
            if (!TryGetFeature(out handTrackingMesh_))
            {
                Debug.LogError("You need to enable the openXR hand tracking mesh support extension");
                this.enabled = false;
                return;
            }

            handTracking_.SessionBegin += HandBegin;
            handTracking_.SessionEnd += HandEnd;
        }

        void HandBegin(HandTrackingFeature feature, ulong session)
        {
            leftTracker_ = HandTracker.CreateTracker(feature, session, HandTrackingFeature.XrHandEXT.XR_HAND_LEFT_EXT);
            if (leftTracker_ == null)
            {
                throw new ArgumentNullException();
            }
            leftHand_ = new HandObject("left");

            rightTracker_ = HandTracker.CreateTracker(feature, session, HandTrackingFeature.XrHandEXT.XR_HAND_RIGHT_EXT);
            if (rightTracker_ == null)
            {
                throw new ArgumentNullException();
            }
            rightHand_ = new HandObject("right");
        }

        void HandEnd()
        {
            Debug.Log("HandEnd");
            if (leftHand_ != null)
            {
                leftHand_.Dispose();
                leftHand_ = null;
            }
            if (leftTracker_ != null)
            {
                leftTracker_.Dispose();
                leftTracker_ = null;
            }

            if (rightHand_ != null)
            {
                rightHand_.Dispose();
                rightHand_ = null;
            }
            if (rightTracker_ != null)
            {
                rightTracker_.Dispose();
                rightTracker_ = null;
            }
        }

        void Update()
        {
            if (!frameTime_.enabled)
            {
                return;
            }
            var time = frameTime_.FrameTime;
            var space = frameTime_.CurrentAppSpace;
            if (!handTracking_.enabled)
            {
                return;
            }

            if (leftTracker_ != null && leftHand_ != null)
            {
                if (leftTracker_.TryGetJoints(time, space, out var joints))
                {
                    leftHand_.SetMesh(handTrackingMesh_, leftTracker_, HandMaterial);
                    leftHand_.Update(joints);
                }
            }
            if (rightTracker_ != null && rightHand_ != null)
            {
                if (rightTracker_.TryGetJoints(time, space, out var joints))
                {
                    rightHand_.SetMesh(handTrackingMesh_, rightTracker_, HandMaterial);
                    rightHand_.Update(joints);
                }
            }
        }
    }
}