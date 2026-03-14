using UnityEngine;
using UnityEngine.SceneManagement;

namespace GlitchRacer
{
    public static class GlitchRacerRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneCallbacks()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrap()
        {
            BuildIntoCurrentScene(false);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureBootstrap();
        }

        public static GlitchRacerGame BuildIntoCurrentScene(bool clearScene)
        {
#if UNITY_EDITOR
            if (clearScene)
            {
                GameObject[] sceneObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                for (int i = 0; i < sceneObjects.Length; i++)
                {
                    if (sceneObjects[i].transform.parent == null)
                    {
                        Object.DestroyImmediate(sceneObjects[i]);
                    }
                }
            }
#endif

            CleanupDuplicateNamedObjects("LeftTrailLine");
            CleanupDuplicateNamedObjects("RightTrailLine");

            GameObject root = FindOrCreateRoot();
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
            GameObject playerRoot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            playerRoot.name = "VirusCar";
            playerRoot.transform.position = new Vector3(0f, 0.95f, 0f);
            playerRoot.transform.localScale = new Vector3(1.8f, 1.1f, 3.2f);
            ApplyRuntimeColor(playerRoot, new Color(0.08f, 0.95f, 0.78f));

            Rigidbody body = playerRoot.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            BoxCollider collider = playerRoot.GetComponent<BoxCollider>();
            collider.size = new Vector3(0.95f, 0.95f, 0.95f);

            GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabin.name = "Cabin";
            cabin.transform.SetParent(playerRoot.transform, false);
            cabin.transform.localPosition = new Vector3(0f, 0.55f, -0.1f);
            cabin.transform.localScale = new Vector3(0.7f, 0.5f, 0.45f);
            ApplyRuntimeColor(cabin, new Color(0.6f, 0.96f, 1f));
            RemoveCollider(cabin);

            RunnerPlayer runnerPlayer = playerRoot.AddComponent<RunnerPlayer>();
            playerRoot.AddComponent<VirusCarEffects>();
            return runnerPlayer;
        }

        private static void EnsurePlayerVisuals(GameObject playerRoot)
        {
            playerRoot.name = "VirusCar";
            playerRoot.transform.position = new Vector3(0f, 0.95f, 0f);
            playerRoot.transform.localScale = new Vector3(1.8f, 1.1f, 3.2f);
            ApplyRuntimeColor(playerRoot, new Color(0.08f, 0.95f, 0.78f));

            Transform cabin = playerRoot.transform.Find("Cabin");
            if (cabin == null)
            {
                GameObject cabinObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cabinObject.name = "Cabin";
                cabinObject.transform.SetParent(playerRoot.transform, false);
                cabinObject.transform.localPosition = new Vector3(0f, 0.55f, -0.1f);
                cabinObject.transform.localScale = new Vector3(0.7f, 0.5f, 0.45f);
                ApplyRuntimeColor(cabinObject, new Color(0.6f, 0.96f, 1f));
                RemoveCollider(cabinObject);
            }
            else
            {
                cabin.localPosition = new Vector3(0f, 0.55f, -0.1f);
                cabin.localRotation = Quaternion.identity;
                cabin.localScale = new Vector3(0.7f, 0.5f, 0.45f);
                ApplyRuntimeColor(cabin.gameObject, new Color(0.6f, 0.96f, 1f));
            }
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
            collider.size = new Vector3(0.95f, 0.95f, 0.95f);
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

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
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
            ScalableBufferManager.ResizeBuffers(currentScale, currentScale);
        }
    }
}
