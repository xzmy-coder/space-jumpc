using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Volumecontrol : MonoBehaviour
{
    // Start is called before the first frame update
    private AudioSource menuaudio;
    private Slider audioslider;
    void Start()
    {
        menuaudio = GameObject.FindGameObjectWithTag("MenuMusic").GetComponent<AudioSource>();
        
        audioslider = GetComponent<Slider>();
    }

    // Update is called once per frame
    void Update()
    {
        VolumeControl();
        CloseGameSettingUI();
    }

    public void VolumeControl()
    {
        menuaudio.volume = audioslider.value;
    }

    public void CloseGameSettingUI()
    {
        if (Input.GetKey(KeyCode.Tab))
        {
            GameObject mainMenu = GameObject.FindGameObjectWithTag("MainMenuRoot").gameObject;
            GameObject gameSettingUI = GameObject.FindGameObjectWithTag("GameSettingUIRoot").gameObject;
            mainMenu.transform.GetChild(0).gameObject.SetActive(true);
            gameSettingUI.transform.GetChild(0).gameObject.SetActive(false);
        }
        
    }
}
