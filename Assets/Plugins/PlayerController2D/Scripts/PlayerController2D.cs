using System;
using System.Linq;
using UnityEngine;
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

#pragma warning disable CS0649
        [SerializeField]
        private bool displayDebugColliders;
#pragma warning restore CS0649

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

        private Vector2 _velocity = Vector2.zero;

        public Vector2 position { get; private set; } = Vector2.zero;
        public Vector2 velocity => _velocity;

        private const float frictionRaycastRadius = 0.2f;

        private InputManager inputManager;
        private BoxCollider2D boxCollider;
        private Vector3 extents;

        private float verticalFriction;
        private float horizontalFriction;

        private Vector2 verticalExtents;
        private Vector2 horizontalExtents;

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

            extents = boxCollider.bounds.extents;

            verticalExtents = new Vector2(0, extents.y);
            horizontalExtents = new Vector2(extents.x, 0);

        }

        private void FixedUpdate()
        {

            position = gameObject.transform.position;

            RunStateLoop();

            gameObject.transform.position = position;

            inputManager.Reset();

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

            _velocity.x = 0;
            _velocity.y = 0;

            IdleSwitch?.Invoke();

        }

        private void StateIdleLoop()
        {

            var bounds = CalculateMovementBounds();

            if (IsFalling(bounds))
            {

                state = STATE.Falling;

                return;

            }

            if (IsRunning(bounds))
            {

                state = STATE.Running;

                return;

            }

            if (IsJumping())
            {

                state = STATE.Jumping;

                return;

            }

            IdleLoop?.Invoke();

        }


        private bool IsIdle(MovementBounds bounds)
        {

            return bounds.bottom.NearlyEqual(position.y - extents.y) && _velocity.x.NearlyEqual(0);

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

            _velocity.y = 0;

            RunningSwitch?.Invoke();

        }

        private void StateRunningLoop()
        {

            _velocity.x = CalculateHorizontalVelocity(_velocity.x);

            var bounds = CalculateMovementBounds();

            CalculateFriction();

            position = MoveStep(bounds);

            if (IsIdle(bounds))
            {

                state = STATE.Idle;

                return;

            }

            if (IsFalling(bounds))
            {

                state = STATE.Falling;

                return;

            }

            if (IsJumping())
            {

                state = STATE.Jumping;

                return;

            }

            RunningLoop?.Invoke();

        }

        private bool IsRunning(MovementBounds bounds)
        {

            return bounds.bottom.NearlyEqual(position.y - extents.y) &&
                (Mathf.Abs(inputManager.inputHorizontal) > 0 || Mathf.Abs(_velocity.x) > 0) &&
                (bounds.right > position.x + extents.x || bounds.left < position.x - extents.x);

        }

        private void StateFallingSwitch()
        {

            _velocity.y = 0;

            FallingSwitch?.Invoke();

        }

        private void StateFallingLoop()
        {

            _velocity.x = CalculateHorizontalVelocity(_velocity.x);
            _velocity.y = CalculateVerticalVelocity(_velocity.y);

            var bounds = CalculateMovementBounds();

            position = MoveStep(bounds);

            if (IsIdle(bounds))
            {

                state = STATE.Idle;

                return;

            }

            if (IsRunning(bounds))
            {

                state = STATE.Running;

                return;

            }

            FallingLoop?.Invoke();

        }

        private bool IsFalling(MovementBounds bounds)
        {

            return (bounds.bottom.Equals(Mathf.NegativeInfinity) || !position.y.NearlyEqual(bounds.bottom + extents.y)) && _velocity.y <= 0 || position.y.NearlyEqual(bounds.top - extents.y);

        }

        private void StateJumpingSwitch()
        {

            _velocity.y = highJumpSpeed;

            JumpingSwitch?.Invoke();

        }

        private void StateJumpingLoop()
        {

            _velocity.x = CalculateHorizontalVelocity(_velocity.x);
            _velocity.y = CalculateVerticalVelocity(_velocity.y);

            var bounds = CalculateMovementBounds();

            position = MoveStep(bounds);

            if (IsIdle(bounds))
            {

                state = STATE.Idle;

                return;

            }

            if (IsFalling(bounds))
            {

                state = STATE.Falling;

                return;

            }

            if (IsRunning(bounds))
            {

                state = STATE.Running;

                return;

            }

            JumpingLoop?.Invoke();

        }

        private bool IsJumping()
        {

            return inputManager.inputJumpDown;

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

        private float CalculateHorizontalVelocity(float velocityX)
        {

            if (Mathf.Abs(inputManager.inputHorizontal) > 0)
            {

                velocityX = Mathf.Lerp(velocityX, inputManager.inputHorizontal * horizontalSpeed, horizontalSpeed * Time.deltaTime);

            }

            if (velocity.x > 0)
            {

                velocityX = Mathf.Min(Mathf.Max(velocityX - Mathf.Max(horizontalResistance, horizontalFriction), 0), horizontalSpeed);

            }
            else if (velocity.x < 0)
            {

                velocityX = Mathf.Max(Mathf.Min(velocityX + Mathf.Max(horizontalResistance, horizontalFriction), 0), -horizontalSpeed);

            }

            return velocityX;

        }

        private float CalculateVerticalVelocity(float velocityY)
        {

            velocityY = Mathf.Max(velocityY + Physics2D.gravity.y * gravityMultiplier * Time.deltaTime, Physics2D.gravity.y);

            return velocityY;

        }

        private Vector2 MoveStep(MovementBounds bounds)
        {

            var nextPosition = position;

            nextPosition += _velocity * Time.fixedDeltaTime;

            nextPosition.x = Mathf.Clamp(nextPosition.x, bounds.left + extents.x, bounds.right - extents.x);
            nextPosition.y = Mathf.Clamp(nextPosition.y, bounds.bottom + extents.y, bounds.top - extents.y);

            return nextPosition;

        }

        private MovementBounds CalculateMovementBounds()
        {

            var size = boxCollider.bounds.size;

            var hitLeftRay = Physics2D.BoxCastAll(position, size, 0f, Vector2.left, size.x, layerMask.left)
                .FirstOrDefault(h => h.point.x < boxCollider.bounds.min.x);
            var hitRightRay = Physics2D.BoxCastAll(position, size, 0f, Vector2.right, size.x, layerMask.right)
                .FirstOrDefault(h => h.point.x > boxCollider.bounds.max.x);
            var hitTopRay = Physics2D.BoxCastAll(position, size, 0f, Vector2.up, size.y, layerMask.top)
                .FirstOrDefault(h => h.point.y > boxCollider.bounds.max.y);
            var hitBottomRay = Physics2D.BoxCastAll(position, size, 0f, Vector2.down, size.y, layerMask.bottom)
                .FirstOrDefault(h => h.point.y < boxCollider.bounds.min.y);

            var bounds = new MovementBounds
            {
                left = hitLeftRay && hitLeftRay.point.x < boxCollider.bounds.min.x ? hitLeftRay.point.x : Mathf.NegativeInfinity,
                right = hitRightRay && hitRightRay.point.x > boxCollider.bounds.max.x ? hitRightRay.point.x : Mathf.Infinity,
                top = hitTopRay && hitTopRay.point.y > boxCollider.bounds.max.y ? hitTopRay.point.y : Mathf.Infinity,
                bottom = hitBottomRay && hitBottomRay.point.y < boxCollider.bounds.min.y ? hitBottomRay.point.y : Mathf.NegativeInfinity
            };

            return bounds;

        }

        private void CalculateFriction()
        {

            var hitLeftRay = Physics2D.CircleCast(position - horizontalExtents, frictionRaycastRadius, Vector2.zero, 0, layerMask.left);
            var hitRightRay = Physics2D.CircleCast(position + horizontalExtents, frictionRaycastRadius, Vector2.zero, 0, layerMask.right);
            var hitTopRay = Physics2D.CircleCast(position - verticalExtents, frictionRaycastRadius, Vector2.zero, 0, layerMask.top);
            var hitBottomRay = Physics2D.CircleCast(position + verticalExtents, frictionRaycastRadius, Vector2.zero, 0, layerMask.bottom);

            if (hitLeftRay) verticalFriction = hitLeftRay.collider.friction;
            else if (hitRightRay) verticalFriction = hitRightRay.collider.friction;
            else verticalFriction = 0;

            if (hitTopRay) horizontalFriction = hitTopRay.collider.friction;
            else if (hitBottomRay) horizontalFriction = hitBottomRay.collider.friction;
            else horizontalFriction = 0;

        }

        private void OnDrawGizmos()
        {

            if (displayDebugColliders)
            {

                boxCollider = gameObject.GetComponent<BoxCollider2D>();

                extents = boxCollider.bounds.extents;

                verticalExtents = new Vector2(0, extents.y);
                horizontalExtents = new Vector2(extents.x, 0);

                var size = boxCollider.bounds.size;

                position = gameObject.transform.position;

                var bounds = CalculateMovementBounds();

                Gizmos.color = Color.green;

                // Left
                Gizmos.DrawWireCube(position + Vector2.left * size.x, size);
                Gizmos.DrawWireSphere(position - horizontalExtents, frictionRaycastRadius);
                Gizmos.DrawWireSphere(new Vector2(bounds.left, position.y), 1);

                // Right
                Gizmos.DrawWireCube(position + Vector2.right * size.x, size);
                Gizmos.DrawWireSphere(position + horizontalExtents, frictionRaycastRadius);
                Gizmos.DrawWireSphere(new Vector2(bounds.right, position.y), 1);

                // Top
                Gizmos.DrawWireCube(position + Vector2.up * size.y, size);
                Gizmos.DrawWireSphere(position - verticalExtents, frictionRaycastRadius);
                Gizmos.DrawWireSphere(new Vector2(position.x, bounds.top), 1);

                // Bottom
                Gizmos.DrawWireCube(position + Vector2.down * size.y, size);
                Gizmos.DrawWireSphere(position + verticalExtents, frictionRaycastRadius);
                Gizmos.DrawWireSphere(new Vector2(position.x, bounds.bottom), 1);

            }

        }

    }

}
