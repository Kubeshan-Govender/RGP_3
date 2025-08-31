using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StandingsUI : MonoBehaviour
{
    public TextMeshProUGUI standingsText;
    void Start()
    {
        if (standingsText == null)
            Debug.LogError("StandingsUI: standingsText is not assigned!");
    }


    void Update()
    {
        if (RaceManager.Instance == null || RaceManager.Instance.Leaderboard == null)
            return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== Race Standings ===");

        // Check if all cars have finished the race
        bool raceFinished = RaceManager.Instance.Leaderboard.All(car => car.HasFinished);

        var carsToDisplay = raceFinished
        ? RaceManager.Instance.Leaderboard.OrderBy(car => car.FinalRacePosition).ToList()  // convert to List<CarScript>
        : RaceManager.Instance.Leaderboard;


        int rank = 1;
        foreach (var car in carsToDisplay)
        {
            string status;

            if (car.HasFinished)
                status = $"FINISHED - Position #{car.FinalRacePosition}";
            else if (car.IsInPit)
                status = $"IN PIT - {car.ElapsedPitTime:F1}s";
            else
                status = $"RACING - PIT TIME: {car.ElapsedPitTime:F1}s";

            sb.AppendLine($"{rank}. {car.name} | Lap: {car.LapsCompleted} | {status}");
            rank++;
        }

        standingsText.text = sb.ToString();
    }

}
