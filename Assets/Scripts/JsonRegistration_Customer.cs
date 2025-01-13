using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;
using UnityEngine.SceneManagement;
using Firebase.Database;
using System.Security.Cryptography;
using System.Text;
using ZXing.Client.Result;



[System.Serializable]
public class Customer
{
    public string customer_id;
    public CustomerDetails customer;
    public Transaction[] transactions;
}

[System.Serializable]
public class CustomerDetails
{
    public string name;
    public string pin;
    public string email;
}

[System.Serializable]
public class Transaction
{
    public string transaction_date;
    public Points points;
    public string payment_method;
    public string NameOfAdminThatTransact;
}

[System.Serializable]
public class Points
{
    public int deducted;
    public int added;
    public int current_points;
}

[System.Serializable]
public class CustomersData
{
    public Customer[] customers;
}

[System.Serializable]
public static class DataHolder
{
    public static string customerID;
    public static string customerEmail;
}



public class JsonRegistration_Customer : MonoBehaviour
{
    // Path for the JSON file, will be set based on platform
    private string filePath;

    // Reference to the TMP Input Fields in the UI
    public TMP_InputField nameInputField;
    public TMP_InputField pinInputField;
    public TMP_InputField emailInputField;

    // Reference to the Submit Button
    public Button submitButton;

    // Reference to the TMP Text for displaying error messages
    public TextMeshProUGUI errorMessage;
    public GameObject OutputError;
    // Firebase Database Reference
    private DatabaseReference dbref;
    public bool isReg= false;

    private void Start()
    {
        // Initialize Firebase Database Reference
        dbref = FirebaseDatabase.DefaultInstance.RootReference;

        // Set the file path for the current platform
        SetFilePath();

        // Disable the submit button by default
        submitButton.interactable = false;

        // Add listeners to check for changes in the input fields
        nameInputField.onValueChanged.AddListener(delegate { ValidateInput(); });
        pinInputField.onValueChanged.AddListener(delegate { ValidateInput(); });
        emailInputField.onValueChanged.AddListener(delegate { ValidateInput(); });

        // Create the file if it doesn't exist
        CreateFileIfNotExists();
    }
    private void Update()
    {
        if (isReg)
        {
           SceneManager.LoadScene("PicCustomer");
        }
    }

    private void SetFilePath()
    {
        // Use Application.persistentDataPath for Android and other platforms
        filePath = Path.Combine(Application.persistentDataPath, "customersData.json");
        Debug.Log("File Path: " + filePath); // Log the file path for debugging
    }

    private void CreateFileIfNotExists()
    {
        if (!File.Exists(filePath))
        {
            // If the file does not exist, create an empty CustomersData object and save it
            CustomersData newCustomersData = new CustomersData
            {
                customers = new Customer[0] // Create an empty array of customers
            };

            // Convert the empty CustomersData to JSON
            string emptyJson = JsonUtility.ToJson(newCustomersData, true);

            // Write the empty JSON to the file
            File.WriteAllText(filePath, emptyJson);

            Debug.Log("File created at: " + filePath);
        }
        else
        {
            Debug.Log("File already exists at: " + filePath); // Log when file already exists
        }
    }

    // Method to validate whether all input fields are filled
    private void ValidateInput()
    {
        // Enable the submit button only if all fields have text
        if (!string.IsNullOrEmpty(nameInputField.text) &&
            !string.IsNullOrEmpty(pinInputField.text) &&
            !string.IsNullOrEmpty(emailInputField.text))
        {
            submitButton.interactable = true; // Enable the button
            errorMessage.text = ""; // Clear any previous error message
        }
        else
        {
            submitButton.interactable = false; // Disable the button
        }
    }

    // Call this method when the submit button is clicked
    private string HashPin(string pin)
    {
        using (SHA256 sha256Hash = SHA256.Create())
        {
            // Convert the pin string to a byte array
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(pin));

            // Convert byte array to a string
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }

    // Modify the RegisterNewCustomer method
    // Modify the RegisterNewCustomer method
    public void RegisterNewCustomer()
    {
        try
        {
/*            OutputError.SetActive(false);
            OutputError.SetActive(true);*/
            // Get the input from the TMP Input Fields
            string input_name = nameInputField.text;
            string input_pin = pinInputField.text;
            string input_email = emailInputField.text;

            // Hash the pin before storing it
            string hashedPin = HashPin(input_pin);

            // Check Firebase for existing customers with the same email
            dbref.Child("customers").GetValueAsync().ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;

                    // Check Firebase for duplicate email
                    foreach (DataSnapshot customerSnapshot in snapshot.Children)
                    {
                        string existingEmail = customerSnapshot.Child("customer/email").Value.ToString();

                        if (existingEmail == input_email)
                        {
                            // Email already exists in Firebase, display error message and stop registration
                            errorMessage.text = "Error: A customer with this email already exists in Firebase.";
                            Debug.LogError("Error: A customer with this email already exists in Firebase.");
                            return;
                        }
                    }

                    // If no duplicate email is found in Firebase, check locally
                    if (!CheckLocalForDuplicateEmail(input_email))
                    {
                        // If no duplicate found locally, proceed with registration
                        RegisterCustomerLocallyAndInFirebase(input_name, hashedPin, input_email);
                    }
                    else
                    {
                        errorMessage.text = "Error: A customer with this email already exists locally.";
                        Debug.LogError("Error: A customer with this email already exists locally.");
                    }
                }
                else
                {
                    Debug.LogError("Error checking Firebase for existing customers: " + task.Exception);
                    errorMessage.text = "Error checking Firebase for existing customers.";
                }
            });
        }
        catch (System.Exception ex)
        {
            // Display error message using TextMeshPro
            errorMessage.text = $"Error: {ex.Message}";
            Debug.LogError($"Error: {ex.Message}"); // Log the error
        }
    }

    // Helper function to check locally for duplicate emails
    private bool CheckLocalForDuplicateEmail(string input_email)
    {
        // Check if the file exists
        if (File.Exists(filePath))
        {
            // Read the JSON file content
            string json = File.ReadAllText(filePath);
            CustomersData customersData = JsonUtility.FromJson<CustomersData>(json);

            // Check if the email already exists locally
            foreach (Customer customer in customersData.customers)
            {
                if (customer.customer.email == input_email)
                {
                    return true; // Email already exists
                }
            }
        }

        return false; // No duplicate email found locally
    }

    // Helper method to register the customer locally and in Firebase
    private void RegisterCustomerLocallyAndInFirebase(string input_name, string hashedPin, string input_email)
    {
        try
        {
/*            OutputError.SetActive(false);
            OutputError.SetActive(true);*/
            CustomersData customersData = new CustomersData();

            // Check if the file exists
            if (File.Exists(filePath))
            {
                // Read the JSON file content
                string json = File.ReadAllText(filePath);
                Debug.Log("Original JSON: " + json); // Log the content of the original JSON file

                // Deserialize the content into the CustomersData object
                customersData = JsonUtility.FromJson<CustomersData>(json);
            }
            else
            {
                customersData.customers = new Customer[0];
            }

            // Create a new customer with the input data, including the hashed pin
            Customer newCustomer = new Customer
            {
                customer_id = GenerateTransactionID(),
                customer = new CustomerDetails
                {
                    name = input_name,
                    pin = hashedPin,  // Store hashed pin
                    email = input_email
                },
                transactions = new Transaction[0]
            };

            Debug.Log("Customer Registered: " + newCustomer.customer_id); // Log customer registration

            // Add the new customer to the local list
            var customersList = new List<Customer>(customersData.customers);
            customersList.Add(newCustomer);
            customersData.customers = customersList.ToArray();

            // Convert the updated data back to JSON
            string updatedJson = JsonUtility.ToJson(customersData, true);
            Debug.Log("Updated JSON: " + updatedJson); // Log the updated JSON

            // Write the updated JSON to the file
            File.WriteAllText(filePath, updatedJson);

            // Store the new customer's customer_id in the static DataHolder class
            DataHolder.customerID = newCustomer.customer_id;
            DataHolder.customerEmail = newCustomer.customer.email;

            // Save the new customer data to Firebase
            string jsonToFirebase = JsonUtility.ToJson(newCustomer);
            dbref.Child("customers").Child(newCustomer.customer_id).SetRawJsonValueAsync(jsonToFirebase);

            Debug.Log("Customer data saved to Firebase. " + isReg);
            isReg = true;
            sceneloader();

            // Transition to the scene where the QR code is generated

        }
        catch (System.Exception ex)
        {
            // Display error message using TextMeshPro
            errorMessage.text = $"Error: {ex.Message}";
            Debug.LogError($"Error: {ex.Message}"); // Log the error
        }
    }
    public void sceneloader()
    {
        Debug.Log("Transitioning "+isReg);
        if (isReg)
        {
            SceneManager.LoadScene("PicCustomer");
        }
        else
        {
            Debug.Log("Gay");
        }
    }
    private string GenerateTransactionID()
    {
        return System.Guid.NewGuid().ToString(); // Generate a unique ID for the customer
    }

}
