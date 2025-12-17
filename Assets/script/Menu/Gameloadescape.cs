using UnityEngine;

public class Gameloadescape : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseLoadUI();
        }
    }

    public void CloseLoadUI()
    {
        // 查找主菜单根对象
        GameObject mainMenu = GameObject.FindGameObjectWithTag("MainMenuRoot");
        // 查找读档UI根对象
        GameObject gameLoadUI = GameObject.FindGameObjectWithTag("GameLoadUIRoot");

        // 直接控制根对象显示/隐藏
        if (mainMenu != null) mainMenu.SetActive(true);
        if (gameLoadUI != null) gameLoadUI.SetActive(false);

        Debug.Log("ESC关闭读档UI：主菜单显示，读档UI隐藏");
    }
}