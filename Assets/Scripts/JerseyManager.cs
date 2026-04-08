using UnityEngine;

[ExecuteAlways]
public class JerseyManager : MonoBehaviour
{
    [Header("--- 1. 颜色选择 ---")]
    public JerseyPack currentPack;

    [Header("--- 2. 号码 ---")]
    [Range(1, 9)] public int number = 1;

    [Header("--- 3. 关联的模型 ---")]
    public Renderer jerseyRenderer;
    public Renderer hatRenderer;

    private MaterialPropertyBlock propBlock;

    [ContextMenu("手动刷新材质")]
    public void UpdateAppearance()
    {
        if (currentPack == null || jerseyRenderer == null) return;

        // --- 1. 处理衣服 ---
        Material[] mats = jerseyRenderer.sharedMaterials;

        for (int i = 0; i < mats.Length; i++)
        {
            // 【核心修复】：如果发现这个位置是空的(粉色原因)，先强行给它穿上底色材质
            if (mats[i] == null)
            {
                Debug.Log($"发现第 {i} 个位置是空的，已自动补全底色材质。");
                mats[i] = currentPack.jerseyBase;
            }

            string matName = mats[i].name.ToLower();

            if (matName.Contains("logo"))
            {
                if (currentPack.logo != null) mats[i] = currentPack.logo;
            }
            else if (matName.Contains("num"))
            {
                if (currentPack.number != null)
                {
                    mats[i] = currentPack.number;
                    UpdateOffset(i); // 更新数字偏移
                }
            }
            // 如果名字里包含 red/blue/jersey，或者它是我们刚刚补全的那个空材质
            else if (matName.Contains("red") || matName.Contains("blue") || matName.Contains("jersey") || mats[i] == currentPack.jerseyBase)
            {
                if (currentPack.jerseyBase != null) mats[i] = currentPack.jerseyBase;
            }
        }

        jerseyRenderer.sharedMaterials = mats;

        // --- 2. 处理帽子 ---
        if (hatRenderer != null)
        {
            Material targetHat = (currentPack.hat != null) ? currentPack.hat : currentPack.jerseyBase;
            if (targetHat != null) hatRenderer.sharedMaterial = targetHat;
        }
    }

    private void UpdateOffset(int slotIndex)
    {
        if (propBlock == null) propBlock = new MaterialPropertyBlock();
        jerseyRenderer.GetPropertyBlock(propBlock, slotIndex);
        float oX = ((number - 1) % 3) * 0.33333f;
        float oY = -((number - 1) / 3) * 0.33333f;
        propBlock.SetVector("_BaseMap_ST", new Vector4(1, 1, oX, oY));
        propBlock.SetVector("_MainTex_ST", new Vector4(1, 1, oX, oY));
        jerseyRenderer.SetPropertyBlock(propBlock, slotIndex);
    }

    void OnValidate() { UpdateAppearance(); }
    void Start() { UpdateAppearance(); }
}