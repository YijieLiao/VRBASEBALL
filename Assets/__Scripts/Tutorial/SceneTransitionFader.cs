using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionFader : MonoBehaviour
{
    private const string SpawnPointName = "SMALL ROOM POINT";
    private const string XrOriginName = "XR Origin";

    public static SceneTransitionFader Instance { get; private set; }

    public float fadeInDuration = 0.4f;
    public float fadeOutDuration = 0.6f;
    public float delayBeforeLoad = 0.1f;
    public float delayBeforeFadeOut = 0.3f;
    public Color fadeColor = Color.black;

    private bool isTransitioning;
    private Camera xrCamera;
    private MeshRenderer fadeMeshRenderer;
    private Material fadeMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (Instance != null)
            return;

        GameObject go = new GameObject(nameof(SceneTransitionFader));
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<SceneTransitionFader>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        ResolveCamera();
        RebuildFadeOverlay();
        SetFadeAlpha(0f);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (fadeMaterial != null)
            Destroy(fadeMaterial);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResolveCamera();
        RebuildFadeOverlay();

        if (!isTransitioning)
            return;

        StopAllCoroutines();
        SetFadeAlpha(1f);
        AlignPersistentXrOrigin(scene);
        StartCoroutine(FadeOutRoutine());
    }

    public void FadeToBlackThenLoad(string sceneName)
    {
        if (isTransitioning)
            return;

        ResolveCamera();
        RebuildFadeOverlay();
        isTransitioning = true;
        StartCoroutine(FadeInThenLoadRoutine(sceneName));
    }

    private IEnumerator FadeInThenLoadRoutine(string sceneName)
    {
        yield return Fade(0f, 1f, fadeInDuration);

        if (delayBeforeLoad > 0f)
            yield return new WaitForSeconds(delayBeforeLoad);

        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator FadeOutRoutine()
    {
        CanvasFadeInfo[] fadeInfos = PrepareSceneCanvasFade(SceneManager.GetActiveScene());

        if (delayBeforeFadeOut > 0f)
            yield return new WaitForSeconds(delayBeforeFadeOut);

        yield return FadeScreenAndCanvasOut(fadeInfos);
        CleanupCanvasFade(fadeInfos);
        isTransitioning = false;
    }

    private void AlignPersistentXrOrigin(Scene scene)
    {
        Transform spawnPoint = FindRootTransform(scene, SpawnPointName);
        if (spawnPoint == null)
            return;

        GameObject xrOrigin = GameObject.Find(XrOriginName);
        if (xrOrigin == null)
            return;

        xrOrigin.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
    }

    private bool ResolveCamera()
    {
        GameObject xrOrigin = GameObject.Find(XrOriginName);
        if (xrOrigin != null && xrOrigin.activeInHierarchy)
            xrCamera = xrOrigin.GetComponentInChildren<Camera>(true);

        if (xrCamera == null)
            xrCamera = Camera.main;

        return xrCamera != null;
    }

    private static Transform FindRootTransform(Scene scene, string objectName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == objectName)
                return roots[i].transform;
        }

        return null;
    }

    private CanvasFadeInfo[] PrepareSceneCanvasFade(Scene scene)
    {
        List<CanvasFadeInfo> infos = new List<CanvasFadeInfo>();
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            Canvas[] canvases = roots[i].GetComponentsInChildren<Canvas>(true);
            for (int j = 0; j < canvases.Length; j++)
            {
                if (canvases[j].renderMode != RenderMode.WorldSpace)
                    continue;

                if (!canvases[j].gameObject.activeInHierarchy)
                    continue;

                PanelController panelController = canvases[j].GetComponent<PanelController>();
                if (panelController != null)
                    continue;

                CanvasGroup existingGroup = canvases[j].GetComponent<CanvasGroup>();
                if (existingGroup != null && existingGroup.alpha <= 0.001f)
                    continue;

                CanvasGroup group = existingGroup;
                if (group == null)
                    group = canvases[j].gameObject.AddComponent<CanvasGroup>();

                SpriteRenderer[] sprites = canvases[j].GetComponentsInChildren<SpriteRenderer>(true);
                float[] spriteAlphas = new float[sprites.Length];
                for (int k = 0; k < sprites.Length; k++)
                    spriteAlphas[k] = sprites[k].color.a;

                CanvasFadeInfo info = new CanvasFadeInfo
                {
                    group = group,
                    originalGroupAlpha = group.alpha,
                    sprites = sprites,
                    originalSpriteAlphas = spriteAlphas,
                };

                group.alpha = 0f;
                for (int k = 0; k < sprites.Length; k++)
                {
                    Color color = sprites[k].color;
                    color.a = 0f;
                    sprites[k].color = color;
                }

                infos.Add(info);
            }
        }

        return infos.ToArray();
    }

    private IEnumerator FadeScreenAndCanvasOut(CanvasFadeInfo[] fadeInfos)
    {
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            SetFadeAlpha(Mathf.Lerp(1f, 0f, t));

            for (int i = 0; i < fadeInfos.Length; i++)
            {
                fadeInfos[i].group.alpha = Mathf.Lerp(0f, fadeInfos[i].originalGroupAlpha, t);
                for (int j = 0; j < fadeInfos[i].sprites.Length; j++)
                {
                    Color color = fadeInfos[i].sprites[j].color;
                    color.a = Mathf.Lerp(0f, fadeInfos[i].originalSpriteAlphas[j], t);
                    fadeInfos[i].sprites[j].color = color;
                }
            }

            yield return null;
        }

        SetFadeAlpha(0f);
        for (int i = 0; i < fadeInfos.Length; i++)
        {
            fadeInfos[i].group.alpha = fadeInfos[i].originalGroupAlpha;
            for (int j = 0; j < fadeInfos[i].sprites.Length; j++)
            {
                Color color = fadeInfos[i].sprites[j].color;
                color.a = fadeInfos[i].originalSpriteAlphas[j];
                fadeInfos[i].sprites[j].color = color;
            }
        }
    }

    private static void CleanupCanvasFade(CanvasFadeInfo[] fadeInfos)
    {
        for (int i = 0; i < fadeInfos.Length; i++)
        {
            fadeInfos[i].group.alpha = fadeInfos[i].originalGroupAlpha;
            for (int j = 0; j < fadeInfos[i].sprites.Length; j++)
            {
                Color color = fadeInfos[i].sprites[j].color;
                color.a = fadeInfos[i].originalSpriteAlphas[j];
                fadeInfos[i].sprites[j].color = color;
            }
        }
    }

    private struct CanvasFadeInfo
    {
        public CanvasGroup group;
        public float originalGroupAlpha;
        public SpriteRenderer[] sprites;
        public float[] originalSpriteAlphas;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        SetFadeAlpha(from);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetFadeAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }

        SetFadeAlpha(to);
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeMaterial == null || fadeMeshRenderer == null)
            return;

        Color color = fadeColor;
        color.a = alpha;
        fadeMaterial.color = color;
        fadeMeshRenderer.enabled = alpha > 0.001f;
    }

    private void RebuildFadeOverlay()
    {
        if (xrCamera == null)
            return;

        Transform existing = xrCamera.transform.Find("_SceneTransitionFadeOverlay");
        if (existing != null)
            Destroy(existing.gameObject);

        if (fadeMaterial != null)
        {
            Destroy(fadeMaterial);
            fadeMaterial = null;
        }

        CreateFadeOverlay();
    }

    private void CreateFadeOverlay()
    {
        if (xrCamera == null)
            return;

        GameObject fadeObject = new GameObject("_SceneTransitionFadeOverlay");
        fadeObject.transform.SetParent(xrCamera.transform, false);
        fadeObject.transform.localPosition = Vector3.zero;
        fadeObject.layer = LayerMask.NameToLayer("UI");

        MeshFilter meshFilter = fadeObject.AddComponent<MeshFilter>();
        fadeMeshRenderer = fadeObject.AddComponent<MeshRenderer>();
        meshFilter.sharedMesh = BuildInvertedSphereMesh(0.4f, 16, 10);

        Shader shader = Shader.Find("Custom/ScreenFade");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        fadeMaterial = new Material(shader);
        fadeMaterial.renderQueue = 5000;
        fadeMeshRenderer.sharedMaterial = fadeMaterial;
        fadeMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        fadeMeshRenderer.receiveShadows = false;
        fadeMeshRenderer.enabled = false;
    }

    private static Mesh BuildInvertedSphereMesh(float radius, int lon, int lat)
    {
        Mesh mesh = new Mesh { name = "SceneTransitionFadeSphere" };
        Vector3[] vertices = new Vector3[(lon + 1) * (lat + 1)];
        int[] triangles = new int[lon * lat * 6];

        int vertexIndex = 0;
        for (int latIndex = 0; latIndex <= lat; latIndex++)
        {
            float theta = Mathf.PI * latIndex / lat;
            float sinTheta = Mathf.Sin(theta);
            float cosTheta = Mathf.Cos(theta);

            for (int lonIndex = 0; lonIndex <= lon; lonIndex++)
            {
                float phi = 2f * Mathf.PI * lonIndex / lon;
                vertices[vertexIndex++] = new Vector3(sinTheta * Mathf.Cos(phi), cosTheta, sinTheta * Mathf.Sin(phi)) * radius;
            }
        }

        int triangleIndex = 0;
        for (int latIndex = 0; latIndex < lat; latIndex++)
        {
            for (int lonIndex = 0; lonIndex < lon; lonIndex++)
            {
                int current = latIndex * (lon + 1) + lonIndex;
                int next = current + lon + 1;
                triangles[triangleIndex++] = current;
                triangles[triangleIndex++] = current + 1;
                triangles[triangleIndex++] = next;
                triangles[triangleIndex++] = next;
                triangles[triangleIndex++] = current + 1;
                triangles[triangleIndex++] = next + 1;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }
}
