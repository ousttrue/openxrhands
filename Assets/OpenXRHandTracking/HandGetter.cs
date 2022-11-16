using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.OpenXR;


namespace openxr
{
    public class HandGetter : MonoBehaviour
    {
        [SerializeField]
        public Material HandMaterial;

        FrameTimeFeature frameTime_ = null;

        public GameObject leftHand_;
        public GameObject rightHand_;

        HandTrackingFeature handTracking_ = null;
        HandTrackingMeshFeature handTrackingMesh_ = null;
        BodyTrackingFeature bodyTracking_ = null;

        List<Transform> body_ = new List<Transform>();

        void Start()
        {
            handTracking_ = OpenXRSettings.Instance.GetFeature<HandTrackingFeature>();
            if (handTracking_ == null || handTracking_.enabled == false)
            {
                Debug.LogError("You need to enable the openXR hand tracking support extension");
                this.enabled = false;
                return;
            }
            handTracking_.SessionBegin += HandBegin;
            handTracking_.SessionEnd += HandEnd;

            handTrackingMesh_ = OpenXRSettings.Instance.GetFeature<HandTrackingMeshFeature>();
            if (handTrackingMesh_ == null || handTrackingMesh_.enabled == false)
            {
                Debug.LogError("You need to enable the openXR hand tracking mesh support extension");
                this.enabled = false;
                return;
            }

            frameTime_ = OpenXRSettings.Instance.GetFeature<FrameTimeFeature>();
            if (frameTime_ == null || frameTime_ == false)
            {
                Debug.LogError("FrameTimeFeature required");
                this.enabled = false;
                return;
            }

            bodyTracking_ = OpenXRSettings.Instance.GetFeature<BodyTrackingFeature>();
            if (bodyTracking_ == null || bodyTracking_ == false)
            {
                Debug.LogError("FrameTimeFeature required");
                this.enabled = false;
                return;
            }
            bodyTracking_.SessionBegin += BodyBegin;
            bodyTracking_.SessionEnd += BodyEnd;
        }

        void HandBegin()
        {
        }

        void HandEnd()
        {
            if (leftHand_ != null)
            {
                GameObject.Destroy(leftHand_);
                leftHand_ = null;
            }
            if (rightHand_ != null)
            {
                GameObject.Destroy(rightHand_);
                rightHand_ = null;
            }
        }

        void BodyBegin()
        {
            for (int i = 0; i < 70; ++i)
            {
                var t = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                t.localScale = new Vector3(0.02f, 0.02f, 0.02f);
                body_.Add(t);
            }
        }

        void BodyEnd()
        {
            foreach (var x in body_)
            {
                GameObject.Destroy(x.gameObject);
            }
            body_.Clear();
        }

        void Update()
        {
            {
                var handle = handTracking_.GetHandle(HandTrackingFeature.Hand_Index.L);
                if (handle != 0)
                {
                    if (leftHand_ == null)
                    {
                        {
                            leftHand_ = handTrackingMesh_.CreateHandMesh(transform, HandMaterial, handle, "_lh");
                            if (leftHand_ != null)
                            {
                                Debug.Log(leftHand_);
                            }
                        }
                    }
                    else
                    {
                        Transform[] bones = leftHand_.GetComponent<SkinnedMeshRenderer>().bones;
                        float[] radius;
                        Vector3[] positions;
                        Quaternion[] orientations;
                        if (handTracking_.TryGetJoints(frameTime_.FrameTime, handle, out positions, out orientations, out radius))
                        {
                            if (radius.Length == bones.Length && radius[0] > 0)
                            {
                                for (int c = 0; c < bones.Length; c++)
                                {
                                    bones[c].position = positions[c];
                                    bones[c].rotation = orientations[c];
                                }
                            }
                        }
                    }
                }
            }

            {
                var handle = handTracking_.GetHandle(HandTrackingFeature.Hand_Index.R);
                if (handle != 0)
                {
                    if (rightHand_ == null)
                    {
                        rightHand_ = handTrackingMesh_.CreateHandMesh(transform, HandMaterial, handle, "_rh");
                        if (rightHand_ != null)
                        {
                            Debug.Log(rightHand_);
                        }
                    }
                    else
                    {
                        float[] radius;
                        Vector3[] positions;
                        Quaternion[] orientations;
                        if (handTracking_.TryGetJoints(frameTime_.FrameTime, handle, out positions, out orientations, out radius))
                        {
                            var bones = rightHand_.GetComponent<SkinnedMeshRenderer>().bones;
                            if (radius.Length == bones.Length && radius[0] > 0)
                            {
                                for (int c = 0; c < bones.Length; c++)
                                {
                                    bones[c].position = positions[c];
                                    bones[c].rotation = orientations[c];
                                }
                            }
                        }
                    }
                }
            }

            {
                Vector3[] positions;
                Quaternion[] orientations;
                if (bodyTracking_.TryGetJoints(frameTime_.FrameTime, out positions, out orientations))
                {
                    for (int c = 0; c < body_.Count; c++)
                    {
                        body_[c].position = positions[c];
                        body_[c].rotation = orientations[c];
                    }
                }
            }
        }
    }
}