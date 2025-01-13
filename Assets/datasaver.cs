using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Firebase.Database;

[Serializable]
public class dataToSave
{
    public string name;
    public int coins;
    public int lvl;
    public int highscore;
}

public class datasaver : MonoBehaviour
{
    public dataToSave data;
    public string userID;
    DatabaseReference dbref;


    private void Awake()
    {
        dbref = FirebaseDatabase.DefaultInstance.RootReference;


    }
    public void savedatafn()
    {
        string json = JsonUtility.ToJson(data);
        dbref.Child("users").Child(userID).SetRawJsonValueAsync(json);
    }
    public void loaddatafn()
    {

    }
}
