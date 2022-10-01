using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefixSums : MonoBehaviour
{
    public ComputeShader prefixSumsShader;
    ComputeBuffer inputBuffer;
    ComputeBuffer outputBuffer;
    [Range(1, 256)]
    public int size = 256;
    int[] dataArray;
    int[] outputArray;

    void Start()
    {
        dataArray = new int[size];
        outputArray = new int[size];
        for (int i = 0; i < size; i++)
        {
            dataArray[i] = Random.Range(0, 2);
        }
        inputBuffer = new ComputeBuffer(size, sizeof(int));
        outputBuffer = new ComputeBuffer(size, sizeof(int));
        inputBuffer.SetData(dataArray);
        prefixSumsShader.SetBuffer(1, "VoteBuffer", inputBuffer);
        prefixSumsShader.SetBuffer(1, "ScanBuffer", outputBuffer);
        int a = Mathf.CeilToInt(size / 128f);
        prefixSumsShader.Dispatch(1, a, 1, 1);
        outputBuffer.GetData(outputArray);
        inputBuffer.Release();
        inputBuffer = null;
        outputBuffer.Release();
        outputBuffer = null;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
