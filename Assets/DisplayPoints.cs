using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine.Networking;
using UnityEngine.UI; // For the RawImage

public class DisplayPoints : MonoBehaviour
{
    public TextMeshProUGUI currentPointsText; // Text to show the current points of the customer
    public TextMeshProUGUI customerNameText; // Text to show the customer's name
    public RawImage customerImage; // Image to display the customer's image
    private DatabaseReference databaseReference; // Reference to the Firebase database
    private string customerID = QR_Scanning.ScannedQrCode; // Scanned QR code, assumed to be the customer ID

    void Start()
    {
        // Initialize Firebase
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                InitializeFirebase();
                FetchAndDisplayCustomerData(); // Fetch and display customer data
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
                currentPointsText.text = "Error fetching customer data.";
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                if (snapshot.Exists)
                {
                    // Get current points from the last transaction
                    DataSnapshot transactionsSnapshot = snapshot.Child("transactions");
                    int currentPoints = 0;

                    if (transactionsSnapshot.ChildrenCount > 0)
                    {
                        // Get points from the last transaction
                        foreach (DataSnapshot transaction in transactionsSnapshot.Children)
                        {
                            if (transaction.HasChild("points"))
                            {
                                currentPoints = int.Parse(transaction.Child("points").Child("current_points").Value.ToString());
                            }
                        }
                    }

                    // Update the UI with the current points
                    currentPointsText.text = $"Current Points: {currentPoints}";

                    // Get customer's name
                    string customerName = snapshot.Child("customer").Child("name").Value.ToString();
                    customerNameText.text = $"Customer Name: {customerName}";

                    // Get the customer's image path and load it
                    string imagePath = $"gs://vapethrift-c86fe.appspot.com/customer_images/{customerID}.png"; // Adjust if necessary
                    StartCoroutine(LoadCustomerImage(imagePath));
                }
                else
                {
                    Debug.LogError("Customer ID not found in Firebase.");
                    currentPointsText.text = "Customer not found!";
                }
            }
        });
    }

    private IEnumerator LoadCustomerImage(string imagePath)
    {
        // Create a URL for the image using Firebase Storage's REST API
        string url = imagePath.Replace("gs://", "https://firebasestorage.googleapis.com/v0/b/vapethrift-c86fe.appspot.com/o/customer_images%2F" + customerID + ".png?alt=media"); // Adjust the file extension if necessary

        using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url))
        {
            // Send the request and wait for a response
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error loading customer image: " + webRequest.error);
            }
            else
            {
                // Get the downloaded texture and apply it to the RawImage
                Texture2D texture = DownloadHandlerTexture.GetContent(webRequest);
                customerImage.texture = texture;
            }
        }
    }
}
