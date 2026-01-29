// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ChatGptSampleApp;

/// <summary>
/// Repository interface for workout data storage and retrieval.
/// Supports both interactive workout sessions and historical workout data.
/// </summary>
public interface IWorkoutRepository
{
    // Template Management
    List<WorkoutTemplate> GetWorkoutTemplates();
    WorkoutTemplate? GetWorkoutTemplateById(string templateId);
    void SaveWorkoutTemplate(WorkoutTemplate template);

    // Active Workout Session Management
    void SaveActiveWorkoutSession(ActiveWorkoutSession session);
    ActiveWorkoutSession? GetActiveWorkoutSession();
    void UpdateActiveWorkoutSession(ActiveWorkoutSession session);
    void ClearActiveWorkoutSession();

    // Historical Performance
    ExercisePerformance? GetLastPerformance(string exerciseName);

    // Completed Workouts
    void SaveWorkout(WorkoutSession workout);
    List<WorkoutSession> GetWorkouts(int days, string? typeFilter = null);
    List<WorkoutSession> SearchWorkouts(string query);

    // Statistics
    List<PersonalRecord> GetPersonalRecords(string? exerciseName = null);

    // User Profile
    UserProfile? GetUserProfile();
    void SaveUserProfile(UserProfile profile);
}

// Domain Models

public class WorkoutTemplate
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int EstimatedDurationMinutes { get; set; }
    public List<TemplateExercise> Exercises { get; set; } = new();
}

public class TemplateExercise
{
    public string Name { get; set; } = string.Empty;
    public string MuscleGroup { get; set; } = string.Empty;
    public int TargetSets { get; set; }
    public int TargetReps { get; set; }
    public int RestSeconds { get; set; } = 90;
    public string? Notes { get; set; }
}

public class ActiveWorkoutSession
{
    public string Id { get; set; } = string.Empty;
    public string TemplateId { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public List<ActiveExercise> Exercises { get; set; } = new();
    public int CurrentExerciseIndex { get; set; }
    public bool IsComplete { get; set; }
}

public class ActiveExercise
{
    public string Name { get; set; } = string.Empty;
    public string MuscleGroup { get; set; } = string.Empty;
    public int TargetSets { get; set; }
    public int TargetReps { get; set; }
    public int RestSeconds { get; set; }
    public string? Notes { get; set; }
    public List<ExerciseSet> CompletedSets { get; set; } = new();
}

public class ExercisePerformance
{
    public string ExerciseName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public List<ExerciseSet> Sets { get; set; } = new();
}

public class WorkoutSession
{
    public string Id { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int PerceivedEffort { get; set; }
    public int EnergyLevel { get; set; }
    public string? Notes { get; set; }
    public List<Exercise> Exercises { get; set; } = new();
}

public class Exercise
{
    public string Name { get; set; } = string.Empty;
    public string MuscleGroup { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<ExerciseSet> Sets { get; set; } = new();
}

public class ExerciseSet
{
    public int Reps { get; set; }
    public double Weight { get; set; }
    public int? Rpe { get; set; }
    public bool IsPR { get; set; }
}

public class PersonalRecord
{
    public string ExerciseName { get; set; } = string.Empty;
    public double Weight { get; set; }
    public int Reps { get; set; }
    public DateTime DateAchieved { get; set; }
}

public class UserProfile
{
    public List<string> Goals { get; set; } = new();
    public List<string> AvailableEquipment { get; set; } = new();
    public int WorkoutDaysPerWeek { get; set; } = 4;
    public int SessionDurationMinutes { get; set; } = 60;
    public List<string> Injuries { get; set; } = new();
    public string ExperienceLevel { get; set; } = "Intermediate";
    public string WeightUnit { get; set; } = "lbs";
}

// Statistics Models

public class WorkoutSummary
{
    public int TotalSessions { get; set; }
    public double TotalVolume { get; set; }
    public int AverageSessionDuration { get; set; }
    public List<string> MostTrainedMuscleGroups { get; set; } = new();
    public double AveragePerceivedEffort { get; set; }
    public Dictionary<string, int> WorkoutTypes { get; set; } = new();
}

public class WorkoutStatistics
{
    public int TotalWorkouts { get; set; }
    public double WorkoutsPerWeek { get; set; }
    public double TotalVolumeLifted { get; set; }
    public int UniqueExercises { get; set; }
    public double ConsistencyScore { get; set; }
    public Dictionary<string, int> MuscleGroupDistribution { get; set; } = new();
    public List<WeeklyData> WeeklyTrend { get; set; } = new();
    public string RecentFatigue { get; set; } = "Rested";
    public List<string> ExerciseNames { get; set; } = new();
    public List<ExerciseProgressionData> ExerciseProgressions { get; set; } = new();
    public List<PersonalRecordSummary> PersonalRecords { get; set; } = new();
}

public class WeeklyData
{
    public int Week { get; set; }
    public int Workouts { get; set; }
    public double TotalVolume { get; set; }
}

public class ExerciseProgressionData
{
    public string ExerciseName { get; set; } = string.Empty;
    public string MuscleGroup { get; set; } = string.Empty;
    public List<ExerciseProgressionPoint> DataPoints { get; set; } = new();
    public double StartingWeight { get; set; }
    public double CurrentWeight { get; set; }
    public double ProgressPercent { get; set; }
}

public class ExerciseProgressionPoint
{
    public DateTime Date { get; set; }
    public double MaxWeight { get; set; }
    public int BestReps { get; set; }
    public double EstimatedOneRepMax { get; set; }
}

public class PersonalRecordSummary
{
    public string ExerciseName { get; set; } = string.Empty;
    public string MuscleGroup { get; set; } = string.Empty;
    public double MaxWeight { get; set; }
    public int Reps { get; set; }
    public double EstimatedOneRepMax { get; set; }
    public DateTime DateAchieved { get; set; }
    public bool IsRecent { get; set; }
}
