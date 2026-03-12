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
            if (Object.FindFirstObjectByType<GlitchRacerGame>() != null)
            {
                return;
            }

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

            GameObject root = new("GlitchRacerBootstrap");
            GlitchRacerGame game = root.AddComponent<GlitchRacerGame>();
            TrackSegmentSpawner spawner = root.AddComponent<TrackSegmentSpawner>();
            GlitchRacerHud hud = root.AddComponent<GlitchRacerHud>();

            Camera camera = Camera.main;
            if (camera == null)
            {
                camera = Object.FindFirstObjectByType<Camera>();
            }

            if (camera == null)
            {
                camera = new GameObject("Main Camera").AddComponent<Camera>();
            }

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

            RunnerPlayer player = CreatePlayer();

            spawner.Configure(game);
            rig.Configure(game, player.transform);
            hud.Configure(game);
            player.Configure(game);
            player.GetComponent<VirusCarEffects>()?.Configure(game);
            game.Configure(player, spawner, rig, hud);

            return game;
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
