using UnityEngine;

namespace SaveSystemTutorial
{
    public static class SaveSystem
    {
        // 固定存档键（无需按钮，全程用这个键存档/读档）
        public const string DEFAULT_SAVE_KEY = "DefaultSave";

        // 保存数据（返回是否成功）
        public static bool SaveData(object data)
        {
            if (data == null)
            {
                Debug.LogError("存档失败：数据为空！");
                return false;
            }

            try
            {
                string json = JsonUtility.ToJson(data);
                PlayerPrefs.SetString(DEFAULT_SAVE_KEY, json);
                PlayerPrefs.Save();
                Debug.Log($"存档成功！位置：{((SaveData)data).playerPosition}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"存档异常：{e.Message}");
                return false;
            }
        }

        // 加载数据（返回是否成功，out参数返回存档数据）
        public static bool LoadData(out SaveData saveData)
        {
            saveData = null;
            string json = PlayerPrefs.GetString(DEFAULT_SAVE_KEY, null);

            if (string.IsNullOrEmpty(json))
            {
                Debug.Log("无存档数据，将进入默认位置");
                return false;
            }

            try
            {
                saveData = JsonUtility.FromJson<SaveData>(json);
                if (saveData == null || saveData.playerPosition == Vector3.zero)
                {
                    Debug.LogError("读档失败：数据解析错误或位置无效");
                    return false;
                }
                Debug.Log($"读档成功！目标位置：{saveData.playerPosition}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"读档异常：{e.Message}");
                return false;
            }
        }

        // 清空存档（可选，编辑器用）
        public static void ClearSaveData()
        {
            PlayerPrefs.DeleteKey(DEFAULT_SAVE_KEY);
            PlayerPrefs.Save();
            Debug.Log("已清空存档数据");
        }
    }

    // 存档数据结构（仅保存玩家位置）
    [System.Serializable]
    public class SaveData
    {
        public Vector3 playerPosition;
        public long saveTime; // 存档时间戳（验证数据有效性）
    }
}