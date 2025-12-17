using UnityEngine;
using SaveSystemTutorial;

public class Savestrigger : MonoBehaviour
{
    // 玩家进入存档点触发
    private void OnTriggerEnter(Collider other)
    {
        // 仅响应玩家
        if (!other.CompareTag("Player")) return;

        // 赋值玩家当前位置
        Save.PlayerPosition = other.transform.position;

        // 构建存档数据
        SaveData saveData = new SaveData
        {
            playerPosition = Save.PlayerPosition,
            saveTime = System.DateTime.Now.Ticks
        };

        // 执行存档
        SaveSystem.SaveData(saveData);
    }
}