using UnityEngine;

[CreateAssetMenu(fileName = "NewJerseyPack", menuName = "JerseySystem/JerseyPack")]
public class JerseyPack : ScriptableObject
{
    public Material jerseyBase; // 衣服底色
    public Material logo;       // Logo
    public Material number;     // 数字
    public Material hat;        // 帽子
}