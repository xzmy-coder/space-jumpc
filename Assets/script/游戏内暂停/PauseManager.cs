using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using SaveSystemTutorial; // 核心：保留命名空间，删除重复类

public class PauseManager : MonoBehaviour
{
    // ====================== 仅需改这2处！======================
    private int mainMenuIndex = 0;       // 改成你的主菜单场景索引（比如1/2）
    // ==========================================================

    public GameObject pausePanel;        // 拖入暂停面板对象
    public GameObject player;            // 手动拖入Hierarchy里的玩家对象（必拖！）
    private bool isPaused = false;

    void Awake()
    {
        // 核心：跨场景不销毁，解决主菜单二次返回失效
        DontDestroyOnLoad(gameObject);
        // 监听场景加载，保障主菜单激活
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Update()
    {
        // 监听ESC键切换暂停/继续
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    // 暂停游戏：解锁鼠标+显示UI+暂停时间
    void PauseGame()
    {
        isPaused = true;
        pausePanel.SetActive(true);
        Time.timeScale = 0;

        // 解决鼠标消失问题
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        EventSystem.current.sendNavigationEvents = true;
    }

    // 继续游戏：恢复时间+隐藏UI
    public void ResumeGame()
    {
        isPaused = false;
        pausePanel.SetActive(false);
        Time.timeScale = 1;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // 回到主菜单（修复多次返回消失）
    public void BackToMainMenu()
    {
        // 强制恢复游戏状态
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 加载主菜单（Single模式清空场景）
        SceneManager.LoadScene(mainMenuIndex, LoadSceneMode.Single);
    }

    // 回到上一次存档（修复无效+编译错误）
    public void LoadLastSave()
    {
        // 1. 强制恢复游戏状态
        Time.timeScale = 1;
        Cursor.visible = true;
        pausePanel.SetActive(false);
        isPaused = false;

        // 2. 直接读取存档（避免封装层问题）
        string saveJson = PlayerPrefs.GetString("DefaultSave", "");
        if (string.IsNullOrEmpty(saveJson))
        {
            Debug.Log("无存档数据！请先走到存档点完成存档");
            return;
        }

        // 3. 解析存档位置（使用命名空间的SaveData，解决重复类错误）
        SaveSystemTutorial.SaveData saveData = JsonUtility.FromJson<SaveSystemTutorial.SaveData>(saveJson);
        if (saveData == null || saveData.playerPosition == Vector3.zero)
        {
            Debug.Log("存档数据损坏或位置无效！");
            return;
        }

        // 4. 手动拖入玩家，100%找到并赋值位置
        if (player != null)
        {
            player.transform.position = saveData.playerPosition;
            Debug.Log("回档成功！玩家位置：" + saveData.playerPosition);
        }
        else
        {
            Debug.Log("请把玩家对象拖入PauseManager的player字段！");
        }
    }

    // 场景加载回调：兜底激活主菜单
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex == mainMenuIndex)
        {
            GameObject mainMenu = GameObject.FindGameObjectWithTag("MainMenuRoot");
            if (mainMenu != null)
            {
                mainMenu.SetActive(true);
                Debug.Log("主菜单已强制激活");
            }
        }
    }

    // 移除监听，防止内存泄漏
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}