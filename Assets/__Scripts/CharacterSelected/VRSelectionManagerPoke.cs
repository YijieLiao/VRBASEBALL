using DG.Tweening;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Unity.XR.PXR;
using System.Text;
using UnityEngine.XR;
using System.Collections.Generic;

public class VRSelectionManagerPoke: MonoBehaviour
{
    [Header("3D 配置")]
    public float flyHeight = 0.4f;
    public float animDuration = 0.5f;

    [Header("UI 配置")]
    public Transform uiPanelRoot;

    private GameObject currentSelectedDoll;
    private Transform currentCircle;
    private CanvasGroup currentActivePanel;

    // 小鸡配置
    private float chickHomeY;
    private bool isHomeYCaptured = false;
    private readonly string CHICK_FLIGHT_ID = "ChickFlightID";

    void Start()
    {
        GameObject[] dolls = GameObject.FindGameObjectsWithTag("Doll");

        // 1. 优先记录小鸡初始高度
        foreach (GameObject doll in dolls)
        {
            if (doll.name.ToUpper().Contains("CHICK"))
            {
                chickHomeY = doll.transform.position.y;
                isHomeYCaptured = true;
                break;
            }
        }

        // 2. 初始化 UI 和 3D 状态同步
        SyncInitialUI(dolls);
    }

    public void OnDollHovered(HoverEnterEventArgs args)
    {
        // 调试日志
        Debug.Log("<color=yellow>【交互触发】</color> 摸到了: " + args.interactableObject.transform.name);

        GameObject clickedDoll = args.interactableObject.transform.gameObject;

        // 检查标签并防止重复触发
        if (clickedDoll.CompareTag("Doll") && clickedDoll != currentSelectedDoll)
        {
            Debug.Log("<color=green>【逻辑执行】</color> 正在切换到: " + clickedDoll.name);

            ChangeSelection(clickedDoll);

            // 传入 interactor 进行震动
            TriggerHaptic(args.interactorObject);
        }
    }

    private void TriggerHaptic(IXRHoverInteractor interactor)
    {
        // 基础震动：使用 XRI 标准接口（PICO 会自动转换）
        var controller = (interactor as MonoBehaviour)?.GetComponentInParent<ActionBasedController>();
        if (controller != null)
        {
            controller.SendHapticImpulse(0.5f, 0.1f);
        }
    }

    void SyncInitialUI(GameObject[] dolls)
    {
        if (uiPanelRoot == null) return;

        GameObject initialSelected = null;
        foreach (GameObject doll in dolls)
        {
            Transform circle = doll.transform.Find("Circle");
            if (circle != null && circle.gameObject.activeSelf)
            {
                initialSelected = doll;
                currentSelectedDoll = doll;
                currentCircle = circle;
                break;
            }
        }

        foreach (Transform child in uiPanelRoot)
        {
            if (IsAnimalPanel(child.name))
            {
                CanvasGroup cg = child.GetComponent<CanvasGroup>();
                if (cg == null) cg = child.gameObject.AddComponent<CanvasGroup>();

                if (initialSelected != null && initialSelected.name.ToUpper().Contains(child.name.ToUpper()))
                {
                    child.gameObject.SetActive(true);
                    cg.alpha = 1f;
                    currentActivePanel = cg;
                    PlayDollAction(initialSelected);
                }
                else
                {
                    cg.alpha = 0f;
                    child.gameObject.SetActive(false);
                }
            }
        }
    }

    void Update()
    {
    }

    void HandleSelection()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.CompareTag("Doll"))
            {
                GameObject clickedDoll = hit.collider.gameObject;
                if (clickedDoll != currentSelectedDoll)
                {
                    ChangeSelection(clickedDoll);
                }
            }
        }
    }

    void ChangeSelection(GameObject newDoll)
    {
        // --- 1. 处理旧目标 ---
        if (currentSelectedDoll != null)
        {
            if (currentSelectedDoll.name.ToUpper().Contains("CHICK")) LandChick(currentSelectedDoll);

            if (currentCircle != null)
            {
                // 【核心修复】：使用局部变量 old 锁定当前这个圈的引用
                // 防止 0.2s 后 OnComplete 错误地关闭了新赋值给 currentCircle 的物体
                Transform oldCircle = currentCircle;
                oldCircle.DOKill();
                oldCircle.DOScale(Vector3.zero, 0.2f).OnComplete(() => {
                    // 只有当这个圈确实不是当前选中的圈时才关闭
                    if (oldCircle != currentCircle) oldCircle.gameObject.SetActive(false);
                });
            }

            FadeOutUIPanel(currentActivePanel);
        }

        // --- 2. 更新引用 ---
        currentSelectedDoll = newDoll;
        currentCircle = newDoll.transform.Find("Circle");

        // --- 3. 激活新目标 ---
        if (currentCircle != null)
        {
            currentCircle.DOKill(); // 确保干净
            currentCircle.gameObject.SetActive(true);
            currentCircle.localScale = Vector3.zero;
            currentCircle.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);

            PlayDollAction(newDoll);
            FadeInUIPanel(newDoll.name);
        }
    }

    // ================= UI 逻辑 =================

    bool IsAnimalPanel(string name)
    {
        string n = name.ToUpper();
        return n == "COW" || n == "CHICK" || n == "SHEEP";
    }

    void FadeInUIPanel(string dollName)
    {
        foreach (Transform child in uiPanelRoot)
        {
            if (dollName.ToUpper().Contains(child.name.ToUpper()) && IsAnimalPanel(child.name))
            {
                child.gameObject.SetActive(true);
                child.DOKill();

                CanvasGroup cg = child.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.DOKill();
                    cg.alpha = 0;
                    cg.DOFade(1f, 0.5f);
                }

                SpriteRenderer[] sprites = child.GetComponentsInChildren<SpriteRenderer>();
                foreach (var sr in sprites)
                {
                    sr.DOKill();
                    Color c = sr.color;
                    c.a = 0;
                    sr.color = c;
                    sr.DOFade(1f, 0.5f);
                }

                currentActivePanel = cg;
                break;
            }
        }
    }

    void FadeOutUIPanel(CanvasGroup panelCG)
    {
        if (panelCG == null) return;

        Transform panelTransform = panelCG.transform;

        // 【核心修复】：同样使用局部变量锁定当前正在淡出的面板
        CanvasGroup oldCG = panelCG;
        oldCG.DOKill();
        panelTransform.DOKill();

        oldCG.DOFade(0f, 0.4f);

        SpriteRenderer[] sprites = panelTransform.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in sprites)
        {
            sr.DOKill();
            sr.DOFade(0f, 0.4f);
        }

        DOVirtual.DelayedCall(0.41f, () => {
            // 只有当它确实不是当前选中的面板时才关闭
            if (oldCG != currentActivePanel)
            {
                oldCG.gameObject.SetActive(false);
            }
        });
    }

    // ================= 3D 动作逻辑 =================

    void PlayDollAction(GameObject doll)
    {
        // 这里的 DOKill 只杀父物体，不会杀子物体（Circle）的缩放动画
        doll.transform.DOKill();
        Animator anim = doll.GetComponent<Animator>();
        if (anim == null) return;

        if (doll.name.ToUpper().Contains("CHICK"))
        {
            anim.SetBool("isFlying", true);
            DOTween.Kill(CHICK_FLIGHT_ID);
            doll.transform.DOMoveY(chickHomeY + flyHeight, animDuration).SetEase(Ease.OutCubic).OnComplete(() => {
                doll.transform.DOMoveY(chickHomeY + flyHeight + 0.05f, 1f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetId(CHICK_FLIGHT_ID);
            });
        }
        else
        {
            anim.SetTrigger("Jump");
            doll.transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0.1f), 0.3f);
        }
    }

    void LandChick(GameObject chick)
    {
        chick.transform.DOKill();
        Animator anim = chick.GetComponent<Animator>();
        if (anim != null) anim.SetBool("isFlying", false);
        DOTween.Kill(CHICK_FLIGHT_ID);
        chick.transform.DOMoveY(chickHomeY, animDuration).SetEase(Ease.InQuad);
    }

    // 辅助方法：获取 Transform 的层级路径
    private string GetHierarchyPath(Transform transform)
    {
        StringBuilder sb = new StringBuilder(transform.name);
        Transform parent = transform.parent;
        while (parent != null)
        {
            sb.Insert(0, "/");
            sb.Insert(0, parent.name);
            parent = parent.parent;
        }
        return sb.ToString();
    }
}