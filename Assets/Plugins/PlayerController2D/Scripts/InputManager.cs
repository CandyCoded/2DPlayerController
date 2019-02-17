using UnityEngine;

namespace CandyCoded.PlayerController2D
{

    public class InputManager : MonoBehaviour
    {

        public bool inputJumpDown { get; private set; }
        public bool inputJumpHeld { get; private set; }
        public float inputHorizontal { get; private set; }

        private void Update()
        {

            inputJumpDown |= Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Joystick1Button16);
            inputJumpHeld |= Input.GetButton("Jump") || Input.GetKey(KeyCode.Joystick1Button16);

            inputHorizontal = Input.GetAxisRaw("Horizontal");

        }

        private void LateUpdate()
        {

            inputJumpDown = false;
            inputJumpHeld = false;

        }

    }

}
