using UnityEngine;

[ExecuteAlways]
public class ChangeJerseyNumber : MonoBehaviour
{
    [Header("衣服模型")]
    public Renderer characterRenderer;

    [Header("对应的数字材质球")]
    // 比如：直接把 Project 窗口里的 rednum 材质球拖进来
    public Material targetNumberMaterial;

    [Header("球衣号码")]
    [Range(1, 9)]
    public int currentNumber = 1;

    private MaterialPropertyBlock propBlock;

    void OnValidate()
    {
        UpdateNumber();
    }

    void Start()
    {
        UpdateNumber();
    }

    public void UpdateNumber()
    {
        if (characterRenderer == null || targetNumberMaterial == null) return;

        // --- 核心逻辑：自动寻找序号 ---
        int foundIndex = -1;
        // 获取模型身上所有的材质列表
        Material[] sharedMaterials = characterRenderer.sharedMaterials;

        for (int i = 0; i < sharedMaterials.Length; i++)
        {
            // 如果模型身上的第 i 个材质，就是你拖进来的那个材质
            if (sharedMaterials[i] == targetNumberMaterial)
            {
                foundIndex = i; // 找到了！
                break;
            }
        }

        // 如果没找到，说明你拖错材质了，或者这件衣服没用这个材质
        if (foundIndex == -1)
        {
            // 可以在控制台输出一个提示，方便你排查
            // Debug.LogWarning("在这件衣服上没找到对应的数字材质球！");
            return;
        }

        // --- 以下是修改偏移的代码 ---
        if (propBlock == null) propBlock = new MaterialPropertyBlock();

        // 精准对准我们刚刚“搜”出来的那个序号
        characterRenderer.GetPropertyBlock(propBlock, foundIndex);

        int index = currentNumber - 1;
        int col = index % 3;
        int row = index / 3;

        float offsetX = col * 0.33333f;
        float offsetY = -row * 0.33333f;

        propBlock.SetVector("_MainTex_ST", new Vector4(1, 1, offsetX, offsetY));
        propBlock.SetVector("_BaseMap_ST", new Vector4(1, 1, offsetX, offsetY));

        characterRenderer.SetPropertyBlock(propBlock, foundIndex);
    }
}