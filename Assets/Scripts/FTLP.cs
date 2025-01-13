using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using Firebase.Database;
using Firebase.Extensions;

public class FTLP : MonoBehaviour
{
    public TMP_InputField pinInputField;
    private string currentPin = "";
    private string hashedAdminPin = ""; // Admin PIN loaded from Firebase (hashed)

    private string filePath;
    private AdminsData adminsData;
    private DatabaseReference dbref;

    private void Start()
    {
        dbref = FirebaseDatabase.DefaultInstance.RootReference;
        SetFilePath();

        // Check if locally saved credentials exist
        CheckLocalPreferences();
    }

    private void SetFilePath()
    {
        filePath = Path.Combine(Application.persistentDataPath, "adminsData.json");
        Debug.Log("FTLP File Path: " + filePath);
    }

    private void CheckLocalPreferences()
    {
        // Check if there is a locally saved email and hashed PIN
        if (PlayerPrefs.HasKey("LastEmail") && PlayerPrefs.HasKey("HashedPIN"))
        {
            string savedEmail = PlayerPrefs.GetString("LastEmail");
            string savedHashedPin = PlayerPrefs.GetString("HashedPIN");

            Debug.Log("Local credentials found. Verifying with Firebase...");

            // Check Firebase for the saved admin
            CheckFirebaseForAdmin(savedEmail, savedHashedPin);
        }
        else
        {
            // If no local credentials are found, redirect to the login page
            Debug.Log("No local credentials found. Redirecting to Login Page.");
            StartCoroutine(RedirectToLoginPage());
        }
    }

    private void CheckFirebaseForAdmin(string email, string localHashedPIN)
    {
        dbref.Child("admins").OrderByChild("admin/email").EqualTo(email).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                if (snapshot.Exists)
                {
                    // There should be at most one matching admin
                    foreach (DataSnapshot adminSnapshot in snapshot.Children)
                    {
                        // Fetch the hashed admin PIN from Firebase
                        hashedAdminPin = adminSnapshot.Child("admin").Child("pin").Value.ToString();

                        if (hashedAdminPin == localHashedPIN)
                        {
                            // Store admin ID for later use
                            string adminId = adminSnapshot.Child("admin_id").Value.ToString();
                            AdminDataHolder.adminID = adminId;

                            Debug.Log("Admin credentials verified with Firebase.");

                            // Proceed with PIN entry
                            return;
                        }
                        else
                        {
                            Debug.Log("Hashed PIN mismatch. Redirecting to Login Page.");
                            StartCoroutine(RedirectToLoginPage());
                        }
                    }
                }
                else
                {
                    // If no admin found in Firebase
                    Debug.Log("No matching admin found in Firebase. Redirecting to Login Page.");
                    StartCoroutine(RedirectToLoginPage());
                }
            }
            else
            {
                Debug.LogError("Failed to check Firebase. Redirecting to Login Page.");
                StartCoroutine(RedirectToLoginPage());
            }
        });
    }

    IEnumerator RedirectToLoginPage()
    {
        yield return new WaitForSeconds(1); // Optional delay
        SceneManager.LoadScene("LoginPage");
    }

    public void OnNumberButtonClick(string number)
    {
        if (pinInputField == null)
        {
            Debug.LogError("pinInputField is not assigned!");
            return;
        }

        if (currentPin.Length < 4) // Assuming a 4-digit PIN
        {
            currentPin += number;
            pinInputField.text = currentPin;
        }

        // Check if PIN is complete (4 digits) and valid
        if (currentPin.Length == 4)
        {
            // Hash the entered PIN
            string hashedEnteredPin = HashPIN(currentPin);

            if (hashedEnteredPin == hashedAdminPin) // Compare hashed entered PIN with hashed admin PIN
            {
                Debug.Log("Correct PIN. Proceeding to the next scene.");
                ProceedToApp();
            }
            else
            {
                Debug.Log("Incorrect PIN. Resetting input.");
                OnClearButtonClick();
            }
        }
    }

    public void OnClearButtonClick()
    {
        currentPin = "";
        pinInputField.text = currentPin;
    }

    public void OnSubmitButtonClick()
    {
        Debug.Log("Entered PIN: " + currentPin);
    }

    public void OnBackspaceButtonClick()
    {
        if (currentPin.Length > 0)
        {
            currentPin = currentPin.Substring(0, currentPin.Length - 1);
            pinInputField.text = currentPin;
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
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }

    private void ProceedToApp()
    {
        // Proceed to the next scene, e.g., the main app scene
        SceneManager.LoadScene("Home");
    }
}
