using UnityEngine;
using UnityEngine.SceneManagement;
using SaveSystemTutorial; // 确保引用命名空间

public class menu : MonoBehaviour
{
    // 自定义参数：游戏场景索引（改成你的游戏场景序号，比如1）
    private int gameSceneIndex = 1;

    // 退出游戏
    public void ExitGame()
    {
        Debug.Log("退出游戏");
        Application.Quit();
    }

    // 打开设置UI（保留，无需可删除）
    public void OpenGameSettingUI()
    {
        GameObject mainMenu = FindMainMenu();
        GameObject settingUI = GameObject.FindGameObjectWithTag("GameSettingUIRoot");

        if (mainMenu != null) mainMenu.SetActive(false);
        if (settingUI != null) settingUI.SetActive(true);
    }

    // 开始游戏：自动检测存档，匹配SaveData类型
    public void StartGame()
    {
        // 1. 隐藏主菜单
        GameObject mainMenu = FindMainMenu();
        if (mainMenu != null) mainMenu.SetActive(false);

        // 2. 检测存档（使用命名空间的SaveData，解决类型错误）
        SaveSystemTutorial.SaveData saveData;
        if (SaveSystem.LoadData(out saveData))
        {
            Save.LoadPosition = saveData.playerPosition;
        }
        else
        {
            Save.LoadPosition = Vector3.zero; // 无存档则默认位置
        }

        // 3. 加载游戏场景（Single模式，清空其他场景）
        SceneManager.LoadScene(gameSceneIndex, LoadSceneMode.Single);
        Debug.Log("开始游戏！");
    }

    // 查找主菜单（标签：MainMenuRoot）
    private GameObject FindMainMenu()
    {
        GameObject mainMenu = GameObject.FindGameObjectWithTag("MainMenuRoot");
        if (mainMenu == null)
        {
            Debug.LogError("未找到主菜单（标签：MainMenuRoot）");
        }
        return mainMenu;
    }
}