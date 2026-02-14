using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class StationaryController : MonoBehaviour
{
    [Header("Look & Physics")]
    public float mouseSensitivity = 100f;
    public float upperLookLimit = -80f;
    public float lowerLookLimit = 80f;
    public float standingCameraHeight = 0.8f;
    public float crouchingCameraHeight = 0.2f;
    public float crouchSpeed = 8f;

    [Header("Camera Tool Settings")]
    public GameObject cameraHolder; 
    public RawImage photoDisplayUI; 
    public Image flashOverlay;
    public float photoVisibleTime = 2.0f; 
    public float fadeSpeed = 2.0f;

    [Header("Screen Shake Settings")]
    public float shakeDuration = 0.1f;
    public float shakeMagnitude = 0.05f;

    [Header("Audio")]
    public AudioClip shutterSound;
    private AudioSource audioSource;

    private float xRotation = 0f;
    private bool isCrouching = false;
    private bool isHoldingCamera = false;
    private CapsuleCollider playerCollider;
    private float defaultPosY;
    private CanvasGroup photoCanvasGroup;
    private Vector3 cameraBeforeShakePos;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        playerCollider = GetComponentInParent<CapsuleCollider>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        defaultPosY = transform.localPosition.y;
        
        // The photoCanvasGroup should be on the Parent Frame (the white box)
        if(photoDisplayUI) photoCanvasGroup = photoDisplayUI.GetComponentInParent<CanvasGroup>();
        
        cameraHolder.SetActive(false);
        if(photoCanvasGroup) photoCanvasGroup.gameObject.SetActive(false);
        
        Color fColor = flashOverlay.color;
        fColor.a = 0;
        flashOverlay.color = fColor;
    }

    void Update()
    {
        HandleLook();
        HandleCrouch();
        HandleCameraTool();
    }

    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        if (transform.parent != null) transform.parent.Rotate(Vector3.up * mouseX);
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, upperLookLimit, lowerLookLimit);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    void HandleCrouch()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl)) isCrouching = !isCrouching;

        float targetCamHeight = isCrouching ? crouchingCameraHeight : standingCameraHeight;
        defaultPosY = Mathf.Lerp(defaultPosY, targetCamHeight, Time.deltaTime * crouchSpeed);
        
        // Apply the base height (Head bob or shake will add to this)
        transform.localPosition = new Vector3(transform.localPosition.x, defaultPosY, transform.localPosition.z);

        if (playerCollider != null)
        {
            playerCollider.height = Mathf.Lerp(playerCollider.height, isCrouching ? 1f : 2f, Time.deltaTime * crouchSpeed);
            playerCollider.center = new Vector3(0, playerCollider.height / 2, 0);
        }
    }

    void HandleCameraTool()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            isHoldingCamera = !isHoldingCamera;
            cameraHolder.SetActive(isHoldingCamera);
        }

        if (isHoldingCamera && Input.GetMouseButtonDown(0))
        {
            StartCoroutine(CapturePhotoSequence());
        }
    }

    IEnumerator CapturePhotoSequence()
    {
        // 1. SOUND & SHAKE START
        if(shutterSound) audioSource.PlayOneShot(shutterSound);
        cameraBeforeShakePos = transform.localPosition;
        
        // 2. FLASH & HIDE TOOL
        flashOverlay.color = new Color(1, 1, 1, 1);
        cameraHolder.SetActive(false);
        
        // 3. CAPTURE
        yield return new WaitForEndOfFrame();
        Texture2D screenShot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        screenShot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenShot.Apply();

        // 4. SHOW PHOTO & SHAKE EFFECT
        photoDisplayUI.texture = screenShot;
        photoCanvasGroup.alpha = 1f;
        photoCanvasGroup.gameObject.SetActive(true);
        if(isHoldingCamera) cameraHolder.SetActive(true);

        // Perform Screen Shake
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;
            transform.localPosition = new Vector3(cameraBeforeShakePos.x + x, cameraBeforeShakePos.y + y, cameraBeforeShakePos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = new Vector3(transform.localPosition.x, defaultPosY, transform.localPosition.z);

        // 5. FLASH FADE
        while (flashOverlay.color.a > 0)
        {
            Color fColor = flashOverlay.color;
            fColor.a -= Time.deltaTime * 10f; 
            flashOverlay.color = fColor;
            yield return null;
        }

        // 6. WAIT & FADE PHOTO
        yield return new WaitForSeconds(photoVisibleTime);
        while (photoCanvasGroup.alpha > 0)
        {
            photoCanvasGroup.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }
        photoCanvasGroup.gameObject.SetActive(false);
    }
}