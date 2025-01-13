using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Storage;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using TMPro;
#if UNITY_EDITOR || UNITY_STANDALONE
using SFB; // StandaloneFileBrowser for PC
#endif

public class UploadImage : MonoBehaviour
{
    public Button selectImageButton; // Button to select the image
    public Button uploadButton;        // Button to upload the image
    public RawImage previewImage;      // Image preview
    public TextMeshProUGUI statusText; // Status text for feedback
    string customerID = DataHolder.customerID; // Customer ID
    private string filePath; // The selected file path
    private Texture2D selectedTexture; // To hold the selected image texture

    void Start()
    {
        selectImageButton.onClick.AddListener(OpenFilePicker);
        uploadButton.onClick.AddListener(UploadImageToFirebase);
    }

    // Open file picker depending on platform
    void OpenFilePicker()
    {
#if UNITY_ANDROID
        // For Android, open the gallery
        OpenGallery();
#elif UNITY_EDITOR || UNITY_STANDALONE
        // For PC, open file dialog
        OpenFileDialog();
#endif
    }

#if UNITY_ANDROID
    // Use Native Gallery plugin for Android file picking
    private void OpenGallery()
    {
        NativeGallery.Permission permission = NativeGallery.GetImageFromGallery((path) =>
        {
            if (path != null)
            {
                filePath = path;
                StartCoroutine(LoadAndPreviewImage(path));
            }
        }, "Select an image", "image/*");
    }
#endif

#if UNITY_EDITOR || UNITY_STANDALONE
    // For PC, use the Standalone File Browser to select the image
    private void OpenFileDialog()
    {
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Select an Image", "", "jpg,png,jpeg", false);
        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            filePath = paths[0];
            StartCoroutine(LoadAndPreviewImage(filePath));
        }
    }
#endif

    // Load and preview the selected image
    private IEnumerator LoadAndPreviewImage(string path)
    {
        if (File.Exists(path))
        {
            // Load image into a Texture2D
            byte[] fileData = File.ReadAllBytes(path);
            selectedTexture = new Texture2D(2, 2);
            selectedTexture.LoadImage(fileData);

            // Set image preview
            previewImage.texture = selectedTexture;
        }
        else
        {
            Debug.LogError("File not found at: " + path);
        }
        yield return null;
    }

    // Upload the selected image to Firebase Storage
    private void UploadImageToFirebase()
    {
        if (selectedTexture != null)
        {
            StartCoroutine(UploadToFirebase(selectedTexture));
        }
        else
        {
            statusText.text = "No image selected.";
            Debug.LogError("No image selected to upload.");
        }
    }

    // Coroutine to upload the selected image to Firebase
    private IEnumerator UploadToFirebase(Texture2D texture)
    {
        // Ensure customer ID is set
        if (string.IsNullOrEmpty(customerID))
        {
            statusText.text = "Customer ID is not set.";
            Debug.LogError("Customer ID is not set.");
            yield break;
        }

        // The filename is customer_ID.jpg
        string filename = customerID + ".jpg";
        StorageReference storageRef = FirebaseStorage.DefaultInstance.GetReference("customer_images/" + filename);

        // Convert Texture2D to byte array
        byte[] imageBytes = texture.EncodeToJPG(); // or .EncodeToPNG()

        var metadata = new MetadataChange
        {
            ContentType = "image/jpeg" // or "image/png"
        };

        // Upload file
        var uploadTask = storageRef.PutBytesAsync(imageBytes, metadata);

        yield return new WaitUntil(() => uploadTask.IsCompleted);

        if (uploadTask.Exception != null)
        {
            Debug.LogError("Failed to upload image: " + uploadTask.Exception);
            statusText.text = "Upload failed.";
        }
        else
        {
            Debug.Log("Image uploaded successfully as " + filename);
            statusText.text = "Upload successful.";
            SceneManager.LoadScene("GenQR");
        }
    }
}
