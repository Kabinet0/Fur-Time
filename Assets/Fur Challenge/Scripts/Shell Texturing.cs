using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShellTexturing : MonoBehaviour
{
    [SerializeField] private Mesh ShellMesh;
    [SerializeField] private Shader ShellShader;
    [SerializeField] private bool LiveUpdate;

    [Header("Properties")]

    [SerializeField] private int ShellCount;

    [SerializeField] private int Density;

    [SerializeField] private float ShellExtent;

    [SerializeField] private Color ShellColor;

    [SerializeField] private Color ShellOcclusionColor;

    [Range(0, 1)]
    [SerializeField] private float DisplacementStrength;

    [Range(0, 1)]
    [SerializeField] private float NormalTension;

    [Header("Blade Settings")]

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

    [Header("Vectors")]
    [SerializeField] private Vector3 WindVector;
    [SerializeField] private Vector3 DisplacementVector;

    private GameObject[] ShellList;
    private MaterialPropertyBlock ShellProperties;
    private Material ShellMaterial;

    void Start()
    {
        ClearShells();

        ShellList = new GameObject[ShellCount];
        ShellProperties = new MaterialPropertyBlock();
        ShellMaterial = new Material(ShellShader);

        //Debug.Log("Generating Shells");

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
        }
    }

    void Update()
    {
        if (LiveUpdate)
        {
            UpdateShellValues();
        }
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
        ShellProperties.SetFloat("_DisplacementStrength", DisplacementStrength);
        ShellProperties.SetVector("_DisplacementVector", DisplacementVector);
        ShellProperties.SetFloat("_NormalTension", NormalTension);

        ShellProperties.SetFloat("_Curvature", Curvature);
        ShellProperties.SetFloat("_DistanceAttenuation", HeightAttenuation);
        ShellProperties.SetFloat("_JitterAmount", JitterAmount);
        ShellProperties.SetFloat("_WindStrength", WindStrength);

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
}
