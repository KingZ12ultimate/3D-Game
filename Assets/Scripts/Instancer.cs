using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Instancer : MonoBehaviour
{
    ComputeBuffer inputBuffer, voteBuffer, scanBuffer, groupSumsBuffer, scannedGroupSumsBuffer, outputBuffer, counter;
    public ComputeShader cullShader;

    [SerializeField]
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
    static readonly int counterID = Shader.PropertyToID("Counter");
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
        outputBuffer = new ComputeBuffer(numInstances, sizeof(uint));
        counter = new ComputeBuffer(1, sizeof(uint));

        int[] zero = { 0 };
        counter.SetData(zero);

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
        groupSumsBuffer = new ComputeBuffer(numThreadGroups, sizeof(uint));
        scannedGroupSumsBuffer = new ComputeBuffer(numThreadGroups, sizeof(uint));
    }

    private void OnDisable()
    {
        inputBuffer?.Release();
        voteBuffer?.Release();
        scanBuffer?.Release();
        outputBuffer?.Release();
        counter?.Release();
        scannedGroupSumsBuffer?.Release();
        groupSumsBuffer?.Release();
        inputBuffer = null;
        voteBuffer = null;
        scanBuffer = null;
        outputBuffer = null;
        counter = null;
        scannedGroupSumsBuffer = null;
        groupSumsBuffer = null;
    }

    private void InintializeBuffers()
    {
        for (int i = 0; i < numInstances; i++)
            values[i] = 0;
        outputBuffer.SetData(values);
        for (int i = 0; i < numInstances; i++)
            values[i] = Random.Range(-100, 101);
        inputBuffer.SetData(values);
    }

    private void Start()
    {
        InintializeBuffers();

        #region Culling
        cullShader.SetBuffer(0, inputBufferID, inputBuffer);
        cullShader.SetBuffer(0, voteBufferID, voteBuffer);
        cullShader.Dispatch(0, numVoteThreadGroups, 1, 1);

        cullShader.SetBuffer(1, voteBufferID, voteBuffer);
        cullShader.SetBuffer(1, scanBufferID, scanBuffer);
        cullShader.SetBuffer(1, groupSumsBufferID, groupSumsBuffer);
        cullShader.Dispatch(1, numThreadGroups, 1, 1);

        cullShader.SetInt(numGroupsID, numThreadGroups);
        cullShader.SetBuffer(2, groupSumsBufferInID, groupSumsBuffer);
        cullShader.SetBuffer(2, groupSumsBufferOutID, scannedGroupSumsBuffer);
        cullShader.Dispatch(2, numGroupScanThreadGroups, 1, 1);

        cullShader.SetBuffer(3, inputBufferID, inputBuffer);
        cullShader.SetBuffer(3, voteBufferID, voteBuffer);
        cullShader.SetBuffer(3, scanBufferID, scanBuffer);
        cullShader.SetBuffer(3, groupSumsBufferID, scannedGroupSumsBuffer);
        cullShader.SetBuffer(3, counterID, counter);
        cullShader.SetBuffer(3, outputBufferID, outputBuffer);
        cullShader.Dispatch(3, numThreadGroups, 1, 1);
        #endregion

        uint[] count = new uint[1];
        counter.GetData(count);
        int[] output = new int[numInstances];
        outputBuffer.GetData(output);
    }

    // Update is called once per frame
    void Update()
    {

    }
}