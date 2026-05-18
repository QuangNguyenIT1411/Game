using System.Collections.Generic;
using DungeonCrawler.Combat;
using DungeonCrawler.Pathfinding;
using DungeonCrawler.Progression;
using DungeonCrawler.Stats;
using UnityEngine;

using DungeonCrawler.AI;

namespace DungeonCrawler.Enemy
{
    /// <summary>
    /// Basic top-down enemy AI that chases the Player inside detection range.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyController : MonoBehaviour
    {
public enum PathfindingMode { AStar, BFS }

        [SerializeField] private PathfindingMode pathfindingMode = PathfindingMode.AStar;
[SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float detectionRange = 20f;
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private int attackDamage = 10;
        [SerializeField] private float attackCooldown = 1f;
        [SerializeField] private float pathRefreshInterval = 0.25f;
        [SerializeField] private float waypointReachDistance = 0.35f;
[SerializeField] private Transform target;
        [SerializeField] private PathfindingGrid specificGrid;

        public bool enableDebugLogs = false;
public bool showPathDebug = true;

        private Rigidbody2D rb;
private CharacterStats stats;
        private Health health;
        private DamageDealer damageDealer;
        private CircleCollider2D circleCollider;
        private SpriteRenderer spriteRenderer;
        private Transform directionArrow;
        private LineRenderer pathLineRenderer;
        private readonly List<Vector3> currentPath = new List<Vector3>();
        private Vector2 moveDirection;
        private Vector2 lastLookDirection = Vector2.down;
        private Vector3 spawnPosition;
        private int pathIndex;
        private float pathRefreshTimer;
        private float nextAttackTime;
        private float nextCombatDebugTime;
        private float flashUntilTime;
        private bool isChasing;
        private bool isReturning;
        private float lastAttackTime;
        private Color baseTint = Color.white;

        public PathfindingMode Mode
        {
            get => pathfindingMode;
            set
            {
                pathfindingMode = value;
                UpdateVisualTint();
            }
        }

        public void Initialize(Transform targetTransform, PathfindingGrid grid)
        {
            target = targetTransform;
            specificGrid = grid;
            
            // Force immediate path refresh for stats recording
            RefreshPath();
            
            if (enableDebugLogs)
            {
                Debug.Log($"EnemyController initialized with target: {(target != null ? target.name : "null")} and grid: {(specificGrid != null ? specificGrid.name : "null")}", this);
            }
        }

        public bool IsMoving => moveDirection.sqrMagnitude > 0.001f;
public bool IsChasing => isChasing;
        public bool IsRecentlyAttacking => Time.time - lastAttackTime <= 0.18f;

        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = Mathf.Max(0f, value);
        }

        public float DetectionRange
        {
            get => detectionRange;
            set => detectionRange = Mathf.Max(0f, value);
        }

        public float AttackRange
        {
            get => attackRange;
            set => attackRange = Mathf.Max(0f, value);
        }

        public int AttackDamage
        {
            get => attackDamage;
            set => attackDamage = Mathf.Max(0, value);
        }

        public float AttackCooldown
        {
            get => attackCooldown;
            set => attackCooldown = Mathf.Max(0f, value);
        }

        public void FlashHitFeedback()
        {
            flashUntilTime = Time.time + 0.12f;
            EnemyVisualController visualController = GetComponent<EnemyVisualController>();
            if (visualController != null)
            {
                visualController.PlayHurtFeedback();
            }
        }

        private void Awake()
        {
            SetupRequiredComponents();
            spawnPosition = transform.position;
            SubscribeHealthEvents();
            UpdateVisualTint();
        }

        private void UpdateVisualTint()
        {
            // We now use distinct prefabs (Green for A*, Blue for BFS)
            // so we use white tint to preserve prefab colors.
            baseTint = Color.white;

            EnemyVisualController visual = GetComponent<EnemyVisualController>();
            if (visual != null)
            {
                visual.SetBaseTint(baseTint);
            }
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

            FindTargetIfMissing();
if (IsPlayerDead())
            {
                UpdateReturnToSpawn();
                UpdateVisualFeedback();
                return;
            }

            DebugCombatDistance();
            TryAttackPlayerByDistance();
            UpdateChaseState();
            UpdateVisualFeedback();
        }

        private void FixedUpdate()
        {
            if (rb == null)
            {
                return;
            }

            Vector2 velocity = moveDirection * moveSpeed;

            // Physics cast safety for Enemy
            if (velocity.sqrMagnitude > 0.0001f)
            {
                float moveDistance = velocity.magnitude * Time.fixedDeltaTime;
                int wallLayerMask = LayerMask.GetMask("Wall");
                ContactFilter2D filter = new ContactFilter2D { layerMask = wallLayerMask, useLayerMask = true };
                RaycastHit2D[] results = new RaycastHit2D[1];

                if (rb.Cast(velocity.normalized, filter, results, moveDistance + 0.05f) > 0)
                {
                    Vector2 hitNormal = results[0].normal;
                    float project = Vector2.Dot(velocity, hitNormal);
                    if (project < 0) velocity -= project * hitNormal;

                    if (rb.Cast(velocity.normalized, filter, results, velocity.magnitude * Time.fixedDeltaTime + 0.02f) > 0)
                    {
                        velocity = Vector2.zero;
                    }
                }
            }

            rb.linearVelocity = velocity;
        }

        private void SetupRequiredComponents()
        {
            bool removed3DRenderComponents = Remove3DRenderComponents();

            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
            }

            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 0f;
                rb.freezeRotation = true;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }

            // Ensure Layer
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer != -1) gameObject.layer = enemyLayer;

            stats = GetComponent<CharacterStats>();
            if (stats == null)
            {
                stats = gameObject.AddComponent<CharacterStats>();
                stats.ConfigureEnemyDefaults(10);
            }

            moveSpeed = stats.MoveSpeed;
            attackDamage = stats.Attack;

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
                damageDealer.Cooldown = attackCooldown;
            }

            circleCollider = GetComponent<CircleCollider2D>();
            if (circleCollider == null)
            {
                circleCollider = gameObject.AddComponent<CircleCollider2D>();
            }

            if (circleCollider != null)
            {
                circleCollider.radius = 0.38f;
                circleCollider.isTrigger = false;
                circleCollider.sharedMaterial = new PhysicsMaterial2D("Enemy_NoFriction")
                {
                    friction = 0f,
                    bounciness = 0f
                };
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

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = CreateSquareSprite(new Color(1f, 0.12f, 0.08f));
                spriteRenderer.color = Color.white;
                spriteRenderer.sortingOrder = 10;
            }

            directionArrow = SetupDirectionArrow();
            pathLineRenderer = SetupPathLineRenderer();
            FindTargetIfMissing();
            Debug.Log("Physics movement enabled for Enemy");
            Debug.Log("Enemy collision configured");
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
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = CreateSquareSprite(new Color(1f, 0.12f, 0.08f));
                spriteRenderer.color = Color.white;
                spriteRenderer.sortingOrder = 10;
            }
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

            SpriteRenderer arrowRenderer = arrow.GetComponent<SpriteRenderer>();
            if (arrowRenderer == null)
            {
                arrowRenderer = arrow.gameObject.AddComponent<SpriteRenderer>();
            }

            if (arrowRenderer != null)
            {
                arrowRenderer.sprite = CreateTriangleSprite(new Color(1f, 1f, 1f));
                arrowRenderer.color = Color.white;
                arrowRenderer.sortingOrder = 11;
            }

            return arrow;
        }

        private LineRenderer SetupPathLineRenderer()
        {
            LineRenderer lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            if (lineRenderer == null)
            {
                return null;
            }

            Shader spriteShader = Shader.Find("Sprites/Default");
            if (spriteShader != null)
            {
                lineRenderer.material = new Material(spriteShader);
            }

            lineRenderer.enabled = true;
            lineRenderer.useWorldSpace = true;
            lineRenderer.startWidth = 0.06f;
            lineRenderer.endWidth = 0.06f;
            lineRenderer.startColor = new Color(1f, 0f, 0f, 0.9f);
            lineRenderer.endColor = new Color(1f, 0.7f, 0.1f, 0.9f);
            lineRenderer.sortingOrder = 12;
            lineRenderer.positionCount = 0;

            return lineRenderer;
        }

        private void FindTargetIfMissing()
        {
            if (target != null)
            {
                return;
            }

            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                target = player.transform;
                if (enableDebugLogs)
                {
                    Debug.Log($"Enemy target acquired: {target.name}", this);
                }
            }
        }

        private void UpdateChaseState()
        {
            moveDirection = Vector2.zero;
            isChasing = false;

            if (target == null)
            {
                ClearPath();
                return;
            }

            Vector2 enemyPosition = rb != null ? rb.position : (Vector2)transform.position;
            Vector2 playerPosition = (Vector2)target.position;
            float distanceToTarget = Vector2.Distance(enemyPosition, playerPosition);
            Vector2 toTarget = playerPosition - enemyPosition;

            float stopDistance = enableDebugLogs ? 0.1f : attackRange;
            if (distanceToTarget > detectionRange || distanceToTarget <= stopDistance)
{
                if (distanceToTarget > 0.001f)
                {
                    lastLookDirection = toTarget.normalized;
                }

                ClearPath();
                return;
            }

            pathRefreshTimer -= Time.deltaTime;
            if (pathRefreshTimer <= 0f)
            {
                RefreshPath();
                pathRefreshTimer = pathRefreshInterval;
            }

            FollowCurrentPath();
            isChasing = moveDirection.sqrMagnitude > 0.001f;
        }

        private void RefreshPath()
        {
            if (target == null)
            {
                ClearPath();
                return;
            }

            RefreshPathTo(target.position);
        }

        private void RefreshPathTo(Vector3 destination)
        {
            currentPath.Clear();
            pathIndex = 0;

            PathfindingGrid grid = specificGrid != null ? specificGrid : PathfindingGrid.Instance;
            if (grid == null)
            {
                UpdatePathLineRenderer();
                return;
            }

            if (pathfindingMode == PathfindingMode.BFS)
            {
                currentPath.AddRange(BreadthFirstSearchPathfinder.FindPath(grid, transform.position, destination, PathfindingBenchmarkUI.BFSStats));
            }
            else
            {
                currentPath.AddRange(AStarPathfinder.FindPath(grid, transform.position, destination, PathfindingBenchmarkUI.AStarStats));
            }

            UpdatePathLineRenderer();
        }

        private void FollowCurrentPath()
        {
moveDirection = Vector2.zero;

            if (currentPath.Count == 0 || pathIndex >= currentPath.Count)
            {
                return;
            }

            Vector2 currentPosition = rb != null ? rb.position : (Vector2)transform.position;
            Vector2 waypoint = (Vector2)currentPath[pathIndex];

            while (Vector2.Distance(currentPosition, waypoint) <= waypointReachDistance)
            {
                pathIndex++;
                if (pathIndex >= currentPath.Count)
                {
                    UpdatePathLineRenderer();
                    return;
                }

                waypoint = (Vector2)currentPath[pathIndex];
            }

            moveDirection = (waypoint - currentPosition).normalized;
            lastLookDirection = moveDirection;
        }

        private void ClearPath()
        {
            currentPath.Clear();
            pathIndex = 0;
            UpdatePathLineRenderer();
        }

        private void UpdatePathLineRenderer()
        {
            if (pathLineRenderer == null)
            {
                return;
            }

            if (!showPathDebug)
            {
                pathLineRenderer.positionCount = 0;
                return;
            }

            // Algorithm-specific colors
            if (pathfindingMode == PathfindingMode.BFS)
            {
                pathLineRenderer.startColor = Color.cyan;
                pathLineRenderer.endColor = new Color(0, 0.5f, 1f, 0.8f);
                pathLineRenderer.startWidth = 0.4f;
                pathLineRenderer.endWidth = 0.4f;
            }
            else
            {
                pathLineRenderer.startColor = Color.yellow;
                pathLineRenderer.endColor = new Color(1f, 0.5f, 0f, 0.8f);
                pathLineRenderer.startWidth = 0.4f;
                pathLineRenderer.endWidth = 0.4f;
            }

            int remainingCount = Mathf.Max(0, currentPath.Count - pathIndex);
            pathLineRenderer.positionCount = remainingCount;

            for (int i = 0; i < remainingCount; i++)
            {
                Vector3 point = currentPath[pathIndex + i];
                point.z = -0.1f;
                pathLineRenderer.SetPosition(i, point);
            }
        }

        private void UpdateVisualFeedback()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Time.time < flashUntilTime ? Color.white : isChasing ? new Color(1f, 0.35f, 0.25f) : baseTint;
            }

            if (directionArrow == null)
            {
                return;
            }

            Vector2 safeDirection = lastLookDirection.sqrMagnitude > 0.001f ? lastLookDirection.normalized : Vector2.down;
            directionArrow.localPosition = safeDirection * 0.65f;
            directionArrow.localScale = new Vector3(0.35f, 0.35f, 1f);

            float angle = Mathf.Atan2(safeDirection.y, safeDirection.x) * Mathf.Rad2Deg - 90f;
            directionArrow.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void DebugCombatDistance()
        {
            if (!enableDebugLogs)
            {
                return;
            }

            if (Time.time < nextCombatDebugTime)
            {
                return;
            }

            nextCombatDebugTime = Time.time + 1f;

            Health playerHealth = target != null ? target.GetComponent<Health>() : null;
            float distance = target != null ? Vector2.Distance((Vector2)transform.position, (Vector2)target.position) : -1f;
            float cooldownRemaining = Mathf.Max(0f, nextAttackTime - Time.time);

            Debug.Log(
                $"Enemy combat debug | enemy: {transform.position} | player: {(target != null ? target.position.ToString() : "null")} | distance: {distance:0.00} | attackRange: {attackRange:0.00} | cooldownRemaining: {cooldownRemaining:0.00} | playerHealthNull: {playerHealth == null}",
                this);
        }

        private void TryAttackPlayerByDistance()
        {
            if (target == null || IsPlayerDead())
            {
                return;
            }

            float distance = Vector2.Distance((Vector2)transform.position, (Vector2)target.position);
            if (distance > attackRange)
            {
                return;
            }

            if (Time.time < nextAttackTime)
            {
                return;
            }

            Health playerHealth = target.GetComponent<Health>();
            if (playerHealth == null)
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning($"Enemy attack skipped: target {target.name} has no Health.", this);
                }

                return;
            }

            if (damageDealer == null)
            {
                return;
            }

            Debug.Log($"ENEMY ATTACK PLAYER: damage={damageDealer.Damage}", this);
            damageDealer.TryDealDamage(playerHealth);
            lastAttackTime = Time.time;
            nextAttackTime = Time.time + attackCooldown;
        }

        private bool IsPlayerDead()
        {
            Health playerHealth = target != null ? target.GetComponent<Health>() : null;
            return playerHealth != null && playerHealth.IsDead;
        }

        private void UpdateReturnToSpawn()
        {
            isChasing = false;

            float distanceToSpawn = Vector2.Distance((Vector2)transform.position, (Vector2)spawnPosition);
            if (distanceToSpawn <= 0.12f)
            {
                moveDirection = Vector2.zero;
                ClearPath();
                return;
            }

            pathRefreshTimer -= Time.deltaTime;
            if (pathRefreshTimer <= 0f)
            {
                RefreshPathTo(spawnPosition);
                pathRefreshTimer = pathRefreshInterval;
            }

            FollowCurrentPath();
            if (moveDirection.sqrMagnitude <= 0.001f)
            {
                Vector2 toSpawn = (Vector2)(spawnPosition - transform.position);
                moveDirection = toSpawn.sqrMagnitude > 0.001f ? toSpawn.normalized : Vector2.zero;
            }

            if (moveDirection.sqrMagnitude > 0.001f)
            {
                lastLookDirection = moveDirection;
            }
        }

        private void SubscribeHealthEvents()
        {
            if (health == null)
            {
                return;
            }

            health.OnDied += OnDied;
        }

        private void UnsubscribeHealthEvents()
        {
            if (health == null)
            {
                return;
            }

            health.OnDied -= OnDied;
        }

        private void OnDied(Health deadHealth)
        {
            GameObject player = GameObject.Find("Player");
            PlayerLevel playerLevel = player != null ? player.GetComponent<PlayerLevel>() : null;
            if (playerLevel != null)
            {
                playerLevel.AddExp(10);
            }

            Destroy(gameObject);
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
