using UnityEngine;

namespace DungeonCrawler.Player
{
    public class PlayerVisualController : MonoBehaviour
    {
        private const int Columns = 10;
        private const int ExpectedSpriteCount = 40;
        private static readonly int[] WalkColumns = { 0, 1, 2 };

        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float framesPerSecond = 10f;
        [SerializeField] private bool useBobScale;
        [SerializeField] private float bobStrength = 0.08f;
        [SerializeField] private bool enableAnimationDebug;
        [SerializeField] public int downRow = 0;
        [SerializeField] public int leftRow = 1;
        [SerializeField] public int rightRow = 2;
        [SerializeField] public int upRow = 3;
        [SerializeField] private bool enableDirectionDebug;

        private Vector2 lastDirection = Vector2.down;
        private Vector3 baseVisualScale;
        private float animationTime;
        private float attackTimer;
        private bool loggedLoaded;
        private bool loggedFallback;
        private bool loggedBaseScale;
        private bool loggedFrameWarning;

        public bool HasSpriteSheet => frames != null && frames.Length > 0;

        private void Awake()
        {
            EnsureReferences();
            CaptureBaseScale();
            ApplyFrame(0);
            LogState();
        }

        private void Update()
        {
            EnsureReferences();
            if (!HasSpriteSheet)
            {
                ApplyFallback();
                ApplyBobScale(false);
                LogState();
                return;
            }

            Vector2 moveDirection = GetMoveDirection(out bool moving);
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                attackTimer = 0.12f;
            }

            if (moving)
            {
                lastDirection = moveDirection.normalized;
                animationTime += Time.deltaTime;
            }
            else
            {
                animationTime = 0f;
            }

            int directionRow = GetDirectionRow(lastDirection);
            bool attacking = attackTimer > 0f;
            if (attacking)
            {
                attackTimer -= Time.deltaTime;
            }

            int selectedColumn = attacking
                ? 3
                : moving
                    ? WalkColumns[Mathf.FloorToInt(animationTime * framesPerSecond) % WalkColumns.Length]
                    : 7;
            int frameIndex = attacking
                ? GetFrameIndex(directionRow, selectedColumn)
                : moving
                    ? GetFrameIndex(directionRow, selectedColumn)
                    : GetFrameIndex(directionRow, selectedColumn);
            ApplyFrame(frameIndex);
            ApplyBobScale(moving);

            if (enableAnimationDebug)
            {
                Debug.Log($"Player animation direction={lastDirection} frame={frameIndex} moving={moving}", this);
            }

            if (enableDirectionDebug)
            {
                Debug.Log($"Player direction input={moveDirection} row={directionRow} column={selectedColumn}", this);
            }
        }

        public void SetFrames(Sprite[] newFrames)
        {
            frames = newFrames;
            EnsureReferences();
            ApplyFrame(0);
            loggedLoaded = false;
            loggedFallback = false;
            LogState();
        }

        private void EnsureReferences()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            if (rb == null)
            {
                rb = GetComponentInParent<Rigidbody2D>();
            }

            if (playerMovement == null)
            {
                playerMovement = GetComponentInParent<PlayerMovement>();
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = 20;
                spriteRenderer.enabled = true;
            }

        }

        private void CaptureBaseScale()
        {
            if (baseVisualScale == Vector3.zero)
            {
                baseVisualScale = transform.localScale;
            }

            if (!loggedBaseScale)
            {
                Debug.Log($"Player visual base scale = {baseVisualScale}", this);
                loggedBaseScale = true;
            }
        }

        private void ApplyBobScale(bool moving)
        {
            if (!useBobScale)
            {
                transform.localScale = baseVisualScale;
                return;
            }

            float bob = moving ? 1f + Mathf.Sin(Time.time * 14f) * bobStrength : 1f;
            transform.localScale = baseVisualScale * bob;
        }

        private void ApplyFrame(int index)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (HasSpriteSheet && index >= 0 && index < frames.Length)
            {
                spriteRenderer.sprite = frames[index];
                spriteRenderer.color = Color.white;
                return;
            }

            ApplyFallback();
        }

        private void ApplyFallback()
        {
            if (spriteRenderer == null || spriteRenderer.sprite != null)
            {
                return;
            }

            spriteRenderer.sprite = CreateSquareSprite(new Color(1f, 0.9f, 0.1f, 1f));
            spriteRenderer.color = Color.white;
        }

        private void LogState()
        {
            if (HasSpriteSheet && !loggedLoaded)
            {
                Debug.Log($"Player visual loaded: {frames.Length} sprites", this);
                loggedLoaded = true;
                if (frames.Length < ExpectedSpriteCount && !loggedFrameWarning)
                {
                    Debug.LogWarning($"Player sprite sheet has {frames.Length} sprites, expected 40 for 4 directions x 10 columns.", this);
                    loggedFrameWarning = true;
                }
            }
            else if (!HasSpriteSheet && !loggedFallback)
            {
                Debug.Log("Fallback placeholder used for Player", this);
                loggedFallback = true;
            }
        }

        private int GetDirectionRow(Vector2 direction)
        {
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                return direction.x < 0f ? leftRow : rightRow;
            }

            return direction.y > 0f ? upRow : downRow;
        }

        private int GetFrameIndex(int directionRow, int column)
        {
            int mappedIndex = directionRow * Columns + column;
            if (frames.Length >= ExpectedSpriteCount)
            {
                return Mathf.Clamp(mappedIndex, 0, frames.Length - 1);
            }

            return Mathf.Abs(mappedIndex) % frames.Length;
        }

        private Vector2 GetMoveDirection(out bool moving)
        {
            if (playerMovement != null)
            {
                Vector2 input = playerMovement.MovementInput;
                if (input.sqrMagnitude > 0.01f)
                {
                    moving = true;
                    return input.normalized;
                }

                Vector2 movementVelocity = playerMovement.CurrentVelocity;
                if (movementVelocity.sqrMagnitude > 0.01f)
                {
                    moving = true;
                    return movementVelocity.normalized;
                }
            }

            Vector2 velocity = rb != null ? rb.linearVelocity : Vector2.zero;
            if (velocity.sqrMagnitude > 0.01f)
            {
                moving = true;
                return velocity.normalized;
            }

            moving = false;
            return lastDirection;
        }

        private static Sprite CreateSquareSprite(Color color)
        {
            Texture2D texture = new Texture2D(1, 1)
            {
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave
            };

            texture.SetPixel(0, 0, color);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
