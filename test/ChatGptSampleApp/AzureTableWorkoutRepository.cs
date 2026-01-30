// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure;
using Azure.Data.Tables;
using System.Text.Json;

namespace ChatGptSampleApp;

/// <summary>
/// Azure Table Storage implementation of IWorkoutRepository.
/// Stores workout data, templates, and user profiles in Azure Tables.
/// </summary>
public class AzureTableWorkoutRepository : IWorkoutRepository
{
    private readonly TableClient _workoutsTable;
    private readonly TableClient _templatesTable;
    private readonly TableClient _activeSessionTable;
    private readonly TableClient _profileTable;
    private readonly string _userId;

    public AzureTableWorkoutRepository(string connectionString, string userId)
    {
        _userId = userId;

        var serviceClient = new TableServiceClient(connectionString);

        // Create tables if they don't exist
        _workoutsTable = serviceClient.GetTableClient("workouts");
        _workoutsTable.CreateIfNotExists();

        _templatesTable = serviceClient.GetTableClient("templates");
        _templatesTable.CreateIfNotExists();

        _activeSessionTable = serviceClient.GetTableClient("activesessions");
        _activeSessionTable.CreateIfNotExists();

        _profileTable = serviceClient.GetTableClient("profiles");
        _profileTable.CreateIfNotExists();

        // Initialize default templates
        InitializeDefaultTemplates();
    }

    #region Template Management

    public List<WorkoutTemplate> GetWorkoutTemplates()
    {
        var templates = new List<WorkoutTemplate>();

        var query = _templatesTable.Query<TableEntity>(
            filter: $"PartitionKey eq 'TEMPLATE'");

        foreach (var entity in query)
        {
            var template = DeserializeFromEntity<WorkoutTemplate>(entity);
            if (template != null)
            {
                templates.Add(template);
            }
        }

        return templates.OrderBy(t => t.Name).ToList();
    }

    public WorkoutTemplate? GetWorkoutTemplateById(string templateId)
    {
        try
        {
            var entity = _templatesTable.GetEntity<TableEntity>("TEMPLATE", templateId);
            return DeserializeFromEntity<WorkoutTemplate>(entity.Value);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public void SaveWorkoutTemplate(WorkoutTemplate template)
    {
        var entity = new TableEntity("TEMPLATE", template.Id);
        SerializeToEntity(template, entity);
        _templatesTable.UpsertEntity(entity);
    }

    private void InitializeDefaultTemplates()
    {
        // Check if templates already exist
        var existingTemplates = GetWorkoutTemplates();
        if (existingTemplates.Any())
        {
            return;
        }

        // Create default templates (Push, Pull, Legs)
        var templates = InMemoryWorkoutRepository.InitializeDefaultTemplates();
        foreach (var template in templates)
        {
            SaveWorkoutTemplate(template);
        }
    }

    #endregion

    #region Active Session Management

    public void SaveActiveWorkoutSession(ActiveWorkoutSession session)
    {
        var entity = new TableEntity(_userId, "ACTIVE");
        SerializeToEntity(session, entity);
        _activeSessionTable.UpsertEntity(entity);
    }

    public ActiveWorkoutSession? GetActiveWorkoutSession()
    {
        try
        {
            var entity = _activeSessionTable.GetEntity<TableEntity>(_userId, "ACTIVE");
            return DeserializeFromEntity<ActiveWorkoutSession>(entity.Value);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public void UpdateActiveWorkoutSession(ActiveWorkoutSession session)
    {
        SaveActiveWorkoutSession(session);
    }

    public void ClearActiveWorkoutSession()
    {
        try
        {
            _activeSessionTable.DeleteEntity(_userId, "ACTIVE");
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Already deleted, ignore
        }
    }

    #endregion

    #region Workout History

    public void SaveWorkout(WorkoutSession workout)
    {
        // Ensure table exists before trying to save
        _workoutsTable.CreateIfNotExists();

        // Use reverse timestamp for RowKey to get most recent first
        var reverseTimestamp = DateTime.MaxValue.Ticks - workout.Date.Ticks;
        var rowKey = $"{reverseTimestamp:D19}_{workout.Id}";

        var entity = new TableEntity(_userId, rowKey);
        SerializeToEntity(workout, entity);
        _workoutsTable.UpsertEntity(entity);
    }

    public List<WorkoutSession> GetWorkouts(int days, string? typeFilter = null)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);
        var cutoffTicks = DateTime.MaxValue.Ticks - cutoffDate.Ticks;

        var filter = $"PartitionKey eq '{_userId}' and RowKey lt '{cutoffTicks:D19}'";

        var workouts = new List<WorkoutSession>();

        try
        {
            _workoutsTable.CreateIfNotExists();
            var query = _workoutsTable.Query<TableEntity>(filter: filter);

            foreach (var entity in query)
            {
                var workout = DeserializeFromEntity<WorkoutSession>(entity);
                if (workout != null)
                {
                    if (typeFilter == null || workout.Type.Equals(typeFilter, StringComparison.OrdinalIgnoreCase))
                    {
                        workouts.Add(workout);
                    }
                }
            }
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Table doesn't exist yet or no data, return empty list
            return workouts;
        }

        return workouts.OrderByDescending(w => w.Date).ToList();
    }

    public List<WorkoutSession> SearchWorkouts(string query)
    {
        var lowerQuery = query.ToLowerInvariant();
        var allWorkouts = GetWorkouts(365); // Search last year

        return allWorkouts
            .Where(w =>
                w.Type.ToLowerInvariant().Contains(lowerQuery) ||
                w.Notes?.ToLowerInvariant().Contains(lowerQuery) == true ||
                w.Exercises.Any(e => e.Name.ToLowerInvariant().Contains(lowerQuery)))
            .OrderByDescending(w => w.Date)
            .ToList();
    }

    #endregion

    #region Performance and Records

    public ExercisePerformance? GetLastPerformance(string exerciseName)
    {
        var workouts = GetWorkouts(90); // Look back 90 days

        var lastWorkout = workouts
            .Where(w => w.Exercises.Any(e =>
                e.Name.Equals(exerciseName, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(w => w.Date)
            .FirstOrDefault();

        if (lastWorkout == null) return null;

        var exercise = lastWorkout.Exercises
            .First(e => e.Name.Equals(exerciseName, StringComparison.OrdinalIgnoreCase));

        return new ExercisePerformance
        {
            ExerciseName = exercise.Name,
            Date = lastWorkout.Date,
            Sets = exercise.Sets
        };
    }

    public List<PersonalRecord> GetPersonalRecords(string? exerciseName = null)
    {
        var workouts = GetWorkouts(365); // Last year

        var exerciseGroups = workouts
            .SelectMany(w => w.Exercises.Select(e => new { Workout = w, Exercise = e }))
            .SelectMany(x => x.Exercise.Sets.Select(s => new
            {
                x.Workout.Date,
                x.Exercise.Name,
                Set = s
            }));

        if (!string.IsNullOrEmpty(exerciseName))
        {
            exerciseGroups = exerciseGroups
                .Where(x => x.Name.Equals(exerciseName, StringComparison.OrdinalIgnoreCase));
        }

        var prs = exerciseGroups
            .GroupBy(x => x.Name)
            .Select(g =>
            {
                var best = g.OrderByDescending(x => x.Set.Weight).First();
                return new PersonalRecord
                {
                    ExerciseName = best.Name,
                    Weight = best.Set.Weight,
                    Reps = best.Set.Reps,
                    DateAchieved = best.Date
                };
            })
            .OrderByDescending(pr => pr.Weight)
            .ToList();

        return prs;
    }

    #endregion

    #region User Profile

    public UserProfile? GetUserProfile()
    {
        try
        {
            var entity = _profileTable.GetEntity<TableEntity>(_userId, "PROFILE");
            return DeserializeFromEntity<UserProfile>(entity.Value);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public void SaveUserProfile(UserProfile profile)
    {
        var entity = new TableEntity(_userId, "PROFILE");
        SerializeToEntity(profile, entity);
        _profileTable.UpsertEntity(entity);
    }

    #endregion

    #region Serialization Helpers

    private void SerializeToEntity<T>(T obj, TableEntity entity)
    {
        var json = JsonSerializer.Serialize(obj);
        entity["Data"] = json;
    }

    private T? DeserializeFromEntity<T>(TableEntity entity)
    {
        if (!entity.TryGetValue("Data", out var dataObj))
        {
            return default;
        }

        var json = dataObj?.ToString();
        if (string.IsNullOrEmpty(json))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(json);
    }

    #endregion
}

// Extension class for InMemoryWorkoutRepository to expose default templates
public static class InMemoryWorkoutRepositoryExtensions
{
    public static List<WorkoutTemplate> CreateDefaultTemplates()
    {
        return new List<WorkoutTemplate>
        {
            new WorkoutTemplate
            {
                Id = "push-1",
                Name = "Push Day",
                Type = "Push",
                Description = "Chest, shoulders, and triceps focused workout",
                EstimatedDurationMinutes = 60,
                Exercises = new List<TemplateExercise>
                {
                    new TemplateExercise
                    {
                        Name = "Bench Press",
                        MuscleGroup = "Chest",
                        TargetSets = 4,
                        TargetReps = 8,
                        RestSeconds = 5,
                        Notes = "Focus on controlled descent and explosive press"
                    },
                    new TemplateExercise
                    {
                        Name = "Incline Dumbbell Press",
                        MuscleGroup = "Chest",
                        TargetSets = 3,
                        TargetReps = 10,
                        RestSeconds = 5,
                        Notes = "Upper chest emphasis, 30-45 degree incline"
                    },
                    new TemplateExercise
                    {
                        Name = "Overhead Press",
                        MuscleGroup = "Shoulders",
                        TargetSets = 4,
                        TargetReps = 8,
                        RestSeconds = 5,
                        Notes = "Standing or seated, maintain core stability"
                    },
                    new TemplateExercise
                    {
                        Name = "Lateral Raises",
                        MuscleGroup = "Shoulders",
                        TargetSets = 3,
                        TargetReps = 12,
                        RestSeconds = 5,
                        Notes = "Light weight, strict form, focus on side delts"
                    },
                    new TemplateExercise
                    {
                        Name = "Tricep Dips",
                        MuscleGroup = "Triceps",
                        TargetSets = 3,
                        TargetReps = 10,
                        RestSeconds = 5,
                        Notes = "Bodyweight or weighted, lean forward slightly"
                    },
                    new TemplateExercise
                    {
                        Name = "Skull Crushers",
                        MuscleGroup = "Triceps",
                        TargetSets = 3,
                        TargetReps = 12,
                        RestSeconds = 5,
                        Notes = "Keep elbows stationary, full range of motion"
                    }
                }
            },
            new WorkoutTemplate
            {
                Id = "pull-1",
                Name = "Pull Day",
                Type = "Pull",
                Description = "Back, biceps, and rear delts focused workout",
                EstimatedDurationMinutes = 60,
                Exercises = new List<TemplateExercise>
                {
                    new TemplateExercise
                    {
                        Name = "Deadlift",
                        MuscleGroup = "Back",
                        TargetSets = 4,
                        TargetReps = 6,
                        RestSeconds = 5,
                        Notes = "Heavy compound movement, maintain neutral spine"
                    },
                    new TemplateExercise
                    {
                        Name = "Pull-Ups",
                        MuscleGroup = "Back",
                        TargetSets = 4,
                        TargetReps = 8,
                        RestSeconds = 5,
                        Notes = "Wide grip, full extension at bottom"
                    },
                    new TemplateExercise
                    {
                        Name = "Barbell Row",
                        MuscleGroup = "Back",
                        TargetSets = 4,
                        TargetReps = 10,
                        RestSeconds = 5,
                        Notes = "Pull to lower chest, squeeze at top"
                    },
                    new TemplateExercise
                    {
                        Name = "Face Pulls",
                        MuscleGroup = "Shoulders",
                        TargetSets = 3,
                        TargetReps = 15,
                        RestSeconds = 5,
                        Notes = "Rear delts and upper back, pull to face level"
                    },
                    new TemplateExercise
                    {
                        Name = "Barbell Curl",
                        MuscleGroup = "Biceps",
                        TargetSets = 3,
                        TargetReps = 10,
                        RestSeconds = 5,
                        Notes = "Strict form, no momentum, squeeze at top"
                    },
                    new TemplateExercise
                    {
                        Name = "Hammer Curls",
                        MuscleGroup = "Biceps",
                        TargetSets = 3,
                        TargetReps = 12,
                        RestSeconds = 5,
                        Notes = "Neutral grip, targets brachialis and forearms"
                    }
                }
            },
            new WorkoutTemplate
            {
                Id = "legs-1",
                Name = "Leg Day",
                Type = "Legs",
                Description = "Comprehensive lower body workout",
                EstimatedDurationMinutes = 70,
                Exercises = new List<TemplateExercise>
                {
                    new TemplateExercise
                    {
                        Name = "Back Squat",
                        MuscleGroup = "Legs",
                        TargetSets = 4,
                        TargetReps = 8,
                        RestSeconds = 5,
                        Notes = "King of leg exercises, go deep with good form"
                    },
                    new TemplateExercise
                    {
                        Name = "Romanian Deadlift",
                        MuscleGroup = "Legs",
                        TargetSets = 4,
                        TargetReps = 10,
                        RestSeconds = 5,
                        Notes = "Hamstring focus, feel the stretch"
                    },
                    new TemplateExercise
                    {
                        Name = "Leg Press",
                        MuscleGroup = "Legs",
                        TargetSets = 3,
                        TargetReps = 12,
                        RestSeconds = 5,
                        Notes = "Feet shoulder-width, push through heels"
                    },
                    new TemplateExercise
                    {
                        Name = "Walking Lunges",
                        MuscleGroup = "Legs",
                        TargetSets = 3,
                        TargetReps = 10,
                        RestSeconds = 5,
                        Notes = "10 reps per leg, maintain upright posture"
                    },
                    new TemplateExercise
                    {
                        Name = "Leg Curl",
                        MuscleGroup = "Legs",
                        TargetSets = 3,
                        TargetReps = 12,
                        RestSeconds = 5,
                        Notes = "Hamstring isolation, controlled movement"
                    },
                    new TemplateExercise
                    {
                        Name = "Calf Raises",
                        MuscleGroup = "Legs",
                        TargetSets = 4,
                        TargetReps = 15,
                        RestSeconds = 5,
                        Notes = "Full range of motion, squeeze at top"
                    }
                }
            }
        };
    }
}
