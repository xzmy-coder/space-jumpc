using UnityEngine;
using UnityEngine.SceneManagement;
using SaveSystemTutorial;

public class menu : MonoBehaviour
{
    // 退出游戏
    public void ExitGame()
    {
        Debug.Log("退出游戏");
        Application.Quit();
    }

    // 打开设置UI（保留，无需可删除）
    public void OpenGameSettingUI()
    {
        GameObject mainMenu = FindMainMenu().gameObject;
        GameObject settingUI = GameObject.FindGameObjectWithTag("GameSettingUIRoot").gameObject;

        mainMenu.transform.GetChild(0).gameObject.SetActive(false);
        settingUI.transform.GetChild(0).gameObject.SetActive(true);
    }

    // 【核心】开始游戏：自动检测存档，有则读档，无则默认位置
    public void StartGame()
    {
        // 1. 隐藏主菜单
        GameObject mainMenu = FindMainMenu();
        if (mainMenu != null) mainMenu.SetActive(false);

        // 2. 检测存档，有则赋值读档位置
        SaveData saveData;
        if (SaveSystem.LoadData(out saveData))
        {
            Save.LoadPosition = saveData.playerPosition;
        }
        else
        {
            Save.LoadPosition = Vector3.zero; // 无存档则默认位置
        }

        // 3. 加载游戏场景（场景1）
        SceneManager.LoadScene(1);
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



#if UNITY_EDITOR
    // 编辑器菜单：清空存档（方便测试）
    [UnityEditor.MenuItem("Developer/清空存档数据")]
    private static void ClearSave()
    {
        SaveSystem.ClearSaveData();
    }
#endif
}