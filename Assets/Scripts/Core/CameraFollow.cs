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
        public void SetTarget(Transform value) { target = value; yaw = value.eulerAngles.y; }
        private void LateUpdate()
        {
            if (target == null) return;
            bool mouseLook = !GameEvents.InputBlocked && (Input.GetMouseButton(0) || Input.GetMouseButton(1));
            Cursor.lockState = mouseLook ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !mouseLook;
            if (mouseLook) { yaw += Input.GetAxis("Mouse X") * 3; pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * 2, -10, 70); }
            if (!GameEvents.InputBlocked) distance = Mathf.Clamp(distance - Input.mouseScrollDelta.y, 2.5f, 11);
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
