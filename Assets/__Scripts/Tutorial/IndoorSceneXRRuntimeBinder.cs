using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class IndoorSceneXRRuntimeBinder : MonoBehaviour
{
    [Header("Scene names")]
    public string indoorSceneName = "Indoor Scene(0.3）";

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        TryBindForActiveScene();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryBindForScene(scene);
    }

    private void TryBindForActiveScene()
    {
        TryBindForScene(SceneManager.GetActiveScene());
    }

    private void TryBindForScene(Scene scene)
    {
        if (scene.name != indoorSceneName)
            return;

        XRInteractionManager interactionManager = FindActiveInteractionManager();
        Transform xrOrigin = FindActiveXrOrigin();
        Camera xrCamera = xrOrigin != null ? xrOrigin.GetComponentInChildren<Camera>(true) : null;

        RebindViewTransitionManagers(xrOrigin, xrCamera);

        if (interactionManager != null)
            RebindInteractionManagers(interactionManager);
    }

    private static XRInteractionManager FindActiveInteractionManager()
    {
        XRInteractionManager[] managers = FindObjectsOfType<XRInteractionManager>(true);
        for (int i = 0; i < managers.Length; i++)
        {
            if (managers[i].gameObject.activeInHierarchy)
                return managers[i];
        }

        return null;
    }

    private static Transform FindActiveXrOrigin()
    {
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == "XR Origin" && roots[i].activeInHierarchy)
                return roots[i].transform;
        }

        GameObject fallback = GameObject.Find("XR Origin");
        return fallback != null && fallback.activeInHierarchy ? fallback.transform : null;
    }

    private static void RebindViewTransitionManagers(Transform xrOrigin, Camera xrCamera)
    {
        if (xrOrigin == null)
            return;

        ViewTransitionManager[] managers = FindObjectsOfType<ViewTransitionManager>(true);
        for (int i = 0; i < managers.Length; i++)
        {
            managers[i].BindRuntimeRig(xrOrigin, xrCamera);
        }
    }

    private static void RebindInteractionManagers(XRInteractionManager interactionManager)
    {
        XRBaseInteractable[] interactables = FindObjectsOfType<XRBaseInteractable>(true);
        for (int i = 0; i < interactables.Length; i++)
            interactables[i].interactionManager = interactionManager;

        XRBaseInteractor[] interactors = FindObjectsOfType<XRBaseInteractor>(true);
        for (int i = 0; i < interactors.Length; i++)
            interactors[i].interactionManager = interactionManager;

        XRInteractionGroup[] groups = FindObjectsOfType<XRInteractionGroup>(true);
        for (int i = 0; i < groups.Length; i++)
            groups[i].interactionManager = interactionManager;
    }
}
