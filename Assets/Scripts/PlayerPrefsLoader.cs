using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerPrefsLoader : MonoBehaviour
{
    // Reference to your TextMesh
    public TextMeshProUGUI textMesh;

    // Key for the PlayerPrefs value
    public string playerPrefKey = "MyValue";

    void Start()
    {
        // Retrieve the value from PlayerPrefs (defaulting to an empty string if the key doesn't exist)
        string value = PlayerPrefs.GetString("Username");

        // Assign the retrieved value to the TextMesh
        if (textMesh != null)
        {
            textMesh.text = value;
        }
        else
        {
            Debug.LogError("TextMesh component is not assigned.");
        }
    }
}