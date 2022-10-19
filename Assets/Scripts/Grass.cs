using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Grass : MonoBehaviour
{
    public Mesh grassMesh;
    public Material grassMaterial;
    public Terrain terrain;
    public Texture2D terrainHeightMap;
    public ComputeShader initializeGrassShader, cullGrassShader;
    private ComputeBuffer positionsBuffer, voteBuffer, scanBuffer, culledPositionsBuffer,
        groupSumsBuffer, scannedGroupSumsBuffer;
    private Camera cam;

    [Range(1, 20)]
    public int density = 1;
    public int fieldSize = 1;
    public int scale = 1;
    [Range(0f, 1000f)]
    public float cullDistance = 30;

    private int numInstances;
    private int numThreadGroups;
    private int numGroupScanThreadGroups;
    private Vector3[] positions;
    private List<List<GrassData>> batches = new List<List<GrassData>>();

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

    private struct GrassData
    {
        public Vector3 position;
        public Vector3 scale;
        public Quaternion rotation;
        public Matrix4x4 matrix
        {
            get
            {
                return Matrix4x4.TRS(position, rotation, scale);
            }
        }

        public GrassData(Vector3 pos, Vector3 s, Quaternion rot)
        {
            position = pos;
            scale = s;
            rotation = rot;
        }
    }

    private void PrintArray<T>(T[] arr)
    {
        for (int i = 0; i < arr.Length; i++)
            Debug.Log(arr[i]);
    }

    private int SizeOfWrapper<T>()
    {
        return System.Runtime.InteropServices.Marshal.SizeOf<T>();
    }

    private void Awake()
    {
        cam = Camera.main;
        terrain = GetComponent<Terrain>();
    }

    private void OnEnable()
    {
        fieldSize = Mathf.FloorToInt(terrain.terrainData.size.x) / 16;
        fieldSize *= density;
        positionsBuffer = new ComputeBuffer(fieldSize * fieldSize, 3 * sizeof(float));
        numInstances = positionsBuffer.count;
        voteBuffer = new ComputeBuffer(numInstances, sizeof(int));
        scanBuffer = new ComputeBuffer(numInstances, sizeof(int));
        culledPositionsBuffer = new ComputeBuffer(numInstances, 3 * sizeof(float));
        positions = new Vector3[numInstances];

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
        initializeGrassShader.SetFloat(yScaleID, terrain.terrainData.heightmapScale.y);
        initializeGrassShader.SetFloat(terrainSizeID, terrain.terrainData.size.x);
        initializeGrassShader.Dispatch(0, Mathf.CeilToInt(fieldSize / 8f), Mathf.CeilToInt(fieldSize / 8f), 1);
        positionsBuffer.GetData(positions);
        UpdatePositions(positions);
    }

    private void CullGrass()
    {
        Matrix4x4 V = cam.transform.worldToLocalMatrix;
        Matrix4x4 P = cam.projectionMatrix;
        Matrix4x4 VP = P * V;

        // Set global variables
        positionsBuffer.SetData(positions);
        cullGrassShader.SetMatrix(matrixVPID, VP);
        cullGrassShader.SetVector(cameraPositionID, cam.transform.position);
        cullGrassShader.SetFloat(distanceID, cullDistance);
        cullGrassShader.SetInt(numGroupsID, numThreadGroups);

        // Vote on positions
        cullGrassShader.SetBuffer(0, positionsBufferID, positionsBuffer);
        cullGrassShader.SetBuffer(0, voteBufferID, voteBuffer);
        cullGrassShader.Dispatch(0, numThreadGroups, 1, 1);
        int[] a = new int[positionsBuffer.count];
        voteBuffer.GetData(a);

        // Scan votes
        cullGrassShader.SetBuffer(1, voteBufferID, voteBuffer);
        cullGrassShader.SetBuffer(1, scanBufferID, scanBuffer);
        cullGrassShader.SetBuffer(1, groupSumsBufferID, groupSumsBuffer);
        cullGrassShader.Dispatch(1, numThreadGroups, 1, 1);
        int[] b = new int[positionsBuffer.count];
        scanBuffer.GetData(b);

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
        
        Vector3[] culledPositions = new Vector3[numInstances];
        culledPositionsBuffer.GetData(culledPositions);
        UpdatePositions(culledPositions);
    }

    private void UpdatePositions(Vector3[] updatedPositions)
    {
        for (int i = 0; i < updatedPositions.Length; i++)
        {
            int u = i / 1024;
            int v = i % 1024;
            GrassData temp = batches[u][v];
            temp.position = updatedPositions[i];
            batches[u][v] = temp;
        }
    }

    private void OnDisable()
    {
        positionsBuffer.Release();
        voteBuffer.Release();
        scanBuffer.Release();
        culledPositionsBuffer.Release();
        groupSumsBuffer.Release();
        scannedGroupSumsBuffer.Release();
        positionsBuffer = null;
        voteBuffer = null;
        scanBuffer = null;
        culledPositionsBuffer = null;
        groupSumsBuffer = null;
        scannedGroupSumsBuffer = null;
    }

    private bool Filter(Matrix4x4 m)
    {
        for (int i = 0; i < 16; i++)
            if (!float.IsFinite(m[i]) || float.IsNaN(m[i]))
            {
                // Debug.Log("Found");
                return true;
            }
        return false;
    }

    private void RenderBatches()
    {
        foreach (var batch in batches)
        {
            //List<Matrix4x4> matrix4X4s = batch.Select(a => a.matrix).ToList();
            //List<Matrix4x4> filter = new List<Matrix4x4>();
            //foreach (Matrix4x4 matrix in matrix4X4s)
            //{
            //    if (!Filter(matrix)) filter.Add(matrix);
            //}
            Graphics.DrawMeshInstanced(grassMesh, 0, grassMaterial, batch.Select(a => a.matrix).ToList());
        }
    }

    void Start()
    {
        int batchIndexNum = 0;
        List<GrassData> currentBatch = new List<GrassData>();
        for (int i = 0; i < numInstances; i++)
        {
            currentBatch.Add(new GrassData(Vector3.zero, Vector3.one * scale, Quaternion.Euler(0, 90, 90)));
            batchIndexNum++;
            if (batchIndexNum > 1023)
            {
                batches.Add(currentBatch);
                currentBatch = new List<GrassData>();
                batchIndexNum = 0;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // CullGrass();
        RenderBatches();
    }
}
