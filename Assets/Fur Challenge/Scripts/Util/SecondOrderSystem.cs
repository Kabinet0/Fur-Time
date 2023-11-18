using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//TODO:
// [-] Add options for velocity input
// [X] Make resettable
// [-] Editable in inspector

// Credit to t3ssel8r on youtube

[System.Serializable]
public struct DynamicsParameters
{
    public DynamicsParameters(float frequency, float damping, float response)
    {
        Frequency = frequency;
        Damping = damping;
        Response = response;    
    }

    public float Frequency;
    [Range(0, 4)]
    public float Damping;
    [Range(-5, 5)]
    public float Response;
}


public class FloatSecondOrderDynamics
{
    private float k1, k2, k3;
    private float previousnput;
    private float position;
    private float velocity;


    public FloatSecondOrderDynamics(DynamicsParameters parameters, float startingPosition)
    {
        UpdateParameters(parameters);

        previousnput = startingPosition;
        position = startingPosition;
        velocity = 0;
    }

    public void UpdateParameters(DynamicsParameters parameters)
    {
        float frequency = parameters.Frequency;
        float damping = parameters.Damping;
        float response = parameters.Response;

        if (frequency <= 0) { Debug.LogError("Invalid Dynamics Parameter"); }

        k1 = damping / (Mathf.PI * frequency);
        k2 = 1 / Mathf.Pow(2 * Mathf.PI * frequency, 2);
        k3 = (response * damping) / (2 * Mathf.PI * frequency);
    }

    public void SetValues(float Position, float Velocity) 
    { 
        position = Position;
        previousnput = Position;
        velocity = Velocity;
    }

    // If precise velocity is needed, and is available, copy func and replace TargetVelocity with a parameter
    public float Evaluate(float Target)
    {
        float T = Time.deltaTime;

        // Estimate Target Velocity
        float TargetVelocity = (Target - previousnput) / T;
        previousnput = Target;

        // Magic as far as I'm concerned
        float k2_stable = Mathf.Max(k2, T*T/2 + T*k1/2, T*k1); // Clamp k2 if timestep is too low to be stable

        position = position + T * velocity; // Integrate position
        float acceleration = (Target + k3 * TargetVelocity - position - k1 * velocity) / k2_stable;
        velocity = velocity + T * acceleration; // Integrate velocity

        return position;
    }
}

public class AngleSecondOrderDynamics
{
    private float k1, k2, k3;
    private float previousnput;
    private float position;
    private float velocity;


    public AngleSecondOrderDynamics(DynamicsParameters parameters, float startingPosition)
    {
        UpdateParameters(parameters);

        previousnput = startingPosition;
        position = startingPosition;
        velocity = 0;
    }

    public void UpdateParameters(DynamicsParameters parameters)
    {
        float frequency = parameters.Frequency;
        float damping = parameters.Damping;
        float response = parameters.Response;

        if (frequency <= 0) { Debug.LogError("Invalid Dynamics Parameter"); }

        k1 = damping / (Mathf.PI * frequency);
        k2 = 1 / Mathf.Pow(2 * Mathf.PI * frequency, 2);
        k3 = (response * damping) / (2 * Mathf.PI * frequency);
    }

    public void SetValues(float Position, float Velocity)
    {
        position = Position;
        previousnput = Position;
        velocity = Velocity;
    }

    // If precise velocity is needed, and is available, copy func and replace TargetVelocity with a parameter
    public float Evaluate(float Target)
    {
        Target = position + Mathf.DeltaAngle(position, Target);
        float T = Time.deltaTime;

        // Estimate Target Velocity
        float TargetVelocity = (Target - previousnput) / T;
        previousnput = Target;

        // Magic as far as I'm concerned
        float k2_stable = Mathf.Max(k2, T * T / 2 + T * k1 / 2, T * k1); // Clamp k2 if timestep is too low to be stable

        position = position + T * velocity; // Integrate position
        float acceleration = (Target + k3 * TargetVelocity - position - k1 * velocity) / k2_stable;
        velocity = velocity + T * acceleration; // Integrate velocity

        return position;
    }
}

public class Vector2SecondOrderDynamics
{
    private float k1, k2, k3;

    private Vector2 previousnput;
    private Vector2 position;
    private Vector2 velocity;


    public Vector2SecondOrderDynamics(DynamicsParameters parameters, Vector2 startingPosition)
    {
        UpdateParameters(parameters);

        previousnput = startingPosition;
        position = startingPosition;
        velocity = Vector2.zero;
    }

    public void UpdateParameters(DynamicsParameters parameters)
    {
        float frequency = parameters.Frequency;
        float damping = parameters.Damping;
        float response = parameters.Response;

        if (frequency <= 0) { Debug.LogError("Invalid Dynamics Parameter"); }

        k1 = damping / (Mathf.PI * frequency);
        k2 = 1 / Mathf.Pow(2 * Mathf.PI * frequency, 2);
        k3 = (response * damping) / (2 * Mathf.PI * frequency);
    }

    public void SetValues(Vector2 Position, Vector2 Velocity)
    {
        position = Position;
        previousnput = Position;
        velocity = Velocity;
    }

    // If precise velocity is needed, and is available, copy func and replace TargetVelocity with a parameter
    public Vector2 Evaluate(Vector2 Target)
    {
        float T = Time.deltaTime;

        // Estimate Target Velocity
        Vector2 TargetVelocity = (Target - previousnput) / T;
        previousnput = Target;

        // Magic as far as I'm concerned
        float k2_stable = Mathf.Max(k2, T * T / 2 + T * k1 / 2, T * k1); // Clamp k2 if timestep is too low to be stable

        position = position + T * velocity; // Integrate position
        Vector2 acceleration = (Target + k3 * TargetVelocity - position - k1 * velocity) / k2_stable;
        velocity = velocity + T * acceleration; // Integrate velocity

        return position;
    }
}

public class Vector3SecondOrderDynamics
{
    private float k1, k2, k3;

    private Vector3 previousnput;
    private Vector3 position;
    private Vector3 velocity;


    public Vector3SecondOrderDynamics(DynamicsParameters parameters, Vector3 startingPosition)
    {
        UpdateParameters(parameters);

        previousnput = startingPosition;
        position = startingPosition;
        velocity = Vector3.zero;
    }

    public void UpdateParameters(DynamicsParameters parameters)
    {
        float frequency = parameters.Frequency;
        float damping = parameters.Damping;
        float response = parameters.Response;

        if (frequency <= 0) { Debug.LogError("Invalid Dynamics Parameter"); }

        k1 = damping / (Mathf.PI * frequency);
        k2 = 1 / Mathf.Pow(2 * Mathf.PI * frequency, 2);
        k3 = (response * damping) / (2 * Mathf.PI * frequency);
    }

    public void SetValues(Vector3 Position, Vector3 Velocity)
    {
        position = Position;
        previousnput = Position;
        velocity = Velocity;
    }

    // If precise velocity is needed, and is available, copy func and replace TargetVelocity with a parameter
    public Vector3 Evaluate(Vector3 Target)
    {
        float T = Time.deltaTime;

        // Estimate Target Velocity
        Vector3 TargetVelocity = (Target - previousnput) / T;
        previousnput = Target;

        // Magic as far as I'm concerned
        float k2_stable = Mathf.Max(k2, T * T / 2 + T * k1 / 2, T * k1); // Clamp k2 if timestep is too low to be stable

        position = position + T * velocity; // Integrate position
        Vector3 acceleration = (Target + k3 * TargetVelocity - position - k1 * velocity) / k2_stable;
        velocity = velocity + T * acceleration; // Integrate velocity

        return position;
    }
}
