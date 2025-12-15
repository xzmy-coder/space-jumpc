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

    // 场景加载完成后赋值位置（确保时机正确）
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 只在游戏场景（场景1）执行
        if (scene.buildIndex != 1) return;

        // 有读档位置则赋值，无则默认
        if (Save.LoadPosition != Vector3.zero && !Mathf.Approximately(Save.LoadPosition.magnitude, 0))
        {
            transform.position = Save.LoadPosition;
            Debug.Log($"玩家位置赋值：{transform.position}");
            // 延迟清空，避免重复赋值
            Invoke(nameof(ClearLoadPosition), 0.1f);
        }
        else
        {
            Debug.Log($"无读档位置，使用默认位置：{transform.position}");
        }
    }

    // 清空读档位置
    private void ClearLoadPosition()
    {
        Save.LoadPosition = Vector3.zero;
    }
}