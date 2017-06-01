using UnityEngine;
using System.Collections;

public class Bomb : MonoBehaviour {

    float lifeTime, moveDistance;

    float skinWidth = 0.1f;
    float endTime;

    Ray[] rays;

    public LayerMask collisionMask;//объекты, с которыми обрабатываются столкновения
    public GameObject[] fire;//анимации огней

    MapGenerator generator;

    void Start ()
    {
        generator = GameObject.FindGameObjectWithTag("Generator").GetComponent<MapGenerator>();
        //отключаем эффекты взрыва
        for (int i = 0; i < fire.Length; i++)
        {
            fire[i].transform.parent = null;
            fire[i].transform.localScale = new Vector3(fire[i].transform.localScale.x, fire[i].transform.localScale.y, fire[i].transform.localScale.z * moveDistance * 2);
            fire[i].GetComponent<ParticleSystem>().Stop();
        }
        //создаем массив с направлениями для лучей
        rays = new Ray[4];
        rays[0] = new Ray(transform.position, transform.forward);
        rays[1] = new Ray(transform.position, -transform.forward);
        rays[2] = new Ray(transform.position, transform.right);
        rays[3] = new Ray(transform.position, -transform.right);
        endTime = Time.time + lifeTime;
        
        CheckCollisions(moveDistance);
        Destroy(gameObject, lifeTime);
    }
	
	void Update ()
    {
        if (endTime - Time.time < lifeTime / 10)
        {
            //включаем эффекты
            for (int i = 0; i < fire.Length; i++)
            {
                if(fire[i] != null)
                {
                    fire[i].GetComponent<ParticleSystem>().Play();
                    Destroy(fire[i], lifeTime);
                } 
            }
            CheckCollisions(moveDistance);
            endTime *= 100;
        }
	}

    //установка времени жизни бомбы
    public void SetLifeTime(float time)
    {
        lifeTime = time;
    }

    //установка дальности действия бомбы
    public void SetMoveDistance(float _moveDistance)
    {
        moveDistance = _moveDistance;
    }

    //Проверка на наличие столкновений
    void CheckCollisions(float moveDistance)
    {
        RaycastHit hit;
        for(int i = 0; i < 4; i++)
        {
            //если луч натыкается на объект, то передаем объект обработчику
            if (Physics.Raycast(rays[i], out hit, moveDistance, collisionMask, QueryTriggerInteraction.Collide))
            {
                OnHitObject(hit.collider, hit.point);
            }
        }
    }

    //обработка столкновений
    void OnHitObject(Collider col, Vector3 hitPoint)
    {
        IDamagable obj = col.GetComponent<IDamagable>();
        //если объект имеет данный интерфейс, то его необходимо уничтожить
        if (obj != null)
        {
            obj.TakeDamage(endTime - Time.time);
            //если объект имеет метку, то удаляем его с карты
            if (col.tag == "Destructable")
            {
                generator.RemoveObstacleFromMap(generator.GetCellIndexByPosition(col.transform.position).z, generator.GetCellIndexByPosition(col.transform.position).x);
            }
        }
    }

}
