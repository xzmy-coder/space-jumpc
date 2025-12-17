using SaveSystemTutorial;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class strigger : MonoBehaviour
{
    
    void Start()
    {
        
    }

   
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject==GameObject.FindGameObjectWithTag("ThirdEnd"))
        {
            SceneManager.LoadScene(2);
        }
        if(other.gameObject==GameObject.FindGameObjectWithTag("TheEnd"))
        {
            GameObject endText = GameObject.FindGameObjectWithTag("EndText").gameObject.transform.GetChild(0).gameObject;
            endText.SetActive(true);
            /// Û±Í≥ˆœ÷
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}

