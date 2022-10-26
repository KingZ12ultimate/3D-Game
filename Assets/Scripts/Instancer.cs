using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Instancer : MonoBehaviour
{
    public Mesh mesh;
    public Material material;
    ComputeBuffer positionsBuffer, voteBuffer, scanBuffer, culledPositionsBuffer,
        groupSumsBuffer, scannedGroupSumsBuffer;
    GraphicsBuffer commandBuffer;
    GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;
    Camera cam;

    public ComputeShader initializeGrassShader;
    public ComputeShader cullGrassShader;
    public Texture2D terrainHeightMap;

    [Range (1, 5)]
    public int density = 1;
    [Range(0, 1024)]
    public int fieldSize = 300;
    [Range(0f, 1000f)]
    public float cullDistance = 30;

    private int numInstances;
    private int numThreadGroups;
    private int numGroupScanThreadGroups;

    #region Initialize Grass Shader IDs
    static readonly int positionsBufferID = Shader.PropertyToID("PositionsBuffer");
    static readonly int fieldSizeID = Shader.PropertyToID("FieldSize");
    static readonly int densityID = Shader.PropertyToID("Density");
    static readonly int yScaleID = Shader.PropertyToID("YScale");
    static readonly int terrainSizeID = Shader.PropertyToID("TerrainSize");
    static readonly int heightMapID = Shader.PropertyToID("_Heightmap");
    #endregion

    #region Cull Grass Shader IDs
    static readonly int voteBufferID = Shader.PropertyToID("VoteBuffer");
    static readonly int scanBufferID = Shader.PropertyToID("ScanBuffer");
    static readonly int groupSumsBufferID = Shader.PropertyToID("GroupSumsBuffer");
    static readonly int groupSumsInBufferID = Shader.PropertyToID("GroupSumsInBuffer");
    static readonly int groupSumsOutBufferID = Shader.PropertyToID("GroupSumsOutBuffer");
    static readonly int culledGrassBufferID = Shader.PropertyToID("CulledGrassBuffer");
    static readonly int matrixVPID = Shader.PropertyToID("MATRIX_VP");
    static readonly int cameraPositionID = Shader.PropertyToID("CameraPosition");
    static readonly int distanceID = Shader.PropertyToID("Distacne");
    static readonly int numGroupsID = Shader.PropertyToID("NumGroups");
    #endregion

    private void Awake()
    {
        commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
        fieldSize *= density;
        cam = Camera.main;
    }

    private void OnEnable()
    {
        positionsBuffer = new ComputeBuffer(fieldSize * fieldSize, 4 * sizeof(float));
        commandBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);

        numInstances = positionsBuffer.count;
        voteBuffer = new ComputeBuffer(numInstances, sizeof(int));
        scanBuffer = new ComputeBuffer(numInstances, sizeof(int));
        culledPositionsBuffer = new ComputeBuffer(numInstances, 4 * sizeof(float));

        numThreadGroups = Mathf.CeilToInt(numInstances / 128f);
        if (numThreadGroups > 128)
        {
            int powerOfTwo = 128;
            while (powerOfTwo < numThreadGroups)
                powerOfTwo *= 2;
            numThreadGroups = powerOfTwo;
        }
        else
        {
            while (128 % numThreadGroups != 0)
                numThreadGroups++;
        }

        numGroupScanThreadGroups = Mathf.CeilToInt(numInstances / 1024f);
        groupSumsBuffer = new ComputeBuffer(numThreadGroups, sizeof(int));
        scannedGroupSumsBuffer = new ComputeBuffer(numThreadGroups, sizeof(int));

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
        voteBuffer?.Release();
        scanBuffer?.Release();
        culledPositionsBuffer?.Release();
        groupSumsBuffer?.Release();
        scannedGroupSumsBuffer?.Release();
        commandBuffer?.Release();
        positionsBuffer = null;
        voteBuffer = null;
        scanBuffer = null;
        culledPositionsBuffer = null;
        groupSumsBuffer = null;
        scannedGroupSumsBuffer = null;
        commandBuffer = null;
    }

    private void CullGrass()
    {
        Matrix4x4 V = cam.transform.worldToLocalMatrix;
        Matrix4x4 P = cam.projectionMatrix;
        Matrix4x4 VP = P * V;

        // Set global variables
        cullGrassShader.SetMatrix(matrixVPID, VP);
        cullGrassShader.SetVector(cameraPositionID, cam.transform.position);
        cullGrassShader.SetFloat(distanceID, cullDistance);
        cullGrassShader.SetInt(numGroupsID, numThreadGroups);

        // Vote on positions
        cullGrassShader.SetBuffer(0, positionsBufferID, positionsBuffer);
        cullGrassShader.SetBuffer(0, voteBufferID, voteBuffer);
        cullGrassShader.Dispatch(0, numThreadGroups, 1, 1);

        // Scan votes
        cullGrassShader.SetBuffer(1, voteBufferID, voteBuffer);
        cullGrassShader.SetBuffer(1, scanBufferID, scanBuffer);
        cullGrassShader.SetBuffer(1, groupSumsBufferID, groupSumsBuffer);
        cullGrassShader.Dispatch(1, numThreadGroups, 1, 1);

        // Scan group sums
        cullGrassShader.SetBuffer(2, groupSumsInBufferID, groupSumsBuffer);
        cullGrassShader.SetBuffer(2, groupSumsOutBufferID, scannedGroupSumsBuffer);
        cullGrassShader.Dispatch(2, numGroupScanThreadGroups, 1, 1);

        // Compact
        cullGrassShader.SetBuffer(3, positionsBufferID, positionsBuffer);
        cullGrassShader.SetBuffer(3, voteBufferID, voteBuffer);
        cullGrassShader.SetBuffer(3, scanBufferID, scanBuffer);
        cullGrassShader.SetBuffer(3, groupSumsBufferID, scannedGroupSumsBuffer);
        cullGrassShader.SetBuffer(3, culledGrassBufferID, culledPositionsBuffer);
        cullGrassShader.Dispatch(3, numThreadGroups, 1, 1);
    }

    // Update is called once per frame
    void Update()
    {
        // CullGrass();

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