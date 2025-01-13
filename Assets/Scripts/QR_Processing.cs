using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Storage;
using Firebase.Database;
using Firebase.Extensions;
using System.IO;
using UnityEngine.SceneManagement;

public class QR_Processing : MonoBehaviour
{
    public TMP_InputField transactionCostInput; // InputField for entering the transaction cost
    public TextMeshProUGUI feedbackText; // Text to show feedback on points awarded
    public TextMeshProUGUI currentPointsText; // Text to show the current points of the customer
    public Button addPointsButton; // Button to trigger point calculation

    public TextMeshProUGUI scannedCodeText; // TextMeshProUGUI to display the scanned code
    public TextMeshProUGUI customerNameText; // TextMeshProUGUI to display the customer's name
    public TextMeshProUGUI customerEmailText; // TextMeshProUGUI to display the customer's email
    public RawImage customerImage; // RawImage to display the customer image

    public TMP_Dropdown paymentMethodDropdown; // Dropdown for payment method

    private DatabaseReference databaseReference; // Reference to the Firebase database
    private FirebaseStorage storage; // Reference to Firebase Storage
    private StorageReference storageReference; // Reference to storage bucket
    private string customerID = QR_Scanning.ScannedQrCode; // Scanned QR code, assumed to be the customer ID
    private string testName; // Transactor's name (retrieved from PlayerPrefs)
    private string paymentMethod = ""; // Default payment method

    void Start()
    {
        // Initialize Firebase and other components
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                InitializeFirebase();
                DisplayScannedCode();
                FetchAndDisplayCustomerData();
                FetchAndDisplayCustomerImage();
                addPointsButton.onClick.AddListener(AddPointsBasedOnTransaction);

                // Add listener for payment method dropdown
                paymentMethodDropdown.onValueChanged.AddListener(delegate { OnPaymentMethodChanged(); });
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
        storageReference = storage.GetReferenceFromUrl("gs://vapethrift-c86fe.appspot.com"); // Replace with your Firebase Storage bucket URL
    }

    private void DisplayScannedCode()
    {
        // Display the scanned QR code on the UI
        if (!string.IsNullOrEmpty(customerID))
        {
            scannedCodeText.text = $"Scanned QR Code: {customerID}";
        }
        else
        {
            scannedCodeText.text = "No QR Code Scanned.";
        }
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
                feedbackText.text = "Error fetching customer data.";
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                if (snapshot.Exists)
                {
                    // Retrieve and display the customer's name and email
                    string customerName = snapshot.Child("customer").Child("name").Value.ToString();
                    string customerEmail = snapshot.Child("customer").Child("email").Value.ToString();

                    customerNameText.text = $"Customer Name: {customerName}";
                    customerEmailText.text = $"Customer Email: {customerEmail}";

                    // Fetch and display the customer's current points
                    DisplayCustomerPoints(snapshot);
                }
                else
                {
                    Debug.LogError("Customer ID not found in Firebase.");
                    feedbackText.text = "Customer not found!";
                }
            }
        });
    }

    private void DisplayCustomerPoints(DataSnapshot snapshot)
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
        Debug.Log("Points Display");
    }

    private void FetchAndDisplayCustomerImage()
    {
        // The image file is named as the customer ID (e.g., customerID.jpg)
        string imageFileName = customerID + ".jpg";

        // Get a reference to the image in Firebase Storage
        StorageReference imageRef = storageReference.Child("customer_images/" + imageFileName);

        // Download the image to a local temporary file
        const long maxAllowedSize = 1 * 1024 * 1024; // Max size is 1MB
        imageRef.GetBytesAsync(maxAllowedSize).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Failed to download image from Firebase Storage: " + task.Exception);
            }
            else
            {
                byte[] fileContents = task.Result;
                Debug.Log("Image downloaded successfully!");

                // Load the downloaded bytes into a Texture2D
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(fileContents);

                // Set the Texture2D on the RawImage component
                customerImage.texture = texture;
            }
        });
    }

    // This method updates the paymentMethod variable when the dropdown changes
    void OnPaymentMethodChanged()
    {
        // Update the paymentMethod string based on the dropdown selection
        int selectedIndex = paymentMethodDropdown.value;
        string selectedMethod = paymentMethodDropdown.options[selectedIndex].text;

        paymentMethod = selectedMethod;
        Debug.Log("Payment Method Selected: " + paymentMethod); // Debugging to ensure correct method is selected
    }

    public void AddPointsBasedOnTransaction()
    {
        // Prevent submission if payment method is "None" or an empty string
        if (string.IsNullOrEmpty(paymentMethod) || paymentMethod == "None")
        {
            feedbackText.text = "Please select a valid payment method!";
            Debug.LogError("Submission blocked due to invalid payment method: " + paymentMethod);
            return; // Exit the function
        }

        if (!string.IsNullOrEmpty(transactionCostInput.text))
        {
            if (float.TryParse(transactionCostInput.text, out float transactionCost))
            {
                // Calculate points as 1% of the transaction cost
                int pointsToAdd = Mathf.FloorToInt(transactionCost * 0.01f);

                // Fetch customer data and update points only after fetching current points
                ValidateAndAddPoints(transactionCost, pointsToAdd);
            }
            else
            {
                feedbackText.text = "Invalid transaction cost!";
            }
        }
        else
        {
            feedbackText.text = "Please enter a transaction cost!";
        }
    }


    private void ValidateAndAddPoints(float transactionCost, int pointsToAdd)
    {
        // Fetch the customer data from Firebase
        databaseReference.Child("customers").Child(customerID).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                // Handle any errors that occurred during the retrieval
                Debug.LogError("Failed to retrieve customer data from Firebase: " + task.Exception);
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

                    // Proceed with updating points
                    UpdatePointsInFirebase(transactionCost, pointsToAdd, currentPoints);
                }
                else
                {
                    Debug.LogError("Customer ID not found in Firebase.");
                    feedbackText.text = "Customer not found!";
                }
            }
        });
    }

    private void UpdatePointsInFirebase(float transactionCost, int pointsToAdd, int currentPoints)
    {
        // Prepare new transaction data
        string newTransactionKey = databaseReference.Child("customers").Child(customerID).Child("transactions").Push().Key;
        Dictionary<string, object> newTransaction = new Dictionary<string, object>
    {
        { "transaction_date", System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") },
        { "transaction_cost", transactionCost },
        { "payment_method", paymentMethod },
        { "NameOfAdminThatTransact", testName }, // Using the testName retrieved from PlayerPrefs
        { "points", new Dictionary<string, object>
            {
                { "deducted", 0 },
                { "added", pointsToAdd },
                { "current_points", currentPoints + pointsToAdd }
            }
        }
    };

        // Update Firebase with the new transaction
        databaseReference.Child("customers").Child(customerID).Child("transactions").Child(newTransactionKey).SetValueAsync(newTransaction)
        .ContinueWithOnMainThread(updateTask =>
        {
            if (updateTask.IsFaulted)
            {
                // Handle the error
                feedbackText.text = "Error adding points: " + updateTask.Exception.Message;
                Debug.LogError("Failed to update Firebase: " + updateTask.Exception);
            }
            else if (updateTask.IsCompleted)
            {
                Debug.Log("Transaction added successfully to Firebase.");
                feedbackText.text = "Points added successfully! Dont click any button";

                // Call the delayed scene transition after the points are added
                DelayedSceneTransition();
            }
        });
    }

    public void DelayedSceneTransition()
    {
        // Wait for 5 seconds and then load the "CustomerDecision" scene
        Invoke("GoToCustomerDecisionScene", 3f);
    }

    private void GoToCustomerDecisionScene()
    {
        SceneManager.LoadScene("CustomerDecision"); // Transition to the "CustomerDecision" scene
    }

    public void Logout()
    {
        PlayerPrefs.DeleteKey("LastEmail"); // Clear the stored email
        SceneManager.LoadScene("LoginScene"); // Go back to the login scene
    }
}
