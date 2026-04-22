using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarHandler : MonoBehaviour
{
    [SerializeField]
    Rigidbody rb;

    [SerializeField]
    Transform gameModel;

    [Header("SFX")]
    [SerializeField]
    AudioSource carEngineAS;

    [SerializeField]
    AnimationCurve carPitchAnimationCurve;

    [Header("Crash Settings")]
    [SerializeField]
    float crashSpeedThreshold = 3f;
    [SerializeField]
    float crashAudioFadeTime = 0.5f;

    [Header("Speed Progression")]
    [SerializeField] bool useSpeedProgression = true;
    [SerializeField] float startMaxSpeed = 15f;
    [SerializeField] float finalMaxSpeed = 35f;
    [SerializeField] float timeToReachMaxSpeed = 120f;
    [SerializeField] AnimationCurve speedProgressionCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Minimum Speed (Anti-Stuck)")]
    [SerializeField] bool enableMinSpeed = true;
    [SerializeField] float minSpeed = 12f;           // Минимальная скорость 43 км/ч
    [SerializeField] bool increaseMinSpeedOverTime = true; // Увеличивать мин. скорость со временем
    [SerializeField] float finalMinSpeed = 25f;       // Конечная минимальная скорость (90 км/ч)

    //Max values
    float maxSteerVelocity = 12;
    float maxForwardVelocity = 20;

    //Multipliers
    float accelerationMultiplier = 5;
    float breakstionMultiplier = 15;
    float steeringMultiplier = 10;

    //Input
    Vector2 input = Vector2.zero;

    bool isPlayer = true;
    private bool audioInitialized = false;
    private bool isCrashed = false;
    private float originalVolume;

    private float gameStartTime;
    private float currentMaxSpeed;
    private float currentMinSpeed;

    void Start()
    {
        gameModel.transform.localRotation = Quaternion.identity;
        isPlayer = CompareTag("Player");

        if (isPlayer)
        {
            InitCarAudio();
            gameStartTime = Time.time;
            UpdateMaxSpeed();
            currentMinSpeed = minSpeed;
        }
    }

    void Update()
    {
        gameModel.transform.rotation = Quaternion.Euler(0, rb.velocity.x * 5, 0);

        if (isCrashed && carEngineAS != null && carEngineAS.isPlaying)
        {
            carEngineAS.Stop();
        }

        if (isPlayer && useSpeedProgression && !isCrashed)
        {
            UpdateMaxSpeed();

            // Увеличиваем минимальную скорость со временем
            if (increaseMinSpeedOverTime)
            {
                float elapsedTime = Time.time - gameStartTime;
                float t = Mathf.Clamp01(elapsedTime / timeToReachMaxSpeed);
                currentMinSpeed = Mathf.Lerp(minSpeed, finalMinSpeed, t);
            }
        }

        UpdateCarAudio();
    }

    void UpdateMaxSpeed()
    {
        float elapsedTime = Time.time - gameStartTime;
        float t = Mathf.Clamp01(elapsedTime / timeToReachMaxSpeed);
        float curveValue = speedProgressionCurve.Evaluate(t);
        currentMaxSpeed = Mathf.Lerp(startMaxSpeed, finalMaxSpeed, curveValue);
        maxForwardVelocity = currentMaxSpeed;
    }

    void InitCarAudio()
    {
        if (carEngineAS == null)
        {
            carEngineAS = GetComponent<AudioSource>();
            if (carEngineAS == null)
                return;
        }

        if (carEngineAS.clip == null)
            return;

        carEngineAS.loop = true;
        carEngineAS.playOnAwake = false;
        originalVolume = 0.6f;
        carEngineAS.volume = originalVolume;

        carEngineAS.Play();
        audioInitialized = true;
    }

    private void FixedUpdate()
    {
        if (isCrashed)
            return;

        // ========== ПРИНУДИТЕЛЬНАЯ МИНИМАЛЬНАЯ СКОРОСТЬ ==========
        if (isPlayer && enableMinSpeed && rb.velocity.z < currentMinSpeed)
        {
            // Не даём упасть ниже минимальной скорости
            rb.velocity = new Vector3(rb.velocity.x, 0, currentMinSpeed);
        }

        if (input.y > 0)
        {
            Accelerate();
        }
        else
            rb.drag = 0.2f;

        if (input.y < 0)
        {
            Brake();
        }

        Steer();

        if (rb.velocity.z < 0)
            rb.velocity = new Vector3(rb.velocity.x, 0, 0);
    }

    void Accelerate()
    {
        rb.drag = 0;

        if (rb.velocity.z >= maxForwardVelocity)
            return;

        rb.AddForce(rb.transform.forward * accelerationMultiplier * input.y);
    }

    void Brake()
    {
        // Если достигли минимальной скорости - тормоз не работает
        if (isPlayer && enableMinSpeed && rb.velocity.z <= currentMinSpeed)
            return;

        if (rb.velocity.z <= 0)
            return;

        rb.AddForce(rb.transform.forward * breakstionMultiplier * input.y);
    }

    void Steer()
    {
        if (Mathf.Abs(input.x) > 0)
        {
            float speedBaseSteerLimit = rb.velocity.z / 5.0f;
            speedBaseSteerLimit = Mathf.Clamp01(speedBaseSteerLimit);

            rb.AddForce(rb.transform.right * steeringMultiplier * input.x * speedBaseSteerLimit);

            float normalizedX = rb.velocity.x / maxSteerVelocity;
            normalizedX = Mathf.Clamp(normalizedX, -1.0f, 1.0f);
            rb.velocity = new Vector3(normalizedX * maxSteerVelocity, 0, rb.velocity.z);
        }
        else
        {
            rb.velocity = Vector3.Lerp(rb.velocity, new Vector3(0, 0, rb.velocity.z), Time.fixedDeltaTime * 3);
        }
    }

    void UpdateCarAudio()
    {
        if (!isPlayer)
            return;

        if (isCrashed)
        {
            if (carEngineAS != null && carEngineAS.isPlaying)
                carEngineAS.Stop();
            return;
        }

        if (!audioInitialized || carEngineAS == null)
            return;

        if (!carEngineAS.isPlaying && carEngineAS.clip != null)
            carEngineAS.Play();

        float carMaxSpeedPercentage = Mathf.Clamp01(rb.velocity.z / maxForwardVelocity);

        if (carPitchAnimationCurve != null && carPitchAnimationCurve.keys.Length > 0)
            carEngineAS.pitch = carPitchAnimationCurve.Evaluate(carMaxSpeedPercentage);
        else
            carEngineAS.pitch = 0.7f + carMaxSpeedPercentage * 0.8f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.isTrigger)
            return;

        if (collision.gameObject == gameObject)
            return;

        if (isCrashed)
            return;

        if (collision.gameObject.CompareTag("Wall"))
            return;

        if (collision.gameObject.name.Contains("Cube") ||
            collision.gameObject.name.Contains("Wall") ||
            collision.gameObject.name.Contains("Barrier"))
            return;

        if (collision.gameObject.CompareTag("Road"))
            return;

        float crashSpeed = collision.relativeVelocity.magnitude;

        if (crashSpeed > crashSpeedThreshold)
            Crash();
    }

    void Crash()
    {
        if (isCrashed) return;

        isCrashed = true;

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (carEngineAS != null)
        {
            carEngineAS.Stop();
            carEngineAS.enabled = false;
            carEngineAS.volume = 0f;
        }

        this.enabled = false;
    }

    public void RepairCar()
    {
        isCrashed = false;
        this.enabled = true;

        if (isPlayer && carEngineAS != null)
        {
            carEngineAS.enabled = true;
            carEngineAS.volume = originalVolume;
            carEngineAS.Play();
            audioInitialized = true;
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

    public bool IsCrashed()
    {
        return isCrashed;
    }

    public void StopCarAudio()
    {
        if (carEngineAS != null)
        {
            carEngineAS.Stop();
            carEngineAS.enabled = false;
            carEngineAS.volume = 0f;
        }
    }

    public float GetCurrentMinSpeed()
    {
        return currentMinSpeed;
    }
    
}