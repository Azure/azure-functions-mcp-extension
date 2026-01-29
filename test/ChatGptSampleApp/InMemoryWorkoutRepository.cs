// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ChatGptSampleApp;

/// <summary>
/// In-memory implementation of IWorkoutRepository with predefined workout templates.
/// In production, this would be replaced with a database-backed implementation.
/// </summary>
public class InMemoryWorkoutRepository : IWorkoutRepository
{
    private readonly List<WorkoutTemplate> _templates;
    private readonly List<WorkoutSession> _workouts;
    private ActiveWorkoutSession? _activeSession;
    private UserProfile? _userProfile;

    public InMemoryWorkoutRepository()
    {
        _templates = InitializeDefaultTemplates();
        _workouts = new List<WorkoutSession>();
    }

    public static List<WorkoutTemplate> InitializeDefaultTemplates()
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
                        RestSeconds = 5, // 5 seconds for demo
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

    // Template Management
    public List<WorkoutTemplate> GetWorkoutTemplates() => _templates;

    public WorkoutTemplate? GetWorkoutTemplateById(string templateId) =>
        _templates.FirstOrDefault(t => t.Id == templateId);

    public void SaveWorkoutTemplate(WorkoutTemplate template)
    {
        var existing = _templates.FirstOrDefault(t => t.Id == template.Id);
        if (existing != null)
        {
            _templates.Remove(existing);
        }
        _templates.Add(template);
    }

    // Active Workout Session Management
    public void SaveActiveWorkoutSession(ActiveWorkoutSession session)
    {
        _activeSession = session;
    }

    public ActiveWorkoutSession? GetActiveWorkoutSession() => _activeSession;

    public void UpdateActiveWorkoutSession(ActiveWorkoutSession session)
    {
        _activeSession = session;
    }

    public void ClearActiveWorkoutSession()
    {
        _activeSession = null;
    }

    // Historical Performance
    public ExercisePerformance? GetLastPerformance(string exerciseName)
    {
        // Find the most recent workout that included this exercise
        var lastWorkout = _workouts
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

    // Completed Workouts
    public void SaveWorkout(WorkoutSession workout)
    {
        _workouts.Add(workout);
    }

    public List<WorkoutSession> GetWorkouts(int days, string? typeFilter = null)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);
        var query = _workouts.Where(w => w.Date >= cutoffDate);

        if (!string.IsNullOrEmpty(typeFilter))
        {
            query = query.Where(w => w.Type.Equals(typeFilter, StringComparison.OrdinalIgnoreCase));
        }

        return query.OrderByDescending(w => w.Date).ToList();
    }

    public List<WorkoutSession> SearchWorkouts(string query)
    {
        var lowerQuery = query.ToLowerInvariant();
        return _workouts
            .Where(w =>
                w.Type.ToLowerInvariant().Contains(lowerQuery) ||
                w.Notes?.ToLowerInvariant().Contains(lowerQuery) == true ||
                w.Exercises.Any(e => e.Name.ToLowerInvariant().Contains(lowerQuery)))
            .OrderByDescending(w => w.Date)
            .ToList();
    }

    // Statistics
    public List<PersonalRecord> GetPersonalRecords(string? exerciseName = null)
    {
        var exerciseGroups = _workouts
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

    // User Profile
    public UserProfile? GetUserProfile() => _userProfile;

    public void SaveUserProfile(UserProfile profile)
    {
        _userProfile = profile;
    }
}
