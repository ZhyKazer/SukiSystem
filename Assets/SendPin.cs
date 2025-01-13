using System;
using System.Net;
using System.Net.Mail;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;  // Assuming you're using Unity's UI components

public class EmailPinSender : MonoBehaviour
{
    // UI Elements for PIN entry
    public TMP_InputField pinInputField; // Drag your input field here in the Unity Inspector
    private string currentPin = "";   // Store the current entered PIN
    private string generatedPin;      // Store the randomly generated PIN

    // Email settings
    private string recipientEmail = "vapethrftcard@gmail.com"; // Receiver's email
    private string senderEmail = "vapethrift.business@gmail.com"; // Sender's email
    private string senderPassword = "tfif cone ercz thrx"; // App Password
    private string smtpHost = "smtp.gmail.com"; // SMTP server host (for Gmail)
    private int smtpPort = 587; // SMTP port (for Gmail)
    private bool enableSSL = true; // Enable SSL for secure connection

    void Start()
    {
        SendRandomPin();  // Send the random PIN to the specified email
    }

    public void SendRandomPin()
    {
        try
        {
            // Generate a random 4-digit PIN
            generatedPin = UnityEngine.Random.Range(1000, 9999).ToString();
            Debug.Log("Generated PIN: " + generatedPin);

            // Create the email message
            MailMessage mail = new MailMessage();
            mail.From = new MailAddress(senderEmail);
            mail.To.Add(recipientEmail);
            mail.Subject = "Your Random 4-Digit PIN";
            mail.Body = "Your PIN is: " + generatedPin;

            // Set up the SMTP client
            SmtpClient smtpServer = new SmtpClient(smtpHost);
            smtpServer.Port = smtpPort;
            smtpServer.Credentials = new NetworkCredential(senderEmail, senderPassword) as ICredentialsByHost;
            smtpServer.EnableSsl = enableSSL;

            // Send the email
            smtpServer.Send(mail);
            Debug.Log("Email sent successfully");
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to send email: " + e.Message);
        }
    }

    // Handles the number button clicks for entering the PIN
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

        // Check if the entered PIN is complete (4 digits)
        if (currentPin.Length == 4)
        {
            if (currentPin == generatedPin) // Compare the entered PIN with the generated PIN
            {
                Debug.Log("Correct PIN. Proceeding to the next scene.");
                ProceedToApp();  // Call the method to move to the next scene or action
            }
            else
            {
                Debug.Log("Incorrect PIN. Resetting input.");
                OnClearButtonClick();  // Clear the input if the PIN is incorrect
            }
        }
    }

    // Clears the current PIN input
    public void OnClearButtonClick()
    {
        currentPin = "";
        pinInputField.text = currentPin;
    }

    // Displays the current entered PIN (for debugging or logging purposes)
    public void OnSubmitButtonClick()
    {
        Debug.Log("Entered PIN: " + currentPin);
    }

    // Handles the backspace functionality to remove the last entered digit
    public void OnBackspaceButtonClick()
    {
        if (currentPin.Length > 0)
        {
            currentPin = currentPin.Substring(0, currentPin.Length - 1);
            pinInputField.text = currentPin;
        }
    }

    // A placeholder method to move to the next scene or application step
    private void ProceedToApp()
    {
        // Insert logic to load a new scene or perform the desired action after correct PIN entry
        SceneManager.LoadScene("RegAdmin");
    }
}
