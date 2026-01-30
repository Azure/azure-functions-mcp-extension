// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Text.Json;

namespace ChatGptSampleApp;

/// <summary>
/// MCP Functions for interactive workout tracking with real-time set logging and rest timers.
/// Integrates with ChatGPT to provide guided workout sessions and AI-powered recommendations.
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

    [Function(nameof(GetStartWorkoutWidget))]
    public string GetStartWorkoutWidget(
    [McpResourceTrigger(
        "ui://workout/start-workout.html",
        "Workout Tracker",
        MimeType = "text/html;profile=mcp-app",
        Description = "Select a workout template, track your sets, and log your progress")]
    ResourceInvocationContext context)
    {
        var file = Path.Combine(AppContext.BaseDirectory,"app", "dist", "start-workout.html");
        return File.ReadAllText(file);
    }

    [Function(nameof(GetWorkoutSummaryWidget))]
    public string GetWorkoutSummaryWidget(
        [McpResourceTrigger(
        "ui://workout/workout-summary.html",
        "Workout Summary",
        MimeType = "text/html;profile=mcp-app",
        Description = "View your completed workout summary and overall progress")]
    [McpResourceMetadata("ui.prefersBorder", true)]
    ResourceInvocationContext context)
    {
        var file = Path.Combine(AppContext.BaseDirectory, "app", "dist", "workout-summary.html");
        return File.ReadAllText(file);
    }

    #endregion

    #region Tools - Interactive Workout Session Management

    [Function(nameof(GetWorkoutTemplates))]
    public GetWorkoutTemplatesResponse GetWorkoutTemplates(
        [McpToolTrigger(nameof(GetWorkoutTemplates),
            "Get predefined workout templates (Push, Pull, Legs). Returns template details with exercises and recommended sets/reps.")]
        ToolInvocationContext context)
    {
        var templates = _workoutRepo.GetWorkoutTemplates();

        return new GetWorkoutTemplatesResponse
        {
            Templates = templates.Select(t => new WorkoutTemplateOutput
            {
                Id = t.Id,
                Name = t.Name,
                Type = t.Type,
                Description = t.Description,
                EstimatedDurationMinutes = t.EstimatedDurationMinutes,
                Exercises = t.Exercises.Select(e => new TemplateExerciseOutput
                {
                    Name = e.Name,
                    MuscleGroup = e.MuscleGroup,
                    TargetSets = e.TargetSets,
                    TargetReps = e.TargetReps,
                    RestSeconds = e.RestSeconds,
                    Notes = e.Notes
                }).ToList()
            }).ToList(),
            Count = templates.Count
        };
    }

    [Function(nameof(StartWorkoutSession))]
    public StartWorkoutSessionResponse StartWorkoutSession(
        [McpToolTrigger(nameof(StartWorkoutSession),
            "Start a new workout session from a template. Creates an active workout session that can be logged set-by-set.")]
        StartWorkoutSessionRequest request,
        ToolInvocationContext context)
    {
        var template = _workoutRepo.GetWorkoutTemplateById(request.TemplateId);
        if (template == null)
        {
            return new StartWorkoutSessionResponse
            {
                Success = false,
                Message = $"Template {request.TemplateId} not found"
            };
        }

        var session = new ActiveWorkoutSession
        {
            Id = Guid.NewGuid().ToString(),
            TemplateId = request.TemplateId,
            TemplateName = template.Name,
            Type = template.Type,
            StartTime = DateTime.UtcNow,
            Exercises = template.Exercises.Select(e => new ActiveExercise
            {
                Name = e.Name,
                MuscleGroup = e.MuscleGroup,
                TargetSets = e.TargetSets,
                TargetReps = e.TargetReps,
                RestSeconds = e.RestSeconds,
                Notes = e.Notes,
                CompletedSets = new List<ExerciseSet>()
            }).ToList(),
            CurrentExerciseIndex = 0,
            IsComplete = false
        };

        _workoutRepo.SaveActiveWorkoutSession(session);

        _logger.LogInformation("Started workout session {SessionId} from template {TemplateName}",
            session.Id, template.Name);

        return new StartWorkoutSessionResponse
        {
            Success = true,
            Message = "Workout session started!",
            SessionId = session.Id,
            TemplateName = template.Name,
            Type = template.Type,
            TotalExercises = session.Exercises.Count,
            EstimatedDurationMinutes = template.EstimatedDurationMinutes,
            FirstExercise = new ExercisePreview
            {
                Name = session.Exercises[0].Name,
                MuscleGroup = session.Exercises[0].MuscleGroup,
                TargetSets = session.Exercises[0].TargetSets,
                TargetReps = session.Exercises[0].TargetReps,
                RestSeconds = session.Exercises[0].RestSeconds
            }
        };
    }

    [Function(nameof(GetActiveWorkout))]
    public GetActiveWorkoutResponse GetActiveWorkout(
        [McpToolTrigger(nameof(GetActiveWorkout),
            "Get the current active workout session with progress and current exercise details.")]
        ToolInvocationContext context)
    {
        var session = _workoutRepo.GetActiveWorkoutSession();
        if (session == null)
        {
            return new GetActiveWorkoutResponse
            {
                IsActive = false,
                Message = "No active workout session"
            };
        }

        var currentExercise = session.Exercises[session.CurrentExerciseIndex];
        var previousPerformance = _workoutRepo.GetLastPerformance(currentExercise.Name);

        var totalSetsCompleted = session.Exercises.Sum(e => e.CompletedSets.Count);
        var totalSetsTarget = session.Exercises.Sum(e => e.TargetSets);

        return new GetActiveWorkoutResponse
        {
            IsActive = true,
            SessionId = session.Id,
            TemplateName = session.TemplateName,
            Type = session.Type,
            StartTime = session.StartTime,
            ElapsedMinutes = (int)(DateTime.UtcNow - session.StartTime).TotalMinutes,
            CurrentExerciseIndex = session.CurrentExerciseIndex,
            TotalExercises = session.Exercises.Count,
            TotalSetsCompleted = totalSetsCompleted,
            TotalSetsTarget = totalSetsTarget,
            CurrentExercise = new CurrentExerciseOutput
            {
                Name = currentExercise.Name,
                MuscleGroup = currentExercise.MuscleGroup,
                TargetSets = currentExercise.TargetSets,
                TargetReps = currentExercise.TargetReps,
                RestSeconds = currentExercise.RestSeconds,
                CompletedSets = currentExercise.CompletedSets.Count,
                Sets = currentExercise.CompletedSets.Select(s => new SetOutput
                {
                    Reps = s.Reps,
                    Weight = s.Weight,
                    Rpe = s.Rpe,
                    IsPR = s.IsPR
                }).ToList(),
                Notes = currentExercise.Notes
            },
            PreviousPerformance = previousPerformance != null ? new PreviousPerformanceOutput
            {
                Date = previousPerformance.Date,
                Sets = previousPerformance.Sets.Select(s => new SetOutput
                {
                    Reps = s.Reps,
                    Weight = s.Weight,
                    Rpe = s.Rpe,
                    IsPR = s.IsPR
                }).ToList()
            } : null
        };
    }

    [Function(nameof(LogSet))]
    public LogSetResponse LogSet(
        [McpToolTrigger(nameof(LogSet),
            "Log a single set for the current exercise in the active workout. Returns rest timer information and progress.")]
        LogSetRequest request,
        ToolInvocationContext context)
    {
        var session = _workoutRepo.GetActiveWorkoutSession();
        if (session == null)
        {
            return new LogSetResponse
            {
                Success = false,
                Message = "No active workout session"
            };
        }

        var currentExercise = session.Exercises[session.CurrentExerciseIndex];

        var newSet = new ExerciseSet
        {
            Reps = request.Reps,
            Weight = request.Weight,
            Rpe = request.Rpe,
            IsPR = request.IsPR
        };

        currentExercise.CompletedSets.Add(newSet);
        _workoutRepo.UpdateActiveWorkoutSession(session);

        var setsRemaining = currentExercise.TargetSets - currentExercise.CompletedSets.Count;
        var isExerciseComplete = currentExercise.CompletedSets.Count >= currentExercise.TargetSets;

        _logger.LogInformation("Logged set {SetNumber}/{TargetSets} for {ExerciseName}: {Reps}x{Weight}",
            currentExercise.CompletedSets.Count, currentExercise.TargetSets,
            currentExercise.Name, request.Reps, request.Weight);

        return new LogSetResponse
        {
            Success = true,
            Message = $"Set {currentExercise.CompletedSets.Count}/{currentExercise.TargetSets} logged",
            SetNumber = currentExercise.CompletedSets.Count,
            TargetSets = currentExercise.TargetSets,
            SetsRemaining = Math.Max(0, setsRemaining),
            IsExerciseComplete = isExerciseComplete,
            RestSeconds = currentExercise.RestSeconds,
            CurrentExercise = currentExercise.Name,
            Reps = request.Reps,
            Weight = request.Weight
        };
    }

    [Function(nameof(NextExercise))]
    public NextExerciseResponse NextExercise(
        [McpToolTrigger(nameof(NextExercise),
            "Move to the next exercise in the active workout session.")]
        ToolInvocationContext context)
    {
        var session = _workoutRepo.GetActiveWorkoutSession();
        if (session == null)
        {
            return new NextExerciseResponse
            {
                Success = false,
                Message = "No active workout session"
            };
        }

        if (session.CurrentExerciseIndex >= session.Exercises.Count - 1)
        {
            return new NextExerciseResponse
            {
                Success = false,
                Message = "Already on the last exercise. Complete the workout to finish.",
                IsLastExercise = true
            };
        }

        session.CurrentExerciseIndex++;
        _workoutRepo.UpdateActiveWorkoutSession(session);

        var nextExercise = session.Exercises[session.CurrentExerciseIndex];
        var previousPerformance = _workoutRepo.GetLastPerformance(nextExercise.Name);

        return new NextExerciseResponse
        {
            Success = true,
            Message = $"Moved to exercise {session.CurrentExerciseIndex + 1}/{session.Exercises.Count}",
            ExerciseNumber = session.CurrentExerciseIndex + 1,
            TotalExercises = session.Exercises.Count,
            IsLastExercise = session.CurrentExerciseIndex >= session.Exercises.Count - 1,
            CurrentExercise = new CurrentExerciseOutput
            {
                Name = nextExercise.Name,
                MuscleGroup = nextExercise.MuscleGroup,
                TargetSets = nextExercise.TargetSets,
                TargetReps = nextExercise.TargetReps,
                RestSeconds = nextExercise.RestSeconds,
                CompletedSets = 0,
                Sets = new List<SetOutput>(),
                Notes = nextExercise.Notes
            },
            PreviousPerformance = previousPerformance != null ? new PreviousPerformanceOutput
            {
                Date = previousPerformance.Date,
                Sets = previousPerformance.Sets.Select(s => new SetOutput
                {
                    Reps = s.Reps,
                    Weight = s.Weight,
                    Rpe = s.Rpe,
                    IsPR = s.IsPR
                }).ToList()
            } : null
        };
    }

    [Function(nameof(CompleteWorkout))]
    public CompleteWorkoutResponse CompleteWorkout(
        [McpToolTrigger(nameof(CompleteWorkout),
            "Complete the active workout session and save it to history. Calculates final statistics and PRs.")]
        CompleteWorkoutRequest request,
        ToolInvocationContext context)
    {
        var session = _workoutRepo.GetActiveWorkoutSession();
        if (session == null)
        {
            return new CompleteWorkoutResponse
            {
                Success = false,
                Message = "No active workout session to complete"
            };
        }

        session.EndTime = DateTime.UtcNow;
        session.IsComplete = true;

        // Convert active session to completed workout
        var workout = new WorkoutSession
        {
            Id = session.Id,
            Date = session.StartTime,
            Type = session.Type,
            DurationMinutes = (int)(session.EndTime.Value - session.StartTime).TotalMinutes,
            PerceivedEffort = request.PerceivedEffort,
            EnergyLevel = request.EnergyLevel,
            Notes = request.Notes,
            Exercises = session.Exercises.Select(e => new Exercise
            {
                Name = e.Name,
                MuscleGroup = e.MuscleGroup,
                Notes = e.Notes,
                Sets = e.CompletedSets
            }).ToList()
        };

        _workoutRepo.SaveWorkout(workout);
        _workoutRepo.ClearActiveWorkoutSession();

        var totalVolume = workout.Exercises.Sum(e => e.Sets.Sum(s => s.Reps * s.Weight));
        var totalSets = workout.Exercises.Sum(e => e.Sets.Count);
        var newPRs = workout.Exercises
            .SelectMany(e => e.Sets.Where(s => s.IsPR).Select(s => e.Name))
            .Distinct()
            .ToList();

        _logger.LogInformation("Completed workout {WorkoutId} - Duration: {Duration}min, Volume: {Volume}, PRs: {PRCount}",
            workout.Id, workout.DurationMinutes, totalVolume, newPRs.Count);

        return new CompleteWorkoutResponse
        {
            Success = true,
            Message = "Workout completed and saved!",
            WorkoutId = workout.Id,
            Date = workout.Date,
            DurationMinutes = workout.DurationMinutes,
            TotalExercises = workout.Exercises.Count,
            TotalSets = totalSets,
            TotalVolume = totalVolume,
            NewPRs = newPRs,
            Summary = new WorkoutSummaryOutput
            {
                ExerciseCount = workout.Exercises.Count,
                TotalSets = totalSets,
                TotalVolume = totalVolume,
                MuscleGroups = string.Join(", ", workout.Exercises.Select(e => e.MuscleGroup).Distinct())
            }
        };
    }

    [Function(nameof(CancelWorkout))]
    public CancelWorkoutResponse CancelWorkout(
        [McpToolTrigger(nameof(CancelWorkout),
            "Cancel the active workout session without saving.")]
        ToolInvocationContext context)
    {
        var session = _workoutRepo.GetActiveWorkoutSession();
        if (session == null)
        {
            return new CancelWorkoutResponse
            {
                Success = false,
                Message = "No active workout session to cancel"
            };
        }

            _workoutRepo.ClearActiveWorkoutSession();

            return new CancelWorkoutResponse
            {
                Success = true,
                Message = "Workout session cancelled"
            };
        }

        [Function(nameof(GetWorkoutHistory))]
        public GetWorkoutHistoryResponse GetWorkoutHistory(
            [McpToolTrigger(nameof(GetWorkoutHistory),
                "Get the user's workout history for a specified number of days. Returns completed workouts with exercises, sets, and statistics.")]
            GetWorkoutHistoryRequest request,
            ToolInvocationContext context)
        {
            var days = request.Days > 0 ? request.Days : 30;
            var typeFilter = request.WorkoutType?.ToString();
            var workouts = _workoutRepo.GetWorkouts(days, typeFilter);

            _logger.LogInformation("Retrieved {Count} workouts from last {Days} days", workouts.Count, days);

            var totalVolume = workouts.Sum(w => w.Exercises.Sum(e => e.Sets.Sum(s => s.Reps * s.Weight)));
            var totalSets = workouts.Sum(w => w.Exercises.Sum(e => e.Sets.Count));

            return new GetWorkoutHistoryResponse
            {
                Period = $"Last {days} days",
                TotalWorkouts = workouts.Count,
                Workouts = workouts.Select(w => new WorkoutHistoryItem
                {
                    Id = w.Id,
                    Date = w.Date,
                    Type = w.Type,
                    DurationMinutes = w.DurationMinutes,
                    PerceivedEffort = w.PerceivedEffort,
                    EnergyLevel = w.EnergyLevel,
                    Notes = w.Notes,
                    Exercises = w.Exercises.Select(e => new ExerciseOutput
                    {
                        Name = e.Name,
                        MuscleGroup = e.MuscleGroup,
                        Notes = e.Notes,
                        Sets = e.Sets.Select(s => new SetOutput
                        {
                            Reps = s.Reps,
                            Weight = s.Weight,
                            Rpe = s.Rpe,
                            IsPR = s.IsPR
                        }).ToList()
                    }).ToList()
                }).ToList(),
                Summary = new WorkoutPeriodSummary
                {
                    TotalSessions = workouts.Count,
                    TotalVolume = totalVolume,
                    AverageSessionDuration = workouts.Count > 0 ? (int)workouts.Average(w => w.DurationMinutes) : 0,
                    AveragePerceivedEffort = workouts.Count > 0 ? workouts.Average(w => w.PerceivedEffort) : 0,
                    MostTrainedMuscleGroups = workouts
                        .SelectMany(w => w.Exercises)
                        .GroupBy(e => e.MuscleGroup)
                        .OrderByDescending(g => g.Count())
                        .Take(3)
                        .Select(g => g.Key)
                        .ToList(),
                    WorkoutTypes = workouts.GroupBy(w => w.Type)
                        .ToDictionary(g => g.Key, g => g.Count())
                }
            };
        }

        [Function(nameof(ShareWorkoutToTeams))]
        public async Task<ShareWorkoutToTeamsResponse> ShareWorkoutToTeams(
            [McpToolTrigger(nameof(ShareWorkoutToTeams),
                "Share a workout summary to Microsoft Teams. Posts an Adaptive Card with workout details to a configured Teams channel.")]
            ShareWorkoutToTeamsRequest request,
            ToolInvocationContext context)
        {
            var webhookUrl = Environment.GetEnvironmentVariable("TEAMS_WEBHOOK_URL");
            if (string.IsNullOrEmpty(webhookUrl))
            {
                return new ShareWorkoutToTeamsResponse
                {
                    Success = false,
                    Message = "Teams webhook URL not configured. Set TEAMS_WEBHOOK_URL environment variable."
                };
            }

            // Get the workout to share
            WorkoutSession? workout = null;
            if (!string.IsNullOrEmpty(request.WorkoutId))
            {
                var workouts = _workoutRepo.GetWorkouts(30, null);
                workout = workouts.FirstOrDefault(w => w.Id == request.WorkoutId);
            }
            else
            {
                // Get most recent workout
                var workouts = _workoutRepo.GetWorkouts(7, null);
                workout = workouts.FirstOrDefault();
            }

            if (workout == null)
            {
                return new ShareWorkoutToTeamsResponse
                {
                    Success = false,
                    Message = "No workout found to share"
                };
            }

            var totalVolume = workout.Exercises.Sum(e => e.Sets.Sum(s => s.Reps * s.Weight));
            var totalSets = workout.Exercises.Sum(e => e.Sets.Count);
            var prs = workout.Exercises.SelectMany(e => e.Sets.Where(s => s.IsPR)).Count();

            // Build Teams Adaptive Card
            var card = new
            {
                type = "message",
                attachments = new[]
                {
                    new
                    {
                        contentType = "application/vnd.microsoft.card.adaptive",
                        content = new
                        {
                            type = "AdaptiveCard",
                            version = "1.4",
                            body = new object[]
                            {
                                new
                                {
                                    type = "TextBlock",
                                    size = "Large",
                                    weight = "Bolder",
                                    text = $"💪 {workout.Type} Workout Complete!",
                                    wrap = true
                                },
                                new
                                {
                                    type = "TextBlock",
                                    text = $"📅 {workout.Date:MMMM dd, yyyy}",
                                    spacing = "None"
                                },
                                new
                                {
                                    type = "FactSet",
                                    facts = new[]
                                    {
                                        new { title = "Duration", value = $"{workout.DurationMinutes} minutes" },
                                        new { title = "Exercises", value = $"{workout.Exercises.Count}" },
                                        new { title = "Total Sets", value = $"{totalSets}" },
                                        new { title = "Volume", value = $"{totalVolume:N0} lbs" },
                                        new { title = "Effort (RPE)", value = $"{workout.PerceivedEffort}/10" },
                                        new { title = "Energy", value = $"{workout.EnergyLevel}/10" }
                                    }
                                },
                                new
                                {
                                    type = "TextBlock",
                                    text = "**Exercises:**",
                                    wrap = true,
                                    spacing = "Medium"
                                },
                                new
                                {
                                    type = "TextBlock",
                                    text = string.Join("\n", workout.Exercises.Select(e =>
                                        $"• {e.Name}: {e.Sets.Count} sets")),
                                    wrap = true
                                },
                                prs > 0 ? new
                                {
                                    type = "TextBlock",
                                    text = $"🏆 **{prs} Personal Record(s)!**",
                                    color = "Good",
                                    spacing = "Medium"
                                } : null!,
                                !string.IsNullOrEmpty(request.CustomMessage) ? new
                                {
                                    type = "TextBlock",
                                    text = $"💬 \"{request.CustomMessage}\"",
                                    wrap = true,
                                    spacing = "Medium",
                                    isSubtle = true
                                } : null!
                            }.Where(x => x != null).ToArray()
                        }
                    }
                }
            };

            try
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.PostAsJsonAsync(webhookUrl, card);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Posted workout {WorkoutId} to Teams", workout.Id);
                    return new ShareWorkoutToTeamsResponse
                    {
                        Success = true,
                        Message = "Workout shared to Teams successfully! 🎉",
                        WorkoutId = workout.Id,
                        WorkoutType = workout.Type,
                        WorkoutDate = workout.Date
                    };
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to post to Teams: {Error}", error);
                    return new ShareWorkoutToTeamsResponse
                    {
                        Success = false,
                        Message = $"Failed to post to Teams: {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting to Teams");
                return new ShareWorkoutToTeamsResponse
                {
                    Success = false,
                    Message = $"Error posting to Teams: {ex.Message}"
                };
            }
        }

        #endregion

        #region Request/Response POCOs

    // New Interactive Workout POCOs
    public class StartWorkoutSessionRequest
    {
        [Description("ID of the workout template to use")]
        public required string TemplateId { get; set; }
    }

    [McpResult]
    public class GetWorkoutTemplatesResponse
    {
        [Description("Instructions for the assistant - follow these")]
        public string Instructions { get; set; } = "Do NOT ask the user which workout to start. The Start Workout widget is displayed and the user will select a template there. Just acknowledge the templates are available and wait for the user to make their selection in the widget.";

        [Description("List of available workout templates")]
        public List<WorkoutTemplateOutput> Templates { get; set; } = new();

        [Description("Total number of templates")]
        public int Count { get; set; }
    }

    public class WorkoutTemplateOutput
    {
        [Description("Unique template ID")]
        public string Id { get; set; } = string.Empty;

        [Description("Template name")]
        public string Name { get; set; } = string.Empty;

        [Description("Workout type")]
        public string Type { get; set; } = string.Empty;

        [Description("Template description")]
        public string Description { get; set; } = string.Empty;

        [Description("Estimated duration in minutes")]
        public int EstimatedDurationMinutes { get; set; }

        [Description("List of exercises in the template")]
        public List<TemplateExerciseOutput> Exercises { get; set; } = new();
    }

    public class TemplateExerciseOutput
    {
        [Description("Exercise name")]
        public string Name { get; set; } = string.Empty;

        [Description("Primary muscle group")]
        public string MuscleGroup { get; set; } = string.Empty;

        [Description("Target number of sets")]
        public int TargetSets { get; set; }

        [Description("Target reps per set")]
        public int TargetReps { get; set; }

        [Description("Recommended rest time in seconds")]
        public int RestSeconds { get; set; }

        [Description("Exercise notes or tips")]
        public string? Notes { get; set; }
    }

    [McpResult]
    public class StartWorkoutSessionResponse
    {
        [Description("Whether the session started successfully")]
        public bool Success { get; set; }

        [Description("Status message")]
        public string Message { get; set; } = string.Empty;

        [Description("Active session ID")]
        public string SessionId { get; set; } = string.Empty;

        [Description("Template name")]
        public string TemplateName { get; set; } = string.Empty;

        [Description("Workout type")]
        public string Type { get; set; } = string.Empty;

        [Description("Total number of exercises")]
        public int TotalExercises { get; set; }

        [Description("Estimated duration in minutes")]
        public int EstimatedDurationMinutes { get; set; }

        [Description("First exercise preview")]
        public ExercisePreview FirstExercise { get; set; } = new();
    }

    public class ExercisePreview
    {
        [Description("Exercise name")]
        public string Name { get; set; } = string.Empty;

        [Description("Muscle group")]
        public string MuscleGroup { get; set; } = string.Empty;

        [Description("Target sets")]
        public int TargetSets { get; set; }

        [Description("Target reps")]
        public int TargetReps { get; set; }

        [Description("Rest seconds between sets")]
        public int RestSeconds { get; set; }
    }

    [McpResult]
    public class GetActiveWorkoutResponse
    {
        [Description("Whether there is an active workout")]
        public bool IsActive { get; set; }

        [Description("Status message")]
        public string Message { get; set; } = string.Empty;

        [Description("Active session ID")]
        public string SessionId { get; set; } = string.Empty;

        [Description("Template name")]
        public string TemplateName { get; set; } = string.Empty;

        [Description("Workout type")]
        public string Type { get; set; } = string.Empty;

        [Description("Workout start time")]
        public DateTime StartTime { get; set; }

        [Description("Elapsed time in minutes")]
        public int ElapsedMinutes { get; set; }

        [Description("Current exercise index (0-based)")]
        public int CurrentExerciseIndex { get; set; }

        [Description("Total number of exercises")]
        public int TotalExercises { get; set; }

        [Description("Total sets completed across all exercises")]
        public int TotalSetsCompleted { get; set; }

        [Description("Total target sets across all exercises")]
        public int TotalSetsTarget { get; set; }

        [Description("Current exercise details")]
        public CurrentExerciseOutput CurrentExercise { get; set; } = new();

        [Description("Previous performance for this exercise (if available)")]
        public PreviousPerformanceOutput? PreviousPerformance { get; set; }
    }

    public class CurrentExerciseOutput
    {
        [Description("Exercise name")]
        public string Name { get; set; } = string.Empty;

        [Description("Muscle group")]
        public string MuscleGroup { get; set; } = string.Empty;

        [Description("Target number of sets")]
        public int TargetSets { get; set; }

        [Description("Target reps per set")]
        public int TargetReps { get; set; }

        [Description("Rest time in seconds")]
        public int RestSeconds { get; set; }

        [Description("Number of completed sets")]
        public int CompletedSets { get; set; }

        [Description("List of completed sets")]
        public List<SetOutput> Sets { get; set; } = new();

        [Description("Exercise notes")]
        public string? Notes { get; set; }
    }

    public class PreviousPerformanceOutput
    {
        [Description("Date of previous performance")]
        public DateTime Date { get; set; }

        [Description("Sets from previous workout")]
        public List<SetOutput> Sets { get; set; } = new();
    }

    public class LogSetRequest
    {
        [Description("Number of reps completed")]
        public int Reps { get; set; }

        [Description("Weight used")]
        public double Weight { get; set; }

        [Description("Rate of perceived exertion (1-10)")]
        public int? Rpe { get; set; }

        [Description("Whether this is a personal record")]
        public bool IsPR { get; set; }
    }

    [McpResult]
    public class LogSetResponse
    {
        [Description("Whether the set was logged successfully")]
        public bool Success { get; set; }

        [Description("Status message")]
        public string Message { get; set; } = string.Empty;

        [Description("Set number just completed")]
        public int SetNumber { get; set; }

        [Description("Target sets for this exercise")]
        public int TargetSets { get; set; }

        [Description("Sets remaining")]
        public int SetsRemaining { get; set; }

        [Description("Whether all sets are complete")]
        public bool IsExerciseComplete { get; set; }

        [Description("Recommended rest time in seconds")]
        public int RestSeconds { get; set; }

        [Description("Current exercise name")]
        public string CurrentExercise { get; set; } = string.Empty;

        [Description("Reps completed")]
        public int Reps { get; set; }

        [Description("Weight used")]
        public double Weight { get; set; }
    }

    [McpResult]
    public class NextExerciseResponse
    {
        [Description("Whether moved successfully")]
        public bool Success { get; set; }

        [Description("Status message")]
        public string Message { get; set; } = string.Empty;

        [Description("Current exercise number")]
        public int ExerciseNumber { get; set; }

        [Description("Total exercises")]
        public int TotalExercises { get; set; }

        [Description("Whether this is the last exercise")]
        public bool IsLastExercise { get; set; }

        [Description("New current exercise details")]
        public CurrentExerciseOutput CurrentExercise { get; set; } = new();

        [Description("Previous performance for this exercise")]
        public PreviousPerformanceOutput? PreviousPerformance { get; set; }
    }

    public class CompleteWorkoutRequest
    {
        [Description("Rate of perceived exertion 1-10")]
        public int PerceivedEffort { get; set; } = 5;

        [Description("Energy level 1-10")]
        public int EnergyLevel { get; set; } = 5;

        [Description("Overall workout notes")]
        public string? Notes { get; set; }
    }

    [McpResult]
    public class CompleteWorkoutResponse
    {
        [Description("Whether workout was completed successfully")]
        public bool Success { get; set; }

        [Description("Status message")]
        public string Message { get; set; } = string.Empty;

        [Description("Completed workout ID")]
        public string WorkoutId { get; set; } = string.Empty;

        [Description("Workout date")]
        public DateTime Date { get; set; }

        [Description("Duration in minutes")]
        public int DurationMinutes { get; set; }

        [Description("Total exercises completed")]
        public int TotalExercises { get; set; }

        [Description("Total sets completed")]
        public int TotalSets { get; set; }

        [Description("Total volume lifted")]
        public double TotalVolume { get; set; }

        [Description("Exercises where PRs were achieved")]
        public List<string> NewPRs { get; set; } = new();

        [Description("Workout summary")]
        public WorkoutSummaryOutput Summary { get; set; } = new();
    }

    [McpResult]
    public class CancelWorkoutResponse
    {
        [Description("Whether cancellation was successful")]
        public bool Success { get; set; }

        [Description("Status message")]
        public string Message { get; set; } = string.Empty;
    }

    public class ShareWorkoutToTeamsRequest
    {
        [Description("Optional workout ID to share. If not provided, shares the most recent workout.")]
        public string? WorkoutId { get; set; }

        [Description("Optional custom message to include with the post")]
        public string? CustomMessage { get; set; }
    }

    [McpResult]
    public class ShareWorkoutToTeamsResponse
    {
        [Description("Whether the post was successful")]
        public bool Success { get; set; }

        [Description("Status message")]
        public string Message { get; set; } = string.Empty;

        [Description("ID of the shared workout")]
        public string WorkoutId { get; set; } = string.Empty;

        [Description("Type of workout shared")]
        public string WorkoutType { get; set; } = string.Empty;

        [Description("Date of the shared workout")]
        public DateTime WorkoutDate { get; set; }
    }

    // Legacy POCOs (keeping for backwards compatibility)
    public class LogWorkoutRequest
    {
        [Description("Type of workout")]
        public WorkoutType Type { get; set; } = WorkoutType.General;

        [Description("List of exercises in format: 'ExerciseName|MuscleGroup|Sets x Reps @ Weight'")]
        public List<string> Exercises { get; set; } = new();

        [Description("Duration in minutes")]
        public int DurationMinutes { get; set; } = 60;

        [Description("Date of workout")]
        public DateTime? Date { get; set; }

        [Description("Rate of perceived exertion 1-10")]
        public int PerceivedEffort { get; set; } = 5;

        [Description("Energy level 1-10")]
        public int EnergyLevel { get; set; } = 5;

        [Description("Optional notes")]
        public string? Notes { get; set; }
    }

    [McpResult]
    public class LogWorkoutResponse
    {
        [Description("Success status")]
        public bool Success { get; set; }

        [Description("Status message")]
        public string Message { get; set; } = string.Empty;

        [Description("Workout ID")]
        public string WorkoutId { get; set; } = string.Empty;

        [Description("Date")]
        public DateTime Date { get; set; }

        [Description("Type")]
        public string Type { get; set; } = string.Empty;

        [Description("Duration")]
        public int DurationMinutes { get; set; }

        [Description("Perceived effort")]
        public int PerceivedEffort { get; set; }

        [Description("Energy level")]
        public int EnergyLevel { get; set; }

        [Description("Notes")]
        public string? Notes { get; set; }

        [Description("Exercises")]
        public List<ExerciseOutput> Exercises { get; set; } = new();

        [Description("Summary")]
        public WorkoutSummaryOutput Summary { get; set; } = new();
    }

    public class ExerciseOutput
    {
        [Description("Exercise name")]
        public string Name { get; set; } = string.Empty;

        [Description("Muscle group")]
        public string MuscleGroup { get; set; } = string.Empty;

        [Description("Sets")]
        public List<SetOutput> Sets { get; set; } = new();

        [Description("Notes")]
        public string? Notes { get; set; }
    }

    public class SetOutput
    {
        [Description("Reps")]
        public int Reps { get; set; }

        [Description("Weight")]
        public double Weight { get; set; }

        [Description("RPE")]
        public int? Rpe { get; set; }

        [Description("Is PR")]
        public bool IsPR { get; set; }
    }

    public class WorkoutSummaryOutput
    {
        [Description("Exercise count")]
        public int ExerciseCount { get; set; }

        [Description("Total sets")]
        public int TotalSets { get; set; }

        [Description("Total volume")]
        public double TotalVolume { get; set; }

        [Description("Muscle groups")]
        public string MuscleGroups { get; set; } = string.Empty;
    }

    public class GetWorkoutHistoryRequest
    {
        [Description("Number of days to look back")]
        public int Days { get; set; } = 30;

        [Description("Optional workout type filter")]
        public WorkoutType? WorkoutType { get; set; }
    }

    [McpResult]
    public class GetWorkoutHistoryResponse
    {
        [Description("Time period")]
        public string Period { get; set; } = string.Empty;

        [Description("Total workouts")]
        public int TotalWorkouts { get; set; }

        [Description("Workouts")]
        public List<WorkoutHistoryItem> Workouts { get; set; } = new();

        [Description("Summary")]
        public WorkoutPeriodSummary Summary { get; set; } = new();
    }

    public class WorkoutHistoryItem
    {
        [Description("Workout ID")]
        public string Id { get; set; } = string.Empty;

        [Description("Date")]
        public DateTime Date { get; set; }

        [Description("Type")]
        public string Type { get; set; } = string.Empty;

        [Description("Duration")]
        public int DurationMinutes { get; set; }

        [Description("Perceived effort")]
        public int PerceivedEffort { get; set; }

        [Description("Energy level")]
        public int EnergyLevel { get; set; }

        [Description("Notes")]
        public string? Notes { get; set; }

        [Description("Exercises")]
        public List<ExerciseOutput> Exercises { get; set; } = new();
    }

    public class WorkoutPeriodSummary
    {
        [Description("Total sessions")]
        public int TotalSessions { get; set; }

        [Description("Total volume")]
        public double TotalVolume { get; set; }

        [Description("Average duration")]
        public int AverageSessionDuration { get; set; }

        [Description("Most trained muscle groups")]
        public List<string> MostTrainedMuscleGroups { get; set; } = new();

        [Description("Average perceived effort")]
        public double AveragePerceivedEffort { get; set; }

        [Description("Workout types")]
        public Dictionary<string, int> WorkoutTypes { get; set; } = new();
    }

    public class GetWorkoutStatsRequest
    {
        [Description("Days to analyze")]
        public int Days { get; set; } = 30;
    }

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
        if (!workouts.Any())
        {
            return new WorkoutStatistics
            {
                TotalWorkouts = 0,
                WorkoutsPerWeek = 0,
                TotalVolumeLifted = 0,
                UniqueExercises = 0,
                ConsistencyScore = 0,
                MuscleGroupDistribution = new Dictionary<string, int>(),
                WeeklyTrend = new List<WeeklyData>(),
                RecentFatigue = "Rested",
                ExerciseNames = new List<string>(),
                ExerciseProgressions = new List<ExerciseProgressionData>(),
                PersonalRecords = new List<PersonalRecordSummary>()
            };
        }

        var oldestWorkout = workouts.Min(w => w.Date);
        var newestWorkout = workouts.Max(w => w.Date);
        var actualDays = (newestWorkout - oldestWorkout).TotalDays;
        if (actualDays == 0) actualDays = 1;
        var weeks = Math.Max(0.14, actualDays / 7.0);

        var exerciseNames = workouts
            .SelectMany(w => w.Exercises)
            .Select(e => e.Name)
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        return new WorkoutStatistics
        {
            TotalWorkouts = workouts.Count,
            WorkoutsPerWeek = Math.Round(workouts.Count / weeks, 1),
            TotalVolumeLifted = workouts.Sum(w => w.Exercises.Sum(e => e.Sets.Sum(s => s.Reps * s.Weight))),
            UniqueExercises = exerciseNames.Count,
            ConsistencyScore = CalculateConsistencyScore(workouts, days),
            MuscleGroupDistribution = CalculateMuscleGroupDistribution(workouts),
            WeeklyTrend = new List<WeeklyData>(),
            RecentFatigue = "Rested",
            ExerciseNames = exerciseNames,
            ExerciseProgressions = new List<ExerciseProgressionData>(),
            PersonalRecords = new List<PersonalRecordSummary>()
        };
    }

    private double CalculateConsistencyScore(List<WorkoutSession> workouts, int days)
    {
        if (!workouts.Any()) return 0;
        var oldestWorkout = workouts.Min(w => w.Date);
        var newestWorkout = workouts.Max(w => w.Date);
        var actualDays = Math.Max(1, (newestWorkout - oldestWorkout).TotalDays);
        var uniqueWorkoutDays = workouts.Select(w => w.Date.Date).Distinct().Count();
        var expectedWorkoutsPerWeek = 4;
        var expectedTotal = (actualDays / 7.0) * expectedWorkoutsPerWeek;
        var score = (uniqueWorkoutDays / expectedTotal) * 100;
        return Math.Min(100, Math.Round(score, 1));
    }

    private Dictionary<string, int> CalculateMuscleGroupDistribution(List<WorkoutSession> workouts)
    {
        return workouts
            .SelectMany(w => w.Exercises)
            .GroupBy(e => e.MuscleGroup)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    #endregion
}
