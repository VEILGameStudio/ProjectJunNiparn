using UnityEngine;
using UnityEngine.SceneManagement;

// สคริปต์ปุ่มออกจากซีนปริศนา กลับไปซีนหลัก
public class ExitPuzzleButton : MonoBehaviour
{
    [Tooltip("ชื่อซีนหลักที่จะกลับไป (ต้องตรงกับชื่อซีนเป๊ะ)")]
    [SerializeField] private string returnSceneName = "Test_Zones";

    // ผูกกับ OnClick ของปุ่ม
    public void Exit()
    {
        Scene returnScene = SceneManager.GetSceneByName(returnSceneName);

        if (returnScene.isLoaded)
        {
            // ซีนหลักเปิดอยู่แล้ว (ซีนนี้ถูกโหลดซ้อน): ปิดเฉพาะซีนปริศนานี้
            SceneManager.UnloadSceneAsync(gameObject.scene);
        }
        else
        {
            // ซีนหลักยังไม่ได้เปิด (สลับซีน หรือกด Play ซีนนี้โดดๆ): โหลดซีนหลักกลับมา
            SceneManager.LoadScene(returnSceneName);
        }
    }
}