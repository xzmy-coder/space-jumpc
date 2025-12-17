using SaveSystemTutorial;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class strigger1 : MonoBehaviour
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
            SceneManager.LoadScene(1);
        }
    }
}

