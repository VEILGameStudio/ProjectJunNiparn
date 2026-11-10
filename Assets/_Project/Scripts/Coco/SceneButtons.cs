using UnityEngine;

public class SceneButtons : MonoBehaviour
{
    // ใส่ชื่อซีนที่ต้องการ
    public void GoToScene(string sceneName)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("SceneButtons: ไม่พบ GameManager ในซีนนี้ (ต้องมี Managers)");
            return;
        }

        // ส่ง spawn point ว่าง = ไม่ย้ายตัวละคร
        GameManager.Instance.SceneLoader.LoadScene(sceneName, "");
    }
}