using UnityEngine;

public class PlayerLoadPosition : MonoBehaviour
{
    // 玩家启动时执行（场景加载后第一时间赋值位置）
    void Start()
    {
        // 只有全局读档位置不是默认值时，才赋值
        if (Save.LoadPosition != Vector3.zero)
        {
            transform.position = Save.LoadPosition;
            Debug.Log($"🎯 玩家位置已赋值：{transform.position}");
            // 赋值后清空全局变量（避免下次启动重复赋值）
            Save.LoadPosition = Vector3.zero;
        }
        else
        {
            Debug.Log($"📌 无读档位置，使用默认位置：{transform.position}");
        }
    }
}