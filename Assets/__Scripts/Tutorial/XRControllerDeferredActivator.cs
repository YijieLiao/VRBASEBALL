using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class XRControllerDeferredActivator : MonoBehaviour
{
    private static XRControllerDeferredActivator instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (instance != null)
            return;

        GameObject bootstrap = new GameObject(nameof(XRControllerDeferredActivator));
        DontDestroyOnLoad(bootstrap);
        instance = bootstrap.AddComponent<XRControllerDeferredActivator>();
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
        StartCoroutine(EnableControllersNextFrame(scene));
    }

    private IEnumerator EnableControllersNextFrame(Scene scene)
    {
        yield return null;

        GameObject xrOrigin = FindRootObject(scene, "XR Origin");
        if (xrOrigin == null)
            yield break;

        // Re-enable XRInputModalityManager first so it can take over controller mode switching.
        // It was disabled in the scene to prevent it from calling SafeSetActive() on controllers
        // before XR Interaction Groups finish registering with XRInteractionManager.
        XRInputModalityManager modalityManager = xrOrigin.GetComponentInChildren<XRInputModalityManager>(true);
        if (modalityManager != null && !modalityManager.enabled)
            modalityManager.enabled = true;

        SetChildActive(xrOrigin.transform, "Left Controller", true);
        SetChildActive(xrOrigin.transform, "Right Controller", true);
    }

    private static GameObject FindRootObject(Scene scene, string name)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == name && roots[i].activeSelf)
                return roots[i];
        }

        return null;
    }

    private static void SetChildActive(Transform parent, string childName, bool active)
    {
        Transform child = FindChildRecursive(parent, childName);
        if (child == null)
            return;

        if (child.gameObject.activeSelf != active)
            child.gameObject.SetActive(active);
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
