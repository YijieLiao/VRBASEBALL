using System;
using TMPro;
using UnityEngine;

public class VirtualKeyboard : MonoBehaviour
{
    [Header("显示")]
    [SerializeField] private TMP_Text displayText;
    [SerializeField] private int maxLength = 12;

    [Header("键盘容器")]
    [SerializeField] private CanvasGroup canvasGroup;

    public event Action<string> OnSubmit;
    public string CurrentText { get; private set; } = "";

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        Clear();
    }

    public void Clear()
    {
        CurrentText = "";
        UpdateDisplay();
    }

    // ==================== 按钮回调 — 字母 ====================

    public void TypeChar(string c)
    {
        if (CurrentText.Length >= maxLength) return;
        CurrentText += c;
        UpdateDisplay();
    }

    public void TypeA() => TypeChar("A");
    public void TypeB() => TypeChar("B");
    public void TypeC() => TypeChar("C");
    public void TypeD() => TypeChar("D");
    public void TypeE() => TypeChar("E");
    public void TypeF() => TypeChar("F");
    public void TypeG() => TypeChar("G");
    public void TypeH() => TypeChar("H");
    public void TypeI() => TypeChar("I");
    public void TypeJ() => TypeChar("J");
    public void TypeK() => TypeChar("K");
    public void TypeL() => TypeChar("L");
    public void TypeM() => TypeChar("M");
    public void TypeN() => TypeChar("N");
    public void TypeO() => TypeChar("O");
    public void TypeP() => TypeChar("P");
    public void TypeQ() => TypeChar("Q");
    public void TypeR() => TypeChar("R");
    public void TypeS() => TypeChar("S");
    public void TypeT() => TypeChar("T");
    public void TypeU() => TypeChar("U");
    public void TypeV() => TypeChar("V");
    public void TypeW() => TypeChar("W");
    public void TypeX() => TypeChar("X");
    public void TypeY() => TypeChar("Y");
    public void TypeZ() => TypeChar("Z");
    public void Type0() => TypeChar("0");
    public void Type1() => TypeChar("1");
    public void Type2() => TypeChar("2");
    public void Type3() => TypeChar("3");
    public void Type4() => TypeChar("4");
    public void Type5() => TypeChar("5");
    public void Type6() => TypeChar("6");
    public void Type7() => TypeChar("7");
    public void Type8() => TypeChar("8");
    public void Type9() => TypeChar("9");
    public void TypeSpace() => TypeChar(" ");

    // ==================== 功能键 ====================

    public void Backspace()
    {
        if (CurrentText.Length == 0) return;
        CurrentText = CurrentText.Substring(0, CurrentText.Length - 1);
        UpdateDisplay();
    }

    public void Submit()
    {
        string result = CurrentText.Trim();
        OnSubmit?.Invoke(result);
    }

    // ==================== 辅助 ====================

    private void UpdateDisplay()
    {
        if (displayText != null)
            displayText.text = string.IsNullOrEmpty(CurrentText) ? "_" : CurrentText;
    }
}
