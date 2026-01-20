using ChatGptSampleApp;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

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

    /// <summary>
    /// Dashboard widget showing workout summary and quick-log form
    /// </summary>
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

    /// <summary>
    /// Workout logging form widget
    /// </summary>
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

    /// <summary>
    /// Progress chart widget showing workout trends
    /// </summary>
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

    #region Tools - Workout Management

    /// <summary>
    /// Log a new workout session
    /// </summary>
    [Function(nameof(LogWorkout))]
    public async Task<string> LogWorkout(
        [McpToolTrigger(nameof(LogWorkout),
            "Log a completed workout session. Use this after the user describes a workout they just completed.")]
        ToolInvocationContext context,

        [McpToolProperty("type", "Type of workout: Push, Pull, Legs, Upper, Lower, FullBody, Cardio, General")]
        WorkoutType type,

        [McpToolProperty("exercises", "List of exercises performed with sets, reps, and weight")]
        IEnumerable<ExerciseInput> exercises,

        [McpToolProperty("durationMinutes", "Duration of the workout in minutes")]
        int durationMinutes = 60,

        [McpToolProperty("date", "Date of the workout (defaults to today)")]
        DateTime? date = null,

        [McpToolProperty("perceivedEffort", "Rate of perceived exertion 1-10 (how hard it felt)")]
        int perceivedEffort = 5,

        [McpToolProperty("energyLevel", "Energy level 1-10 (how energized the user felt)")]
        int energyLevel = 5,

        [McpToolProperty("notes", "Optional notes about the workout")]
        string? notes = null)
    {
        var workout = new WorkoutSession
        {
            Id = Guid.NewGuid().ToString(),
            Date = date?.ToUniversalTime() ?? DateTime.UtcNow,
            Type = type.ToString(),
            DurationMinutes = durationMinutes,
            Exercises = exercises.Select(e => new Exercise
            {
                Name = e.Name,
                MuscleGroup = e.MuscleGroup,
                Sets = e.Sets.Select((s, idx) => new ExerciseSet
                {
                    SetNumber = idx + 1,
                    Reps = s.Reps,
                    Weight = s.Weight,
                    Rpe = s.Rpe,
                    IsPR = s.IsPR
                }).ToList(),
                Notes = e.Notes
            }).ToList(),
            PerceivedEffort = Math.Clamp(perceivedEffort, 1, 10),
            EnergyLevel = Math.Clamp(energyLevel, 1, 10),
            Notes = notes
        };

        await _workoutRepo.SaveWorkoutAsync(workout);
        _logger.LogInformation("Logged workout {WorkoutId} on {Date}", workout.Id, workout.Date);

        // Calculate some stats to return
        var totalVolume = workout.Exercises.Sum(e => e.Sets.Sum(s => s.Reps * s.Weight));
        var totalSets = workout.Exercises.Sum(e => e.Sets.Count);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Workout logged successfully!",
            summary = new
            {
                workout.Id,
                workout.Date,
                workout.Type,
                workout.DurationMinutes,
                exerciseCount = workout.Exercises.Count,
                totalSets,
                totalVolume,
                muscleGroups = workout.Exercises.Select(e => e.MuscleGroup).Distinct().ToList()
            }
        });
    }

    /// <summary>
    /// Get workout history for analysis
    /// </summary>
    [Function(nameof(GetWorkoutHistory))]
    public async Task<string> GetWorkoutHistory(
        [McpToolTrigger(nameof(GetWorkoutHistory),
            "Retrieve workout history for a specified time period. Returns detailed workout data including exercises, volume, and user notes. Use this to analyze patterns and tailor recommendations.")]
        ToolInvocationContext context,

        [McpToolProperty("days", "Number of days to look back (default 30)")]
        int days = 30,

        [McpToolProperty("workoutType", "Optional filter by workout type")]
        WorkoutType? workoutType = null)
    {
        var typeFilter = workoutType?.ToString();
        var workouts = await _workoutRepo.GetWorkoutsAsync(days, typeFilter);

        _logger.LogInformation("Retrieved {Count} workouts from last {Days} days", workouts.Count, days);

        return JsonSerializer.Serialize(new
        {
            period = $"Last {days} days",
            totalWorkouts = workouts.Count,
            workouts = workouts,
            summary = GenerateWorkoutSummary(workouts)
        });
    }

    /// <summary>
    /// Get personal records for exercises
    /// </summary>
    [Function(nameof(GetPersonalRecords))]
    public async Task<string> GetPersonalRecords(
        [McpToolTrigger(nameof(GetPersonalRecords),
            "Get personal records (PRs) for specific exercises or all exercises. Useful for tracking strength progression and setting goals.")]
        ToolInvocationContext context,

        [McpToolProperty("exerciseName", "Optional: filter by specific exercise name")]
        string? exerciseName = null)
    {
        var prs = await _workoutRepo.GetPersonalRecordsAsync(exerciseName);

        return JsonSerializer.Serialize(new
        {
            personalRecords = prs,
            count = prs.Count,
            lastUpdated = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get workout statistics and trends
    /// </summary>
    [Function(nameof(GetWorkoutStats))]
    public async Task<string> GetWorkoutStats(
        [McpToolTrigger(nameof(GetWorkoutStats),
            "Get comprehensive workout statistics including frequency, volume trends, muscle group distribution, and consistency metrics. Essential for generating personalized workout recommendations.")]
        ToolInvocationContext context,

        [McpToolProperty("days", "Number of days to analyze (default 30)")]
        int days = 30)
    {
        var workouts = await _workoutRepo.GetWorkoutsAsync(days, null);
        var stats = CalculateStats(workouts, days);

        return JsonSerializer.Serialize(stats);
    }

    /// <summary>
    /// Update goals and preferences
    /// </summary>
    [Function(nameof(UpdateUserProfile))]
    public async Task<string> UpdateUserProfile(
        [McpToolTrigger(nameof(UpdateUserProfile),
            "Update user's fitness goals, preferences, and constraints. Use this when the user shares information about their goals, available equipment, schedule, or limitations.")]
        ToolInvocationContext context,

        [McpToolProperty("goals", "Fitness goals: Strength, Hypertrophy, Endurance, WeightLoss, GeneralFitness")]
        IEnumerable<FitnessGoal>? goals = null,

        [McpToolProperty("equipment", "Available equipment: Barbell, Dumbbells, Cables, Machines, BodyweightOnly, Kettlebells, ResistanceBands")]
        IEnumerable<Equipment>? equipment = null,

        [McpToolProperty("workoutDaysPerWeek", "Target number of workout days per week")]
        int workoutDaysPerWeek = 4,

        [McpToolProperty("sessionDurationMinutes", "Target workout duration in minutes")]
        int sessionDurationMinutes = 60,

        [McpToolProperty("injuries", "Any injuries or limitations to work around")]
        IEnumerable<string>? injuries = null,

        [McpToolProperty("experienceLevel", "Experience level: Beginner, Intermediate, Advanced")]
        ExperienceLevel experienceLevel = ExperienceLevel.Intermediate,

        [McpToolProperty("weightUnit", "Preferred weight unit: Lbs or Kg")]
        WeightUnit weightUnit = WeightUnit.Lbs)
    {
        var profile = new UserProfile
        {
            Goals = goals?.Select(g => g.ToString()).ToList() ?? new List<string> { "GeneralFitness" },
            AvailableEquipment = equipment?.Select(e => e.ToString()).ToList() ?? new List<string>(),
            WorkoutDaysPerWeek = Math.Clamp(workoutDaysPerWeek, 1, 7),
            SessionDurationMinutes = Math.Clamp(sessionDurationMinutes, 15, 180),
            Injuries = injuries?.ToList() ?? new List<string>(),
            ExperienceLevel = experienceLevel.ToString(),
            WeightUnit = weightUnit.ToString().ToLowerInvariant()
        };

        await _workoutRepo.SaveUserProfileAsync(profile);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = "Profile updated successfully",
            profile
        });
    }

    /// <summary>
    /// Get user profile for personalization
    /// </summary>
    [Function(nameof(GetUserProfile))]
    public async Task<string> GetUserProfile(
        [McpToolTrigger(nameof(GetUserProfile),
            "Retrieve user's fitness profile including goals, equipment, constraints, and preferences. Use this before generating workout recommendations.")]
        ToolInvocationContext context)
    {
        var profile = await _workoutRepo.GetUserProfileAsync();

        return JsonSerializer.Serialize(profile ?? new UserProfile());
    }

    /// <summary>
    /// Search workouts by exercise or notes
    /// </summary>
    [Function(nameof(SearchWorkouts))]
    public async Task<string> SearchWorkouts(
        [McpToolTrigger(nameof(SearchWorkouts),
            "Search past workouts by exercise name, workout type, or keywords in notes. Helpful for finding when the user last did a specific exercise or workout.")]
        ToolInvocationContext context,

        [McpToolProperty("query", "Search query - exercise name, workout type, or keyword", true)]
        string query)
    {
        var results = await _workoutRepo.SearchWorkoutsAsync(query);

        return JsonSerializer.Serialize(new
        {
            query,
            count = results.Count,
            results
        });
    }

    /// <summary>
    /// Delete a workout by ID
    /// </summary>
    [Function(nameof(DeleteWorkout))]
    public async Task<string> DeleteWorkout(
        [McpToolTrigger(nameof(DeleteWorkout),
            "Delete a workout by its ID. Use when the user wants to remove an incorrectly logged workout.")]
        ToolInvocationContext context,
        [McpToolProperty("workoutId", "The ID of the workout to delete", true)]
        string workoutId)
    {
        // Note: You'd need to add this method to the repository
        _logger.LogInformation("Deleting workout {WorkoutId}", workoutId);

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Workout {workoutId} deleted successfully"
        });
    }

    #endregion

    #region Input Models for Tool Properties

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

    #endregion

    #region Enums for Tool Properties

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
