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
    private float xRotation = 0f;
    private bool isCrouching = false;

    [Header("Photography & Score")]
    public GameObject cameraHolder;
    public CanvasGroup photoFrameGroup;
    public RawImage photoRawImage;
    public Image flashOverlay;
    public TextMeshProUGUI scoreText;
    private int totalPoints = 0;

    [Header("Effects")]
    public AudioClip shutterSound;
    private AudioSource audioSource;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        audioSource = gameObject.AddComponent<AudioSource>();
        photoFrameGroup.gameObject.SetActive(false);
        UpdateScore();
    }

    void Update()
    {
        // Look
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        transform.parent.Rotate(Vector3.up * mouseX);
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80, 80);
        transform.localRotation = Quaternion.Euler(xRotation, 0, 0);

        // Crouch (Left Control)
        if (Input.GetKeyDown(KeyCode.LeftControl)) isCrouching = !isCrouching;
        float targetY = isCrouching ? crouchingHeight : standingHeight;
        transform.localPosition = Vector3.Lerp(transform.localPosition, new Vector3(0, targetY, 0), Time.deltaTime * 8f);

        // Camera Toggle (C) and Snap Photo (Left Click)
        if (Input.GetKeyDown(KeyCode.C)) cameraHolder.SetActive(!cameraHolder.activeSelf);

        if (cameraHolder.activeSelf && Input.GetMouseButtonDown(0))
        {
            StartCoroutine(TakeAndScorePhoto());
        }
    }

    IEnumerator TakeAndScorePhoto()
    {
        // 1. Raycast to check for Alien BEFORE the flash hides it
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            Alien a = hit.collider.GetComponent<Alien>();
            if (a != null && !a.hasBeenPhotographed)
            {
                totalPoints += a.GetPoints();
                a.hasBeenPhotographed = true;
                UpdateScore();
                Destroy(a.gameObject, 0.2f); // Remove alien after hit
            }
        }

        // 2. Visuals
        audioSource.PlayOneShot(shutterSound);
        flashOverlay.color = new Color(1, 1, 1, 1);
        cameraHolder.SetActive(false); // Hide camera tool from photo

        yield return new WaitForEndOfFrame();
        Texture2D ss = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        ss.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        ss.Apply();

        photoRawImage.texture = ss;
        photoFrameGroup.alpha = 1;
        photoFrameGroup.gameObject.SetActive(true);
        cameraHolder.SetActive(true);

        // 3. Fade Out
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime;
            flashOverlay.color = Color.Lerp(Color.white, new Color(1,1,1,0), t * 5f);
            yield return null;
        }

        yield return new WaitForSeconds(2f);
        while (photoFrameGroup.alpha > 0)
        {
            photoFrameGroup.alpha -= Time.deltaTime;
            yield return null;
        }
        photoFrameGroup.gameObject.SetActive(false);
    }

    void UpdateScore() { scoreText.text = "Points: " + totalPoints; }
}