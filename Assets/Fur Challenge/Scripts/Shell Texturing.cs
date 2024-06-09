using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static VerletIntegration;

public class ShellTexturing : MonoBehaviour
{
    [SerializeField] private Mesh ShellMesh;
    [SerializeField] private Shader ShellShader;
    [SerializeField] private bool LiveUpdate;
    [SerializeField] private Transform Target;

    [Header("Properties")]

    [SerializeField] private DynamicsParameters ObjectDynamicsParameters;

    [SerializeField] private int ShellCount;

    [SerializeField] private int Density;

    [SerializeField] private float ShellExtent;

    [SerializeField] private Color ShellColor;

    [SerializeField] private Color ShellOcclusionColor;

    [Range(0, 1)]
    [SerializeField] private float DisplacementStrength;

    [Range(1, 5)]
    [SerializeField] private float DisplacementShaping;

    [Header("Strand Settings")]

    [SerializeField] private float Thickness;

    [Range(0, 4)]
    [SerializeField] private float Curvature;

    [Range(0, 4)]
    [SerializeField] private float HeightAttenuation;

    [Range(0, 2)]
    [SerializeField] private float JitterAmount;

    [Header("Wind")]

    [Range(0, 5)]
    [SerializeField] private float WindStrength;

    [Range(0, 5)]
    [SerializeField] private float WindSpeed;

    [Range(1, 20)]
    [SerializeField] private float WindNoiseDensity;

    [SerializeField] private float DampingFactor;

    [SerializeField] private float RestoringForce;

    [Header("Vectors")]
    [SerializeField] private Vector3 WindVector;
    //[SerializeField] private Vector3 DisplacementVector;

    private GameObject[] ShellList;
    [SerializeField] private Vector3[] ShellVelocities; 
    private MaterialPropertyBlock ShellProperties;
    private Material ShellMaterial;

    private Vector3SecondOrderDynamics TargetFollowDynamics;
    private Vector3 ObjectPosition;
    private float DistanceBetweenShells;
    //private Vector3 LastFramePosition;


    private VerletIntegration verletIntegration;
    void Start()
    {
        verletIntegration = GetComponent<VerletIntegration>();
        ClearShells();

        ShellList = new GameObject[ShellCount];
        ShellProperties = new MaterialPropertyBlock();
        ShellMaterial = new Material(ShellShader);
        ShellVelocities = new Vector3[ShellCount];

        TargetFollowDynamics = new Vector3SecondOrderDynamics(ObjectDynamicsParameters, transform.position);

        DistanceBetweenShells = ShellExtent / ShellCount;
        Debug.Log("Distance is: " + DistanceBetweenShells);

        //LastFramePosition = Target.transform.position;

        List<Point> points = new List<Point>();
        List<Connection> connections = new List<Connection>();
        for (int i = 0; i < ShellCount; i++)
        {
            GameObject ShellObject = new GameObject("Shell " + i.ToString());
            ShellObject.transform.SetParent(transform, false);
            ShellList[i] = ShellObject;

            MeshFilter ShellFilter = ShellObject.AddComponent<MeshFilter>();
            ShellFilter.mesh = ShellMesh;

            MeshRenderer ShellRenderer = ShellObject.AddComponent<MeshRenderer>();
            ShellRenderer.material = ShellMaterial;

            PreparePropertyBlock(i);
            ShellRenderer.SetPropertyBlock(ShellProperties);

            ShellVelocities[i] = Vector3.zero;

            points.Add(new Point(transform.position - new Vector3(0, 0.05f * i, 0), false, 1));
        }

        // Hack to fix point
        points[0].locked = true;
        points[0].inverseMass = 0;

        for (int i = 0; i < points.Count - 1; i++)
        {
            connections.Add(new Connection(points[i], points[i + 1], DistanceBetweenShells));
        }

        verletIntegration.SetPointData(points);
        verletIntegration.SetConnectionData(connections);
        verletIntegration.OnApplyCustomConstraints += ApplyCustomConstraints;
    }

    void Update()
    {
        //Vector3 PosDelta = Target.transform.position - LastFramePosition;
        transform.position = TargetFollowDynamics.Evaluate(Target.transform.position);

        //ShellVelocities[0] = Vector3.zero;
        //ShellList[0].transform.position = ObjectPosition;

        //for (int i = 1; i < ShellCount; i++)
        //{
        //    ShellList[i].transform.position = ShellList[i].transform.position + ShellVelocities[i] * Time.deltaTime;
        //}

        // Pulling mechanics
        //for (int i = 1; i < ShellCount; i++)
        //{
        //    Vector3 CurrentPosition = ShellList[i].transform.position;
        //    float DistanceToPreviousShell = Vector3.Distance(CurrentPosition, ShellList[i - 1].transform.position);



        //    // If too far from last shell, pull
        //    if (DistanceToPreviousShell > DistanceBetweenShells)
        //    {
        //        Vector3 ShellTargetPosition = Vector3.MoveTowards(CurrentPosition, ShellList[i - 1].transform.position, DistanceToPreviousShell - DistanceBetweenShells);
        //        ShellVelocities[i] = (ShellTargetPosition - CurrentPosition) * 1.001f / Time.deltaTime;
        //        ShellList[i].transform.position = ShellList[i].transform.position + ShellVelocities[i] * Time.deltaTime;
        //    }
        //    else
        //    {

        //        //ShellVelocities[i] = ShellVelocities[i] + RestoringForce * (Target.transform.position - CurrentPosition) * Time.deltaTime + PosDelta * Time.deltaTime;
        //    }
        //    //ShellVelocities[i] = ShellVelocities[i] - new Vector3(0, 1, 0) * Time.deltaTime;
        //    //ShellList[i].transform.position = ShellList[i].transform.position + ShellVelocities[i] * Time.deltaTime;
        //}

        //Debug.Log(ShellVelocities[5].normalized);
        var points = verletIntegration.GetPointData();
        for (int i = 0; i < points.Length; i++)
        {
            ShellList[i].transform.position = points[i].position;
        }
        if (LiveUpdate)
        {
            UpdateShellValues();
        }

        //LastFramePosition = Target.transform.position;
    }

    private void FixedUpdate()
    {
        verletIntegration.Simulate();
    }

    void PreparePropertyBlock(int index)
    {
        ShellProperties.Clear();
        ShellProperties.SetInt("_ShellIndex", index);
        ShellProperties.SetInt("_ShellCount", ShellCount);
        ShellProperties.SetFloat("_ShellExtent", ShellExtent);
        ShellProperties.SetInt("_Density", Density);
        ShellProperties.SetVector("_ShellColor", ShellColor.linear);
        ShellProperties.SetVector("_ShellOcclusionColor", ShellOcclusionColor.linear);
        ShellProperties.SetFloat("_Thickness", Thickness);
        //ShellProperties.SetFloat("_DisplacementStrength", DisplacementStrength);
        
        ShellProperties.SetFloat("_DisplacementShaping", DisplacementShaping);

        ShellProperties.SetFloat("_Curvature", Curvature);
        ShellProperties.SetFloat("_DistanceAttenuation", HeightAttenuation);
        ShellProperties.SetFloat("_JitterAmount", JitterAmount);
        ShellProperties.SetFloat("_WindStrength", WindStrength);

        Vector3 DisplacementVector = transform.position - ShellList[index].transform.position;
        float mag = DisplacementVector.magnitude;
        DisplacementVector = DisplacementVector.normalized;

        ShellProperties.SetVector("_DisplacementVector", new Vector4(DisplacementVector.x, DisplacementVector.y, DisplacementVector.z, mag * DisplacementStrength));

        Vector3 SpeedAdjustedWind = WindVector * WindSpeed;
        ShellProperties.SetVector("_WindVector", new Vector4(SpeedAdjustedWind.x, SpeedAdjustedWind.y, SpeedAdjustedWind.z, WindNoiseDensity));
    }

    void UpdateShellValues()
    {
        for (int i = 0; i < ShellCount; i++)
        {
            PreparePropertyBlock(i);

            ShellList[i].GetComponent<MeshRenderer>().SetPropertyBlock(ShellProperties);
        }
    }

    void ClearShells()
    {
        if (ShellList != null)
        {
            foreach (GameObject shell in ShellList)
            {
                Destroy(shell);
            }
            ShellList = null;
        }
    }

    void ApplyCustomConstraints(ref Point[] points, ref Connection[] connections)
    {
        points[0].position = transform.position;
    }
}
