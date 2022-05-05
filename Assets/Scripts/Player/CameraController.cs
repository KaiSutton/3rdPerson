using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    private InputManager input;

    public Transform targetTransform; //where the camera will eventually move to
    public Transform camTransform; //location of the actual camera
    public Transform pivotTransform; //location of the camera pivot

    private Vector3 camPos; //the position data of the camera transform. Only matters for z position.
    public LayerMask ignoreLayers;

    private Vector3 cameraFollowVelocity = Vector3.zero;

    public float lookSpeed = .03f;
    public float followSpeed = .1f;
    public float pivotSpeed = .03f;

    public float targetPos;
    private float defaultPos;
    private float lookAngle;
    private float pivotAngle;

    public float minPivot = -35;
    public float maxPivot = 35;

    public float cameraSphereRadius = .2f;
    public float cameraCollisionOffset = .2f;
    public float minCollisionOffset = .2f;


    private void Awake()
    {
        defaultPos = camTransform.localPosition.z;
    }

    void Start()
    {
        input = InputManager.instance;
    }

    // Update is called once per frame
    void Update()
    {
        FollowTarget(Time.deltaTime);
        HandleCameraRotation(Time.deltaTime);
        
    }

    /// <summary>
    /// Moves the transform of the camera holder gameobject towards the player's location.
    /// </summary>
    /// <param name="delta"></param>
    private void FollowTarget(float delta)
    {
        //create a position somewhere between the current position and target position
        Vector3 targetPosition = Vector3.SmoothDamp(transform.position, targetTransform.position, ref cameraFollowVelocity, delta / followSpeed);

        //assign the value of targetPosition to the main transform.
        transform.position = targetPosition;

        HandleCameraCollision(delta);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="delta"></param>
    private void HandleCameraRotation(float delta)
    {
        float mouseX = input.look.x;
        float mouseY = input.look.y;

        //calculate 'looking' AKA side to side turning
        lookAngle += (mouseX * lookSpeed) / delta;

        //calculate 'pivot' AKA up and down tilting
        pivotAngle -= (mouseY * pivotSpeed) / delta;

        //clamp the pivot so we can't tilt too far
        pivotAngle = Mathf.Clamp(pivotAngle, minPivot, maxPivot);

        //THE FOLLOWING SECTION APPLIES SIDE TO SIDE ROTATION:

        //create a vector to store a new rotation into (we must initialize it or Quaternion.Euler() will complain)
        Vector3 rotation = Vector3.zero;

        //What would the y value of our new rotation be? Thats right. The look angle we calculated
        rotation.y = lookAngle;

        //make a quaternion out of the euler angle
        Quaternion targetRotation = Quaternion.Euler(rotation);

        transform.rotation = targetRotation;

        //THE FOLLOWING SECTION APPLIES UP AND DOWN ROTATION:

        //set the rotation back to zero
        rotation = Vector3.zero;

        //we want to tilt this pivot. So we affect the x angle.
        rotation.x = pivotAngle;

        targetRotation = Quaternion.Euler(rotation);

        //why local rotation? See below. 
        pivotTransform.localRotation = targetRotation;

        /*
         * When we are 'looking' we are turning the entire camera holder. 
         * When we are 'tilting' we are tilting the CAMERA PIVOT *RELATIVE* to the camera holder.
         * To do that, we have to use localRotation. 
         */
    }

    private void HandleCameraCollision(float delta)
    {
        //the targetPos is the location (on the z) we want to move the camera to.
        //by default every frame, the target z position is set to the default. So it is trying to reset itself every frame. 
        targetPos = defaultPos;

        //create a raycast hit to store hit data
        RaycastHit hit;

        //calculate the direction we want to cast in (from the camera position to the pivot)
        Vector3 direction = camTransform.position - pivotTransform.position;
        direction.Normalize();

        //cast a sphere (of cameraSphereRadius size) from the pivot in the direction calculated earlier. 
        //the furthest we need to cast is the absolute value of the farthest the camera can possibly go.
        if(Physics.SphereCast(pivotTransform.position, cameraSphereRadius, direction, out hit, Mathf.Abs(targetPos), ignoreLayers))
        {
            //if we hit something, find out how far it was from the pivot to that point. 
            float dis = Vector3.Distance(pivotTransform.position, hit.point);

            //update the target position with this distance made negative
            targetPos = -(dis - cameraCollisionOffset);

            //if our new target position is less than our minimum offset, update to the minimum collision offset.
            //This will keep the camera from going inside the player.
            if (Mathf.Abs(targetPos) < minCollisionOffset)
            {
                targetPos = -minCollisionOffset;
            }
        }

        //calculate a new position between the camera current position and the target position
        camPos.z = Mathf.Lerp(camTransform.localPosition.z, targetPos, delta / .2f);
        //move the actual camera position to the calculated position.
        camTransform.localPosition = camPos;

        //NOTE: Since we know that the cameraTransform x and y values are zero, setting them equal to camPos works. 
        //If for some reason they were not zero, we would need to update camPos. 

        //NOTE: Why do we keep targetPos negative? Because we want the camera to be behind the cameraPivot. 
        //When there is a successful spherecast, we are not increasing the distance between the pivot and the camera, we are decreasing it. 
        //So when a collision occurs, targetPos will be set to a smaller negative number (in terms of absolute value) than it was before. 
    }
}
