using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Init : MonoBehaviour
{
    
    private void OnSignedIn()
    {
        Debug.Log(message: $"Token: {AuthenticationService.Instance.AccessToken}");
        Debug.Log(message: $"Player ID: {AuthenticationService.Instance.PlayerId}");

    }
    // Start is called before the first frame update
    async void Start()
    {
        await UnityServices.InitializeAsync();

        if(UnityServices.State == ServicesInitializationState.Initialized)
        {

            AuthenticationService.Instance.SignedIn += OnSignedIn;
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

           
            if (AuthenticationService.Instance.IsSignedIn)
            {
                string username = PlayerPrefs.GetString(key: "Username");

                if(username == "")
                {
                    username = "Player";
                    PlayerPrefs.SetString("Username", username);
                }

                SceneManager.LoadScene("MainMenu");
            }
        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
