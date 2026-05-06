using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.XR;

public class IntroSequenceController : MonoBehaviour
{
    public enum GuideStage
    {
        Idle,
        IntroSubtitle,
        FirstBoard,
        TurnTutorialSubtitle,
        TurnLeftStep,
        TurnRightStep,
        AfterTurnSubtitle,
        TeleportHintOne,
        TeleportHintTwo,
        TeleportToTable,
        TableArrivalSubtitle,
        TablePanel,
        FinalSubtitle,
        Completed
    }

    [Header("字幕显示对象")]
    public CanvasGroupFader subtitlePanel;
    public TMP_Text subtitleText;

    [Header("第一段眼前字幕")]
    [TextArea(2, 4)] public string[] introSubtitleLines =
    {
        "你好 欢迎进入动物棒球",
        "这是基础的 VR 交互引导",
        "请你目视前方看向前面的指示板"
    };
    public float introSubtitleStayDuration = 1.8f;
    public float introSubtitleGapDuration = 0.25f;
    public float delayBeforeIntroSubtitle = 0.5f;
    public float delayAfterIntroSubtitle = 0.4f;

    [Header("第一段世界面板")]
    public CanvasGroupFader firstBoard;
    public CanvasGroupFader leftHintText;
    public CanvasGroupFader rightHintText;
    public float boardDelay = 0.2f;
    public float hintGap = 0.15f;

    [Header("第二段眼前字幕（第一段世界面板之后）")]
    [TextArea(2, 4)] public string[] afterFirstBoardSubtitleLines =
    {
        "接下来是转向教程"
    };
    public float delayBeforeAfterFirstBoardSubtitle = 0.45f;
    public float afterFirstBoardSubtitleStayDuration = 1.8f;
    public float afterFirstBoardSubtitleGapDuration = 0.25f;

    [Header("转向教程面板")]
    public CanvasGroupFader turnForwardIntroBoard;
    public CanvasGroupFader turnLeftButtonBoard;
    public CanvasGroupFader turnLeftInstructionBoard;
    public CanvasGroupFader turnForwardReturnButtonBoard;
    public float delayBeforeTurnBoards = 0.35f;
    public float delayBetweenTurnSteps = 0.35f;

    [Header("右转确认后的眼前字幕")]
    [TextArea(2, 4)] public string[] afterTurnSubtitleLines =
    {
        "很好 你已经学会了转向",
        "接下来我们来练习传送"
    };
    public float delayBeforeAfterTurnSubtitle = 0.35f;
    public float afterTurnSubtitleStayDuration = 1.8f;
    public float afterTurnSubtitleGapDuration = 0.25f;

    [Header("传送教程")]
    public CanvasGroupFader teleportHintOne;
    public CanvasGroupFader teleportPointOne;
    public CanvasGroupFader teleportHintTwo;
    public CanvasGroupFader teleportPointTwo;
    public CanvasGroupFader teleportPointToTable;
    public CanvasGroupFader tableBackHint;
    public CanvasGroupFader characterSelectRoot;
    public CanvasGroupFader tableConfirmPanel;
    [TextArea(2, 4)] public string[] tableArrivalSubtitleLines =
    {
        "你已经来到角色桌前",
        "请看向面前的提示"
    };
    [TextArea(2, 4)] public string[] finalSubtitleLines =
    {
        "做得很好",
        "现在正式进入游戏房间"
    };
    public float delayBeforeTeleportHintOne = 0.35f;
    public float delayBeforeTeleportHintTwo = 0.35f;
    public float delayBeforeTeleportToTableObjects = 0.35f;
    public float delayBeforeTableArrivalSubtitle = 0.2f;
    public float tableArrivalSubtitleStayDuration = 1.8f;
    public float tableArrivalSubtitleGapDuration = 0.25f;
    public float delayBeforeTableConfirmPanel = 5f;
    public float finalSubtitleStayDuration = 1.8f;
    public float finalSubtitleGapDuration = 0.25f;
    public string gameplaySceneName = "Indoor Scene(0.3）";

    [Header("脚下标志")]
    public CanvasGroupFader footMarker;

    [Header("自动播放")]
    public bool playOnStart = true;

    public GuideStage CurrentStage { get; private set; } = GuideStage.Idle;

    private Coroutine introSequenceCoroutine;
    private bool hasPlayed;
    private bool firstBoardConfirmed;
    private bool turnLeftConfirmed;
    private bool turnRightConfirmed;
    private bool teleportOneReached;
    private bool teleportTwoReached;
    private bool teleportTableReached;
    private bool tablePanelConfirmed;
    private bool skipTriggered;
    private bool leftPrimaryButtonWasPressed;
    private bool leftSecondaryButtonWasPressed;

    void Start()
    {
        HideAllImmediate();

        if (playOnStart)
            Play();
    }

    void Update()
    {
        if (skipTriggered || CurrentStage == GuideStage.Completed)
            return;

        if (GetLeftPrimaryButtonDown())
        {
            SkipToGameplayScene();
            return;
        }

        if (GetLeftSecondaryButtonDown())
            Play();
    }

    [ContextMenu("Play Intro Sequence")]
    public void Play()
    {
        if (hasPlayed)
            return;

        hasPlayed = true;
        if (footMarker != null) footMarker.FadeOut();
        introSequenceCoroutine = StartCoroutine(PlayIntroRoutine());
    }

    [ContextMenu("Replay Intro Sequence")]
    public void Replay()
    {
        StopAllCoroutines();
        introSequenceCoroutine = null;
        hasPlayed = false;
        firstBoardConfirmed = false;
        turnLeftConfirmed = false;
        turnRightConfirmed = false;
        teleportOneReached = false;
        teleportTwoReached = false;
        teleportTableReached = false;
        tablePanelConfirmed = false;
        skipTriggered = false;
        leftPrimaryButtonWasPressed = false;
        leftSecondaryButtonWasPressed = false;
        CurrentStage = GuideStage.Idle;
        HideAllImmediate();
        if (footMarker != null) footMarker.FadeIn();
        Play();
    }

    public void ConfirmFirstBoard()
    {
        if (CurrentStage != GuideStage.FirstBoard || firstBoardConfirmed)
            return;

        firstBoardConfirmed = true;

        if (introSequenceCoroutine != null)
        {
            StopCoroutine(introSequenceCoroutine);
            introSequenceCoroutine = null;
        }

        StartCoroutine(HandleFirstBoardConfirmedRoutine());
    }

    public void ConfirmTurnLeftStep()
    {
        if (CurrentStage != GuideStage.TurnLeftStep || turnLeftConfirmed)
            return;

        turnLeftConfirmed = true;
        StartCoroutine(HandleTurnLeftConfirmedRoutine());
    }

    public void ConfirmTurnRightStep()
    {
        if (CurrentStage != GuideStage.TurnRightStep || turnRightConfirmed)
            return;

        turnRightConfirmed = true;
        StartCoroutine(HandleTurnRightConfirmedRoutine());
    }

    public void ReachTeleportPointOne()
    {
        if (CurrentStage != GuideStage.TeleportHintOne || teleportOneReached)
            return;

        teleportOneReached = true;
        StartCoroutine(HandleTeleportPointOneReachedRoutine());
    }

    public void ReachTeleportPointTwo()
    {
        if (CurrentStage != GuideStage.TeleportHintTwo || teleportTwoReached)
            return;

        teleportTwoReached = true;
        StartCoroutine(HandleTeleportPointTwoReachedRoutine());
    }

    public void ReachTeleportTablePoint()
    {
        if (CurrentStage != GuideStage.TeleportToTable || teleportTableReached)
            return;

        teleportTableReached = true;
        StartCoroutine(HandleTeleportTableReachedRoutine());
    }

    public void ConfirmTablePanel()
    {
        if (CurrentStage != GuideStage.TablePanel || tablePanelConfirmed)
            return;

        tablePanelConfirmed = true;
        StartCoroutine(HandleTablePanelConfirmedRoutine());
    }

    private void SkipToGameplayScene()
    {
        if (string.IsNullOrWhiteSpace(gameplaySceneName))
            return;

        skipTriggered = true;
        StopAllCoroutines();
        introSequenceCoroutine = null;
        HideAllImmediate();
        if (footMarker != null) footMarker.SetImmediate(false);
        CurrentStage = GuideStage.Completed;
        SceneTransitionFader.Instance.FadeToBlackThenLoad(gameplaySceneName);
    }

    private bool GetLeftPrimaryButtonDown()
    {
        InputDevice leftHandDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (!leftHandDevice.isValid)
        {
            leftPrimaryButtonWasPressed = false;
            return false;
        }

        bool isPressed = false;
        if (!leftHandDevice.TryGetFeatureValue(CommonUsages.primaryButton, out isPressed))
        {
            leftPrimaryButtonWasPressed = false;
            return false;
        }

        bool wasPressed = leftPrimaryButtonWasPressed;
        leftPrimaryButtonWasPressed = isPressed;
        return isPressed && !wasPressed;
    }

    private bool GetLeftSecondaryButtonDown()
    {
        InputDevice leftHandDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (!leftHandDevice.isValid)
        {
            leftSecondaryButtonWasPressed = false;
            return false;
        }

        bool isPressed = false;
        if (!leftHandDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out isPressed))
        {
            leftSecondaryButtonWasPressed = false;
            return false;
        }

        bool wasPressed = leftSecondaryButtonWasPressed;
        leftSecondaryButtonWasPressed = isPressed;
        return isPressed && !wasPressed;
    }


    private void HideAllImmediate()
    {
        if (subtitlePanel != null) subtitlePanel.SetImmediate(false);
        if (firstBoard != null) firstBoard.SetImmediate(false);
        if (leftHintText != null) leftHintText.SetImmediate(false);
        if (rightHintText != null) rightHintText.SetImmediate(false);
        if (turnForwardIntroBoard != null) turnForwardIntroBoard.SetImmediate(false);
        if (turnLeftButtonBoard != null) turnLeftButtonBoard.SetImmediate(false);
        if (turnLeftInstructionBoard != null) turnLeftInstructionBoard.SetImmediate(false);
        if (turnForwardReturnButtonBoard != null) turnForwardReturnButtonBoard.SetImmediate(false);
        if (teleportHintOne != null) teleportHintOne.SetImmediate(false);
        if (teleportPointOne != null) teleportPointOne.SetImmediate(false);
        if (teleportHintTwo != null) teleportHintTwo.SetImmediate(false);
        if (teleportPointTwo != null) teleportPointTwo.SetImmediate(false);
        if (teleportPointToTable != null) teleportPointToTable.SetImmediate(false);
        if (tableBackHint != null) tableBackHint.SetImmediate(false);
        if (characterSelectRoot != null) characterSelectRoot.SetImmediate(false);
        if (tableConfirmPanel != null) tableConfirmPanel.SetImmediate(false);
    }

    private IEnumerator PlayIntroRoutine()
    {
        CurrentStage = GuideStage.IntroSubtitle;

        if (delayBeforeIntroSubtitle > 0f)
            yield return new WaitForSeconds(delayBeforeIntroSubtitle);

        yield return PlaySubtitleLines(introSubtitleLines, introSubtitleStayDuration, introSubtitleGapDuration);

        if (delayAfterIntroSubtitle > 0f)
            yield return new WaitForSeconds(delayAfterIntroSubtitle);

        if (boardDelay > 0f)
            yield return new WaitForSeconds(boardDelay);

        CurrentStage = GuideStage.FirstBoard;

        if (firstBoard != null)
            firstBoard.FadeIn();

        if (hintGap > 0f)
            yield return new WaitForSeconds(hintGap);

        if (leftHintText != null)
            leftHintText.FadeIn();

        if (hintGap > 0f)
            yield return new WaitForSeconds(hintGap);

        if (rightHintText != null)
            rightHintText.FadeIn();

        introSequenceCoroutine = null;
    }

    private IEnumerator HandleFirstBoardConfirmedRoutine()
    {
        if (firstBoard != null)
            firstBoard.FadeOut();

        if (leftHintText != null)
            leftHintText.FadeOut();

        if (rightHintText != null)
            rightHintText.FadeOut();

        if (delayBeforeAfterFirstBoardSubtitle > 0f)
            yield return new WaitForSeconds(delayBeforeAfterFirstBoardSubtitle);

        CurrentStage = GuideStage.TurnTutorialSubtitle;
        yield return PlaySubtitleLines(afterFirstBoardSubtitleLines, afterFirstBoardSubtitleStayDuration, afterFirstBoardSubtitleGapDuration);

        if (delayBeforeTurnBoards > 0f)
            yield return new WaitForSeconds(delayBeforeTurnBoards);

        CurrentStage = GuideStage.TurnLeftStep;

        if (turnForwardIntroBoard != null)
            turnForwardIntroBoard.FadeIn();

        if (turnLeftButtonBoard != null)
            turnLeftButtonBoard.FadeIn();
    }

    private IEnumerator HandleTurnLeftConfirmedRoutine()
    {
        if (turnForwardIntroBoard != null)
            turnForwardIntroBoard.FadeOut();

        if (turnLeftButtonBoard != null)
            turnLeftButtonBoard.FadeOut();

        if (delayBetweenTurnSteps > 0f)
            yield return new WaitForSeconds(delayBetweenTurnSteps);

        CurrentStage = GuideStage.TurnRightStep;

        if (turnLeftInstructionBoard != null)
            turnLeftInstructionBoard.FadeIn();

        if (turnForwardReturnButtonBoard != null)
            turnForwardReturnButtonBoard.FadeIn();
    }

    private IEnumerator HandleTurnRightConfirmedRoutine()
    {
        if (turnLeftInstructionBoard != null)
            turnLeftInstructionBoard.FadeOut();

        if (turnForwardReturnButtonBoard != null)
            turnForwardReturnButtonBoard.FadeOut();

        if (delayBeforeAfterTurnSubtitle > 0f)
            yield return new WaitForSeconds(delayBeforeAfterTurnSubtitle);

        CurrentStage = GuideStage.AfterTurnSubtitle;
        yield return PlaySubtitleLines(afterTurnSubtitleLines, afterTurnSubtitleStayDuration, afterTurnSubtitleGapDuration);

        if (delayBeforeTeleportHintOne > 0f)
            yield return new WaitForSeconds(delayBeforeTeleportHintOne);

        CurrentStage = GuideStage.TeleportHintOne;

        if (teleportHintOne != null)
            teleportHintOne.FadeIn();

        if (teleportPointOne != null)
            teleportPointOne.FadeIn();
    }

    private IEnumerator HandleTeleportPointOneReachedRoutine()
    {
        if (teleportHintOne != null)
            teleportHintOne.FadeOut();

        if (teleportPointOne != null)
            teleportPointOne.FadeOut();

        if (delayBeforeTeleportHintTwo > 0f)
            yield return new WaitForSeconds(delayBeforeTeleportHintTwo);

        CurrentStage = GuideStage.TeleportHintTwo;

        if (teleportHintTwo != null)
            teleportHintTwo.FadeIn();

        if (teleportPointTwo != null)
            teleportPointTwo.FadeIn();
    }

    private IEnumerator HandleTeleportPointTwoReachedRoutine()
    {
        if (teleportHintTwo != null)
            teleportHintTwo.FadeOut();

        if (teleportPointTwo != null)
            teleportPointTwo.FadeOut();

        if (delayBeforeTeleportToTableObjects > 0f)
            yield return new WaitForSeconds(delayBeforeTeleportToTableObjects);

        CurrentStage = GuideStage.TeleportToTable;

        if (tableBackHint != null)
            tableBackHint.FadeIn();

        if (teleportPointToTable != null)
            teleportPointToTable.FadeIn();

        if (characterSelectRoot != null)
            characterSelectRoot.FadeIn();
    }

    private IEnumerator HandleTeleportTableReachedRoutine()
    {
        if (delayBeforeTableArrivalSubtitle > 0f)
            yield return new WaitForSeconds(delayBeforeTableArrivalSubtitle);

        CurrentStage = GuideStage.TableArrivalSubtitle;
        yield return PlaySubtitleLines(tableArrivalSubtitleLines, tableArrivalSubtitleStayDuration, tableArrivalSubtitleGapDuration);

        if (delayBeforeTableConfirmPanel > 0f)
            yield return new WaitForSeconds(delayBeforeTableConfirmPanel);

        CurrentStage = GuideStage.TablePanel;

        if (tableConfirmPanel != null)
            tableConfirmPanel.FadeIn();
    }

    private IEnumerator HandleTablePanelConfirmedRoutine()
    {
        if (tableConfirmPanel != null)
            tableConfirmPanel.FadeOut();

        if (teleportPointToTable != null)
            teleportPointToTable.FadeOut();

        if (tableBackHint != null)
            tableBackHint.FadeOut();

        if (characterSelectRoot != null)
            characterSelectRoot.FadeOut();

        CurrentStage = GuideStage.FinalSubtitle;
        yield return PlaySubtitleLines(finalSubtitleLines, finalSubtitleStayDuration, finalSubtitleGapDuration);

        CurrentStage = GuideStage.Completed;

        if (!string.IsNullOrWhiteSpace(gameplaySceneName))
            SceneTransitionFader.Instance.FadeToBlackThenLoad(gameplaySceneName);
    }

    private IEnumerator PlaySubtitleLines(string[] lines, float stayDuration, float gapDuration)
    {
        if (subtitlePanel == null || subtitleText == null || lines == null)
            yield break;

        for (int i = 0; i < lines.Length; i++)
        {
            subtitleText.text = lines[i];
            subtitlePanel.FadeIn();

            if (stayDuration > 0f)
                yield return new WaitForSeconds(stayDuration);

            subtitlePanel.FadeOut();

            bool isLastLine = i == lines.Length - 1;
            if (!isLastLine && gapDuration > 0f)
                yield return new WaitForSeconds(gapDuration);
        }
    }
}
