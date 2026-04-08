using UnityEngine;
using Michsky.UI.Dark; // 必须引用这个命名空间

public class DropdownController : MonoBehaviour
{
    // 在 Inspector 面板里把你的 CustomDropdown 物体拖到这里
    public CustomDropdown myDropdown;

    void Start()
    {
        if (myDropdown != null)
        {
            InitializeOptions();
        }
    }

    public void InitializeOptions()
    {
        // 1. 先清空默认的选项（防止你在编辑器里填了别的东西导致重复）
        myDropdown.dropdownItems.Clear();

        // 2. 创建并添加“强”
        AddOption("震动反馈:强");

        // 3. 创建并添加“中”
        AddOption("震动反馈:中");

        // 4. 创建并添加“弱”
        AddOption("震动反馈:弱");

        // 5. 【关键步骤】刷新 UI 表现
        myDropdown.SetupDropdown();

        // 6. 默认选中第一个（“强”）
        myDropdown.ChangeDropdownInfo(0);
    }

    // 这是一个方便添加选项的小方法
    private void AddOption(string title)
    {
        CustomDropdown.Item newItem = new CustomDropdown.Item();
        newItem.itemName = title;

        // 如果你需要点击选项后触发函数，可以在这里写：
        newItem.OnItemSelection = new UnityEngine.Events.UnityEvent();
        newItem.OnItemSelection.AddListener(() => OnDropdownSelected(title));

        myDropdown.dropdownItems.Add(newItem);
    }

    // 当你点击某个选项时，会执行这个函数
    void OnDropdownSelected(string name)
    {
        Debug.Log("你选择了: " + name);
    }
}