using System.Collections.Generic;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using UnityEngine.XR.OpenXR;


namespace openxr
{
#if UNITY_EDITOR
    [UnityEditor.XR.OpenXR.Features.OpenXRFeature(UiName = "Hand tracking mesh Extension",
        BuildTargetGroups = new[] { 
            UnityEditor.BuildTargetGroup.Standalone, UnityEditor.BuildTargetGroup.WSA, UnityEditor.BuildTargetGroup.Android },
        Company = "Joe M",
        Desc = "Enable hand tracking mesh in unity",
        DocumentationLink = "https://docs.unity3d.com/Packages/com.unity.xr.openxr@0.1/manual/index.html",
        OpenxrExtensionStrings = xr_extension,
        Version = "0.0.1",
        FeatureId = featureId)]
#endif
    public class HandTrackingMeshFeature : OpenXRFeature
    {
        public const string featureId = "com.joemarshall.handtracking_mesh";
        public const string xr_extension = "XR_FB_hand_tracking_mesh";
        PFN_xrGetInstanceProcAddr xrGetInstanceProcAddr_;

        ulong instance_;
        ulong session_;

        // hold onto proper versions of the hand mesh arrays 
        // in something outside the main structure
        internal class HandMeshArrays
        {
            public XrPosef[] jointBindPoses;
            public float[] jointRadii;
            public int[] jointParents;
            public XrVector3f[] vertexPositions;
            public XrVector3f[] vertexNormals;
            public XrVector2f[] vertexUVs;
            public XrVector4sFB[] vertexBlendIndices;
            public XrVector4f[] vertexBlendWeights;
            public short[] indices;


            GCHandle[] gcPins;

            public HandMeshArrays(uint joints, uint vertices, uint indexCount, ref XrHandTrackingMeshFB owner)
            {
                jointBindPoses = new XrPosef[joints];
                jointRadii = new float[joints];
                jointParents = new int[joints];
                vertexPositions = new XrVector3f[vertices];
                vertexNormals = new XrVector3f[vertices];
                vertexUVs = new XrVector2f[vertices];
                vertexBlendIndices = new XrVector4sFB[vertices];
                vertexBlendWeights = new XrVector4f[vertices];
                indices = new short[indexCount];
                GCHandle[] pinnedArrays =
                {
                GCHandle.Alloc(jointBindPoses, GCHandleType.Pinned),
                GCHandle.Alloc(jointRadii, GCHandleType.Pinned),
                GCHandle.Alloc(jointParents, GCHandleType.Pinned),
                GCHandle.Alloc(vertexPositions, GCHandleType.Pinned),
                GCHandle.Alloc(vertexNormals, GCHandleType.Pinned),
                GCHandle.Alloc(vertexUVs, GCHandleType.Pinned),
                GCHandle.Alloc(vertexBlendIndices, GCHandleType.Pinned),
                GCHandle.Alloc(vertexBlendWeights, GCHandleType.Pinned),
                GCHandle.Alloc(indices, GCHandleType.Pinned)
            };

                owner.jointBindPoses = pinnedArrays[0].AddrOfPinnedObject();
                owner.jointRadii = pinnedArrays[1].AddrOfPinnedObject();
                owner.jointParents = pinnedArrays[2].AddrOfPinnedObject();
                owner.vertexPositions = pinnedArrays[3].AddrOfPinnedObject();
                owner.vertexNormals = pinnedArrays[4].AddrOfPinnedObject();
                owner.vertexUVs = pinnedArrays[5].AddrOfPinnedObject();
                owner.vertexBlendIndices = pinnedArrays[6].AddrOfPinnedObject();
                owner.vertexBlendWeights = pinnedArrays[7].AddrOfPinnedObject();
                owner.indices = pinnedArrays[8].AddrOfPinnedObject();

                owner.jointCapacityInput = joints;
                owner.vertexCapacityInput = vertices;
                owner.indexCapacityInput = indexCount;
                owner.jointCountOutput = joints;
                owner.vertexCountOutput = vertices;
                owner.indexCountOutput = indexCount;

                gcPins = pinnedArrays;
            }

            ~HandMeshArrays()
            {
                if (gcPins != null)
                {
                    foreach (GCHandle h in gcPins)
                    {
                        h.Free();
                    }
                }
                gcPins = null;
            }
        }
        static Dictionary<XrHandTrackingMeshFB, HandMeshArrays> hand_mesh_pinned_arrays = new Dictionary<XrHandTrackingMeshFB, HandMeshArrays>();

        /*typedef struct XrHandTrackingMeshFB {
            XrStructureType    type;
            void*              next;
            uint32_t           jointCapacityInput;
            uint32_t           jointCountOutput;
            XrPosef*           jointBindPoses;
            float*             jointRadii;
            XrHandJointEXT*    jointParents;
            uint32_t           vertexCapacityInput;
            uint32_t           vertexCountOutput;
            XrVector3f*        vertexPositions;
            XrVector3f*        vertexNormals;
            XrVector2f*        vertexUVs;
            XrVector4sFB*      vertexBlendIndices;
            XrVector4f*        vertexBlendWeights;
            uint32_t           indexCapacityInput;
            uint32_t           indexCountOutput;
            int16_t*           indices;
        } XrHandTrackingMeshFB;*/

        /*typedef struct XrVector4sFB {
            int16_t    x;
            int16_t    y;
            int16_t    z;
            int16_t    w;
        } XrVector4sFB;*/
        [StructLayout(LayoutKind.Sequential)]
        internal struct XrVector4sFB
        {
            public short x;
            public short y;
            public short z;
            public short w;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct XrHandTrackingMeshFB
        {

            public XrHandTrackingMeshFB(uint joints, uint vertices, uint indexCount)
            {
                this.stype = 1000110001;
                this.next = IntPtr.Zero;
                jointCapacityInput = 0;
                jointBindPoses = IntPtr.Zero;
                jointRadii = IntPtr.Zero;
                jointParents = IntPtr.Zero;
                vertexCapacityInput = 0;
                vertexPositions = IntPtr.Zero;
                vertexNormals = IntPtr.Zero;
                vertexUVs = IntPtr.Zero;
                vertexBlendIndices = IntPtr.Zero;
                vertexBlendWeights = IntPtr.Zero;
                indexCapacityInput = 0;
                indices = IntPtr.Zero;
                jointCountOutput = 0;
                vertexCountOutput = 0;
                indexCountOutput = 0;


                if (joints != 0 && vertices != 0 && indexCount != 0)
                {
                    HandMeshArrays arrays = new HandMeshArrays(joints, vertices, indexCount, ref this);
                    hand_mesh_pinned_arrays.Add(this, arrays);
                }

            }

            public HandMeshArrays GetArrays()
            {
                if (hand_mesh_pinned_arrays.ContainsKey(this))
                {
                    HandMeshArrays arrays = hand_mesh_pinned_arrays[this];

                    hand_mesh_pinned_arrays.Remove(this);
                    return arrays;
                }
                return null;
            }

            int stype;
            IntPtr next;
            public uint jointCapacityInput;
            public uint jointCountOutput;
            public IntPtr jointBindPoses;
            public IntPtr jointRadii;
            public IntPtr jointParents;
            public uint vertexCapacityInput;
            public uint vertexCountOutput;
            public IntPtr vertexPositions;
            public IntPtr vertexNormals;
            public IntPtr vertexUVs;
            public IntPtr vertexBlendIndices;
            public IntPtr vertexBlendWeights;
            public uint indexCapacityInput;
            public uint indexCountOutput;
            public IntPtr indices;
        };

        /*XrResult xrGetHandMeshFB(
            XrHandTrackerEXT                            handTracker,
            XrHandTrackingMeshFB*                       mesh);*/
        internal delegate int Type_xrGetHandMeshFB(ulong handTracker, ref XrHandTrackingMeshFB mesh);

        Type_xrGetHandMeshFB xrGetHandMeshFB;

        public HandTrackingMeshFeature()
        {
            Debug.Log("new HandTrackingMeshFeature");
        }

        ~HandTrackingMeshFeature()
        {
            Debug.Log("delete HandTrackingMeshFeature");
        }

        override protected bool OnInstanceCreate(ulong xrInstance)
        {
            instance_ = xrInstance;
            if (!OpenXRRuntime.IsExtensionEnabled(xr_extension))
            {
                Debug.LogWarning($"{xr_extension} is not enabled.");
                // Return false here to indicate the system should disable your feature for this execution.  
                // Note that if a feature is marked required, returning false will cause the OpenXRLoader to abort and try another loader.
                return false;
            }

            xrGetInstanceProcAddr_ = Marshal.GetDelegateForFunctionPointer<PFN_xrGetInstanceProcAddr>(xrGetInstanceProcAddr);

            return true;
        }

        override protected void OnInstanceDestroy(ulong xrInstance)
        {
            instance_ = 0;
        }

        override protected void OnSessionBegin(ulong session)
        {
            session_ = session;
            Debug.Log($"{featureId}: {instance_}.{session_}");

            var getInstanceProcAddr = Marshal.GetDelegateForFunctionPointer<PFN_xrGetInstanceProcAddr>(xrGetInstanceProcAddr);
            Func<string, IntPtr> getAddr = (string name) =>
            {
                IntPtr ptr;
                getInstanceProcAddr(instance_, name, out ptr);
                return ptr;
            };
            xrGetHandMeshFB = Marshal.GetDelegateForFunctionPointer<Type_xrGetHandMeshFB>(getAddr("xrGetHandMeshFB"));
        }

        public GameObject CreateHandMesh(Transform parent, Material mat, ulong handle, string bone_postfix)
        {
            XrHandTrackingMeshFB meshZero = new XrHandTrackingMeshFB(0, 0, 0);
            // find size of mesh
            // Type_xrGetHandMeshFB mesh_get_fn = GetInstanceProc<Type_xrGetHandMeshFB>("xrGetHandMeshFB");
            int retVal = xrGetHandMeshFB(handle, ref meshZero);
            // get actual mesh
            XrHandTrackingMeshFB mesh = new XrHandTrackingMeshFB(meshZero.jointCountOutput, meshZero.vertexCountOutput, meshZero.indexCountOutput);
            retVal = xrGetHandMeshFB(handle, ref mesh);
            if (retVal != 0)
            {
                // Debug.LogError("mesh_get_fn");
                return null;
            }

            // construct mesh and bones and skin it correctly
            var handObj = new GameObject();
            handObj.transform.parent = parent;
            SkinnedMeshRenderer rend = handObj.AddComponent<SkinnedMeshRenderer>();

            HandMeshArrays meshArrays = mesh.GetArrays();
            Mesh handShape = new Mesh();
            Vector3[] vertices = new Vector3[mesh.vertexCountOutput];
            Vector3[] normals = new Vector3[mesh.vertexCountOutput];
            Vector2[] uvs = new Vector2[mesh.vertexCountOutput];
            BoneWeight[] weights = new BoneWeight[mesh.vertexCountOutput];
            int[] triangles = new int[mesh.indexCountOutput];
            for (int c = 0; c < mesh.vertexCountOutput; c++)
            {
                XrVector3f pos = meshArrays.vertexPositions[c];
                XrVector2f uv = meshArrays.vertexUVs[c];
                XrVector3f normal = meshArrays.vertexNormals[c];
                vertices[c] = pos.ToUnity();
                uvs[c] = new Vector2(uv.x, uv.y);
                normals[c] = normal.ToUnity();
                weights[c].boneIndex0 = meshArrays.vertexBlendIndices[c].x;
                weights[c].boneIndex1 = meshArrays.vertexBlendIndices[c].y;
                weights[c].boneIndex2 = meshArrays.vertexBlendIndices[c].z;
                weights[c].boneIndex3 = meshArrays.vertexBlendIndices[c].w;
                weights[c].weight0 = meshArrays.vertexBlendWeights[c].x;
                weights[c].weight1 = meshArrays.vertexBlendWeights[c].y;
                weights[c].weight2 = meshArrays.vertexBlendWeights[c].z;
                weights[c].weight3 = meshArrays.vertexBlendWeights[c].w;
            }
            for (int c = 0; c < mesh.indexCountOutput; c += 3)
            {
                triangles[c] = meshArrays.indices[c + 2];
                triangles[c + 1] = meshArrays.indices[c + 1];
                triangles[c + 2] = meshArrays.indices[c];
            }
            handShape.vertices = vertices;
            handShape.uv = uvs;
            handShape.triangles = triangles;
            handShape.normals = normals;
            //            handShape.RecalculateNormals();
            handShape.RecalculateBounds();
            handShape.RecalculateTangents();
            Transform[] boneTransforms = new Transform[mesh.jointCountOutput];
            GameObject[] bones = new GameObject[mesh.jointCountOutput];
            Matrix4x4[] bindPoses = new Matrix4x4[mesh.jointCountOutput];
            // first make the bone objects - this is because parenting of bones is not always ordered 
            for (int c = 0; c < mesh.jointCountOutput; c++)
            {
                bones[c] = new GameObject("Bone_" + c + bone_postfix);
            }
            for (int c = 0; c < mesh.jointCountOutput; c++)
            {
                XrPosef joint = meshArrays.jointBindPoses[c];
                XrPosef pose = meshArrays.jointBindPoses[c];
                bones[c].transform.position = pose.position.ToUnity();
                bones[c].transform.rotation = pose.orientation.ToUnity();
                bones[c].transform.localScale = new Vector3(meshArrays.jointRadii[c], meshArrays.jointRadii[c], meshArrays.jointRadii[c]);

                if (meshArrays.jointParents[c] < mesh.jointCountOutput)
                {
                    bones[c].transform.parent = bones[meshArrays.jointParents[c]].transform;
                }
                else
                {
                    bones[c].transform.parent = handObj.transform;
                    //rend.rootBone=bones[c].transform;
                }

                bindPoses[c] = bones[c].transform.worldToLocalMatrix;
                boneTransforms[c] = bones[c].transform;
            }
            handShape.bindposes = bindPoses;
            handShape.boneWeights = weights;
            rend.sharedMesh = handShape;
            rend.bones = boneTransforms;
            rend.material = mat;
            rend.updateWhenOffscreen = true;

            return handObj;
        }
    }
}