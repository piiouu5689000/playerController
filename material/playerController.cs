using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerController : MonoBehaviour
{
    [Header("References")]
    public Rigidbody rb;
    public Transform head;
    public Camera camera;

    [Header("moving")]
    public float walkSpeed;
    public float runSpeed;
    public float jumpSpeed;
    public float impactThreshold; //閾值
    public float keyPickupDistance;

    [Header("CCTV")]
    public float baseCamFov = 60f;
    public float baseCamHigh = .85f;

    public float walking = .75f;
    public float running =  1f;
    public float maxWalking = .2f;
    public float maxRunning =.3f;

    public float shakeThreshold = 10f; // 震動閾值
    [Range(0f, 0.03f)] public float shakeRate = 0.15f; // 震動頻率
    public float maxVertivalShakeAngle = 40f;
    public float maxHorizonShakeAngle = 40f;

    [Header("Audio")]
    public AudioSource audioWalk;
    public AudioSource audioWind;
    public AudioSource steelWalk;
    public float windPitchMultiplier;

    [Header("Runtime")]
    Vector3 newVector;
    bool isGrounded = false;
    bool isJumping = false;
    float vyCache; //緩存
    string activeAudioName = "default";
    Transform attachedObject = null;
    float attachedDistance =1.5f;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.visible = false;
        //這行程式碼將 Unity 中的滑鼠游標的可見性設置為 false。
        Cursor.lockState =CursorLockMode.Locked;
        //這行程式碼將 Unity 中的滑鼠游標的鎖定狀態設置為 CursorLockMode.Locked。
        //當滑鼠游標被鎖定時，它將被限制在遊戲窗口中心，不再能夠自由移動。
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(Vector3.up*Input.GetAxis("Mouse X")*2f);

        newVector = Vector3.up * rb.velocity.y;
        float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        newVector.x = Input.GetAxis("Horizontal") * speed;
        newVector.z = Input.GetAxis("Vertical") * speed;

        if (isGrounded) {
            if (Input.GetKeyDown(KeyCode.Space) && ! isJumping)
            {
                newVector.y = jumpSpeed;
                // 在空白鍵按下時執行的程式碼
                Debug.Log("Space key is pressed");
            }

        }
        //移動視野
        bool isMovingOnGround = (Input.GetAxis("Vertical") != 0f || Input.GetAxis("Horizontal") != 0f) && isGrounded;

        if (isMovingOnGround) {
            float bobbingRate = Input.GetKey(KeyCode.LeftShift) ? walking : running;
            float bobbingOffset = Input.GetKey(KeyCode.LeftShift) ? maxWalking : maxRunning;
            Vector3 targetHeadPosition = Vector3.up * baseCamHigh + Vector3.up * (Mathf.PingPong(Time.time * bobbingRate, bobbingOffset) - bobbingOffset * .5f);
            head.localPosition = Vector3.Lerp(head.localPosition, targetHeadPosition, .1f);
        }

        //rb.velocity = newVector; 速度方向相對於世界坐標系
        rb.velocity = transform.TransformDirection(newVector);
        //速度方向相對於物體本身而不是世界坐標系

        //Audio
        audioWalk.enabled = isMovingOnGround && activeAudioName == "default";
        audioWalk.pitch = Input.GetKey(KeyCode.LeftShift) ? 1.75f : 1f;

        steelWalk.enabled = isMovingOnGround && activeAudioName == "steel_floor";
        steelWalk.pitch = Input.GetKey(KeyCode.LeftShift) ? 1.75f : 1f;

        audioWind.enabled = true;
        audioWind.pitch = Mathf.Clamp(Mathf.Abs(rb.velocity.y * windPitchMultiplier), 0.5f, 2f) + Random.Range(-.1f, .1f);

        // picking object
        RaycastHit hit;
        bool cast = Physics.Raycast(head.position, head.forward, out hit, keyPickupDistance);


        if(Input.GetKeyDown(KeyCode.F)){
            Debug.Log("F key is pressed");
            if (attachedObject != null){
                attachedObject.SetParent(null);

                if (attachedObject.GetComponent<Rigidbody>() != null)
                    attachedObject.GetComponent<Rigidbody>().isKinematic = false;

                if (attachedObject.GetComponent<Collider>() != null)
                    attachedObject.GetComponent<Collider>().enabled = true;

                attachedObject = null;
            }
            else {
                if (cast){
                    if (hit.transform.CompareTag("pickAble")){
                        attachedObject = hit.transform;
                        attachedObject.SetParent(transform);

                        if (attachedObject.GetComponent<Rigidbody>() != null)
                            attachedObject.GetComponent<Rigidbody>().isKinematic = true;

                        if (attachedObject.GetComponent<Collider>() != null)
                            attachedObject.GetComponent<Collider>().enabled = false;
                    }
                }
            }
        }     
    }

    void FixedUpdate() 
    {

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1f)){
            isGrounded = true;

            if (hit.transform.tag == "steel_floor"){
                activeAudioName = "steel_floor";
            }
            else{
                activeAudioName = "default";
            }


        } 
        else {
            isGrounded = false;
        }

        vyCache = rb.velocity.y;


    }

    void LateUpdate() {
        //Vertical rotation
        Vector3 e = head.eulerAngles;
        e.x -= Input.GetAxis("Mouse Y")*2f;
        e.x = RestricAngle(e.x, -85f, 85f);
        head.eulerAngles = e;

        //FOV 視野範圍
        float fovOffset = (rb.velocity.y < 0f) ? Mathf.Sqrt(Mathf.Abs(rb.velocity.y)) : 0f;
        camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, baseCamFov + fovOffset, .25f);
        
        //下落
        if (!isGrounded && Mathf.Abs(rb.velocity.y) >= shakeThreshold){
            Vector3 newAngle = head.localEulerAngles;
            newAngle += Vector3.right * Random.Range(-maxVertivalShakeAngle, maxVertivalShakeAngle);
            newAngle += Vector3.up *Random.Range(-maxHorizonShakeAngle, maxHorizonShakeAngle);
            head.localEulerAngles = Vector3.Lerp(head.localEulerAngles, newAngle, shakeRate);
        }
        else {
            e = head.localEulerAngles;
            e.y = 0f;
            head.localEulerAngles = e;
        }

        if (attachedObject != null){
            attachedObject.position = head.position + head.forward * attachedDistance;
            attachedObject.Rotate(transform.right * Input.mouseScrollDelta.y * 30f, Space.World);
        }



    }
    public static float RestricAngle( float angle, float angleMin, float angleMax)
    {
        if (angle > 180)
            angle -= 360;
        else if (angle < -180)
            angle += 360;

        
        if (angle > angleMax)
            angle = angleMax;
        if (angle < angleMin)
            angle = angleMin;


        return angle;

    }
    
    void OnCollisionStay(Collision col) {
        isGrounded = true;
        isJumping = false;
        
    }

    void OnCollisionExit(Collision col) {
        isGrounded = false;
        
    }

    void OnCollisionEnter(Collision other) {
        float acceleration = (rb.velocity.y - vyCache)/ Time.fixedDeltaTime;
        float impactForce = rb.mass * acceleration;

        if (impactForce >= impactThreshold){
            Debug.Log("Damage!!");
        }


    }
        
    
}
