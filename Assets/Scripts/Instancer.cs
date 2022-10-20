using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Instancer : MonoBehaviour
{
    public Mesh mesh;
    public Material material;
    ComputeBuffer positionsBuffer;
    GraphicsBuffer commandBuffer;
    GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;

    [Range(0, 10000)]
    public int numInstances = 10;
    [Range(0, 100)]
    public float radius = 50;

    private void Awake()
    {
        commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
    }

    private void OnEnable()
    {
        positionsBuffer = new ComputeBuffer(numInstances, 4 * sizeof(float));
        commandBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
    }

    private void OnDisable()
    {
        positionsBuffer?.Release();
        positionsBuffer = null;

        commandBuffer?.Release();
        commandBuffer = null;
    }

    private void UpdatePositions()
    {
        Vector4[] positions = new Vector4[numInstances];
        for (int i = 0; i < numInstances; i++)
        {
            Vector3 pos = Random.insideUnitSphere * radius;
            positions[i] = new Vector4(pos.x, pos.y, pos.z, 1f);
        }
        positionsBuffer.Release();
        positionsBuffer = new ComputeBuffer(numInstances, 4 * sizeof(float));
        positionsBuffer.SetData(positions);
    }

    // Update is called once per frame
    void Update()
    {
        UpdatePositions();
        RenderParams renderParams = new RenderParams(material);
        renderParams.worldBounds = new Bounds(Vector3.zero, 1000 * Vector3.one);
        renderParams.matProps = new MaterialPropertyBlock();
        renderParams.matProps.SetBuffer("PositionsBuffer", positionsBuffer);
        renderParams.matProps.SetMatrix("_ObjectToWorld", Matrix4x4.Translate(new Vector3(-4.5f, 0, 0)));
        commandData[0].instanceCount = (uint)numInstances;
        commandData[0].indexCountPerInstance = mesh.GetIndexCount(0);
        commandBuffer.SetData(commandData);
        Graphics.RenderMeshIndirect(renderParams, mesh, commandBuffer);
    }
}