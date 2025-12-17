using UnityEngine;
using SaveSystemTutorial; 

public class GamePauseManager : MonoBehaviour
{
    [Header("UI 设置")]
    public GameObject pauseMenuUI; 

    private bool isPaused = false;

    void Update()
    {
        // 
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    // 基础暂停功能 

    public void PauseGame()
    {
        pauseMenuUI.SetActive(true); // 显示UI
        Time.timeScale = 0f;         // 暂停时间
        isPaused = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        pauseMenuUI.SetActive(false); // 隐藏UI
        Time.timeScale = 1f;          // 恢复时间
        isPaused = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // 按钮绑定的回档功能

    public void LoadLastSavePoint()
    {
        //  读取存档数据
        SaveData saveData;
        if (SaveSystem.LoadData(out saveData))
        {
            // 找到玩家对象
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                Debug.Log($" 暂停界面回档：移动玩家到 {saveData.playerPosition}");

                //处理 CharacterController
                CharacterController cc = player.GetComponent<CharacterController>();

                
                if (cc != null) cc.enabled = false;

                // 位置
                player.transform.position = saveData.playerPosition;

                
                // player.transform.rotation = saveData.playerRotation; 

               
                if (cc != null) cc.enabled = true;

                Debug.Log("回档完成");
            }
            else
            {
                Debug.LogError("回档失败：场景中找不到标签为 Player 的对象！");
            }

            // 4. 回档后自动继续游戏
            ResumeGame();
        }
        else
        {
            Debug.LogWarning("没有找到存档记录，无法回档");
           
        }
        GameObject endText = GameObject.FindGameObjectWithTag("EndText").gameObject.transform.GetChild(0).gameObject;
        if (endText != null)
        { 
            endText.SetActive(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // 退出到主菜单
    public void QuitToMainMenu()
    {
        Time.timeScale = 1f; // 切场景前必须恢复时间，否则新场景也是暂停的
        UnityEngine.SceneManagement.SceneManager.LoadScene(0); 
    }
}