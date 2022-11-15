using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR;


public class HandGetter : MonoBehaviour
{
    HandTrackingFeature hf = null;
    GameObject lhand;
    GameObject rhand;
    public Material mat;

    void Start()
    {
        hf = OpenXRSettings.Instance.GetFeature<HandTrackingFeature>();
    }

    void Update()
    {
        if (hf == null || hf.enabled == false)
        {
            print("You need to enable the openXR hand tracking support extension ");
        }

        if (lhand == null)
        {
            hf.GetHandMesh(HandTrackingFeature.Hand_Index.L, transform, mat, out lhand);
        }
        else
        {
            hf.ApplyHandJointsToMesh(HandTrackingFeature.Hand_Index.L, lhand);
        }
        if (rhand == null)
        {
            hf.GetHandMesh(HandTrackingFeature.Hand_Index.R, transform, mat, out rhand);
        }
        else
        {
            hf.ApplyHandJointsToMesh(HandTrackingFeature.Hand_Index.R, rhand);
        }
    }
}
