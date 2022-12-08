using System;
using UnityEngine;

namespace openxr
{
    public class BodyObject : IDisposable
    {
        GameObject root_;
        Transform[] transforms_;

        public BodyObject()
        {
            root_ = new GameObject("body");

            transforms_ = new Transform[(int)BodyTrackingFeature.XrBodyJointFB.XR_BODY_JOINT_COUNT_FB]; // 70

            for (int i = 0; i < transforms_.Length; ++i)
            {
                // var go = new GameObject();
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"{(BodyTrackingFeature.XrBodyJointFB)i}";
                go.transform.SetParent(root_.transform);
                go.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
                transforms_[i] = go.transform;
            }
        }

        public void Dispose()
        {
            foreach (var t in transforms_)
            {
                GameObject.Destroy(t.gameObject);
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
            }
        }

        public void Update(BodyTrackingFeature.XrBodyJointLocationFB[] joints)
        {
            for (int i = 0; i < joints.Length; ++i)
            {
                var src = joints[i];
                var dst = transforms_[i];
                dst.localPosition = src.pose.position.ToUnity();
                dst.localRotation = src.pose.orientation.ToUnity();
            }
        }
    }
}
