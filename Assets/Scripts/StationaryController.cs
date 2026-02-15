using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class StationaryController : MonoBehaviour
{
    [Header("Look & Movement")]
    public float mouseSensitivity = 100f;
    public float standingHeight = 0.8f;
    public float crouchingHeight = 0.2f;
    public float crouchSpeed = 8f;
    private float xRotation = 0f;
    private bool isCrouching = false;

    [Header("Zoom Settings")]
    public float normalFOV = 60f;
    public float zoomFOV = 20f;
    public float zoomSpeed = 10f;
    private Camera mainCam;

    [Header("Photography & Score")]
    public GameObject cameraHolder;
    public CanvasGroup photoFrameGroup;
    public RawImage photoRawImage;
    public Image flashOverlay;
    public TextMeshProUGUI scoreText;
    public float detectionRadius = 5.0f; // Large detection size
    private int totalPoints = 0;

    [Header("Effects")]
    public AudioClip shutterSound;
    private AudioSource audioSource;

    void Start()
    {
        mainCam = GetComponent<Camera>();
        Cursor.lockState = CursorLockMode.Locked;
        
        // Ensure AudioSource exists
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        
        // Initial UI States
        if (photoFrameGroup) photoFrameGroup.gameObject.SetActive(false);
        if (cameraHolder) cameraHolder.SetActive(false);
        UpdateScore();
    }

    void Update()
    {
        HandleLook();
        HandleCrouch();
        HandleZoom();

        // Toggle Camera with 'C'
        if (Input.GetKeyDown(KeyCode.C)) 
        {
            cameraHolder.SetActive(!cameraHolder.activeSelf);
        }

        // Take Photo with 'Left Click'
        if (cameraHolder.activeSelf && Input.GetMouseButtonDown(0))
        {
            StartCoroutine(TakeAndScorePhoto());
        }
    }

    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        transform.parent.Rotate(Vector3.up * mouseX);
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80, 80);
        transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
    }

    void HandleCrouch()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl)) isCrouching = !isCrouching;
        float targetY = isCrouching ? crouchingHeight : standingHeight;
        transform.localPosition = Vector3.Lerp(transform.localPosition, new Vector3(0, targetY, 0), Time.deltaTime * crouchSpeed);
    }

    void HandleZoom()
    {
        float targetFOV = Input.GetMouseButton(1) ? zoomFOV : normalFOV;
        mainCam.fieldOfView = Mathf.Lerp(mainCam.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
    }

    IEnumerator TakeAndScorePhoto()
    {
        // 1. DETECTION (SphereCast shoots a thick pipe instead of a thin laser)
        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.SphereCast(ray, detectionRadius, out hit, 100f))
        {
            Alien a = hit.collider.GetComponent<Alien>();
            if (a == null) a = hit.collider.GetComponentInParent<Alien>();

            if (a != null && !a.hasBeenPhotographed)
            {
                totalPoints += a.GetPoints();
                a.hasBeenPhotographed = true;
                UpdateScore();
                StartCoroutine(DestroyAlienDelayed(a.gameObject));
            }
        }

        // 2. VISUALS: Sound and Flash
        if(shutterSound) audioSource.PlayOneShot(shutterSound);
        flashOverlay.color = Color.white;
        cameraHolder.SetActive(false); // Hide camera tool from photo

        yield return new WaitForEndOfFrame();

        // Capture screen
        Texture2D ss = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        ss.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        ss.Apply();

        photoRawImage.texture = ss;
        photoFrameGroup.alpha = 1;
        photoFrameGroup.gameObject.SetActive(true);
        cameraHolder.SetActive(true);

        // 3. FADE FLASH
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime;
            flashOverlay.color = Color.Lerp(Color.white, new Color(1, 1, 1, 0), t * 5f);
            yield return null;
        }

        // 4. FADE PHOTO
        yield return new WaitForSeconds(2f);
        while (photoFrameGroup.alpha > 0)
        {
            photoFrameGroup.alpha -= Time.deltaTime;
            yield return null;
        }
        photoFrameGroup.gameObject.SetActive(false);
    }

    IEnumerator DestroyAlienDelayed(GameObject alien)
    {
        yield return new WaitForSeconds(0.1f);
        if(alien != null) Destroy(alien);
    }

    void UpdateScore() 
    { 
        if(scoreText != null) scoreText.text = "Points: " + totalPoints; 
    }
}