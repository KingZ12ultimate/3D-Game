using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grass : MonoBehaviour
{
    public Mesh grassMesh;
    public Material grassMaterial;
    public Terrain terrain;
    public Texture2D terrainHeightMap;
    public ComputeShader initializeGrassShader, cullGrassShader;
    private ComputeBuffer positionsBuffer, voteBuffer, scanBuffer, culledPositionsBuffer,
        groupSumsBuffer, scannedGroupSumsBuffer, grassCountbuffer;
    private Camera cam;
    private Bounds fieldBounds;
    private GrassChunk[] chunks;

    private GraphicsBuffer commandBuffer;
    private GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;

    [SerializeField, Range(1, 20)]
    private int density = 1;
    [SerializeField, Range(1, 1024)]
    private int fieldSize = 1;
    [SerializeField, Range(1, 20)]
    private int numChunks = 1;
    [SerializeField, Range(0f, 1000f)]
    private float cullDistance = 30;
    [SerializeField, Range(1, 10)]
    private int chunkDensity;
    private int chunkSize;

    private int numInstances, numInstancesPerChunk, numThreadGroups, numVoteThreadGroups, numGroupScanThreadGroups;

    #region Initialize Grass Shader IDs
    static readonly int positionsBufferID = Shader.PropertyToID("PositionsBuffer");
    static readonly int fieldSizeID = Shader.PropertyToID("FieldSize");
    static readonly int densityID = Shader.PropertyToID("Density");
    static readonly int numChunksID = Shader.PropertyToID("NumChunks");
    static readonly int chunkSizeID = Shader.PropertyToID("ChunkSize");
    static readonly int xOffsetID = Shader.PropertyToID("XOffset");
    static readonly int yOffsetID = Shader.PropertyToID("YOffset");
    static readonly int yScaleID = Shader.PropertyToID("YScale");
    static readonly int terrainSizeID = Shader.PropertyToID("TerrainSize");
    static readonly int heightMapID = Shader.PropertyToID("_Heightmap");
    #endregion

    #region Cull Grass Shader IDs
    static readonly int voteBufferID = Shader.PropertyToID("VoteBuffer");
    static readonly int scanBufferID = Shader.PropertyToID("ScanBuffer");
    static readonly int groupSumsBufferID = Shader.PropertyToID("GroupSumsBuffer");
    static readonly int groupSumsBufferInID = Shader.PropertyToID("GroupSumsBufferIn");
    static readonly int groupSumsBufferOutID = Shader.PropertyToID("GroupSumsBufferOut");
    static readonly int culledGrassBufferID = Shader.PropertyToID("CulledGrassBuffer");
    static readonly int grassCountBufferID = Shader.PropertyToID("GrassCounter");
    static readonly int matrixVPID = Shader.PropertyToID("MATRIX_VP");
    static readonly int cameraPositionID = Shader.PropertyToID("CameraPosition");
    static readonly int distanceID = Shader.PropertyToID("Distacne");
    static readonly int numGroupsID = Shader.PropertyToID("NumGroups");
    #endregion

    private void PrintArray<T>(T[] arr)
    {
        for (int i = 0; i < arr.Length; i++)
            Debug.Log(arr[i]);
    }

    public struct GrassChunk
    {
        public ComputeBuffer positionsBuffer;
        public ComputeBuffer culledPositionsBuffer;
        public ComputeBuffer grassCounterBuffer;
        public GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;
        public Bounds bounds;
        public Material material;
    }

    private void Awake()
    {
        cam = Camera.main;
        terrain = GetComponent<Terrain>();
        commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
    }

    private void OnEnable()
    {
        fieldBounds = new Bounds(Vector3.zero, Vector3.one * 1024);
        numInstancesPerChunk = Mathf.CeilToInt(fieldSize / numChunks) * chunkDensity;
        chunkSize = numInstancesPerChunk;
        numInstancesPerChunk *= numInstancesPerChunk;
        commandBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);

        numInstances = positionsBuffer.count;
        voteBuffer = new ComputeBuffer(numInstances, sizeof(int));
        scanBuffer = new ComputeBuffer(numInstances, sizeof(int));
        culledPositionsBuffer = new ComputeBuffer(numInstances, 4 * sizeof(float));
        grassCountbuffer = new ComputeBuffer(1, sizeof(int));

        ResetGrassCount();

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
        numVoteThreadGroups = Mathf.CeilToInt(numInstances / 128f);
        numGroupScanThreadGroups = Mathf.CeilToInt(numInstances / 1024f);
        groupSumsBuffer = new ComputeBuffer(numThreadGroups, sizeof(int));
        scannedGroupSumsBuffer = new ComputeBuffer(numThreadGroups, sizeof(int));

        initializeGrassShader.SetTexture(0, heightMapID, terrainHeightMap);
        initializeGrassShader.SetInt(fieldSizeID, fieldSize);
        initializeGrassShader.SetInt(numChunksID, numChunks);
        initializeGrassShader.SetInt(chunkSizeID, chunkSize);
        initializeGrassShader.SetInt(densityID, density);
        initializeGrassShader.SetFloat(yScaleID, terrain.terrainData.heightmapScale.y);
        initializeGrassShader.SetFloat(terrainSizeID, terrain.terrainData.size.x);

        InitializeChunks();
    }

    private void InitializeChunks()
    {
        chunks = new GrassChunk[numChunks * numChunks];
        for (int x = 0; x < numChunks; x++)
        {
            for (int y = 0; y < numChunks; y++)
            {
                chunks[x + y * numChunks] = InitializeChunk(x, y);
            }
        }
    }

    private GrassChunk InitializeChunk(int x, int y)
    {
        GrassChunk chunk = new GrassChunk();
        chunk.positionsBuffer = new ComputeBuffer(numInstancesPerChunk, 4 * sizeof(float));
        chunk.culledPositionsBuffer = new ComputeBuffer(numInstancesPerChunk, 4 * sizeof(float));
        chunk.grassCounterBuffer = new ComputeBuffer(1, sizeof(int));
        int chunkDim = Mathf.CeilToInt(fieldSize / numChunks);

        Vector3 center = new Vector3(chunkDim * x, 0.0f, chunkDim * y);
        chunk.bounds = new Bounds(center, new Vector3(-chunkDim, 700, chunkDim));

        return chunk;
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
        cullGrassShader.Dispatch(0, numVoteThreadGroups, 1, 1);

        //Scan votes
        cullGrassShader.SetBuffer(1, voteBufferID, voteBuffer);
        cullGrassShader.SetBuffer(1, scanBufferID, scanBuffer);
        cullGrassShader.SetBuffer(1, groupSumsBufferID, groupSumsBuffer);
        cullGrassShader.Dispatch(1, numThreadGroups, 1, 1);

        //Scan group sums
        cullGrassShader.SetBuffer(2, groupSumsBufferInID, groupSumsBuffer);
        cullGrassShader.SetBuffer(2, groupSumsBufferOutID, scannedGroupSumsBuffer);
        cullGrassShader.Dispatch(2, numGroupScanThreadGroups, 1, 1);

        //Compact
        cullGrassShader.SetBuffer(3, positionsBufferID, positionsBuffer);
        cullGrassShader.SetBuffer(3, voteBufferID, voteBuffer);
        cullGrassShader.SetBuffer(3, scanBufferID, scanBuffer);
        cullGrassShader.SetBuffer(3, groupSumsBufferID, scannedGroupSumsBuffer);
        cullGrassShader.SetBuffer(3, culledGrassBufferID, culledPositionsBuffer);
        cullGrassShader.SetBuffer(3, grassCountBufferID, grassCountbuffer);
        cullGrassShader.Dispatch(3, numThreadGroups, 1, 1);
    }

    private void OnDisable()
    {
        positionsBuffer?.Release();
        voteBuffer?.Release();
        scanBuffer?.Release();
        culledPositionsBuffer?.Release();
        groupSumsBuffer?.Release();
        scannedGroupSumsBuffer?.Release();
        grassCountbuffer?.Release();
        commandBuffer?.Release();
        positionsBuffer = null;
        voteBuffer = null;
        scanBuffer = null;
        culledPositionsBuffer = null;
        groupSumsBuffer = null;
        scannedGroupSumsBuffer = null;
        grassCountbuffer = null;
        commandBuffer = null;

        fieldSize /= density;
    }

    #region Grass Count Methods
    private void SetGrassCount()
    {
        int[] count = new int[1];
        grassCountbuffer.GetData(count);
        commandData[0].instanceCount = (uint)count[0];
    }

    private void ResetGrassCount()
    {
        int[] zero = { 0 };
        grassCountbuffer.SetData(zero);
    }
    #endregion

    // Update is called once per frame
    void Update()
    {
        CullGrass();

        RenderParams renderParams = new RenderParams(grassMaterial);
        renderParams.worldBounds = fieldBounds;
        renderParams.matProps = new MaterialPropertyBlock();
        renderParams.matProps.SetBuffer("PositionsBuffer", culledPositionsBuffer);
        renderParams.matProps.SetMatrix("Rotation", Matrix4x4.Rotate(Quaternion.Euler(0f, 90f, 90f)));
        SetGrassCount();
        commandData[0].indexCountPerInstance = grassMesh.GetIndexCount(0);
        commandBuffer.SetData(commandData);
        Graphics.RenderMeshIndirect(renderParams, grassMesh, commandBuffer);
        ResetGrassCount();
    }
}
