
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIBase : MonoBehaviour
{
    // its satuday, garfelf's second favorit da

    [Header("Game Objects")]
    public GameObject leftGc;
    public GameObject rightGc;
    public GameObject WCLL;
    public GameObject WCHL;
    public GameObject WCLR;
    public GameObject WCHR;
    public GameObject CCR;
    public GameObject CCL;
    public GameObject landingSmoke;
    public GameObject squishLandHelper;
    public GameObject gunTip;
    public GameObject DogGunPickup;
    public GameObject GunFX;
    public GameObject TeleportSmoke;
    public GameObject crushEffect;
    public GameObject DistrictAIManager;
    [SerializeField] GameObject SoundCreator;
    [SerializeField] AudioClip CallFriendsSound;
    [SerializeField] AudioClip DieSound;
    [SerializeField] AudioClip GunSound;

    [Header("PatrolSettings")]
    public float leftLimit;
    public float rightLimit;
    public float breakTime; //time a guard will wait when they reach a point
    public float breakRng; //randomness allowed in breaktime, in seconds
    [SerializeField] public string moveDir;
    public float canSee;
    //[SerializeField] private FieldOfView fieldOfView;

    //[Header("Misc")]

    public enum AIState { idle, suspicious, boundedPatrol, aggro, dead };
    public AIState aiState;
    public Vector2 velocity;
    public float gravity;
    public float speed;
    public float aggroSpeed;
    private Rigidbody2D rb2d;
    [SerializeField] bool grounded;
    bool ceilinged;
    bool walledLeft;
    bool walledRight;
    public bool hasSetAggro = false;
    public Vector2 JumpTimeRange;
    public Vector2 JumpHeightRange;
    public Vector2 ShotFireTimeRange;
    public Transform shotPos;
    public GameObject Projectile;
    private GameObject Player;
    private float jumpTimer;
    private float shotTimer;
    public float currentSuspicion; //now sus the enemy is
    public float suspicionTriggerLevel = 110;
    private bool queueJump = false;
    private bool hasSquished = false;
    private bool hasSpawnedLandingFX = false;
    private bool nearGrounded = false; // workaround for odd gravity and death stuff
    public bool steadfast;
    bool crushed = false;
    float crushtime;
    [SerializeField] bool onPlatform;
    public Animator anim;
    public Animator emoAnim;
    [SerializeField] FieldOfViewScript fovScript;
    public GameObject susIndicatorGameObject;
    public Sprite exclamationMarkSprite;
    public Sprite questionMarkSprite;
    //helps gravity work correctly
    bool setyVel0 = true;
    bool canPause = false; //for when the ai is sus
    bool canMove = true; //for when the ai is calling its friends
    public bool killedByOtherAI;
    public string isPaused = "unassigned"; // for suspicion state
    bool velHasDiminished = false;//to help the bouncing after death
    public bool hasCalledFriends = false; //helps to limit how many times frieds are called
    public bool hasCalledFriendsSus = false;
    bool hasDroppedGun;
    bool hasdied; // to keep the guy from bein crushed

    void Start()
    {
        SoundCreator = Instantiate(SoundCreator, transform);
        rb2d = GetComponent<Rigidbody2D>();
        jumpTimer = JumpTimeRange.y;
        shotTimer = ShotFireTimeRange.y;
        Player = GameObject.Find("Player");
        DistrictAIManager = transform.parent.gameObject;
    }
    public void StateMachine()
    {
        switch (aiState)
        {
            case AIState.idle:
                hasSetAggro = false;
                SoundCreator.GetComponent<AudioProximity>().canPlayMaxTime = 5f;
                hasdied = false;
                break;
            case AIState.boundedPatrol:
                speed = 4f;
                hasSetAggro = false;
                hasdied = false;
                Patrol();
                break;
            case AIState.suspicious:
                speed = 4f;
                hasSetAggro = false;
                hasdied = false;
                StartCoroutine("Suspicious");
                break;
            case AIState.aggro:
                speed = aggroSpeed;
                SoundCreator.GetComponent<AudioProximity>().canPlayMaxTime = 0.1f;
                setAggro();
                hasdied = false;
                hasSetAggro = true;
                Aggro();
                break;
            case AIState.dead:
                DeathProcedure();
                break;
        }
    }

    void Patrol()
    {
        if(moveDir == "right" && canMove == true)
        {
            if (transform.position.x <= rightLimit)
            {
                velocity = new Vector2(speed, rb2d.velocity.y);
            }
            else
            {
                StartCoroutine("PatrolBreak");
            }
        }
        if (moveDir == "left" && canMove == true)
        {
            if (transform.position.x >= leftLimit)
            {
                velocity = new Vector2(-speed, rb2d.velocity.y);
            }
            else
            {
                StartCoroutine("PatrolBreak");
            }
        }

    }

    void Aggro()
    {
       
        if (walledLeft)
        {
            velocity.x = speed;
        }
        else if (walledRight)
        {
            velocity.x = -speed;
        }
        if (Mathf.Abs(transform.position.x - Player.transform.position.x) > 30 && Mathf.Abs(transform.position.x - Player.transform.position.x) < 30 && shotTimer < 0.1f)
        {
            if (transform.position.x > Player.transform.position.x)
            {
                velocity.x = -speed;
            }
            else if (transform.position.x < Player.transform.position.x)
            {
                velocity.x = speed;
            }
        }
        shotTimer -= Time.deltaTime;
        if (fovScript.canSeePlayer == true)
        {
            
            DistrictAIManager.GetComponent<DistrictAIManagerScript>().aggroTimeLeft = DistrictAIManager.GetComponent<DistrictAIManagerScript>().aggroTime;
        }
        if (shotTimer <= 0 && -2.2f < (transform.position.y-Player.transform.position.y) && (transform.position.y - Player.transform.position.y) < 2.2f )
        {
            shotTimer = Random.Range(ShotFireTimeRange.x, ShotFireTimeRange.y);
            if (Random.Range(0, 100) > 30)
            {
                if (transform.position.x > Player.transform.position.x)
                {
                    velocity.x = -speed;
                }
                else if (transform.position.x < Player.transform.position.x)
                {
                    velocity.x = speed;
                }
            }
            for (int i = 0; i < Projectile.GetComponent<ProjectileScript>().shotCount; i++)
            {
                
                Shoot();
                
            } 
        }       
        else if (shotTimer <= 0)
        {
            shotTimer = Random.Range(ShotFireTimeRange.x, ShotFireTimeRange.y);
            if (Random.Range(0, 100) > 70)
            {
                if (transform.position.x > Player.transform.position.x)
                {
                    velocity.x = -speed;
                }
                else if (transform.position.x < Player.transform.position.x)
                {
                    velocity.x = speed;
                }
            }
        }
        jumpTimer -= Time.deltaTime;
        if (jumpTimer <= 0)
        {
            queueJump = true;
        }

        if (grounded == true && queueJump == true)
        {
            queueJump = false;
            velocity.y = Random.Range(JumpHeightRange.x, JumpHeightRange.y);
            jumpTimer = Random.Range(JumpTimeRange.x, JumpTimeRange.y);
        }

        if (grounded == false)
        {
            setyVel0 = true;
        }
    }

    private IEnumerator PatrolBreak()
    {
        aiState = AIState.idle;
        velocity = new Vector2(0, rb2d.velocity.y);
        yield return new WaitForSeconds (Random.Range(breakTime-breakRng, breakTime+breakRng));
        if (aiState == AIState.idle)
        {
            aiState = AIState.boundedPatrol;
            if (fovScript.canSeePlayer == false)
            {
                if (moveDir == "right")
                {
                    moveDir = "left";
                }
                else
                {
                    moveDir = "right";
                }
            }
            
        }
    }

    private IEnumerator Suspicious()
    {
        if (steadfast == true)
        {
            canMove = false;
            canPause = true;
        }
        RaycastHit2D GroundCheckLeft = Physics2D.Linecast(leftGc.transform.position, leftGc.transform.position - new Vector3(0, -0.1f, 0), 1 << LayerMask.NameToLayer("Ground"));
        RaycastHit2D GroundCheckRight = Physics2D.Linecast(rightGc.transform.position, rightGc.transform.position - new Vector3(0, -0.1f, 0), 1 << LayerMask.NameToLayer("Ground"));
        RaycastHit2D WallCheckLeft = Physics2D.Linecast(WCLL.transform.position, WCHL.transform.position, 1 << LayerMask.NameToLayer("Ground"));
        //RaycastHit2D WallCheckHighLeft = Physics2D.Linecast(WCHL.transform.position, WCHL.transform.position - new Vector3(0, -0.1f, 0), 1 << LayerMask.NameToLayer("Ground"));
        RaycastHit2D WallCheckRight = Physics2D.Linecast(WCLR.transform.position, WCHR.transform.position - new Vector3(0, -0.1f, 0), 1 << LayerMask.NameToLayer("Ground"));
        //RaycastHit2D WallCheckHighRight = Physics2D.Linecast(WCHR.transform.position, WCHR.transform.position - new Vector3(0, -0.1f, 0), 1 << LayerMask.NameToLayer("Ground"));
        if ((GroundCheckLeft.collider == null && GroundCheckRight.collider != null)) //|| (WallCheckLeft.collider != null))
        {
            canPause = false;
            //velocity.x = Mathf.Abs(velocity.x);
            if (fovScript.canSeePlayer == false)
            {
                moveDir = "right";
            }
            else
            {
                moveDir = "left";
            }

        }
        else if (WallCheckLeft.collider != null)
        {
            moveDir = "right";
        }
        else if ((GroundCheckLeft.collider != null && GroundCheckRight.collider == null)) //|| (WallCheckHighRight.collider != null || WallCheckLowRight.collider != null))
        {
            canPause = false;
            //velocity.x = -Mathf.Abs(velocity.x);
            if (fovScript.canSeePlayer == false)
            {
                moveDir = "left";
            }
            else
            {
                moveDir = "right";
            }
        }
        else if (WallCheckRight.collider != null)
        {
            moveDir = "left";
        }
        if (fovScript.canSeePlayer == false)
        {
            canPause = true;
        }
        if (moveDir == "right" && canMove == true)
        {
            if (isPaused == "true" && canPause == true)
            {
                velocity.x = 0f;
            }
            else
            {
                velocity.x = speed;
            }

        }
        else if (canMove == true)
        {
            if (isPaused == "true" && canPause == true)
            {
                velocity.x = 0f;
            }
            else
            {
                velocity.x = -speed;
            }
        }
        if (isPaused == "false" && canPause == true)
        {
            yield return new WaitForSeconds(4f);
            if (Random.Range(0, 100) > 50f && isPaused == "false")
            {
                isPaused = "true";
                yield return new WaitForSeconds(Random.Range(2, 5));
                isPaused = "false";
            }
        }
        else if (isPaused != "true" && canPause == true)
        {
            isPaused = "true";
            yield return new WaitForSeconds(Random.Range(2, 5));
            isPaused = "false";
        }



    }

    // Update is called once per frame
    void Update()
    {
        if (crushed == false)
        {
            StateMachine();
        }

        //check for ground
        //check for wall
        if(Physics2D.Linecast(WCLL.transform.position, WCHL.transform.position, 1 << LayerMask.NameToLayer("Ground")))
        {
            walledLeft = true;
        }
        else
        {
            walledLeft = false;
        }
        if (Physics2D.Linecast(WCHR.transform.position, WCLR.transform.position, 1 << LayerMask.NameToLayer("Ground")))
        {
            walledRight = true;
        }
        else
        {
            walledRight = false;
        }

        if (Physics2D.Linecast(CCR.transform.position, CCL.transform.position, 1 << LayerMask.NameToLayer("Ground")))
        {
            ceilinged = true;
            velocity = new Vector2(velocity.x, Mathf.Abs(velocity.y) * -0.4f);
        }
        else
        {
            ceilinged = false;
        }
        RaycastHit2D GroundCheck = Physics2D.Linecast(leftGc.transform.position, rightGc.transform.position, 1<<LayerMask.NameToLayer("Ground"));
        //RaycastHit2D GroundCheckRight = Physics2D.Linecast(rightGc.transform.position, rightGc.transform.position - new Vector3(0, -0.1f,0),1<<LayerMask.NameToLayer("Ground"));
        if (GroundCheck.collider != null)// || GroundCheckRight.collider != null)
        {
            grounded = true;
            StartCoroutine("SquishOnLand");
            if (GroundCheck.collider.gameObject.tag == "Platform")
            {
                if (GroundCheck.collider.gameObject.tag == "Platform")
                {
                    transform.SetParent(GroundCheck.collider.gameObject.transform);
                }

                onPlatform = true;
            }
            else
            {
                transform.parent = null;
                onPlatform = false;
            }
            if (hasSpawnedLandingFX == false)
            {
                hasSpawnedLandingFX = true;
                if (aiState != AIState.dead)
                {
                    Instantiate(landingSmoke, transform.GetChild(0).transform.position + new Vector3(0, -1.6f, 0), Quaternion.Euler(-90, 0, 0));
                }
                else
                {
                    Instantiate(landingSmoke, transform.GetChild(0).transform.position + new Vector3(0, -0.8f, 0), Quaternion.Euler(-90, 0, 0));
                }


            }
            if (aiState == AIState.dead)
            {
                anim.SetBool("DeadUp", true);
                anim.SetBool("DeadDown", false);
            }

        }
        else
        {
            transform.parent = null;
            onPlatform = false;
            hasSquished = false;
            hasSpawnedLandingFX = false;
            grounded = false;
        }
        if (ceilinged && grounded == true)
        {
            crushtime += Time.deltaTime;
            if (crushtime > 0.04f)
            {
                Crushed();
            }
            
        }
        else
        {
            crushtime = 0;
        }

        if (aiState != AIState.dead)
        {
                if (currentSuspicion >= suspicionTriggerLevel)
                {
                    susIndicatorGameObject.transform.GetComponent<SpriteRenderer>().sprite = exclamationMarkSprite;
                    susIndicatorGameObject.transform.localScale = new Vector3(0.5f, 0.5f, 0);
                    susIndicatorGameObject.transform.localPosition = Vector3.Lerp(susIndicatorGameObject.transform.localPosition, new Vector3(0, -0.75f, 0), Time.deltaTime * 4f);
                }
                else
                {
                    susIndicatorGameObject.transform.GetComponent<SpriteRenderer>().sprite = questionMarkSprite;
                    susIndicatorGameObject.transform.localPosition = Vector3.Lerp(susIndicatorGameObject.transform.localPosition, new Vector3(0, 0, 0), Time.deltaTime * 4f);
                    susIndicatorGameObject.transform.localScale = new Vector3(currentSuspicion / 200, currentSuspicion / 200, 1);
                    susIndicatorGameObject.transform.localPosition = new Vector2(0, 0f + (currentSuspicion / 200));
                }
        }
        

    }

    private void FixedUpdate()
    {
        fovScript.SetOrigin(transform.GetChild(3).position + new Vector3(transform.position.x, transform.position.y + 1.65f, 0));
        if (fovScript.canSeePlayer == true)
        {
            currentSuspicion += 0.8f*fovScript.suspicionMultiplier;
            //if (Player.GetComponent<PlayerController>().isKilling == true)
            //{
                //GameObject NewProj;
                //if (moveDir == "right")
                //{
                //    NewProj = Instantiate(Projectile, new Vector3(transform.position.x + 1.6f, transform.position.y, //transform.position.z), Quaternion.identity);
                //}
                //else
                //{
                //    NewProj = Instantiate(Projectile, new Vector3(transform.position.x - 1.6f, transform.position.y, //transform.position.z), Quaternion.identity);
                //}
                //if (moveDir == "right")
                //{
                //    NewProj.GetComponent<Rigidbody2D>().velocity = new Vector2(Projectile.GetComponent<ProjectileScript>().ProjectileSpeed, Random.Range(-0.5f, 2f));
                //}
                //else
                //{
                //    NewProj.GetComponent<Rigidbody2D>().velocity = new Vector2(-Projectile.GetComponent<ProjectileScript>().ProjectileSpeed, Random.Range(-0.5f, 2f));
                //}
                //setAggro();
            //}
        }
        if (currentSuspicion >= suspicionTriggerLevel)
        {
            setAggro();
        }
        if (currentSuspicion > ((suspicionTriggerLevel / 2) - 1) && aiState != AIState.aggro)
        {
            StartCoroutine("CallFriendsSus");
        }
        if (velocity.x > 0)
        {
            moveDir = "right";
            
        }
        else if (velocity.x < 0)
        {
            moveDir = "left";
        }
        if (velocity.x == 0)// for animation 
        { 
            anim.SetBool("IsWalking", false);
            anim.SetBool("IsRunning", false);
        }
        else
        {
            if (aiState == AIState.aggro)
            {
                anim.SetBool("IsWalking", false);
                if (grounded == true)
                {
                    anim.SetBool("IsRunning", true);
                    anim.SetBool("JumpingUp", false);
                    anim.SetBool("JumpingDown", false);
                }
                else
                {
                    anim.SetBool("IsRunning", false);
                    if (velocity.y > 2)
                    {
                        anim.SetBool("JumpingUp", true);
                        anim.SetBool("JumpingDown", false);
                    }
                    else
                    {
                        anim.SetBool("JumpingDown", true);
                        anim.SetBool("JumpingUp", false);
                    }
                    
                    
                }
            }
            else
            {
                anim.SetBool("IsWalking", true);
            }

        }
        if (grounded == false)//gravity
        {
            velocity.y += gravity;
            if (aiState != AIState.dead)
            {
                setyVel0 = true;
            }
            anim.SetBool("JumpingUp", true);
        }
        else
        {
            if (setyVel0 == true && (onPlatform == false && aiState != AIState.dead))
            {
                velocity.y = 0;
                setyVel0 = false;
            }
            anim.SetBool("JumpingUp", false);
            anim.SetBool("JumpingDown", false);
            
        }
        rb2d.velocity = new Vector2(velocity.x, velocity.y);

        //makes enemy look direction they are moving
        if (moveDir == "right")
        {
            fovScript.SetAimDirection(moveDir);
            anim.gameObject.transform.localScale = new Vector3(-1, 1, 1);
            emoAnim.transform.parent.gameObject.transform.localScale = new Vector3(-1, 1, 1);
            emoAnim.transform.parent.gameObject.transform.localPosition = new Vector3(0.83f, 2, 1);
        }
        else
        {
            fovScript.SetAimDirection(moveDir);
            anim.gameObject.transform.localScale = new Vector3(1, 1, 1);
            emoAnim.transform.parent.gameObject.transform.localScale = new Vector3(1, 1, 1);
            emoAnim.transform.parent.gameObject.transform.localPosition = new Vector3(-0.83f, 2, 1);
        }
        //makes them hit their head on the ceiling
        
    }

    public void setAggro()
    {
        if (aiState != AIState.dead)
        {
            
            if (DistrictAIManager.GetComponent<DistrictAIManagerScript>().HasCalled == false)
            {
                StartCoroutine("CallFriends");
            }
            else if (hasSetAggro == false)
            {

                aiState = AIState.aggro;
                if (GameObject.Find("Player").GetComponent<Transform>().position.x > transform.position.x)
                {
                    velocity.x = aggroSpeed;
                }
                else
                {
                    velocity.x = -aggroSpeed;
                }
                hasSetAggro = true;
            }
        }
        
        
    }
    public IEnumerator CallFriendsSus()
    {
        if (hasCalledFriendsSus == false && aiState != AIState.dead)
        {
            hasCalledFriendsSus = true;
            if (hasCalledFriends == false)
            {
                
                SoundCreator.transform.position = transform.position;
                SoundCreator.GetComponent<AudioProximity>().PlaySound(CallFriendsSound, 80f, 0.9f);
            }
            anim.SetBool("IsWalking", false);
            anim.SetBool("IsCalling", true);
            anim.SetBool("IsRunning", false);
            canMove = false;
            velocity.x = 0;
            aiState = AIState.idle;
            transform.GetChild(1).transform.GetChild(0).transform.GetChild(0).transform.GetChild(9).gameObject.SetActive(true);
            transform.GetChild(1).transform.GetChild(0).transform.GetChild(0).transform.GetChild(8).gameObject.SetActive(false);

            yield return new WaitForSeconds(3);
            canMove = true;
            anim.SetBool("IsCalling", false);  
            if (hasCalledFriends == true)
            {
                if (aiState != AIState.dead)// && hasCalledFriends == false)
                {
                    //hasCalledFriends = true;
                    DistrictAIManager.GetComponent<DistrictAIManagerScript>().CallAll();
                    for (int i = 0; i < Projectile.GetComponent<ProjectileScript>().shotCount; i++)
                    {
                        Shoot();
                    }

                }
            }
            else
            {
                if (aiState != AIState.dead)
                {
                    aiState = AIState.suspicious;
                    transform.GetChild(1).transform.GetChild(0).transform.GetChild(0).transform.GetChild(9).gameObject.SetActive(false);
                    transform.GetChild(1).transform.GetChild(0).transform.GetChild(0).transform.GetChild(8).gameObject.SetActive(true);
                    DistrictAIManager.GetComponent<DistrictAIManagerScript>().CallAllSus();
                }
                
            }
        }
        
    }

    public IEnumerator CallFriends() // talks to the parent to anger all AI in the area
    {
        if (hasCalledFriends == false && aiState != AIState.dead)
        {
            hasCalledFriends = true;
            if (hasCalledFriendsSus == true)
            {
                SoundCreator.transform.position = transform.position;
                SoundCreator.GetComponent<AudioProximity>().PlaySound(CallFriendsSound, 80f, 0.9f);
            }
            anim.SetBool("IsWalking", false);
            anim.SetBool("IsCalling", true);
            velocity.x = 0;
            aiState = AIState.idle;
            transform.GetChild(1).transform.GetChild(0).transform.GetChild(0).transform.GetChild(9).gameObject.SetActive(true);
            transform.GetChild(1).transform.GetChild(0).transform.GetChild(0).transform.GetChild(8).gameObject.SetActive(false);
            canMove = false;
            yield return new WaitForSeconds(3f);
            anim.SetBool("IsCalling", false);
            canMove = true;

            if (aiState != AIState.dead)// && hasCalledFriends == false)
            {
                for (int i = 0; i < Projectile.GetComponent<ProjectileScript>().shotCount; i++)
                {
                    Shoot();
                }
                DistrictAIManager.GetComponent<DistrictAIManagerScript>().CallAll();
            }
        }
        
        
    }

    private void DeathProcedure()
    {
        gameObject.layer = 10;
        if (hasdied == false)
        {
            hasdied = true;
            SoundCreator.transform.position = transform.position;
            SoundCreator.GetComponent<AudioProximity>().PlaySound(DieSound, 80f, 0.9f);
            RaycastHit2D CheckRight = Physics2D.Linecast(transform.position, transform.position + new Vector3(2.5f, 0, 0), 1 << LayerMask.NameToLayer("Ground"));
            RaycastHit2D CheckLeft = Physics2D.Linecast(transform.position, transform.position + new Vector3(-2.5f, 0, 0), 1 << LayerMask.NameToLayer("Ground"));
            if (CheckLeft.collider != null)
            {
                transform.position = transform.position + new Vector3(1.5f, 0, 0);
            }
            else if (CheckRight.collider != null)
            {
                transform.position = transform.position + new Vector3(-1.5f, 0, 0);
            }
        }
        transform.GetChild(2).transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = false;
        transform.GetChild(1).transform.GetChild(0).transform.GetChild(0).transform.GetChild(9).gameObject.SetActive(false);
        transform.GetChild(1).transform.GetChild(0).transform.GetChild(0).transform.GetChild(8).gameObject.SetActive(false);
        transform.GetChild(1).transform.GetChild(0).transform.GetChild(0).transform.GetChild(3).gameObject.SetActive(false);
        susIndicatorGameObject.gameObject.SetActive(false);
        transform.GetChild(1).transform.GetChild(0).transform.GetChild(0).transform.GetChild(10).gameObject.SetActive(true);
        if (hasDroppedGun == false)
        {
            hasDroppedGun = true;
            GameObject DGP = Instantiate(DogGunPickup, transform.position + new Vector3(0,2,0), Quaternion.identity);
            DGP.GetComponent<SimpleBoxObjectPhysics>().velocity = velocity * 0.7f;
        }
        if (anim.gameObject.transform.localScale.x > 0)
        {
            anim.gameObject.transform.localPosition = new Vector3(-0.759f,0.225f,0);
        }
        else
        {
            anim.gameObject.transform.localPosition = new Vector3(0.728f,0.225f,0);
        }
        GetComponent<BoxCollider2D>().offset = new Vector2(0f, 0);
        leftGc.transform.localPosition = new Vector2(-1.71f, -0.694f);
        rightGc.transform.localPosition = new Vector3(1.71f, -0.694f, 0);
        CCL.transform.localPosition = new Vector2(-1.71f, 0.694f);
        CCR.transform.localPosition = new Vector2(1.71f, 0.694f);
        WCHL.transform.localPosition = new Vector2(-1.754f, 0.657f);
        WCLL.transform.localPosition = new Vector2(-1.754f, -0.657f);
        WCLR.transform.localPosition = new Vector2(1.754f, -0.657f);
        WCHR.transform.localPosition = new Vector2(1.754f, 0.657f);
        GetComponent<BoxCollider2D>().size = new Vector2(3.47414f, 1.348f);
        RaycastHit2D NearGroundCheck = Physics2D.Linecast(leftGc.transform.position + new Vector3(0, -0.1f, 0), rightGc.transform.position + new Vector3(0, -0.1f, 0), 1 << LayerMask.NameToLayer("Ground"));
        if (velocity.y >= -4 && nearGrounded == false)
        {
            anim.SetBool("DeadUp", true);
            anim.SetBool("DeadDown", false);
        }
        else if (nearGrounded == false)
        {
            anim.SetBool("DeadUp", false);
            anim.SetBool("DeadDown", true);
        }
        if (NearGroundCheck.collider != null)
        {
            nearGrounded = true;
            velocity.x = Mathf.Lerp(velocity.x, 0, Time.deltaTime * 3f);
        }
        else
        {
            nearGrounded = false;
        }
        if (grounded == true)
        {
            if (velocity.y < -15f)
            {
                velocity.y = Mathf.Abs(velocity.y);
                velHasDiminished = false;
            }
            else
            {
                velocity.y = Mathf.Lerp(velocity.y, -1, Time.deltaTime * 10);
                velocity.x = Mathf.Lerp(velocity.x, 0, Time.deltaTime * 2f);
            }
        }
        else if (velHasDiminished == false)
        {
            velHasDiminished = true;
            velocity.x *= 0.5f;
            velocity.y *= 0.6f;
        }
        if (walledLeft)
        {
            velocity.x = Mathf.Abs(velocity.x);
        }
        if (walledRight)
        {
            velocity.x = -Mathf.Abs(velocity.x);
        }
        if (ceilinged)
        {
            velocity.y = -Mathf.Abs(velocity.y);
        }
        if (velocity.y > -2.5f)
        {
            anim.SetBool("DeadUp", true);
            anim.SetBool("DeadDown", false);
        }
        else
        {
            anim.SetBool("DeadUp", false);
            anim.SetBool("DeadDown", true);
        }
        if (nearGrounded == false)//workaround for gravity being wonky
        {
            velocity.y += gravity * Time.deltaTime;
        }
        rb2d.velocity = velocity;
    }
    //for emotion animation
    public IEnumerator Surprised()
    {
        emoAnim.SetBool("IsSurprised", true);
        yield return new WaitForSeconds(0.7f);
        emoAnim.SetBool("IsSurprised", false);
    }

    public IEnumerator Angry()
    {
        emoAnim.SetBool("IsAngry", true);
        yield return new WaitForSeconds(0.7f);
        emoAnim.SetBool("IsAngry", false);
    }
    public IEnumerator Worried()
    {
        emoAnim.SetBool("IsWorried", true);
        yield return new WaitForSeconds(0.7f);
        emoAnim.SetBool("IsWorried", false);
    }

    //makes the ai squish when it lands
    private IEnumerator SquishOnLand()
    {
        if (hasSquished == false)
        {

            squishLandHelper.transform.localScale = Vector3.Lerp(squishLandHelper.transform.localScale, new Vector3(1.1f, 0.9f, 1), Time.deltaTime * 45f);
            yield return new WaitForSeconds(0.08f);
            hasSquished = true;

        }

        squishLandHelper.transform.localScale = Vector3.Lerp(squishLandHelper.transform.localScale, new Vector3(1, 1f, 1), Time.deltaTime * 45f);

    }

    //stimuli responses
    public void HearSound(Vector3 HitPos)
    {
        if (aiState != AIBase.AIState.dead)
        {
            if (velocity.y < 3 && velocity.y < 0.5f && aiState != AIBase.AIState.aggro)
            {
                if (onPlatform == false)
                {
                    velocity.y = 6;
                }
            }
            if (HitPos.x > transform.position.x)
            {
                moveDir = "right";
            }
            else
            {
                moveDir = "left";
            }
            
            currentSuspicion += 31;
            StartCoroutine("Surprised");
            //enemiesInSound[i].gameObject.GetComponent<AIBase>().aiState = AIBase.AIState.suspicious;

        }
    }

    public void Teleport(Vector3 location)
    {
        transform.position = location;
        Instantiate(TeleportSmoke, location + new Vector3(0, -0.7f, 0), Quaternion.Euler(-90, 0, 0));
    }

    private void Shoot()
    {
        SoundCreator.GetComponent<AudioProximity>().PlaySound(GunSound, 150f, 0.3f);
        GameObject NewProj;
        if (moveDir == "right")
        {
            NewProj = Instantiate(Projectile, gunTip.transform.position, Quaternion.identity);
            NewProj.GetComponent<Rigidbody2D>().velocity = new Vector2(Projectile.GetComponent<ProjectileScript>().ProjectileSpeed, Random.Range(-0.5f, 2f));
            GameObject newFX = Instantiate(GunFX, gunTip.transform.position, Quaternion.Euler(-90, 0, 0));
            newFX.transform.parent = transform;
        }
        else
        {
            NewProj = Instantiate(Projectile, gunTip.transform.position, Quaternion.identity);
            NewProj.GetComponent<Rigidbody2D>().velocity = new Vector2(-Projectile.GetComponent<ProjectileScript>().ProjectileSpeed, Random.Range(-0.5f, 2f));
            GameObject newFX = Instantiate(GunFX, gunTip.transform.position, Quaternion.Euler(-90, 180, 0));
            newFX.transform.parent = transform;
        }
        
    }

    public void Crushed()
    {
        if(crushed == false)
        {
            SoundCreator.GetComponent<AudioProximity>().PlaySound(DieSound, 80f, 0.9f);
            crushed = true;
            GetComponent<BoxCollider2D>().enabled = false;
            aiState = AIState.dead;
            rb2d.bodyType = RigidbodyType2D.Static;
            transform.GetChild(1).gameObject.SetActive(false);
            Instantiate(crushEffect, transform.position, Quaternion.identity);
        }

        
    }
}
