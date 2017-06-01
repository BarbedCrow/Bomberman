using UnityEngine;
using System.Collections;

public class MapGenerator : MonoBehaviour {

    public int mapSize;
    public int minObstaclesAmount, maxObstaclesAmount;
    public int minBoostersAmount, maxBoostersAmount;
    public GameObject quadPrefab;
    public GameObject undestrObstaclePrefab;
    public GameObject destrObstaclePrefab;
    public GameObject[] boosters;

    int obstaclesAmount;
    int boostersAmount;

    float objectSize;

    int[,] map;//игровая карта
    //возможные состояния игровой карты
    public const int FREECELL = 0;
    public const int UNDESTRUCTABLEOBSTACLE = -1;
    public const int DESTRUCTABLEOBSTACLE = 1;

    //позиция объекта на карте
    public struct Position
    {
        public int z, x;
        public Position(int _z, int _x)
        {
            z = _z;
            x = _x;
        }

        public static bool operator ==(Position pos1, Position pos2)
        {
            if (pos1.z == pos2.z && pos1.x == pos2.x)
                return true;
            else return false;
        }

        public static bool operator !=(Position pos1, Position pos2)
        {
            if (pos1.z != pos2.z || pos1.x != pos2.x)
                return true;
            else return false;
        }
    }

    //TODO//
    //Переделать спаун бустеров( отдельная карта)

    void Awake()
    {
        //Карта обязана быть нечетного размера и >7 для того, чтобы подходить под заданные рамки
        if (mapSize % 2 == 1 && mapSize > 6)
        {
            if (maxObstaclesAmount >= ((mapSize - 2) * (mapSize - 2) - 12 - (mapSize / 2 - 1)* (mapSize / 2 - 1)))
            {
                maxObstaclesAmount = (mapSize - 2) * (mapSize - 2) - 12 - (mapSize / 2 - 1) * (mapSize / 2 - 1);
                minObstaclesAmount = maxObstaclesAmount / 3;
            }
            if(maxBoostersAmount >= ((mapSize - 2) * (mapSize - 2) - 12 - (mapSize / 2 - 1) * (mapSize / 2 - 1)))
            {
                maxBoostersAmount = ((mapSize - 2) * (mapSize - 2) - 12 - (mapSize / 2 - 1) * (mapSize / 2 - 1));
                minBoostersAmount = maxObstaclesAmount / 3;
            }   

            //определяем количество бустеров на карте
            boostersAmount = Random.Range(minBoostersAmount, maxBoostersAmount);
            objectSize = quadPrefab.GetComponent<Renderer>().bounds.size.x;
            map = new int[mapSize, mapSize];
            MoveCamera();
            StartGeneration();
        }
        else
        {
            Debug.LogError("Map size is even or less than 7");
        }
    }

    public void StartGeneration()
    {

        //помечаем точки спауна
        map[1, 1] = FREECELL;
        map[mapSize - 2, mapSize - 2] = FREECELL;
        map[1, mapSize - 2] = FREECELL;
        map[mapSize - 2, 1] = FREECELL;
        //ограничиваем поле
        for (int i = 0; i < mapSize; i++)
        {
            map[0, i] = UNDESTRUCTABLEOBSTACLE;
            map[mapSize - 1, i] = UNDESTRUCTABLEOBSTACLE;
            map[i, 0] = UNDESTRUCTABLEOBSTACLE;
            map[i, mapSize - 1] = UNDESTRUCTABLEOBSTACLE;
        }
        //расставляем начальные препятствия
        for (int i = 1; i < mapSize - 2; i++)
        {
            for (int j = 1; j < mapSize - 2; j++)
            {
                if (i % 2 == 0 && j % 2 == 0)
                {
                    map[i, j] = UNDESTRUCTABLEOBSTACLE;
                }
            }
        }
        // генерируем случайные препятствия
        GenerateObstacles();
        //Создаем карту
        SpawnMap();
        SpawnBoosters();
    }

    //генерация препятствия на карте
    void GenerateObstacles()
    {
        obstaclesAmount = Random.Range(minObstaclesAmount, maxObstaclesAmount + 1);
        int zPos = 0, xPos = 0;
        for (int i = 0; i < obstaclesAmount; i++)
        {
            do
            {
                zPos = Random.Range(1, mapSize);
                xPos = Random.Range(1, mapSize);
            } while (CheckObstacleSpawnConditions(zPos, xPos));
            map[zPos, xPos] = DESTRUCTABLEOBSTACLE;
        }
    }


    //проверка на то, занимает ли текущая клетка позиции спауна персонажей и есть ли у игроков хотя бы одна позиция для отхода
    bool CheckObstacleSpawnConditions(int zPos, int xPos)
    {
        //если клетка свободна и не является позицией для спауна
        if (map[zPos, xPos] == FREECELL &&
            (zPos != 1 || xPos != 1) && (zPos != 1 || xPos != 2) && (zPos != 2 || xPos != 1) &&
            (zPos != mapSize - 2 || xPos != 1) && (zPos != mapSize - 3 || xPos != 1) && (zPos != mapSize - 2 || xPos != 2) &&
            (zPos != 1 || xPos != mapSize - 2) && (zPos != 1 || xPos != mapSize - 3) && (zPos != 2 || xPos != mapSize - 2) &&
            (zPos != mapSize - 2 || xPos != mapSize - 2) && (zPos != mapSize - 3 || xPos != mapSize - 2) && (zPos != mapSize - 2 || xPos != mapSize - 3))
        {
            return false;
        }else
        {
            return true;
        }
    }

    //создание объектов на карте
    void SpawnMap()
    {
        for (int zPos = 0; zPos < mapSize; zPos++)
        {
            for (int xPos = 0; xPos < mapSize; xPos++)
            {
                GameObject obj;
                switch (map[zPos, xPos])
                {
                    case FREECELL:
                        obj = SpawnObject(GetCellPositionInUnits(zPos, xPos), quadPrefab);
                        obj.transform.parent = GameObject.FindGameObjectWithTag("Map").transform;
                        break;
                    case UNDESTRUCTABLEOBSTACLE:
                        obj = SpawnObject(new Vector3(GetCellPositionInUnits(zPos, xPos).x, objectSize / 2, GetCellPositionInUnits(zPos, xPos).z), undestrObstaclePrefab);
                        obj.transform.parent = GameObject.FindGameObjectWithTag("Map").transform;
                        break;
                        //в случае разрушаемого объекта определяемся, спаунится ли бустер и спауним объект земли, 
                        //чтобы после разрушения земли не остаться с пустой клеткой
                    case DESTRUCTABLEOBSTACLE:
                        obj = SpawnObject(GetCellPositionInUnits(zPos, xPos), quadPrefab);
                        obj.transform.parent = GameObject.FindGameObjectWithTag("Map").transform;
                        obj = SpawnObject(new Vector3(GetCellPositionInUnits(zPos, xPos).x, objectSize / 2, GetCellPositionInUnits(zPos, xPos).z), destrObstaclePrefab);
                        obj.transform.parent = GameObject.FindGameObjectWithTag("Map").transform;
                        break;
                }
            }
        }
    }

    //спаун бустеров
    void SpawnBoosters()
    {
        bool[,] boostersMap = new bool[mapSize, mapSize];
        int zPos, xPos;
        do
        {
            zPos = Random.Range(1, mapSize - 1);
            xPos = Random.Range(1, mapSize - 1);
            if (map[zPos, xPos] == DESTRUCTABLEOBSTACLE && !boostersMap[zPos,xPos])
            {
                boostersMap[zPos, xPos] = true;
                int booster = Random.Range(0, boosters.Length);
                GameObject ob = Instantiate(boosters[booster], GetCellPositionInUnits(zPos, xPos), boosters[booster].transform.rotation) as GameObject;
                ob.transform.parent = GameObject.FindGameObjectWithTag("Map").transform;
                boostersAmount--;
            }
        } while (boostersAmount > 0);
        
    }

    //получить позицию в единицах юнити по индексу
    public Vector3 GetCellPositionInUnits(int z, int x)
    {
        Vector3 startPoint = new Vector3(0, 0, 0);
        float xPos = startPoint.x + x * objectSize;
        float zPos = startPoint.z - z * objectSize;
        return new Vector3(xPos, 0, zPos);
    }

    //получить позицию на карте
    public Position GetCellIndexByPosition(Vector3 pos)
    {
        int x = (int)(pos.x / objectSize);
        int z = (int)Mathf.Abs((pos.z / objectSize));
        return new Position(z, x);
    }

    //помещение камеры по центру карты
    void MoveCamera()
    {
        Camera.main.transform.position = new Vector3(GetCellPositionInUnits((mapSize - 1) / 2, (mapSize - 1) / 2).x, Camera.main.transform.position.y, GetCellPositionInUnits((mapSize - 1) / 2, (mapSize - 1) / 2).z);
        GameObject.FindGameObjectWithTag("Light").transform.position = Camera.main.transform.position;
    }

    //Спаун объекта
    public GameObject SpawnObject(Vector3 pos, GameObject gO)
    {
        return Instantiate(gO, pos, Quaternion.Euler(new Vector3(90, 0, 0))) as GameObject;
    }

    //получить размер карты
    public int GetMapSize()
    {
        return mapSize;
    }

    //получить карту
    public int[,] GetMap()
    {
        return map;
    }

    //удалить объект с карты
    public void RemoveObstacleFromMap(int i, int j)
    {
        map[i, j] = FREECELL;
    }
}
