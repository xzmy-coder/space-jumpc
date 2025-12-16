using SaveSystemTutorial;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerLoadPosition : MonoBehaviour
{
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 只在游戏场景（场景1）执行
        if (scene.buildIndex != 1) return;

        // 判断是否有读档数据 (建议配合我上一条回答中的 bool 开关方案，这里先兼容你原有的判断方式)
        bool hasSaveData = Save.LoadPosition != Vector3.zero && !Mathf.Approximately(Save.LoadPosition.magnitude, 0);

        if (hasSaveData)
        {
            Debug.Log($"🎮 准备读档，目标位置：{Save.LoadPosition}");

            // ================= 核心修复代码开始 =================

            // 1. 获取 CharacterController 组件
            CharacterController cc = GetComponent<CharacterController>();

            // 2. 如果存在，必须先禁用！(打晕它)
            if (cc != null)
            {
                cc.enabled = false;
            }

            // 3. 放心大胆地赋值位置 (搬运)
            transform.position = Save.LoadPosition;

            // 4. 重新启用 (叫醒它)
            if (cc != null)
            {
                cc.enabled = true;
            }

            // ================= 核心修复代码结束 =================

            Debug.Log($"✅ 玩家位置已修正为：{transform.position}");

            // 延迟清空
            Invoke(nameof(ClearLoadPosition), 0.1f);
        }
        else
        {
            Debug.Log($"🎮 无读档位置，使用默认位置：{transform.position}");
        }
    }

    private void ClearLoadPosition()
    {
        Save.LoadPosition = Vector3.zero;
    }
}