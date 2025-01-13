using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZXing;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;

public class QR_Scanning : MonoBehaviour
{
    [SerializeField]
    private RawImage _rawImageBackground;

    [SerializeField]
    private AspectRatioFitter _aspectRatioFitter;

    [SerializeField]
    private TextMeshProUGUI _textOut;

    [SerializeField]
    private RectTransform _scanZone;

    [SerializeField]
    private string _processingSceneName; // Name of the scene to load after scanning

    private bool _isCamAvailable;
    private WebCamTexture _cameraTexture;
    private string _lastDetectedCode;

    private float _scanInterval = 2f; // Time in seconds between scans
    private float _timeSinceLastScan;

    // Static variable to pass scanned QR code to the next scene
    public static string ScannedQrCode { get; private set; }

    void Start()
    {
        SetUpCamera();
    }

    void Update()
    {
        UpdateCameraRender();

        if (_isCamAvailable)
        {
            _timeSinceLastScan += Time.deltaTime;

            if (_timeSinceLastScan >= _scanInterval)
            {
                _timeSinceLastScan = 0f;
                StartCoroutine(ScanQRCode()); // Use coroutine to scan QR code asynchronously
            }
        }
    }

    private void SetUpCamera()
    {
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length == 0)
        {
            _isCamAvailable = false;
            Debug.LogError("No camera devices found.");
            return;
        }

        // Try to find the first back-facing camera
        for (int i = 0; i < devices.Length; i++)
        {
            if (!devices[i].isFrontFacing) // This ensures we are using the back camera
            {
                _cameraTexture = new WebCamTexture(devices[i].name, 640, 480, 15); // Use higher resolution if needed
                break;
            }
        }

        // If no back-facing camera is found, use the first available camera
        if (_cameraTexture == null)
        {
            _cameraTexture = new WebCamTexture(devices[0].name, 640, 480, 15);
        }

        if (_cameraTexture == null)
        {
            Debug.LogError("No suitable camera device found.");
            return;
        }

        _cameraTexture.Play();
        _rawImageBackground.texture = _cameraTexture;
        _isCamAvailable = true;
    }

    private void UpdateCameraRender()
    {
        if (!_isCamAvailable)
        {
            return;
        }

        // Adjust the aspect ratio to prevent distortion
        float ratio = (float)_cameraTexture.width / (float)_cameraTexture.height;
        _aspectRatioFitter.aspectRatio = ratio;

        int orientation = -_cameraTexture.videoRotationAngle;
        _rawImageBackground.rectTransform.localEulerAngles = new Vector3(0, 0, orientation);

        // Ensure the camera feed is displayed at the correct resolution without scaling
        _rawImageBackground.rectTransform.sizeDelta = new Vector2(_cameraTexture.width, _cameraTexture.height);
    }

    private IEnumerator ScanQRCode()
    {
        yield return null;  // Yield control for a frame to prevent blocking the main thread

        try
        {
            IBarcodeReader barcodeReader = new BarcodeReader
            {
                AutoRotate = true,
                TryInverted = true
            };

            Result result = barcodeReader.Decode(_cameraTexture.GetPixels32(), _cameraTexture.width, _cameraTexture.height);

            if (result != null && result.Text != _lastDetectedCode)
            {
                _lastDetectedCode = result.Text;

                // Update UI with the scanned code
                _textOut.text = "Scanned Code: " + result.Text;

                // Store the scanned QR code in the static variable
                ScannedQrCode = result.Text;

                // Start a coroutine to check the customer ID in Firebase
                StartCoroutine(CheckFirebaseCustomerID(ScannedQrCode));
            }
        }
        catch (System.Exception ex)
        {
            // Handle any exceptions and update UI
            _textOut.text = "Error: " + ex.Message;
            Debug.LogError("Exception during QR code scanning: " + ex.Message);
        }
    }

    private IEnumerator CheckFirebaseCustomerID(string scannedId)
    {
        // Perform the Firebase query asynchronously
        var task = FirebaseDatabase.DefaultInstance
            .GetReference("customers")
            .GetValueAsync();

        // Wait until the Firebase task is completed
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
        {
            // Handle the error
            _textOut.text = "Error accessing database.";
            Debug.LogError("Database error: " + task.Exception);
            yield break;
        }

        if (task.IsCompleted)
        {
            DataSnapshot snapshot = task.Result;

            bool idFound = false;

            // Iterate over all customers to see if the scanned ID exists
            foreach (DataSnapshot customerSnapshot in snapshot.Children)
            {
                string customerId = customerSnapshot.Child("customer_id").Value.ToString();

                if (customerId == scannedId)
                {
                    idFound = true;
                    break;
                }
            }

            // Process the result on the main thread
            if (idFound)
            {
                // ID found, load the next scene
                SceneManager.LoadScene("CustomerDecision");
            }
            else
            {
                // ID not found, show error message
                _textOut.text = "Error: Customer ID not found.";
            }
        }
    }

    private void OnDisable()
    {
        if (_cameraTexture != null)
        {
            _cameraTexture.Stop();
            _cameraTexture = null;
        }
    }
}
