using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.OpenXR;


namespace openxr
{
    public class HandGetter : MonoBehaviour
    {
        public GameObject leftHand_;
        public GameObject rightHand_;

        FrameTimeFeature frameTime_;
        HandTrackingFeature handTracking_;
        HandTrackingMeshFeature handTrackingMesh_;

        HandTrackingTracker left_;
        HandTrackingTracker right_;

        [SerializeField]
        public Material HandMaterial;
        public GameObject leftMesh_;
        public GameObject rightMesh_;

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

        void HandBegin(HandTrackingTracker left, HandTrackingTracker right)
        {
            left_ = left;
            if (left_ == null)
            {
                throw new ArgumentNullException();
            }
            leftMesh_ = handTrackingMesh_.CreateHandMesh(left_, HandMaterial);

            right_ = right;
            if (right_ == null)
            {
                throw new ArgumentNullException();
            }
            rightMesh_ = handTrackingMesh_.CreateHandMesh(right_, HandMaterial);
        }

        void HandEnd()
        {
            Debug.Log("HandEnd");
            GameObject.Destroy(leftMesh_);
            left_ = null;
            if (leftHand_ != null)
            {
                GameObject.Destroy(leftHand_);
                leftHand_ = null;
            }

            GameObject.Destroy(rightMesh_);
            right_ = null;
            if (rightHand_ != null)
            {
                GameObject.Destroy(rightHand_);
                rightHand_ = null;
            }
        }

        void Update()
        {
            var time = frameTime_.FrameTime;

            //         {
            //             var handle = handTracking_.GetHandle(HandTrackingFeature.Hand_Index.L);
            //             if (handle != 0)
            //             {
            //                 if (leftHand_ == null)
            //                 {
            //                     {
            //                         leftHand_ = handTrackingMesh_.CreateHandMesh(transform, HandMaterial, handle, "_lh");
            //                         if (leftHand_ != null)
            //                         {
            //                             Debug.Log(leftHand_);
            //                         }
            //                     }
            //                 }
            //                 else
            //                 {
            //                     Transform[] bones = leftHand_.GetComponent<SkinnedMeshRenderer>().bones;
            //                     float[] radius;
            //                     Vector3[] positions;
            //                     Quaternion[] orientations;
            //                     if (handTracking_.TryGetJoints(frameTime_.FrameTime, handle, out positions, out orientations, out radius))
            //                     {
            //                         if (radius.Length == bones.Length && radius[0] > 0)
            //                         {
            //                             for (int c = 0; c < bones.Length; c++)
            //                             {
            //                                 bones[c].position = positions[c];
            //                                 bones[c].rotation = orientations[c];
            //                             }
            //                         }
            //                     }
            //                 }
            //             }
            //         }

            //         {
            //             var handle = handTracking_.GetHandle(HandTrackingFeature.Hand_Index.R);
            //             if (handle != 0)
            //             {
            //                 if (rightHand_ == null)
            //                 {
            //                     rightHand_ = handTrackingMesh_.CreateHandMesh(transform, HandMaterial, handle, "_rh");
            //                     if (rightHand_ != null)
            //                     {
            //                         Debug.Log(rightHand_);
            //                     }
            //                 }
            //                 else
            //                 {
            //                     float[] radius;
            //                     Vector3[] positions;
            //                     Quaternion[] orientations;
            //                     if (handTracking_.TryGetJoints(frameTime_.FrameTime, handle, out positions, out orientations, out radius))
            //                     {
            //                         var bones = rightHand_.GetComponent<SkinnedMeshRenderer>().bones;
            //                         if (radius.Length == bones.Length && radius[0] > 0)
            //                         {
            //                             for (int c = 0; c < bones.Length; c++)
            //                             {
            //                                 bones[c].position = positions[c];
            //                                 bones[c].rotation = orientations[c];
            //                             }
            //                         }
            //                     }
            //                 }
            //             }
            //         }
        }
    }
}