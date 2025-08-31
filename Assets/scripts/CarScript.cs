using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class CarScript : MonoBehaviour
{
    public float moveSpeed = 50f;
    private float screenLeftEdge;
    private float screenRightEdge;

    public Transform pitEntryPoint;     // Where the car turns into the pit lane
    public Transform pitBoxPosition;    // Where the car parks in the pit lane
    public Transform pitExitPoint;      // Where the car rejoins the track

    private enum CarState { Racing, EnteringPit, InPit, ExitingPit, Parked, Resetting, Ready }
    private CarState currentState = CarState.Racing;

    private int lapsCompleted = 0;
    private int totalLaps = 52;
    private int[] pitstopLaps;
    private float pitstopTimer = 0f;

    public float pitMoveSpeed = 20f; // slower movement in pit

    public float pitY = 3.9f;             // Y coordinate for pit lane
    public float trackY = 0f;             // Y coordinate for the main track
    public float pitStopX;                // Target X position in the pit
    public float pitSpeed = 20f;          // Speed in pit lane
    private Vector3 originalScale;        // Store original scale

    public int LapsCompleted => lapsCompleted;
    public bool IsInPit => currentState == CarState.InPit || currentState == CarState.EnteringPit;
    public float LastPitStopDuration { get; private set; } = 0f;
    public bool HasFinished { get; private set; } = false;
    public int FinalRacePosition { get; set; } = -1;

    private Vector3 finalPitPosition;
    private bool isMovingToPark = false;
    private float elapsedPitTime = 0f;
    public float ElapsedPitTime => elapsedPitTime;
    private bool wasInPit = false;
    public AudioSource sharedCarSound;
    public AudioSource startSound;



    void Start()
    {
        originalScale = transform.localScale;
        transform.localScale = originalScale * 0.5f;

        float distanceFromCamera = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);
        screenLeftEdge = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, distanceFromCamera)).x;
        screenRightEdge = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, distanceFromCamera)).x;

        // Pick 5 random pitstop laps
        pitstopLaps = new int[5];
        HashSet<int> uniquePitLaps = new HashSet<int>();
        while (uniquePitLaps.Count < 5)
        {
            uniquePitLaps.Add(Random.Range(1, totalLaps + 1));
        }
        pitstopLaps = uniquePitLaps.ToArray();


        if (RaceManager.Instance != null)
            RaceManager.Instance.RegisterCar(this);
        else
            Debug.LogError("RaceManager instance not found!");

    }



    void Update()
    {
        if (RaceManager.Instance == null)
            return;

        // Only return early if the race is not started *and* the car hasn't finished yet
        if (!RaceManager.Instance.RaceStarted && !HasFinished)
            return;

        switch (currentState)
        {
            case CarState.Racing:
                HandleRacing();
                break;

            case CarState.EnteringPit:
                EnterPit();
                break;

            case CarState.InPit:
                if (!wasInPit)
                {
                    // Just entered pit
                    elapsedPitTime = 0f;
                    wasInPit = true;
                }

                elapsedPitTime += Time.deltaTime;
                pitstopTimer -= Time.deltaTime;

                // Move toward the assigned pitStopX
                if (transform.position.x > pitStopX)
                {
                    transform.Translate(Vector3.left * pitSpeed * Time.deltaTime);
                }

                if (pitstopTimer <= 0f && transform.position.x <= pitStopX + 0.1f)
                {
                    currentState = CarState.ExitingPit;
                }

                break;


            case CarState.ExitingPit:
                ExitPit();
                break;

            case CarState.Parked:
                if (isMovingToPark)
                {
                    HandleParked();
                }
                break;
            case CarState.Resetting:
                ResetCar();
                break;
            case CarState.Ready:
                break;

        }

    }


    void HandleRacing()
    {
        transform.Translate(Vector3.left * moveSpeed * Time.deltaTime);

        if (transform.position.x < screenLeftEdge - 5)
        {
            Vector3 newPosition = transform.position;
            newPosition.x = screenRightEdge + 5;
            transform.position = newPosition;
            sharedCarSound.Play();
            lapsCompleted++;

            if (System.Array.Exists(pitstopLaps, lap => lap == lapsCompleted))
            {
                currentState = CarState.EnteringPit;
            }

            if (lapsCompleted >= totalLaps)
            {
                FinishRace();
            }
        }
    }

    void EnterPit()
    {
        // Move off-screen to the right, at pitY and z = 3
        transform.position = new Vector3(screenRightEdge + 5f, pitY, 3f);
        transform.localScale = originalScale * 0.5f;

        currentState = CarState.InPit;
        pitstopTimer = Random.Range(1.8f, 3f);
        LastPitStopDuration = pitstopTimer;
        elapsedPitTime = 0f;
    }

    void ExitPit()
    {
        float step = pitSpeed * Time.deltaTime;

        // Phase 1: Drive left off-screen in pit lane (at pitY, z = 3)
        if (transform.position.x > screenLeftEdge - 5f && transform.position.y == pitY)
        {
            transform.Translate(Vector3.left * step);
        }
        // Phase 2: Teleport to right side at trackY, z = 0
        else if (transform.position.y == pitY)
        {
            transform.position = new Vector3(screenRightEdge + 5f, trackY, 0f);
        }
        // Phase 3: Drive left into screen on the track
        else if (transform.position.y == trackY)
        {
            transform.Translate(Vector3.left * step);

            // When car is back on screen, resume racing
            if (transform.position.x <= screenRightEdge)
            {
                transform.localScale = originalScale;
                currentState = CarState.Racing;
                wasInPit = false;
            }
        }
    }

    void HandleParked()
    {
        float step = pitSpeed * Time.deltaTime;

        // When close enough to final position, stop moving

        if (transform.position.x > pitStopX)
        {
            transform.Translate(Vector3.left * pitSpeed * Time.deltaTime);
        }

        if (Vector3.Distance(transform.position, new Vector3(pitStopX, pitY, 3f)) < 0.1f)
        {
            isMovingToPark = false;
        }
    }

    void FinishRace()
    {
        if (HasFinished) return;

        HasFinished = true;
        RaceManager.Instance.ReportFinish(this);

        currentState = CarState.Parked;

        finalPitPosition = new Vector3(pitStopX, pitY, transform.position.z);
        isMovingToPark = true;
        transform.position = new Vector3(screenRightEdge + 5f, pitY, 3f);
        transform.localScale = originalScale * 0.5f;
    }





    public void ResetCarState()
    {
        currentState = CarState.Resetting;
        elapsedPitTime = 0f;
        lapsCompleted = 0;
        HasFinished = false;
        FinalRacePosition = -1;
        enabled = true; // make sure Update() runs

    }


    void ResetCar()
    {
        float step = pitSpeed * Time.deltaTime;

        // Phase 1: Drive left off-screen in pit lane (at pitY, z = 3)
        if (transform.position.x > screenLeftEdge - 5f && transform.position.y == pitY)
        {
            transform.Translate(Vector3.left * step);
        }
        // Phase 2: Teleport to right side at trackY, z = 0
        else if (transform.position.y == pitY)
        {
            transform.position = new Vector3(screenRightEdge + 5f, trackY, 0f);
            transform.localScale = originalScale;
        }
        // Phase 3: Drive left into screen on the track
        else if (transform.position.y == trackY)
        {
            // When car is back on screen, resume racing
            if (transform.position.x <= 0f)
            {
                currentState = CarState.Ready;
                startSound.Play();
                RaceManager.Instance.NotifyCarReady(this);
            }
            else { transform.Translate(Vector3.left * step); }

        }

    }
    public void StartRacing()
    {
        currentState = CarState.Racing;
    }


}



