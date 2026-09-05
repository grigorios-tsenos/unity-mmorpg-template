using MmoTemplate.Rpg;
using UnityEngine;
namespace MmoTemplate.Player
{
    public class CameraFollow : MonoBehaviour
    {
        public static CameraFollow Active { get; private set; }
        private void Awake() => Active = this;
        private void Start() { if (PlayerStats.Local != null) SetTarget(PlayerStats.Local.transform); }
        private Transform target;
        private float yaw, pitch = 28, distance = 7;
        public float Yaw => yaw;
        public void SetTarget(Transform value) { target = value; yaw = value.eulerAngles.y; lastTargetYaw = yaw; }
        private bool dragStartedOverUi;
        private float lastTargetYaw;
        private void LateUpdate()
        {
            if (target == null) return;
            bool held = Input.GetMouseButton(0) || Input.GetMouseButton(1);
            // A drag that begins on the HUD belongs to the UI: locking the cursor recentres it, so the
            // button never sees pointer-up on the element it was pressed on and onClick never fires.
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) dragStartedOverUi = GameEvents.PointerOverUi;
            if (!held) dragStartedOverUi = false;
            bool mouseLook = !GameEvents.InputBlocked && held && !dragStartedOverUi;
            Cursor.lockState = mouseLook ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !mouseLook;
            // Keyboard turning carries the camera around with the character so it stays behind them,
            // preserving any orbit offset the player set with the mouse. Right-drag is excluded: there
            // the character takes its heading from the camera, so following would double the rotation.
            float targetYaw = target.eulerAngles.y;
            float turned = Mathf.DeltaAngle(lastTargetYaw, targetYaw);
            lastTargetYaw = targetYaw;
            if (!(mouseLook && Input.GetMouseButton(1))) yaw += turned;
            if (mouseLook) { yaw += Input.GetAxis("Mouse X") * 3; pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * 2, -10, 70); }
            if (!GameEvents.InputBlocked && !GameEvents.PointerOverUi) distance = Mathf.Clamp(distance - Input.mouseScrollDelta.y, 2.5f, 11);
            Vector3 focus = target.position + Vector3.up * 1.5f;
            Quaternion rotation = Quaternion.Euler(pitch,yaw,0);
            Vector3 back = rotation * Vector3.back;
            float length = distance;
            // Ignore player/NPC colliders: only world geometry occludes the camera.
            if (Physics.SphereCast(focus,.2f,back,out var hit,distance,1,QueryTriggerInteraction.Ignore)) length = Mathf.Max(.4f,hit.distance - .15f);
            transform.SetPositionAndRotation(focus + back * length, rotation);
        }
        private void OnDisable() { if (Active == this) Active = null; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }
}
