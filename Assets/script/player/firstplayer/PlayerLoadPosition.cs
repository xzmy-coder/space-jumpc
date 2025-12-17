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

        // 判断是否有读档数据
        bool hasSaveData = Save.LoadPosition != Vector3.zero && !Mathf.Approximately(Save.LoadPosition.magnitude, 0);

        if (hasSaveData)
        {
            Debug.Log($"准备读档，目标位置：{Save.LoadPosition}");

            

           
            CharacterController cc = GetComponent<CharacterController>();

            
            if (cc != null)
            {
                cc.enabled = false;
            }

           
            transform.position = Save.LoadPosition;

          
            if (cc != null)
            {
                cc.enabled = true;
            }

           

            Debug.Log($"玩家位置已修正为：{transform.position}");

           
            Invoke(nameof(ClearLoadPosition), 0.1f);
        }
        else
        {
            Debug.Log($"无读档位置，使用默认位置：{transform.position}");
        }
    }

    private void ClearLoadPosition()
    {
        Save.LoadPosition = Vector3.zero;
    }
}