using UnityEngine;
using Rewired;
using UnityEditor;
using TMPro;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{

    // The Rewired player id of this character
    public int playerId = 0;

    // The movement speed of this character
    public float moveSpeed = 10f;

    public float dodgeDuration = 0.1f;
    public float dodgeSpeed = 50f;
    public float dodgeStoppingSpeed = .1f;
    public float dodgeCooldown = 4f;
    public float throwStrength = 500f;
    public float currentThrowStrength;
    public float throwAngle = 0f;
    public float currentThrowAngle;
    public float chargeStrengthSpeed = 200f;
    public float chargeAngleSpeed = 50f;

    public GameObject ballPrefab;
    public GameObject circleObject;

    public Color playerColor;
    public string playerName;

    public GameObject tmp;

    public TrajectorySimulation ts;
    public float simulationMultiplier = 0.022f;

    public float interactableRange = 5f;

    private float currentDodgeCooldown;
    private float elapsedDodgeTime;

    public float jumpStrength = .3f;
    public float gravity = 2f;

    public Player player; // The Rewired Player
    private CharacterController cc;
    private Vector3 moveVector;
    private Vector3 dodgeVector;
    private Vector3 lookVector;
    private Vector3 lastLookVector = new Vector3(90, 0,0);

    private GameObject outlinedGameObject = null;
    private GameObject heldGameObject = null;
    
    private bool dodgeInput;
    private bool throwInput;
    private bool releaseInput;
    private bool dropInput;
    private bool jumpInput;

    private bool chargingBall;
    private bool dodgeing = false;

    public int teamNumber = 1;
    private float teamAxis = 1;

    void Awake()
    {
        // Get the Rewired Player object for this player and keep it for the duration of the character's lifetime
        player = ReInput.players.GetPlayer(playerId);

        // Get the character controller
        cc = GetComponent<CharacterController>();

        elapsedDodgeTime = dodgeDuration;
        currentDodgeCooldown = 0.0f;
        currentThrowStrength = throwStrength;
        currentThrowAngle = throwAngle;

        if (teamNumber == 2)
        {
            teamAxis = -1;
        }

        tmp.GetComponent<TextMeshPro>().text = playerName;
    }

    private void Start()
    {
        playerColor = Random.ColorHSV();
        ts.color = playerColor;
        circleObject.GetComponent<SpriteRenderer>().color = playerColor;        
    }

    void Update()
    {
        GetInput();
        ProcessInput();
        HighlightClosestInteractable();
    }

    private void HighlightClosestInteractable()
    {
        float closestDistanceSqr = Mathf.Infinity;
        Vector3 currentPosition = transform.position;

        outlinedGameObject = null;

        foreach (GameObject interactable in GameObject.FindGameObjectsWithTag("Interactable"))
        {
            Vector3 directionToTarget = interactable.transform.position - currentPosition;
            float dSqrToTarget = directionToTarget.sqrMagnitude;
            if (dSqrToTarget < closestDistanceSqr && dSqrToTarget < interactableRange)
            {
                closestDistanceSqr = dSqrToTarget;
                outlinedGameObject = interactable;
            }
            
            //interactable.GetComponent<Outline>().enabled = false;
        }

        if (outlinedGameObject)
        {
            //outlinedGameObject.GetComponent<Outline>().enabled = true; 
        }

    }


    private void GetInput()
    {
        // Get the input from the Rewired Player. All controllers that the Player owns will contribute, so it doesn't matter
        // whether the input is coming from a joystick, the keyboard, mouse, or a custom controller.

        moveVector.x = player.GetAxis("Move Horizontal") * teamAxis; // get input by name or action id
        moveVector.z = player.GetAxis("Move Vertical") * teamAxis;            

        dodgeInput = player.GetButtonDown("Dodge");
        if (dodgeInput && currentDodgeCooldown <= 0.0f)
        {
            elapsedDodgeTime = 0.0f;
            currentDodgeCooldown = dodgeCooldown;
            dodgeing = true;
            dodgeVector = moveVector;
        } 

        throwInput = player.GetButtonDown("Throw");
        releaseInput = player.GetButtonUp("Throw");
        dropInput = player.GetButtonDown("Drop");
        jumpInput = player.GetButtonDown("Jump");

        float h = player.GetAxis("Look Horizontal");
        float v = player.GetAxis("Look Vertical");

        if (Mathf.Abs(h) < 0.1f && Mathf.Abs(v) < 0.1f)
        {
            lookVector = lastLookVector;
        }
        else
        {
            lookVector = new Vector3(90, -1 * (Mathf.Atan2(v, h) * Mathf.Rad2Deg) + (90 * teamAxis), 0);
            lastLookVector = lookVector;
        }

    }

    private void ProcessInput()
    {

        //jump
        if (cc.isGrounded)
        {
            moveVector.y = 0;
            if (jumpInput)
            {
                moveVector.y = jumpStrength;
            }            
        }

        //dodge
        if (currentDodgeCooldown > 0.0f)
        {
            currentDodgeCooldown -= Time.deltaTime;
        }
        if (elapsedDodgeTime < dodgeDuration)
        {
            elapsedDodgeTime += Time.deltaTime;
            dodgeing = true;
            moveVector = dodgeVector;

            //if no direction, dodge backwards
            if (dodgeVector.x == 0 && dodgeVector.z == 0)
            {
                dodgeVector.z = -1;
            }
        }
        else
        {
            dodgeing = false;
        }

        //move
        moveVector.y -= gravity * Time.deltaTime;
        moveVector.x *= (dodgeing ? dodgeSpeed : moveSpeed) * Time.deltaTime;
        moveVector.z *= (dodgeing ? dodgeSpeed : moveSpeed) * Time.deltaTime;

        cc.Move(moveVector);


        if (transform.position.z * teamAxis > 0)
        {
            transform.position.Set(transform.position.x, transform.position.y, 0);
        }

        //rotate
        circleObject.transform.rotation = Quaternion.Euler(lookVector);
        ts.Enabled = false;

        //keep ball
        if (heldGameObject)
        {

            #region Update HeldObject
                heldGameObject.transform.position = new Vector3(this.transform.position.x, this.transform.position.y + 2, this.transform.position.z);
                heldGameObject.transform.rotation = Quaternion.Euler(new Vector3(-currentThrowAngle, lookVector.y, 0));
                Rigidbody rb = heldGameObject.GetComponent<Rigidbody>();
                rb.velocity = heldGameObject.transform.forward;
            #endregion

            if (dropInput)
            {
                heldGameObject = null;
                chargingBall = false;
                throwInput = false;
            }

            if (throwInput || chargingBall)
            {
                //charge
                chargingBall = true;

                currentThrowStrength = Mathf.Clamp(currentThrowStrength + (Time.deltaTime * chargeStrengthSpeed), 500, 1000);
                currentThrowAngle = Mathf.Clamp(currentThrowAngle + (Time.deltaTime * chargeAngleSpeed), 0, 45);

                ts.SimulatePath(heldGameObject, (rb.velocity * throwStrength * simulationMultiplier) + new Vector3(0, 0, moveVector.z), rb.mass, rb.drag);
                ts.Enabled = true;
            }


            if (releaseInput && chargingBall)
            {

                chargingBall = false;
                ////throw
                rb.AddForce((heldGameObject.transform.forward * throwStrength) + new Vector3(0,0,moveVector.z));

                heldGameObject = null;

                currentThrowStrength = throwStrength;
                currentThrowAngle = throwAngle;
            }

            //let go of exploded bomb
            if (heldGameObject && heldGameObject.GetComponent<BombConroller>())
            {
                if (heldGameObject.GetComponent<BombConroller>().dissolve)
                {
                    heldGameObject = null;
                }
            }
        }

        if (throwInput)
        {
            //pickup
            if (outlinedGameObject && !outlinedGameObject.GetComponent<BombConroller>().dissolve)
            {
                heldGameObject = outlinedGameObject;
                outlinedGameObject = null;
                heldGameObject.GetComponent<Animator>().enabled = true;
            }
        }

    }
}