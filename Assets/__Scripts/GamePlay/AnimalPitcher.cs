using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PitchPositionConfig
{
    [Tooltip("动物站位")]
    public Transform standPosition;

    [Tooltip("此位置的落点（为空则沿用 Pitcher 上的默认落点）")]
    public Transform targetPoint;

    [Tooltip("球飞行时间，越大弧线越高")]
    public float flightTime = 1.8f;

    [Tooltip("微重力倍率，越小下坠越慢")]
    public float gravityScale = 1f;
}

public class AnimalPitcher : MonoBehaviour
{
    [Header("Prefab 引用")]
    [SerializeField] private GameObject cowPrefab;
    [SerializeField] private GameObject chickPrefab;
    [SerializeField] private GameObject sheepPrefab;

    [Header("发球锚点偏移")]
    [SerializeField] private Vector3 cowAnchorOffset = new Vector3(0, 1.2f, 0.35f);
    [SerializeField] private Vector3 chickAnchorOffset = new Vector3(0, 0.7f, 0.25f);
    [SerializeField] private Vector3 sheepAnchorOffset = new Vector3(0, 1.05f, 0.3f);

    [Header("移动")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private Transform homePlate;
    [SerializeField] private Transform defaultPosition;

    [Header("发球位置配置")]
    [SerializeField] private PitchPositionConfig[] pitchConfigs;

    [Header("动物音效（发球时随机播放）")]
    [SerializeField] private AudioClip[] cowSounds;
    [SerializeField] private AudioClip[] chickSounds;
    [SerializeField] private AudioClip[] sheepSounds;

    private string animalType;
    private GameObject animalInstance;
    private Animator animator;
    private Transform launchPointAnchor;
    private int currentConfigIndex;
    private List<int> unusedIndices;
    private Coroutine moveCoroutine;

    public Transform CurrentLaunchPoint => launchPointAnchor;
    public Vector3 AnimalPosition => animalInstance != null ? animalInstance.transform.position : transform.position;
    public Transform AnimalTransform => animalInstance != null ? animalInstance.transform : null;
    public PitchPositionConfig CurrentConfig =>
        pitchConfigs != null && currentConfigIndex < pitchConfigs.Length
            ? pitchConfigs[currentConfigIndex] : null;

    public void Initialize(string type)
    {
        animalType = type;

        GameObject prefab = animalType switch
        {
            "CHICK" => chickPrefab,
            "SHEEP" => sheepPrefab,
            _       => cowPrefab
        };

        animalInstance = Instantiate(prefab, transform);
        animator = animalInstance.GetComponentInChildren<Animator>();

        // Rigidbody 落地：先 kinematic 定位再切回物理
        var rb = animalInstance.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.mass = 10f;
        rb.drag = 2f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        int animalLayer = LayerMask.NameToLayer("Animal");
        foreach (Transform t in animalInstance.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = animalLayer;

        Vector3 anchorOffset = animalType switch
        {
            "CHICK" => chickAnchorOffset,
            "SHEEP" => sheepAnchorOffset,
            _       => cowAnchorOffset
        };
        GameObject anchor = new GameObject("LaunchPoint");
        anchor.transform.SetParent(animalInstance.transform);
        anchor.transform.localPosition = anchorOffset;
        launchPointAnchor = anchor.transform;

        var pitcher = FindObjectOfType<Pitcher>();

        bool isClassic = GameManager.Instance != null
            && GameManager.Instance.CurrentMode == GameManager.GameMode.Classic;

        bool hasConfigs = pitchConfigs != null && pitchConfigs.Length > 0;

        if (isClassic && hasConfigs)
        {
            unusedIndices = new List<int>(pitchConfigs.Length);
            for (int i = 0; i < pitchConfigs.Length; i++)
                unusedIndices.Add(i);

            currentConfigIndex = 0;
            animalInstance.transform.position = pitchConfigs[0].standPosition.position;
            unusedIndices.RemoveAt(0);
        }
        else if (defaultPosition != null)
        {
            animalInstance.transform.position = defaultPosition.position;
        }

        // 永久绑死发球点到动物锚点 + 订阅发球事件
        if (pitcher != null)
        {
            pitcher.SetLaunchPoint(launchPointAnchor);
            pitcher.onPitch += PlayPitchAnimation;
            pitcher.SetBallVisible(false);
        }

        // 解除父子关系，物理位置同步，再开重力
        animalInstance.transform.SetParent(null);
        rb.position = animalInstance.transform.position;
        rb.rotation = animalInstance.transform.rotation;
        rb.isKinematic = false;

        FaceHomePlate();
    }

    public IEnumerator MoveToNextPosition()
    {
        if (pitchConfigs == null || pitchConfigs.Length == 0)
            yield break;

        if (unusedIndices.Count == 0)
        {
            for (int i = 0; i < pitchConfigs.Length; i++)
                unusedIndices.Add(i);
        }

        currentConfigIndex = unusedIndices[0];
        unusedIndices.RemoveAt(0);

        Vector3 target = pitchConfigs[currentConfigIndex].standPosition.position;

        if (animator != null)
            animator.CrossFade("Run", 0.1f);

        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(MoveToTarget(target));
        yield return moveCoroutine;
        moveCoroutine = null;

        FaceHomePlate();
        if (animator != null)
            animator.CrossFade("Idle_A", 0.15f);
    }

    private IEnumerator MoveToTarget(Vector3 target)
    {
        while (Vector3.Distance(animalInstance.transform.position, target) > 0.05f)
        {
            animalInstance.transform.position = Vector3.MoveTowards(
                animalInstance.transform.position, target, moveSpeed * Time.deltaTime);
            Vector3 dir = target - animalInstance.transform.position;
            dir.y = 0;
            if (dir.magnitude > 0.01f)
                animalInstance.transform.rotation = Quaternion.LookRotation(dir);
            yield return null;
        }
    }

    private void FaceHomePlate()
    {
        if (homePlate == null) return;
        Vector3 dir = homePlate.position - animalInstance.transform.position;
        dir.y = 0;
        if (dir.magnitude > 0.01f)
            animalInstance.transform.rotation = Quaternion.LookRotation(dir);
    }

    public void PlayPitchAnimation()
    {
        if (animator != null)
            animator.SetTrigger("Attack");

        PlayRandomSound();
    }

    private void PlayRandomSound()
    {
        AudioClip[] clips = animalType switch
        {
            "CHICK" => chickSounds,
            "SHEEP" => sheepSounds,
            _       => cowSounds
        };

        if (clips == null || clips.Length == 0) return;

        int idx = UnityEngine.Random.Range(0, clips.Length);
        if (clips[idx] != null)
            AudioManager.Instance?.PlaySFX(clips[idx], 1f);
    }

    public void Cleanup()
    {
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = null;

        var pitcher = FindObjectOfType<Pitcher>();
        if (pitcher != null) pitcher.onPitch -= PlayPitchAnimation;
        pitcher?.ResetLaunchPoint();

        if (animalInstance != null)
            Destroy(animalInstance);

        animalType = null;
        animalInstance = null;
        animator = null;
        launchPointAnchor = null;
        currentConfigIndex = 0;
        unusedIndices = null;
    }

    public void StopMovement()
    {
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = null;
    }
}
