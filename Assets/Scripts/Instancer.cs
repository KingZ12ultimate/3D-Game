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

    public ComputeShader initializeGrassShader;
    public Texture2D terrainHeightMap;

    [Range(0, 10000)]
    public int numInstances = 10;
    [Range(0, 100)]
    public float radius = 50;
    [Range (1, 5)]
    public int density = 1;
    [Range(0, 1024)]
    public int fieldSize = 300;

    #region Initialize Grass Shader IDs
    static readonly int positionsBufferID = Shader.PropertyToID("PositionsBuffer");
    static readonly int fieldSizeID = Shader.PropertyToID("FieldSize");
    static readonly int densityID = Shader.PropertyToID("Density");
    static readonly int yScaleID = Shader.PropertyToID("YScale");
    static readonly int terrainSizeID = Shader.PropertyToID("TerrainSize");
    static readonly int heightMapID = Shader.PropertyToID("_Heightmap");
    #endregion

    private void Awake()
    {
        commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
        fieldSize *= density;
    }

    private void OnEnable()
    {
        positionsBuffer = new ComputeBuffer(fieldSize * fieldSize, 4 * sizeof(float));
        commandBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);

        initializeGrassShader.SetBuffer(0, positionsBufferID, positionsBuffer);
        initializeGrassShader.SetTexture(0, heightMapID, terrainHeightMap);
        initializeGrassShader.SetInt(fieldSizeID, fieldSize);
        initializeGrassShader.SetInt(densityID, density);
        initializeGrassShader.SetFloat(yScaleID, 600);
        initializeGrassShader.SetFloat(terrainSizeID, 1024);
        initializeGrassShader.Dispatch(0, Mathf.CeilToInt(fieldSize / 8f), Mathf.CeilToInt(fieldSize / 8f), 1);
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
        RenderParams renderParams = new RenderParams(material);
        renderParams.worldBounds = new Bounds(Vector3.zero, Vector3.one * 1024);
        renderParams.matProps = new MaterialPropertyBlock();
        renderParams.matProps.SetBuffer("PositionsBuffer", positionsBuffer);
        renderParams.matProps.SetMatrix("Rotation", Matrix4x4.Rotate(Quaternion.Euler(0f, 90f, 90f)));
        commandData[0].instanceCount = (uint)positionsBuffer.count;
        commandData[0].indexCountPerInstance = mesh.GetIndexCount(0);
        commandBuffer.SetData(commandData);
        Graphics.RenderMeshIndirect(renderParams, mesh, commandBuffer);
    }
}