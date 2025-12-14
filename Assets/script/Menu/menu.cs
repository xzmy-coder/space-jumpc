using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class menu : MonoBehaviour
{

    public void ExitGame()
    {
        Application.Quit();
    }

    public void OpenGameSettingUI()
    {
        GameObject mainMenu = GameObject.FindGameObjectWithTag("mainmenu").gameObject;
        GameObject gameSettingUI = GameObject.FindGameObjectWithTag("gamesettingui").gameObject;
        mainMenu.transform.GetChild(0).gameObject.SetActive(false);
        gameSettingUI.transform.GetChild(0).gameObject.SetActive(true);
    }

    public void OpenGameLoadUI()
    {
        GameObject mainMenu = GameObject.FindGameObjectWithTag("mainmenu").gameObject;
        GameObject gameLoadUI = GameObject.FindGameObjectWithTag("Gameload").gameObject;
        mainMenu.transform.GetChild(0).gameObject.SetActive(false);
        gameLoadUI.transform.GetChild(0).gameObject.SetActive(true);
    }
}
