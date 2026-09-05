using MmoTemplate.Rpg;
using Unity.Netcode;
using UnityEngine;
namespace MmoTemplate.Player
{
    [RequireComponent(typeof(CharacterController), typeof(PlayerStats))]
    public class PlayerController : NetworkBehaviour
    {
        [SerializeField] private float speed = 5, gravity = -20, jumpSpeed = 7;
        public NetworkVariable<Unity.Collections.FixedString32Bytes> PlayerName = new(default);
        private CharacterController controller;
        private PlayerStats stats;
        private CameraFollow follow;
        private Vector2 direction;
        private Vector3 airMomentum;
        private float vertical, yaw, localHeading, lastInput, nextSend;
        private bool jump;
        private void Awake() { controller = GetComponent<CharacterController>(); stats = GetComponent<PlayerStats>(); }
        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;
            if (Camera.main != null) { follow = Camera.main.GetComponent<CameraFollow>(); follow?.SetTarget(transform); }
            localHeading = transform.eulerAngles.y;
            NameRpc(GameEvents.LocalPlayerName);
        }
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)] private void NameRpc(string value)
        {
            value = value.Replace("<", "").Replace(">", "");
            // FixedString32Bytes has a 29-byte UTF-8 capacity.
            while (System.Text.Encoding.UTF8.GetByteCount(value) > 29) value = value.Substring(0, value.Length - 1);
            PlayerName.Value = value;
        }
        private void Update()
        {
            if (!IsOwner) return;
            follow = CameraFollow.Active;
            bool blocked = GameEvents.InputBlocked || stats.IsDead;
            Vector2 input = blocked ? Vector2.zero : new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            bool steering = !blocked && Input.GetMouseButton(1);
            if (!blocked && Input.GetMouseButton(0) && steering) input.y = 1;
            float heading = steering && follow != null ? follow.Yaw : localHeading;
            if (!steering && !blocked) { heading += input.x * 120 * Time.deltaTime; input.x = 0; }
            if (!blocked) input.x += (Input.GetKey(KeyCode.Q) ? -1 : 0);
            localHeading = heading;
            bool leap = !blocked && Input.GetKeyDown(KeyCode.Space);
            if (Time.unscaledTime >= nextSend || leap || blocked)
            { nextSend = Time.unscaledTime + .05f; SubmitRpc(Vector2.ClampMagnitude(input,1), heading, leap); }
        }
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)] private void SubmitRpc(Vector2 input, float heading, bool leap)
        {
            if (!float.IsFinite(input.x) || !float.IsFinite(input.y) || !float.IsFinite(heading)) return;
            direction = Vector2.ClampMagnitude(input,1); yaw = heading; jump |= leap; lastInput = Time.time;
        }
        private void FixedUpdate()
        {
            if (!IsServer || !IsSpawned) return;
            if (Time.time - lastInput > .25f || stats.IsDead || stats.State.Value == CharacterState.Stunned) { direction = Vector2.zero; jump = false; }
            transform.rotation = Quaternion.Euler(0,yaw,0);
            Vector3 motion = transform.TransformDirection(new Vector3(direction.x,0,direction.y)) * speed;
            if (controller.isGrounded)
            {
                if (vertical < 0) vertical = -2;
                airMomentum = motion;
                if (jump) vertical = jumpSpeed;
            }
            else motion = airMomentum;
            jump = false; vertical += gravity * Time.fixedDeltaTime;
            stats.Moving = motion.sqrMagnitude > .01f || !controller.isGrounded;
            motion.y = vertical; controller.Move(motion * Time.fixedDeltaTime);
        }
    }
}
