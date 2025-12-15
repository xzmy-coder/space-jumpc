using UnityEngine;

// 挂载到主菜单的MainMenu对象上，场景加载自动激活
public class MainMenuActivator : MonoBehaviour
{
    void Start()
    {
        // 强制激活主菜单，不管之前状态
        gameObject.SetActive(true);
        Debug.Log("主菜单已自激活");
    }

    void OnEnable()
    {
        // 二次保障：激活时强制显示
        gameObject.SetActive(true);
    }
}