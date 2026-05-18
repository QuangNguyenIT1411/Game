using System.Collections;
using DungeonCrawler.Combat;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DungeonCrawler.Enemy
{
    public class EnemyVisualController : MonoBehaviour
    {
        private const string NormalSlimePrefabPath = "Assets/Art/Characters/Enemy/Slime/Slime/Prefabs/Green_Slime.prefab";
        private const string BFSSlimePrefabPath = "Assets/Art/Characters/Enemy/Slime/Slime/Prefabs/Blue_Slime.prefab";
        private const string BossSlimePrefabPath = "Assets/Art/Characters/Enemy/Slime/Slime/Prefabs/Purple_Slime.prefab";
        private const string BossSlimeFallbackPrefabPath = "Assets/Art/Characters/Enemy/Slime/Slime/Prefabs/Red_Slime.prefab";

        [SerializeField] private bool isBoss;
        [SerializeField] private GameObject normalSlimePrefab;
        [SerializeField] private GameObject bfsSlimePrefab;
        [SerializeField] private GameObject bossSlimePrefab;
        [SerializeField] private GameObject bossFallbackSlimePrefab;
        [SerializeField] public bool preserveInspectorScale = true;
[SerializeField] public Vector3 defaultNormalVisualScale = new Vector3(8f, 8f, 1f);
        [SerializeField] public Vector3 defaultBossVisualScale = new Vector3(12f, 12f, 1f);
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer[] renderers;
        [SerializeField] private SpriteRenderer bodyRenderer;

        private EnemyController enemyController;
        private Health health;
        private Color[] originalColors;
        private bool slimeLoaded;
        private bool loggedRendererState;
        private bool loggedRuntimeScale;
        private bool loggedBoundsSize;
        private bool capturedInspectorScale;
        private Vector3 baseEnemyVisualScale;
        private Vector3 baseSlimeVisualScale;

        public bool HasLoadedVisual => slimeLoaded && visualRoot != null && visualRoot.GetComponentsInChildren<SpriteRenderer>(true).Length > 0;

        private void Awake()
        {
            enemyController = GetComponent<EnemyController>();
            health = GetComponent<Health>();
            // Capture scale as early as possible before any logic might modify it
            CaptureInspectorScaleIfNeeded();
            LoadVisualIfNeeded();
            SubscribeHealth();
        }

        private void Start()
        {
            Debug.Log("EnemyVisualController Start", this);
            LoadVisualIfNeeded();
            // Final capture attempt just in case visual was loaded here
            CaptureInspectorScaleIfNeeded();
            EnsureRuntimeVisualActive();
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.OnDied -= OnDied;
            }
        }

        private void Update()
        {
            UpdateAnimatorState();
        }

        private void LateUpdate()
        {
            EnsureRuntimeVisualActive(false);
        }

        public void Configure(bool boss)
        {
            Configure(boss, false);
        }

        public void Configure(bool boss, bool forceReloadVisual)
        {
            isBoss = boss;
            ResetVisualLogs();
            LoadVisualIfNeeded(forceReloadVisual);
        }

        public void Configure(bool boss, GameObject normalPrefab, GameObject bossPrefab, GameObject fallbackBossPrefab)
        {
            Configure(boss, normalPrefab, bossPrefab, fallbackBossPrefab, false);
        }

        public void Configure(bool boss, GameObject normalPrefab, GameObject bossPrefab, GameObject fallbackBossPrefab, bool forceReloadVisual)
        {
            Configure(boss, normalPrefab, null, bossPrefab, fallbackBossPrefab, forceReloadVisual);
        }

        public void Configure(bool boss, GameObject normalPrefab, GameObject bfsPrefab, GameObject bossPrefab, GameObject fallbackBossPrefab, bool forceReloadVisual)
        {
            isBoss = boss;
            normalSlimePrefab = normalPrefab;
            bfsSlimePrefab = bfsPrefab;
            bossSlimePrefab = bossPrefab;
            bossFallbackSlimePrefab = fallbackBossPrefab;
            ResetVisualLogs();
            LoadVisualIfNeeded(forceReloadVisual);
        }

        public void SetBaseTint(Color tint)
{
            if (renderers == null)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].color = tint;
                    if (originalColors != null && i < originalColors.Length)
                    {
                        originalColors[i] = tint;
                    }
                }
            }
        }

        public void PlayHurtFeedback()
        {
            if (!slimeLoaded || renderers == null || renderers.Length == 0)
            {
                return;
            }

            StopCoroutine(nameof(FlashRoutine));
            StartCoroutine(nameof(FlashRoutine));
            TrySetTrigger("hurt");
        }

        public void ResetVisualScaleToDefault()
        {
            if (visualRoot == null)
            {
                Transform existingRoot = transform.Find("EnemyVisual");
                if (existingRoot == null)
                {
                    GameObject rootObject = new GameObject("EnemyVisual");
                    rootObject.transform.SetParent(transform, false);
                    existingRoot = rootObject.transform;
                }

                visualRoot = existingRoot;
            }

            visualRoot.localScale = GetDefaultVisualRootScale();
            capturedInspectorScale = false;
            CaptureInspectorScaleIfNeeded();
        }

        private void LoadVisualIfNeeded(bool forceReload = false)
        {
            if (slimeLoaded && !forceReload)
            {
                return;
            }

            CleanOrphanSlimeVisuals();

            if (visualRoot == null)
            {
                Transform existingRoot = transform.Find("EnemyVisual");
                if (existingRoot == null)
                {
                    GameObject rootObject = new GameObject("EnemyVisual");
                    rootObject.transform.SetParent(transform, false);
                    existingRoot = rootObject.transform;
                }

                visualRoot = existingRoot;
            }

            // Capture inspector scale immediately after finding/creating visualRoot
            CaptureInspectorScaleIfNeeded();

            bool hasExistingSlimeChild = visualRoot.childCount > 0;
            visualRoot.localPosition = Vector3.zero;
            visualRoot.localRotation = Quaternion.identity;

            if (forceReload)
            {
                for (int i = visualRoot.childCount - 1; i >= 0; i--)
                {
                    Transform child = visualRoot.GetChild(i);
                    child.SetParent(null);
                    DestroyObject(child.gameObject);
                }
            }

            CleanDuplicateSlimeVisualChildren();

            if (visualRoot.childCount == 0)
            {
                if (!hasExistingSlimeChild || forceReload)
                {
                    ApplyDefaultVisualRootScaleIfNeeded();
                }

                GameObject prefab = LoadSlimePrefab();
                if (prefab != null)
                {
                    GameObject visual = Instantiate(prefab, visualRoot);
                    visual.name = "SlimeVisual";
                    visual.transform.localPosition = Vector3.zero;
                    visual.transform.localRotation = Quaternion.identity;
                    
                    // Capture child scale if it wasn't captured yet
                    CaptureInspectorScaleIfNeeded();
                    
                    DisableDemoScripts(visual);
                    slimeLoaded = true;
                    Debug.Log("Created SlimeVisual under EnemyVisual", this);
                    Debug.Log($"SlimeVisual parent path: {GetPath(visual.transform)}", this);
                    Debug.Log(isBoss ? "Boss slime visual loaded" : "Enemy slime visual loaded", this);
                }
                else
                {
                    Debug.LogWarning($"Slime prefab missing at runtime | normalNull={normalSlimePrefab == null} bossNull={bossSlimePrefab == null} bossFallbackNull={bossFallbackSlimePrefab == null}", this);
                }
            }
            else
            {
                slimeLoaded = true;
            }

            EnsureSlimeVisualChildName();
            CaptureInspectorScaleIfNeeded();
            EnsureRuntimeVisualActive(true);

            if (!slimeLoaded)
            {
                Debug.Log("Fallback placeholder used for Enemy", this);
            }
        }

        private void ResetVisualLogs()
        {
            loggedRuntimeScale = false;
            loggedBoundsSize = false;
            loggedRendererState = false;
            capturedInspectorScale = false;
        }

        public static void CleanOrphanSlimeVisuals()
        {
            foreach (Transform candidate in FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (candidate == null || candidate.name != "SlimeVisual")
                {
                    continue;
                }

                if (IsValidSlimeVisualParent(candidate))
                {
                    continue;
                }

                string path = GetPath(candidate);
                Debug.Log($"Cleaned orphan SlimeVisual: {path}", candidate);
                DestroyObject(candidate.gameObject);
            }
        }

        private void CleanDuplicateSlimeVisualChildren()
        {
            if (visualRoot == null)
            {
                return;
            }

            bool foundSlimeVisual = false;
            for (int i = visualRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = visualRoot.GetChild(i);
                if (child == null || child.name != "SlimeVisual")
                {
                    continue;
                }

                if (!foundSlimeVisual)
                {
                    foundSlimeVisual = true;
                    continue;
                }

                string path = GetPath(child);
                Debug.Log($"Cleaned orphan SlimeVisual: {path}", child);
                child.SetParent(null);
                DestroyObject(child.gameObject);
            }
        }

        private static bool IsValidSlimeVisualParent(Transform candidate)
        {
            return candidate.parent != null
                && candidate.parent.name == "EnemyVisual"
                && candidate.parent.parent != null
                && candidate.parent.parent.GetComponent<EnemyVisualController>() != null;
        }

        private static void DestroyObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private GameObject LoadSlimePrefab()
        {
            if (isBoss)
            {
                if (bossSlimePrefab != null)
                {
                    return bossSlimePrefab;
                }

                if (bossFallbackSlimePrefab != null)
                {
                    return bossFallbackSlimePrefab;
                }
            }
            else
            {
                if (enemyController != null && enemyController.Mode == EnemyController.PathfindingMode.BFS)
                {
                    if (bfsSlimePrefab != null) return bfsSlimePrefab;
                }
                else if (normalSlimePrefab != null)
                {
                    return normalSlimePrefab;
                }
            }

        #if UNITY_EDITOR
            if (isBoss)
            {
                GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossSlimePrefabPath);
                if (bossPrefab != null)
                {
                    return bossPrefab;
                }

                return AssetDatabase.LoadAssetAtPath<GameObject>(BossSlimeFallbackPrefabPath);
            }

            if (enemyController != null && enemyController.Mode == EnemyController.PathfindingMode.BFS)
            {
                GameObject bfsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BFSSlimePrefabPath);
                if (bfsPrefab != null) return bfsPrefab;
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(NormalSlimePrefabPath);
        #else
            return null;
        #endif
        }

        private void EnsureRuntimeVisualActive(bool logState = true)
        {
            CacheVisualComponents();
            bool hasSlimeChild = renderers != null && renderers.Length > 0;
            slimeLoaded = slimeLoaded || hasSlimeChild;

            if (logState)
            {
                Debug.Log($"Has slime child: {hasSlimeChild}", this);
            }

            SetPlaceholderVisible(!hasSlimeChild);
            if (hasSlimeChild)
            {
                ForceSlimeRenderersVisible();
                SelectAndFixBodyRenderer(logState);
                ApplyVisualScale();
                LogBodyWorldBoundsOnce();
                if (logState)
                {
                    Debug.Log("Placeholder disabled", this);
                    Debug.Log("Enemy slime visual active at runtime", this);
                    LogRendererStateOnce();
                }
            }
        }

        private void ForceSlimeRenderersVisible()
        {
            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                renderer.enabled = true;
                renderer.sortingOrder = IsShadowRenderer(renderer) ? 29 : 30;
                if (IsShadowRenderer(renderer))
                {
                    renderer.enabled = false;
                }
            }
        }

        private void SelectAndFixBodyRenderer(bool logState)
        {
            bodyRenderer = FindBestBodyRenderer();
            if (bodyRenderer == null)
            {
                return;
            }

            bodyRenderer.enabled = true;
            Color color = bodyRenderer.color;
            bodyRenderer.color = new Color(color.r, color.g, color.b, 1f);
            bodyRenderer.sortingOrder = 30;

            if (bodyRenderer.sprite == null || bodyRenderer.sprite.bounds.size.sqrMagnitude <= 0.0001f)
            {
                Sprite fallback = FindFallbackSlimeSprite();
                if (fallback != null)
                {
                    bodyRenderer.sprite = fallback;
                }
            }

            if (animator != null && (bodyRenderer.sprite == null || bodyRenderer.color.a < 0.5f))
            {
                animator.enabled = false;
                Sprite fallback = FindFallbackSlimeSprite();
                if (fallback != null)
                {
                    bodyRenderer.sprite = fallback;
                    bodyRenderer.color = Color.white;
                }
            }

            if (logState)
            {
                Debug.Log($"Selected slime body renderer: {GetPath(bodyRenderer.transform)}, sprite={(bodyRenderer.sprite != null ? bodyRenderer.sprite.name : "null")}", bodyRenderer);
            }
        }

        private SpriteRenderer FindBestBodyRenderer()
        {
            SpriteRenderer best = null;
            float bestScore = float.MinValue;
            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer == null || renderer.sprite == null)
                {
                    continue;
                }

                if (IsShadowRenderer(renderer))
                {
                    continue;
                }

                float alpha = renderer.color.a;
                float size = renderer.sprite.bounds.size.x * renderer.sprite.bounds.size.y;
                float nameBonus = renderer.sprite.name.ToLowerInvariant().Contains("slime") || renderer.name.ToLowerInvariant().Contains("slime") ? 10f : 0f;
                float score = size + nameBonus + alpha * 5f;
                if (alpha <= 0.5f)
                {
                    score -= 100f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = renderer;
                }
            }

            return best;
        }

        private static bool IsShadowRenderer(SpriteRenderer renderer)
        {
            string rendererName = renderer.name.ToLowerInvariant();
            string spriteName = renderer.sprite != null ? renderer.sprite.name.ToLowerInvariant() : string.Empty;
            return rendererName.Contains("shadow")
                || rendererName.Contains("blob_shadow")
                || rendererName.Contains("floor_shadow")
                || rendererName.Contains("effect")
                || spriteName.Contains("shadow")
                || spriteName.Contains("effect");
        }

        private Vector3 GetDefaultVisualRootScale()
        {
            return isBoss ? defaultBossVisualScale : defaultNormalVisualScale;
        }

        private void ApplyDefaultVisualRootScaleIfNeeded()
        {
            if (visualRoot == null || preserveInspectorScale)
            {
                return;
            }

            visualRoot.localScale = GetDefaultVisualRootScale();
        }

        private void CaptureInspectorScaleIfNeeded()
        {
            if (capturedInspectorScale)
            {
                return;
            }

            // Ensure visualRoot is assigned if possible
            if (visualRoot == null)
            {
                visualRoot = transform.Find("EnemyVisual");
            }

            if (visualRoot == null)
            {
                return;
            }

            if (preserveInspectorScale)
            {
                baseEnemyVisualScale = visualRoot.localScale;
                Transform slimeVisual = GetSlimeVisualTransform();
                if (slimeVisual != null)
                {
                    baseSlimeVisualScale = slimeVisual.localScale;
                    capturedInspectorScale = true;
                    Debug.Log($"Preserving EnemyVisual inspector scale: {baseEnemyVisualScale}", this);
                    Debug.Log($"Preserving SlimeVisual inspector scale: {baseSlimeVisualScale}", slimeVisual);
                }
                else
                {
                    // If slime visual is not yet loaded, we capture root but don't mark as fully captured
                    // so we can capture the slime child later
                    baseSlimeVisualScale = Vector3.one;
                    Debug.Log($"Captured EnemyVisual inspector scale: {baseEnemyVisualScale}. Waiting for SlimeVisual...", this);
                }
            }
            else
            {
                baseEnemyVisualScale = GetDefaultVisualRootScale();
                baseSlimeVisualScale = Vector3.one;
                capturedInspectorScale = true;
                Debug.Log($"Using default enemy visual scale: {baseEnemyVisualScale}", this);
            }
        }

        private void ApplyVisualScale()
        {
            if (visualRoot == null)
            {
                return;
            }

            CaptureInspectorScaleIfNeeded();

            visualRoot.localScale = baseEnemyVisualScale;
            if (preserveInspectorScale)
            {
                Transform slimeVisual = GetSlimeVisualTransform();
                if (slimeVisual != null)
                {
                    slimeVisual.localScale = baseSlimeVisualScale;
                }
            }

            if (!loggedRuntimeScale)
            {
                Debug.Log($"EnemyVisual runtime scale = {visualRoot.localScale}", this);
                Transform slimeVisual = GetSlimeVisualTransform();
                if (slimeVisual != null)
                {
                    Debug.Log($"SlimeVisual runtime local scale = {slimeVisual.localScale}, world scale = {slimeVisual.lossyScale}", slimeVisual);
                }

                loggedRuntimeScale = true;
            }
        }

        private Transform GetSlimeVisualTransform()
        {
            if (visualRoot == null || visualRoot.childCount == 0)
            {
                return null;
            }

            return visualRoot.GetChild(0);
        }

        private void EnsureSlimeVisualChildName()
        {
            Transform slimeVisual = GetSlimeVisualTransform();
            if (slimeVisual == null)
            {
                return;
            }

            slimeVisual.name = "SlimeVisual";
        }

        private void LogBodyWorldBoundsOnce()
        {
            if (loggedBoundsSize || bodyRenderer == null)
            {
                return;
            }

            Debug.Log($"Slime body world bounds size after scale = {bodyRenderer.bounds.size}", bodyRenderer);
            loggedBoundsSize = true;
        }

        private void CacheVisualComponents()
        {
            renderers = visualRoot != null ? visualRoot.GetComponentsInChildren<SpriteRenderer>(true) : new SpriteRenderer[0];
            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sortingOrder = IsShadowRenderer(renderers[i]) ? 29 : 30;
                renderers[i].enabled = !IsShadowRenderer(renderers[i]);
                originalColors[i] = renderers[i].color;
            }

            animator = visualRoot != null ? visualRoot.GetComponentInChildren<Animator>(true) : null;
        }

        private void SetPlaceholderVisible(bool visible)
        {
            SpriteRenderer placeholder = GetComponent<SpriteRenderer>();
            if (placeholder != null)
            {
                placeholder.enabled = visible;
                if (!visible)
                {
                    placeholder.sprite = null;
                    placeholder.color = new Color(placeholder.color.r, placeholder.color.g, placeholder.color.b, 0f);
                }

                placeholder.sortingOrder = 0;
            }
        }

        private void LogRendererStateOnce()
        {
            if (loggedRendererState)
            {
                return;
            }

            SpriteRenderer rootRenderer = GetComponent<SpriteRenderer>();
            if (rootRenderer != null)
            {
                Debug.Log($"Renderer state | object={rootRenderer.gameObject.name} enabled={rootRenderer.enabled} sortingOrder={rootRenderer.sortingOrder} sprite={(rootRenderer.sprite != null ? rootRenderer.sprite.name : "null")}", rootRenderer);
            }

            foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>(true))
            {
                string spriteName = renderer.sprite != null ? renderer.sprite.name : "null";
                string boundsSize = renderer.sprite != null ? renderer.sprite.bounds.size.ToString() : "null";
                Debug.Log($"Renderer state | object={GetPath(renderer.transform)} enabled={renderer.enabled} sortingOrder={renderer.sortingOrder} sprite={spriteName} color={renderer.color} bounds={boundsSize}", renderer);
            }

            loggedRendererState = true;
        }

        private static string GetPath(Transform transform)
        {
            string path = transform.name;
            Transform current = transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }

        private Sprite FindFallbackSlimeSprite()
        {
#if UNITY_EDITOR
            Object[] assets = AssetDatabase.LoadAllAssetRepresentationsAtPath("Assets/Art/Characters/Enemy/Slime/Slime/Sprites/Slime.png");
            Sprite best = null;
            float bestSize = 0f;
            foreach (Object asset in assets)
            {
                if (asset is not Sprite sprite)
                {
                    continue;
                }

                string spriteName = sprite.name.ToLowerInvariant();
                if (spriteName.Contains("shadow") || spriteName.Contains("effect"))
                {
                    continue;
                }

                float size = sprite.bounds.size.x * sprite.bounds.size.y;
                if (size > bestSize)
                {
                    bestSize = size;
                    best = sprite;
                }
            }

            return best;
#else
            return null;
#endif
        }

        private void DisableDemoScripts(GameObject visual)
        {
            MonoBehaviour[] behaviours = visual.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour != null && behaviour.GetType().Name == "SlimeAnimator")
                {
                    behaviour.enabled = false;
                }
            }
        }

        private void SubscribeHealth()
        {
            if (health == null)
            {
                return;
            }

            health.Damaged -= OnDamaged;
            health.Damaged += OnDamaged;
            health.OnDied -= OnDied;
            health.OnDied += OnDied;
        }

        private void OnDamaged(Health damagedHealth, int damage)
        {
            PlayHurtFeedback();
        }

        private void OnDied(Health deadHealth)
        {
            TrySetTrigger("die");
        }

        private IEnumerator FlashRoutine()
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].color = Color.red;
                }
            }

            yield return new WaitForSeconds(0.12f);

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].color = i < originalColors.Length ? originalColors[i] : Color.white;
                }
            }
        }

        private void UpdateAnimatorState()
        {
            if (animator == null || enemyController == null)
            {
                return;
            }

            if (enemyController.IsRecentlyAttacking)
            {
                TrySetTrigger("jump_attack");
            }
            else if (enemyController.IsMoving)
            {
                TrySetTrigger("jump_attack");
            }
            else
            {
                TrySetTrigger("idle");
            }
        }

        private void TrySetTrigger(string triggerName)
        {
            if (animator == null)
            {
                return;
            }

            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == triggerName)
                {
                    animator.SetTrigger(triggerName);
                    return;
                }
            }
        }
    }
}
