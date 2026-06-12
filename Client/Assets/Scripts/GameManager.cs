using Assets.Scripts.GameData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class GameManager : MonoBehaviour
{
     public string server_address = "127.0.0.1:65001";




    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (!_instance)
            {
                _instance = FindObjectOfType(typeof(GameManager)) as GameManager;

                if (_instance == null)
                    Debug.Log("no Singleton obj");
            }
            return _instance;
        }
    }

    public ResourceLoader resource = new ResourceLoader();

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);


        StartCoroutine(resource.LoadAsync()); // 시작 시 게임 데이터를 병렬로 로드
    }

    async Task Start()
    {
        //SRDebug.Init();
    }

    public void StartGame()
    {

    }

    public void GameOver()
    {

    }

    public void BackToMenu()
    {

    }
}

