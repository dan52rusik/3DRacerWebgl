using UnityEngine;
using UnityEngine.SceneManagement;

namespace GlitchRacer
{
    public static class GlitchRacerRuntimeBootstrap
    {
        private static bool bootstrapPending = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneCallbacks()
        {
            bootstrapPending = true;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrap()
        {
            TryBootstrapCurrentScene();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryBootstrapCurrentScene();
        }

        private static void TryBootstrapCurrentScene()
        {
            if (!bootstrapPending)
            {
                return;
            }

            bootstrapPending = false;
            BuildIntoCurrentScene(false);
        }

        public static GlitchRacerGame BuildIntoCurrentScene(bool clearScene)
        {
#if UNITY_EDITOR
            if (clearScene)
            {
                GameObject[] sceneObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                for (int i = 0; i < sceneObjects.Length; i++)
                {
                    if (sceneObjects[i] != null && sceneObjects[i].transform.parent == null)
                    {
                        Object.DestroyImmediate(sceneObjects[i]);
                    }
                }
            }
#endif

            CleanupDuplicateNamedObjects("LeftTrailLine");
            CleanupDuplicateNamedObjects("RightTrailLine");

            GameObject root = FindOrCreateRoot();
            CleanupGeneratedChildren(root.transform);
            GlitchRacerGame game = GetOrAddComponent<GlitchRacerGame>(root);
            TrackSegmentSpawner spawner = GetOrAddComponent<TrackSegmentSpawner>(root);
            GlitchRacerHud hud = GetOrAddComponent<GlitchRacerHud>(root);

            Camera camera = Camera.main;
            if (camera == null)
            {
                camera = Object.FindFirstObjectByType<Camera>();
            }

            if (camera == null)
            {
                camera = new GameObject("Main Camera").AddComponent<Camera>();
            }

            camera.gameObject.name = "Main Camera";
            camera.tag = "MainCamera";
            if (camera.GetComponent<AudioListener>() == null)
            {
                camera.gameObject.AddComponent<AudioListener>();
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.fieldOfView = 74f;

            GlitchCameraRig rig = camera.GetComponent<GlitchCameraRig>();
            if (rig == null)
            {
                rig = camera.gameObject.AddComponent<GlitchCameraRig>();
            }

            if (camera.GetComponent<AdaptiveRenderScale>() == null)
            {
                camera.gameObject.AddComponent<AdaptiveRenderScale>();
            }

            if (Object.FindFirstObjectByType<Light>() == null)
            {
                GameObject lightObject = new("Directional Light");
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 0.92f;
                light.color = new Color(0.45f, 0.68f, 1f);
                light.transform.rotation = Quaternion.Euler(24f, -32f, 0f);
            }

            RunnerPlayer player = FindOrCreatePlayer();

            // FIX: порядок инициализации:
            // 1. Configure() — назначаем все ссылки во все компоненты
            // 2. EnterMainMenu() — теперь все ссылки (player, spawner, rig) уже живые
            // 3. SnapToTarget() — камера встаёт на место уже в правильном состоянии
            //
            // Раньше GlitchRacerGame.Awake() вызывал EnterMainMenu() ещё до Configure(),
            // player/spawner/rig были null — вся инициализация уходила впустую,
            // а потом Bootstrap вызывал её повторно уже корректно.
            spawner.Configure(game);
            rig.Configure(game, player.transform);
            hud.Configure(game);
            player.Configure(game);
            player.GetComponent<VirusCarEffects>()?.Configure(game);
            game.Configure(player, spawner, rig, hud);

            if (Application.isPlaying)
            {
                player.ResetRunner();
                game.EnterMainMenu();
                rig.SnapToTarget();
            }
            else
            {
                player.ResetRunner();
                rig.SnapToTarget();
            }

            return game;
        }

        private static GameObject FindOrCreateRoot()
        {
            GlitchRacerGame existingGame = Object.FindFirstObjectByType<GlitchRacerGame>();
            if (existingGame != null)
            {
                existingGame.gameObject.name = "GlitchRacerBootstrap";
                return existingGame.gameObject;
            }

            GameObject existingRoot = GameObject.Find("GlitchRacerBootstrap");
            if (existingRoot != null)
            {
                return existingRoot;
            }

            return new GameObject("GlitchRacerBootstrap");
        }

        private static RunnerPlayer FindOrCreatePlayer()
        {
            RunnerPlayer[] existingPlayers = Object.FindObjectsByType<RunnerPlayer>(FindObjectsSortMode.None);
            RunnerPlayer player = existingPlayers.Length > 0 ? existingPlayers[0] : null;

            for (int i = 1; i < existingPlayers.Length; i++)
            {
                DestroyObject(existingPlayers[i].gameObject);
            }

            if (player == null)
            {
                GameObject playerObject = GameObject.Find("VirusCar");
                if (playerObject != null)
                {
                    player = GetOrAddComponent<RunnerPlayer>(playerObject);
                }
            }

            if (player == null)
            {
                player = CreatePlayer();
            }

            EnsurePlayerVisuals(player.gameObject);
            EnsurePlayerPhysics(player.gameObject);
            GetOrAddComponent<VirusCarEffects>(player.gameObject);

            return player;
        }

        private static RunnerPlayer CreatePlayer()
        {
            GameObject playerRoot = new GameObject("VirusCar");
            playerRoot.transform.position = new Vector3(0f, 0.95f, 0f);

            EnsurePlayerVisuals(playerRoot);
            EnsurePlayerPhysics(playerRoot);

            RunnerPlayer runnerPlayer = playerRoot.AddComponent<RunnerPlayer>();
            playerRoot.AddComponent<VirusCarEffects>();
            return runnerPlayer;
        }

        private static void EnsurePlayerVisuals(GameObject playerRoot)
        {
            playerRoot.name = "VirusCar";
            playerRoot.transform.position = new Vector3(0f, 0.95f, 0f);
            playerRoot.transform.localScale = Vector3.one;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (playerRoot.GetComponent<MeshRenderer>()) Object.DestroyImmediate(playerRoot.GetComponent<MeshRenderer>());
                if (playerRoot.GetComponent<MeshFilter>()) Object.DestroyImmediate(playerRoot.GetComponent<MeshFilter>());
            }
            else
#endif
            {
                if (playerRoot.GetComponent<MeshRenderer>()) Object.Destroy(playerRoot.GetComponent<MeshRenderer>());
                if (playerRoot.GetComponent<MeshFilter>()) Object.Destroy(playerRoot.GetComponent<MeshFilter>());
            }

            Transform visuals = playerRoot.transform.Find("Visuals");
            if (visuals != null)
            {
                DestroyObject(visuals.gameObject);
            }
            Transform oldCabin = playerRoot.transform.Find("Cabin");
            if (oldCabin != null) DestroyObject(oldCabin.gameObject);

            GameObject vRoot = new GameObject("Visuals");
            vRoot.transform.SetParent(playerRoot.transform, false);

            Color armorColor = new Color(0.12f, 0.16f, 0.22f); 
            Color wingColor = new Color(0.08f, 0.12f, 0.18f);
            Color accentColor = new Color(0f, 0.96f, 0.82f); 
            Color engineColor = new Color(1f, 0.25f, 0.1f);
            Color glassColor = new Color(0.15f, 0.85f, 1f);

            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Cube);
            core.name = "Fuselage";
            core.transform.SetParent(vRoot.transform, false);
            core.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            core.transform.localScale = new Vector3(1.0f, 0.6f, 3.2f);
            ApplyRuntimeColor(core, armorColor);
            RemoveCollider(core);

            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            canopy.name = "Canopy";
            canopy.transform.SetParent(vRoot.transform, false);
            canopy.transform.localPosition = new Vector3(0f, 0.45f, 0.4f);
            canopy.transform.localRotation = Quaternion.Euler(14f, 0f, 0f);
            canopy.transform.localScale = new Vector3(0.75f, 0.4f, 1.4f);
            ApplyRuntimeColor(canopy, glassColor);
            RemoveCollider(canopy);

            GameObject wingL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wingL.name = "WingL";
            wingL.transform.SetParent(vRoot.transform, false);
            wingL.transform.localPosition = new Vector3(-1.1f, 0.18f, -0.6f);
            wingL.transform.localRotation = Quaternion.Euler(0f, 28f, 0f);
            wingL.transform.localScale = new Vector3(1.6f, 0.15f, 1.4f);
            ApplyRuntimeColor(wingL, wingColor);
            RemoveCollider(wingL);

            GameObject wingR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wingR.name = "WingR";
            wingR.transform.SetParent(vRoot.transform, false);
            wingR.transform.localPosition = new Vector3(1.1f, 0.18f, -0.6f);
            wingR.transform.localRotation = Quaternion.Euler(0f, -28f, 0f);
            wingR.transform.localScale = new Vector3(1.6f, 0.15f, 1.4f);
            ApplyRuntimeColor(wingR, wingColor);
            RemoveCollider(wingR);

            GameObject engL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            engL.name = "EngineL";
            engL.transform.SetParent(vRoot.transform, false);
            engL.transform.localPosition = new Vector3(-1.3f, 0.25f, -1.3f);
            engL.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            engL.transform.localScale = new Vector3(0.40f, 0.6f, 0.40f);
            ApplyRuntimeColor(engL, armorColor);
            RemoveCollider(engL);

            GameObject engR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            engR.name = "EngineR";
            engR.transform.SetParent(vRoot.transform, false);
            engR.transform.localPosition = new Vector3(1.3f, 0.25f, -1.3f);
            engR.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            engR.transform.localScale = new Vector3(0.40f, 0.6f, 0.40f);
            ApplyRuntimeColor(engR, armorColor);
            RemoveCollider(engR);

            GameObject glowL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            glowL.name = "GlowL";
            glowL.transform.SetParent(vRoot.transform, false);
            glowL.transform.localPosition = new Vector3(-1.3f, 0.25f, -1.95f);
            glowL.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            ApplyRuntimeColor(glowL, engineColor);
            RemoveCollider(glowL);

            GameObject glowR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            glowR.name = "GlowR";
            glowR.transform.SetParent(vRoot.transform, false);
            glowR.transform.localPosition = new Vector3(1.3f, 0.25f, -1.95f);
            glowR.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            ApplyRuntimeColor(glowR, engineColor);
            RemoveCollider(glowR);

            GameObject thruster = GameObject.CreatePrimitive(PrimitiveType.Cube);
            thruster.name = "ThrusterMain";
            thruster.transform.SetParent(vRoot.transform, false);
            thruster.transform.localPosition = new Vector3(0f, 0.2f, -1.65f);
            thruster.transform.localScale = new Vector3(0.85f, 0.35f, 0.25f);
            ApplyRuntimeColor(thruster, accentColor);
            RemoveCollider(thruster);

            GameObject finL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            finL.name = "FinL";
            finL.transform.SetParent(vRoot.transform, false);
            finL.transform.localPosition = new Vector3(-0.45f, 0.75f, -1.0f);
            finL.transform.localRotation = Quaternion.Euler(0f, 0f, 22f);
            finL.transform.localScale = new Vector3(0.12f, 0.7f, 1.0f);
            ApplyRuntimeColor(finL, accentColor);
            RemoveCollider(finL);

            GameObject finR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            finR.name = "FinR";
            finR.transform.SetParent(vRoot.transform, false);
            finR.transform.localPosition = new Vector3(0.45f, 0.75f, -1.0f);
            finR.transform.localRotation = Quaternion.Euler(0f, 0f, -22f);
            finR.transform.localScale = new Vector3(0.12f, 0.7f, 1.0f);
            ApplyRuntimeColor(finR, accentColor);
            RemoveCollider(finR);

            GameObject noseL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            noseL.name = "NoseL";
            noseL.transform.SetParent(vRoot.transform, false);
            noseL.transform.localPosition = new Vector3(-0.45f, 0.2f, 1.0f);
            noseL.transform.localScale = new Vector3(0.08f, 0.08f, 1.8f);
            ApplyRuntimeColor(noseL, accentColor);
            RemoveCollider(noseL);

            GameObject noseR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            noseR.name = "NoseR";
            noseR.transform.SetParent(vRoot.transform, false);
            noseR.transform.localPosition = new Vector3(0.45f, 0.2f, 1.0f);
            noseR.transform.localScale = new Vector3(0.08f, 0.08f, 1.8f);
            ApplyRuntimeColor(noseR, accentColor);
            RemoveCollider(noseR);
        }

        private static void EnsurePlayerPhysics(GameObject playerRoot)
        {
            Rigidbody body = GetOrAddComponent<Rigidbody>(playerRoot);
            body.isKinematic = true;
            body.useGravity = false;

            BoxCollider collider = playerRoot.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = playerRoot.AddComponent<BoxCollider>();
            }

            collider.isTrigger = false;
            collider.center = new Vector3(0f, 0.4f, 0f);
            collider.size = new Vector3(1.7f, 1.0f, 3.2f);
        }

        private static void CleanupDuplicateNamedObjects(string objectName)
        {
            GameObject[] objects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            bool keepFirst = true;

            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i].name != objectName)
                {
                    continue;
                }

                if (keepFirst)
                {
                    keepFirst = false;
                    continue;
                }

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyObject(objects[i]);
                    continue;
                }
#endif
                DestroyObject(objects[i]);
            }
        }

        private static void CleanupGeneratedChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (!ShouldDestroyGeneratedChild(child.name))
                {
                    continue;
                }

                DestroyObject(child.gameObject);
            }
        }

        private static bool ShouldDestroyGeneratedChild(string childName)
        {
            return childName.StartsWith("TrackSegment_")
                || childName == "GlitchCanvasHud";
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            if (component == null)
            {
                component = target.AddComponent<T>();
            }

            return component;
        }

        private static void DestroyObject(Object target)
        {
            if (target == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(target);
                return;
            }
#endif

            Object.Destroy(target);
        }

        private static void ApplyRuntimeColor(GameObject target, Color color)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            Shader shader = FindRuntimeShader(
                "Universal Render Pipeline/Unlit",
                "Unlit/Color",
                "Sprites/Default");
            if (shader == null)
            {
                Debug.LogError("GlitchRacerRuntimeBootstrap: runtime shader not found. Add a fallback shader to Always Included Shaders.");
                return;
            }

            Material material = new(shader);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            renderer.sharedMaterial = material;
        }

        private static void RemoveCollider(GameObject target)
        {
            Collider collider = target.GetComponent<Collider>();
            if (collider == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(collider);
                return;
            }
#endif

            Object.Destroy(collider);
        }

        private static Shader FindRuntimeShader(params string[] shaderNames)
        {
            for (int i = 0; i < shaderNames.Length; i++)
            {
                Shader shader = Shader.Find(shaderNames[i]);
                if (shader != null)
                {
                    return shader;
                }
            }

            return null;
        }
    }

    public class AdaptiveRenderScale : MonoBehaviour
    {
        [SerializeField] private float desktopScale = 1f;
        [SerializeField] private float mobileMinScale = 0.72f;
        [SerializeField] private float mobileMaxScale = 0.92f;
        [SerializeField] private float upscaleFps = 58f;
        [SerializeField] private float downscaleFps = 52f;
        [SerializeField] private float sampleDuration = 1f;

        private float currentScale = 1f;
        private float targetMaxScale = 1f;
        private float sampleTimer;
        private float averageDelta = 1f / 60f;

        private void OnEnable()
        {
            targetMaxScale = ComputeTargetMaxScale();
            currentScale = targetMaxScale;
            ApplyScale();
        }

        private void Update()
        {
            float dt = Mathf.Clamp(Time.unscaledDeltaTime, 0.0001f, 0.1f);
            averageDelta = Mathf.Lerp(averageDelta, dt, 0.08f);
            sampleTimer += dt;

            if (sampleTimer < sampleDuration)
            {
                return;
            }

            sampleTimer = 0f;
            float fps = 1f / averageDelta;

            if (fps < downscaleFps)
            {
                currentScale = Mathf.Max(mobileMinScale, currentScale - 0.05f);
                ApplyScale();
            }
            else if (fps > upscaleFps)
            {
                currentScale = Mathf.Min(targetMaxScale, currentScale + 0.04f);
                ApplyScale();
            }
        }

        private float ComputeTargetMaxScale()
        {
            if (!IsMobileLikeDevice())
            {
                return desktopScale;
            }

            int longestSide = Mathf.Max(Screen.width, Screen.height);
            if (longestSide >= 1440)
            {
                return 0.78f;
            }

            if (longestSide >= 1080)
            {
                return 0.84f;
            }

            if (longestSide >= 900)
            {
                return 0.9f;
            }

            return mobileMaxScale;
        }

        private static bool IsMobileLikeDevice()
        {
            if (Application.isMobilePlatform)
            {
                return true;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            return Screen.width < 1100 || Screen.height < 1100;
#else
            return false;
#endif
        }

        private void ApplyScale()
        {
#if !UNITY_WEBGL
            ScalableBufferManager.ResizeBuffers(currentScale, currentScale);
#endif
        }
    }
}
