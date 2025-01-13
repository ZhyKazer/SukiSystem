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

public class DeductPoints : MonoBehaviour
{
    public TMP_InputField transactionCostInput; // InputField for entering the transaction cost
    public TextMeshProUGUI feedbackText; // Text to show feedback on points awarded
    public TextMeshProUGUI currentPointsText; // Text to show the current points of the customer
    public Button deductPointsButton; // Button to trigger point deduction

    public TextMeshProUGUI scannedCodeText; // TextMeshProUGUI to display the scanned code
    public TextMeshProUGUI customerNameText; // TextMeshProUGUI to display the customer's name
    public TextMeshProUGUI customerEmailText; // TextMeshProUGUI to display the customer's email
    public RawImage customerImage; // RawImage to display the customer image

    private DatabaseReference databaseReference; // Reference to the Firebase database
    private FirebaseStorage storage; // Reference to Firebase Storage
    private StorageReference storageReference; // Reference to storage bucket
    private string customerID = QR_Scanning.ScannedQrCode; // Scanned QR code, assumed to be the customer ID
    private string testName; // Transactor's name (retrieved from PlayerPrefs)
    private string paymentMethod = "Cash"; // Example payment method

    void Start()
    {
        testName = PlayerPrefs.GetString("LastEmail", "Unknown");

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                InitializeFirebase();
                DisplayScannedCode();
                FetchAndDisplayCustomerData();
                FetchAndDisplayCustomerImage();
                deductPointsButton.onClick.AddListener(DeductPointsBasedOnTransaction);
            }
            else
            {
                Debug.LogError("Could not resolve all Firebase dependencies: " + task.Result);
            }
        });
    }

    void InitializeFirebase()
    {
        databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
        storage = FirebaseStorage.DefaultInstance;
        storageReference = storage.GetReferenceFromUrl("gs://vapethrift-c86fe.appspot.com");
    }

    private void DisplayScannedCode()
    {
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
        databaseReference.Child("customers").Child(customerID).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                DataSnapshot snapshot = task.Result;
                string customerName = snapshot.Child("customer").Child("name").Value.ToString();
                string customerEmail = snapshot.Child("customer").Child("email").Value.ToString();

                customerNameText.text = $"Customer Name: {customerName}";
                customerEmailText.text = $"Customer Email: {customerEmail}";
                DisplayCustomerPoints(snapshot);
            }
            else
            {
                feedbackText.text = "Customer not found!";
            }
        });
    }

    private void DisplayCustomerPoints(DataSnapshot snapshot)
    {
        int currentPoints = 0;
        DataSnapshot transactionsSnapshot = snapshot.Child("transactions");

        if (transactionsSnapshot.ChildrenCount > 0)
        {
            foreach (DataSnapshot transaction in transactionsSnapshot.Children)
            {
                if (transaction.HasChild("points"))
                {
                    currentPoints = int.Parse(transaction.Child("points").Child("current_points").Value.ToString());
                }
            }
        }

        currentPointsText.text = $"Current Points: {currentPoints}";
    }

    private void FetchAndDisplayCustomerImage()
    {
        string imageFileName = customerID + ".jpg";
        StorageReference imageRef = storageReference.Child("customer_images/" + imageFileName);

        const long maxAllowedSize = 1 * 1024 * 1024;
        imageRef.GetBytesAsync(maxAllowedSize).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                byte[] fileContents = task.Result;
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(fileContents);
                customerImage.texture = texture;
            }
        });
    }

    public void DeductPointsBasedOnTransaction()
    {
        if (float.TryParse(transactionCostInput.text, out float transactionCost))
        {
            int pointsToDeduct = Mathf.FloorToInt(transactionCost);
            ValidateAndDeductPoints(transactionCost, pointsToDeduct);
        }
        else
        {
            feedbackText.text = "Invalid transaction cost!";
        }
    }

    private void ValidateAndDeductPoints(float transactionCost, int pointsToDeduct)
    {
        databaseReference.Child("customers").Child(customerID).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                DataSnapshot snapshot = task.Result;
                int currentPoints = 0;
                DataSnapshot transactionsSnapshot = snapshot.Child("transactions");

                if (transactionsSnapshot.ChildrenCount > 0)
                {
                    foreach (DataSnapshot transaction in transactionsSnapshot.Children)
                    {
                        if (transaction.HasChild("points"))
                        {
                            currentPoints = int.Parse(transaction.Child("points").Child("current_points").Value.ToString());
                        }
                    }
                }

                if (currentPoints >= pointsToDeduct)
                {
                    UpdatePointsInFirebase(transactionCost, pointsToDeduct, currentPoints);
                }
                else
                {
                    feedbackText.text = "Not enough points to deduct!";
                }
            }
        });
    }

    private void UpdatePointsInFirebase(float transactionCost, int pointsToDeduct, int currentPoints)
    {
        string newTransactionKey = databaseReference.Child("customers").Child(customerID).Child("transactions").Push().Key;
        Dictionary<string, object> newTransaction = new Dictionary<string, object>
        {
            { "transaction_date", System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") },
            { "transaction_cost", transactionCost },
            { "payment_method", paymentMethod },
            { "NameOfAdminThatTransact", testName },
            { "points", new Dictionary<string, object>
                {
                    { "deducted", pointsToDeduct },
                    { "added", 0 },
                    { "current_points", currentPoints - pointsToDeduct }
                }
            }
        };

        databaseReference.Child("customers").Child(customerID).Child("transactions").Child(newTransactionKey).SetValueAsync(newTransaction)
        .ContinueWithOnMainThread(updateTask =>
        {
            if (updateTask.IsCompleted)
            {
                feedbackText.text = "Points deducted successfully! Dont click any button";
                DelayedSceneTransition(); // Call the delayed transition
            }
        });
    }

    public void DelayedSceneTransition()
    {
        Invoke("GoToCustomerDecisionScene", 3f); // Wait for 5 seconds
    }

    private void GoToCustomerDecisionScene()
    {
        SceneManager.LoadScene("CustomerDecision"); // Transition to the "CustomerDecision" scene
    }
}
