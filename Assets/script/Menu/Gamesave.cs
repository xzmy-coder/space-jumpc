using System.Collections;
using SaveSystemTutorial;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// 仅在Unity编辑器中引入Editor命名空间（打包时自动忽略）
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Save : MonoBehaviour
{
    public const string MENU_SAVE = "MenuSave";
    public GameObject save;
    public static string saveKey; // 存档/读档用的键
    public static Vector3 PlayerPosition;
    public static Vector3 LoadPosition;

    public static string SelectedSaveKey;

    [System.Serializable]
    class ButtonStateData
    {
        public string btnKey;
        public bool child1Active;
    }

    [System.Serializable]
    class SaveData
    {
        public Vector3 playerPosition;
    }

    void Start()
    {
        LoadButtonChild1State();
    }

    void Update() { }

    // 保留之前的SaveButtonChild1State/LoadButtonChild1State方法（不变）
    private void SaveButtonChild1State()
    {
        if (save == null) return;
        Transform child0 = save.transform.GetChild(0);
        if (child0 == null) return;
        TextMeshProUGUI keyText = child0.GetComponent<TextMeshProUGUI>();
        if (keyText == null) return;
        string btnKey = keyText.text;
        if (string.IsNullOrEmpty(btnKey)) return;

        ButtonStateData stateData = new ButtonStateData();
        stateData.btnKey = btnKey;
        stateData.child1Active = save.transform.GetChild(1).gameObject.activeSelf;

        string json = JsonUtility.ToJson(stateData);
        PlayerPrefs.SetString(MENU_SAVE + "_BtnState_" + btnKey, json);
        PlayerPrefs.Save();
    }

    private void LoadButtonChild1State()
    {
        save = this.gameObject;
        if (save == null) return;
        Transform child0 = save.transform.GetChild(0);
        if (child0 == null) return;
        TextMeshProUGUI keyText = child0.GetComponent<TextMeshProUGUI>();
        if (keyText == null) return;
        string btnKey = keyText.text;
        if (string.IsNullOrEmpty(btnKey)) return;

        string json = PlayerPrefs.GetString(MENU_SAVE + "_BtnState_" + btnKey, null);
        if (string.IsNullOrEmpty(json)) return;
        ButtonStateData stateData = JsonUtility.FromJson<ButtonStateData>(json);
        if (stateData == null) return;

        Transform child1 = save.transform.GetChild(1);
        if (child1 != null)
        {
            child1.gameObject.SetActive(stateData.child1Active);
        }
    }

    // 保留SaveGamePoint方法（改用SelectedSaveKey存档）
    public static void SaveGamePoint()
    {
        // 空值防护：确保选中了按钮键
        if (string.IsNullOrEmpty(Save.SelectedSaveKey))
        {
            Debug.LogError("未选中任何存档按钮！先点击存档按钮再存档");
            return;
        }
        var saveData = new SaveData();
        saveData.playerPosition = PlayerPosition;
        // 用“当前选中的按钮键”存档（核心修改）
        SaveSystem.SaveByPlayerPrefs(Save.SelectedSaveKey, saveData);
        Debug.Log($"存档成功！键：{Save.SelectedSaveKey}，位置：{PlayerPosition}");

        if (Instance != null && Instance.save != null)
        {
            Transform child1 = Instance.save.transform.GetChild(1);
            if (child1 != null)
            {
                child1.gameObject.SetActive(false);
                Instance.SaveButtonChild1State();
            }
        }
    }

    private static Save Instance;
    private void Awake()
    {
        Instance = this;
    }

    // 【核心修改】重写SaveOrLoad：点击按钮时先记录“选中键”
    public void SaveOrLoad()
    {
        save = this.gameObject;
        if (save == null) return;
        Transform child0 = save.transform.GetChild(0);
        if (child0 == null) return;
        TextMeshProUGUI keyText = child0.GetComponent<TextMeshProUGUI>();
        if (keyText == null) return;
        Transform child1 = save.transform.GetChild(1);
        if (child1 == null) return;

        // 1. 读取按钮文本，设为“当前选中键”（核心！点哪个按钮，就存哪个键）
        string btnText = keyText.text;
        Save.SelectedSaveKey = btnText;
        saveKey = btnText; // 兼容旧逻辑
        Debug.Log($"选中存档键：{Save.SelectedSaveKey}");

        // 2. 读档分支：子物体1未激活时
        if (!child1.gameObject.activeSelf)
        {
            var json = SaveSystem.LoadFromPlayerPrefs(saveKey);
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("读档失败：无存档数据！");
                SceneManager.LoadScene(1);
                return;
            }
            var saveData = JsonUtility.FromJson<SaveData>(json);
            if (saveData == null)
            {
                Debug.LogError("读档失败：数据解析错误！");
                SceneManager.LoadScene(1);
                return;
            }
            Save.LoadPosition = saveData.playerPosition;
            Debug.Log($"读档数据已存全局变量：{Save.LoadPosition}");
        }
        // 3. 首次点击分支：子物体1激活时
        else
        {
            child1.gameObject.SetActive(false);
            SaveButtonChild1State();
            Debug.Log("首次点击，子物体1已设为未激活");
        }

        // 4. 加载场景1
        SceneManager.LoadScene(1);
    }

    // 编辑器专属方法：清空PlayerPrefs数据（仅在编辑器中显示菜单）
#if UNITY_EDITOR
    [MenuItem("Developer/Delete Player Data Prefs")]
    public static void DeletePlayerDataPrefs()
    {
        PlayerPrefs.DeleteAll();
        Debug.Log("已清空所有PlayerPrefs数据");
    }
#endif
}