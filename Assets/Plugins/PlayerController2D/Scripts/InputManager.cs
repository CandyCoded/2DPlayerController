using UnityEngine;

namespace CandyCoded.PlayerController2D
{

    public class InputManager : MonoBehaviour
    {

        public string jumpButtonName = "Jump";
        public KeyCode jumpKey = KeyCode.Joystick1Button16;

        public bool inputJumpDown { get; private set; }
        public bool inputJumpHeld { get; private set; }
        public float inputHorizontal { get; private set; }

        private void Update()
        {

            inputJumpDown |= Input.GetButtonDown(jumpButtonName) || Input.GetKeyDown(jumpKey);
            inputJumpHeld |= Input.GetButton(jumpButtonName) || Input.GetKey(jumpKey);

            inputHorizontal = Input.GetAxisRaw("Horizontal");

        }

        private void LateUpdate()
        {

            inputJumpDown = false;
            inputJumpHeld = false;

        }

    }

}
