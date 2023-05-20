using System.Collections;
using System.Collections.Generic;
using Unity.Services.Core;
using Unity.Services.Authentication;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine.SceneManagement;
public class LobbyManager : MonoBehaviour
{
    [Header("Main Menu")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private Button getLobbiesListBtn;
    [SerializeField] private GameObject lobbyinfoprefabs;
    [SerializeField] private GameObject lobbiesinfocontent;
    [SerializeField] private GameObject lobbyList;


    [Space(10)]
    [Header("Create Room Panel")]
    [SerializeField] private GameObject createRoomPanel;
    [SerializeField] private TMP_InputField roomNameIF;
    [SerializeField] private TextMeshProUGUI maxPlayer;
    [SerializeField] private Button CreateRoomBtn;
    [SerializeField] private TMP_InputField playerNameIF;
    [SerializeField] private Toggle isPrivateToggle;

    [Space(10)]
    [Header("Room Panel")]
    [SerializeField] private GameObject roomPanel;
    [SerializeField] private TextMeshProUGUI roomName;
    [SerializeField] private TextMeshProUGUI roomCode;
    [SerializeField] private GameObject playerInfoContent;
    [SerializeField] private GameObject PlayerInfoPrefab;
    [SerializeField] private Button leaveRoomBtn;
    [SerializeField] private Button startGameBtn;


    [Space(10)]
    [Header("Join Room Panel")]
    [SerializeField] private GameObject joinRoomPanel;
    [SerializeField] private TMP_InputField roomCodeIF;
    [SerializeField] private Button joinRoomBtn;

    Lobby currentLobby;
    private string PlayerId;
    // Start is called before the first frame update
    async void Start()
    {
        await UnityServices.InitializeAsync();
        AuthenticationService.Instance.SignedIn += () =>
        {
            PlayerId = AuthenticationService.Instance.PlayerId;
            Debug.Log("Signed in " + PlayerId);
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        CreateRoomBtn.onClick.AddListener(CreateLobby);
        joinRoomBtn.onClick.AddListener(JoinLobbyWithCode);
        getLobbiesListBtn.onClick.AddListener(ListPubbicLobbies);

        playerNameIF.onValueChanged.AddListener(delegate
        {
            PlayerPrefs.SetString("Name", playerNameIF.text);
        });

        playerNameIF.text = PlayerPrefs.GetString("Name");

        leaveRoomBtn.onClick.AddListener(LeaveRoom);
    }

    // Update is called once per frame
    void Update()
    {
        HandleLobbiesListUpdate();
        HandleLobbyHeartBeat();
        HandleRoomUpdate();
    }

    private async void CreateLobby()
    {    
        
           try
            {
                string lobbyName = roomNameIF.text;
                int.TryParse(maxPlayer.text, out int maxPlayers);
                CreateLobbyOptions options = new CreateLobbyOptions
                {
                    IsPrivate = isPrivateToggle.isOn,
                    Player = GetPlayer(),
                    Data = new Dictionary<string, DataObject>
                {
                    {"IsGameStarted", new DataObject(DataObject.VisibilityOptions.Member,"false") }
                }
                };
                currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
                Debug.Log("Room Created: " + currentLobby.Id);
                EnterRoom();

            }
            catch (LobbyServiceException ex)
            {
                Debug.Log(ex);
            }
  

    }

    private void EnterRoom()
    {
        mainMenu.SetActive(false);
        createRoomPanel.SetActive(false);
        lobbyList.SetActive(false);

        roomPanel.SetActive(true);
        roomName.text = currentLobby.Name;
        roomCode.text = currentLobby.LobbyCode;

        foreach (Player player in currentLobby.Players)
        {
            Debug.Log("Player name: " + player.Data["PlayerName"].Value);
        }
        VisualizeRoomDetails();
    }
    private float roomUpdateTimer = 2f;
    private async void HandleRoomUpdate()
    {
        if (currentLobby != null)
        {
            roomUpdateTimer -= Time.deltaTime;
            if (roomUpdateTimer <= 0)
            {
                roomUpdateTimer = 2f;
                try
                {
                    if (isInLobby())
                    {
                        currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
                        VisualizeRoomDetails();
                    }
                   
                }
                catch (LobbyServiceException e)
                {
                    Debug.Log(e);
                    if(currentLobby.IsPrivate && (e.Reason == LobbyExceptionReason.Forbidden || e.Reason == LobbyExceptionReason.LobbyNotFound))
                    {
                        currentLobby = null;
                        ExitRoom();
                    }
                }
            }
        }


    }

    private bool isInLobby()
    {
        foreach(Player _player in currentLobby.Players)
        {
            if (_player.Id == PlayerId)
            {
                return true;
            }
        }
        currentLobby = null;
        return false;
    }

    private void VisualizeRoomDetails()
    {
        for(int i = 0; i < playerInfoContent.transform.childCount; i++)
        {
            Destroy(playerInfoContent.transform.GetChild(i).gameObject);
        }
        if (isInLobby())
        {
            foreach (Player player in currentLobby.Players)
            {
                GameObject newPlayerInfo = Instantiate(PlayerInfoPrefab, playerInfoContent.transform);
                newPlayerInfo.GetComponentInChildren<TextMeshProUGUI>().text = player.Data["PlayerName"].Value;
                if (IsHost() && player.Id!=PlayerId)
                {
                    Button kickBtn = newPlayerInfo.GetComponentInChildren<Button>(true);
                    kickBtn.onClick.AddListener(()=>KickPlayer(player.Id));
                    kickBtn.gameObject.SetActive(true);
                }
            }
            if (IsHost())
            {
                startGameBtn.onClick.AddListener(StartGame);
                startGameBtn.GetComponentInChildren<TextMeshProUGUI>().text = "Start Game";
                startGameBtn.gameObject.SetActive(true);
            }
            else
            {
                if(IsGameStarted())
                {
                    startGameBtn.onClick.AddListener(EnterGame);
                    startGameBtn.gameObject.SetActive(true);
                    startGameBtn.GetComponentInChildren<TextMeshProUGUI>().text = "Enter Game";
                }
                else
                {
                    startGameBtn.onClick.RemoveAllListeners();
                    startGameBtn.gameObject.SetActive(false);
                }
            }
        }
        else
        {
            ExitRoom();
        }
       
    }

    private async void ListPubbicLobbies()
    {
        try
        {
            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync();
            VisualizeLobbyList(response.Results);
        }
        catch(LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private float updateLobbiesListTimer = 2f;
    private void HandleLobbiesListUpdate()
    {
        updateLobbiesListTimer -= Time.deltaTime;
        if (updateLobbiesListTimer<=0)
        {
            ListPubbicLobbies();
            updateLobbiesListTimer = 2f;
        }
    }

    private void VisualizeLobbyList(List<Lobby> publicLobbies)
    {
        for (int i = 0; i < lobbiesinfocontent.transform.childCount; i++){
            Destroy(lobbiesinfocontent.transform.GetChild(i).gameObject);
        }
        foreach (Lobby _lobby in publicLobbies)
        {
            GameObject newLobbyInfo = Instantiate(lobbyinfoprefabs, lobbiesinfocontent.transform);
            var lobbyDetailsTexts = newLobbyInfo.GetComponentsInChildren<TextMeshProUGUI>();
            lobbyDetailsTexts[0].text = _lobby.Name;
            lobbyDetailsTexts[1].text = (_lobby.MaxPlayers - _lobby.AvailableSlots).ToString() + "/" +_lobby.MaxPlayers.ToString();

            newLobbyInfo.GetComponentInChildren<Button>().onClick.AddListener(()=>JoinLobby(_lobby.Id));
        }
    }

    private async void JoinLobby(string _lobbyId)
    {
        try
        {
            JoinLobbyByIdOptions options = new JoinLobbyByIdOptions()
            {
                Player = GetPlayer()
            };
            currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(_lobbyId, options);
            EnterRoom();
            Debug.Log("Player in room: " + currentLobby.Players.Count);
        }catch(LobbyServiceException e)
        {
            Debug.Log(e);
        }
        
    }

    private float heartbeattimer = 15f;
    private async void HandleLobbyHeartBeat()
    {
        if(currentLobby != null && IsHost())
        {
            heartbeattimer -= Time.deltaTime;
            if(heartbeattimer <= 0)
            {
                heartbeattimer = 15f;
                await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);

            }
        }
    }

    private bool IsHost()
    {
        if(currentLobby != null && currentLobby.HostId == PlayerId)
        {
            return true;
        }
        return false;
    }

    private Player GetPlayer()
    {
        string playerName = PlayerPrefs.GetString("Name");
        if (playerName == null || playerName == "")
            playerName = PlayerId;
        Player player = new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) }
            }
        };
        return player;
    }

    private async void LeaveRoom()
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, PlayerId);
            ExitRoom();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void KickPlayer(string _playerId)
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, _playerId);
        }
        catch(LobbyServiceException e) 
        {
            Debug.Log(e);
        }
    }

    private void ExitRoom()
    {
        mainMenu.SetActive(true);
        createRoomPanel.SetActive(false);
        roomPanel.SetActive(false);
        ListPubbicLobbies();
    }

    private async void JoinLobbyWithCode()
    {
        string _lobbyCode = roomCodeIF.text;
        try
        {
            JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions()
            {
                Player = GetPlayer()
            };
            currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(_lobbyCode, options);
            EnterRoom();
            Debug.Log("Player in room: " + currentLobby.Players.Count);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

    }
    private async void StartGame()
    {
        if (currentLobby != null && IsHost())
        {
            try
            {
                UpdateLobbyOptions updateoptions = new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>()
                {
                    {"IsGameStarted", new DataObject(DataObject.VisibilityOptions.Member,"true") }
                }
                };
                currentLobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, updateoptions);

                EnterGame();
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }

    }
    private bool IsGameStarted()
    {
        if(currentLobby!=null)
        {
            if(currentLobby.Data["IsGameStarted"].Value=="true")
            {
                return true;
            }
        }
        return false;
    }
    private void EnterGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }


}
