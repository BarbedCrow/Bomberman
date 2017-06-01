using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour, IDamagable {

    public Rigidbody playerRB;
    public int playerNum;
    MapGenerator generator;
    GameManager GM;
    Animator anim;//контроллер анимации

    //границы характеристик персонажа и орудий
    public float
        minSpeed, maxSpeed,
        minTimeBetweenAttack, maxTimeBetweenAttack,
        minAttackTime, maxAttackTime,
        minAttackPower, maxAttackPower;

    

    //характеристики
    float speed, timeBetweenAttack, attackTime, attackPower;
    float timeForNextAttack = 0;

    public Bomb bombPrefab;


    void Start ()
    {
        generator = GameObject.FindGameObjectWithTag("Generator").GetComponent<MapGenerator>();
        GM = GameObject.FindGameObjectWithTag("GM").GetComponent<GameManager>();
        anim = GetComponent<Animator>();
        playerRB = GetComponent<Rigidbody>();
        playerRB.freezeRotation = true;
        //устанавливаем характеристики
        speed = minSpeed;
        timeBetweenAttack = minTimeBetweenAttack;
        attackTime = minAttackTime;
        attackPower = minAttackPower;

	}
	
	void Update ()
    {
        MovementHandler();
        AttackHandler();
	}

    void MovementHandler()
    {
        switch (playerNum)
        {
            case 1:
                if (Input.GetKey(KeyCode.W))
                {
                    anim.SetBool("Move", true);
                    Vector3 velocity = new Vector3(0, 0, speed);
                    transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
                    playerRB.MovePosition(playerRB.position + velocity * Time.deltaTime);
                }
                else
                if (Input.GetKey(KeyCode.S))
                {
                    anim.SetBool("Move", true);
                    Vector3 velocity = new Vector3(0, 0, -speed);
                    transform.rotation = Quaternion.Euler(new Vector3(0, 180, 0));
                    playerRB.MovePosition(playerRB.position + velocity * Time.deltaTime);
                }
                else
                if (Input.GetKey(KeyCode.D))
                {
                    anim.SetBool("Move", true);
                    Vector3 velocity = new Vector3(speed, 0, 0);
                    transform.rotation = Quaternion.Euler(new Vector3(0, 90, 0));
                    playerRB.MovePosition(playerRB.position + velocity * Time.deltaTime);
                }
                else
                if (Input.GetKey(KeyCode.A))
                {
                    anim.SetBool("Move", true);
                    Vector3 velocity = new Vector3(-speed, 0, 0);
                    transform.rotation = Quaternion.Euler(new Vector3(0, -90, 0));
                    playerRB.MovePosition(playerRB.position + velocity * Time.deltaTime);
                }
                else
                {
                    anim.SetBool("Move", false);
                }
                break;
            case 2:
                if (Input.GetKey(KeyCode.UpArrow))
                {
                    anim.SetBool("Move", true);
                    Vector3 velocity = new Vector3(0, 0, speed);
                    transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
                    playerRB.MovePosition(playerRB.position + velocity * Time.deltaTime);
                }
                else
                if (Input.GetKey(KeyCode.DownArrow))
                {
                    anim.SetBool("Move", true);
                    Vector3 velocity = new Vector3(0, 0, -speed);
                    transform.rotation = Quaternion.Euler(new Vector3(0, 180, 0));
                    playerRB.MovePosition(playerRB.position + velocity * Time.deltaTime);
                }
                else
                if (Input.GetKey(KeyCode.RightArrow))
                {
                    anim.SetBool("Move", true);
                    Vector3 velocity = new Vector3(speed, 0, 0);
                    transform.rotation = Quaternion.Euler(new Vector3(0, 90, 0));
                    playerRB.MovePosition(playerRB.position + velocity * Time.deltaTime);
                }
                else
                if (Input.GetKey(KeyCode.LeftArrow))
                {
                    anim.SetBool("Move", true);
                    Vector3 velocity = new Vector3(-speed, 0, 0);
                    transform.rotation = Quaternion.Euler(new Vector3(0, -90, 0));
                    playerRB.MovePosition(playerRB.position + velocity * Time.deltaTime);
                }
                else
                {
                    anim.SetBool("Move", false);
                }
                break;
        }
        
        
    }

    void AttackHandler()
    {
        switch (playerNum)
        {
            case 1:
                if (Input.GetKey(KeyCode.Space) && Time.time > timeForNextAttack)
                {
                    anim.SetBool("Attack", true);
                    Vector3 pos = new Vector3(transform.position.x, 0.5f, transform.position.z);
                    Bomb bomb = Instantiate(bombPrefab, pos, transform.rotation) as Bomb;
                    timeForNextAttack = Time.time + timeBetweenAttack;
                    bomb.SetLifeTime(attackTime);
                    bomb.SetMoveDistance(attackPower);
                }
                else
                {
                    anim.SetBool("Attack", false);
                }
                break;
            case 2:
                if (Input.GetKey(KeyCode.Keypad0) && Time.time > timeForNextAttack)
                {
                    anim.SetBool("Attack", true);
                    Vector3 pos = new Vector3(transform.position.x, 0.5f, transform.position.z);
                    Bomb bomb = Instantiate(bombPrefab, pos, transform.rotation) as Bomb;
                    timeForNextAttack = Time.time + timeBetweenAttack;
                    bomb.SetLifeTime(attackTime);
                    bomb.SetMoveDistance(attackPower);
                }
                else
                {
                    anim.SetBool("Attack", false);
                }
                break;
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
