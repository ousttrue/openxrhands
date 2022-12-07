using System;
using System.Linq;
using UnityEngine;

namespace openxr
{
    public class BodyObject : IDisposable
    {
        GameObject root_;
        Transform[] transforms_;
        Quaternion[] initialRotations_;

        public BodyObject()
        {
            root_ = new GameObject("body");

            transforms_ = new Transform[(int)BodyTrackingFeature.XrBodyJointFB.XR_BODY_JOINT_COUNT_FB]; // 70
            initialRotations_ = new Quaternion[(int)BodyTrackingFeature.XrBodyJointFB.XR_BODY_JOINT_COUNT_FB];

            for (int i = 0; i < transforms_.Length; ++i)
            {
                var go = new GameObject();
                go.name = $"{(BodyTrackingFeature.XrBodyJointFB)i}";
                go.transform.SetParent(root_.transform);
                transforms_[i] = go.transform;

                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
                cube.transform.SetParent(go.transform);
            }

        }

        public void Dispose()
        {
            if (transforms_ != null)
            {
                foreach (var t in transforms_)
                {
                    GameObject.Destroy(t.gameObject);
                }
            }
            GameObject.Destroy(root_);
        }

        public void UpdateTPose(BodyTrackingFeature.XrBodySkeletonJointFB[] joints)
        {
            for (int i = 0; i < joints.Length; ++i)
            {
                var src = joints[i];
                var dst = transforms_[i];
                dst.position = src.pose.position.ToUnity();
                dst.rotation = src.pose.orientation.ToUnity();
                // if (src.parentJoint >= 0 && src.parentJoint < transforms_.Length)
                // {
                //     dst.SetParent(transforms_[src.parentJoint], true);
                // }
                initialRotations_[i] = dst.rotation;
            }
        }

        public void Update(BodyTrackingFeature.XrBodyJointLocationFB[] joints)
        {
            for (int i = 0; i < joints.Length; ++i)
            {

                var src = joints[i];
                var dst = transforms_[i];
                if (src.locationFlags.HasFlag(XrSpaceLocationFlags.XR_SPACE_LOCATION_POSITION_VALID_BIT))
                {
                    dst.position = src.pose.position.ToUnity();
                }
                if (src.locationFlags.HasFlag(XrSpaceLocationFlags.XR_SPACE_LOCATION_ORIENTATION_VALID_BIT))
                {
                    dst.rotation = src.pose.orientation.ToUnity();
                }
            }
        }
    }
}
