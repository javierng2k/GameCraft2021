﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundSlamAnimController : MonoBehaviour
{
    GroundSlamEnemyController controller;

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponentInParent<GroundSlamEnemyController>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void runAttack()
    {
        
    }

    public void endAttack()
    {

    }
}