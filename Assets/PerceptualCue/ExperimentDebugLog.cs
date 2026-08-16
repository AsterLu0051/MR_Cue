using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ExperimentDebugLog : MonoBehaviour
{
    [SerializeField]
    private TMP_Text logText;

    [SerializeField]
    private int maxLines = 12;

    private Queue<string> logLines = new Queue<string>();

    private void Start()
    {
        Log("Debug panel initialized.");
        Log("Quest experiment ready.");
    }

    public void Log(string message)
    {
        // 同时输出到普通 Unity Console
        Debug.Log(message);

        // 添加到 headset debug panel
        logLines.Enqueue(message);

        // 超过最大行数时删除最早的 log
        while (logLines.Count > maxLines)
        {
            logLines.Dequeue();
        }

        // 更新 UI
        logText.text = string.Join("\n", logLines);
    }

    public void Clear()
    {
        logLines.Clear();
        logText.text = "";
    }

    
}
