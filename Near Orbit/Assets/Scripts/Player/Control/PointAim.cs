﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attached to a controller and handles point aiming.
/// </summary>
public class PointAim : MonoBehaviour {

    [SerializeField]
    private Transform headTrack;
    [SerializeField]
    private Transform reticle;

    // TODO: Only for testing, remove later
    [SerializeField]
    private Transform aimPoint;

    private const float reticleDist = 3f;

    private float aimPointDist = 5f;

    void Update() {
        aimPoint.position = GetAimPoint();
        Debug.Log(Vector3.Distance(headTrack.position, reticle.position));
    }

    public void SetPointDist(float dist) {
        aimPointDist = dist;
    }

    public Vector3 GetAimPoint() {
        Vector3 vectorA = headTrack.position - transform.position;
        Vector3 vectorP = transform.forward * int.MaxValue;
        float degXYW = Vector3.Angle(vectorA, vectorP);

        float lenXW = Mathf.Sin(degXYW * Mathf.Deg2Rad) * vectorA.magnitude;
        float degZXW = Mathf.Acos(lenXW / reticleDist) * Mathf.Rad2Deg;
        float degZXY = degZXW + (90f - degXYW);

        float lenYZ = LawOfCosines(vectorA.magnitude, reticleDist, degZXY * Mathf.Deg2Rad);
        reticle.position = transform.position + (transform.forward * lenYZ);

        Vector3 aimVector = (reticle.position - headTrack.position).normalized;
        RaycastHit hit;
        bool hitSomething = Physics.Raycast(headTrack.position, aimVector, out hit, aimPointDist);

        return hitSomething ? hit.point : headTrack.position + (aimVector * aimPointDist);
    }

    /// <summary>
    /// Returns c in the law of cosines equation.
    /// </summary>
    private float LawOfCosines(float lenA, float lenB, float radC) {
        return Mathf.Sqrt(Mathf.Pow(lenA, 2) + Mathf.Pow(lenB, 2) - (2 * lenA * lenB * Mathf.Cos(radC)));
    }

}