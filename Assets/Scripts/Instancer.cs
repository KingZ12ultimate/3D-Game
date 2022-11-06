using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Instancer : MonoBehaviour
{
    ComputeBuffer inputBuffer, voteBuffer, scanBuffer, groupSumsBuffer, scannedGroupSumsBuffer, outputBuffer;
    public ComputeShader cullShader;

    [SerializeField, Range(1, 1000)]
    private int numInstances = 8;

    private int[] values;

    private int numThreadGroups, numVoteThreadGroups, numGroupScanThreadGroups;

    #region Cull Grass Shader IDs
    static readonly int inputBufferID = Shader.PropertyToID("Input");
    static readonly int voteBufferID = Shader.PropertyToID("VoteBuffer");
    static readonly int scanBufferID = Shader.PropertyToID("ScanBuffer");
    static readonly int groupSumsBufferID = Shader.PropertyToID("GroupSumsBuffer");
    static readonly int groupSumsBufferInID = Shader.PropertyToID("GroupSumsBufferIn");
    static readonly int groupSumsBufferOutID = Shader.PropertyToID("GroupSumsBufferOut");
    static readonly int outputBufferID = Shader.PropertyToID("Output");
    static readonly int numGroupsID = Shader.PropertyToID("NumGroups");
    #endregion

    private void Awake()
    {
        values = new int[numInstances];
    }

    private void OnEnable()
    {
        inputBuffer = new ComputeBuffer(numInstances, sizeof(int));
        voteBuffer = new ComputeBuffer(numInstances, sizeof(uint));
        scanBuffer = new ComputeBuffer(numInstances, sizeof(uint));

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

        groupSumsBuffer = new ComputeBuffer(numGroupScanThreadGroups, sizeof(uint));
        scannedGroupSumsBuffer = new ComputeBuffer(numInstances, sizeof(uint));
        voteBuffer = new ComputeBuffer(numInstances, sizeof(uint));

    }

    private void OnDisable()
    {
        inputBuffer?.Release();
        voteBuffer?.Release();
        inputBuffer = null;
        voteBuffer = null;
    }

    private void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < numInstances; i++)
            values[i] = Random.Range(-10, 11);
        inputBuffer.SetData(values);
        cullShader.SetBuffer(0, inputBufferID, inputBuffer);
        cullShader.SetBuffer(0, outputBufferID, voteBuffer);
        cullShader.Dispatch(0, 1, 1, 1);
    }
}