using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour {

    public GameObject[] players;
    MapGenerator generator;

    int playersAmount;
    
    MapGenerator.Position[] playersSpawnPositions;

    void Start ()
    {
        GetPlayersAmountInCurrentGame(1);
        generator = GameObject.FindGameObjectWithTag("Generator").GetComponent<MapGenerator>();
        //устанавливаем точки для спауна персонажей
        playersSpawnPositions = new MapGenerator.Position[4];
        playersSpawnPositions[0] = new MapGenerator.Position(1, 1);
        playersSpawnPositions[1] = new MapGenerator.Position(1, generator.GetMapSize() - 2);
        playersSpawnPositions[2] = new MapGenerator.Position(generator.GetMapSize() - 2, 1);
        playersSpawnPositions[3] = new MapGenerator.Position(generator.GetMapSize() - 2, generator.GetMapSize() - 2);
        SpawnPlayers();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("QUIT");
            Application.Quit();
        }
    }

    //спаун игровых персонажей
    void SpawnPlayers()
    {
        playersAmount = players.Length;
        for(int i = 0; i < playersAmount; i++)
        {
            Vector3 pos = (generator.GetCellPositionInUnits(playersSpawnPositions[i].z, playersSpawnPositions[i].x));
            Instantiate(players[i], pos, players[i].transform.rotation);
        }
    }

    //получение текущего количества персонажей в игре
    public void GetPlayersAmountInCurrentGame(int n)
    {
        playersAmount = n;
    }

    //уменьшить количество персонажей(после смерти)
    public void DecreasePlayersAmount()
    {
        playersAmount--;
    }

    //закрыть игру

}
