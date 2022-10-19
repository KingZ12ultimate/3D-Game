using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Mesh)), RequireComponent(typeof(Material))]
public class Instancer : MonoBehaviour
{
    [SerializeField]
    Mesh mesh;
    [SerializeField]
    Material material;
    List<List<Matrix4x4>> matrices = new List<List<Matrix4x4>>();
    ComputeBuffer argsBuffer;
    ComputeBuffer positionsBuffer;
    ComputeShader positionsShader;
    
    [Range(0, 10000)]
    public int numInstances;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

    private void OnEnable()
    {
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        positionsBuffer = new ComputeBuffer(numInstances, 4 * sizeof(int));
        Vector4[] positions = new Vector4[numInstances];
        for (int i = 0; i < numInstances; i++)
        {
            float angle = Random.Range(0.0f, Mathf.PI * 2.0f);
            float distance = Random.Range(20.0f, 100.0f);
            float height = Random.Range(-2.0f, 2.0f);
            float size = Random.Range(0.05f, 0.25f);
            positions[i] = new Vector4(Mathf.Sin(angle) * distance, height, Mathf.Cos(angle) * distance, size);
        }
        positionsBuffer.SetData(positions);
        material.SetBuffer("PositionsBuffer", positionsBuffer);

        args[0] = (uint)mesh.GetIndexCount(0);
        args[1] = (uint)numInstances;
        args[2] = (uint)mesh.GetIndexStart(0);
        args[3] = (uint)mesh.GetBaseVertex(0);

        argsBuffer.SetData(args);
    }

    private void OnDisable()
    {
        if (positionsBuffer != null)
            positionsBuffer.Release();
        positionsBuffer = null;

        if (argsBuffer != null)
            argsBuffer.Release();
        argsBuffer = null;
    }

    // Update is called once per frame
    void Update()
    {
        // Graphics.DrawMeshInstancedIndirect(mesh, 0, material, )
    }
}
