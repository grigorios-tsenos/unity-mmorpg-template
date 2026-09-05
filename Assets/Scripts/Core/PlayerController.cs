using MmoTemplate.Rpg;
using UnityEngine;
namespace MmoTemplate.Player
{
    [RequireComponent(typeof(CharacterController),typeof(PlayerStats))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float speed=4.6f, gravity=-22, jumpSpeed=7, turnSpeed=135;
        private CharacterController controller;
        private PlayerStats stats;
        private Vector2 direction;
        private Vector3 airMomentum;
        private float vertical,yaw;
        private bool jump,steerStartedOverUi;
        public bool ReadInput { get; set; } = true;
        public bool Grounded=>controller.isGrounded;
        private void Awake(){controller=GetComponent<CharacterController>();stats=GetComponent<PlayerStats>();yaw=transform.eulerAngles.y;}
        private void Start(){stats.Recovered+=ResetMotion;CameraFollow.Active?.SetTarget(transform);}
        private void OnDestroy(){if(stats!=null)stats.Recovered-=ResetMotion;}
        private void ResetMotion(){direction=Vector2.zero;airMomentum=Vector3.zero;vertical=0;jump=false;yaw=transform.eulerAngles.y;CameraFollow.Active?.SetTarget(transform);}
        private void Update()
        {
            if (!ReadInput) return;
            bool blocked=GameEvents.InputBlocked||stats.IsDead||stats.State.Value==CharacterState.Stunned;
            if(blocked){direction=Vector2.zero;jump=false;return;}
            float horizontal=Input.GetAxisRaw("Horizontal");
            // Mirrors CameraFollow: a right-drag begun on the HUD is the UI's, not a steer. Without
            // this the heading would snap to the camera and drag the camera with it.
            if(Input.GetMouseButtonDown(1))steerStartedOverUi=GameEvents.PointerOverUi;
            if(!Input.GetMouseButton(1))steerStartedOverUi=false;
            bool steer=Input.GetMouseButton(1)&&!steerStartedOverUi;
            if(steer&&CameraFollow.Active!=null)yaw=CameraFollow.Active.Yaw;
            else yaw+=horizontal*turnSpeed*Time.deltaTime;
            float strafe=(steer?horizontal:0)+(Input.GetKey(KeyCode.Q)?-1:0)+(Input.GetKey(KeyCode.R)?1:0);
            float forward=Input.GetMouseButton(0)&&steer?1:Input.GetAxisRaw("Vertical");
            SetMovement(new Vector2(strafe,forward),yaw,Input.GetKeyDown(KeyCode.Space));
        }
        /// <summary>Single local input path, also used by deterministic movement checks.</summary>
        public void SetMovement(Vector2 input,float heading,bool leap)
        {
            if(!float.IsFinite(input.x)||!float.IsFinite(input.y)||!float.IsFinite(heading))return;
            direction=Vector2.ClampMagnitude(input,1);yaw=heading;jump|=leap;
        }
        private void FixedUpdate()
        {
            if(stats.IsDead||stats.State.Value==CharacterState.Stunned){direction=Vector2.zero;jump=false;}
            transform.rotation=Quaternion.Euler(0,yaw,0);
            Vector3 motion=transform.TransformDirection(new Vector3(direction.x,0,direction.y))*speed;
            if(direction.y<0)motion*=.65f;
            if(controller.isGrounded)
            {
                if(vertical<0)vertical=-2;
                airMomentum=motion;
                if(jump)vertical=jumpSpeed;
            }
            else motion=airMomentum;
            jump=false;vertical+=gravity*Time.fixedDeltaTime;
            stats.Moving=motion.sqrMagnitude>.01f||!controller.isGrounded;
            motion.y=vertical;controller.Move(motion*Time.fixedDeltaTime);
            // Recovery boundary protects against leaving the authored room through bad collision.
            if(transform.position.y < -5){controller.enabled=false;transform.position=new Vector3(-2,.2f,4);controller.enabled=true;ResetMotion();}
        }
    }
}
