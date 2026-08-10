using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CubeExperimentManager : MonoBehaviour
{
    [Header("Experiment Cubes")]
    [Tooltip("把场景中的九个 Cube 拖到这里")]
    [SerializeField]
    private List<ExperimentCube> experimentCubes
        = new List<ExperimentCube>();

    [Header("Materials")]
    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Material highlightMaterial;

    [Header("UI")]
    [SerializeField] private TMP_Text experimentText;

    [Header("Experiment Settings")]
    [Min(1)]
    [SerializeField] private int totalTrials = 20;

    [Tooltip("正确触碰后，下一次任务开始前的等待时间")]
    [Min(0f)]
    [SerializeField] private float intervalBetweenTrials = 1.0f;

    [Tooltip("实验开始后是否自动开始第一次 Trial")]
    [SerializeField] private bool startAutomatically = true;

    [Tooltip("允许触发 Cube 的物体 Layer")]
    [SerializeField] private LayerMask validTouchLayers;

    private ExperimentCube currentTarget;
    private int previousTargetIndex = -1;

    private int currentTrialNumber;
    private int totalErrors;
    private int currentTrialErrors;

    private float trialStartTime;
    private bool trialActive;
    private bool experimentRunning;

    private readonly List<TrialResult> trialResults
        = new List<TrialResult>();

    private void Start()
    {
        if (!ValidateSetup())
        {
            enabled = false;
            return;
        }

        InitializeCubes();

        if (startAutomatically)
        {
            StartExperiment();
        }
        else
        {
            SetText("准备完毕\n等待实验开始");
        }
    }

    private bool ValidateSetup()
    {
        if (experimentCubes == null || experimentCubes.Count == 0)
        {
            Debug.LogError("ExperimentManager 中没有添加任何 Cube。");
            return false;
        }

        if (defaultMaterial == null || highlightMaterial == null)
        {
            Debug.LogError("请指定 Default Material 和 Highlight Material。");
            return false;
        }

        return true;
    }

    private void InitializeCubes()
    {
        foreach (ExperimentCube cube in experimentCubes)
        {
            if (cube == null)
            {
                continue;
            }

            cube.Initialize(
                this,
                defaultMaterial,
                highlightMaterial
            );
        }
    }

    /// <summary>
    /// 开始整组实验。
    /// 之后可以把这个方法绑定到 UI Button 或 Controller 按键。
    /// </summary>
    public void StartExperiment()
    {
        if (experimentRunning)
        {
            return;
        }

        StopAllCoroutines();

        currentTrialNumber = 0;
        totalErrors = 0;
        currentTrialErrors = 0;
        previousTargetIndex = -1;

        trialResults.Clear();

        experimentRunning = true;
        trialActive = false;

        ResetAllCubes();

        StartCoroutine(StartNextTrialAfterDelay(0f));
    }

    private IEnumerator StartNextTrialAfterDelay(float delay)
    {
        trialActive = false;
        ResetAllCubes();

        SetText(
            $"Trial {currentTrialNumber + 1}/{totalTrials}\n" +
            "准备"
        );

        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        StartNextTrial();
    }

    private void StartNextTrial()
    {
        if (!experimentRunning)
        {
            return;
        }

        if (currentTrialNumber >= totalTrials)
        {
            FinishExperiment();
            return;
        }

        currentTrialNumber++;
        currentTrialErrors = 0;

        int targetIndex = GetRandomTargetIndex();

        previousTargetIndex = targetIndex;
        currentTarget = experimentCubes[targetIndex];

        ResetAllCubes();
        currentTarget.SetAsTarget(true);

        // 在目标正式亮起后开始计时
        trialStartTime = Time.realtimeSinceStartup;
        trialActive = true;

        SetText(
            $"Trial {currentTrialNumber}/{totalTrials}\n" +
            "触碰亮起的 Cube"
        );

        Debug.Log(
            $"Trial {currentTrialNumber} 开始，目标：{currentTarget.name}"
        );
    }

    private int GetRandomTargetIndex()
    {
        if (experimentCubes.Count == 1)
        {
            return 0;
        }

        int randomIndex;

        // 避免连续两次选择同一个 Cube
        do
        {
            randomIndex = Random.Range(0, experimentCubes.Count);
        }
        while (randomIndex == previousTargetIndex);

        return randomIndex;
    }

    /// <summary>
    /// 由 ExperimentCube 在检测到触碰时调用。
    /// </summary>
    public void RegisterCubeTouch(ExperimentCube touchedCube)
    {
        if (!experimentRunning || !trialActive)
        {
            return;
        }

        if (touchedCube == null)
        {
            return;
        }

        if (touchedCube == currentTarget)
        {
            RegisterCorrectTouch();
        }
        else
        {
            RegisterWrongTouch(touchedCube);
        }
    }

    private void RegisterCorrectTouch()
    {
        // 立刻锁定 Trial，避免同一次触碰被重复记录
        trialActive = false;

        float reactionTime =
            Time.realtimeSinceStartup - trialStartTime;

        TrialResult result = new TrialResult
        {
            trialNumber = currentTrialNumber,
            targetName = currentTarget.name,
            reactionTime = reactionTime,
            errorCount = currentTrialErrors
        };

        trialResults.Add(result);

        currentTarget.SetAsTarget(false);

        Debug.Log(
            $"Trial {currentTrialNumber} 正确。" +
            $"反应时间：{reactionTime:F3} 秒，" +
            $"错误次数：{currentTrialErrors}"
        );

        SetText(
            $"正确\n" +
            $"用时：{reactionTime:F3} 秒\n" +
            $"本次错误：{currentTrialErrors}"
        );

        StartCoroutine(
            StartNextTrialAfterDelay(intervalBetweenTrials)
        );
    }

    private void RegisterWrongTouch(ExperimentCube touchedCube)
    {
        currentTrialErrors++;
        totalErrors++;

        Debug.Log(
            $"错误触碰：{touchedCube.name}。" +
            $"当前 Trial 错误：{currentTrialErrors}"
        );

        SetText(
            $"Trial {currentTrialNumber}/{totalTrials}\n" +
            $"错误：{currentTrialErrors}\n" +
            "请触碰亮起的 Cube"
        );
    }

    private void FinishExperiment()
    {
        experimentRunning = false;
        trialActive = false;

        ResetAllCubes();

        float averageReactionTime = CalculateAverageReactionTime();

        SetText(
            "实验结束\n" +
            $"平均反应时间：{averageReactionTime:F3} 秒\n" +
            $"总错误次数：{totalErrors}"
        );

        Debug.Log("========== 实验结果 ==========");

        foreach (TrialResult result in trialResults)
        {
            Debug.Log(
                $"Trial {result.trialNumber}, " +
                $"Target: {result.targetName}, " +
                $"Reaction Time: {result.reactionTime:F3}, " +
                $"Errors: {result.errorCount}"
            );
        }

        Debug.Log(
            $"平均反应时间：{averageReactionTime:F3} 秒，" +
            $"总错误次数：{totalErrors}"
        );
    }

    private float CalculateAverageReactionTime()
    {
        if (trialResults.Count == 0)
        {
            return 0f;
        }

        float totalTime = 0f;

        foreach (TrialResult result in trialResults)
        {
            totalTime += result.reactionTime;
        }

        return totalTime / trialResults.Count;
    }

    private void ResetAllCubes()
    {
        foreach (ExperimentCube cube in experimentCubes)
        {
            if (cube != null)
            {
                cube.SetAsTarget(false);
            }
        }

    }

    public bool IsValidTouchObject(GameObject touchObject)
    {
        int objectLayerMask = 1 << touchObject.layer;

        return (validTouchLayers.value & objectLayerMask) != 0;
    }

    private void SetText(string message)
    {
        if (experimentText != null)
        {
            experimentText.text = message;
        }
    }

    [System.Serializable]
    private class TrialResult
    {
        public int trialNumber;
        public string targetName;
        public float reactionTime;
        public int errorCount;
    }
}
