using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyController : MonoBehaviour, IDamagable {

    MapGenerator generator;
    Animator anim;
    Rigidbody rB;
    GameManager GM;

    //границы характеристик персонажа и орудий
    public float
        minSpeed, maxSpeed,
        minTimeBetweenAttack, maxTimeBetweenAttack,
        minAttackTime, maxAttackTime,
        minAttackPower, maxAttackPower;
    public float distanceForCheckEnemy = 3f;
    public Bomb bombPrefab;
    public LayerMask bombCollisionMask;
    public LayerMask playersMask;

    //характеристики
    float speed, timeBetweenAttack, attackTime, attackPower;

    float timeBetweenPathUpdates = 1f, timeForNextPathUpdate = 0, timeForNextAttack = 0;

    //все переменные, необходимые для нахождения пути
    //структура для хранения данных клетки для а*
    struct Cell
    {
        public int h, g, f;
        public MapGenerator.Position pos,parentPos;

        public Cell(int _h, int _g, MapGenerator.Position _pos, MapGenerator.Position _parentPos)
        {
            h = _h;
            g = _g;
            f = h + g;
            pos = _pos;
            parentPos = _parentPos;
        }
    }
    GameObject[] enemies;
    Stack<MapGenerator.Position> path;
    MapGenerator.Position startPos, endPos;
    Cell startCell, currCell;
    List<Cell> opened;
    List<MapGenerator.Position> closed;
    Cell[,] map;
    public int[,] startMap;
    int destructableObstacleModifyer = 5;

    bool followPath = true;

    void Start()
    {
        //инициализируем компоненты
        generator = GameObject.FindGameObjectWithTag("Generator").GetComponent<MapGenerator>();
        GM = GameObject.FindGameObjectWithTag("GM").GetComponent<GameManager>();
        anim = GetComponent<Animator>();
        rB = GetComponent<Rigidbody>();
        rB.freezeRotation = true;
        //устанавливаем характеристики
        speed = minSpeed;
        timeBetweenAttack = minTimeBetweenAttack;
        attackTime = minAttackTime;
        attackPower = minAttackPower;
    }

    void Update()
    {
        anim.SetBool("Move", false);
        //поиск пути происходит фиксированное кол-во раз в секунду
        if (Time.time > timeForNextPathUpdate)
        {
            //CheckEnemyBomb();
            timeForNextPathUpdate = Time.time + timeBetweenPathUpdates;
            //endPos = FindEnemy();
            //если функция возвращает (0,0), то на карте остались только мы
            //if (endPos == new MapGenerator.Position(0, 0))
                //return;
            FindPath();
            StopCoroutine("FollowPath");
            StartCoroutine("FollowPath");
        }
    }

    bool AttackPlayer()
    {
        //Debug.Log("CHECKPLAYER");
        if(!CheckObject(generator.GetCellIndexByPosition(transform.position), distanceForCheckEnemy, playersMask, "Entity"))
        {
            //Debug.Log("FOUNDPLAYER");
            if (Time.time > timeForNextAttack)
            {
                Bomb bomb = Instantiate(bombPrefab, transform.position, transform.rotation) as Bomb;
                timeForNextAttack = Time.time + timeBetweenAttack;
                timeForNextPathUpdate = Time.time + timeBetweenAttack;
                bomb.SetLifeTime(attackTime);
                bomb.SetMoveDistance(attackPower);
            }
            StopCoroutine("FollowPath");

            FindNearestSafeCell();
            StartCoroutine("FollowPath");
            return true;
        }
        return false;
    }

    IEnumerator FollowPath()
    {
        int counter = (generator.GetMapSize() - 2) * (generator.GetMapSize() - 2);
        //Debug.Log("PATH:");
        if (path.Count != 0 && followPath)
        {
            MapGenerator.Position currWaypoint = path.Pop();
            while (true)
            {
                if (transform.position == generator.GetCellPositionInUnits(currWaypoint.z, currWaypoint.x))
                {
                    counter--;
                    if (path.Count <= 0 || counter <= 0)
                    {
                        yield break;
                    }
                    currWaypoint = path.Pop();
                    //Debug.Log("PP: " + currWaypoint.z + " " + currWaypoint.x);
                    CheckEnemyBomb();
                    if (startMap[currWaypoint.z, currWaypoint.x] == MapGenerator.DESTRUCTABLEOBSTACLE || AttackPlayer())
                    {
                        if(Time.time > timeForNextAttack)
                        {
                            Bomb bomb = Instantiate(bombPrefab, transform.position, transform.rotation) as Bomb;
                            timeForNextAttack = Time.time + timeBetweenAttack;
                            timeForNextPathUpdate = Time.time + timeBetweenAttack;
                            bomb.SetLifeTime(attackTime);
                            bomb.SetMoveDistance(attackPower);
                        }
                        StopCoroutine("FollowPath");
                        
                        FindNearestSafeCell();
                        StartCoroutine("FollowPath");
                    }
                    
                    if (generator.GetCellPositionInUnits(currWaypoint.z, currWaypoint.x).z < transform.position.z)
                    {
                        transform.rotation = Quaternion.Euler(new Vector3(0, 180, 0));
                    }
                    else if (generator.GetCellPositionInUnits(currWaypoint.z, currWaypoint.x).z > transform.position.z)
                    {
                        transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
                    }
                    else if (generator.GetCellPositionInUnits(currWaypoint.z, currWaypoint.x).x < transform.position.x)
                    {
                        transform.rotation = Quaternion.Euler(new Vector3(0, -90, 0));
                    }
                    else if (generator.GetCellPositionInUnits(currWaypoint.z, currWaypoint.x).x > transform.position.x)
                    {
                        transform.rotation = Quaternion.Euler(new Vector3(0, 90, 0));
                    }
                }
                anim.SetBool("Move", true);
                transform.position = Vector3.MoveTowards(transform.position, generator.GetCellPositionInUnits(currWaypoint.z, currWaypoint.x), speed * Time.deltaTime);
                yield return null;
            }//while true
        }else
        {
            anim.SetBool("Move", false);
        }

    }

    //нахождение ближайшей безопасной клетки, в которой персонажа не заденет бомба
    void FindNearestSafeCell()
    {
        startMap = generator.GetMap();
        MapGenerator.Position startPos = generator.GetCellIndexByPosition(transform.position);
        MapGenerator.Position currPos = startPos;
        List<MapGenerator.Position> closed = new List<MapGenerator.Position>();
        closed.Add(currPos);
        //расчитываем позиции соседей
        MapGenerator.Position[] neighbs = new MapGenerator.Position[4];
        
        Stack<MapGenerator.Position> newPath = new Stack<MapGenerator.Position>();
        newPath.Push(currPos);
        int counter = (generator.GetMapSize() - 2) * (generator.GetMapSize() - 2);
        while (true)
        {
            counter--;
            if (counter <= 0)
                return;
            neighbs[0] = new MapGenerator.Position(currPos.z - 1, currPos.x);
            neighbs[1] = new MapGenerator.Position(currPos.z + 1, currPos.x);
            neighbs[2] = new MapGenerator.Position(currPos.z, currPos.x - 1);
            neighbs[3] = new MapGenerator.Position(currPos.z, currPos.x + 1);

            if (neighbs[0].z > 0 && startMap[neighbs[0].z, neighbs[0].x] == MapGenerator.FREECELL && !closed.Contains(neighbs[0]))
            {
                currPos = neighbs[0];
                newPath.Push(currPos);
            }
            else if (neighbs[1].z < generator.GetMapSize() - 1 && startMap[neighbs[1].z, neighbs[1].x] == MapGenerator.FREECELL && !closed.Contains(neighbs[1]))
            {
                currPos = neighbs[1];
                newPath.Push(currPos);
            }
            else if (neighbs[2].x > 0 && startMap[neighbs[2].z, neighbs[2].x] == MapGenerator.FREECELL && !closed.Contains(neighbs[2]))
            {
                currPos = neighbs[2];
                newPath.Push(currPos);
            }
            else if (neighbs[3].z < generator.GetMapSize() - 1 && startMap[neighbs[3].z, neighbs[3].x] == MapGenerator.FREECELL && !closed.Contains(neighbs[3]))
            {
                currPos = neighbs[3];
                newPath.Push(currPos);
            }

            if (CheckObject(currPos, attackPower, bombCollisionMask, "Bomb"))
            {
                path = new Stack<MapGenerator.Position>();
                for (int i = 0; i < newPath.Count; i++)
                {
                    path.Push(newPath.Pop());
                }
                return;
            }
        }
        
    }

    //Проверяем, находится ли в области видимости персонажа бомба
    bool CheckObject(MapGenerator.Position pos, float distance, LayerMask collisionMask, string tag)
    {
        //Debug.Log("Check" + pos.z + " " + pos.x);
        Ray[] rays = new Ray[4];
        rays[0] = new Ray(new Vector3(generator.GetCellPositionInUnits(pos.z, pos.x).x, .5f, generator.GetCellPositionInUnits(pos.z, pos.x).z), transform.forward * distance);
        rays[1] = new Ray(new Vector3(generator.GetCellPositionInUnits(pos.z, pos.x).x, .5f, generator.GetCellPositionInUnits(pos.z, pos.x).z), -transform.forward * distance);
        rays[2] = new Ray(new Vector3(generator.GetCellPositionInUnits(pos.z, pos.x).x, .5f, generator.GetCellPositionInUnits(pos.z, pos.x).z), transform.right * distance);
        rays[3] = new Ray(new Vector3(generator.GetCellPositionInUnits(pos.z, pos.x).x, .5f, generator.GetCellPositionInUnits(pos.z, pos.x).z), -transform.right * distance);
        RaycastHit hit;

        for (int i = 0; i < 4; i++)
        {
            if (Physics.Raycast(rays[i], out hit, distance, collisionMask, QueryTriggerInteraction.Collide))
            {
                    if(hit.collider.tag == tag)
                        return false;
            }
        }
        
        return true;
    }

    bool CheckEnemyBomb()
    {
        if (!CheckObject(generator.GetCellIndexByPosition(transform.position), distanceForCheckEnemy, bombCollisionMask, "Bomb"))
        {
            timeForNextPathUpdate = Time.time + timeBetweenAttack;
            StopCoroutine("FollowPath");
            FindNearestSafeCell();
            StartCoroutine("FollowPath");
            return true;
        }
        return false;
    }

    MapGenerator.Position FindEnemy()
    {
        //находим всех противников
        enemies = GameObject.FindGameObjectsWithTag("Entity");
        if (enemies.Length == 1)
            return new MapGenerator.Position(0, 0);
        int choice = 0;
        //исключаем возможность выбора самого себя
        do
        {
            choice = Random.Range(0, enemies.Length);
        } while (enemies[choice].transform.position == transform.position);

        return generator.GetCellIndexByPosition(enemies[choice].transform.position);
    }

    MapGenerator.Position FindRandomTarget()
    {
        MapGenerator.Position pos;
        do
        {
            pos.z = Random.Range(1, generator.GetMapSize() - 1);
            pos.x = Random.Range(1, generator.GetMapSize() - 1);
        } while (startMap[pos.z, pos.x] != MapGenerator.FREECELL);
        return pos;
    }

    void FindPath()
    {
        followPath = true;
        //Debug.Log("START_FIND");
        //получаем обновленную игровую карту
        startMap = generator.GetMap();
        endPos = FindRandomTarget();
        //создаем карту для текущего алгоритма
        map = new Cell[generator.GetMapSize(), generator.GetMapSize()];
        //обновляем списки
        opened = new List<Cell>();
        closed = new List<MapGenerator.Position>();
        //задаем начальную позицию, т.е. позицию нашего персонажа
        startPos = generator.GetCellIndexByPosition(transform.position);
        //инициализируем начальные клетки и помещаем их на текущую карту
        startCell = new Cell(CalculateDistance('h', startCell.pos), 0, startPos, startPos);
        map[startPos.z, startPos.x] = startCell;
        currCell = startCell;
        int counter = 0;
        while (true)
        {
            counter++;
            if(counter > (generator.GetMapSize()-2)*(generator.GetMapSize()-2))
            {
                followPath = false;
                return;
            }
            //Debug.Log("TRUE");
            closed.Add(currCell.pos);
            GetBestNeighbour(currCell);
            if (opened.Count == 0)
                return;
            currCell = opened[0];
            //Debug.Log("CP: " + currCell.pos.z + " " + currCell.pos.x);
            //Debug.Log("EP: " + endPos.z + " " + endPos.x);
            opened.RemoveAt(0);
            if(currCell.pos == endPos)
                break;
        }
        //Debug.Log("FOUND");
        path = new Stack<MapGenerator.Position>();
        //path.Push(currCell.pos);
        do
        {
            path.Push(currCell.parentPos);
            currCell = map[currCell.parentPos.z, currCell.parentPos.x];
        } while (currCell.pos != startCell.pos);
        //path.Pop();
    }

    void GetBestNeighbour(Cell cell)
    {
        //расчитываем позиции соседей
        MapGenerator.Position[] neighbs = new MapGenerator.Position[4];
        neighbs[0] = new MapGenerator.Position(cell.pos.z - 1, cell.pos.x);
        neighbs[1] = new MapGenerator.Position(cell.pos.z + 1, cell.pos.x);
        neighbs[2] = new MapGenerator.Position(cell.pos.z, cell.pos.x - 1);
        neighbs[3] = new MapGenerator.Position(cell.pos.z, cell.pos.x + 1);
        //если клетка не выходит за границы полей, не является неразрушимым объектом и уже не была включена в просмотренные
        //она нам подходит        
        if (neighbs[0].z > 0 && startMap[neighbs[0].z, neighbs[0].x] != MapGenerator.UNDESTRUCTABLEOBSTACLE && !closed.Contains(neighbs[0]))
        {
            //Если клетка уже содержится в открытом списке, то проверяем, не будет ли путь из текущей клетки короче
            if (opened.Contains(map[neighbs[0].z, neighbs[0].x]))
            {
                switch (startMap[cell.pos.z, cell.pos.x])
                {
                    case MapGenerator.DESTRUCTABLEOBSTACLE:
                        if (cell.g + 5 < map[neighbs[0].z, neighbs[0].x].g)
                        {
                            map[neighbs[0].z, neighbs[0].x].g = cell.g + 5;
                            map[neighbs[0].z, neighbs[0].x].parentPos = cell.pos;
                        }
                        break;
                    case MapGenerator.FREECELL:
                        if (cell.g + 1 < map[neighbs[0].z, neighbs[0].x].g)
                        {
                            map[neighbs[0].z, neighbs[0].x].g = cell.g + 1;
                            map[neighbs[0].z, neighbs[0].x].parentPos = cell.pos;
                        }
                        break;
                }//switch
            }else
            {
                Cell newCell = new Cell(CalculateDistance('h', neighbs[0]), CalculateDistance('g', neighbs[0]), neighbs[0], cell.pos);
                map[newCell.pos.z, newCell.pos.x] = newCell;
                opened.Add(newCell);
            }
        }
        if (neighbs[1].z < generator.GetMapSize() - 1 && startMap[neighbs[1].z, neighbs[1].x] != MapGenerator.UNDESTRUCTABLEOBSTACLE && !closed.Contains(neighbs[1]))
        {
            //Если клетка уже содержится в открытом списке, то проверяем, не будет ли путь из текущей клетки короче
            if (opened.Contains(map[neighbs[1].z, neighbs[1].x]))
            {
                switch (startMap[cell.pos.z, cell.pos.x])
                {
                    case MapGenerator.DESTRUCTABLEOBSTACLE:
                        if (cell.g + 5 < map[neighbs[1].z, neighbs[1].x].g)
                        {
                            map[neighbs[1].z, neighbs[1].x].g = cell.g + 5;
                            map[neighbs[1].z, neighbs[1].x].parentPos = cell.pos;
                        }
                        break;
                    case MapGenerator.FREECELL:
                        if (cell.g + 1 < map[neighbs[1].z, neighbs[1].x].g)
                        {
                            map[neighbs[1].z, neighbs[1].x].g = cell.g + 1;
                            map[neighbs[1].z, neighbs[1].x].parentPos = cell.pos;
                        }
                        break;
                }//switch
            }
            else
            {
                Cell newCell = new Cell(CalculateDistance('h', neighbs[1]), CalculateDistance('g', neighbs[1]), neighbs[1], cell.pos);
                map[newCell.pos.z, newCell.pos.x] = newCell;
                opened.Add(newCell);
            }
        }
        if (neighbs[2].x > 0 && startMap[neighbs[2].z, neighbs[2].x] != MapGenerator.UNDESTRUCTABLEOBSTACLE && !closed.Contains(neighbs[2]))
        {
            //Если клетка уже содержится в открытом списке, то проверяем, не будет ли путь из текущей клетки короче
            if (opened.Contains(map[neighbs[2].z, neighbs[2].x]))
            {
                switch (startMap[cell.pos.z, cell.pos.x])
                {
                    case MapGenerator.DESTRUCTABLEOBSTACLE:
                        if (cell.g + 5 < map[neighbs[2].z, neighbs[2].x].g)
                        {
                            map[neighbs[2].z, neighbs[2].x].g = cell.g + 5;
                            map[neighbs[2].z, neighbs[2].x].parentPos = cell.pos;
                        }
                        break;
                    case MapGenerator.FREECELL:
                        if (cell.g + 1 < map[neighbs[2].z, neighbs[2].x].g)
                        {
                            map[neighbs[2].z, neighbs[2].x].g = cell.g + 1;
                            map[neighbs[2].z, neighbs[2].x].parentPos = cell.pos;
                        }
                        break;
                }//switch
            }
            else
            {
                Cell newCell = new Cell(CalculateDistance('h', neighbs[2]), CalculateDistance('g', neighbs[2]), neighbs[2], cell.pos);
                map[newCell.pos.z, newCell.pos.x] = newCell;
                opened.Add(newCell);
            }
        }
        if (neighbs[3].x < generator.GetMapSize() - 1 && startMap[neighbs[3].z, neighbs[3].x] != MapGenerator.UNDESTRUCTABLEOBSTACLE && !closed.Contains(neighbs[3]))
        {
            //Если клетка уже содержится в открытом списке, то проверяем, не будет ли путь из текущей клетки короче
            if (opened.Contains(map[neighbs[3].z, neighbs[3].x]))
            {
                switch (startMap[cell.pos.z, cell.pos.x])
                {
                    case MapGenerator.DESTRUCTABLEOBSTACLE:
                        if (cell.g + 5 < map[neighbs[3].z, neighbs[3].x].g)
                        {
                            map[neighbs[3].z, neighbs[3].x].g = cell.g + 5;
                            map[neighbs[3].z, neighbs[3].x].parentPos = cell.pos;
                        }
                        break;
                    case MapGenerator.FREECELL:
                        if (cell.g + 1 < map[neighbs[3].z, neighbs[3].x].g)
                        {
                            map[neighbs[3].z, neighbs[3].x].g = cell.g + 1;
                            map[neighbs[3].z, neighbs[3].x].parentPos = cell.pos;
                        }
                        break;
                }//switch
            }
            else
            {
                Cell newCell = new Cell(CalculateDistance('h', neighbs[3]), CalculateDistance('g', neighbs[3]), neighbs[3], cell.pos);
                map[newCell.pos.z, newCell.pos.x] = newCell;
                opened.Add(newCell);
            }
        }

        //сортируем список
        for(int i = 0; i < opened.Count; i++)
        {
            for(int j = 0; j < opened.Count - 1 - i; j++)
            {
                if(opened[j].h > opened[j + 1].h)
                {
                    Cell tmp = opened[j];
                    opened[j] = opened[j + 1];
                    opened[j + 1] = tmp;
                }
            }
        }
    }

    int CalculateDistance(char ch, MapGenerator.Position pos)
    {
        int distance = 0;
        switch (ch)
        {
            case 'g':
                distance = Mathf.Abs(pos.x - startPos.x) + Mathf.Abs(pos.z - startPos.z);
                return distance;
            case 'h':
                switch (startMap[pos.z, pos.x])
                {
                    case MapGenerator.FREECELL:
                        distance = Mathf.Abs(pos.x - endPos.x) + Mathf.Abs(pos.z - endPos.z);
                        break;
                    case MapGenerator.DESTRUCTABLEOBSTACLE:
                        distance = Mathf.Abs(pos.x - endPos.x) + Mathf.Abs(pos.z - endPos.z) + destructableObstacleModifyer;
                        break;
                }
                return distance;
            default:
                return -1;
        }
    }

    public void TakeDamage(float time)
    {
        Destroy(gameObject, time);
        GM.DecreasePlayersAmount();
    }

    //Повышение характеристик
    void OnTriggerEnter(Collider col)
    {
        switch (col.tag)
        {
            case "Speed":
                if (speed + 1 <= maxSpeed)
                    speed++;
                Destroy(col.gameObject);
                break;
            case "Power":
                if (attackPower + 1 <= maxAttackPower)
                    attackPower++;
                Destroy(col.gameObject);
                break;
            case "TimerForAttack":
                if (attackTime - .5f >= maxAttackTime)
                    attackTime--;
                Destroy(col.gameObject);
                break;
            case "TimerBetweenAttacks":
                if (timeBetweenAttack - 1 >= maxAttackTime)
                    timeBetweenAttack--;
                Destroy(col.gameObject);
                break;
        }
    }
}
