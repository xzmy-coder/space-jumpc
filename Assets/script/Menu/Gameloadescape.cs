using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gameloadescape : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        CloseLoadUI();
    }

    public void CloseLoadUI()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            GameObject mainMenu = GameObject.FindGameObjectWithTag("mainmenu").gameObject;
            GameObject gameLoadUI = GameObject.FindGameObjectWithTag("Gameload").gameObject;
            mainMenu.transform.GetChild(0).gameObject.SetActive(true);
            gameLoadUI.transform.GetChild(0).gameObject.SetActive(false);
        }
    }
}
