using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARTapPlaceCube : MonoBehaviour
{
    public GameObject cubePrefab;
    public Camera arCamera;

    private ARRaycastManager raycastManager;
    private int frameCounter = 0;
    private bool captureStarted = false;

    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Start()
    {
        raycastManager = GetComponent<ARRaycastManager>();
    }

    void Update()
    {
        if ((Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) || Input.GetMouseButtonDown(0))
        {
            Vector2 touchPosition;
            Ray ray;

            // Handle touch input
            if (Input.touchCount > 0)
            {
                touchPosition = Input.GetTouch(0).position;
                ray = Camera.main.ScreenPointToRay(touchPosition);
            }
            // Handle mouse input
            else
            {
                touchPosition = Input.mousePosition;
                ray = Camera.main.ScreenPointToRay(touchPosition);
            }

            if (raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
            {

                RaycastHit hit;
                // To detect hit point 
                Physics.Raycast(ray, out hit);
                Vector3 spawnPosition;
               

                Quaternion spawnRotation;

                if (hit.collider.tag == "Cube")
                {
                    spawnPosition = hit.point + hit.normal * (cubePrefab.transform.localScale.y / 2); // Offset by half cube height
                    spawnRotation = Quaternion.FromToRotation(Vector3.up, hit.normal); // Align cube normal to surface
                }
                else
                {
                    spawnPosition = hits[0].pose.position;
                    spawnRotation = Quaternion.identity;
                }

                Pose hitPose = hits[0].pose;
                GameObject cube = Instantiate(cubePrefab, spawnPosition, hitPose.rotation);
                cube.GetComponentInChildren<Renderer>().material.color = GetRandomColor();

                // Start capturing process
                frameCounter = 0;
                captureStarted = true;
            }
        }

        if (captureStarted)
        {
            frameCounter++;
            if (frameCounter == 10)
            {
                StartCoroutine(CaptureAndSaveImage());
                captureStarted = false;
            }
        }

    }

    private Color GetRandomColor()
    {
        return new Color(Random.value, Random.value, Random.value);
    }

    private IEnumerator CaptureAndSaveImage()
    {
        yield return new WaitForEndOfFrame(); // Wait to capture after rendering

        // Create a texture for the full screen
        Texture2D screenCapture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);

        // Read pixels from the screen buffer
        screenCapture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenCapture.Apply();

        // Generate a unique filename using timestamp
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filename = $"ARImage_{timestamp}.png";
        string path = System.IO.Path.Combine(Application.persistentDataPath, filename);

        // Save the image
        System.IO.File.WriteAllBytes(path, screenCapture.EncodeToPNG());
        Debug.Log($"Image saved to {path}");

        // Display the popup
        ShowPopup($"Screenshot saved: {filename}");


    }



    private void ShowPopup(string message)
    {
        GameObject canvas = GameObject.Find("PopupCanvas");

        if (canvas == null)
        {
            // Create a new canvas if not found
            canvas = new GameObject("PopupCanvas");
            Canvas c = canvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;

            
            CanvasScaler scaler = canvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            
            canvas.AddComponent<GraphicRaycaster>();
        }

        // Create the popup object
        GameObject popup = new GameObject("PopupMessage");
        popup.transform.SetParent(canvas.transform);

       
        RectTransform rectTransform = popup.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(600, 100);
        rectTransform.anchoredPosition = new Vector2(0, 200);

        // Add a background color to the popup
        Image bg = popup.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.8f);

        // Add text component for the popup message
        GameObject textObj = new GameObject("PopupText");
        textObj.transform.SetParent(popup.transform);

        Text popupText = textObj.AddComponent<Text>();
        popupText.text = message;
        popupText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        popupText.color = Color.white;
        popupText.alignment = TextAnchor.MiddleCenter;

        // Ensure the RectTransform of the text is sized and positioned properly
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(600, 100);
        textRect.anchoredPosition = Vector2.zero;

        // Destroy the popup after 1.5 seconds
        GameObject.Destroy(popup, 1.5f);
    }


}

