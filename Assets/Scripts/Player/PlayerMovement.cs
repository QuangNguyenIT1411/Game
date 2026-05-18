using DungeonCrawler.Combat;
using DungeonCrawler.Items;
using DungeonCrawler.Progression;
using DungeonCrawler.Stats;
using DungeonCrawler.CameraControls;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace DungeonCrawler.Player
{
    /// <summary>
    /// Handles top-down 2D player movement and camera follow setup.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float acceleration = 18f;
        [SerializeField] private float attackRange = 1.2f;
        [SerializeField] private Camera followCamera;
        [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 0f, -10f);

        private static readonly Vector3 DefaultVisualScale = new Vector3(8f, 8f, 1f);

        private Rigidbody2D rb;
        private CharacterStats stats;
        private Health health;
        private DamageDealer damageDealer;
        private Collider2D playerCollider;
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer visualSpriteRenderer;
        private Transform visualTransform;
        private Transform directionArrow;
        private SpriteRenderer directionArrowRenderer;
        private TrailRenderer trailRenderer;
        private Vector2 movementInput;
        private Vector2 currentVelocity;
        private Vector2 lastMoveDirection = Vector2.down;

        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = Mathf.Max(0f, value);
        }

        public Vector2 MovementInput => movementInput;
        public Vector2 CurrentVelocity => currentVelocity;
        public Vector2 LastMoveDirection => lastMoveDirection;
        public bool IsMoving => currentVelocity.sqrMagnitude > 0.01f || movementInput.sqrMagnitude > 0.01f;

        private void Awake()
        {
            SetupRequiredComponents();
            SubscribeHealthEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeHealthEvents();
        }

        private void Reset()
        {
            SetupRequiredComponents();
        }

        private void Update()
        {
            if (Time.timeScale == 0)
            {
                return;
            }

            if (health != null && health.IsDead)
{
                movementInput = Vector2.zero;
                return;
            }

            ReadMovementInput();
            ReadAttackInput();
            ReadDebugDamageInput();
        }

        private void FixedUpdate()
        {
            if (health != null && health.IsDead)
            {
                currentVelocity = Vector2.zero;
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                }

                return;
            }

            if (rb == null && !SetupRequiredComponents())
            {
                return;
            }

            MovePlayer();
        }

        private void LateUpdate()
        {
            UpdateVisualFeedback();
            FollowPlayerWithCamera();
        }

        private bool SetupRequiredComponents()
        {
            bool removed3DRenderComponents = Remove3DRenderComponents();

            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
            }

            if (rb == null)
            {
                Debug.LogWarning($"{nameof(PlayerMovement)} requires a Rigidbody2D on {name}.", this);
                enabled = false;
                return false;
            }

            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            // Ensure Layer
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer != -1) gameObject.layer = playerLayer;

            stats = GetComponent<CharacterStats>();
            if (stats == null)
            {
                stats = gameObject.AddComponent<CharacterStats>();
                stats.ConfigurePlayerDefaults();
            }

            moveSpeed = stats.MoveSpeed;

            health = GetComponent<Health>();
            if (health == null)
            {
                health = gameObject.AddComponent<Health>();
            }

            health.Configure(stats.MaxHP, stats.MaxHP);

            damageDealer = GetComponent<DamageDealer>();
            if (damageDealer == null)
            {
                damageDealer = gameObject.AddComponent<DamageDealer>();
            }

            if (damageDealer != null)
            {
                damageDealer.Damage = stats.Attack;
                damageDealer.Cooldown = 0.4f;
            }

            if (GetComponent<PlayerItemUse>() == null)
            {
                gameObject.AddComponent<PlayerItemUse>();
            }

            if (GetComponent<EquipmentInventory>() == null)
            {
                gameObject.AddComponent<EquipmentInventory>();
            }

            if (GetComponent<PlayerLevel>() == null)
            {
                gameObject.AddComponent<PlayerLevel>();
            }

            playerCollider = GetComponent<Collider2D>();
            if (playerCollider == null)
            {
                CircleCollider2D circleCollider = gameObject.AddComponent<CircleCollider2D>();
                if (circleCollider != null)
                {
                    circleCollider.radius = 0.35f;
                }

                playerCollider = circleCollider;
            }

            if (playerCollider == null)
            {
                Debug.LogWarning($"{nameof(PlayerMovement)} could not create a Collider2D on {name}.", this);
                enabled = false;
                return false;
            }

            playerCollider.isTrigger = false;
            playerCollider.sharedMaterial = new PhysicsMaterial2D("Player_NoFriction")
            {
                friction = 0f,
                bounciness = 0f
            };

            Debug.Log("Physics movement enabled for Player");
            Debug.Log("Player movement uses Rigidbody2D only");

            // Log collision sanity check
            int wallLayer = LayerMask.NameToLayer("Wall");
            if (wallLayer != -1)
            {
                bool collide = !Physics2D.GetIgnoreLayerCollision(gameObject.layer, wallLayer);
                Debug.Log($"Collision setup: Player Layer({gameObject.layer}) vs Wall Layer({wallLayer}) collide={collide}");
            }

            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null && !removed3DRenderComponents)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
            else if (spriteRenderer == null)
            {
                StartCoroutine(AddSpriteRendererAfter3DComponentsAreDestroyed());
            }

            SetupSpriteRenderer(spriteRenderer);
            transform.localScale = Vector3.one;
            visualTransform = SetupVisualChild(spriteRenderer);
            directionArrow = SetupDirectionArrow();
            trailRenderer = SetupTrailRenderer();

            if (followCamera == null)
            {
                followCamera = GetOrCreateMainCamera();
            }

            ConfigureFollowCamera();
            UpdateVisualFeedback();

            return true;
        }

        private bool Remove3DRenderComponents()
        {
            bool removedAny = false;

            foreach (MeshRenderer meshRenderer in GetComponents<MeshRenderer>())
            {
                removedAny = true;
                Destroy(meshRenderer);
            }

            foreach (MeshFilter meshFilter in GetComponents<MeshFilter>())
            {
                removedAny = true;
                Destroy(meshFilter);
            }

            return removedAny;
        }

        private System.Collections.IEnumerator AddSpriteRendererAfter3DComponentsAreDestroyed()
        {
            yield return null;

            if (this == null || GetComponent<SpriteRenderer>() != null)
            {
                yield break;
            }

            if (GetComponent<MeshFilter>() != null || GetComponent<MeshRenderer>() != null)
            {
                yield break;
            }

            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            SetupSpriteRenderer(spriteRenderer);
        }

        private static void SetupSpriteRenderer(SpriteRenderer targetSpriteRenderer)
        {
            if (targetSpriteRenderer == null)
            {
                return;
            }

            targetSpriteRenderer.sprite = CreateSquareSprite(new Color(1f, 0.9f, 0.1f));
            targetSpriteRenderer.color = Color.white;
            targetSpriteRenderer.sortingOrder = 20;
        }

        private Transform SetupVisualChild(SpriteRenderer targetSpriteRenderer)
        {
            Transform visual = transform.Find("PlayerVisual");
            bool createdVisual = false;
            if (visual == null)
            {
                GameObject visualObject = new GameObject("PlayerVisual");
                visualObject.transform.SetParent(transform);
                visual = visualObject.transform;
                createdVisual = true;
            }

            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
            if (createdVisual)
            {
                visual.localScale = DefaultVisualScale;
            }

            if (targetSpriteRenderer != null)
            {
                targetSpriteRenderer.enabled = false;
            }

            SpriteRenderer visualRenderer = visual.GetComponent<SpriteRenderer>();
            if (visualRenderer == null)
            {
                visualRenderer = visual.gameObject.AddComponent<SpriteRenderer>();
            }

            SetupSpriteRenderer(visualRenderer);
            visualSpriteRenderer = visualRenderer;
            PlayerVisualController visualController = visual.gameObject.GetComponent<PlayerVisualController>();
            if (visualController == null)
            {
                visualController = visual.gameObject.AddComponent<PlayerVisualController>();
            }

            return visual;
        }

        private Transform SetupDirectionArrow()
        {
            Transform arrow = transform.Find("DirectionArrow");
            if (arrow == null)
            {
                GameObject arrowObject = new GameObject("DirectionArrow");
                arrowObject.transform.SetParent(transform);
                arrow = arrowObject.transform;
            }

            directionArrowRenderer = arrow.GetComponent<SpriteRenderer>();
            if (directionArrowRenderer == null)
            {
                directionArrowRenderer = arrow.gameObject.AddComponent<SpriteRenderer>();
            }

            directionArrowRenderer.sprite = CreateTriangleSprite(new Color(0.2f, 1f, 1f));
            directionArrowRenderer.color = Color.white;
            directionArrowRenderer.sortingOrder = 11;

            return arrow;
        }

        private TrailRenderer SetupTrailRenderer()
        {
            TrailRenderer trail = GetComponent<TrailRenderer>();
            if (trail == null)
            {
                trail = gameObject.AddComponent<TrailRenderer>();
            }

            if (trail == null)
            {
                return null;
            }

            trail.emitting = false;
            trail.time = 0.28f;
            trail.startWidth = 0.35f;
            trail.endWidth = 0f;
            trail.sortingOrder = 9;
            Shader spriteShader = Shader.Find("Sprites/Default");
            if (spriteShader != null)
            {
                trail.material = new Material(spriteShader);
            }

            trail.startColor = new Color(0.2f, 1f, 1f, 0.8f);
            trail.endColor = new Color(0.2f, 1f, 1f, 0f);

            return trail;
        }

        private void ReadMovementInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                movementInput = Vector2.zero;
                return;
            }

            movementInput = new Vector2(
                GetAxisFromKeys(keyboard.aKey, keyboard.dKey),
                GetAxisFromKeys(keyboard.sKey, keyboard.wKey));

            movementInput = Vector2.ClampMagnitude(movementInput, 1f);
        }

        private void ReadAttackInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || keyboard.spaceKey == null || !keyboard.spaceKey.wasPressedThisFrame)
            {
                return;
            }

            TryAttackEnemy();
        }

        private void TryAttackEnemy()
        {
            if (damageDealer == null || !damageDealer.CanDealDamage)
            {
                return;
            }

            Vector2 attackDirection = lastMoveDirection.sqrMagnitude > 0.001f ? lastMoveDirection.normalized : Vector2.down;
            Vector2 attackCenter = (Vector2)transform.position + attackDirection * 0.75f;
            AttackFeedback.SpawnSlash(transform.position, attackDirection);

            Collider2D[] hits = Physics2D.OverlapCircleAll(attackCenter, attackRange * 0.6f);
            foreach (Collider2D hit in hits)
            {
                DungeonCrawler.Enemy.EnemyController enemyController = hit != null ? hit.GetComponent<DungeonCrawler.Enemy.EnemyController>() : null;
                if (hit == null || hit.gameObject == gameObject || enemyController == null)
                {
                    continue;
                }

                Health enemyHealth = hit.GetComponent<Health>();
                if (enemyHealth != null && damageDealer.TryDealDamage(enemyHealth))
                {
                    Debug.Log($"PLAYER HIT ENEMY: damage={damageDealer.Damage}", hit);
                    enemyController.FlashHitFeedback();
                    AttackFeedback.FlashHit(this, hit.GetComponent<SpriteRenderer>());
                    return;
                }
            }
        }

        private void ReadDebugDamageInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || keyboard.hKey == null || !keyboard.hKey.wasPressedThisFrame)
            {
                return;
            }

            if (health == null)
            {
                Debug.LogWarning("H test damage failed: Player Health is null.", this);
                return;
            }

            health.TakeDamage(10);
        }

        private static float GetAxisFromKeys(KeyControl negativeKey, KeyControl positiveKey)
        {
            float axis = 0f;

            if (negativeKey != null && negativeKey.isPressed)
            {
                axis -= 1f;
            }

            if (positiveKey != null && positiveKey.isPressed)
            {
                axis += 1f;
            }

            return axis;
        }

        private void MovePlayer()
        {
            if (rb == null)
            {
                return;
            }

            Vector2 targetVelocity = movementInput * moveSpeed;
            currentVelocity = Vector2.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);

            // MANDATORY: Physics cast check to prevent walking through walls
            if (currentVelocity.sqrMagnitude > 0.0001f)
            {
                float moveDistance = currentVelocity.magnitude * Time.fixedDeltaTime;
                int wallLayerMask = LayerMask.GetMask("Wall");
                ContactFilter2D filter = new ContactFilter2D { layerMask = wallLayerMask, useLayerMask = true };
                RaycastHit2D[] results = new RaycastHit2D[1];

                // Cast ahead to detect walls before moving into them
                if (rb.Cast(currentVelocity.normalized, filter, results, moveDistance + 0.05f) > 0)
                {
                    // Allow sliding by projecting velocity onto the hit normal
                    Vector2 hitNormal = results[0].normal;
                    float project = Vector2.Dot(currentVelocity, hitNormal);
                    
                    if (project < 0) // Only project if moving towards the wall
                    {
                        currentVelocity -= project * hitNormal;
                    }

                    // Final check to ensure we don't move into a wall after projection
                    if (rb.Cast(currentVelocity.normalized, filter, results, currentVelocity.magnitude * Time.fixedDeltaTime + 0.02f) > 0)
                    {
                        currentVelocity = Vector2.zero;
                    }
                }
            }

            rb.linearVelocity = currentVelocity;
        }

        private void UpdateVisualFeedback()
        {
            bool isMoving = currentVelocity.sqrMagnitude > 0.01f;

            if (isMoving)
            {
                lastMoveDirection = currentVelocity.normalized;
            }

            UpdateDirectionArrow(lastMoveDirection);
            UpdateTrail(isMoving);
            UpdateBobScale(isMoving);
        }

        private void UpdateDirectionArrow(Vector2 direction)
        {
            if (directionArrow == null)
            {
                return;
            }

            PlayerVisualController visualController = visualTransform != null ? visualTransform.GetComponent<PlayerVisualController>() : null;
            if (visualController != null && visualController.HasSpriteSheet)
            {
                directionArrow.gameObject.SetActive(false);
                return;
            }

            if (!directionArrow.gameObject.activeSelf)
            {
                directionArrow.gameObject.SetActive(true);
            }

            Vector2 safeDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.down;
            directionArrow.localPosition = safeDirection * 0.55f;
            directionArrow.localScale = new Vector3(0.35f, 0.35f, 1f);

            float angle = Mathf.Atan2(safeDirection.y, safeDirection.x) * Mathf.Rad2Deg - 90f;
            directionArrow.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void UpdateTrail(bool isMoving)
        {
            if (trailRenderer == null)
            {
                return;
            }

            trailRenderer.emitting = isMoving;
        }

        private void UpdateBobScale(bool isMoving)
        {
            // PlayerVisualController owns visual scale so inspector-authored size is preserved.
        }

        private void FollowPlayerWithCamera()
        {
            if (VisualizationCameraController.IsVisualizationCameraActive)
            {
                return;
            }

            if (followCamera == null)
            {
                return;
            }

            ConfigureFollowCamera();
            Vector3 targetPosition = transform.position + cameraOffset;
            followCamera.transform.position = new Vector3(targetPosition.x, targetPosition.y, -10f);
        }

        private void ConfigureFollowCamera()
        {
            if (followCamera == null)
            {
                return;
            }

            followCamera.orthographic = true;
            followCamera.orthographicSize = 8f;
            followCamera.cullingMask |= 1 << LayerMask.NameToLayer("Default");
            followCamera.transform.rotation = Quaternion.identity;
            cameraOffset.z = -10f;
        }

        private void SubscribeHealthEvents()
        {
            if (health == null)
            {
                return;
            }

            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        private void UnsubscribeHealthEvents()
        {
            if (health == null)
            {
                return;
            }

            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        private void OnDamaged(Health damagedHealth, int damage)
        {
            StartCoroutine(FlashHitColor());
        }

        private void OnDied(Health deadHealth)
        {
            Debug.Log("Player died", this);
        }

        private System.Collections.IEnumerator FlashHitColor()
        {
            SpriteRenderer targetRenderer = visualSpriteRenderer != null ? visualSpriteRenderer : spriteRenderer;
            if (targetRenderer == null)
            {
                yield break;
            }

            Color originalColor = targetRenderer.color;
            targetRenderer.color = Color.red;
            yield return new WaitForSeconds(0.12f);
            targetRenderer.color = originalColor;
        }

        private static Camera GetOrCreateMainCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                return mainCamera;
            }

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";

            Camera newCamera = cameraObject.AddComponent<Camera>();
            newCamera.orthographic = true;
            newCamera.orthographicSize = 12f;
            newCamera.cullingMask |= 1 << LayerMask.NameToLayer("Default");
            newCamera.transform.position = new Vector3(0f, 0f, -10f);
            return newCamera;
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

        private static Sprite CreateTriangleSprite(Color color)
        {
            const int textureSize = 32;
            Texture2D texture = new Texture2D(textureSize, textureSize)
            {
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color clear = new Color(0f, 0f, 0f, 0f);
            for (int x = 0; x < textureSize; x++)
            {
                for (int y = 0; y < textureSize; y++)
                {
                    texture.SetPixel(x, y, clear);
                }
            }

            for (int y = 4; y < textureSize - 2; y++)
            {
                float width = Mathf.Lerp(13f, 2f, y / (float)(textureSize - 1));
                int minX = Mathf.RoundToInt((textureSize * 0.5f) - width);
                int maxX = Mathf.RoundToInt((textureSize * 0.5f) + width);

                for (int x = minX; x <= maxX; x++)
                {
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, textureSize, textureSize), new Vector2(0.5f, 0.5f), textureSize);
        }
    }
}
