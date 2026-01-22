// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure;
using Azure.Data.Tables;
using System.Text.Json;

namespace ChatGptSampleApp;

/// <summary>
/// Repository interface for workout data persistence (synchronous)
/// </summary>
public interface IWorkoutRepository
{
    void SaveWorkout(WorkoutSession workout);
    List<WorkoutSession> GetWorkouts(int days, string? workoutType);
    List<PersonalRecord> GetPersonalRecords(string? exerciseName);
    void SaveUserProfile(UserProfile profile);
    UserProfile? GetUserProfile();
    List<WorkoutSession> SearchWorkouts(string query);
}

/// <summary>
/// Azure Table Storage implementation of workout repository (synchronous).
/// Uses a combination of Table Storage for structured data.
/// </summary>
public class AzureTableWorkoutRepository : IWorkoutRepository
{
    private readonly TableClient _workoutsTable;
    private readonly TableClient _profileTable;
    private readonly TableClient _prsTable;
    private readonly string _userId;

    public AzureTableWorkoutRepository(string connectionString, string userId)
    {
        _userId = userId;

        var serviceClient = new TableServiceClient(connectionString);

        _workoutsTable = serviceClient.GetTableClient("Workouts");
        _workoutsTable.CreateIfNotExists();

        _profileTable = serviceClient.GetTableClient("UserProfiles");
        _profileTable.CreateIfNotExists();

        _prsTable = serviceClient.GetTableClient("PersonalRecords");
        _prsTable.CreateIfNotExists();
    }

    public void SaveWorkout(WorkoutSession workout)
    {
        var entity = new WorkoutEntity
        {
            PartitionKey = _userId,
            RowKey = workout.Id,
            Date = workout.Date,
            Type = workout.Type,
            DurationMinutes = workout.DurationMinutes,
            ExercisesJson = JsonSerializer.Serialize(workout.Exercises),
            PerceivedEffort = workout.PerceivedEffort,
            EnergyLevel = workout.EnergyLevel,
            Notes = workout.Notes
        };

        _workoutsTable.UpsertEntity(entity);

        // Update PRs if needed
        UpdatePersonalRecords(workout);
    }

    public List<WorkoutSession> GetWorkouts(int days, string? workoutType)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);
        var workouts = new List<WorkoutSession>();

        var query = _workoutsTable.Query<WorkoutEntity>(
            filter: $"PartitionKey eq '{_userId}' and Date ge datetime'{cutoffDate:O}'");

        foreach (var entity in query)
        {
            if (workoutType != null && !entity.Type.Equals(workoutType, StringComparison.OrdinalIgnoreCase))
                continue;

            workouts.Add(MapToWorkoutSession(entity));
        }

        return workouts.OrderByDescending(w => w.Date).ToList();
    }

    public List<PersonalRecord> GetPersonalRecords(string? exerciseName)
    {
        var prs = new List<PersonalRecord>();

        var filter = $"PartitionKey eq '{_userId}'";
        if (!string.IsNullOrEmpty(exerciseName))
        {
            filter += $" and ExerciseName eq '{exerciseName}'";
        }

        var query = _prsTable.Query<PersonalRecordEntity>(filter: filter);

        foreach (var entity in query)
        {
            prs.Add(new PersonalRecord
            {
                ExerciseName = entity.ExerciseName,
                Weight = entity.Weight,
                Reps = entity.Reps,
                EstimatedOneRepMax = entity.EstimatedOneRepMax,
                DateAchieved = entity.DateAchieved
            });
        }

        return prs.OrderByDescending(p => p.EstimatedOneRepMax).ToList();
    }

    public void SaveUserProfile(UserProfile profile)
    {
        var entity = new UserProfileEntity
        {
            PartitionKey = "profiles",
            RowKey = _userId,
            GoalsJson = JsonSerializer.Serialize(profile.Goals),
            EquipmentJson = JsonSerializer.Serialize(profile.AvailableEquipment),
            WorkoutDaysPerWeek = profile.WorkoutDaysPerWeek,
            SessionDurationMinutes = profile.SessionDurationMinutes,
            InjuriesJson = JsonSerializer.Serialize(profile.Injuries),
            ExperienceLevel = profile.ExperienceLevel,
            WeightUnit = profile.WeightUnit
        };

        _profileTable.UpsertEntity(entity);
    }

    public UserProfile? GetUserProfile()
    {
        try
        {
            var response = _profileTable.GetEntity<UserProfileEntity>("profiles", _userId);
            var entity = response.Value;

            return new UserProfile
            {
                Goals = JsonSerializer.Deserialize<List<string>>(entity.GoalsJson) ?? new(),
                AvailableEquipment = JsonSerializer.Deserialize<List<string>>(entity.EquipmentJson) ?? new(),
                WorkoutDaysPerWeek = entity.WorkoutDaysPerWeek,
                SessionDurationMinutes = entity.SessionDurationMinutes,
                Injuries = JsonSerializer.Deserialize<List<string>>(entity.InjuriesJson) ?? new(),
                ExperienceLevel = entity.ExperienceLevel,
                WeightUnit = entity.WeightUnit
            };
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public List<WorkoutSession> SearchWorkouts(string query)
    {
        var allWorkouts = GetWorkouts(365, null); // Search last year
        var queryLower = query.ToLowerInvariant();

        return allWorkouts.Where(w =>
            w.Type.ToLowerInvariant().Contains(queryLower) ||
            w.Notes?.ToLowerInvariant().Contains(queryLower) == true ||
            w.Exercises.Any(e => e.Name.ToLowerInvariant().Contains(queryLower))
        ).ToList();
    }

    private void UpdatePersonalRecords(WorkoutSession workout)
    {
        foreach (var exercise in workout.Exercises)
        {
            foreach (var set in exercise.Sets)
            {
                var estimated1RM = Calculate1RM(set.Weight, set.Reps);

                try
                {
                    var existing = _prsTable.GetEntity<PersonalRecordEntity>(
                        _userId, exercise.Name.ToLowerInvariant().Replace(" ", "-"));

                    if (estimated1RM > existing.Value.EstimatedOneRepMax)
                    {
                        SavePR(exercise.Name, set, estimated1RM, workout.Date);
                    }
                }
                catch (RequestFailedException ex) when (ex.Status == 404)
                {
                    // No existing PR, save this one
                    SavePR(exercise.Name, set, estimated1RM, workout.Date);
                }
            }
        }
    }

    private void SavePR(string exerciseName, ExerciseSet set, double estimated1RM, DateTime date)
    {
        var entity = new PersonalRecordEntity
        {
            PartitionKey = _userId,
            RowKey = exerciseName.ToLowerInvariant().Replace(" ", "-"),
            ExerciseName = exerciseName,
            Weight = set.Weight,
            Reps = set.Reps,
            EstimatedOneRepMax = estimated1RM,
            DateAchieved = date
        };

        _prsTable.UpsertEntity(entity);
    }

    private double Calculate1RM(double weight, int reps)
    {
        // Brzycki formula
        if (reps == 1) return weight;
        return weight * (36.0 / (37.0 - reps));
    }

    private WorkoutSession MapToWorkoutSession(WorkoutEntity entity)
    {
        return new WorkoutSession
        {
            Id = entity.RowKey,
            Date = entity.Date,
            Type = entity.Type,
            DurationMinutes = entity.DurationMinutes,
            Exercises = JsonSerializer.Deserialize<List<Exercise>>(entity.ExercisesJson) ?? new(),
            PerceivedEffort = entity.PerceivedEffort,
            EnergyLevel = entity.EnergyLevel,
            Notes = entity.Notes
        };
    }
}

#region Table Entities

public class WorkoutEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public string ExercisesJson { get; set; } = "[]";
    public int PerceivedEffort { get; set; }
    public int EnergyLevel { get; set; }
    public string? Notes { get; set; }
}

public class UserProfileEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string GoalsJson { get; set; } = "[]";
    public string EquipmentJson { get; set; } = "[]";
    public int WorkoutDaysPerWeek { get; set; }
    public int SessionDurationMinutes { get; set; }
    public string InjuriesJson { get; set; } = "[]";
    public string ExperienceLevel { get; set; } = "Intermediate";
    public string WeightUnit { get; set; } = "lbs";
}

public class PersonalRecordEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string ExerciseName { get; set; } = string.Empty;
    public double Weight { get; set; }
    public int Reps { get; set; }
    public double EstimatedOneRepMax { get; set; }
    public DateTime DateAchieved { get; set; }
}

#endregion
