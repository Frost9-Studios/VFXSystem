using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Frost9.VFX;

namespace Project.VfxSandbox
{
    /// <summary>
    /// Manual sandbox for validating Layer 1 VFX behavior without touching package runtime code.
    /// </summary>
    public class VfxLayer1SandboxController : MonoBehaviour
    {
        [Header("Catalog Binding")]
        [SerializeField]
        [Tooltip("Prefab used as the test effect in this sandbox catalog.")]
        private GameObject vfxPrefab;

        [SerializeField]
        [Tooltip("Optional camera override for click-to-spawn input.")]
        private Camera targetCamera;

        [SerializeField]
        [Tooltip("Optional target used by PlayOn tests.")]
        private Transform attachTarget;

        [SerializeField]
        [Tooltip("Create a default moving target when attachTarget is not assigned.")]
        private bool autoCreateAttachTarget = true;

        [SerializeField]
        [Tooltip("Auto-configure a basic camera + light layout when the scene is empty.")]
        private bool autoConfigureSceneDefaults = true;

        [Header("Spawn Settings")]
        [SerializeField]
        [Min(0.01f)]
        [Tooltip("Default effect lifetime override in seconds.")]
        private float defaultLifetimeSeconds = 1.2f;

        [SerializeField]
        [Min(1)]
        [Tooltip("How many effects to spawn for the spam test.")]
        private int spamCount = 24;

        [SerializeField]
        [Min(0.1f)]
        [Tooltip("Radius used for radial spam spawns.")]
        private float spamRadius = 4f;

        [SerializeField]
        [Min(1)]
        [Tooltip("Burst size used by deterministic pool reuse verification.")]
        private int verificationBurstCount = 16;

        [SerializeField]
        [Min(0.05f)]
        [Tooltip("Lifetime used by deterministic pool reuse verification.")]
        private float verificationLifetimeSeconds = 0.35f;

        [SerializeField]
        [Tooltip("Draw keyboard controls and stats via runtime Canvas overlay.")]
        private bool showOnScreenOverlay = true;

        [SerializeField]
        [Tooltip("When enabled, keeps pool manager in active scene hierarchy instead of DontDestroyOnLoad.")]
        private bool showPoolRootInSceneHierarchy;

        [SerializeField]
        [Tooltip("Write periodic stats to the Console as a fallback when UI is not visible.")]
        private bool enablePeriodicStatsLog = true;

        [SerializeField]
        [Min(0.25f)]
        [Tooltip("Interval for periodic stats logs in seconds.")]
        private float statsLogIntervalSeconds = 2f;

        private IVfxService vfxService;
        private VfxCatalog runtimeCatalog;
        private VfxSystemConfiguration runtimeConfiguration;
        private GameObject poolManagerObject;
        private VfxHandle lastHandle;
        private bool isRunningStaleHandleCheck;
        private bool isRunningPoolReuseCheck;
        private Canvas overlayCanvas;
        private Text overlayText;
        private float nextStatsLogTime;

        /// <summary>
        /// Initializes sandbox runtime objects and service.
        /// </summary>
        private void Awake()
        {
            if (autoConfigureSceneDefaults)
            {
                EnsureSceneDefaults();
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (vfxPrefab == null)
            {
                Debug.LogError("[VfxLayer1Sandbox] No vfxPrefab assigned. Sandbox disabled.");
                enabled = false;
                return;
            }

            EnsureAttachTarget();
            EnsureOverlayUI();
            InitializeService();
        }

        /// <summary>
        /// Handles keyboard and mouse controls for sandbox testing.
        /// </summary>
        private void Update()
        {
            if (vfxService == null)
            {
                return;
            }

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null)
            {
                return;
            }

            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                SpawnAtMousePosition(mouse.position.ReadValue());
            }

            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                SpawnAtPoint(Vector3.zero);
            }

            if (keyboard.digit2Key.wasPressedThisFrame)
            {
                SpawnSpamBurst();
            }

            if (keyboard.oKey.wasPressedThisFrame)
            {
                SpawnOnTarget();
            }

            if (keyboard.sKey.wasPressedThisFrame)
            {
                var stopped = vfxService.Stop(lastHandle);
                Debug.Log($"[VfxLayer1Sandbox] Stop(lastHandle) -> {stopped}");
            }

            if (keyboard.gKey.wasPressedThisFrame)
            {
                if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
                {
                    var globalStopped = vfxService.StopAll(VfxStopFilter.Global);
                    Debug.Log($"[VfxLayer1Sandbox] StopAll(Global) stopped {globalStopped} effects.");
                }
                else
                {
                    var defaultStopped = vfxService.StopAll();
                    Debug.Log($"[VfxLayer1Sandbox] StopAll(Default Gameplay) stopped {defaultStopped} effects.");
                }
            }

            if (keyboard.uKey.wasPressedThisFrame)
            {
                var update = VfxParams.Empty
                    .WithScale(Random.Range(0.6f, 1.7f))
                    .WithIntensity(Random.Range(0.5f, 1.8f))
                    .WithColor(Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.7f, 1f));

                var updated = vfxService.TryUpdate(lastHandle, in update);
                Debug.Log($"[VfxLayer1Sandbox] TryUpdate(lastHandle) -> {updated}");
            }

            if (keyboard.hKey.wasPressedThisFrame && !isRunningStaleHandleCheck)
            {
                StartCoroutine(RunStaleHandleCheck());
            }

            if (keyboard.vKey.wasPressedThisFrame && !isRunningPoolReuseCheck)
            {
                StartCoroutine(RunPoolReuseVerification());
            }

            UpdateOverlayText();
            TickStatsLog();
        }

        /// <summary>
        /// Releases sandbox-created runtime resources.
        /// </summary>
        private void OnDestroy()
        {
            vfxService?.Dispose();
            vfxService = null;

            if (runtimeCatalog != null)
            {
                Destroy(runtimeCatalog);
            }

            if (runtimeConfiguration != null)
            {
                Destroy(runtimeConfiguration);
            }

            if (poolManagerObject != null)
            {
                Destroy(poolManagerObject);
            }

            if (overlayCanvas != null)
            {
                Destroy(overlayCanvas.gameObject);
            }
        }

        private void InitializeService()
        {
            runtimeCatalog = ScriptableObject.CreateInstance<VfxCatalog>();
            runtimeCatalog.SetEntries(new[]
            {
                new VfxCatalogEntry(VFXRefs.Effects.VfxPrefab, vfxPrefab)
            });

            runtimeConfiguration = ScriptableObject.CreateInstance<VfxSystemConfiguration>();
            runtimeConfiguration.SetDefaultsForRuntime(
                runtimeCatalog,
                initialPoolSize: 4,
                maxPoolSize: 64,
                maxActive: 512,
                dontDestroyPoolRoot: !showPoolRootInSceneHierarchy,
                configuredPoolRootName: "Sandbox_VFXPoolRoot");

            poolManagerObject = new GameObject("Sandbox_VFXPoolManager");
            var poolManager = poolManagerObject.AddComponent<VfxPoolManager>();
            vfxService = new VfxService(poolManager, runtimeCatalog, runtimeConfiguration);

            var poolScope = showPoolRootInSceneHierarchy ? "active scene hierarchy" : "DontDestroyOnLoad scene";
            Debug.Log($"[VfxLayer1Sandbox] Pool manager initialized. Look for 'Sandbox_VFXPoolManager' under {poolScope}.");
        }

        private void EnsureAttachTarget()
        {
            if (attachTarget != null || !autoCreateAttachTarget)
            {
                return;
            }

            var targetObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            targetObject.name = "SandboxAttachTarget";
            targetObject.transform.position = new Vector3(0f, 1f, 3f);
            var orbit = targetObject.AddComponent<VfxSandboxOrbitTarget>();
            orbit.SetDefaults(radius: 2f, speedDegrees: 45f, center: new Vector3(0f, 1f, 3f));
            attachTarget = targetObject.transform;
        }

        private void EnsureSceneDefaults()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                camera = Object.FindFirstObjectByType<Camera>();
            }

            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                camera = cameraObject.AddComponent<Camera>();
            }

            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 8f, -10f);
            camera.transform.rotation = Quaternion.Euler(30f, 0f, 0f);

            var light = Object.FindFirstObjectByType<Light>();
            if (light == null)
            {
                var lightObject = new GameObject("Directional Light");
                light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
            }

            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private void SpawnAtMousePosition(Vector2 pointerPosition)
        {
            var spawnPoint = ResolveMouseSpawnPoint(pointerPosition);
            SpawnAtPoint(spawnPoint);
        }

        private void SpawnAtPoint(Vector3 point)
        {
            var parameters = VfxParams.Empty.WithLifetimeOverride(defaultLifetimeSeconds);
            lastHandle = vfxService.PlayAt(VFXRefs.Effects.VfxPrefab, point, Quaternion.identity, parameters);
        }

        private void SpawnSpamBurst()
        {
            var basePosition = ResolveMouseSpawnPoint(GetPointerPositionOrScreenCenter());
            for (var i = 0; i < spamCount; i++)
            {
                var angle = i * Mathf.PI * 2f / spamCount;
                var offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * spamRadius;
                var position = basePosition + offset;

                var parameters = VfxParams.Empty
                    .WithLifetimeOverride(Random.Range(0.6f, 1.8f))
                    .WithScale(Random.Range(0.75f, 1.45f));

                lastHandle = vfxService.PlayAt(VFXRefs.Effects.VfxPrefab, position, Quaternion.identity, parameters);
            }
        }

        private void SpawnOnTarget()
        {
            if (attachTarget == null)
            {
                Debug.LogWarning("[VfxLayer1Sandbox] No attach target assigned.");
                return;
            }

            var parameters = VfxParams.Empty
                .WithLifetimeOverride(2f)
                .WithScale(1.2f);

            lastHandle = vfxService.PlayOn(
                VFXRefs.Effects.VfxPrefab,
                attachTarget.gameObject,
                AttachMode.AttachToTransform,
                parameters);
        }

        private Vector3 ResolveMouseSpawnPoint(Vector2 pointerPosition)
        {
            if (targetCamera == null)
            {
                return Vector3.zero;
            }

            var ray = targetCamera.ScreenPointToRay(pointerPosition);
            var groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out var distance))
            {
                return ray.GetPoint(distance);
            }

            return targetCamera.transform.position + targetCamera.transform.forward * 8f;
        }

        private static Vector2 GetPointerPositionOrScreenCenter()
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                return mouse.position.ReadValue();
            }

            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }

        private void EnsureOverlayUI()
        {
            if (!showOnScreenOverlay || overlayCanvas != null)
            {
                return;
            }

            var canvasObject = new GameObject("VfxSandboxOverlayCanvas");
            overlayCanvas = canvasObject.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = 2000;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var raycaster = canvasObject.AddComponent<GraphicRaycaster>();
            raycaster.enabled = false;

            var panelObject = new GameObject("Panel");
            panelObject.transform.SetParent(canvasObject.transform, false);
            var panelRect = panelObject.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(12f, -12f);
            panelRect.sizeDelta = new Vector2(660f, 250f);

            var panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.55f);

            var textObject = new GameObject("Text");
            textObject.transform.SetParent(panelObject.transform, false);
            var textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = new Vector2(10f, 10f);
            textRect.offsetMax = new Vector2(-10f, -10f);

            overlayText = textObject.AddComponent<Text>();
            overlayText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            overlayText.fontSize = 17;
            overlayText.color = Color.white;
            overlayText.alignment = TextAnchor.UpperLeft;
            overlayText.horizontalOverflow = HorizontalWrapMode.Wrap;
            overlayText.verticalOverflow = VerticalWrapMode.Overflow;
            overlayText.supportRichText = false;
            overlayText.text = "VFX sandbox overlay initializing...";
        }

        private void UpdateOverlayText()
        {
            if (!showOnScreenOverlay || overlayText == null || vfxService == null)
            {
                return;
            }

            var stats = vfxService.GetStats();
            var poolScope = showPoolRootInSceneHierarchy ? "Scene" : "DontDestroyOnLoad";

            overlayText.text =
                "VFX Layer 1 Sandbox\n" +
                "LMB: Spawn | 1: Origin | 2: Burst | O: PlayOn\n" +
                "U: TryUpdate(last) | S: Stop(last) | H: Stale-handle check | V: Pool verify\n" +
                "G: StopAll(Gameplay) | Shift+G: StopAll(Global)\n\n" +
                $"Pool Scope: {poolScope} (Object: Sandbox_VFXPoolManager)\n" +
                $"Stats -> Active: {stats.TotalActiveInstances}, Pooled: {stats.TotalPooledInstances}, " +
                $"Created: {stats.TotalCreatedInstances}, Recycled: {stats.TotalRecycleCount}\n" +
                $"Last Handle Valid: {lastHandle.IsValid}";
        }

        private void TickStatsLog()
        {
            if (!enablePeriodicStatsLog || vfxService == null)
            {
                return;
            }

            if (Time.unscaledTime < nextStatsLogTime)
            {
                return;
            }

            nextStatsLogTime = Time.unscaledTime + statsLogIntervalSeconds;
            var stats = vfxService.GetStats();
            Debug.Log(
                $"[VfxLayer1Sandbox][Stats] Active={stats.TotalActiveInstances} " +
                $"Pooled={stats.TotalPooledInstances} Created={stats.TotalCreatedInstances} Recycled={stats.TotalRecycleCount}");
        }

        private IEnumerator RunStaleHandleCheck()
        {
            isRunningStaleHandleCheck = true;
            try
            {
                var shortHandle = vfxService.PlayAt(
                    VFXRefs.Effects.VfxPrefab,
                    Vector3.left * 2f,
                    Quaternion.identity,
                    VfxParams.Empty.WithLifetimeOverride(0.05f));

                yield return new WaitForSeconds(0.2f);

                var longHandle = vfxService.PlayAt(
                    VFXRefs.Effects.VfxPrefab,
                    Vector3.right * 2f,
                    Quaternion.identity,
                    VfxParams.Empty.WithLifetimeOverride(2f));

                var staleStop = vfxService.Stop(shortHandle);
                var validStop = vfxService.Stop(longHandle);

                Debug.Log($"[VfxLayer1Sandbox] Stale handle stop expected false -> {staleStop}");
                Debug.Log($"[VfxLayer1Sandbox] Fresh handle stop expected true -> {validStop}");
            }
            finally
            {
                isRunningStaleHandleCheck = false;
            }
        }

        private IEnumerator RunPoolReuseVerification()
        {
            isRunningPoolReuseCheck = true;
            try
            {
                Debug.Log("[VfxLayer1Sandbox] Pool reuse verification started.");
                var center = Vector3.zero;

                SpawnVerificationBurst(center);
                yield return new WaitForSeconds(verificationLifetimeSeconds + 0.4f);
                var afterFirstWave = vfxService.GetStats();

                SpawnVerificationBurst(center + Vector3.forward * 2f);
                yield return new WaitForSeconds(0.05f);
                var duringSecondWave = vfxService.GetStats();
                var createdDeltaSecondWave = duringSecondWave.TotalCreatedInstances - afterFirstWave.TotalCreatedInstances;

                var reuseLikely = createdDeltaSecondWave == 0;
                Debug.Log(
                    $"[VfxLayer1Sandbox] Pool reuse verification result: Reused={reuseLikely} " +
                    $"CreatedDeltaSecondWave={createdDeltaSecondWave} " +
                    $"AfterFirst(Created={afterFirstWave.TotalCreatedInstances}, Recycled={afterFirstWave.TotalRecycleCount}) " +
                    $"DuringSecond(Created={duringSecondWave.TotalCreatedInstances}, Recycled={duringSecondWave.TotalRecycleCount}).");
            }
            finally
            {
                isRunningPoolReuseCheck = false;
            }
        }

        private void SpawnVerificationBurst(Vector3 center)
        {
            for (var i = 0; i < verificationBurstCount; i++)
            {
                var angle = i * Mathf.PI * 2f / verificationBurstCount;
                var offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 2f;
                var parameters = VfxParams.Empty.WithLifetimeOverride(verificationLifetimeSeconds);
                vfxService.PlayAt(VFXRefs.Effects.VfxPrefab, center + offset, Quaternion.identity, parameters);
            }
        }
    }
}
