// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatGptSampleApp;

/// <summary>
/// MCP Functions for tracking workouts and providing AI-powered recommendations.
/// Integrates with ChatGPT to analyze workout history and tailor future sessions.
/// </summary>
public class WorkoutFunctions
{
    private readonly ILogger<WorkoutFunctions> _logger;
    private readonly IWorkoutRepository _workoutRepo;

    public WorkoutFunctions(ILogger<WorkoutFunctions> logger, IWorkoutRepository workoutRepo)
    {
        _logger = logger;
        _workoutRepo = workoutRepo;
    }

    #region Resources - UI Widgets

    [Function(nameof(GetDashboardWidget))]
    public string GetDashboardWidget(
        [McpResourceTrigger(
            "ui://widget/dashboard.html",
            "Workout Dashboard",
            MimeType = "text/html+skybridge",
            Description = "Interactive dashboard to view workout history and log new sessions")]
        [McpResourceMetadata("openai/widgetPrefersBorder", true)]
        [McpResourceMetadata("openai/widgetDomain", "https://chatgpt.com")]
        [McpResourceMetadata("openai/widgetCSP", "{\"connect_domains\":[],\"resource_domains\":[]}")]
        ResourceInvocationContext context)
    {
        var file = Path.Combine(AppContext.BaseDirectory, "resources", "dashboard.html");
        return File.ReadAllText(file);
    }

    [Function(nameof(GetLogWorkoutWidget))]
    public string GetLogWorkoutWidget(
        [McpResourceTrigger(
            "ui://widget/log-workout.html",
            "Log Workout Form",
            MimeType = "text/html+skybridge",
            Description = "Form to log a new workout session with exercises, sets, reps, and notes")]
        [McpResourceMetadata("openai/widgetPrefersBorder", true)]
        [McpResourceMetadata("openai/widgetDomain", "https://chatgpt.com")]
        [McpResourceMetadata("openai/widgetCSP", "{\"connect_domains\":[],\"resource_domains\":[]}")]
        ResourceInvocationContext context)
    {
        var file = Path.Combine(AppContext.BaseDirectory, "resources", "log-workout.html");
        return File.ReadAllText(file);
    }

    [Function(nameof(GetProgressChartWidget))]
    public string GetProgressChartWidget(
        [McpResourceTrigger(
            "ui://widget/progress-chart.html",
            "Progress Charts",
            MimeType = "text/html+skybridge",
            Description = "Visual charts showing workout volume, frequency, and strength progression")]
        [McpResourceMetadata("openai/widgetPrefersBorder", true)]
        [McpResourceMetadata("openai/widgetDomain", "https://chatgpt.com")]
        [McpResourceMetadata("openai/widgetCSP", "{\"connect_domains\":[],\"resource_domains\":[]}")]
        ResourceInvocationContext context)
    {
        var file = Path.Combine(AppContext.BaseDirectory, "resources", "progress-chart.html");
        return File.ReadAllText(file);
    }

    #endregion

    #region Tools - Workout Management (Synchronous, Return POCOs)

    [Function(nameof(LogWorkout))]
    public LogWorkoutResponse LogWorkout(
        [McpToolTrigger(nameof(LogWorkout),
            "Log a completed workout session. Use this after the user describes a workout they just completed.")]
        LogWorkoutRequest request,
        ToolInvocationContext context)
    {
        var workout = new WorkoutSession
        {
            Id = Guid.NewGuid().ToString(),
            Date = request.Date?.ToUniversalTime() ?? DateTime.UtcNow,
            Type = request.Type.ToString(),
            DurationMinutes = request.DurationMinutes,
            PerceivedEffort = Math.Clamp(request.PerceivedEffort, 1, 10),
            EnergyLevel = Math.Clamp(request.EnergyLevel, 1, 10),
            Notes = request.Notes
        };

        _workoutRepo.SaveWorkout(workout);
        _logger.LogInformation("Logged workout {WorkoutId} on {Date}", workout.Id, workout.Date);

        var totalVolume = workout.Exercises.Sum(e => e.Sets.Sum(s => s.Reps * s.Weight));
        var totalSets = workout.Exercises.Sum(e => e.Sets.Count);

        return new LogWorkoutResponse
        {
            Success = true,
            Message = "Workout logged successfully!",
            WorkoutId = workout.Id,
            Date = workout.Date,
            Type = workout.Type,
            DurationMinutes = workout.DurationMinutes,
            PerceivedEffort = workout.PerceivedEffort,
            EnergyLevel = workout.EnergyLevel,
            Notes = workout.Notes,
            ExerciseCount = workout.Exercises.Count,
            TotalSets = totalSets,
            TotalVolume = totalVolume,
            MuscleGroups = string.Join(", ", workout.Exercises.Select(e => e.MuscleGroup).Distinct()),
            ExercisesJson = JsonSerializer.Serialize(workout.Exercises)
        };
    }

    [Function(nameof(GetWorkoutHistory))]
    public GetWorkoutHistoryResponse GetWorkoutHistory(
        [McpToolTrigger(nameof(GetWorkoutHistory),
            "Retrieve workout history for a specified time period. Returns detailed workout data including exercises, volume, and user notes. Use this to analyze patterns and tailor recommendations.")]
        GetWorkoutHistoryRequest request,
        ToolInvocationContext context)
    {
        var typeFilter = request.WorkoutType?.ToString();
        var workouts = _workoutRepo.GetWorkouts(request.Days, typeFilter);

        _logger.LogInformation("Retrieved {Count} workouts from last {Days} days", workouts.Count, request.Days);

        var summary = GenerateWorkoutSummary(workouts);

        return new GetWorkoutHistoryResponse
        {
            Period = $"Last {request.Days} days",
            TotalWorkouts = workouts.Count,
            WorkoutsJson = JsonSerializer.Serialize(workouts),
            TotalSessions = summary.TotalSessions,
            TotalVolume = summary.TotalVolume,
            AverageSessionDuration = summary.AverageSessionDuration,
            MostTrainedMuscleGroups = string.Join(", ", summary.MostTrainedMuscleGroups),
            AveragePerceivedEffort = summary.AveragePerceivedEffort,
            WorkoutTypesJson = JsonSerializer.Serialize(summary.WorkoutTypes)
        };
    }

    [Function(nameof(GetPersonalRecords))]
    public GetPersonalRecordsResponse GetPersonalRecords(
        [McpToolTrigger(nameof(GetPersonalRecords),
            "Get personal records (PRs) for specific exercises or all exercises. Useful for tracking strength progression and setting goals.")]
        GetPersonalRecordsRequest request,
        ToolInvocationContext context)
    {
        var prs = _workoutRepo.GetPersonalRecords(request.ExerciseName);

        return new GetPersonalRecordsResponse
        {
            PersonalRecordsJson = JsonSerializer.Serialize(prs),
            Count = prs.Count,
            LastUpdated = DateTime.UtcNow
        };
    }

    [Function(nameof(GetWorkoutStats))]
    public WorkoutStatistics GetWorkoutStats(
        [McpToolTrigger(nameof(GetWorkoutStats),
            "Get comprehensive workout statistics including frequency, volume trends, muscle group distribution, and consistency metrics. Essential for generating personalized workout recommendations.")]
        GetWorkoutStatsRequest request,
        ToolInvocationContext context)
    {
        var workouts = _workoutRepo.GetWorkouts(request.Days, null);
        return CalculateStats(workouts, request.Days);
    }

    [Function(nameof(UpdateUserProfile))]
    public UpdateUserProfileResponse UpdateUserProfile(
        [McpToolTrigger(nameof(UpdateUserProfile),
            "Update user's fitness goals, preferences, and constraints. Use this when the user shares information about their goals, available equipment, schedule, or limitations.")]
        UpdateUserProfileRequest request,
        ToolInvocationContext context)
    {
        var profile = new UserProfile
        {
            Goals = request.Goals?.Select(g => g.ToString()).ToList() ?? new List<string> { "GeneralFitness" },
            AvailableEquipment = request.Equipment?.Select(e => e.ToString()).ToList() ?? new List<string>(),
            WorkoutDaysPerWeek = Math.Clamp(request.WorkoutDaysPerWeek, 1, 7),
            SessionDurationMinutes = Math.Clamp(request.SessionDurationMinutes, 15, 180),
            Injuries = request.Injuries?.ToList() ?? new List<string>(),
            ExperienceLevel = request.ExperienceLevel.ToString(),
            WeightUnit = request.WeightUnit.ToString().ToLowerInvariant()
        };

        _workoutRepo.SaveUserProfile(profile);

        return new UpdateUserProfileResponse
        {
            Success = true,
            Message = "Profile updated successfully",
            Goals = string.Join(", ", profile.Goals),
            AvailableEquipment = string.Join(", ", profile.AvailableEquipment),
            WorkoutDaysPerWeek = profile.WorkoutDaysPerWeek,
            SessionDurationMinutes = profile.SessionDurationMinutes,
            Injuries = string.Join(", ", profile.Injuries),
            ExperienceLevel = profile.ExperienceLevel,
            WeightUnit = profile.WeightUnit
        };
    }

    [Function(nameof(GetUserProfile))]
    public UserProfile GetUserProfile(
        [McpToolTrigger(nameof(GetUserProfile),
            "Retrieve user's fitness profile including goals, equipment, constraints, and preferences. Use this before generating workout recommendations.")]
        ToolInvocationContext context)
    {
        var profile = _workoutRepo.GetUserProfile();
        return profile ?? new UserProfile();
    }

    [Function(nameof(SearchWorkouts))]
    public SearchWorkoutsResponse SearchWorkouts(
        [McpToolTrigger(nameof(SearchWorkouts),
            "Search past workouts by exercise name, workout type, or keywords in notes. Helpful for finding when the user last did a specific exercise or workout.")]
        SearchWorkoutsRequest request,
        ToolInvocationContext context)
    {
        var results = _workoutRepo.SearchWorkouts(request.Query);

        return new SearchWorkoutsResponse
        {
            Query = request.Query,
            Count = results.Count,
            ResultsJson = JsonSerializer.Serialize(results)
        };
    }

    [Function(nameof(DeleteWorkout))]
    public DeleteWorkoutResponse DeleteWorkout(
        [McpToolTrigger(nameof(DeleteWorkout),
            "Delete a workout by its ID. Use when the user wants to remove an incorrectly logged workout.")]
        DeleteWorkoutRequest request,
        ToolInvocationContext context)
    {
        _logger.LogInformation("Deleting workout {WorkoutId}", request.WorkoutId);

        // TODO: Add DeleteWorkout to repository

        return new DeleteWorkoutResponse
        {
            Success = true,
            Message = $"Workout {request.WorkoutId} deleted successfully",
            DeletedWorkoutId = request.WorkoutId
        };
    }

    #endregion

    #region Request POCOs

    public class LogWorkoutRequest
    {
        [Description("Type of workout: Push, Pull, Legs, Upper, Lower, FullBody, Cardio, General")]
        public WorkoutType Type { get; set; } = WorkoutType.General;

        [Description("List of exercises in format: 'ExerciseName|MuscleGroup|Sets x Reps @ Weight'. Example: 'Bench Press|Chest|3x8@185', 'Squat|Legs|4x10@225'")]
        public List<string> Exercises { get; set; } = new();

        [Description("Duration of the workout in minutes")]
        public int DurationMinutes { get; set; } = 60;

        [Description("Date of the workout (defaults to today)")]
        public DateTime? Date { get; set; }

        [Description("Rate of perceived exertion 1-10 (how hard it felt)")]
        public int PerceivedEffort { get; set; } = 5;

        [Description("Energy level 1-10 (how energized the user felt)")]
        public int EnergyLevel { get; set; } = 5;

        [Description("Optional notes about the workout")]
        public string? Notes { get; set; }
    }

    public class ExerciseInput
    {
        [Description("Name of the exercise (e.g., 'Bench Press', 'Squat')")]
        public required string Name { get; set; }

        [Description("Primary muscle group: Chest, Back, Shoulders, Biceps, Triceps, Legs, Core, Cardio")]
        public string MuscleGroup { get; set; } = "General";

        [Description("List of sets performed")]
        public List<SetInput> Sets { get; set; } = new();

        [Description("Optional notes for this exercise")]
        public string? Notes { get; set; }
    }

    public class SetInput
    {
        [Description("Number of reps performed")]
        public int Reps { get; set; }

        [Description("Weight used (in user's preferred unit)")]
        public double Weight { get; set; }

        [Description("Rate of perceived exertion for this set (1-10)")]
        public int? Rpe { get; set; }

        [Description("Whether this was a personal record attempt")]
        public bool IsPR { get; set; }
    }

    public class GetWorkoutHistoryRequest
    {
        [Description("Number of days to look back (default 30)")]
        public int Days { get; set; } = 30;

        [Description("Optional filter by workout type")]
        public WorkoutType? WorkoutType { get; set; }
    }

    public class GetPersonalRecordsRequest
    {
        [Description("Optional: filter by specific exercise name")]
        public string? ExerciseName { get; set; }
    }

    public class GetWorkoutStatsRequest
    {
        [Description("Number of days to analyze (default 30)")]
        public int Days { get; set; } = 30;
    }

    public class UpdateUserProfileRequest
    {
        [Description("Fitness goals: Strength, Hypertrophy, Endurance, WeightLoss, GeneralFitness")]
        public List<FitnessGoal>? Goals { get; set; }

        [Description("Available equipment: Barbell, Dumbbells, Cables, Machines, BodyweightOnly, Kettlebells, ResistanceBands")]
        public List<Equipment>? Equipment { get; set; }

        [Description("Target number of workout days per week")]
        public int WorkoutDaysPerWeek { get; set; } = 4;

        [Description("Target workout duration in minutes")]
        public int SessionDurationMinutes { get; set; } = 60;

        [Description("Any injuries or limitations to work around")]
        public List<string>? Injuries { get; set; }

        [Description("Experience level: Beginner, Intermediate, Advanced")]
        public ExperienceLevel ExperienceLevel { get; set; } = ExperienceLevel.Intermediate;

        [Description("Preferred weight unit: Lbs or Kg")]
        public WeightUnit WeightUnit { get; set; } = WeightUnit.Lbs;
    }

    public class SearchWorkoutsRequest
    {
        [Description("Search query - exercise name, workout type, or keyword")]
        public required string Query { get; set; }
    }

    public class DeleteWorkoutRequest
    {
        [Description("The ID of the workout to delete")]
        public required string WorkoutId { get; set; }
    }

    #endregion

    #region Response POCOs (Structured Content)

    [McpResult]
    public class LogWorkoutResponse
    {
        [Description("Whether the workout was logged successfully")]
        public bool Success { get; set; }

        [Description("Status message")]
        public string Message { get; set; } = string.Empty;

        // Flattened workout data
        [Description("Unique ID of the workout")]
        public string WorkoutId { get; set; } = string.Empty;

        [Description("Date of the workout")]
        public DateTime Date { get; set; }

        [Description("Type of workout")]
        public string Type { get; set; } = string.Empty;

        [Description("Duration in minutes")]
        public int DurationMinutes { get; set; }

        [Description("Rate of perceived exertion 1-10")]
        public int PerceivedEffort { get; set; }

        [Description("Energy level 1-10")]
        public int EnergyLevel { get; set; }

        [Description("Optional notes about the workout")]
        public string? Notes { get; set; }

        // Flattened summary data
        [Description("Number of exercises performed")]
        public int ExerciseCount { get; set; }

        [Description("Total sets across all exercises")]
        public int TotalSets { get; set; }

        [Description("Total volume (weight × reps)")]
        public double TotalVolume { get; set; }

        [Description("Muscle groups targeted (comma-separated)")]
        public string MuscleGroups { get; set; } = string.Empty;

        [Description("JSON string containing exercise details")]
        public string ExercisesJson { get; set; } = string.Empty;
    }

    public class GetWorkoutHistoryResponse
    {
        [Description("Time period covered")]
        public string Period { get; set; } = string.Empty;

        [Description("Total number of workouts in the period")]
        public int TotalWorkouts { get; set; }

        [Description("JSON string containing all workouts")]
        public string WorkoutsJson { get; set; } = string.Empty;

        // Flattened summary data
        [Description("Total workout sessions")]
        public int TotalSessions { get; set; }

        [Description("Total volume lifted across all workouts")]
        public double TotalVolume { get; set; }

        [Description("Average session duration in minutes")]
        public int AverageSessionDuration { get; set; }

        [Description("Most trained muscle groups (comma-separated)")]
        public string MostTrainedMuscleGroups { get; set; } = string.Empty;

        [Description("Average perceived effort across workouts")]
        public double AveragePerceivedEffort { get; set; }

        [Description("JSON string of workout type distribution")]
        public string WorkoutTypesJson { get; set; } = string.Empty;
    }

    public class GetPersonalRecordsResponse
    {
        [Description("JSON string containing list of personal records")]
        public string PersonalRecordsJson { get; set; } = string.Empty;

        [Description("Total number of PRs")]
        public int Count { get; set; }

        [Description("When this data was retrieved")]
        public DateTime LastUpdated { get; set; }
    }

    public class UpdateUserProfileResponse
    {
        [Description("Whether the profile was updated successfully")]
        public bool Success { get; set; }

        [Description("Status message")]
        public string Message { get; set; } = string.Empty;

        // Flattened profile data
        [Description("Fitness goals (comma-separated)")]
        public string Goals { get; set; } = string.Empty;

        [Description("Available equipment (comma-separated)")]
        public string AvailableEquipment { get; set; } = string.Empty;

        [Description("Target workout days per week")]
        public int WorkoutDaysPerWeek { get; set; }

        [Description("Target session duration in minutes")]
        public int SessionDurationMinutes { get; set; }

        [Description("Injuries or limitations (comma-separated)")]
        public string Injuries { get; set; } = string.Empty;

        [Description("Experience level")]
        public string ExperienceLevel { get; set; } = string.Empty;

        [Description("Preferred weight unit")]
        public string WeightUnit { get; set; } = string.Empty;
    }

    public class SearchWorkoutsResponse
    {
        [Description("The search query used")]
        public string Query { get; set; } = string.Empty;

        [Description("Number of matching workouts")]
        public int Count { get; set; }

        [Description("JSON string containing matching workouts")]
        public string ResultsJson { get; set; } = string.Empty;
    }

    public class DeleteWorkoutResponse
    {
        [Description("Whether the workout was deleted successfully")]
        public bool Success { get; set; }

        [Description("Status message")]
        public string Message { get; set; } = string.Empty;

        [Description("ID of the deleted workout")]
        public string DeletedWorkoutId { get; set; } = string.Empty;
    }

    #endregion

    #region Enums

    public enum WorkoutType
    {
        Push,
        Pull,
        Legs,
        Upper,
        Lower,
        FullBody,
        Cardio,
        General
    }

    public enum FitnessGoal
    {
        Strength,
        Hypertrophy,
        Endurance,
        WeightLoss,
        GeneralFitness
    }

    public enum Equipment
    {
        Barbell,
        Dumbbells,
        Cables,
        Machines,
        BodyweightOnly,
        Kettlebells,
        ResistanceBands,
        PullUpBar,
        Bench
    }

    public enum ExperienceLevel
    {
        Beginner,
        Intermediate,
        Advanced
    }

    public enum WeightUnit
    {
        Lbs,
        Kg
    }

    #endregion

    #region Helper Methods

    private WorkoutSummary GenerateWorkoutSummary(List<WorkoutSession> workouts)
    {
        if (!workouts.Any())
        {
            return new WorkoutSummary();
        }

        var muscleGroups = workouts
            .SelectMany(w => w.Exercises)
            .GroupBy(e => e.MuscleGroup)
            .ToDictionary(g => g.Key, g => g.Count());

        return new WorkoutSummary
        {
            TotalSessions = workouts.Count,
            TotalVolume = workouts.Sum(w => w.Exercises.Sum(e => e.Sets.Sum(s => s.Reps * s.Weight))),
            AverageSessionDuration = (int)workouts.Average(w => w.DurationMinutes),
            MostTrainedMuscleGroups = muscleGroups.OrderByDescending(kv => kv.Value).Take(3).Select(kv => kv.Key).ToList(),
            AveragePerceivedEffort = workouts.Average(w => w.PerceivedEffort),
            WorkoutTypes = workouts.GroupBy(w => w.Type).ToDictionary(g => g.Key, g => g.Count())
        };
    }

    private WorkoutStatistics CalculateStats(List<WorkoutSession> workouts, int days)
    {
        var weeks = days / 7.0;

        return new WorkoutStatistics
        {
            TotalWorkouts = workouts.Count,
            WorkoutsPerWeek = Math.Round(workouts.Count / weeks, 1),
            TotalVolumeLifted = workouts.Sum(w => w.Exercises.Sum(e => e.Sets.Sum(s => s.Reps * s.Weight))),
            UniqueExercises = workouts.SelectMany(w => w.Exercises).Select(e => e.Name).Distinct().Count(),
            ConsistencyScore = CalculateConsistencyScore(workouts, days),
            MuscleGroupDistribution = CalculateMuscleGroupDistribution(workouts),
            WeeklyTrend = CalculateWeeklyTrend(workouts),
            RecentFatigue = CalculateFatigueIndicator(workouts)
        };
    }

    private double CalculateConsistencyScore(List<WorkoutSession> workouts, int days)
    {
        var expectedWorkoutsPerWeek = 4;
        var expectedTotal = (days / 7.0) * expectedWorkoutsPerWeek;
        return Math.Min(100, Math.Round((workouts.Count / expectedTotal) * 100, 1));
    }

    private Dictionary<string, int> CalculateMuscleGroupDistribution(List<WorkoutSession> workouts)
    {
        return workouts
            .SelectMany(w => w.Exercises)
            .GroupBy(e => e.MuscleGroup)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private List<WeeklyData> CalculateWeeklyTrend(List<WorkoutSession> workouts)
    {
        return workouts
            .GroupBy(w => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                w.Date, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
            .Select(g => new WeeklyData
            {
                Week = g.Key,
                Workouts = g.Count(),
                TotalVolume = g.Sum(w => w.Exercises.Sum(e => e.Sets.Sum(s => s.Reps * s.Weight)))
            })
            .OrderBy(w => w.Week)
            .ToList();
    }

    private string CalculateFatigueIndicator(List<WorkoutSession> workouts)
    {
        var recentWorkouts = workouts.Where(w => w.Date >= DateTime.UtcNow.AddDays(-7)).ToList();
        if (!recentWorkouts.Any()) return "Rested";

        var avgEffort = recentWorkouts.Average(w => w.PerceivedEffort);
        var avgEnergy = recentWorkouts.Average(w => w.EnergyLevel);

        if (avgEffort > 8 && avgEnergy < 4) return "High - Consider deload";
        if (avgEffort > 7 || avgEnergy < 5) return "Moderate";
        return "Low - Good to push";
    }

    #endregion
}
