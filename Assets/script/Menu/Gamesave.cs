using UnityEngine;

// 全局存档变量，跨场景保留
public class Save : MonoBehaviour
{
    // 读档位置（全局唯一）
    public static Vector3 LoadPosition;
    // 存档位置（玩家触发存档点时赋值）
    public static Vector3 PlayerPosition;

    // 单例实例（确保全局唯一）
    private static Save _instance;
    public static Save Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<Save>();
                if (_instance == null)
                {
                    GameObject saveObj = new GameObject("[SaveManager]");
                    _instance = saveObj.AddComponent<Save>();
                }
            }
            return _instance;
        }
    }

    // 确保跨场景不销毁
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        
    }
}