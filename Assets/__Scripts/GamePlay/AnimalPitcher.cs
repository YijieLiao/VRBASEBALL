using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    private string animalType;
    private GameObject animalInstance;
    private Animator animator;
    private Transform[] positions;
    private Transform launchPointAnchor;
    private List<int> unusedIndices;
    private Coroutine moveCoroutine;

    public Transform CurrentLaunchPoint => launchPointAnchor;

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

        ReadPositionsFromScene();

        var pitcher = FindObjectOfType<Pitcher>();

        bool isClassic = GameManager.Instance != null
            && GameManager.Instance.CurrentMode == GameManager.GameMode.Classic;

        if (isClassic && positions.Length > 0)
        {
            unusedIndices = new List<int>(positions.Length);
            for (int i = 0; i < positions.Length; i++)
                unusedIndices.Add(i);

            animalInstance.transform.position = positions[0].position;
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

    private void ReadPositionsFromScene()
    {
        GameObject container = GameObject.Find("PitcherPositions");
        if (container == null)
        {
            positions = new Transform[0];
            return;
        }

        int count = container.transform.childCount;
        positions = new Transform[count];
        for (int i = 0; i < count; i++)
            positions[i] = container.transform.GetChild(i);
    }

    public IEnumerator MoveToNextPosition()
    {
        if (positions.Length == 0)
            yield break;

        if (unusedIndices.Count == 0)
        {
            for (int i = 0; i < positions.Length; i++)
                unusedIndices.Add(i);
        }

        int pick = unusedIndices[0];
        unusedIndices.RemoveAt(0);

        Vector3 target = positions[pick].position;

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
        positions = null;
        unusedIndices = null;
    }

    public void StopMovement()
    {
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = null;
    }
}
