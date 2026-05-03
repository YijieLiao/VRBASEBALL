using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class PersistentXRCore : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        XRInteractionManager persistentInteractionManager = GetComponent<XRInteractionManager>();
        if (persistentInteractionManager == null)
            persistentInteractionManager = GetComponentInChildren<XRInteractionManager>(true);

        Transform persistentXrOrigin = transform.name == "XR Origin" ? transform : FindChildRecursive(transform, "XR Origin");
        Camera persistentCamera = persistentXrOrigin != null ? persistentXrOrigin.GetComponentInChildren<Camera>(true) : null;

        RebindViewTransitionManagers(persistentXrOrigin, persistentCamera);

        if (persistentInteractionManager != null)
            RebindInteractionManagers(persistentInteractionManager);
    }

    private static void RebindViewTransitionManagers(Transform persistentXrOrigin, Camera persistentCamera)
    {
        if (persistentXrOrigin == null)
            return;

        ViewTransitionManager[] managers = FindObjectsOfType<ViewTransitionManager>(true);
        for (int i = 0; i < managers.Length; i++)
            managers[i].BindRuntimeRig(persistentXrOrigin, persistentCamera);
    }

    private static void RebindInteractionManagers(XRInteractionManager persistentInteractionManager)
    {
        XRBaseInteractable[] interactables = FindObjectsOfType<XRBaseInteractable>(true);
        for (int i = 0; i < interactables.Length; i++)
            interactables[i].interactionManager = persistentInteractionManager;

        XRBaseInteractor[] interactors = FindObjectsOfType<XRBaseInteractor>(true);
        for (int i = 0; i < interactors.Length; i++)
            interactors[i].interactionManager = persistentInteractionManager;

        XRInteractionGroup[] groups = FindObjectsOfType<XRInteractionGroup>(true);
        for (int i = 0; i < groups.Length; i++)
            groups[i].interactionManager = persistentInteractionManager;
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent.name == childName)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChildRecursive(parent.GetChild(i), childName);
            if (found != null)
                return found;
        }

        return null;
    }
}
