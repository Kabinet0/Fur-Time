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

            ShellProperties.Clear();
            ShellProperties.SetInt("_ShellIndex", i);
            ShellProperties.SetInt("_ShellCount", ShellCount);
            ShellProperties.SetFloat("_ShellExtent", ShellExtent);
            ShellProperties.SetInt("_Density", Density);
            ShellProperties.SetVector("_ShellColor", ShellColor.linear);
            ShellProperties.SetVector("_ShellOcclusionColor", ShellOcclusionColor.linear);

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

    void UpdateShellValues()
    {
        for (int i = 0; i < ShellCount; i++)
        {
            ShellProperties.Clear();
            ShellProperties.SetInt("_ShellIndex", i);
            ShellProperties.SetInt("_ShellCount", ShellCount);
            ShellProperties.SetFloat("_ShellExtent", ShellExtent);
            ShellProperties.SetInt("_Density", Density);
            ShellProperties.SetVector("_ShellColor", ShellColor.linear);
            ShellProperties.SetVector("_ShellOcclusionColor", ShellOcclusionColor.linear);

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
