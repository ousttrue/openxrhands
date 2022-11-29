using UnityEngine.XR.OpenXR.Features;
using System.Runtime.InteropServices;
using System;
using UnityEngine.XR.OpenXR;
using UnityEngine;

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
        [StructLayout(LayoutKind.Sequential)]
        internal struct XrHandTrackingMeshFB
        {
            public int stype;
            public IntPtr next;
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
        internal delegate XrResult Type_xrGetHandMeshFB(ulong handTracker, ref XrHandTrackingMeshFB mesh);

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

        public GameObject CreateHandMesh(ulong handle, Material mat)
        {
            var xrMesh = new XrHandTrackingMeshFB
            {
                stype = 1000110001,
            };
            // find size of mesh
            // Type_xrGetHandMeshFB mesh_get_fn = GetInstanceProc<Type_xrGetHandMeshFB>("xrGetHandMeshFB");
            var retVal = xrGetHandMeshFB(handle, ref xrMesh);
            if (retVal != 0)
            {
                Debug.LogError($"mesh_get_fn: {retVal}");
                return null;
            }

            // get actual mesh
            // alloc data
            using (var data = new HandTrackingMeshData((int)xrMesh.jointCountOutput, (int)xrMesh.vertexCountOutput, (int)xrMesh.indexCountOutput))
            {
                // alloc
                xrMesh.jointBindPoses = data.jointBindPoses.Ptr;
                xrMesh.jointRadii = data.jointRadii.Ptr;
                xrMesh.jointParents = data.jointParents.Ptr;
                xrMesh.jointCapacityInput = xrMesh.jointCountOutput;

                xrMesh.vertexPositions = data.vertexPositions.Ptr;
                xrMesh.vertexNormals = data.vertexNormals.Ptr;
                xrMesh.vertexUVs = data.vertexUVs.Ptr;
                xrMesh.vertexBlendIndices = data.vertexBlendIndices.Ptr;
                xrMesh.vertexBlendWeights = data.vertexBlendWeights.Ptr;
                xrMesh.vertexCapacityInput = xrMesh.vertexCountOutput;

                xrMesh.indices = data.indices.Ptr;
                xrMesh.indexCapacityInput = xrMesh.indexCountOutput;

                retVal = xrGetHandMeshFB(handle, ref xrMesh);
                if (retVal != 0)
                {
                    Debug.LogError($"mesh_get_fn: {retVal}");
                    return null;
                }

                // unity Mesh
                return data.CreateSkinnedMesh();
            }
        }
    }
}
