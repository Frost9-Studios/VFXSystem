using System.Collections;
using Frost9.VFX;
using Frost9.VFX.Integration.VContainer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;
using VContainer;

namespace Project.VfxSandbox
{
    /// <summary>
    /// Manual sandbox for validating Frost9.VFX runtime behavior from Play Mode.
    /// </summary>
    public class VfxLayer1SandboxController : MonoBehaviour
    {
        private enum SandboxBootstrapMode
        {
            DirectService = 0,
            VContainerHelper = 1
        }

        [Header("Catalog Binding")]
        [SerializeField]
        [Tooltip("Prefab used as the generic runtime prefab effect id (Effects.VfxPrefab).")]
        private GameObject vfxPrefab;

        [SerializeField]
        [Tooltip("Optional camera override for click-to-spawn input.")]
        private Camera targetCamera;

        [SerializeField]
        [Tooltip("Optional target used by PlayOn attach tests.")]
        private Transform attachTarget;

        [SerializeField]
        [Tooltip("Create a default moving target when attachTarget is not assigned.")]
        private bool autoCreateAttachTarget = true;

        [SerializeField]
        [Tooltip("Auto-configure a basic camera + light layout when the scene is empty.")]
        private bool autoConfigureSceneDefaults = true;

        [Header("Bootstrap")]
        [SerializeField]
        [Tooltip("How the sandbox creates IVfxService.")]
        private SandboxBootstrapMode bootstrapMode = SandboxBootstrapMode.DirectService;

        [SerializeField]
        [Tooltip("Allow runtime toggle between Direct and VContainer bootstrap modes with B.")]
        private bool allowRuntimeBootstrapToggle = true;

        [SerializeField]
        [Tooltip("Pool manager object name used by both direct and VContainer startup paths.")]
        private string poolManagerObjectName = "Sandbox_VFXPoolManager";

        [Header("Spawn Settings")]
        [SerializeField]
        [Min(0.01f)]
        [Tooltip("Default prefab-effect lifetime override in seconds.")]
        private float defaultLifetimeSeconds = 1.2f;

        [SerializeField]
        [Min(1)]
        [Tooltip("How many effects to spawn for the burst stress test.")]
        private int spamCount = 24;

        [SerializeField]
        [Min(0.1f)]
        [Tooltip("Radius used for radial burst spawns.")]
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
        [Tooltip("Default color used by the runtime line preview effect.")]
        private Color linePreviewColor = new Color(0.15f, 0.9f, 1f, 1f);

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("Default width used by line preview tests.")]
        private float linePreviewWidth = 0.08f;

        [Header("Diagnostics UI")]
        [SerializeField]
        [Tooltip("Draw keyboard controls and stats via runtime Canvas overlay.")]
        private bool showOnScreenOverlay = true;

        [SerializeField]
        [Tooltip("When enabled, keeps pool manager in active scene hierarchy instead of DontDestroyOnLoad.")]
        private bool showPoolRootInSceneHierarchy;

        [SerializeField]
        [Tooltip("Write periodic stats to Console as a fallback if overlay is hidden.")]
        private bool enablePeriodicStatsLog = true;

        [SerializeField]
        [Min(0.25f)]
        [Tooltip("Interval for periodic stats logs in seconds.")]
        private float statsLogIntervalSeconds = 2f;

        private static readonly VfxId LinePreviewId = new VfxId("Effects.LinePreview");

        private IVfxService vfxService;
        private IObjectResolver vfxResolver;
        private VfxCatalog runtimeCatalog;
        private VfxSystemConfiguration runtimeConfiguration;
        private GameObject poolManagerObject;
        private GameObject linePreviewPrefab;
        private Material linePreviewMaterial;
        private VfxHandle lastPrefabHandle;
        private VfxHandle lastUiHandle;
        private VfxHandle lastLineHandle;
        private AttachMode attachMode = AttachMode.FollowTransform;
        private bool ignoreTargetScaleOnAttach;
        private bool isRunningStaleHandleCheck;
        private bool isRunningPoolReuseCheck;
        private Canvas overlayCanvas;
        private Text overlayText;
        private float nextStatsLogTime;
        private string lastActionMessage = "Ready.";

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
                Debug.LogError("[VfxSandbox] No VFX prefab assigned. Sandbox disabled.");
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

            if (keyboard.pKey.wasPressedThisFrame)
            {
                CycleAttachMode();
            }

            if (keyboard.iKey.wasPressedThisFrame)
            {
                ignoreTargetScaleOnAttach = !ignoreTargetScaleOnAttach;
                SetActionMessage($"Ignore target scale: {ignoreTargetScaleOnAttach}");
            }

            if (keyboard.sKey.wasPressedThisFrame)
            {
                var stopped = vfxService.Stop(lastPrefabHandle);
                SetActionMessage($"Stop(last prefab handle) -> {stopped}");
            }

            if (keyboard.uKey.wasPressedThisFrame)
            {
                var update = VfxParams.Empty
                    .WithScale(Random.Range(0.6f, 1.7f))
                    .WithIntensity(Random.Range(0.5f, 1.8f))
                    .WithColor(Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.7f, 1f));

                var updated = vfxService.TryUpdate(lastPrefabHandle, in update);
                SetActionMessage($"TryUpdate(last prefab handle) -> {updated}");
            }

            if (keyboard.cKey.wasPressedThisFrame)
            {
                SpawnUiChannelEffect();
            }

            if (keyboard.gKey.wasPressedThisFrame)
            {
                StopByScope(keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
            }

            if (keyboard.lKey.wasPressedThisFrame)
            {
                SpawnOrUpdateLinePreview(forceRespawn: true);
            }

            if (keyboard.tKey.wasPressedThisFrame)
            {
                UpdateLineTargetFromMouse();
            }

            if (keyboard.kKey.wasPressedThisFrame)
            {
                var stopped = vfxService.Stop(lastLineHandle);
                SetActionMessage($"Stop(line handle) -> {stopped}");
            }

            if (keyboard.hKey.wasPressedThisFrame && !isRunningStaleHandleCheck)
            {
                StartCoroutine(RunStaleHandleCheck());
            }

            if (keyboard.vKey.wasPressedThisFrame && !isRunningPoolReuseCheck)
            {
                StartCoroutine(RunPoolReuseVerification());
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                ReinitializeService("Service reset (R)");
            }

            if (allowRuntimeBootstrapToggle && keyboard.bKey.wasPressedThisFrame)
            {
                bootstrapMode = bootstrapMode == SandboxBootstrapMode.DirectService
                    ? SandboxBootstrapMode.VContainerHelper
                    : SandboxBootstrapMode.DirectService;

                ReinitializeService($"Bootstrap mode switched to {bootstrapMode}");
            }

            if (keyboard.mKey.wasPressedThisFrame)
            {
                showOnScreenOverlay = !showOnScreenOverlay;
                if (overlayCanvas != null)
                {
                    overlayCanvas.enabled = showOnScreenOverlay;
                }
            }

            UpdateOverlayText();
            TickStatsLog();
        }

        /// <summary>
        /// Releases sandbox-created runtime resources.
        /// </summary>
        private void OnDestroy()
        {
            ShutdownService();

            DestroyUnityObject(runtimeCatalog);
            runtimeCatalog = null;

            DestroyUnityObject(runtimeConfiguration);
            runtimeConfiguration = null;

            DestroyUnityObject(linePreviewPrefab);
            linePreviewPrefab = null;

            DestroyUnityObject(linePreviewMaterial);
            linePreviewMaterial = null;

            if (overlayCanvas != null)
            {
                DestroyUnityObject(overlayCanvas.gameObject);
                overlayCanvas = null;
            }
        }

        private void InitializeService()
        {
            DestroyUnityObject(runtimeCatalog);
            DestroyUnityObject(runtimeConfiguration);
            DestroyUnityObject(linePreviewPrefab);
            DestroyUnityObject(linePreviewMaterial);

            linePreviewPrefab = CreateLinePreviewPrefab();

            runtimeCatalog = ScriptableObject.CreateInstance<VfxCatalog>();
            runtimeCatalog.SetEntries(new[]
            {
                new VfxCatalogEntry(VFXRefs.Effects.VfxPrefab, vfxPrefab),
                new VfxCatalogEntry(LinePreviewId, linePreviewPrefab)
            });

            runtimeConfiguration = ScriptableObject.CreateInstance<VfxSystemConfiguration>();
            runtimeConfiguration.SetDefaultsForRuntime(
                runtimeCatalog,
                initialPoolSize: 4,
                maxPoolSize: 64,
                maxActive: 512,
                dontDestroyPoolRoot: !showPoolRootInSceneHierarchy,
                configuredPoolRootName: "Sandbox_VFXPoolRoot");

            if (bootstrapMode == SandboxBootstrapMode.VContainerHelper)
            {
                InitializeViaVContainer();
            }
            else
            {
                InitializeDirect();
            }

            lastPrefabHandle = VfxHandle.Invalid;
            lastUiHandle = VfxHandle.Invalid;
            lastLineHandle = VfxHandle.Invalid;

            var poolScope = showPoolRootInSceneHierarchy ? "active scene hierarchy" : "DontDestroyOnLoad";
            SetActionMessage(
                $"Initialized via {bootstrapMode}. " +
                $"Pool manager: {poolManagerObjectName} ({poolScope}).");
        }

        private void InitializeDirect()
        {
            poolManagerObject = new GameObject(poolManagerObjectName);
            var poolManager = poolManagerObject.AddComponent<VfxPoolManager>();
            vfxService = new VfxService(poolManager, runtimeCatalog, runtimeConfiguration);
        }

        private void InitializeViaVContainer()
        {
            var builder = new ContainerBuilder();
            builder.RegisterVfx(
                catalog: runtimeCatalog,
                configuration: runtimeConfiguration,
                poolManagerObjectName: poolManagerObjectName,
                dontDestroyOnLoad: !showPoolRootInSceneHierarchy);

            vfxResolver = builder.Build();
            vfxService = vfxResolver.Resolve<IVfxService>();
            poolManagerObject = FindPoolManagerObject(poolManagerObjectName);
        }

        private void ShutdownService()
        {
            if (vfxResolver != null)
            {
                vfxResolver.Dispose();
                vfxResolver = null;
            }

            if (vfxService != null)
            {
                vfxService.Dispose();
                vfxService = null;
            }

            if (poolManagerObject == null)
            {
                poolManagerObject = FindPoolManagerObject(poolManagerObjectName);
            }

            DestroyUnityObject(poolManagerObject);
            poolManagerObject = null;
        }

        private void ReinitializeService(string reason)
        {
            ShutdownService();
            InitializeService();
            SetActionMessage(reason);
        }

        private static GameObject FindPoolManagerObject(string name)
        {
            var managers = Object.FindObjectsByType<VfxPoolManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < managers.Length; i++)
            {
                var manager = managers[i];
                if (manager != null &&
                    manager.gameObject != null &&
                    manager.gameObject.name == name)
                {
                    return manager.gameObject;
                }
            }

            return null;
        }

        private GameObject CreateLinePreviewPrefab()
        {
            var prefab = new GameObject("Sandbox_LinePreviewPrefab");
            var lineRenderer = prefab.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 0;
            lineRenderer.widthMultiplier = linePreviewWidth;
            lineRenderer.receiveShadows = false;
            lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
            lineRenderer.textureMode = LineTextureMode.Stretch;

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                         Shader.Find("Sprites/Default") ??
                         Shader.Find("Unlit/Color");
            linePreviewMaterial = shader != null ? new Material(shader) : null;
            if (linePreviewMaterial != null)
            {
                linePreviewMaterial.color = linePreviewColor;
                lineRenderer.material = linePreviewMaterial;
            }

            prefab.AddComponent<LineArcVfxPlayable>();
            prefab.SetActive(false);
            return prefab;
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
            var camera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
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
            lastPrefabHandle = vfxService.PlayAt(VFXRefs.Effects.VfxPrefab, point, Quaternion.identity, parameters);
            SetActionMessage($"Spawn prefab at {point} -> {lastPrefabHandle.IsValid}");
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

                lastPrefabHandle = vfxService.PlayAt(VFXRefs.Effects.VfxPrefab, position, Quaternion.identity, parameters);
            }

            SetActionMessage($"Burst spawned: {spamCount} effects.");
        }

        private void SpawnOnTarget()
        {
            if (attachTarget == null)
            {
                SetActionMessage("No attach target assigned.");
                return;
            }

            var parameters = VfxParams.Empty
                .WithLifetimeOverride(2f)
                .WithScale(1.2f);

            var options = VfxPlayOptions.DefaultGameplay
                .WithIgnoreTargetScale(ignoreTargetScaleOnAttach);

            lastPrefabHandle = vfxService.PlayOn(
                VFXRefs.Effects.VfxPrefab,
                attachTarget.gameObject,
                attachMode,
                parameters,
                options);

            SetActionMessage(
                $"PlayOn target -> {lastPrefabHandle.IsValid} | Mode={attachMode} | IgnoreScale={ignoreTargetScaleOnAttach}");
        }

        private void SpawnUiChannelEffect()
        {
            var position = new Vector3(-4f, 0f, -2f);
            var parameters = VfxParams.Empty.WithLifetimeOverride(6f);
            var options = VfxPlayOptions.DefaultGameplay
                .WithChannel(VfxChannel.UI)
                .WithAutoRelease(false);

            lastUiHandle = vfxService.PlayAt(VFXRefs.Effects.VfxPrefab, position, Quaternion.identity, parameters, options);
            SetActionMessage($"Spawn UI-channel effect -> {lastUiHandle.IsValid}");
        }

        private void StopByScope(bool global)
        {
            var stopped = global
                ? vfxService.StopAll(VfxStopFilter.Global)
                : vfxService.StopAll();

            var uiStillActive = vfxService.TryUpdate(lastUiHandle, VfxParams.Empty);
            SetActionMessage(
                global
                    ? $"StopAll(Global) stopped {stopped}. UI alive after stop: {uiStillActive} (expected false)."
                    : $"StopAll(Default Gameplay) stopped {stopped}. UI alive after stop: {uiStillActive} (expected true).");
        }

        private void SpawnOrUpdateLinePreview(bool forceRespawn)
        {
            var targetPoint = ResolveMouseSpawnPoint(GetPointerPositionOrScreenCenter());
            if (!forceRespawn && vfxService.TryUpdate(lastLineHandle, VfxParams.Empty.WithTargetPoint(targetPoint)))
            {
                SetActionMessage("Line target updated.");
                return;
            }

            var start = attachTarget != null ? attachTarget.position : Vector3.zero;
            var parameters = VfxParams.Empty
                .WithTargetPoint(targetPoint)
                .WithColor(linePreviewColor)
                .WithIntensity(1f)
                .WithScale(linePreviewWidth)
                .WithLifetimeOverride(0f);

            var options = VfxPlayOptions.DefaultGameplay.WithAutoRelease(false);
            lastLineHandle = vfxService.PlayAt(LinePreviewId, start, Quaternion.identity, parameters, options);
            SetActionMessage($"Spawn line preview -> {lastLineHandle.IsValid}");
        }

        private void UpdateLineTargetFromMouse()
        {
            var targetPoint = ResolveMouseSpawnPoint(GetPointerPositionOrScreenCenter());
            var updated = vfxService.TryUpdate(
                lastLineHandle,
                VfxParams.Empty.WithTargetPoint(targetPoint).WithColor(linePreviewColor));
            SetActionMessage($"TryUpdate(line target) -> {updated}");
        }

        private void CycleAttachMode()
        {
            attachMode = attachMode switch
            {
                AttachMode.WorldLocked => AttachMode.FollowTransform,
                AttachMode.FollowTransform => AttachMode.FollowPositionOnly,
                _ => AttachMode.WorldLocked
            };

            SetActionMessage($"Attach mode: {attachMode}");
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
            if (overlayCanvas != null)
            {
                return;
            }

            var canvasObject = new GameObject("VfxSandboxOverlayCanvas");
            overlayCanvas = canvasObject.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = 5000;
            overlayCanvas.enabled = showOnScreenOverlay;

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
            panelRect.anchoredPosition = new Vector2(10f, -10f);
            panelRect.sizeDelta = new Vector2(900f, 510f);

            var panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.68f);

            var textObject = new GameObject("Text");
            textObject.transform.SetParent(panelObject.transform, false);
            var textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = new Vector2(10f, 8f);
            textRect.offsetMax = new Vector2(-10f, -8f);

            overlayText = textObject.AddComponent<Text>();
            overlayText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            overlayText.fontSize = 14;
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
            var poolScope = showPoolRootInSceneHierarchy ? "Scene Hierarchy" : "DontDestroyOnLoad";
            var currentBootstrap = bootstrapMode.ToString();

            overlayText.text =
                "Frost9.VFX Sandbox\n" +
                $"Bootstrap: {currentBootstrap} (B toggles) | Pool Scope: {poolScope}\n" +
                $"Attach Mode: {attachMode} | Ignore Target Scale: {ignoreTargetScaleOnAttach}\n\n" +
                "Spawn / Attach\n" +
                "  LMB: PlayAt(mouse)  | 1: PlayAt(origin) | 2: Burst spawn\n" +
                "  O: PlayOn(target)   | P: Cycle attach mode | I: Toggle ignore-scale\n\n" +
                "Line Runner\n" +
                "  L: Spawn line preview | T: Update line target to mouse | K: Stop line\n\n" +
                "Handle / Scope\n" +
                "  U: TryUpdate(last prefab) | S: Stop(last prefab)\n" +
                "  C: Spawn UI-channel effect (persistent)\n" +
                "  G: StopAll(default Gameplay) | Shift+G: StopAll(Global)\n\n" +
                "Verification\n" +
                "  H: Stale-handle check (expect stale=false, fresh=true)\n" +
                "  V: Pool reuse check (expect CreatedDeltaSecondWave=0)\n" +
                "  R: Reinitialize service | M: Toggle overlay\n\n" +
                $"Stats: Active={stats.TotalActiveInstances} Pooled={stats.TotalPooledInstances} " +
                $"Created={stats.TotalCreatedInstances} Recycled={stats.TotalRecycleCount}\n" +
                $"Handles: Prefab={lastPrefabHandle.IsValid} UI={lastUiHandle.IsValid} Line={lastLineHandle.IsValid}\n" +
                $"Last: {lastActionMessage}";
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
                $"[VfxSandbox][Stats] Bootstrap={bootstrapMode} Active={stats.TotalActiveInstances} " +
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

                SetActionMessage(
                    $"Stale-handle check: stale stop={staleStop} (expected false), " +
                    $"fresh stop={validStop} (expected true).");
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
                var center = Vector3.zero;

                SpawnVerificationBurst(center);
                yield return new WaitForSeconds(verificationLifetimeSeconds + 0.4f);
                var afterFirstWave = vfxService.GetStats();

                SpawnVerificationBurst(center + Vector3.forward * 2f);
                yield return new WaitForSeconds(0.05f);
                var duringSecondWave = vfxService.GetStats();

                var createdDeltaSecondWave = duringSecondWave.TotalCreatedInstances - afterFirstWave.TotalCreatedInstances;
                var reuseLikely = createdDeltaSecondWave == 0;
                SetActionMessage(
                    $"Pool reuse check: reused={reuseLikely}, CreatedDeltaSecondWave={createdDeltaSecondWave}.");
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

        private void SetActionMessage(string message)
        {
            lastActionMessage = message;
            Debug.Log($"[VfxSandbox] {message}");
        }

        private static void DestroyUnityObject(Object target)
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
    }
}
