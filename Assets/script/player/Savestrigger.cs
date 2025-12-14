using UnityEngine;

public class Savestrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 只响应存档点（DotsLoad标签）
        if (other.CompareTag("DotsLoad"))
        {
            // 空值防护：确保已选中存档按钮
            if (string.IsNullOrEmpty(Save.SelectedSaveKey))
            {
                Debug.LogError("❌ 请先点击存档按钮（如存档2），再走到存档点！");
                return;
            }
            // 赋值玩家当前位置（无需手动设键，用选中的键）
            Save.PlayerPosition = this.transform.position;
            // 执行存档（自动用选中的键，比如“存档2”）
            Save.SaveGamePoint();
            Debug.Log($"📍 存档点触发！使用选中键：{Save.SelectedSaveKey}，位置：{Save.PlayerPosition}");
        }
    }
}