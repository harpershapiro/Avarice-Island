﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaveBoy : Enemy
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// CLASS VARIABLES
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    protected string FLASH_ANIM = "RaveBoyFlashAnim";

    public void Start()
    {
        PUNCH_ANIM = "RaveBoyPunchAnim";
        BREATHING_ANIM = "BreathAnim";
        EXTRA_ATTACK1_ANIM = "Attack2Anim";
        EXTRA_ATTACK2_ANIM = "Attack3Anim";
        EXTRA_ATTACK3_ANIM = "RaveBoyFlashAnim";
        LAUNCH_RISE_ANIM = "RaveBoyLaunchAnim";
        LAUNCH_FALL_ANIM = "RaveBoyLaunchAnim";
        LAUNCH_LAND_ANIM = "RaveBoyLaunchAnim";
        GROUNDED_ANIM = "RaveBoyGroundedAnim";
        STAND_ANIM = "RaveBoyGetUpAnim";
        HURT_GROUNDED_ANIM = "RaveBoyHurtGrounded";
        HURT_STANDING_ANIM = "RaveBoyHurtAnim";


        targetPosition = new Vector3(body.position.x, startingPosition.y, startingPosition.z);
        startingPosition = targetPosition;
        playerReference = GameObject.Find("Player");
        currentState = EnemyState.idle;
        currentHealth = maxHealth;
        isWaiting = true;
        fleeHealth = 30;
    }

    public override void Attack()
    {
        Stop();
        float attackThreshold;
        float flashThreshold = .1f;

        switch (currentLevel)
        {
            case DifficultyLevel.easy:
                attackThreshold = .6f;
                break;
            case DifficultyLevel.medium:
                attackThreshold = .8f;
                break;
            case DifficultyLevel.hard:
                attackThreshold = .95f;
                break;
            default:
                attackThreshold = .6f;
                break;
        }

        float rand = Random.value;
        if (rand <= flashThreshold)
        {
          Flash();
        }
        else if (rand <= attackThreshold)
        {
            if (lastAttack == LastAttack.punch2)
            {
                Kick();
            }
            else if(lastAttack == LastAttack.punch1)
            {
                Punch2();
            }
            else
            {
                Punch();
            }
        }
        else
        {
            StopAndPause(attackingPauseTime);
        }
    }

    public void Punch2()
    {
        //seperate methods for each possible attack and select randomly? How many attacks will grunts have?
        Stop();
        //face the player
        Vector3 playerPosition = playerReference.transform.position;
        FlipSprite(body.position.x > playerPosition.x);
        baseAnim.SetTrigger("Punch2");
        lastAttack = LastAttack.punch2;
    }

    public void Flash()
    {
        //seperate methods for each possible attack and select randomly? How many attacks will grunts have?
        baseAnim.SetTrigger("Flash");
    }
}
