using UnityEngine;
using Firebase.Database;
using System.Text;
using System.Security.Cryptography;
using UnityEngine.SceneManagement;
using Firebase.Extensions;
using TMPro;

public static class AdminDataHolder
{
    public static string adminID;
    // Add other admin-related data here if needed
}

public class LoginPage : MonoBehaviour
{
    private DatabaseReference dbref;

    // Reference to the TMP Input Fields
    public TMP_InputField EmailInputField; // Username (or email) input field
    public TMP_InputField pinInputField;   // PIN input field

    // Reference to display error messages to the user
    public TextMeshProUGUI errorMessage;

    private void Start()
    {
        ClearAllPlayerPrefs();
        dbref = FirebaseDatabase.DefaultInstance.RootReference;
    }

    // This method is called when the submit button is clicked
    public void OnSubmitButtonClick()
    {
        string email = EmailInputField.text; // Get the username/email
        string pin = pinInputField.text;        // Get the PIN

        // Input validation
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pin))
        {
            errorMessage.text = "Username and PIN cannot be empty.";
            Debug.LogError("Username or PIN is empty.");
            return;
        }

        OnSubmitLogin(email, pin); // Call the login method
    }

    public void OnSubmitLogin(string username, string pin)
    {
        // Hash the entered PIN without a salt
        string hashedPIN = HashPin(pin);

        // Check if the entered username (email) and hashed PIN match an admin
        CheckAdminLogin(username, hashedPIN);
    }

    void CheckAdminLogin(string adminEmail, string hashedPIN)
    {
        // Query the Firebase database for the admin with the provided email
        dbref.Child("admins").OrderByChild("admin/email").EqualTo(adminEmail).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot.Exists)
                {
                    // Loop through the found admins (there should be at most one)
                    foreach (DataSnapshot adminSnapshot in snapshot.Children)
                    {
                        // Retrieve the stored hashed PIN for comparison
                        string storedHashedPIN = adminSnapshot.Child("admin/pin").Value.ToString();
                        string storedUsername = adminSnapshot.Child("admin/name").Value.ToString();
                        // Compare the stored hashed PIN with the entered hashed PIN
                        if (storedHashedPIN == hashedPIN)
                        {
                            Debug.Log("Admin login successful.");

                            // Save login details locally
                            SaveLoginDetails(adminEmail, hashedPIN, storedUsername);

                            // Store admin ID for later use
                            string adminId = adminSnapshot.Child("admin_id").Value.ToString();
                            AdminDataHolder.adminID = adminId;

                            // Proceed to the main app
                            ProceedToApp();
                        }
                        else
                        {
                            errorMessage.text = "Incorrect PIN. Please try again.";
                            Debug.LogError("Incorrect PIN.");
                        }
                    }
                }
                else
                {
                    errorMessage.text = "Admin not found. Please check your username.";
                    Debug.LogError("Admin not found.");
                }
            }
            else
            {
                errorMessage.text = "Failed to retrieve admin data. Please check your internet connection.";
                Debug.LogError("Failed to retrieve admin data.");
            }
        });
    }

    void SaveLoginDetails(string email, string hashedPIN, string admin)
    {
        // Save the username (email) and hashed PIN locally, overwriting any previous saved login
        PlayerPrefs.SetString("Username", admin);
        PlayerPrefs.SetString("LastEmail", email);
        PlayerPrefs.SetString("HashedPIN", hashedPIN);
        PlayerPrefs.Save();
        Debug.Log("Login details saved to PlayerPrefs.");
    }

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

    public void ClearAllPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save(); // Ensure changes are saved
        Debug.Log("All PlayerPrefs have been cleared.");
    }

    void ProceedToApp()
    {
        // Load the main app scene
        SceneManager.LoadScene("Home");
        Debug.Log("Proceeding to main app.");
    }

    // Method to handle logout
    public void OnLogout()
    {
        // Clear the saved login details from PlayerPrefs
        PlayerPrefs.DeleteKey("LastEmail");
        PlayerPrefs.DeleteKey("HashedPIN");
        PlayerPrefs.Save();
        Debug.Log("Logged out and cleared saved credentials.");

        // Optionally, redirect to login page after logout
        SceneManager.LoadScene("LoginPage");
    }
}
