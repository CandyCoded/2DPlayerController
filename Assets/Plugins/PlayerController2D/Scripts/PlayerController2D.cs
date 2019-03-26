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

        [Serializable]
        public class EventWithState : UnityEvent<STATE>
        {
        }

        [Serializable]
        public class EventWithStateComparison : UnityEvent<STATE, STATE>
        {
        }

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
        public const float DEFAULT_GRAVITY = -60f;
        public const float DEFAULT_WALL_SLIDE_SPEED = -2.0f;
        public const float DEFAULT_WALL_STICK_TRANSITION_DELAY = 0.2f;
        public const int DEFAULT_MAX_AVAILABLE_JUMPS = 2;

        public float horizontalSpeed = DEFAULT_HORIZONTAL_SPEED;
        public float horizontalResistance = DEFAULT_HORIZONTAL_RESISTANCE;
        public float lowJumpSpeed = DEFAULT_LOW_JUMP_SPEED;
        public float highJumpSpeed = DEFAULT_HIGH_JUMP_SPEED;
        public float gravity = DEFAULT_GRAVITY;
        public float wallSlideSpeed = DEFAULT_WALL_SLIDE_SPEED;
        public float wallStickTransitionDelay = DEFAULT_WALL_STICK_TRANSITION_DELAY;
        public int maxAvailableJumps = DEFAULT_MAX_AVAILABLE_JUMPS;

        public LayerMaskGroup layerMask = new LayerMaskGroup();

#pragma warning disable CS0649
        [SerializeField]
        private bool displayDebugColliders;
#pragma warning restore CS0649

        public EventWithStateComparison StateSwitch;
        public EventWithState StateLoop;

        public UnityEvent IdleSwitch;
        public UnityEvent IdleLoop;

        public UnityEvent WalkingSwitch;
        public UnityEvent WalkingLoop;

        public UnityEvent RunningSwitch;
        public UnityEvent RunningLoop;

        public UnityEvent FallSwitch;

        public UnityEvent JumpSwitch;

        public UnityEvent VerticalMovementLoop;

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
            Fall,
            Jump,
            VerticalMovement,
            WallSliding,
            WallSticking,
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

                    var previousState = _state;


                    Debug.Log(string.Format("Switched from state {0} to {1}.", _state, value));

                    _state = value;

                    RunStateSwitch();

                    StateSwitch?.Invoke(previousState, value);

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
            else if (state.Equals(STATE.Fall)) StateFallSwitch();
            else if (state.Equals(STATE.Jump)) StateJumpSwitch();
            else if (state.Equals(STATE.WallSliding)) StateWallSlidingSwitch();
            else if (state.Equals(STATE.WallSticking)) StateWallStickingSwitch();
            else if (state.Equals(STATE.WallDismount)) StateWallDismountSwitch();

        }

        private void RunStateLoop()
        {

            if (state.Equals(STATE.Idle)) StateIdleLoop();
            else if (state.Equals(STATE.Walking)) StateWalkingLoop();
            else if (state.Equals(STATE.Running)) StateRunningLoop();
            else if (state.Equals(STATE.VerticalMovement)) StateVerticalMovementLoop();
            else if (state.Equals(STATE.WallSliding)) StateWallSlidingLoop();
            else if (state.Equals(STATE.WallSticking)) StateWallStickingLoop();

            StateLoop?.Invoke(state);

        }

        private void Loop(bool freezeVelocityX = false, bool freezeVelocityY = false)
        {

            if (!freezeVelocityX) _velocity.x = CalculateHorizontalVelocity(_velocity.x);
            if (!freezeVelocityY) _velocity.y = CalculateVerticalVelocity(_velocity.y);

            var bounds = CalculateMovementBounds();

            CalculateFriction();

            position = MoveStep(bounds);

            if (IsIdle(bounds))
            {

                state = STATE.Idle;

                return;

            }

            if (IsWallDismounting(bounds))
            {

                state = STATE.WallDismount;

                return;

            }

            if (IsWallSliding(bounds))
            {

                state = STATE.WallSliding;

                return;

            }

            if (IsFalling(bounds))
            {

                state = STATE.Fall;

                return;

            }

            if (IsRunning(bounds))
            {

                state = STATE.Running;

                return;

            }

            if (IsJumping())
            {

                state = STATE.Jump;

                return;

            }

        }

        private void StateIdleSwitch()
        {

            _velocity.x = 0;
            _velocity.y = 0;

            IdleSwitch?.Invoke();

        }

        private void StateIdleLoop()
        {

            Loop(freezeVelocityX: true, freezeVelocityY: true);

            IdleLoop?.Invoke();

        }

        private bool IsIdle(MovementBounds bounds)
        {

            return !state.Equals(STATE.Idle) && bounds.bottom.NearlyEqual(position.y) && _velocity.x.NearlyEqual(0);

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

            Loop(freezeVelocityY: true);

            RunningLoop?.Invoke();

        }

        private bool IsRunning(MovementBounds bounds)
        {

            return !state.Equals(STATE.Running) && bounds.bottom.NearlyEqual(position.y) &&
                (Mathf.Abs(inputManager.inputHorizontal) > 0 || Mathf.Abs(_velocity.x) > 0) &&
                (bounds.right > position.x || bounds.left < position.x);

        }

        private void StateFallSwitch()
        {

            _velocity.y = 0;

            FallSwitch?.Invoke();

            state = STATE.VerticalMovement;

        }

        private bool IsFalling(MovementBounds bounds)
        {

            return !state.Equals(STATE.VerticalMovement) && !state.Equals(STATE.WallSliding) && (bounds.bottom.Equals(Mathf.NegativeInfinity) || !position.y.NearlyEqual(bounds.bottom)) && _velocity.y <= 0 || position.y.NearlyEqual(bounds.top);

        }

        private void StateJumpSwitch()
        {

            _velocity.y = highJumpSpeed;

            JumpSwitch?.Invoke();

            state = STATE.VerticalMovement;

        }

        private bool IsJumping()
        {

            return !state.Equals(STATE.VerticalMovement) && inputManager.inputJumpDown;

        }

        private void StateVerticalMovementLoop()
        {

            Loop();

            VerticalMovementLoop?.Invoke();

        }

        private void StateWallSlidingSwitch()
        {

            _velocity.x = 0;

            WallSlideSwitch?.Invoke();

        }

        private void StateWallSlidingLoop()
        {

            Loop(freezeVelocityX: true);

            WallSlideLoop?.Invoke();

        }

        private bool IsWallSliding(MovementBounds bounds)
        {

            return !state.Equals(STATE.WallSliding) &&
                (position.x.NearlyEqual(bounds.left) || position.x.NearlyEqual(bounds.right)) &&
                (!position.y.NearlyEqual(bounds.top) && !position.y.NearlyEqual(bounds.bottom));

        }

        private void StateWallStickingSwitch()
        {

            WallStickSwitch?.Invoke();

        }

        private void StateWallStickingLoop()
        {

            WallStickLoop?.Invoke();

        }

        private void StateWallJumpingSwitch()
        {

            WallJumpSwitch?.Invoke();

        }

        private void StateWallDismountSwitch()
        {

            WallDismountSwitch?.Invoke();

            state = STATE.VerticalMovement;

        }

        private bool IsWallDismounting(MovementBounds bounds)
        {

            return state.Equals(STATE.WallSliding) &&
                (
                    (position.x.NearlyEqual(bounds.left) && inputManager.inputHorizontal > 0) ||
                    (position.x.NearlyEqual(bounds.right) && inputManager.inputHorizontal < 0)
                );

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

            velocityY = Mathf.Max(velocityY + gravity * Time.deltaTime, Physics2D.gravity.y);

            return velocityY;

        }

        private Vector2 MoveStep(MovementBounds bounds)
        {

            var nextPosition = position;

            nextPosition += _velocity * Time.fixedDeltaTime;

            nextPosition.x = Mathf.Clamp(nextPosition.x, bounds.left, bounds.right);
            nextPosition.y = Mathf.Clamp(nextPosition.y, bounds.bottom, bounds.top);

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
                left = hitLeftRay ? hitLeftRay.point.x + extents.x : Mathf.NegativeInfinity,
                right = hitRightRay ? hitRightRay.point.x - extents.x : Mathf.Infinity,
                top = hitTopRay ? hitTopRay.point.y - extents.y : Mathf.Infinity,
                bottom = hitBottomRay ? hitBottomRay.point.y + extents.y : Mathf.NegativeInfinity
            };

            return bounds;

        }

        private void CalculateFriction()
        {

            var hitLeftRay = Physics2D.CircleCast(position - horizontalExtents, frictionRaycastRadius, Vector2.zero, 0, layerMask.left);
            var hitRightRay = Physics2D.CircleCast(position + horizontalExtents, frictionRaycastRadius, Vector2.zero, 0, layerMask.right);
            var hitTopRay = Physics2D.CircleCast(position + verticalExtents, frictionRaycastRadius, Vector2.zero, 0, layerMask.top);
            var hitBottomRay = Physics2D.CircleCast(position - verticalExtents, frictionRaycastRadius, Vector2.zero, 0, layerMask.bottom);

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
                Gizmos.DrawWireSphere(new Vector2(bounds.left - extents.x, position.y), 1);

                // Right
                Gizmos.DrawWireCube(position + Vector2.right * size.x, size);
                Gizmos.DrawWireSphere(position + horizontalExtents, frictionRaycastRadius);
                Gizmos.DrawWireSphere(new Vector2(bounds.right + extents.x, position.y), 1);

                // Top
                Gizmos.DrawWireCube(position + Vector2.up * size.y, size);
                Gizmos.DrawWireSphere(position + verticalExtents, frictionRaycastRadius);
                Gizmos.DrawWireSphere(new Vector2(position.x, bounds.top + extents.y), 1);

                // Bottom
                Gizmos.DrawWireCube(position + Vector2.down * size.y, size);
                Gizmos.DrawWireSphere(position - verticalExtents, frictionRaycastRadius);
                Gizmos.DrawWireSphere(new Vector2(position.x, bounds.bottom - extents.y), 1);

            }

        }

    }

}
