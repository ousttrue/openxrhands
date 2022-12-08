using System;
using UnityEngine;

namespace openxr
{
    public class HandObject : IDisposable
    {
        GameObject root_;
        Transform[] transforms_;
        Mesh mesh_;

        public HandObject(string name)
        {
            root_ = new GameObject(name);

            transforms_ = new Transform[HandTrackingFeature.XR_HAND_JOINT_COUNT_EXT];
            for (int i = 0; i < transforms_.Length; ++i)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"{(HandTrackingFeature.XrHandJointEXT)i}";
                go.transform.SetParent(root_.transform);
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
            GameObject.Destroy(mesh_);
        }

        public void Update(HandTrackingFeature.XrHandJointLocationEXT[] joints)
        {
            for (int i = 0; i < joints.Length; ++i)
            {
                var src = joints[i];
                var dst = transforms_[i];
                dst.localPosition = src.pose.position.ToUnity();
                dst.localRotation = src.pose.orientation.ToUnity();
                dst.localScale = new Vector3(src.radius, src.radius, src.radius);
            }
        }

        public void SetMesh(HandTrackingMeshFeature feature, HandTracker tracker, Material mat)
        {
            if (mesh_ != null)
            {
                return;
            }
            if (feature == null)
            {
                return;
            }
            if (transforms_ == null)
            {
                return;
            }
            var renderer = HandTrackingMeshData.CreateHandMesh(feature, tracker, transforms_, mat);
            if (renderer == null)
            {
                return;
            }

            mesh_ = renderer.sharedMesh;
        }
    }
}
