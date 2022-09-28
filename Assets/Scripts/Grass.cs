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
    private ComputeBuffer positionsBuffer, voteBuffer, scanBuffer, culledGrassPositionsBuffer;

    [Range(1, 20)]
    public int density = 1;
    public int fieldSize = 1;
    public int scale;
    [Range(0f, 1000f)]
    public float cullDistance;

    private int numInstances;
    private int numThreadGroups;
    private Vector3[] positions;
    private List<List<GrassData>> batches = new List<List<GrassData>>();

    // Initialize Grass Shader
    static readonly int positionsBufferID = Shader.PropertyToID("PositionsBuffer");
    static readonly int fieldSizeID = Shader.PropertyToID("FieldSize");
    static readonly int densityID = Shader.PropertyToID("Density");
    static readonly int yScaleID = Shader.PropertyToID("YScale");
    static readonly int terrainSizeID = Shader.PropertyToID("TerrainSize");
    static readonly int heightMapID = Shader.PropertyToID("_Heightmap");

    // Cull Grass Shader
    static readonly int voteBufferID = Shader.PropertyToID("VoteBuffer");
    static readonly int scanBufferID = Shader.PropertyToID("ScanBuffer");
    static readonly int culledGrassBufferID = Shader.PropertyToID("CulledGrassBuffer");
    static readonly int cameraPositionID = Shader.PropertyToID("CameraPosition");
    static readonly int distanceID = Shader.PropertyToID("Distacne");

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

    private void Awake()
    {
        terrain = GetComponent<Terrain>();
        fieldSize = Mathf.FloorToInt(terrain.terrainData.size.x) / 16;
        fieldSize *= density;
        positionsBuffer = new ComputeBuffer(fieldSize * fieldSize, 3 * sizeof(float));
        numInstances = positionsBuffer.count;
        voteBuffer = new ComputeBuffer(numInstances, sizeof(int));
        scanBuffer = new ComputeBuffer(numInstances, sizeof(int));
        culledGrassPositionsBuffer = new ComputeBuffer(numInstances, 3 * sizeof(float));
        positions = new Vector3[numInstances];
        numThreadGroups = Mathf.CeilToInt(numInstances / 128f);
    }

    private void OnEnable()
    {
        initializeGrassShader.SetBuffer(0, positionsBufferID, positionsBuffer);
        initializeGrassShader.SetTexture(0, heightMapID, terrainHeightMap);
        initializeGrassShader.SetInt(fieldSizeID, fieldSize);
        initializeGrassShader.SetInt(densityID, density);
        initializeGrassShader.SetFloat(yScaleID, terrain.terrainData.heightmapScale.y);
        initializeGrassShader.SetFloat(terrainSizeID, terrain.terrainData.size.x);
        initializeGrassShader.Dispatch(0, Mathf.CeilToInt(fieldSize / 8f), Mathf.CeilToInt(fieldSize / 8f), 1);
        // Debug.Log(System.Runtime.InteropServices.Marshal.SizeOf(typeof(GrassData)));
    }

    private void CullGrass()
    {
        cullGrassShader.SetBuffer(0, positionsBufferID, positionsBuffer);
        cullGrassShader.SetBuffer(0, voteBufferID, voteBuffer);
        cullGrassShader.SetVector(cameraPositionID, Camera.main.transform.position);
        cullGrassShader.SetFloat(distanceID, cullDistance);
        cullGrassShader.Dispatch(0, numThreadGroups, 1, 1);

        cullGrassShader.SetBuffer(1, voteBufferID, voteBuffer);
        cullGrassShader.SetBuffer(1, scanBufferID, scanBuffer);
        cullGrassShader.Dispatch(1, numThreadGroups, 1, 1);

        cullGrassShader.SetBuffer(2, positionsBufferID, positionsBuffer);
        cullGrassShader.SetBuffer(2, voteBufferID, voteBuffer);
        cullGrassShader.SetBuffer(2, scanBufferID, scanBuffer);
        cullGrassShader.SetBuffer(2, culledGrassBufferID, culledGrassPositionsBuffer);
        cullGrassShader.Dispatch(2, numThreadGroups, 1, 1);

        culledGrassPositionsBuffer.GetData(positions);
    }

    private void OnDisable()
    {
        positionsBuffer.Release();
        voteBuffer.Release();
        scanBuffer.Release();
        culledGrassPositionsBuffer.Release();
        positionsBuffer = null;
        voteBuffer = null;
        scanBuffer = null;
        culledGrassPositionsBuffer = null;
    }

    private void RenderBatches()
    {
        foreach (var batch in batches)
        {
            Graphics.DrawMeshInstanced(grassMesh, 0, grassMaterial, batch.Select(a => a.matrix).ToList());
        }
    }

    void Start()
    {
        int batchIdexNum = 0;
        List<GrassData> currentBatch = new List<GrassData>();
        for (int i = 0; i < positionsBuffer.count; i++)
        {
            AddObject(currentBatch, i);
            batchIdexNum++;
            if (batchIdexNum > 1023)
            {
                batches.Add(currentBatch);
                currentBatch = BuildNewBatch();
                batchIdexNum = 0;
            }
        }
        
    }

    private void AddObject(List<GrassData> batch, int i)
    {
        Vector3 pos = positions[i];
        batch.Add(new GrassData(pos, new Vector3(scale, scale, scale), Quaternion.Euler(0, 90, 90)));
    }
    
    private List<GrassData> BuildNewBatch()
    {
        return new List<GrassData>();
    }

    // Update is called once per frame
    void Update()
    {
        CullGrass();
        RenderBatches();
    }
}
