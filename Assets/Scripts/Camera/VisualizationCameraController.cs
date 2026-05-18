using DungeonCrawler.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonCrawler.CameraControls
{
    public class VisualizationCameraController : MonoBehaviour
    {
        public static VisualizationCameraController Instance { get; private set; }
        public static bool IsVisualizationCameraActive => Instance != null && Instance.isVisualizationActive;

        [SerializeField] private float minOrthographicSize = 4f;
        [SerializeField] private float maxOrthographicSize = 20f;
        [SerializeField] private float zoomSpeed = 1.2f;

        private Camera targetCamera;
        private bool isVisualizationActive;
        private bool isDragging;
        private Vector3 savedPosition;
        private float savedOrthographicSize;
        private Vector3 lastMouseWorldPosition;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            targetCamera = GetComponent<Camera>();
        }

        public static VisualizationCameraController GetOrCreate()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return null;
            }

            VisualizationCameraController controller = mainCamera.GetComponent<VisualizationCameraController>();
            if (controller == null)
            {
                controller = mainCamera.gameObject.AddComponent<VisualizationCameraController>();
            }

            return controller;
        }

        public void EnterVisualizationMode()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera == null)
            {
                return;
            }

            savedPosition = targetCamera.transform.position;
            savedOrthographicSize = targetCamera.orthographicSize;
            targetCamera.orthographic = true;
            isVisualizationActive = true;
            isDragging = false;
            Debug.Log("Visualization camera pan enabled");
        }

        public void ExitVisualizationMode(bool restoreCamera)
        {
            isVisualizationActive = false;
            isDragging = false;

            if (restoreCamera && targetCamera != null)
            {
                targetCamera.transform.position = savedPosition;
                targetCamera.orthographicSize = savedOrthographicSize;
            }
        }

        private void LateUpdate()
        {
            if (!isVisualizationActive || targetCamera == null)
            {
                return;
            }

            if (GameUIManager.Instance == null || GameUIManager.Instance.CurrentState != UIState.PathfindingVisualization)
            {
                return;
            }

            HandlePan();
            HandleZoom();
        }

        private void HandlePan()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                isDragging = true;
                lastMouseWorldPosition = GetMouseWorldPosition(mouse.position.ReadValue());
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                isDragging = false;
            }

            if (!isDragging || !mouse.leftButton.isPressed)
            {
                return;
            }

            Vector3 currentMouseWorldPosition = GetMouseWorldPosition(mouse.position.ReadValue());
            Vector3 delta = lastMouseWorldPosition - currentMouseWorldPosition;
            targetCamera.transform.position += new Vector3(delta.x, delta.y, 0f);
            lastMouseWorldPosition = GetMouseWorldPosition(mouse.position.ReadValue());
        }

        private void HandleZoom()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            float scrollY = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scrollY) <= 0.01f)
            {
                return;
            }

            float zoomDelta = scrollY > 0f ? -zoomSpeed : zoomSpeed;
            targetCamera.orthographicSize = Mathf.Clamp(targetCamera.orthographicSize + zoomDelta, minOrthographicSize, maxOrthographicSize);
            Debug.Log($"Visualization camera zoom: {targetCamera.orthographicSize:0.00}");
        }

        private Vector3 GetMouseWorldPosition(Vector2 screenPosition)
        {
            Vector3 screenPoint = new Vector3(screenPosition.x, screenPosition.y, Mathf.Abs(targetCamera.transform.position.z));
            Vector3 worldPosition = targetCamera.ScreenToWorldPoint(screenPoint);
            worldPosition.z = targetCamera.transform.position.z;
            return worldPosition;
        }
    }
}
