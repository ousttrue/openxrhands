using System;
using System.Runtime.InteropServices;

namespace openxr
{
    internal class PinnedArray<T> : IDisposable where T : struct
    {
        GCHandle gcPins;
        public T[] Values;
        public IntPtr Ptr => gcPins.AddrOfPinnedObject();

        public PinnedArray(int length)
        {
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

        public UnityEngine.GameObject CreateSkinnedMesh()
        {
            throw new NotImplementedException();
            // construct mesh and bones and skin it correctly
            // var handObj = new GameObject();
            // handObj.transform.parent = parent;
            // SkinnedMeshRenderer rend = handObj.AddComponent<SkinnedMeshRenderer>();

            // Vector3[] vertices = new Vector3[mesh.vertexCountOutput];
            // Vector3[] normals = new Vector3[mesh.vertexCountOutput];
            // Vector2[] uvs = new Vector2[mesh.vertexCountOutput];
            // BoneWeight[] weights = new BoneWeight[mesh.vertexCountOutput];
            // int[] triangles = new int[mesh.indexCountOutput];
            // for (int c = 0; c < mesh.vertexCountOutput; c++)
            // {
            //     XrVector3f pos = meshArrays.vertexPositions[c];
            //     XrVector2f uv = meshArrays.vertexUVs[c];
            //     XrVector3f normal = meshArrays.vertexNormals[c];
            //     vertices[c] = pos.ToUnity();
            //     uvs[c] = new Vector2(uv.x, uv.y);
            //     normals[c] = normal.ToUnity();
            //     weights[c].boneIndex0 = meshArrays.vertexBlendIndices[c].x;
            //     weights[c].boneIndex1 = meshArrays.vertexBlendIndices[c].y;
            //     weights[c].boneIndex2 = meshArrays.vertexBlendIndices[c].z;
            //     weights[c].boneIndex3 = meshArrays.vertexBlendIndices[c].w;
            //     weights[c].weight0 = meshArrays.vertexBlendWeights[c].x;
            //     weights[c].weight1 = meshArrays.vertexBlendWeights[c].y;
            //     weights[c].weight2 = meshArrays.vertexBlendWeights[c].z;
            //     weights[c].weight3 = meshArrays.vertexBlendWeights[c].w;
            // }
            // for (int c = 0; c < mesh.indexCountOutput; c += 3)
            // {
            //     triangles[c] = meshArrays.indices[c + 2];
            //     triangles[c + 1] = meshArrays.indices[c + 1];
            //     triangles[c + 2] = meshArrays.indices[c];
            // }
            // handShape.vertices = vertices;
            // handShape.uv = uvs;
            // handShape.triangles = triangles;
            // handShape.normals = normals;
            // //            handShape.RecalculateNormals();
            // handShape.RecalculateBounds();
            // handShape.RecalculateTangents();
            // Transform[] boneTransforms = new Transform[mesh.jointCountOutput];
            // GameObject[] bones = new GameObject[mesh.jointCountOutput];
            // Matrix4x4[] bindPoses = new Matrix4x4[mesh.jointCountOutput];
            // // first make the bone objects - this is because parenting of bones is not always ordered 
            // for (int c = 0; c < mesh.jointCountOutput; c++)
            // {
            //     bones[c] = new GameObject("Bone_" + c + bone_postfix);
            // }
            // for (int c = 0; c < mesh.jointCountOutput; c++)
            // {
            //     XrPosef joint = meshArrays.jointBindPoses[c];
            //     XrPosef pose = meshArrays.jointBindPoses[c];
            //     bones[c].transform.position = pose.position.ToUnity();
            //     bones[c].transform.rotation = pose.orientation.ToUnity();
            //     bones[c].transform.localScale = new Vector3(meshArrays.jointRadii[c], meshArrays.jointRadii[c], meshArrays.jointRadii[c]);

            //     if (meshArrays.jointParents[c] < mesh.jointCountOutput)
            //     {
            //         bones[c].transform.parent = bones[meshArrays.jointParents[c]].transform;
            //     }
            //     else
            //     {
            //         bones[c].transform.parent = handObj.transform;
            //         //rend.rootBone=bones[c].transform;
            //     }

            //     bindPoses[c] = bones[c].transform.worldToLocalMatrix;
            //     boneTransforms[c] = bones[c].transform;
            // }
            // handShape.bindposes = bindPoses;
            // handShape.boneWeights = weights;
            // rend.sharedMesh = handShape;
            // rend.bones = boneTransforms;
            // rend.material = mat;
            // rend.updateWhenOffscreen = true;

            // return handShape;
        }
    }
}