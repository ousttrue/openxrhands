using System;
using System.Linq;
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

        uint skeletonChangeCount_;
        public void Update(BodyTrackingFeature.XrBodyJointLocationFB[] joints)
        {
            if (transforms_ == null)
            {
                transforms_ = joints.Select((x, i) =>
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = $"{(BodyTrackingFeature.XrBodyJointFB)i}";
                    go.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
                    return go.transform;
                }).ToArray();
                foreach (var t in transforms_)
                {
                    t.SetParent(root_.transform);
                }
            }

            for (int i = 0; i < joints.Length; ++i)
            {
                var src = joints[i];
                var dst = transforms_[i];
                // dst.localPosition = src.pose.position.ToUnity();
                dst.localRotation = src.pose.orientation.ToUnity();
            }
        }

        public void UpdateTPose(BodyTrackingFeature.XrBodySkeletonJointFB[] joints)
        {

        }
    }
}
