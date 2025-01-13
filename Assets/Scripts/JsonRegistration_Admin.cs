using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;
using UnityEngine.SceneManagement;
using Firebase.Database;
using System.Security.Cryptography;
using System.Text;

[System.Serializable]
public class Admin
{
    public string admin_id;
    public AdminDetails admin;
}

[System.Serializable]
public class AdminDetails
{
    public string name;
    public string pin;
    public string email;
}

[System.Serializable]
public class AdminsData
{
    public Admin[] admins;
}

public class JsonRegistration_Admin : MonoBehaviour
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
    public TextMeshProUGUI errorMessage2;

    public GameObject OutputError;

    // Firebase Database Reference
    private DatabaseReference dbref;

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

    private void SetFilePath()
    {
        // Use Application.persistentDataPath for Android and other platforms
        filePath = Path.Combine(Application.persistentDataPath, "adminsData.json");
        Debug.Log("File Path: " + filePath); // Log the file path for debugging
    }

    private void CreateFileIfNotExists()
    {
        if (!File.Exists(filePath))
        {
            // If the file does not exist, create an empty AdminsData object and save it
            AdminsData newAdminsData = new AdminsData
            {
                admins = new Admin[0] // Create an empty array of admins
            };

            // Convert the empty AdminsData to JSON
            string emptyJson = JsonUtility.ToJson(newAdminsData, true);

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
    // Regex pattern for email validation
    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private void ValidateInput()
    {
        bool isNameValid = !string.IsNullOrEmpty(nameInputField.text);
        bool isPinValid = !string.IsNullOrEmpty(pinInputField.text);
        bool isEmailValid = IsValidEmail(emailInputField.text);

        if (isNameValid && isPinValid && isEmailValid)
        {
            submitButton.interactable = true; // Enable the button
            errorMessage.text = ""; // Clear any previous error message
        }
        else
        {
            submitButton.interactable = false; // Disable the button

            if (!isEmailValid)
            {
                errorMessage.text = "Email is invalid format or empty."; // Display an error message
            }
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

    // Register new admin
    public void RegisterNewAdmin()
    {
        try
        {
            OutputError.SetActive(false);
            OutputError.SetActive(true);
            // Get the input from the TMP Input Fields
            string input_name = nameInputField.text;
            string input_pin = pinInputField.text;
            string input_email = emailInputField.text;

            // Hash the pin before storing it
            string hashedPin = HashPin(input_pin);

            // Check Firebase for existing admins with the same email
            dbref.Child("admins").GetValueAsync().ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;

                    // Iterate through each admin in Firebase
                    foreach (DataSnapshot adminSnapshot in snapshot.Children)
                    {
                        string existingEmail = adminSnapshot.Child("admin/email").Value.ToString();

                        if (existingEmail == input_email)
                        {
                            // Email already exists, display error message and stop registration
                            errorMessage2.text = "Error: An admin with this email already exists.";
                            Debug.LogError("Error: An admin with this email already exists.");
                            
                            return;
                        }
                    }

                    // If no duplicate email is found, proceed with registration
                    RegisterAdminLocallyAndInFirebase(input_name, hashedPin, input_email);
                }
                else
                {
                    Debug.LogError("Error checking Firebase for existing admins: " + task.Exception);
                    errorMessage2.text = "Error checking Firebase for existing admins.";
                    OutputError.SetActive(false);
                    OutputError.SetActive(true);
                }
            });
        }
        catch (System.Exception ex)
        {
            // Display error message using TextMeshPro
            errorMessage2.text = $"Error: {ex.Message}";
            Debug.LogError($"Error: {ex.Message}"); // Log the error
        }
    }

    // Helper method to register the admin locally and in Firebase
    private void RegisterAdminLocallyAndInFirebase(string input_name, string hashedPin, string input_email)
    {
        try
        {
            OutputError.SetActive(false);
            OutputError.SetActive(true);
            AdminsData adminsData = new AdminsData();

            // Check if the file exists
            if (File.Exists(filePath))
            {
                // Read the JSON file content
                string json = File.ReadAllText(filePath);
                Debug.Log("Original JSON: " + json); // Log the content of the original JSON file

                // Deserialize the content into the AdminsData object
                adminsData = JsonUtility.FromJson<AdminsData>(json);
            }
            else
            {
                // Initialize admins array if the file doesn't exist
                adminsData.admins = new Admin[0];
            }

            // Ensure that adminsData.admins is not null
            if (adminsData.admins == null)
            {
                adminsData.admins = new Admin[0]; // Initialize to an empty array if null
            }

            // Create a new admin with the input data, including the hashed pin
            Admin newAdmin = new Admin
            {
                admin_id = GenerateAdminID(),
                admin = new AdminDetails
                {
                    name = input_name,
                    pin = hashedPin,  // Store hashed pin
                    email = input_email
                }
            };

            Debug.Log("Admin Registered: " + newAdmin.admin_id); // Log admin registration

            // Add the new admin to the local list
            var adminsList = new List<Admin>(adminsData.admins);
            adminsList.Add(newAdmin);
            adminsData.admins = adminsList.ToArray();

            // Convert the updated data back to JSON
            string updatedJson = JsonUtility.ToJson(adminsData, true);
            Debug.Log("Updated JSON: " + updatedJson); // Log the updated JSON

            // Write the updated JSON to the file
            File.WriteAllText(filePath, updatedJson);

            // Save the new admin data to Firebase
            string jsonToFirebase = JsonUtility.ToJson(newAdmin);
            dbref.Child("admins").Child(newAdmin.admin_id).SetRawJsonValueAsync(jsonToFirebase);

            Debug.Log("Admin data saved to Firebase.");

            // Transition to the scene where the QR code is generated or another admin-specific scene
            SceneManager.LoadScene("VerifyPin_Admin");
        }
        catch (System.Exception ex)
        {
            // Display error message using TextMeshPro
            errorMessage2.text = $"Error: {ex.Message}";
            Debug.LogError($"Error: {ex.Message}"); // Log the error
        }
    }


    private string GenerateAdminID()
    {
        return System.Guid.NewGuid().ToString(); // Generate a unique ID for the admin
    }
}
