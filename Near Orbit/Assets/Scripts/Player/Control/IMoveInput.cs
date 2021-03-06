﻿using UnityEngine;

/// <summary>
/// Base interface for player inputs.
/// 
/// </summary>
public interface IMoveInput
{

    /// <summary>
    /// Usually always true except for no-idle inputs like GestureInput.
    /// </summary>
    bool ReadInputs
    {
        get;
    }

    bool WeaponActivated();
    bool WeaponNextPressed();
    bool WeaponPrevPressed();

    int SpecialActivated(int index);

    /// <summary>
    /// Get the position of the reticle.
    /// </summary>
    Vector3 GetReticlePoint();

    /// <summary>
    /// Get the position of the aim point.
    /// </summary>
    Vector3 GetAimPoint();

    /// <summary>
    /// Compute the position of the aim point.
    /// </summary>
    void ComputeAimPoint(Vector3 reticlePosition);

    /// <summary>
    /// Set the position of the aim point.
    /// </summary>
    void SetAimPoint(Vector3 aimPosition);

    /// <summary>
    /// Local rotational velocity to be applied on the next frame.
    /// All values should be between -1 and 1.
    /// </summary>
    /// <returns></returns>
    Vector3 GetRotationInput();

    /// <summary>
    /// Forward thrust input to be applied on the next frame.
    /// Value should be between 0 and 1.
    /// </summary>
    /// <returns></returns>
    float GetThrustInput();

    /// <summary>
    /// Convert and update raw input to scaled values, to be called every frame.
    /// </summary>
    void UpdateInput();

    /// <summary>
    /// Mark one-time inputs (such as weapon switching) as read (set to false).
    /// </summary>
    void MarkAsRead();
}
