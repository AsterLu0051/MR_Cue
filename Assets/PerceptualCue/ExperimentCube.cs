using UnityEngine;

public class ExperimentCube : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Renderer cubeRenderer;

    private CubeExperimentManager experimentManager;
    private Material defaultMaterial;
    private Material highlightMaterial;

    public bool IsCurrentTarget { get; private set; }

    private void Awake()
    {
        // 如果 Inspector 没有指定 Renderer，自动获取
        if (cubeRenderer == null)
        {
            cubeRenderer = GetComponent<Renderer>();
        }
    }

    /// <summary>
    /// 由 ExperimentManager 在实验开始时初始化。
    /// </summary>
    public void Initialize(
        CubeExperimentManager manager,
        Material normalMaterial,
        Material targetMaterial)
    {
        experimentManager = manager;
        defaultMaterial = normalMaterial;
        highlightMaterial = targetMaterial;

        SetAsTarget(false);
    }

    /// <summary>
    /// 设置该 Cube 是否为当前目标。
    /// </summary>
    public void SetAsTarget(bool isTarget)
    {
        IsCurrentTarget = isTarget;

        if (cubeRenderer == null)
        {
            Debug.LogError($"{name} 没有 Renderer。");
            return;
        }

        cubeRenderer.material = isTarget
            ? highlightMaterial
            : defaultMaterial;
    }

    /// <summary>
    /// 物理 Collider 进入 Cube 的 Trigger 时调用。
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (experimentManager == null)
        {
            return;
        }

        // 只接受指定 Layer 的物体，例如手指、手或 Controller。
        if (!experimentManager.IsValidTouchObject(other.gameObject))
        {
            return;
        }

        experimentManager.RegisterCubeTouch(this);
    }

    /// <summary>
    /// 如果之后使用 Meta Interaction SDK 的事件，
    /// 可以直接从 UnityEvent 调用这个方法。
    /// </summary>
    public void RegisterTouchFromInteractionEvent()
    {
        if (experimentManager != null)
        {
            experimentManager.RegisterCubeTouch(this);
        }
    }
}
