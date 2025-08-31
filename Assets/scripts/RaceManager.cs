using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance;

    private List<CarScript> cars = new List<CarScript>();

    public List<CarScript> Leaderboard { get; private set; } = new List<CarScript>();

    private int nextFinishPosition = 1;

    private bool raceStarted = false;
    public bool RaceStarted => raceStarted;

    public GameObject startButtonGameObject;

    private List<CarScript> readyCars = new List<CarScript>();
    private bool restartInitiated = false;
    public AudioSource backgroundMusic;


    public void ReportFinish(CarScript car)
    {
        if (car.FinalRacePosition != -1) return; // Already assigned

        car.FinalRacePosition = nextFinishPosition;
        nextFinishPosition++;
    }



    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void RegisterCar(CarScript car)
    {
        if (!cars.Contains(car))
            cars.Add(car);
    }

    void Update()
    {
        // Sort by laps (desc), then x-position (asc since more left = ahead)
        Leaderboard = cars
            .OrderByDescending(c => c.LapsCompleted)
            .ThenBy(c => c.transform.position.x)
            .ToList();

        if (raceStarted && cars.All(c => c.HasFinished))
        {
            raceStarted = false;
            ShowStartButton();
            backgroundMusic.loop = false;
        }
    }


    public void StartRace()
    {
        foreach (var car in cars)
        {
            car.ResetCarState();
        }

        raceStarted = true;
        startButtonGameObject.SetActive(false);
        nextFinishPosition = 1;
        Leaderboard.Clear();
    }


    public void ResetCar(CarScript car)
    {
        car.ResetCarState();
    }

    void ShowStartButton()
    {
        // You can toggle your button GameObject active here
        startButtonGameObject.SetActive(true);
    }

    public void NotifyCarReady(CarScript car)
    {
        if (!readyCars.Contains(car))
        {
            readyCars.Add(car);
        }

        // Once all cars are ready, begin countdown
        if (readyCars.Count == cars.Count && !restartInitiated)
        {
            restartInitiated = true;
            StartCoroutine(StartRaceAfterDelay(3f));
        }
    }

    private IEnumerator StartRaceAfterDelay(float delay)
    {
        // Optional: show countdown UI here
        yield return new WaitForSeconds(delay);

        foreach (CarScript car in cars)
        {
            if (car != null)
                car.StartRacing(); // we’ll define this below
        }

        raceStarted = true;
        readyCars.Clear();
        restartInitiated = false;
    }
}

