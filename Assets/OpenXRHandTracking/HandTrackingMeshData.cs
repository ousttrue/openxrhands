using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace openxr
{
    internal class PinnedArray<T> : IDisposable where T : struct
    {
        GCHandle gcPins;
        public T[] Values;
        public IntPtr Ptr => gcPins.AddrOfPinnedObject();

        public PinnedArray(int length)
        {
            Values = new T[length];
            gcPins = GCHandle.Alloc(Values, GCHandleType.Pinned);
        }

        public void Dispose()
        {
            gcPins.Free();
        }
    }

    internal class HandTrackingMeshData : IDisposable
    {
        // hold onto proper versions of the hand mesh arrays 
        // in something outside the main structure
        public PinnedArray<XrPosef> jointBindPoses;
        public PinnedArray<float> jointRadii;
        public PinnedArray<int> jointParents;
        public PinnedArray<XrVector3f> vertexPositions;
        public PinnedArray<XrVector3f> vertexNormals;
        public PinnedArray<XrVector2f> vertexUVs;
        public PinnedArray<HandTrackingMeshFeature.XrVector4sFB> vertexBlendIndices;
        public PinnedArray<XrVector4f> vertexBlendWeights;
        public PinnedArray<short> indices;

        public HandTrackingMeshData(int joints, int vertices, int indexCount)
        {
            jointBindPoses = new PinnedArray<XrPosef>(joints);
            jointRadii = new PinnedArray<float>(joints);
            jointParents = new PinnedArray<int>(joints);
            vertexPositions = new PinnedArray<XrVector3f>(vertices);
            vertexNormals = new PinnedArray<XrVector3f>(vertices);
            vertexUVs = new PinnedArray<XrVector2f>(vertices);
            vertexBlendIndices = new PinnedArray<HandTrackingMeshFeature.XrVector4sFB>(vertices);
            vertexBlendWeights = new PinnedArray<XrVector4f>(vertices);
            indices = new PinnedArray<short>(indexCount);
        }

        public void Dispose()
        {
            jointBindPoses.Dispose();
            jointRadii.Dispose();
            jointParents.Dispose();
            vertexPositions.Dispose();
            vertexNormals.Dispose();
            vertexUVs.Dispose();
            vertexBlendIndices.Dispose();
            vertexBlendWeights.Dispose();
            indices.Dispose();
        }

        int VertexCount => vertexPositions.Values.Length;
        int IndexCount => indices.Values.Length;
        int JointCount => jointBindPoses.Values.Length;

        Mesh CreateMesh(Matrix4x4[] bindposes)
        {
            var mesh = new Mesh();

            // vertices
            var vertices = new Vector3[VertexCount];
            var normals = new Vector3[VertexCount];
            var uvs = new Vector2[VertexCount];
            var weights = new BoneWeight[VertexCount];
            for (int c = 0; c < VertexCount; c++)
            {
                XrVector3f pos = vertexPositions.Values[c];
                XrVector2f uv = vertexUVs.Values[c];
                XrVector3f normal = vertexNormals.Values[c];
                vertices[c] = pos.ToUnity();
                uvs[c] = new Vector2(uv.x, uv.y);
                normals[c] = normal.ToUnity();
                weights[c].boneIndex0 = vertexBlendIndices.Values[c].x;
                weights[c].boneIndex1 = vertexBlendIndices.Values[c].y;
                weights[c].boneIndex2 = vertexBlendIndices.Values[c].z;
                weights[c].boneIndex3 = vertexBlendIndices.Values[c].w;
                weights[c].weight0 = vertexBlendWeights.Values[c].x;
                weights[c].weight1 = vertexBlendWeights.Values[c].y;
                weights[c].weight2 = vertexBlendWeights.Values[c].z;
                weights[c].weight3 = vertexBlendWeights.Values[c].w;
            }
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.boneWeights = weights;

            // indices
            var triangles = new int[IndexCount];
            for (int c = 0; c < IndexCount; c += 3)
            {
                triangles[c] = indices.Values[c + 2];
                triangles[c + 1] = indices.Values[c + 1];
                triangles[c + 2] = indices.Values[c];
            }
            mesh.triangles = triangles;

            mesh.bindposes = bindposes;

            // handShape.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        public SkinnedMeshRenderer CreateSkinndMesh(Transform[] bones, Material mat)
        {
            var bindPoses = new Matrix4x4[JointCount];

            for (int c = 0; c < JointCount; c++)
            {
                XrPosef joint = jointBindPoses.Values[c];
                XrPosef pose = jointBindPoses.Values[c];
                bones[c].position = pose.position.ToUnity();
                bones[c].rotation = pose.orientation.ToUnity();
                bones[c].localScale = new Vector3(jointRadii.Values[c], jointRadii.Values[c], jointRadii.Values[c]);

                // if (jointParents.Values[c] < JointCount)
                // {
                //     bones[c].parent = bones[jointParents.Values[c]];
                // }

                bindPoses[c] = bones[c].worldToLocalMatrix;
            }

            var bone0 = bones[0].gameObject;
            var renderer = bone0.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = CreateMesh(bindPoses);
            renderer.bones = bones;
            renderer.material = mat;
            renderer.updateWhenOffscreen = true;
            return renderer;
        }

        public static SkinnedMeshRenderer CreateHandMesh(HandTrackingMeshFeature feature, HandTracker tracker, Transform[] bones, Material mat)
        {
            var xrMesh = new HandTrackingMeshFeature.XrHandTrackingMeshFB
            {
                stype = XrStructureType.XR_TYPE_HAND_TRACKING_MESH_FB,
            };

            // find size of mesh
            // Type_xrGetHandMeshFB mesh_get_fn = GetInstanceProc<Type_xrGetHandMeshFB>("xrGetHandMeshFB");
            var retVal = feature.XrGetHandMeshFB(tracker.handle_, ref xrMesh);
            if (retVal != XrResult.XR_SUCCESS)
            {
                Debug.LogError($"XrGetHandMeshFB1: {retVal}");
                return null;
            }

            return CreateHandMesh(feature, tracker, bones, mat,
                (int)xrMesh.jointCountOutput, (int)xrMesh.vertexCountOutput, (int)xrMesh.indexCountOutput);
        }

        static SkinnedMeshRenderer CreateHandMesh(HandTrackingMeshFeature feature, HandTracker tracker, Transform[] bones, Material mat,
            int jointCount, int vertexCount, int indexCount)
        {
            // get actual mesh
            // alloc data
            using (var data = new HandTrackingMeshData(jointCount, vertexCount, indexCount))
            {
                // alloc
                var xrMesh = new HandTrackingMeshFeature.XrHandTrackingMeshFB
                {
                    stype = XrStructureType.XR_TYPE_HAND_TRACKING_MESH_FB,
                    jointBindPoses = data.jointBindPoses.Ptr,
                    jointRadii = data.jointRadii.Ptr,
                    jointParents = data.jointParents.Ptr,
                    jointCapacityInput = (uint)jointCount,
                    // jointCountOutput = (uint)jointCount,
                    vertexPositions = data.vertexPositions.Ptr,
                    vertexNormals = data.vertexNormals.Ptr,
                    vertexUVs = data.vertexUVs.Ptr,
                    vertexBlendIndices = data.vertexBlendIndices.Ptr,
                    vertexBlendWeights = data.vertexBlendWeights.Ptr,
                    vertexCapacityInput = (uint)vertexCount,
                    // vertexCountOutput = (uint)vertexCount,
                    indices = data.indices.Ptr,
                    indexCapacityInput = (uint)indexCount,
                    // indexCountOutput = (uint)indexCount,
                };

                var retVal = feature.XrGetHandMeshFB(tracker.handle_, ref xrMesh);
                if (retVal != XrResult.XR_SUCCESS)
                {
                    Debug.LogError($"XrGetHandMeshFB2: {retVal}");
                    return null;
                }

                // unity Mesh
                return data.CreateSkinndMesh(bones, mat);
            }
        }
    }
}
