using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Test : MonoBehaviour
{
    public Mesh grassMesh;
    public Material grassMaterial;
    public Terrain terrain;
    public RenderTexture terrainHeightMap;
    public ComputeShader initializeGrassShader;
    
    public int fieldSize = 1;
    [Range(1, 20)]
    public int density = 1;
    public int scale;
    private ComputeBuffer positionsBuffer;
    private Vector3[] positionsArray;
    private List<List<GrassData>> batches = new List<List<GrassData>>();

    static readonly int positionsBufferID = Shader.PropertyToID("PositionsBuffer");
    static readonly int fieldSizeID = Shader.PropertyToID("FieldSize");
    static readonly int densityID = Shader.PropertyToID("Density");
    static readonly int heightMapID = Shader.PropertyToID("HeightMap");
    static readonly int yScaleID = Shader.PropertyToID("YSCale");

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

    private void Awake()
    {
        terrain = GetComponent<Terrain>();
        terrainHeightMap = terrain.terrainData.heightmapTexture;
        fieldSize = Mathf.FloorToInt(terrain.terrainData.size.x) / 16;
        fieldSize *= density;
        positionsBuffer = new ComputeBuffer(fieldSize * fieldSize, 3 * sizeof(float));
        positionsArray = new Vector3[positionsBuffer.count];
    }

    private void OnEnable()
    {
        initializeGrassShader.SetBuffer(0, positionsBufferID, positionsBuffer);
        initializeGrassShader.SetInt(fieldSizeID, fieldSize);
        initializeGrassShader.SetInt(densityID, density);
        initializeGrassShader.SetFloat(yScaleID, terrain.terrainData.heightmapScale.y);
        initializeGrassShader.SetTexture(0, heightMapID, terrainHeightMap);
        initializeGrassShader.Dispatch(0, Mathf.CeilToInt(fieldSize / 8f), Mathf.CeilToInt(fieldSize / 8f), 1);
        positionsBuffer.GetData(positionsArray);
    }

    private void OnDisable()
    {
        positionsBuffer.Release();
        positionsBuffer = null;
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
        Vector3 pos = positionsArray[i];
        batch.Add(new GrassData(pos, new Vector3(scale, scale, scale), Quaternion.Euler(0, 90, 90)));
    }
    
    private List<GrassData> BuildNewBatch()
    {
        return new List<GrassData>();
    }

    // Update is called once per frame
    void Update()
    {
        RenderBatches();
    }
}
