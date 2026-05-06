using TMPro;
using UnityEngine;

public class PausePanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text titleText;

    [Header("引用")]
    [SerializeField] private GameManager gameManager;

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
    }

    private void OnEnable()
    {
        if (gameManager != null)
            gameManager.OnStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        if (gameManager != null)
            gameManager.OnStateChanged -= OnGameStateChanged;
    }

    private void Start()
    {
        SetVisible(false);
    }

    private void OnGameStateChanged(GameState from, GameState to)
    {
        if (to == GameState.Pause)
            SetVisible(true);
        else if (from == GameState.Pause)
            SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        if (titleText != null)
            titleText.text = "暂停";
    }

    // 由 VR UI 按钮调用
    public void OnContinueClicked()
    {
        if (gameManager != null && gameManager.CurrentState == GameState.Pause)
            gameManager.TransitionTo(GameState.Batting);
    }

    public void OnReturnToRoomClicked()
    {
        if (gameManager != null && gameManager.CurrentState == GameState.Pause)
            gameManager.ReturnToRoom();
    }
}
