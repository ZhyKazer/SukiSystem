using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine.SceneManagement;
using System.Security.Cryptography;
using System.Text;

public class Deduction_PinConfirmation : MonoBehaviour
{
    public TMP_InputField pinInputField; // Input field for pin entry
    private string currentPin = "";
    private string customerHashedPin = ""; // The hashed pin from Firebase
    private string customerID = QR_Scanning.ScannedQrCode;
    private int pointsToDeduct;
    private string adminName;

    private DatabaseReference databaseReference;

    void Start()
    {
        // Initialize Firebase reference
        databaseReference = FirebaseDatabase.DefaultInstance.RootReference;

        // Fetch customer's hashed PIN from Firebase
        FetchCustomerPin();
    }

    private void FetchCustomerPin()
    {
        databaseReference.Child("customers").Child(customerID).Child("customer").Child("pin").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot.Exists)
                {
                    customerHashedPin = snapshot.Value.ToString(); // Retrieve the hashed pin from the database
                }
                else
                {
                    Debug.LogError("Customer PIN not found.");
                }
            }
            else
            {
                Debug.LogError("Error fetching PIN: " + task.Exception);
            }
        });
    }

    public void OnNumberButtonClick(string number)
    {
        if (pinInputField == null)
        {
            Debug.LogError("PIN input field not assigned!");
            return;
        }

        if (currentPin.Length < 4)
        {
            currentPin += number;
            pinInputField.text = currentPin;
        }

        // Once the user enters 4 digits, validate the pin
        if (currentPin.Length == 4)
        {
            ValidatePin();
        }
    }

    private void ValidatePin()
    {
        // Hash the entered PIN using SHA-256
        string hashedEnteredPin = HashPIN(currentPin);

        // Compare the hashed entered PIN with the stored hashed PIN
        if (hashedEnteredPin == customerHashedPin)
        {
            Debug.Log("Correct PIN.");

            // Load the next scene when PIN is correct
            SceneManager.LoadScene("DeductPoints"); // Replace "NextSceneName" with your scene's actual name
        }
        else
        {
            Debug.Log(hashedEnteredPin);
            Debug.Log(customerHashedPin);
            Debug.Log("Incorrect PIN.");
            OnClearButtonClick(); // Clear the input field
        }
    }

    private string HashPIN(string pin)
    {
        // Hash the PIN using SHA-256
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(pin));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2")); // Convert each byte to hexadecimal
            }
            return builder.ToString();
        }
    }

    public void OnClearButtonClick()
    {
        // Clear the current pin and the input field
        currentPin = "";
        pinInputField.text = currentPin;
    }

    public void OnBackspaceButtonClick()
    {
        if (currentPin.Length > 0)
        {
            currentPin = currentPin.Substring(0, currentPin.Length - 1);
            pinInputField.text = currentPin;
        }
    }
}
