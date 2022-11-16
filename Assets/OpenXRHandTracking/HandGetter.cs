using UnityEngine;
using UnityEngine.XR.OpenXR;


namespace openxr
{
    public class HandGetter : MonoBehaviour
    {
        [SerializeField]
        public Material HandMaterial;

        public GameObject leftHand_;
        public GameObject rightHand_;

        FrameTimeFeature frameTime_ = null;
        HandTrackingFeature handTracking_ = null;
        HandTrackingMeshFeature handTrackingMesh_ = null;

        void Start()
        {
            handTracking_ = OpenXRSettings.Instance.GetFeature<HandTrackingFeature>();
            if (handTracking_ == null || handTracking_.enabled == false)
            {
                Debug.LogError("You need to enable the openXR hand tracking support extension");
                this.enabled = false;
                return;
            }

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

            handTracking_.SessionBegin += SessionBegin;
            handTracking_.SessionEnd += SessionEnd;
            this.enabled = false;
        }

        void SessionBegin()
        {
            this.enabled = true;
        }

        void SessionEnd()
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
            this.enabled = false;
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
                        if (handTracking_.TryGetHandJoints(frameTime_.FrameTime, handle, out positions, out orientations, out radius))
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
                        // handTrackingMesh_.ApplyHandJointsToMesh(frameTime_.FrameTime, handle, rightHand_);
                        Transform[] bones = rightHand_.GetComponent<SkinnedMeshRenderer>().bones;
                        float[] radius;
                        Vector3[] positions;
                        Quaternion[] orientations;
                        if (handTracking_.TryGetHandJoints(frameTime_.FrameTime, handle, out positions, out orientations, out radius))
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
        }
    }
}