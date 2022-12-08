using System;
using UnityEngine;
using UnityEngine.XR.OpenXR;


namespace openxr
{
    public class BodyGetter : MonoBehaviour
    {
        FrameTimeFeature frameTime_;
        BodyTrackingFeature bodyTracking_;
        HandTrackingFeature handTracking_;

        ulong space_;
        BodyTracker bodyTracker_;
        BodyObject body_;

        HandTracker leftHandTracker_;
        HandTracker rightHandTracker_;

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
                Debug.LogError("You need to enable the openXR body tracking support extension");
                return;
            }
            if (!TryGetFeature(out handTracking_))
            {
                this.enabled = false;
                Debug.LogError("You need to enable the openXR hand tracking support extension");
                return;
            }

            bodyTracking_.SessionBegin += BodyBegin;
            bodyTracking_.SessionEnd += BodyEnd;
            handTracking_.SessionBegin += HandBegin;
            handTracking_.SessionEnd += HandEnd;
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

            bodyTracker_ = BodyTracker.CreateTracker(feature, session);
            body_ = new BodyObject();

            bodyTracker_.SkeletonUpdated += (skeletonJoints) =>
            {
                body_.UpdateTPose(skeletonJoints);
            };
        }

        void BodyEnd()
        {
            body_.Dispose();
            body_ = null;
            bodyTracker_.Dispose();
            bodyTracker_ = null;
        }

        void HandBegin(HandTrackingFeature feature, ulong session)
        {
            leftHandTracker_ = HandTracker.CreateTracker(feature, session, HandTrackingFeature.XrHandEXT.XR_HAND_LEFT_EXT);
            rightHandTracker_ = HandTracker.CreateTracker(feature, session, HandTrackingFeature.XrHandEXT.XR_HAND_RIGHT_EXT);
        }

        void HandEnd()
        {
            leftHandTracker_.Dispose();
            leftHandTracker_ = null;
            rightHandTracker_.Dispose();
            rightHandTracker_ = null;
        }

        void Compare(XrSpaceLocationFlags lFlag, XrPosef lhs, XrPosef rhs, XrSpaceLocationFlags rFlag)
        {
            if (!lhs.Equals(rhs))
            {
                Debug.Log($"[body]{lFlag} {lhs}!={rhs} {rFlag}[hand]");
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
            if (bodyTracking_.enabled)
            {
                if (bodyTracker_ != null)
                {
                    if (bodyTracker_.TryGetJoints(time, space, out var joints))
                    {
                        body_.Update(joints);

                        // compare body with hand
                        if (handTracking_.enabled)
                        {
                            if (leftHandTracker_ != null)
                            {
                                if (leftHandTracker_.TryGetJoints(time, space, out var leftHandJoints))
                                {
                                    Compare(
                                        joints[(int)BodyTrackingFeature.XrBodyJointFB.XR_BODY_JOINT_LEFT_HAND_PALM_FB].locationFlags,
                                        joints[(int)BodyTrackingFeature.XrBodyJointFB.XR_BODY_JOINT_LEFT_HAND_PALM_FB].pose,
                                        leftHandJoints[(int)HandTrackingFeature.XrHandJointEXT.XR_HAND_JOINT_PALM_EXT].pose,
                                        leftHandJoints[(int)HandTrackingFeature.XrHandJointEXT.XR_HAND_JOINT_PALM_EXT].locationFlags
                                    );
                                }
                            }
                        }
                    }
                }

            }
        }
    }
}