using UnityEngine;
using SaveSystemTutorial; // 引用你的存档命名空间

public class GamePauseManager : MonoBehaviour
{
    [Header("UI 设置")]
    public GameObject pauseMenuUI; // 拖入你在Canvas里做好的暂停界面面板

    private bool isPaused = false;

    void Update()
    {
        // 监听 ESC 键
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

    // --- 基础暂停功能 ---

    public void PauseGame()
    {
        pauseMenuUI.SetActive(true); // 显示UI
        Time.timeScale = 0f;         // 暂停时间（物理、动画都会停）
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

    // --- 核心：按钮绑定的回档功能 ---

    public void LoadLastSavePoint()
    {
        // 1. 读取存档数据
        SaveData saveData;
        if (SaveSystem.LoadData(out saveData))
        {
            // 2. 找到玩家对象 (假设玩家标签是 "Player")
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                Debug.Log($" 暂停界面回档：移动玩家到 {saveData.playerPosition}");

                // 3. 【再次应用核心技巧】处理 CharacterController
                CharacterController cc = player.GetComponent<CharacterController>();

                // 先打晕
                if (cc != null) cc.enabled = false;

                // 搬运位置
                player.transform.position = saveData.playerPosition;

                // 如果你之后加了旋转，这里也可以赋值：
                // player.transform.rotation = saveData.playerRotation; 

                // 叫醒
                if (cc != null) cc.enabled = true;

                Debug.Log("回档完成");
            }
            else
            {
                Debug.LogError("回档失败：场景中找不到标签为 Player 的对象！");
            }

            // 4. 回档后自动继续游戏（关闭暂停界面）
            ResumeGame();
        }
        else
        {
            Debug.LogWarning("没有找到存档记录，无法回档");
            // 可选：在这里给玩家弹一个提示框说“没有存档”
        }
        GameObject endText = GameObject.FindGameObjectWithTag("EndText").gameObject.transform.GetChild(0).gameObject;
        if (endText != null)
        { 
            endText.SetActive(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // 退出到主菜单（附赠功能，如有需要可绑定）
    public void QuitToMainMenu()
    {
        Time.timeScale = 1f; // 切场景前必须恢复时间，否则新场景也是暂停的
        UnityEngine.SceneManagement.SceneManager.LoadScene(0); // 假设主菜单是 index 0
    }
}