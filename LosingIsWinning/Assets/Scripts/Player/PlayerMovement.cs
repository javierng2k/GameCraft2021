﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Data")]
    public float m_walkSpeed = 100.0f;
    public float m_jumpSpeed = 400.0f;
    public float m_fallMultiplier = 2.5f;

    public float m_dashSpeed = 200.0f;
    public float m_dashTime = 2.0f;
    private float m_currDashTime = 0.0f;

    private Vector2 m_inputDir = Vector2.zero;
    private Vector2 m_faceDir = Vector2.zero; //for animation, dash

    private Rigidbody2D m_rigidBody;

    [Header("States")]
    [Tooltip("Standing on ground will reset jump")]
    public Transform m_groundCheckPos; 
    public float m_groundCheckRadius;
    public LayerMask m_groundLayers;
    
    private int m_currJumps = 0;

    [System.NonSerialized] public bool m_startJump = false; //when just press jump
    [System.NonSerialized] public bool m_isGrounded = true; //check if on ground
    [System.NonSerialized] public bool m_isDashing = false;

    [Header("Combat")]
    public Vector2 m_hitOffset = Vector2.zero;
    public Vector2 m_attackRange = Vector2.zero;
    public LayerMask m_attackObjectsMask;

    public float m_AttackRate = 1.0f;
    private float m_currAttackTime = 0.0f;

    [Header("Effects")]
    public float m_ghostFrequency = 0.2f;
    public float m_currGhostTime = 0.0f;
    private CameraShake m_CameraShake;


    // Start is called before the first frame update
    void Start()
    {
        m_rigidBody = GetComponent<Rigidbody2D>();
        m_CameraShake = Camera.main.GetComponent<CameraShake>();

        m_startJump = false;
        m_isDashing = false;
        m_isGrounded = Physics2D.OverlapCircle(m_groundCheckPos.position, m_groundCheckRadius, m_groundLayers);

        m_currJumps = PlayerData.Instance.m_maxJumps;

        m_currAttackTime = Time.time;

        m_currGhostTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        //do not move when camera still moving
        if (!PlayerData.Instance.CanMove())
        {
            m_inputDir = Vector2.zero;
            return;
        }
            
    
        m_inputDir.x = Input.GetAxisRaw("Horizontal");
        m_inputDir.y = Input.GetAxisRaw("Vertical");

        //update for animation
        if (m_inputDir.x != 0.0f && !m_isDashing)
        {
            m_faceDir.x = m_inputDir.x;
        }

        Combat();

        //jump
        if (Input.GetButtonDown("Jump") && m_currJumps > 0)
        {
            --m_currJumps;
            m_startJump = true;
            ParticleEffectObjectPooler.Instance.PlayParticle(m_groundCheckPos.position, PARTICLE_EFFECT_TYPE.JUMP);
        }

        //for dashing
        if (Input.GetKeyDown(KeyCode.LeftShift) && !m_isDashing)
        {
            m_isDashing = true;
            m_currDashTime = Time.time;
            m_currGhostTime = m_currDashTime;

            if (m_CameraShake != null)
                m_CameraShake.StartShake();

            ParticleEffectObjectPooler.Instance.PlayParticle(transform.position, PARTICLE_EFFECT_TYPE.DASH);
        }

        if (m_isDashing)
        {
            float currTime = Time.time;
            if (currTime - m_currGhostTime >= m_ghostFrequency)
            {
                GameObject obj = ObjectPooler.Instance.FetchGO("GhostPlayer");
                obj.transform.position = transform.position;
                m_currGhostTime = currTime;
            }

            m_isDashing = currTime - m_currDashTime < m_dashTime; 
        }
    }

    private void FixedUpdate()
    {
        //press dash
        if (m_isDashing)
        {
            m_rigidBody.velocity = new Vector2(m_faceDir.x * m_dashSpeed * Time.fixedDeltaTime, 0.0f);
            return;
        }

        //Debug.Log("Jump Bool" + m_startJump);
        //Debug.Log(m_currJumps);
        //press jump
        if (m_startJump)
        {
            m_rigidBody.velocity = Vector2.up * m_jumpSpeed * Time.fixedDeltaTime;
            m_startJump = false;
        }

        m_isGrounded = Physics2D.OverlapCircle(m_groundCheckPos.position, m_groundCheckRadius, m_groundLayers);
        if (m_isGrounded) //reset number of jumps
        {
            if (m_currJumps < PlayerData.Instance.m_maxJumps && m_rigidBody.velocity.y == 0.0f)
            {
                ParticleEffectObjectPooler.Instance.PlayParticle(m_groundCheckPos.position, PARTICLE_EFFECT_TYPE.LAND);
                m_currJumps = PlayerData.Instance.m_maxJumps;
            }
        }

        //update right left movement
        m_rigidBody.velocity = new Vector2(m_inputDir.x * m_walkSpeed * Time.fixedDeltaTime, m_rigidBody.velocity.y);

        //if falling
        if (m_rigidBody.velocity.y < 0.0f)
        {
            m_rigidBody.velocity += Vector2.up * Physics2D.gravity.y * (m_fallMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(m_groundCheckPos.position, m_groundCheckRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + new Vector3(m_hitOffset.x, 0.0f), new Vector3(m_attackRange.x, m_attackRange.y, 1));
        Gizmos.DrawWireCube(transform.position + new Vector3(-m_hitOffset.x, 0.0f), new Vector3(m_attackRange.x, m_attackRange.y, 1));
        Gizmos.DrawWireCube(transform.position + new Vector3(0.0f, m_hitOffset.y), new Vector3(m_attackRange.y, m_attackRange.x, 1));
        Gizmos.DrawWireCube(transform.position + new Vector3(0.0f, -m_hitOffset.y), new Vector3(m_attackRange.y, m_attackRange.x, 1));
    }

    public void resetJump()
    {
        m_isGrounded = true;
        m_currJumps = PlayerData.Instance.m_maxJumps;
    }

    public void Combat()
    {
        if (!Input.GetKeyDown(KeyCode.Return))
            return;

        if (Time.time - m_currAttackTime < m_AttackRate)
            return;

        m_currAttackTime = Time.time;

        Vector3 hitDir = Vector3.zero;
        Vector2 hitSize = Vector2.zero;
        if (!m_isGrounded && m_inputDir.y < 0.0f) //is in air can hit down
        {
            hitDir.y = -m_hitOffset.y;
            hitSize = new Vector2(m_attackRange.y, m_attackRange.x);
        }
        else if (m_inputDir.y > 0.0f) //check if player want to hit up
        {
            hitDir.y = m_hitOffset.y;
            hitSize = new Vector2(m_attackRange.y, m_attackRange.x);
        }
        else //horizontal attack base on where the player is facing
        {
            hitDir.x = m_hitOffset.x * m_faceDir.x;
            hitSize = new Vector2(m_attackRange.x, m_attackRange.y);
        }

        Collider2D[] attackObjs = Physics2D.OverlapBoxAll(transform.position + hitDir, hitSize, m_attackObjectsMask);
        foreach (Collider2D objs in attackObjs)
        {
            HitObjs hitObj = objs.GetComponent<HitObjs>();
            if (hitObj == null)
                continue;

            hitObj.Hit();
        }
    }
}
