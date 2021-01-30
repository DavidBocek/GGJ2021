using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBehaviour : MonoBehaviour
{
    public List<GameObject> patrolPoints;
    public bool poopy;
    public float fov;
    public float maxSightDistDark;
    public float maxSightDistLight;
    public float bodySenseDist;

    enum eState
    {
        IDLE,
        PATROL,
        CHASE
    }

    eState m_myState;
    NavMeshAgent m_nav;

    GameObject<PlayerController> m_player;

    Vector3 m_lastKnownPlayerPos;

    // Start is called before the first frame update
    void Start()
    {
        m_myState = eState.PATROL;
        m_nav = GetComponent<NavMeshAgent>();

        m_player = GameObject.FindGameObjectWithTag( "Player" );
    }

    // Update is called once per frame
    void Update()
    {
        Sense();
        Think();
        Act();
    }

    void Sense()
    {
        
    }

    void Think()
    {

    }

    void Act()
    {

    }
}
