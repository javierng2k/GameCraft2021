﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashEnemyController : MonoBehaviour
{
    public DASH_STATES m_currState;
    public enum DASH_STATES
    {
        STATE_NORMAL = 0,
        STATE_UNMORPHING,
        STATE_MORPHING,
        STATE_MORPHED_IDLE,
        STATE_MORPHED_CHASE,
        STATE_MORPHED_ATTACKING,
        STATE_MORPHED_DEATH,
    }

    public GameObject m_normalGO;
    public GameObject m_morphedGO;
    public GameObject m_attackGO;
    public GameObject m_smokeGO;

    // Need to play test and change values accordingly
    static int HP = 3;
    static int DMG = 1;
    public int m_hp;
    public int m_dmg;

    // When attacktimer reaches attacktime, an attack will be made
    // Need to play test and change values accordingly
    static float ATT_TIME = 2;
    public float m_attackTimer;
    public float m_attackTime;

    [Header("Smoke things")]
    public float m_TimeToSwap;
    public float m_smokeTime;
    float m_timer;
    bool m_smokePlayed;
    bool m_isMorphing = false;

    [Header("Movement variables")]
    public Transform m_groundDetection;
    public float m_speed;
    public float m_dashSpeed = 200.0f;
    public float m_dashCooldown = 1.0f;
    private float m_currDashTime = 0.0f;
    private float m_prevGravity = 0.5f;
    private float m_currDashCooldown = 0.0f;
    public float m_dashTime = 2.0f;

    [Tooltip("For raycasting")]
    public float m_distance;
    [Tooltip("How long they patrol and idle for")]
    public float m_patrolTime;
    [Header("Player Detection")]
    public float m_detectionRange;
    public float m_attackRange;


    [System.NonSerialized] public bool m_movingRight = true;
    bool m_moving;
    bool m_attacking;
    bool m_isDashing = false;
    float m_patrolTimer;
    Rigidbody2D m_rigidBody;


    public void SetMorphing(bool _morphing)
    {
        if (_morphing)
        {
            m_currState = DASH_STATES.STATE_MORPHING;
        }
        else
        {
            m_currState = DASH_STATES.STATE_UNMORPHING;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        m_hp = HP;
        m_dmg = DMG;

        m_attackTimer = 0;
        m_attackTime = ATT_TIME;

        m_normalGO.SetActive(true);
        m_morphedGO.SetActive(false);
        m_attackGO.SetActive(false);
        m_currState = DASH_STATES.STATE_NORMAL;
        m_rigidBody = GetComponent<Rigidbody2D>();
    }


    // Update is called once per frame
    void Update()
    {
        if (PlayerData.Instance.m_isInsane)
        {
            // They need to swap
            if (m_isMorphing == false)
            {
                m_isMorphing = true;
                m_smokePlayed = false;
                m_timer = 0.0f;
                SetMorphing(true);
            }
        }
        else
        {
            if (m_isMorphing)
            {
                m_isMorphing = false;
                m_smokePlayed = false;
                m_timer = 0.0f;
                SetMorphing(false);
            }
        }

        switch (m_currState)
        {
            case DASH_STATES.STATE_NORMAL:
                break;
            case DASH_STATES.STATE_UNMORPHING:
                {
                    m_timer += Time.deltaTime;

                    if (m_timer >= m_smokeTime)
                    {
                        if (!m_smokePlayed)
                        {
                            m_smokePlayed = true;
                            m_smokeGO.GetComponent<ParticleSystem>().Play();
                        }
                    }

                    if (m_timer >= m_TimeToSwap)
                    {
                        StartUnmorphing();
                        m_morphedGO.GetComponent<Animator>().SetBool("Morph", false);
                        m_timer = 0.0f;
                    }

                }
                break;
            case DASH_STATES.STATE_MORPHING:
                {
                    //m_smokeGO.GetComponent<ParticleSystem>().Play();
                    m_timer += Time.deltaTime;

                    if (m_timer >= m_smokeTime)
                    {
                        if (!m_smokePlayed)
                        {
                            m_smokePlayed = true;
                            m_smokeGO.GetComponent<ParticleSystem>().Play();
                        }
                    }

                    if (m_timer >= m_TimeToSwap)
                    {
                        StartMorphing();
                        m_morphedGO.GetComponent<Animator>().SetBool("Morph", true);
                        m_timer = 0.0f;
                    }

                }
                break;
            case DASH_STATES.STATE_MORPHED_IDLE:
                {
                    //m_attackTimer += Time.deltaTime;

                    //if (m_attackTimer >= m_attackTime)
                    //{
                    //    m_attackTimer = 0;
                    //    m_currState = MELEE_STATES.STATE_MORPHED_ATTACKING;
                    //    m_morphedGO.GetComponent<Animator>().SetBool("Attack", true); 
                    //}

                    m_patrolTimer += Time.deltaTime;

                    if (m_patrolTimer >= m_patrolTime)
                    {
                        m_patrolTimer = 0;
                        // Toggle the moving bool
                        m_moving = !m_moving;
                    }
                    CheckForPlayer();
                    IdleMovement();
                }
                break;
            case DASH_STATES.STATE_MORPHED_CHASE:
                {
                    CheckForPlayer();
                    ChasingMovement();
                }
                break;
            case DASH_STATES.STATE_MORPHED_ATTACKING:

                // if it isnt attacking
                if (m_isDashing == false)
                {
                    // If it can dash
                    if (Time.time - m_currDashCooldown > m_dashCooldown)
                    {
                        m_morphedGO.GetComponent<Animator>().SetTrigger("AttackTrigger");
                        //Attack();
                        //m_attacking = true;
                    }
                    else
                    {
                        // if cant dash yet, continue to chase
                        m_currState = DASH_STATES.STATE_MORPHED_CHASE;
                    }
                }

                if (m_isDashing)
                {
                    Debug.Log("Is dashing");
                    float currTime = Time.time;
                    m_isDashing = currTime - m_currDashTime < m_dashTime;
                    if (!m_isDashing)
                    {
                        m_rigidBody.gravityScale = m_prevGravity;
                        m_currState = DASH_STATES.STATE_MORPHED_IDLE;
                       //m_attacking = false;
                        //m_attackTimer = 0;
                        m_currDashCooldown = Time.time;
                    }
                }

                break;
            default:
                break;
        }

       // UpdateAnimations();
    }

    private void FixedUpdate()
    {
        if (m_isDashing)
        {
            Vector2 m_faceDir;

            if (m_movingRight)
            {
                m_faceDir = Vector2.right;
            }
            else
            {
                m_faceDir = Vector2.left;
            }
            Debug.Log(m_faceDir.x);

            m_rigidBody.velocity = new Vector2(m_faceDir.x * m_dashSpeed * Time.fixedDeltaTime, 0.0f);
            return;
        }
    }

    public void UpdateAnimations()
    {
        //m_morphedGO.GetComponent<Animator>().SetBool("Attack", m_attacking);
        //m_morphedGO.GetComponent<Animator>().SetBool("isMoving", m_moving);
    }

    public void Attack()
    {
        // Do the fancy dash shit here

        m_isDashing = true;
        m_rigidBody.gravityScale = 0.0f;
        m_currDashTime = Time.time;

        //m_attackGO.transform.position = transform.position;
        //m_attackGO.SetActive(true);
        //// Debug.Log("In controller script " + m_movingRight);
        //m_attackGO.GetComponent<MeleeEnemyAttack>().startAttack = true;
        //m_attackGO.GetComponent<MeleeEnemyAttack>().m_movingRight = m_movingRight;
        //m_currState = MELEE_STATES.STATE_MORPHED_CHASE;
    }

    public void EndAttack()
    {
        //m_currState = DASH_STATES.STATE_MORPHED_CHASE;
        //m_attacking = false;
        //// m_morphedGO.GetComponent<Animator>().SetBool("Attack", false);
        //m_attackTimer = 0;
    }
    public void CheckForPlayer()
    {
        //Debug.Log("Testing");
        Vector3 centerPosition = transform.position;
        RaycastHit2D hitInfoRight = Physics2D.Raycast(centerPosition, Vector2.right, m_detectionRange);
        RaycastHit2D hitInfoLeft = Physics2D.Raycast(centerPosition, Vector2.left, m_detectionRange);
        Debug.DrawRay(centerPosition, (Vector2.right * m_detectionRange), Color.green);
        Debug.DrawRay(centerPosition, (Vector2.left * m_detectionRange), Color.green);


        if (hitInfoRight.collider == true)
        {
            //Debug.Log(hitInfoRight.collider.gameObject.name);

            if (hitInfoRight.collider.gameObject.tag == "Player")
            {
                m_currState = DASH_STATES.STATE_MORPHED_CHASE;
                m_patrolTimer = 0.0f;
                RotateRight();
            }
        }
        else if (hitInfoLeft.collider == true)
        {
            // Debug.Log(hitInfoLeft.collider.gameObject.name);

            if (hitInfoLeft.collider.gameObject.tag == "Player")
            {
                m_currState = DASH_STATES.STATE_MORPHED_CHASE;
                m_patrolTimer = 0.0f;
                RotateLeft();
            }
        }
    }

    public void IdleMovement()
    {
        if (m_moving)
        {
            transform.Translate(Vector2.right * m_speed * Time.deltaTime);

            // Get the bottom and right hit info 
            // Checks if theres a wall infront or a drop below
            RaycastHit2D hitInfoDown = Physics2D.Raycast(m_groundDetection.position, Vector2.down, m_distance);
            //Debug.DrawRay(m_groundDetection.position, (Vector2.down * m_distance), Color.green);

            RaycastHit2D hitInfoForward;
            if (m_movingRight)
            {
                hitInfoForward = Physics2D.Raycast(m_groundDetection.position, Vector2.right, m_distance);
                // Debug.DrawRay(m_groundDetection.position, (Vector2.right * m_distance), Color.green);

            }
            else
            {
                hitInfoForward = Physics2D.Raycast(m_groundDetection.position, Vector2.left, m_distance);

                // Debug.DrawRay(m_groundDetection.position, (Vector2.left * m_distance), Color.green);
            }

            // Down collider did not hit anything
            // If forward collider hit something, it means there is smth infront of it
            if (hitInfoForward.collider == true && hitInfoForward.collider.gameObject.tag == "Map")
            {
                if (m_movingRight)
                {
                    RotateLeft();
                }
                else
                {
                    RotateRight();
                }
            }
            else if (hitInfoDown.collider == false)
            {
                if (m_movingRight)
                {
                    RotateLeft();
                }
                else
                {
                    RotateRight();
                }
            }
        }
    }

    public void ChasingMovement()
    {
        Vector3 centerPosition = transform.position - new Vector3(0, 0.5f, 0);
        RaycastHit2D hitInfoRight = Physics2D.Raycast(centerPosition, Vector2.right, m_attackRange);
        RaycastHit2D hitInfoLeft = Physics2D.Raycast(centerPosition, Vector2.left, m_attackRange);
        Debug.DrawRay(centerPosition, (Vector2.right * m_attackRange), Color.red);
        Debug.DrawRay(centerPosition, (Vector2.left * m_attackRange), Color.red);

        if (hitInfoRight.collider == true && hitInfoRight.collider.gameObject.tag == "Player")
        {
            // if they are facing left
            if (m_movingRight == false)
            {
                RotateRight();
            }

            m_currState = DASH_STATES.STATE_MORPHED_ATTACKING;
            //m_attacking = true;
            // m_morphedGO.GetComponent<Animator>().SetBool("Attack", true);
            m_patrolTimer = 0.0f;
            m_moving = false;
        }
        else if (hitInfoLeft.collider == true && hitInfoLeft.collider.gameObject.tag == "Player")
        {
            // If they facing left
            if (m_movingRight == true)
            {
                RotateLeft();
            }

            m_currState = DASH_STATES.STATE_MORPHED_ATTACKING;
            // m_morphedGO.GetComponent<Animator>().SetBool("Attack", true);
           // m_attacking = true;
            m_patrolTimer = 0.0f;
            m_moving = false;
        }
        else
        {
            // no player in range
            m_moving = true;
        }

        // else just move towards the player
        if (m_moving)
        {
            transform.Translate(Vector2.right * m_speed * Time.deltaTime);

            // Get the bottom and right hit info 
            // Checks if theres a wall infront or a drop below
            RaycastHit2D hitInfoDown = Physics2D.Raycast(m_groundDetection.position, Vector2.down, m_distance);
            RaycastHit2D hitInfoForward;
            if (m_movingRight)
            {
                hitInfoForward = Physics2D.Raycast(m_groundDetection.position, Vector2.right, m_distance);
                //Debug.DrawRay(m_groundDetection.position, (Vector2.right * m_distance), Color.green);

            }
            else
            {
                hitInfoForward = Physics2D.Raycast(m_groundDetection.position, Vector2.left, m_distance);

                //Debug.DrawRay(m_groundDetection.position, (Vector2.left * m_distance), Color.green);
            }
            // Down collider did not hit anything
            // If forward collider hit something, it means there is smth infront of it
            // If it did hit then it means the player ran
            if (hitInfoForward.collider == true && hitInfoForward.collider.gameObject.tag == "Map")
            {
                m_currState = DASH_STATES.STATE_MORPHED_IDLE;
                m_patrolTimer = 0.0f;
                m_moving = false;
            }
            else if (hitInfoDown.collider == false)
            {
                m_currState = DASH_STATES.STATE_MORPHED_IDLE;
                m_patrolTimer = 0.0f;
                m_moving = false;
            }
        }
    }
    public void RotateLeft()
    {
        transform.eulerAngles = new Vector3(0, -180, 0);
        m_movingRight = false;
    }

    public void RotateRight()
    {
        transform.eulerAngles = new Vector3(0, 0, 0);
        m_movingRight = true;
    }

    public void StartMorphing()
    {

        m_normalGO.SetActive(false);
        m_morphedGO.SetActive(true);

        // When done with morphing
        m_currState = DASH_STATES.STATE_MORPHED_IDLE;
    }

    public void StartUnmorphing()
    {
        m_attackTimer = 0;

        m_normalGO.SetActive(true);
        m_morphedGO.SetActive(false);
        m_attackGO.GetComponent<MeleeEnemyAttack>().ResetAll();

        // When done with unmorphing
        m_currState = DASH_STATES.STATE_NORMAL;
    }

    public void TakeDamage()
    {
        Debug.Log("Damage Taken");
        m_hp -= 1;
        Debug.Log("Health Remaining" + m_hp);
        if (m_hp <= 0)
        {
            m_currState = DASH_STATES.STATE_MORPHED_DEATH;
            m_morphedGO.GetComponent<Animator>().SetBool("Dead", true);

        }
        else
        {
            m_morphedGO.GetComponent<Animator>().SetTrigger("Hit");
        }
    }

    public void Dead()
    {
        gameObject.SetActive(false);
    }
}
