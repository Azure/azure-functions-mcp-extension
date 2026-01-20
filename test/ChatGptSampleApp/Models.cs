

namespace ChatGptSampleApp;

/// <summary>
/// Represents a single workout session
/// </summary>
public class WorkoutSession
{
    public string Id { get; set; } = string.Empty;
    public DateTime Date { get; set; }

    /// <summary>
    /// Type of workout: Push, Pull, Legs, Upper, Lower, Full Body, Cardio, etc.
    /// </summary>
    public string Type { get; set; } = "General";

    public int DurationMinutes { get; set; }
    public List<Exercise> Exercises { get; set; } = new();

    /// <summary>
    /// Rate of Perceived Exertion (1-10)
    /// </summary>
    public int PerceivedEffort { get; set; } = 5;

    /// <summary>
    /// How energized the user felt (1-10)
    /// </summary>
    public int EnergyLevel { get; set; } = 5;

    public string? Notes { get; set; }
}

/// <summary>
/// Represents an exercise within a workout
/// </summary>
public class Exercise
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Primary muscle group: Chest, Back, Shoulders, Biceps, Triceps, Legs, Core, etc.
    /// </summary>
    public string MuscleGroup { get; set; } = string.Empty;

    public List<ExerciseSet> Sets { get; set; } = new();
    public string? Notes { get; set; }
}

/// <summary>
/// Represents a single set of an exercise
/// </summary>
public class ExerciseSet
{
    public int SetNumber { get; set; }
    public int Reps { get; set; }

    /// <summary>
    /// Weight in user's preferred unit (lbs or kg)
    /// </summary>
    public double Weight { get; set; }

    /// <summary>
    /// Rate of Perceived Exertion for this specific set (1-10)
    /// </summary>
    public int? Rpe { get; set; }

    /// <summary>
    /// Whether this was a PR attempt
    /// </summary>
    public bool IsPR { get; set; }
}

/// <summary>
/// User's fitness profile for personalized recommendations
/// </summary>
public class UserProfile
{
    /// <summary>
    /// Fitness goals: Strength, Hypertrophy, Endurance, Weight Loss, General Fitness
    /// </summary>
    public List<string> Goals { get; set; } = new() { "General Fitness" };

    /// <summary>
    /// Available equipment: Barbell, Dumbbells, Cables, Machines, Bodyweight Only, etc.
    /// </summary>
    public List<string> AvailableEquipment { get; set; } = new();

    public int WorkoutDaysPerWeek { get; set; } = 4;
    public int SessionDurationMinutes { get; set; } = 60;

    /// <summary>
    /// Any injuries or limitations to work around
    /// </summary>
    public List<string> Injuries { get; set; } = new();

    /// <summary>
    /// Experience level: Beginner, Intermediate, Advanced
    /// </summary>
    public string ExperienceLevel { get; set; } = "Intermediate";

    /// <summary>
    /// Preferred weight unit: lbs or kg
    /// </summary>
    public string WeightUnit { get; set; } = "lbs";
}

/// <summary>
/// Personal record for an exercise
/// </summary>
public class PersonalRecord
{
    public string ExerciseName { get; set; } = string.Empty;
    public double Weight { get; set; }
    public int Reps { get; set; }

    /// <summary>
    /// Estimated one-rep max
    /// </summary>
    public double EstimatedOneRepMax { get; set; }

    public DateTime DateAchieved { get; set; }
}

/// <summary>
/// Summary of workouts over a period
/// </summary>
public class WorkoutSummary
{
    public int TotalSessions { get; set; }
    public double TotalVolume { get; set; }
    public int AverageSessionDuration { get; set; }
    public List<string> MostTrainedMuscleGroups { get; set; } = new();
    public double AveragePerceivedEffort { get; set; }
    public Dictionary<string, int> WorkoutTypes { get; set; } = new();
}

/// <summary>
/// Comprehensive workout statistics
/// </summary>
public class WorkoutStatistics
{
    public int TotalWorkouts { get; set; }
    public double WorkoutsPerWeek { get; set; }
    public double TotalVolumeLifted { get; set; }
    public int UniqueExercises { get; set; }

    /// <summary>
    /// Consistency score 0-100
    /// </summary>
    public double ConsistencyScore { get; set; }

    public Dictionary<string, int> MuscleGroupDistribution { get; set; } = new();
    public List<WeeklyData> WeeklyTrend { get; set; } = new();

    /// <summary>
    /// Fatigue indicator based on recent perceived effort and energy levels
    /// </summary>
    public string RecentFatigue { get; set; } = "Unknown";
}

/// <summary>
/// Weekly aggregated data for trends
/// </summary>
public class WeeklyData
{
    public int Week { get; set; }
    public int Workouts { get; set; }
    public double TotalVolume { get; set; }
}
