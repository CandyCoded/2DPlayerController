using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CandyCoded;
using UnityEngine.Events;

namespace CandyCoded.PlayerController2D
{

    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(InputManager))]
    public class PlayerController2D : MonoBehaviour
    {

        public struct MovementBounds
        {
            public float left;
            public float right;
            public float top;
            public float bottom;
        }

        [Serializable]
        public struct LayerMaskGroup
        {
            public LayerMask left;
            public LayerMask right;
            public LayerMask top;
            public LayerMask bottom;
        }

        public const float DEFAULT_HORIZONTAL_SPEED = 7.0f;
        public const float DEFAULT_HORIZONTAL_RESISTANCE = 0.02f;
        public const float DEFAULT_LOW_JUMP_SPEED = 10.0f;
        public const float DEFAULT_HIGH_JUMP_SPEED = 15.0f;
        public const float DEFAULT_GRAVITY_MULTIPLIER = 2f;
        public const float DEFAULT_WALL_SLIDE_SPEED = -2.0f;
        public const float DEFAULT_WALL_STICK_TRANSITION_DELAY = 0.2f;
        public const int DEFAULT_MAX_AVAILABLE_JUMPS = 2;

        public float horizontalSpeed = DEFAULT_HORIZONTAL_SPEED;
        public float horizontalResistance = DEFAULT_HORIZONTAL_RESISTANCE;
        public float lowJumpSpeed = DEFAULT_LOW_JUMP_SPEED;
        public float highJumpSpeed = DEFAULT_HIGH_JUMP_SPEED;
        public float gravityMultiplier = DEFAULT_GRAVITY_MULTIPLIER;
        public float wallSlideSpeed = DEFAULT_WALL_SLIDE_SPEED;
        public float wallStickTransitionDelay = DEFAULT_WALL_STICK_TRANSITION_DELAY;
        public int maxAvailableJumps = DEFAULT_MAX_AVAILABLE_JUMPS;

        public LayerMaskGroup layerMask = new LayerMaskGroup();

        public UnityEvent IdleSwitch;
        public UnityEvent IdleLoop;

        public UnityEvent WalkingSwitch;
        public UnityEvent WalkingLoop;

        public UnityEvent RunningSwitch;
        public UnityEvent RunningLoop;

        public UnityEvent FallingSwitch;
        public UnityEvent FallingLoop;

        public UnityEvent JumpingSwitch;
        public UnityEvent JumpingLoop;

        public UnityEvent WallSlideSwitch;
        public UnityEvent WallSlideLoop;

        public UnityEvent WallStickSwitch;
        public UnityEvent WallStickLoop;

        public UnityEvent WallJumpSwitch;

        public UnityEvent WallDismountSwitch;

        private Vector2 _position = Vector2.zero;
        private Vector2 _velocity = Vector2.zero;

        public Vector2 position => _position;
        public Vector2 velocity => _velocity;

        private InputManager inputManager;
        private BoxCollider2D boxCollider;

        public enum STATE
        {
            Idle,
            Walking,
            Running,
            Falling,
            Jumping,
            WallSlide,
            WallStick,
            WallJump,
            WallDismount
        }

        private STATE _state = STATE.Idle;

        public STATE state
        {

            get { return _state; }

            set
            {

                if (!_state.Equals(value))
                {


                    Debug.Log(string.Format("Switched from state {0} to {1}.", _state, value));

                    _state = value;

                    RunStateSwitch();

                }

            }

        }

        private void Awake()
        {

            inputManager = gameObject.GetComponent<InputManager>();
            boxCollider = gameObject.GetComponent<BoxCollider2D>();

        }

        private void Update()
        {




        }

        private void FixedUpdate()
        {

            _position = gameObject.transform.position;

            RunStateLoop();

            gameObject.transform.position = _position;

        }

        private void RunStateSwitch()
        {

            if (state.Equals(STATE.Idle)) StateIdleSwitch();
            else if (state.Equals(STATE.Walking)) StateWalkingSwitch();
            else if (state.Equals(STATE.Running)) StateRunningSwitch();
            else if (state.Equals(STATE.Falling)) StateFallingSwitch();
            else if (state.Equals(STATE.Jumping)) StateJumpingSwitch();
            else if (state.Equals(STATE.WallSlide)) StateWallSlideSwitch();
            else if (state.Equals(STATE.WallStick)) StateWallStickSwitch();

        }

        private void RunStateLoop()
        {

            if (state.Equals(STATE.Idle)) StateIdleLoop();
            else if (state.Equals(STATE.Walking)) StateWalkingLoop();
            else if (state.Equals(STATE.Running)) StateRunningLoop();
            else if (state.Equals(STATE.Falling)) StateFallingLoop();
            else if (state.Equals(STATE.Jumping)) StateJumpingLoop();
            else if (state.Equals(STATE.WallSlide)) StateWallSlideLoop();
            else if (state.Equals(STATE.WallStick)) StateWallStickLoop();

        }

        private void StateIdleSwitch()
        {

            IdleSwitch?.Invoke();

        }

        private void StateIdleLoop()
        {

            IdleLoop?.Invoke();

        }

        private void StateWalkingSwitch()
        {

            WalkingSwitch?.Invoke();

        }

        private void StateWalkingLoop()
        {

            WalkingLoop?.Invoke();

        }

        private void StateRunningSwitch()
        {

            RunningSwitch?.Invoke();

        }

        private void StateRunningLoop()
        {

            RunningLoop?.Invoke();

        }

        private void StateFallingSwitch()
        {

            FallingSwitch?.Invoke();

        }

        private void StateFallingLoop()
        {

            _velocity.y += Physics2D.gravity.y * gravityMultiplier * Time.deltaTime;

            _position = MoveStep(CalculateMovementBounds());

            FallingLoop?.Invoke();

        }

        private void StateJumpingSwitch()
        {

            JumpingSwitch?.Invoke();

        }

        private void StateJumpingLoop()
        {

            JumpingLoop?.Invoke();

        }

        private void StateWallSlideSwitch()
        {

            WallSlideSwitch?.Invoke();

        }

        private void StateWallSlideLoop()
        {

            WallSlideLoop?.Invoke();

        }

        private void StateWallStickSwitch()
        {

            WallStickSwitch?.Invoke();

        }

        private void StateWallStickLoop()
        {

            WallStickLoop?.Invoke();

        }

        private void StateWallJumpSwitch()
        {

            WallJumpSwitch?.Invoke();

        }

        private void StateWallDismountSwitch()
        {

            WallDismountSwitch?.Invoke();

        }

        private Vector2 MoveStep(MovementBounds bounds)
        {

            Vector2 nextPosition = _position;

            nextPosition += _velocity * Time.deltaTime;

            nextPosition.x = Mathf.Clamp(nextPosition.x, bounds.left, bounds.right);
            nextPosition.y = Mathf.Clamp(nextPosition.y, bounds.bottom, bounds.top);

            return nextPosition;

        }

        private MovementBounds CalculateMovementBounds()
        {

            var size = boxCollider.bounds.size;

            var hitLeftRay = Physics2D.BoxCast(_position, size, 0f, Vector2.left, 1f, layerMask.left);
            var hitRightRay = Physics2D.BoxCast(_position, size, 0f, Vector2.right, 1f, layerMask.right);
            var hitTopRay = Physics2D.BoxCast(_position, size, 0f, Vector2.up, 1f, layerMask.top);
            var hitBottomRay = Physics2D.BoxCast(_position, size, 0f, Vector2.down, 1f, layerMask.bottom);

            var bounds = new MovementBounds
            {
                left = hitLeftRay && hitLeftRay.point.x < boxCollider.bounds.min.x ? hitLeftRay.point.x : Mathf.NegativeInfinity,
                right = hitRightRay && hitRightRay.point.x > boxCollider.bounds.max.x ? hitRightRay.point.x : Mathf.Infinity,
                top = hitTopRay && hitTopRay.point.y > boxCollider.bounds.max.y ? hitTopRay.point.y : Mathf.Infinity,
                bottom = hitBottomRay && hitBottomRay.point.y < boxCollider.bounds.min.y ? hitBottomRay.point.y : Mathf.NegativeInfinity
            };

            return bounds;

        }

        private void OnDrawGizmos()
        {

            boxCollider = gameObject.GetComponent<BoxCollider2D>();

            _position = gameObject.transform.position;

            var bounds = CalculateMovementBounds();

            Gizmos.DrawWireSphere(new Vector2(gameObject.transform.position.x, bounds.bottom), 1);
            Gizmos.DrawWireSphere(new Vector2(gameObject.transform.position.x, bounds.top), 1);
            Gizmos.DrawWireSphere(new Vector2(bounds.left, gameObject.transform.position.y), 1);
            Gizmos.DrawWireSphere(new Vector2(bounds.right, gameObject.transform.position.y), 1);

        }

    }

}
