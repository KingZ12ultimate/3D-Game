using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefixSums : MonoBehaviour
{
    public ComputeShader prefixSumsShader;
    ComputeBuffer inputBuffer;
    ComputeBuffer outputBuffer;
    [Range(1, 256)]
    public int size = 1;
    int[] dataArray;

    void Start()
    {
        dataArray = new int[size];
        for (int i = 0; i < size; i++)
        {
            dataArray[i] = Random.Range(0, 10);
            Debug.Log(dataArray[i]);
        }
        inputBuffer = new ComputeBuffer(size, sizeof(int));
        outputBuffer = new ComputeBuffer(size, sizeof(int));
        inputBuffer.SetData(dataArray);
        prefixSumsShader.SetBuffer(0, "Input", inputBuffer);
        prefixSumsShader.SetBuffer(0, "Output", outputBuffer);
        prefixSumsShader.Dispatch(0, Mathf.CeilToInt(size / 128f), 1, 1);
        for (int i = 1; i < size; i++)
        {
            dataArray[i] += dataArray[i - 1];
        }
        for (int i = 0; i < size; i++)
        {
            Debug.Log(dataArray[i]);
        }
        outputBuffer.GetData(dataArray);
        for (int i = 0; i < size; i++)
        {
            Debug.Log(dataArray[i]);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
