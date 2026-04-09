using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarHandler : MonoBehaviour
{
    [SerializeField]
    Rigidbody rb;

    [SerializeField]
    Transform gameModel;

    //Max values
    float maxSteerVelocity = 12;
    float maxForwardVelocity = 20;

    //Multipliers
    float accelerationMultiplier = 5;
    float breakstionMultiplier = 15;
    float steeringMultiplier = 10;

    //Input
    Vector2 input = Vector2.zero;

    void Start()
    {
        gameModel.transform.localRotation = Quaternion.identity;
    }

    void Update()
    {
        //Rotate car model when 'turning'
        gameModel.transform.rotation = Quaternion.Euler(0, rb.velocity.x * 5, 0);
    }

    private void FixedUpdate()
    {
        //Apply Acceleration
        if (input.y > 0)
        {
            Accelerate();
        }
        else
            rb.drag = 0.2f;

        //Apply Brakes
        if (input.y < 0)
        {
            Brake();
        }

        Steer();

        //Force the car not to go backwards - ИСПРАВЛЕНО
        // Теперь блокируется только движение НАЗАД, а не вперёд
        if (rb.velocity.z < 0)
            rb.velocity = new Vector3(rb.velocity.x, 0, 0);
    }

    void Accelerate()
    {
        rb.drag = 0;

        //Stay within the speed limit
        if (rb.velocity.z >= maxForwardVelocity)
            return;

        rb.AddForce(rb.transform.forward * accelerationMultiplier * input.y);
    }

    void Brake()
    {
        //Don't brake unless we are going forward
        if (rb.velocity.z <= 0)
        {
            return;
        }

        rb.AddForce(rb.transform.forward * breakstionMultiplier * input.y);
    }

    void Steer()
    {
        if (Mathf.Abs(input.x) > 0)
        {
            //Move the car sideways
            float speedBaseSteerLimit = rb.velocity.z / 5.0f;
            speedBaseSteerLimit = Mathf.Clamp01(speedBaseSteerLimit);

            rb.AddForce(rb.transform.right * steeringMultiplier * input.x * speedBaseSteerLimit);

            //Normalize the X Velocity
            float normalizedX = rb.velocity.x / maxSteerVelocity;

            //Ensure that we don't allow it to get bigger than 1 in magnitued
            normalizedX = Mathf.Clamp(normalizedX, -1.0f, 1.0f);

            //Make sure we stay within the turn speed limit
            rb.velocity = new Vector3(normalizedX * maxSteerVelocity, 0, rb.velocity.z);
        }
        else
        {
            //Auto center car
            rb.velocity = Vector3.Lerp(rb.velocity, new Vector3(0, 0, rb.velocity.z), Time.fixedDeltaTime * 3);
        }
    }

    public void SetInput(Vector2 inputVector)
    {
        inputVector.Normalize();
        input = inputVector;
    }

    public void SetMaxSpeed(float speed)
    {
        maxForwardVelocity = speed;
    }
    public float GetMaxSpeed()
    {
        return maxForwardVelocity;
    }

    public float GetCurrentSpeed()
    {
        return rb.velocity.z;
    }
}