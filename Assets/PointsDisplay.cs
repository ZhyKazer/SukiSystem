using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // For RawImage
using TMPro;
using Firebase;
using Firebase.Database;
using Firebase.Storage; // For Firebase Storage
using Firebase.Extensions;
using UnityEngine.Networking;

public class PointsDisplay : MonoBehaviour
{
    public TextMeshProUGUI pointsText; // TextMeshProUGUI to display the current points
    public TextMeshProUGUI nameText;
    public RawImage customerImage; // RawImage to display the customer's image
    private DatabaseReference databaseReference; // Reference to the Firebase database
    private FirebaseStorage storage; // Firebase storage reference
    private string customerID = QR_Scanning.ScannedQrCode; // Scanned QR code, assumed to be the customer ID

    void Start()
    {
        // Initialize Firebase
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                InitializeFirebase();
                FetchAndDisplayCustomerPoints();
                FetchAndDisplayCustomerData();// Fetch and display customer points
                LoadCustomerImage(); // Load and display the customer's image
            }
            else
            {
                Debug.LogError("Could not resolve all Firebase dependencies: " + task.Result);
            }
        });
    }

    void InitializeFirebase()
    {
        // Get the root reference location of the Firebase database.
        databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
        // Initialize Firebase Storage
        storage = FirebaseStorage.DefaultInstance;
    }

    private void FetchAndDisplayCustomerPoints()
    {
        // Fetch the customer points from Firebase based on the scanned QR code (customer ID)
        databaseReference.Child("customers").Child(customerID).Child("transactions").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                // Handle any errors that occurred during the retrieval
                Debug.LogError("Failed to retrieve customer points from Firebase: " + task.Exception);
                pointsText.text = "Error fetching points!";
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                if (snapshot.Exists)
                {
                    // Get current points from the last transaction
                    int currentPoints = 0;

                    if (snapshot.ChildrenCount > 0)
                    {
                        // Get points from the last transaction
                        foreach (DataSnapshot transaction in snapshot.Children)
                        {
                            if (transaction.HasChild("points"))
                            {
                                currentPoints = int.Parse(transaction.Child("points").Child("current_points").Value.ToString());
                            }
                        }
                    }

                    // Update the UI with the current points
                    pointsText.text = $"Current Points: {currentPoints}";
                }
                else
                {
                    Debug.LogError("Customer transactions not found in Firebase.");
                    pointsText.text = "No points found!";
                }
            }
        });
    }
    private void FetchAndDisplayCustomerData()
    {
        // Fetch the customer data from Firebase based on the scanned QR code (customer ID)
        databaseReference.Child("customers").Child(customerID).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                // Handle any errors that occurred during the retrieval
                Debug.LogError("Failed to retrieve customer data from Firebase: " + task.Exception);
                //feedbackText.text = "Error fetching customer data.";
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                if (snapshot.Exists)
                {
                    // Retrieve and display the customer's name and email
                    string customerName = snapshot.Child("customer").Child("name").Value.ToString();
                    string customerEmail = snapshot.Child("customer").Child("email").Value.ToString();

                    nameText.text = $"Customer Name: {customerName}";
                    //customerEmailText.text = $"Customer Email: {customerEmail}";

                    // Fetch and display the customer's current points
                    //DisplayCustomerPoints(snapshot);
                }
                else
                {
                    Debug.LogError("Customer ID not found in Firebase.");
                    //feedbackText.text = "Customer not found!";
                }
            }
        });
    }

    private void LoadCustomerImage()
    {
        // Reference to the image in Firebase Storage
        StorageReference imageReference = storage.GetReferenceFromUrl($"gs://vapethrift-c86fe.appspot.com/customer_images/{customerID}.jpg");

        // Get the download URL for the image
        imageReference.GetDownloadUrlAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Failed to get download URL: " + task.Exception);
                return;
            }

            string imageUrl = task.Result.ToString();
            StartCoroutine(DownloadImage(imageUrl)); // Start a coroutine to download the image
        });
    }

    private IEnumerator DownloadImage(string imageUrl)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Failed to download image: " + request.error);
        }
        else
        {
            // Get the downloaded texture
            Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;

            // Resize the texture to fit within the RawImage bounds
            RectTransform rawImageRect = customerImage.GetComponent<RectTransform>();

            // Get the size of the RawImage
            float rawImageWidth = rawImageRect.rect.width;
            float rawImageHeight = rawImageRect.rect.height;

            // Calculate the aspect ratio of the image
            float textureAspectRatio = (float)texture.width / texture.height;
            float rawImageAspectRatio = rawImageWidth / rawImageHeight;

            // Resize based on aspect ratio
            if (textureAspectRatio > rawImageAspectRatio)
            {
                // Image is wider, adjust based on width
                customerImage.texture = texture;
                customerImage.rectTransform.sizeDelta = new Vector2(rawImageWidth, rawImageWidth / textureAspectRatio);
            }
            else
            {
                // Image is taller, adjust based on height
                customerImage.texture = texture;
                customerImage.rectTransform.sizeDelta = new Vector2(rawImageHeight * textureAspectRatio, rawImageHeight);
            }
        }
    }

}
