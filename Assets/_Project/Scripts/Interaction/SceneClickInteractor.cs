using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// ตัวคลิกสำหรับซีนซูมใกล้ (ไม่มีตัวละคร)
// ยิงเมาส์ไปหากรอบ Interactable แล้วสั่ง Interact เหมือน PlayerInteractor
public class SceneClickInteractor : MonoBehaviour
{
    [Tooltip("The camera clicks are cast from. Leave empty to use the Main Camera.")]
    [SerializeField] private Camera interactionCamera;

    private void Awake()
    {
        if (interactionCamera == null)
        {
            interactionCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }
        if (interactionCamera == null)
        {
            return;
        }

        // ไม่รับคลิกที่ตกบน UI (เช่นหน้าต่างกระเป๋า)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
        {
            return;
        }

        Ray ray = interactionCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, GameLayers.InteractableMask, QueryTriggerInteraction.Collide))
        {
            return;
        }

        IInteractable target = hit.collider.GetComponentInParent<IInteractable>();
        if (target == null)
        {
            return;
        }
        if (target.Activation != ActivationMode.Click && target.Activation != ActivationMode.Both)
        {
            return;
        }

        Inventory inventory = GameManager.Instance != null ? GameManager.Instance.Inventory : null;

        // ใช้ตำแหน่งกล้องแทนตัวละคร เพราะซีนนี้ไม่มีตัวละคร (ระยะห่างจึงใกล้พอ)
        PlayerContext context = new PlayerContext(interactionCamera.transform, inventory, null);

        if (target.CanInteract(context))
        {
            target.Interact(context);
        }
    }
}