﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyboardInput : IMoveInput {

    private float mouseMoveSpeed = 0.005f;
    private Vector3 pitchYaw;
    private Vector3 lastPosition;

    private Transform shipTransform;
    private Transform camera, reticlePoint;
    private float baseScale;

    public KeyboardInput(Transform shipT) {
        shipTransform = shipT;
        camera = shipTransform.Find("Camera");
        reticlePoint = shipT.Find("MainReticle");
        baseScale = reticlePoint.localScale.x;
        float TODO = camera.GetComponent<Camera>().fieldOfView;
        lastPosition = new Vector3(Screen.width/2, Screen.height/2);
        UpdateInput();
    }

    public bool ReadInputs {
        get => true;
    }

    public void UpdateInput() {
        //pitchYaw += mouseMoveSpeed * (Input.mousePosition - lastPosition);
        //lastPosition = Input.mousePosition;
        pitchYaw = mouseMoveSpeed * (Input.mousePosition - lastPosition);
        pitchYaw.x = Mathf.Clamp(pitchYaw.x, -1, 1);
        pitchYaw.y = Mathf.Clamp(pitchYaw.y, -1, 1);

        reticlePoint.position = GetReticlePoint();
        float newScale = Vector3.Distance(camera.position, reticlePoint.position) / ReticleAimConstants.MaxReticleDist;
        reticlePoint.localScale = Vector3.one * baseScale * newScale;
        reticlePoint.rotation = Quaternion.LookRotation(camera.position - reticlePoint.position, reticlePoint.up);
    }

    public Vector3 GetReticlePoint() {
        Vector3 aimVector = Quaternion.Euler(ReticleAimConstants.MaxFiringAngle * -pitchYaw.y,
            ReticleAimConstants.MaxFiringAngle * pitchYaw.x, 0) * Vector3.forward;

        aimVector = shipTransform.TransformDirection(aimVector);

        float angle = Vector3.Angle(aimVector, shipTransform.forward);
        if (angle > ReticleAimConstants.MaxFiringAngle) {
            Quaternion arc = Quaternion.FromToRotation(shipTransform.forward, aimVector);
            arc = Quaternion.Slerp(Quaternion.identity, arc, ReticleAimConstants.MaxFiringAngle / angle);
            aimVector = arc * shipTransform.forward;
        }

        bool hitSomething = Physics.Raycast(camera.position, aimVector, out RaycastHit hit, ReticleAimConstants.MaxReticleDist);

        if (hitSomething) {
            if (Vector3.Distance(camera.position, hit.point) < ReticleAimConstants.MinPointDist) {
                aimVector *= ReticleAimConstants.MinPointDist;
            } else {
                return hit.point;
            }
        } else {
            aimVector *= ReticleAimConstants.MaxReticleDist;
        }

        return camera.position + aimVector;
    }

    public Vector3 GetAimPoint() {
        Vector3 aimVector = (reticlePoint.position - camera.position).normalized;

        bool hitSomething = Physics.Raycast(camera.position, aimVector, out RaycastHit hit, ReticleAimConstants.MaxPointDist);

        if (hitSomething) {
            if (Vector3.Distance(camera.position, hit.point) < ReticleAimConstants.MinPointDist) {
                aimVector *= ReticleAimConstants.MinPointDist;
            } else {
                return hit.point;
            }
        } else {
            aimVector *= ReticleAimConstants.MaxPointDist;
        }

        return camera.position + aimVector;
    }

    public Vector3 GetRotationInput() {
        return new Vector3(GetPitchInput(), GetYawInput(), GetRollInput());
    }

    public float GetThrustInput() {
        return Input.GetButton("Thrust") ? 1 : 0;
    }

    public int WeaponActivated() {
        if (Input.GetMouseButton(0)) {
            return 0;
        } else if (Input.GetMouseButton(1)) {
            return 1;
        } else if (Input.GetKey(KeyCode.E)) {
            return 2;
        }
        return int.MaxValue;
    }

    public int SpecialActivated() {
        if (Input.GetKey(KeyCode.LeftShift)) {
            return 0;
        } else if (Input.GetKeyDown(KeyCode.Q)) {
            return 1;
        } else if (Input.GetKey(KeyCode.W)) {
            return 2;
        }
        return int.MaxValue;
    }

    public void ProcessRawInput(Transform shipT) { }

    private float GetRollInput() {
        return -Input.GetAxis("Horizontal");
    }

    private float GetYawInput() {
        return pitchYaw.x;
    }

    private float GetPitchInput() {
        return -pitchYaw.y;
    }

}