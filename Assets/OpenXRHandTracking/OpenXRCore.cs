using System;
using System.Runtime.InteropServices;
using UnityEngine;


namespace openxr
{
    internal enum XrSpaceLocationFlags : int
    {
        XR_SPACE_LOCATION_ORIENTATION_VALID_BIT = 0x00000001,
        XR_SPACE_LOCATION_POSITION_VALID_BIT = 0x00000002,
        XR_SPACE_LOCATION_ORIENTATION_TRACKED_BIT = 0x00000004,
        XR_SPACE_LOCATION_POSITION_TRACKED_BIT = 0x00000008
    };

    /*XrResult xrGetInstanceProcAddr(
        XrInstance                                  instance,
        const char*                                 name,
        PFN_xrVoidFunction*                         function);*/
    internal delegate int PFN_xrGetInstanceProcAddr(
        ulong instance,
        [MarshalAs(UnmanagedType.LPStr)] string name,
        out IntPtr function);

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

        public Vector3 PosToUnity()
        {
            return new Vector3(x, y, -z);
        }
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

        public Quaternion OrientationToUnity()
        {
            return new Quaternion(x, y, -z, -w);
        }
    }

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
}
