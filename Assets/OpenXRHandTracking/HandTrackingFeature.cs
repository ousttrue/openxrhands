using System.Collections.Generic;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine;
using UnityEditor;
using System.Runtime.InteropServices;
using System;
using AOT;


namespace openxr
{
#if UNITY_EDITOR
    [UnityEditor.XR.OpenXR.Features.OpenXRFeature(UiName = "Hand tracking Extension",
        BuildTargetGroups = new[] { BuildTargetGroup.Standalone, BuildTargetGroup.WSA, BuildTargetGroup.Android },
        Company = "Joe M",
        Desc = "Enable hand tracking in unity",
        DocumentationLink = "https://docs.unity3d.com/Packages/com.unity.xr.openxr@0.1/manual/index.html",
        OpenxrExtensionStrings = "XR_EXT_hand_tracking XR_FB_hand_tracking_mesh",
        Version = "0.0.1",
        FeatureId = featureId)]
#endif
    public class HandTrackingFeature : OpenXRFeature
    {
        public const string featureId = "com.joemarshall.handtracking";

        /*XrResult xrGetInstanceProcAddr(
            XrInstance                                  instance,
            const char*                                 name,
            PFN_xrVoidFunction*                         function);*/
        internal delegate int Type_xrGetInstanceProcAddr(ulong instance, [MarshalAs(UnmanagedType.LPStr)] string name, out IntPtr function);

        public enum Hand_Index { L, R };
        ulong instance_;
        ulong session_;

        ulong handle_left = 0;
        ulong handle_right = 0;
        static Dictionary<XrHandTrackingMeshFB, HandMeshArrays> hand_mesh_pinned_arrays = new Dictionary<XrHandTrackingMeshFB, HandMeshArrays>();

        public HandTrackingFeature()
        {
            Debug.Log("new");
        }

        ~HandTrackingFeature()
        {
            Debug.Log("delete");
        }

        Vector3 PosToUnity(XrVector3f pos)
        {
            return new Vector3(pos.x, pos.y, -pos.z);
        }

        Quaternion OrientationToUnity(XrVector4f ori)
        {
            return new Quaternion(ori.x, ori.y, -ori.z, -ori.w);
        }

        ulong GetHandle(Hand_Index hand)
        {
            switch (hand)
            {
                case Hand_Index.L:
                    if (handle_left == 0)
                    {
                        Debug.LogError("handle_left==0");
                    }
                    return handle_left;
                case Hand_Index.R:
                    if (handle_right == 0)
                    {
                        Debug.LogError("handle_right==0");
                    }
                    return handle_right;
                default:
                    return 0;
            }
        }

        // get the address of the hand tracking functions using: OpenXRFeature.xrGetInstanceProcAddr

        /*XrResult xrCreateHandTrackerEXT(
            XrSession                                   session,
            const XrHandTrackerCreateInfoEXT*           createInfo,
            XrHandTrackerEXT*                           handTracker);*/
        internal delegate int Type_xrCreateHandTrackerEXT(ulong session, in XrHandTrackerCreateInfoEXT createInfo, out ulong tracker);

        /*
            XrResult xrDestroyHandTrackerEXT(XrHandTrackerEXT handTracker);
        */
        internal delegate int Type_xrDestroyHandTrackerEXT(ulong tracker);

        /*XrResult xrLocateHandJointsEXT(
            XrHandTrackerEXT                            handTracker,
            const XrHandJointsLocateInfoEXT*            locateInfo,
            XrHandJointLocationsEXT*                    locations);*/
        internal delegate int Type_xrLocateHandJointsEXT(ulong tracker, in XrHandJointsLocateInfoEXT locateInfoEXT, ref XrHandJointLocationsEXT locations);

        /*typedef struct XrHandJointsLocateInfoEXT {
            XrStructureType    type;
            const void*        next;
            XrSpace            baseSpace;
            XrTime             time;
        } XrHandJointsLocateInfoEXT;*/
        [StructLayout(LayoutKind.Sequential)]
        internal struct XrHandJointsLocateInfoEXT
        {
            public XrHandJointsLocateInfoEXT(ulong space, long time)
            {
                stype = 1000051002;
                this.space = space;
                this.time = time;
                this.next = IntPtr.Zero;
            }
            int stype;
            IntPtr next;
            ulong space;
            long time;
        };

        enum XrSpaceLocationFlags
        {
            XR_SPACE_LOCATION_ORIENTATION_VALID_BIT = 0x00000001,
            XR_SPACE_LOCATION_POSITION_VALID_BIT = 0x00000002,
            XR_SPACE_LOCATION_ORIENTATION_TRACKED_BIT = 0x00000004,
            XR_SPACE_LOCATION_POSITION_TRACKED_BIT = 0x00000008
        };

        /*
        typedef struct XrHandJointLocationEXT {
            XrSpaceLocationFlags    locationFlags;
            XrPosef                 pose;
            float                   radius;
        } XrHandJointLocationEXT;
        */
        [StructLayout(LayoutKind.Sequential)]
        internal struct XrHandJointLocationEXT
        {
            // TODO check size of enums and types
            public ulong locationFlags;
            public XrPosef pose;
            public float radius; // joint radius
        }
        static GCHandle pinnedJointArray;
        /*typedef struct XrHandJointLocationsEXT {
        XrStructureType            type;
        void*                      next;
        XrBool32                   isActive;
        uint32_t                   jointCount;
        XrHandJointLocationEXT*    jointLocations;
    } XrHandJointLocationsEXT;*/
        [StructLayout(LayoutKind.Sequential)]
        internal struct XrHandJointLocationsEXT
        {
            public XrHandJointLocationsEXT(ref XrHandJointLocationEXT[] jointArray)
            {
                pinnedJointArray = GCHandle.Alloc(jointArray, GCHandleType.Pinned);
                jointCount = (uint)jointArray.Length;
                stype = 1000051003;
                next = IntPtr.Zero;
                isActive = 0;
                jointLocations = pinnedJointArray.AddrOfPinnedObject();
            }

            public void Unpin()
            {
                pinnedJointArray.Free();
            }
            int stype;
            IntPtr next;
            int isActive;
            uint jointCount;

            IntPtr jointLocations;
        };


        /*typedef struct XrHandTrackerCreateInfoEXT {
            XrStructureType      type;
            const void*          next;
            XrHandEXT            hand;
            XrHandJointSetEXT    handJointSet;
        } XrHandTrackerCreateInfoEXT;*/
        [StructLayout(LayoutKind.Sequential)]
        internal struct XrHandTrackerCreateInfoEXT
        {
            public XrHandTrackerCreateInfoEXT(int hand)
            {
                this.stype = 1000051001;
                this.next = IntPtr.Zero;
                this.hand = hand;
                this.handJointSet = 0; // standard set of joints
            }
            int stype;
            IntPtr next;
            int hand;
            int handJointSet;
        }

        Type_xrCreateHandTrackerEXT xrCreateHandTrackerEXT;
        Type_xrDestroyHandTrackerEXT xrDestroyHandTrackerEXT;
        Type_xrLocateHandJointsEXT xrLocateHandJointsEXT;
        Type_xrGetHandMeshFB xrGetHandMeshFB;

        public event Action SessionBegin;

        override protected void OnSessionBegin(ulong session)
        {
            session_ = session;
            Debug.Log($"OnSessionBegin: {instance_}.{session_}");

            var getInstanceProcAddr = Marshal.GetDelegateForFunctionPointer<Type_xrGetInstanceProcAddr>(xrGetInstanceProcAddr);
            Func<string, IntPtr> getAddr = (string name) =>
            {
                IntPtr ptr;
                getInstanceProcAddr(instance_, name, out ptr);
                return ptr;
            };
            xrCreateHandTrackerEXT = Marshal.GetDelegateForFunctionPointer<Type_xrCreateHandTrackerEXT>(getAddr("xrCreateHandTrackerEXT"));
            xrDestroyHandTrackerEXT = Marshal.GetDelegateForFunctionPointer<Type_xrDestroyHandTrackerEXT>(getAddr("xrDestroyHandTrackerEXT"));
            xrLocateHandJointsEXT = Marshal.GetDelegateForFunctionPointer<Type_xrLocateHandJointsEXT>(getAddr("xrLocateHandJointsEXT"));
            xrGetHandMeshFB = Marshal.GetDelegateForFunctionPointer<Type_xrGetHandMeshFB>(getAddr("xrGetHandMeshFB"));

            {
                XrHandTrackerCreateInfoEXT lh_create = new XrHandTrackerCreateInfoEXT(1);
                var retVal = xrCreateHandTrackerEXT(session, lh_create, out handle_left);
                if (retVal != 0)
                {
                    Debug.Log("Couldn't open left  hand tracker: Error " + retVal);
                    return;
                }
            }

            {
                XrHandTrackerCreateInfoEXT rh_create = new XrHandTrackerCreateInfoEXT(2);
                var retVal = xrCreateHandTrackerEXT(session, rh_create, out handle_right);
                if (retVal != 0)
                {
                    Debug.Log("Couldn't open right  hand tracker: Error " + retVal);
                    return;
                }
            }

            SessionBegin();
        }

        public event Action SessionEnd;

        override protected void OnSessionEnd(ulong session)
        {
            Debug.Log($"OnSessionEnd: {instance_}.{session_}");
            SessionEnd();
            closeHandTracker();
            session_ = 0;
        }

        override protected void OnSessionDestroy(ulong xrSession)
        {
            Debug.Log("OnSessionDestroy");
            closeHandTracker();
        }

        void closeHandTracker()
        {
            if (handle_left != 0)
            {
                // Type_xrDestroyHandTrackerEXT fp = GetInstanceProc<Type_xrDestroyHandTrackerEXT>("xrDestroyHandTrackerEXT");
                // if (fp != null)
                // {
                //     fp(handle_left);
                // }
                xrDestroyHandTrackerEXT(handle_left);
                handle_left = 0;
            }
            if (handle_right != 0)
            {
                // Type_xrDestroyHandTrackerEXT fp = GetInstanceProc<Type_xrDestroyHandTrackerEXT>("xrDestroyHandTrackerEXT");
                // if (fp != null)
                // {
                //     fp(handle_right);
                // }
                xrDestroyHandTrackerEXT(handle_right);
                handle_right = 0;
            }
        }

        override protected bool OnInstanceCreate(ulong xrInstance)
        {
            instance_ = xrInstance;
            return true;
        }

        override protected void OnInstanceDestroy(ulong xrInstance)
        {
            closeHandTracker();
            instance_ = 0;
        }

        public void GetHandJoints(long frame_time, Hand_Index hand, out Vector3[] positions, out Quaternion[] orientations, out float[] radius)
        {
            var handle = GetHandle(hand);
            if (handle != 0)
            {
                XrHandJointLocationEXT[] allJoints = new XrHandJointLocationEXT[26];
                XrHandJointsLocateInfoEXT jli = new XrHandJointsLocateInfoEXT(OpenXRFeature.GetCurrentAppSpace(), frame_time);
                XrHandJointLocationsEXT joints = new XrHandJointLocationsEXT(ref allJoints);
                // Type_xrLocateHandJointsEXT fp = GetInstanceProc<Type_xrLocateHandJointsEXT>("xrLocateHandJointsEXT");
                // if (fp != null)
                {
                    int retVal = xrLocateHandJointsEXT(handle, jli, ref joints);
                    joints.Unpin();
                    if (retVal == 0)
                    {
                        positions = new Vector3[allJoints.Length];
                        orientations = new Quaternion[allJoints.Length];
                        radius = new float[allJoints.Length];
                        for (int c = 0; c < allJoints.Length; c++)
                        {
                            positions[c] = PosToUnity(allJoints[c].pose.position);
                            orientations[c] = OrientationToUnity(allJoints[c].pose.orientation);
                            if ((allJoints[c].locationFlags & 0x3) == 0)
                            {
                                radius[c] = 0f;
                            }
                            else
                            {
                                radius[c] = allJoints[c].radius;
                            }
                        }
                        return;
                    }
                }
            }
            // no tracking yet - return zero arrays
            positions = new Vector3[0];
            orientations = new Quaternion[0];
            radius = new float[0];
        }


        /********************************************************* HAND MESH (oculus specific) STUFF BELOW *********************************/

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

        /*typedef struct XrPosef {
            XrQuaternionf    orientation;
            XrVector3f       position;
        } XrPosef;*/
        [StructLayout(LayoutKind.Sequential)]
        internal struct XrPosef
        {
            public XrVector4f orientation;
            public XrVector3f position;
        }

        /*typedef struct XrVector2f {
            float    x;
            float    y;
        } XrVector2f;*/
        [StructLayout(LayoutKind.Sequential)]
        internal struct XrVector2f
        {
            public float x;
            public float y;
        }


        /*typedef struct XrVector3f {
            float    x;
            float    y;
            float    z;
        } XrVector3f;*/
        [StructLayout(LayoutKind.Sequential)]
        internal struct XrVector3f
        {
            public float x;
            public float y;
            public float z;
        }

        /*typedef struct XrVector4f {
            float    x;
            float    y;
            float    z;
            float    w;
        } XrVector4f;*/
        [StructLayout(LayoutKind.Sequential)]
        internal struct XrVector4f
        {
            public float x;
            public float y;
            public float z;
            public float w;
        }

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

        public GameObject CreateHandMesh(Hand_Index hand, Transform parent, Material mat)
        {
            ulong handle = 0;
            string bone_postfix = "";
            if (hand == Hand_Index.L)
            {
                handle = handle_left;
                bone_postfix = "_lh";
            }
            else
            {
                handle = handle_right;
                bone_postfix = "_rh";
            }
            if (handle == 0)
            {
                // Debug.LogError("no handle");
                return null;
            }

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
            var handObj = new GameObject((hand == Hand_Index.L) ? "lhand" : "rhand");
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
                vertices[c] = PosToUnity(pos);
                uvs[c] = new Vector2(uv.x, uv.y);
                normals[c] = PosToUnity(normal);
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
                bones[c].transform.position = PosToUnity(pose.position);
                bones[c].transform.rotation = OrientationToUnity(pose.orientation);
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

        public void ApplyHandJointsToMesh(long frame_time, Hand_Index hand, GameObject handObj)
        {
            var handle = GetHandle(hand);
            if (handObj != null)
            {
                Transform[] bones = handObj.GetComponent<SkinnedMeshRenderer>().bones;
                float[] radius;
                Vector3[] positions;
                Quaternion[] orientations;
                GetHandJoints(frame_time, hand, out positions, out orientations, out radius);
                if (radius.Length == bones.Length && radius[0] > 0)
                {
                    for (int c = 0; c < bones.Length; c++)
                    {
                        bones[c].position = positions[c];
                        bones[c].rotation = orientations[c];
                    }
                }
            }
        }
    }
}