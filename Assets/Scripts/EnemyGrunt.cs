﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyGrunt : Actor
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// CLASS VARIABLES
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    public int maxHealth = 100;
    public float currentHealth;
    public float walkSpeed = 1.5f;
    public float runSpeed = 4f;
    int rightBound = 15;
    int leftBound = 7;

    bool isAttacking;
    bool isHurting;
    float lastHurtTime;
    bool isLaunching;
    float lastLaunchTime;
    float launchForce = 250f;
    bool isStanding;
    bool isGrounded;

    float waitDistance = 4;
    bool isWaiting;

    int paused;
    int waitingPauseTime = 40;
    int attackingPauseTime = 40;

    bool isMoving;
    float lastWalk;
    Vector3 lastWalkVector;

    Vector3 currentDir;
    bool isFacingLeft;

    Vector3 startingPosition = new Vector3(12.0f, 2.475173f, 1.0f);
    Vector3 targetPosition = new Vector3(12.0f, 2.5f, 1.0f);

    float timeOfLastWander;
    float WanderWaitTime = 5;

    float noticeDistance = 7;
    float walkDistance = 3;
    float attackDistance = 1;
    int fleeHealth;


    public enum EnemyState
    {
        //noticed state? alternatively, "if state not approaching and approachable" case in state logic of update
        idle,
        pacing,
        approaching,
        attacking,
        fleeing,
        wandering,
        waiting
    }

    public DifficultyLevel? currentLevel = null;
    public EnemyState currentState;

    public GameObject playerReference;

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// Game Engine Methods
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Start()
    {
        targetPosition = new Vector3(body.position.x, startingPosition.y, startingPosition.z);
        startingPosition = targetPosition;
        playerReference = GameObject.Find("Player");
        currentState = EnemyState.idle;
        currentHealth = maxHealth;
        isWaiting = true;
        fleeHealth = 30;
    }

    public override void Update()
    {
        base.Update();

        CheckAnims();

        Vector3 playerPosition = playerReference.transform.position;
        float currentDistance = Vector3.Distance(body.position, playerPosition);
        
        if (isWaiting)
        {
            isWaiting = !playerReference.GetComponent<Hero>().Engage(this);
        }

        if (paused >= 0)
        {
            paused--;
        }
        else if (isWaiting)
        {
            currentState = EnemyState.waiting;
        }
        //Universal state change logic
        else if (!(isLaunching || isHurting || isAttacking || isWaiting || isGrounded || isStanding))
        {
            //might have to rework this to flee to a determined point rather than a standing distance
            if (currentDistance <= 3 && (currentHealth <= fleeHealth || currentState == EnemyState.fleeing))
            {
                if (currentState != EnemyState.fleeing)
                {
                    switch (currentLevel)
                    {
                        case DifficultyLevel.easy:
                            fleeHealth -= 30;
                            break;
                        case DifficultyLevel.medium:
                            fleeHealth -= 15;
                            break;
                        case DifficultyLevel.hard:
                            fleeHealth -= 10;
                            break;
                        default:
                            fleeHealth -= 30;
                            break;
                    }
                }
                currentState = EnemyState.fleeing;
            }
            else if (currentDistance <= attackDistance) currentState = EnemyState.attacking;
            else if (currentDistance <= noticeDistance) currentState = EnemyState.approaching;
            else currentState = EnemyState.idle;
        }

        //Act on the current state
        switch (currentState)
        {
            case EnemyState.idle:
                Idle();
                break;
            case EnemyState.pacing:
                Pace();
                break;
            case EnemyState.approaching:
                Approach();
                break;
            case EnemyState.attacking:
                Attack();
                break;
            case EnemyState.fleeing:
                Flee();
                break;
            case EnemyState.wandering:
                Wander();
                break;
            case EnemyState.waiting:
                Wait();
                break;
            default:
                break;
        }

    }

    void FixedUpdate()
    {
        Vector3 moveVector = currentDir * speed;
        body.MovePosition(transform.position + moveVector * Time.fixedDeltaTime);
        baseAnim.SetFloat("Speed", moveVector.magnitude);

        if (moveVector != Vector3.zero)
        {
            if (moveVector.x != 0)
            {
                isFacingLeft = moveVector.x < 0;
            }
            FlipSprite(isFacingLeft);
        }
    }

    protected override void OnCollisionEnter(Collision collision)
    {
        switch (collision.collider.tag)
        {
            case "floor":
                onGround = true;
                baseAnim.SetBool("onGround", onGround);
                Landed();
                break;
            case "hit":
                Hit(0);
                break;
            case "launch":
                Launch(collision.transform.position);
                break;
            default:
                break;
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// State Methods
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Idle()
    {
        //Idle anim? leg up on wall/kicking dirt/hand in pocket/smoking?
        Stop();
    }

    public void Pace()
    {
        float positionX = body.position.x;
        float positionY = body.position.y;

        if (positionX <= leftBound)
        {
            currentDir = Vector3.right;
            isFacingLeft = true;
            FlipSprite(isFacingLeft);
        }
        else if (positionX >= rightBound)
        {
            currentDir = Vector3.left;
            isFacingLeft = false;
            FlipSprite(isFacingLeft);
        }
        Walk();
    }

    public void Approach()
    {
        Vector3 playerPosition = playerReference.transform.position;
        currentDir = playerPosition - body.position;
        float currentDistance = currentDir.magnitude;
        currentDir.Normalize();

        if (currentDistance <= walkDistance)
        {
            Walk();
        }
        else
        {
            Run();
        }
    }

    public void Attack()
    {
        Stop();
        float attackThreshold;

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

        if (Random.value <= attackThreshold)
        {
            Punch();
        }
        else
        {
            StopAndPause(attackingPauseTime);
        }
    }

    //might have to rework this to flee to a determined point rather than a standing distance
    public void Flee()
    {
        Vector3 playerPosition = playerReference.transform.position;
        currentDir = body.position - playerPosition;
        currentDir.Normalize();
        Walk();
    }

    public void Wander()
    {
        // If at the new target and it's time to wander again, get a new target position.
        if (IsCloseTo(targetPosition, body.position)) 
        {
            if ((Time.time - timeOfLastWander) > WanderWaitTime)
            {
                timeOfLastWander = Time.time;
                var wanderBoundsX = (left: startingPosition.x - 2, right: startingPosition.x + 2);
                var wanderBoundsZ = (bottom: -2.5f, top: 1.2f);

                Vector3 currPosition = body.position;
                float targetX = (float)(Random.value * (wanderBoundsX.right - wanderBoundsX.left) + wanderBoundsX.left);
                float targetZ = (float)(Random.value * (wanderBoundsZ.top - wanderBoundsZ.bottom) + wanderBoundsZ.bottom);
                float targetY = currPosition.y;

                targetPosition = new Vector3(targetX, targetY, targetZ);
            }
            else
            {
                Stop();
            }
        }
        else
        {
            currentDir = targetPosition - body.position;
            currentDir.Normalize();
            Walk();
        }
    }

    public void Wait()
    {
        if (IsCloseTo(targetPosition, body.position))
        {
            var wanderBoundsX = (left: startingPosition.x - 5, right: startingPosition.x + 5);
            var wanderBoundsZ = (bottom: -2.5f, top: 1.2f);

            //Form a line connecting the player and enemy
            Vector3 playerPosition = playerReference.transform.position;
            Vector3 desiredLine = body.position - playerPosition;
            desiredLine.Normalize();
            //Find a point on that line that is waitDistance away from player
            targetPosition = playerPosition + desiredLine * waitDistance;

            //Check that it's in bounds
            float targetX = targetPosition.x;
            float targetZ = targetPosition.z;
            if (targetX > wanderBoundsX.right) targetX = wanderBoundsX.right;
            if (targetX < wanderBoundsX.left) targetX = wanderBoundsX.left;
            if (targetZ > wanderBoundsZ.top) targetZ = wanderBoundsZ.top;
            if (targetZ < wanderBoundsZ.bottom) targetZ = wanderBoundsZ.bottom;

            targetPosition = new Vector3(targetX, playerPosition.y, targetZ);
            StopAndPause(waitingPauseTime);
        }
        else
        {
            currentDir = targetPosition - body.position;
            currentDir.Normalize();
            Walk();
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// Action Methods
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Punch()
    {
        //seperate methods for each possible attack and select randomly? How many attacks will grunts have?
        baseAnim.SetTrigger("Punch");
    }

    public void Stop()
    {
        speed = 0;
        isMoving = false;
        baseAnim.SetFloat("Speed", speed);
    }

    public void StopAndPause(int pauseTime)
    {
        Stop();
        paused = pauseTime;
        currentState = EnemyState.idle;
    }

    public void Walk()
    {
        isMoving = true;
        speed = walkSpeed;
        baseAnim.SetFloat("Speed", speed);
    }

    public void Run()
    {
        isMoving = true;
        speed = runSpeed;
        baseAnim.SetFloat("Speed", speed);
    }

    public void Hit(float damage)
    {
        Stop();
        isHurting = true;
        lastHurtTime = Time.time;
        baseAnim.SetTrigger("Hit");
        Hurt(damage);
    }

    public void Launch(Vector3 attackerLocation)
    {
        Stop();
        isLaunching = true;
        lastLaunchTime = Time.time;
        baseAnim.SetTrigger("Launch");

        Vector3 horizontalLaunchInfluence = body.position - attackerLocation;
        horizontalLaunchInfluence.Normalize();
        horizontalLaunchInfluence = horizontalLaunchInfluence * launchForce/2;
        body.AddForce(horizontalLaunchInfluence, ForceMode.Force);

        Vector3 verticalLaunchInfluence = Vector3.up * launchForce;
        body.AddForce(verticalLaunchInfluence, ForceMode.Force);
    }

    public void Hurt(float damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0) Die();
    }

    private void Die()//Method for dying?
    {
        List<Actor> playerEngagements = playerReference.GetComponent<Hero>().engaged;
        playerEngagements.Remove(this);
        GameObject.Destroy(transform.root.gameObject);
    }


    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// Helper Functions
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    //MOVING THIS TO A STATIC METHOD IN ACTOR... -HARPER
    private bool IsCloseTo(Vector3 target, Vector3 position)
    {
        float diffX = System.Math.Abs(target.x - position.x);
        float diffY = System.Math.Abs(target.y - position.y);
        float diffZ = System.Math.Abs(target.z - position.z);

        return diffX <= 0.1 && diffY <= 0.1 && diffZ <= 0.1;
    }

    private void CheckAnims()
    {
        isAttacking = baseAnim.GetCurrentAnimatorStateInfo(0).IsName("enemy_grunt_punch");
        isLaunching = baseAnim.GetCurrentAnimatorStateInfo(0).IsName("enemy_grunt_launch");
        isGrounded = baseAnim.GetCurrentAnimatorStateInfo(0).IsName("enemy_grunt_grounded");
        isStanding = baseAnim.GetCurrentAnimatorStateInfo(0).IsName("enemy_grunt_stand");
        isHurting = baseAnim.GetCurrentAnimatorStateInfo(0).IsName("enemy_grunt_hurt_grounded") || 
            baseAnim.GetCurrentAnimatorStateInfo(0).IsName("enemy_grunt_hurt_standing");
    }
}
