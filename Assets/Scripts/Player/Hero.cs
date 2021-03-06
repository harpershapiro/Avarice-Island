using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Hero : Actor
{

    public List<Actor> engaged;
    private readonly object balanceLock = new object();
    public int numEngagements;

    protected string LAUNCH_RISE_ANIM;
    protected string LAUNCH_FALL_ANIM;
    protected string LAUNCH_LAND_ANIM;
    protected string GROUNDED_ANIM;
    protected string GET_UP_ANIM;
    protected string HURT_GROUNDED_ANIM;

    public const float maxHealth = 150, maxLives = 3;
    public float currentHealth;
    public float currentLives;

    public float walkSpeed = 2;
    public float runSpeed = 5;

    public bool isRunning;
    bool isMoving;
    float lastWalk;
    bool attack;
    public bool freeze = false;

    public bool canRun = true;
    float tapAgainToRunTime = 0.4f;
    Vector3 lastWalkVector;

    Vector3 currentDir;
    public bool isFacingLeft;

    bool isJumpLandAnim;
    bool isJumpingAnim;
    bool isUsingFeverAnim;
    public bool isGrounded;
    public bool isLaunching;
    public bool isFlashed;

    public InputHandler input;

    public float jumpForce = 1700;
    private float jumpDuration = 0.2f;
    private float lastJumpTime;

    bool isAttackingAnim;
    bool heroTemporaryFlashing;
    float lastAttackTime;
    float attackLimit = 0.14f;

    //for player death
    public Dialogue deathDialogue;
    public Animator deathScreenAnim;
    private bool needsRevive;

    public Vector3 startingCoords;

    private Scene lastScene;

    private static Hero instance;

        /*
    public static Hero Instance{
        get{
            if(instance == null){
                instance = GameObject.FindObjectOfType<Hero>();
            }
        return instance;

        }
    }*/

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {

        //Upkeep after player death
        this.enabled = true;
        if(needsRevive){
            ResetHealth();
            baseAnim.SetTrigger("Revive");
            deathScreenAnim.SetTrigger("reset");
            needsRevive = false;
        }
       // deathScreenAnim.SetTrigger("reset");

    }


    void Awake() {
    //Hero Script Persists across scenes
        startingCoords = this.transform.position;
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (instance != null)
        {
            instance.ResetCoords();
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

    }

    public void Start()
    {
        LAUNCH_RISE_ANIM = "LaunchRise";
        LAUNCH_FALL_ANIM = "LaunchFall";
        LAUNCH_LAND_ANIM = "LaunchLand";
        GROUNDED_ANIM = "GroundedAnim";
        GET_UP_ANIM = "GetUpAnim";
        HURT_GROUNDED_ANIM = "None"; //TODO: Add hurt grounded anim for player!

        engaged = new List<Actor>(numEngagements);
        currentHealth = maxHealth;
        currentLives = maxLives;
        deathDialogue = gameObject.GetComponent<Dialogue>();
        deathScreenAnim = GameObject.Find("DeathScreen").GetComponent<Animator>();
        needsRevive = false;
        transform.localScale = new Vector3(this.size, this.size, 1); //fixes scale bug on spawn
    }

    public override void Update()
    {
        base.Update();

        CheckAnims();

        if (isGrounded || isLaunching || isUsingFeverAnim || isFlashed || isAttackingAnim){
          freeze = true;
        }
        else {
          freeze = false;
        }

        float h = input.GetHorizontalAxis();
        float v = input.GetVerticalAxis();
        bool jump = false;
        bool FeverAttack = false;
        if (freeze){
          h = 0;
          v = 0;
        }
        else {
          jump = input.GetJumpButtonDown();
          attack = input.GetAttackButtonDown();
          FeverAttack = input.GetFeverAttackButtonDown();
        }

        currentDir = new Vector3(h, 0, v);
        currentDir.Normalize();

        if (!isAttackingAnim)
        {
            if ((v == 0 && h == 0))
            {
                Stop();
                isMoving = false;
            }
            else if (!isMoving && (v != 0 || h != 0))
            {
                isMoving = true;
                float dotProduct = Vector3.Dot(currentDir, lastWalkVector);
                if (canRun && Time.time < lastWalk + tapAgainToRunTime && dotProduct > 0)
                {
                    Run();
                }
                else
                {
                    Walk();
                    if (h != 0)
                    {
                        lastWalkVector = currentDir;
                        lastWalk = Time.time;
                    }
                }
            }
        }


        if (jump && !isJumpLandAnim && !isAttackingAnim &&
        (onGround || (isJumpingAnim && Time.time < lastJumpTime +
        jumpDuration)))
        {
            Jump(currentDir);
        }

        if (Input.GetButtonDown("Attack"))
        {
            attack = true;
            baseAnim.SetBool("attack", attack);
        }
        else if (Input.GetButtonUp("Attack"))
        {
            attack = false;
            baseAnim.SetBool("attack", attack);

        }

        //check for death
        if(currentHealth<=0 && !heroTemporaryFlashing){
            if(currentLives<=1){
                currentLives--;
                needsRevive = true;
                Die();
            } else {
                currentLives--;
                LoseLife();
                //currentHealth = maxHealth;
            }
        }

    }

    public void Stop()
    {
        speed = 0;
        baseAnim.SetFloat("Speed", speed);
        isRunning = false;
        baseAnim.SetBool("IsRunning", isRunning);
    }

    public IEnumerator Freeze(float time){
      Stop();
      freeze = true;
      yield return new WaitForSeconds(time);
      freeze = false;
    }

    public void Walk()
    {
        speed = walkSpeed;
        baseAnim.SetFloat("Speed", speed);
        isRunning = false;
        baseAnim.SetBool("IsRunning", isRunning);
    }


    void FixedUpdate()
    {
        Vector3 moveVector = currentDir * speed;
        if (freeze){

        }
        else {
          if (onGround && !isAttackingAnim)
          {
              body.MovePosition(transform.position + moveVector * Time.fixedDeltaTime);
              baseAnim.SetFloat("Speed", moveVector.magnitude);
          }

          if (moveVector != Vector3.zero)
          {
              if (moveVector.x != 0)
              {
                  //transform.Rotate(0f, 180f, 0f);

                  isFacingLeft = moveVector.x < 0;
              }

              FlipSprite(isFacingLeft);
          }
        }
    }

    public void Run()
    {
        speed = runSpeed;
        isRunning = true;
        baseAnim.SetBool("IsRunning", isRunning);
        baseAnim.SetFloat("Speed", speed);
    }

    void Jump(Vector3 direction)
    {
        if (!isJumpingAnim)
        {
            baseAnim.SetTrigger("Jump");
            lastJumpTime = Time.time;

            Vector3 horizontalVector = new Vector3(direction.x, 0, direction.z) * speed * 40;
            body.AddForce(horizontalVector, ForceMode.Force);
        }

        Vector3 verticalVector = Vector3.up * jumpForce * Time.deltaTime;
        body.AddForce(verticalVector, ForceMode.Force);
    }


    protected override void Landed()
    {
        base.Landed();
        Walk();
    }

    public bool getRunning()
    {
        return isRunning;
    }

    public void Launch(int damage){
        TakeDamage(damage);
        baseAnim.SetTrigger("Launch");
    }

    //Not sure how Hunter wanted to do these but here is a quick and dirty version -Ethan
    public void Hurt(int damage)
    {
        TakeDamage(damage);
        baseAnim.SetTrigger("Hurt");
    }

    public void Zap(int damage)
    {
        TakeDamage(damage);
        baseAnim.SetTrigger("Zap");
    }

    public void Stunned(){
      baseAnim.SetTrigger("Flashed");
      StartCoroutine(Freeze(1.2f));
    }

    public void TakeDamage(int damage)
    {
        currentHealth = (currentHealth - damage);
        gameObject.GetComponent<Health>().health = (int) Mathf.Ceil(currentHealth / (maxHealth / 5)); //Update the Health script and pips in the UI
    }

    public void Die(){
        //deathDialogue.PlayDialogue(); //This is causing a freeze
        baseAnim.SetTrigger("Dead"); //maybe best to have these triggers have same name?
        deathScreenAnim.SetTrigger("death");
        this.enabled = false;
    }

    public void LoseLife(){
        baseAnim.SetTrigger("Dead");
        StartCoroutine(WaitAndRevive(4));

    }

    public void ResetCoords(){
        Debug.Log("trying to reset player coords");
        transform.position = startingCoords;
        body.MovePosition(startingCoords);
    }

    public void ResetHealth(){
        currentHealth = maxHealth;
        currentLives = maxLives;
    }

    IEnumerator WaitAndRevive(float time){
        input.enabled = false;
        heroTemporaryFlashing=true;
        yield return new WaitForSeconds(time);
        baseAnim.SetTrigger("Revive");
        currentHealth = maxHealth;
        heroTemporaryFlashing=false;
        input.enabled = true;
    }

    public bool Engage(Actor enemy)
    {
        lock (balanceLock)
        {
            if (engaged.Count < numEngagements)
            {
                engaged.Add(enemy);
                return true;
            }
            return false;
        }
    }

    public bool Disengage(Enemy enemy)
    {
      lock (balanceLock)
      {
        engaged.Remove(enemy);
        return true;
      }
    }

    public virtual void CheckAnims()
    {
        isLaunching = baseAnim.GetCurrentAnimatorStateInfo(0).IsName(LAUNCH_RISE_ANIM) ||
            baseAnim.GetCurrentAnimatorStateInfo(0).IsName(LAUNCH_FALL_ANIM) ||
            baseAnim.GetCurrentAnimatorStateInfo(0).IsName(LAUNCH_LAND_ANIM);
        isAttackingAnim = baseAnim.GetCurrentAnimatorStateInfo(0).IsName("Punch") ||
            baseAnim.GetCurrentAnimatorStateInfo(0).IsName("Kick") ||
            baseAnim.GetCurrentAnimatorStateInfo(0).IsName("Roundhouse") ||
            baseAnim.GetCurrentAnimatorStateInfo(0).IsName("Uppercut");
        isJumpLandAnim = baseAnim.GetCurrentAnimatorStateInfo(0).IsName("JumpLand");
        isJumpingAnim = baseAnim.GetCurrentAnimatorStateInfo(0).IsName("JumpRise") ||
              baseAnim.GetCurrentAnimatorStateInfo(0).IsName("JumpFall");
        isUsingFeverAnim = baseAnim.GetCurrentAnimatorStateInfo(0).IsName("FeverAOE") ||
              baseAnim.GetCurrentAnimatorStateInfo(0).IsName("FeverProjectile") ||
              baseAnim.GetCurrentAnimatorStateInfo(0).IsName("FeverLightning");
        isGrounded = baseAnim.GetCurrentAnimatorStateInfo(0).IsName(GROUNDED_ANIM) ||
              baseAnim.GetCurrentAnimatorStateInfo(0).IsName(GET_UP_ANIM);
        isFlashed = baseAnim.GetCurrentAnimatorStateInfo(0).IsName("InitStunAnim") ||
              baseAnim.GetCurrentAnimatorStateInfo(0).IsName("StunnedAnim");
    }
}
