using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Accesiblity : MonoBehaviour
{

    public void scanning()
    {
        SceneManager.LoadScene("ScanQR");
    }

    public void registering()
    {
        SceneManager.LoadScene("RegCustomer");
    }

    public void home()
    {
        SceneManager.LoadScene("Home");
    }

    public void add()
    {
        SceneManager.LoadScene("VerifyPin_Customer_addpoints");
    }
    public void remove()
    {
        SceneManager.LoadScene("VerifyPin_Customer_deductpoints");
    }

    public void back()
    {
        SceneManager.LoadScene("CustomerDecision");
    }

    public void registrationAdmin()
    {
        SceneManager.LoadScene("VerifyPin_AdminCreation");
    }

    public void logout()
    {
        ClearAllPlayerPrefs();
        SceneManager.LoadScene("LoginPage");
    }
    public void back_logout()
    {
        SceneManager.LoadScene("LoginPage");
    }
    public void ClearAllPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save(); // Ensure changes are saved
        Debug.Log("All PlayerPrefs have been cleared.");
    }

}
