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

        HandTrackingFeature handTracking_ = null;

        void Start()
        {
            handTracking_ = OpenXRSettings.Instance.GetFeature<HandTrackingFeature>();
            if (handTracking_ == null || handTracking_.enabled == false)
            {
                Debug.LogError("You need to enable the openXR hand tracking support extension ");
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
            if (leftHand_ == null)
            {
                leftHand_ = handTracking_.CreateHandMesh(HandTrackingFeature.Hand_Index.L, transform, HandMaterial);
                if (leftHand_ != null)
                {
                    Debug.Log(leftHand_);
                }
            }
            else
            {
                handTracking_.ApplyHandJointsToMesh(HandTrackingFeature.Hand_Index.L, leftHand_);
            }
            if (rightHand_ == null)
            {
                rightHand_ = handTracking_.CreateHandMesh(HandTrackingFeature.Hand_Index.R, transform, HandMaterial);
                if (rightHand_ != null)
                {
                    Debug.Log(rightHand_);
                }
            }
            else
            {
                handTracking_.ApplyHandJointsToMesh(HandTrackingFeature.Hand_Index.R, rightHand_);
            }
        }
    }
}