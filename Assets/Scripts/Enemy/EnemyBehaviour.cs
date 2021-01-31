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
    public float timeSpentAtPatrolDestination;
    public float timeSpentAtChaseDestination;
    public float destinationStoppingDist;

    enum eState
    {
        IDLE,
        PATROL,
        CHASE
    }

    // Enemy Vars
    eState m_curState;
    eState m_lastState;
    float m_timeEnteredCurState;
    int m_targetPatrolIndex;
    bool m_newDest;
    float m_timeToSpendInIdleState;

    NavMeshAgent m_nav;
    float m_maxSightDist;
    float m_minDot;


    // Player Vars
    PlayerController m_player;
    GameObject m_playerObj;

    Vector3 m_lastKnownPlayerPos;
    float m_timePlayerLastSeen;
    bool m_canSeePlayer;

    // Start is called before the first frame update
    void Start()
    {
        SwitchState( eState.PATROL );
        m_nav = GetComponent<NavMeshAgent>();
        m_targetPatrolIndex = -1;
        m_newDest = true;

        m_playerObj = GameObject.FindGameObjectWithTag( "Player" );
        m_player = m_playerObj.GetComponent<PlayerController>();
        m_lastKnownPlayerPos = Vector3.zero;
        m_timePlayerLastSeen = 0.0f;
        m_canSeePlayer = false;

        m_minDot = Mathf.Cos( Mathf.Deg2Rad * fov );
    }

    // Update is called once per frame
    void Update()
    {
        Sense();
        Think();
        Act();

        DebugExtension.DrawPoint( m_nav.destination );
    }

    void Sense()
    {
        m_maxSightDist = m_player.IsFlashlightOn() ? maxSightDistLight : maxSightDistDark;

        if ( ! BodySense() && ( Vector3.Distance( m_playerObj.transform.position, transform.position ) > m_maxSightDist || ! PlayerIsInViewCone() ) )
        {
            m_canSeePlayer = false;
        }
        else
        {
            m_lastKnownPlayerPos = m_playerObj.transform.position;
            m_timePlayerLastSeen = Time.time;
            m_canSeePlayer = true;
        }
    }

    void Think()
    {
        switch( m_curState )
        {
            case eState.IDLE:
                if ( Time.time - m_timeEnteredCurState > m_timeToSpendInIdleState )
                {
                    m_newDest = true;
                    SwitchState( eState.PATROL );
                }
                break;
            case eState.PATROL:
                if ( m_canSeePlayer )
                {
                    SwitchState( eState.CHASE );
                }

                if ( ArrivedAtDestination() )
                {
                    SwitchState( eState.IDLE );
                }
                break;
            case eState.CHASE:
                if ( ! m_canSeePlayer && ArrivedAtDestination() )
                {
                    SwitchState( eState.IDLE );
                }
                break;
        }
    }

    void Act()
    {
        switch ( m_curState )
        {
            case eState.IDLE:
                break;
            case eState.PATROL:
                if ( m_newDest )
                {
                    m_nav.SetDestination( patrolPoints[ m_targetPatrolIndex ].transform.position );
                    m_newDest = false;
                }
                break;
            case eState.CHASE:
                if ( m_nav.destination != m_lastKnownPlayerPos )
                {
                    m_nav.SetDestination( m_lastKnownPlayerPos );
                }
                break;
        }
    }

    void OnEnterState( eState newState )
    {
        switch ( newState )
        {
            case eState.IDLE:
                if ( m_lastState == eState.PATROL )
                    m_timeToSpendInIdleState = timeSpentAtPatrolDestination;
                else if ( m_lastState == eState.CHASE )
                    m_timeToSpendInIdleState = timeSpentAtChaseDestination;
                break;
            case eState.PATROL:
                if ( m_targetPatrolIndex == -1 )
                {
                    m_targetPatrolIndex = FindNearestPatrolPointIndex();
                }
                else
                {
                    m_targetPatrolIndex = ( m_targetPatrolIndex + 1 ) % patrolPoints.Count;
                }
                break;
            case eState.CHASE:
                m_nav.SetDestination( m_lastKnownPlayerPos );
                m_targetPatrolIndex = -1;
                break;
        }
    }

    void OnExitState( eState oldState )
    {
        switch ( m_curState )
        {
            case eState.IDLE:
                break;
            case eState.PATROL:
                break;
            case eState.CHASE:
                break;
        }
    }

    void SwitchState( eState newState )
    {
        if ( newState == m_curState )
            return;

        OnExitState( m_curState );

        m_lastState = m_curState;
        m_curState = newState;
        m_timeEnteredCurState = Time.time;

        OnEnterState( m_curState );
    }

    int FindNearestPatrolPointIndex()
    {
        float nearestDist = float.MaxValue;
        int targetPatrolIndex = 0;

        for( int i = 0; i < patrolPoints.Count; i++ )
        {
            GameObject point = patrolPoints[ i ];

            float distToPoint = Vector3.Distance( transform.position, point.transform.position );

            if ( distToPoint < nearestDist )
            {
                targetPatrolIndex = i;
                nearestDist = distToPoint;
            }
        }

        return targetPatrolIndex;
    }

    bool PlayerIsInViewCone()
    {
        Vector3 vecToPlayer = Vector3.Normalize( m_playerObj.transform.position - transform.position );
        float dot = Vector3.Dot( vecToPlayer, transform.forward );

        return dot > m_minDot;
    }

    bool BodySense()
    {
        return Vector3.Distance( m_playerObj.transform.position, transform.position ) < bodySenseDist;
    }

    bool ArrivedAtDestination()
    {
        return Vector3.Distance( transform.position, m_nav.destination ) < destinationStoppingDist;
    }
}
