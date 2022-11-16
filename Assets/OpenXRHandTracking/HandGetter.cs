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
        EyeTrackingFeature eyeTracking_ = null;

        List<Transform> body_ = new List<Transform>();

        public Transform leftEye_;
        public Transform rightEye_;

        bool TryGetFeature<T>(out T feature) where T : UnityEngine.XR.OpenXR.Features.OpenXRFeature
        {
            feature = OpenXRSettings.Instance.GetFeature<T>();
            if (handTracking_ == null || handTracking_.enabled == false)
            {
                return false;
            }
            return true;
        }

        void Start()
        {
            if (!TryGetFeature(out handTracking_))
            {
                this.enabled = false;
                Debug.LogError("You need to enable the openXR hand tracking support extension");
                return;
            }
            handTracking_.SessionBegin += HandBegin;
            handTracking_.SessionEnd += HandEnd;

            if (!TryGetFeature(out handTrackingMesh_))
            {
                Debug.LogError("You need to enable the openXR hand tracking mesh support extension");
                this.enabled = false;
                return;
            }

            if (!TryGetFeature(out frameTime_))
            {
                Debug.LogError("FrameTimeFeature required");
                this.enabled = false;
                return;
            }

            if (!TryGetFeature(out bodyTracking_))
            {
                Debug.LogError("BodyTracking required");
                this.enabled = false;
                return;
            }
            bodyTracking_.SessionBegin += BodyBegin;
            bodyTracking_.SessionEnd += BodyEnd;

            if (!TryGetFeature(out eyeTracking_))
            {
                Debug.LogError("EyeTracking required");
                this.enabled = false;
                return;
            }
            eyeTracking_.SessionBegin += EyeBegin;
            eyeTracking_.SessionEnd += EyeEnd;
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

        void EyeBegin()
        {
            leftEye_ = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            leftEye_.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
            rightEye_ = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            rightEye_.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
        }

        void EyeEnd()
        {
            GameObject.Destroy(leftEye_.gameObject);
            leftEye_ = null;
            GameObject.Destroy(rightEye_.gameObject);
            rightEye_ = null;
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

            if (leftEye_ != null && rightEye_ != null)
            {
                Vector3 lp;
                Quaternion lr;
                Vector3 rp;
                Quaternion rr;
                if (eyeTracking_.TryGetGaze(out lp, out lr, out rp, out rr))
                {
                    leftEye_.position = lp;
                    leftEye_.rotation = lr;
                    rightEye_.position = rp;
                    rightEye_.rotation = rr;
                }
            }
        }
    }
}