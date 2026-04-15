using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 黑屏淡入淡出 + XR Origin 位置切换
/// Canvas 会同步渐隐渐显（包括 CanvasGroup 和 SpriteRenderer）
/// 参考 SelectionManager 做法
/// </summary>
public class ViewTransitionManager : MonoBehaviour
{
    public static ViewTransitionManager Instance { get; private set; }

    public enum ViewMode { Room, Batting }

    [Header("XR 引用")]
    public Transform xrOrigin;
    public Camera xrCamera;

    [Header("视角锚点")]
    [Tooltip("正常房间锚点：决定切回房间时玩家的位置和朝向\n锚点Z轴正向 = 玩家面朝方向")]
    public Transform roomAnchor;

    [Tooltip("击球位置锚点：决定进入击球视角时的位置和朝向\n锚点Z轴正向 = 玩家面朝方向（通常应朝向投手方向）")]
    public Transform battingAnchor;

    [Header("淡入淡出参数")]
    [Tooltip("黑屏淡出的时间（秒）：0.4 = 400毫秒")]
    [Range(0.1f, 2f)]
    public float fadeDuration = 0.4f;

    [Tooltip("全黑后的等待时间（秒）：0.1 = 100毫秒")]
    [Range(0f, 0.5f)]
    public float blackScreenDelay = 0.1f;

    [Tooltip("Canvas 渐隐时间比例（0-1）：0.5 表示 Canvas 在 50% 时间内完成渐隐")]
    [Range(0.1f, 1f)]
    public float canvasFadeRatio = 0.5f;

    public Color fadeColor = Color.black;

    [Header("Canvas 处理")]
    [Tooltip("需要同步渐隐的 Canvas 列表\n留空则自动查找场景中所有 World Space Canvas")]
    public Canvas[] canvasesToFade;

    [Header("高级设置")]
    [Tooltip("勾选 = 锚点代表相机位置（眼睛在哪）\n不勾选 = 锚点代表脚底位置")]
    public bool anchorRepresentsCameraPosition = false;

    public ViewMode CurrentMode { get; private set; } = ViewMode.Room;
    public bool IsTransitioning { get; private set; }

    private MeshRenderer _fadeMeshRenderer;
    private Material _fadeMaterial;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (xrCamera == null) xrCamera = Camera.main;

        CreateFadeOverlay();
        SetFadeAlpha(0f);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (_fadeMaterial != null) Destroy(_fadeMaterial);
    }

    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.T))
            Toggle();
#endif
    }

    public void Toggle()
    {
        TransitionTo(CurrentMode == ViewMode.Room ? ViewMode.Batting : ViewMode.Room);
    }

    public void TransitionTo(ViewMode target)
    {
        if (IsTransitioning || target == CurrentMode) return;
        StartCoroutine(ExecuteTransition(target));
    }

    public void TeleportImmediate(ViewMode target)
    {
        if (IsTransitioning) return;
        MoveXROrigin(target);
        CurrentMode = target;
    }

    private IEnumerator ExecuteTransition(ViewMode target)
    {
        IsTransitioning = true;

        // 获取所有需要处理的 Canvas
        Canvas[] canvases = canvasesToFade ?? FindCanvases();

        // 临时禁用 TrackedDeviceGraphicRaycaster 避免 XRI bug
        var raycasters = new List<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
        foreach (var c in canvases)
        {
            var raycaster = c.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
            if (raycaster != null)
            {
                raycasters.Add(raycaster);
                raycaster.enabled = false;
            }
        }

        CanvasFadeInfo[] fadeInfos = PrepareCanvasFade(canvases);

        // 阶段 1：屏幕 + Canvas 同时淡出
        yield return FadeScreenAndCanvas(1f, fadeInfos, true);

        // 阶段 2：移动位置（此时全黑，Canvas 已隐藏）
        MoveXROrigin(target);
        CurrentMode = target;

        if (blackScreenDelay > 0f)
            yield return new WaitForSeconds(blackScreenDelay);

        // 阶段 3：屏幕 + Canvas 同时淡入
        yield return FadeScreenAndCanvas(0f, fadeInfos, false);

        // 清理
        CleanupCanvasFade(fadeInfos);

        // 恢复 TrackedDeviceGraphicRaycaster
        foreach (var raycaster in raycasters)
        {
            if (raycaster != null)
                raycaster.enabled = true;
        }

        IsTransitioning = false;
    }

    #region Canvas 淡出处理（参考 SelectionManager）

    private Canvas[] FindCanvases()
    {
        Canvas[] all = FindObjectsOfType<Canvas>();
        List<Canvas> list = new List<Canvas>();
        foreach (var c in all)
            if (c.renderMode == RenderMode.WorldSpace)
                list.Add(c);
        return list.ToArray();
    }

    private CanvasFadeInfo[] PrepareCanvasFade(Canvas[] canvases)
    {
        CanvasFadeInfo[] infos = new CanvasFadeInfo[canvases.Length];
        for (int i = 0; i < canvases.Length; i++)
        {
            var canvas = canvases[i];

            // 确保有 CanvasGroup
            CanvasGroup group = canvas.GetComponent<CanvasGroup>();
            if (group == null)
                group = canvas.gameObject.AddComponent<CanvasGroup>();

            // 收集所有 SpriteRenderer（像 SelectionManager 一样处理）
            SpriteRenderer[] sprites = canvas.GetComponentsInChildren<SpriteRenderer>(true);

            infos[i] = new CanvasFadeInfo
            {
                canvas = canvas,
                group = group,
                originalGroupAlpha = group.alpha,
                sprites = sprites,
                originalSpriteAlphas = new float[sprites.Length]
            };

            // 记录 SpriteRenderer 原始 alpha
            for (int j = 0; j < sprites.Length; j++)
                infos[i].originalSpriteAlphas[j] = sprites[j].color.a;
        }
        return infos;
    }

    private IEnumerator FadeScreenAndCanvas(float targetScreenAlpha, CanvasFadeInfo[] fadeInfos, bool isFadingOut)
    {
        float duration = fadeDuration;
        float canvasDuration = duration * canvasFadeRatio;
        float elapsed = 0f;
        float startScreenAlpha = _fadeMaterial.color.a;

        // 记录起始值
        float[] startGroupAlphas = new float[fadeInfos.Length];
        float[][] startSpriteAlphas = new float[fadeInfos.Length][];
        for (int i = 0; i < fadeInfos.Length; i++)
        {
            startGroupAlphas[i] = fadeInfos[i].group.alpha;
            startSpriteAlphas[i] = new float[fadeInfos[i].sprites.Length];
            for (int j = 0; j < fadeInfos[i].sprites.Length; j++)
                startSpriteAlphas[i][j] = fadeInfos[i].sprites[j].color.a;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // 屏幕淡出
            SetFadeAlpha(Mathf.Lerp(startScreenAlpha, targetScreenAlpha, smoothT));

            // Canvas 淡出（更快完成）
            float canvasT = Mathf.Clamp01(elapsed / canvasDuration);
            float canvasSmoothT = Mathf.SmoothStep(0f, 1f, canvasT);

            for (int i = 0; i < fadeInfos.Length; i++)
            {
                var info = fadeInfos[i];

                // CanvasGroup Alpha
                float startAlpha = isFadingOut ? info.originalGroupAlpha : 0f;
                float finalAlpha = isFadingOut ? 0f : info.originalGroupAlpha;
                info.group.alpha = Mathf.Lerp(startAlpha, finalAlpha, canvasSmoothT);

                // SpriteRenderer Alpha
                for (int j = 0; j < info.sprites.Length; j++)
                {
                    float spriteStart = isFadingOut ? info.originalSpriteAlphas[j] : 0f;
                    float spriteFinal = isFadingOut ? 0f : info.originalSpriteAlphas[j];
                    Color c = info.sprites[j].color;
                    c.a = Mathf.Lerp(spriteStart, spriteFinal, canvasSmoothT);
                    info.sprites[j].color = c;
                }
            }

            yield return null;
        }

        // 确保最终值正确
        SetFadeAlpha(targetScreenAlpha);
        foreach (var info in fadeInfos)
        {
            info.group.alpha = isFadingOut ? 0f : info.originalGroupAlpha;
            for (int j = 0; j < info.sprites.Length; j++)
            {
                Color c = info.sprites[j].color;
                c.a = isFadingOut ? 0f : info.originalSpriteAlphas[j];
                info.sprites[j].color = c;
            }
        }
    }

    private void CleanupCanvasFade(CanvasFadeInfo[] fadeInfos)
    {
        foreach (var info in fadeInfos)
        {
            // 恢复原始状态
            info.group.alpha = info.originalGroupAlpha;
            for (int j = 0; j < info.sprites.Length; j++)
            {
                Color c = info.sprites[j].color;
                c.a = info.originalSpriteAlphas[j];
                info.sprites[j].color = c;
            }
        }
    }

    private struct CanvasFadeInfo
    {
        public Canvas canvas;
        public CanvasGroup group;
        public float originalGroupAlpha;
        public SpriteRenderer[] sprites;
        public float[] originalSpriteAlphas;
    }

    #endregion

    #region 位置切换

    private void MoveXROrigin(ViewMode target)
    {
        Transform anchor = target == ViewMode.Room ? roomAnchor : battingAnchor;
        if (xrOrigin == null || anchor == null) return;

        xrOrigin.rotation = anchor.rotation;

        if (anchorRepresentsCameraPosition)
        {
            Vector3 camLocal = xrCamera.transform.localPosition;
            xrOrigin.position = anchor.position - xrOrigin.TransformVector(camLocal);
        }
        else
        {
            Vector3 headOffset = xrCamera.transform.localPosition;
            headOffset.y = 0f;
            xrOrigin.position = anchor.position - xrOrigin.TransformVector(headOffset);
        }
    }

    #endregion

    #region 屏幕淡入淡出

    private void SetFadeAlpha(float alpha)
    {
        Color c = fadeColor;
        c.a = alpha;
        _fadeMaterial.color = c;
        _fadeMeshRenderer.enabled = alpha > 0.001f;
    }

    #endregion

    #region 遮罩球体

    private void CreateFadeOverlay()
    {
        GameObject fadeObj = new GameObject("_ScreenFadeOverlay");
        fadeObj.transform.SetParent(xrCamera.transform, false);
        fadeObj.transform.localPosition = Vector3.zero;
        fadeObj.layer = LayerMask.NameToLayer("UI");

        MeshFilter mf = fadeObj.AddComponent<MeshFilter>();
        _fadeMeshRenderer = fadeObj.AddComponent<MeshRenderer>();
        mf.sharedMesh = BuildInvertedSphereMesh(0.4f, 16, 10);

        Shader shader = Shader.Find("Custom/ScreenFade");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        _fadeMaterial = new Material(shader);
        _fadeMaterial.renderQueue = 5000;
        _fadeMeshRenderer.sharedMaterial = _fadeMaterial;
        _fadeMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _fadeMeshRenderer.receiveShadows = false;
        _fadeMeshRenderer.enabled = false;
    }

    private static Mesh BuildInvertedSphereMesh(float radius, int lon, int lat)
    {
        Mesh mesh = new Mesh { name = "InvertedFadeSphere" };
        Vector3[] verts = new Vector3[(lon + 1) * (lat + 1)];
        int[] tris = new int[lon * lat * 6];

        int vi = 0;
        for (int la = 0; la <= lat; la++)
        {
            float theta = Mathf.PI * la / lat;
            float sinT = Mathf.Sin(theta), cosT = Mathf.Cos(theta);
            for (int lo = 0; lo <= lon; lo++)
            {
                float phi = 2f * Mathf.PI * lo / lon;
                verts[vi++] = new Vector3(sinT * Mathf.Cos(phi), cosT, sinT * Mathf.Sin(phi)) * radius;
            }
        }

        int ti = 0;
        for (int la = 0; la < lat; la++)
            for (int lo = 0; lo < lon; lo++)
            {
                int c = la * (lon + 1) + lo, n = c + lon + 1;
                tris[ti++] = c; tris[ti++] = c + 1; tris[ti++] = n;
                tris[ti++] = n; tris[ti++] = c + 1; tris[ti++] = n + 1;
            }

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
        return mesh;
    }

    #endregion

    #region Editor 可视化

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        DrawAnchorGizmo(roomAnchor, Color.cyan, "Room");
        DrawAnchorGizmo(battingAnchor, Color.magenta, "Batting");
    }

    private void DrawAnchorGizmo(Transform anchor, Color color, string label)
    {
        if (anchor == null) return;

        Gizmos.color = color;
        Vector3 pos = anchor.position;

        float radius = 0.3f;
        for (int i = 0; i < 32; i++)
        {
            float a1 = 2f * Mathf.PI * i / 32;
            float a2 = 2f * Mathf.PI * (i + 1) / 32;
            Gizmos.DrawLine(pos + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * radius,
                            pos + new Vector3(Mathf.Cos(a2), 0, Mathf.Sin(a2)) * radius);
        }

        Vector3 fwd = anchor.forward;
        Gizmos.DrawLine(pos, pos + fwd * 0.5f);
        Gizmos.DrawLine(pos + fwd * 0.5f, pos + fwd * 0.3f + Quaternion.Euler(0, 30, 0) * fwd * 0.2f);
        Gizmos.DrawLine(pos + fwd * 0.5f, pos + fwd * 0.3f + Quaternion.Euler(0, -30, 0) * fwd * 0.2f);

        float headY = anchorRepresentsCameraPosition ? 0f : 1.6f;
        Gizmos.DrawWireSphere(pos + Vector3.up * headY, 0.08f);
    }
#endif

    #endregion
}
