using System;
using System.Linq;
using UnityEngine;

namespace openxr
{
    public class HandObject : IDisposable
    {
        GameObject root_;
        Transform[] transforms_;

        public HandObject(string name)
        {
            root_ = new GameObject(name);
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

        public void Update(HandTrackingFeature.XrHandJointLocationEXT[] joints)
        {
            if (transforms_ == null)
            {
                transforms_ = joints.Select((x, i) =>
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = $"{(HandTrackingFeature.XrHandJointEXT)i}";
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
                dst.localPosition = src.pose.position.ToUnity();
                dst.localRotation = src.pose.orientation.ToUnity();
                dst.localScale = new Vector3(src.radius, src.radius, src.radius);
            }
        }
    }
}
