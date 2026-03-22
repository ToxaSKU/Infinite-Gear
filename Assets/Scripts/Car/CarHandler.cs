using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarHandler : MonoBehaviour
{
    [SerializeField]
    Rigidbody rb;

    //Multipliers
    float accelerationMultiplier = 3;
    float breakstionMultiplier = 15;
    float steeringMultiplier = 5;

    //Input
    Vector2 input = Vector2.zero;

    //Start is called before the first frsme update


    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

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
    }

    void Accelerate()
    {
        rb.drag = 0;

        rb.AddForce(rb.transform.forward * accelerationMultiplier * input.y);
    }

    void Brake()
    {
        //Don't brake unless we are going forward - Не тормозит пока движемся вперед
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
            rb.AddForce(rb.transform.right * steeringMultiplier * input.x);
        }
    }

    public void SetInput(Vector2 inputVector)
    {
        inputVector.Normalize();

        input = inputVector;
    }
}
