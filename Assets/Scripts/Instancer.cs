using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Instancer : MonoBehaviour
{
    ComputeBuffer intBuffer, voteBuffer, scanBuffer, culledIntBuffer,
        groupSumsBuffer, scannedGroupSumsBuffer;
    public ComputeShader cullGrassShader;

    [SerializeField, Range(1, 1000)]
    private int numInstances = 8;

    private int numThreadGroups;
    private int numGroupScanThreadGroups;

    private int[] values;

    #region Cull Grass Shader IDs
    static readonly int intBufferID = Shader.PropertyToID("IntBuffer");
    static readonly int voteBufferID = Shader.PropertyToID("VoteBuffer");
    static readonly int scanBufferID = Shader.PropertyToID("ScanBuffer");
    static readonly int groupSumsBufferID = Shader.PropertyToID("GroupSumsBuffer");
    static readonly int groupSumsInBufferID = Shader.PropertyToID("GroupSumsInBuffer");
    static readonly int groupSumsOutBufferID = Shader.PropertyToID("GroupSumsOutBuffer");
    static readonly int culledIntBufferID = Shader.PropertyToID("CulledIntBuffer");
    static readonly int numGroupsID = Shader.PropertyToID("NumGroups");
    #endregion

    private void Awake()
    {
        values = new int[numInstances];
        for (int i = 0; i < numInstances; i++)
        {
            values[i] = Random.Range(-10, 11);
        }
    }

    private void OnEnable()
    {
        intBuffer = new ComputeBuffer(numInstances, sizeof(int));
        voteBuffer = new ComputeBuffer(numInstances, sizeof(int));
        culledIntBuffer = new ComputeBuffer(numInstances, sizeof(int));
        numThreadGroups = Mathf.CeilToInt(numInstances / 128f);

        CullGrass();

        int[] cVals = new int[numInstances];
        culledIntBuffer.GetData(cVals);
        for (int i = 0; i < numInstances; i++)
        {
            if (cVals[i] >= 10)
            {
                Debug.Log("Error!");
            }
        }
    }

    private void OnDisable()
    {
        intBuffer?.Release();
        voteBuffer?.Release();
        scanBuffer?.Release();
        culledIntBuffer?.Release();
        groupSumsBuffer?.Release();
        scannedGroupSumsBuffer?.Release();
        intBuffer = null;
        voteBuffer = null;
        scanBuffer = null;
        culledIntBuffer = null;
        groupSumsBuffer = null;
        scannedGroupSumsBuffer = null;
    }

    private void CullGrass()
    {
        // Set global variables
        cullGrassShader.SetInt(numGroupsID, numThreadGroups);

        // Vote on positions
        cullGrassShader.SetBuffer(0, intBufferID, intBuffer);
        cullGrassShader.SetBuffer(0, voteBufferID, voteBuffer);
        cullGrassShader.Dispatch(0, numThreadGroups, 1, 1);

        int[] v = new int[voteBuffer.count];
        voteBuffer.GetData(v);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}