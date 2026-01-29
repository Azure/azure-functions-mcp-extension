import { App } from "@modelcontextprotocol/ext-apps";

// Types - support both uppercase and lowercase property names
interface SetData {
    Reps?: number;
    reps?: number;
    Weight?: number;
    weight?: number;
    Rpe?: number | null;
    rpe?: number | null;
    IsPR?: boolean;
    isPR?: boolean;
}

interface ExerciseData {
    Name?: string;
    name?: string;
    MuscleGroup?: string;
    muscleGroup?: string;
    TargetSets?: number;
    targetSets?: number;
    TargetReps?: number;
    targetReps?: number;
    RestSeconds?: number;
    restSeconds?: number;
    CompletedSets?: number;
    completedSets?: number;
    Sets?: SetData[];
    sets?: SetData[];
    Notes?: string;
    notes?: string;
}

interface PreviousPerformance {
    Date?: string;
    date?: string;
    Sets?: SetData[];
    sets?: SetData[];
}

interface ActiveWorkoutResponse {
    IsActive?: boolean;
    isActive?: boolean;
    Message?: string;
    message?: string;
    SessionId?: string;
    sessionId?: string;
    TemplateName?: string;
    templateName?: string;
    Type?: string;
    type?: string;
    StartTime?: string;
    startTime?: string;
    ElapsedMinutes?: number;
    elapsedMinutes?: number;
    CurrentExerciseIndex?: number;
    currentExerciseIndex?: number;
    TotalExercises?: number;
    totalExercises?: number;
    TotalSetsCompleted?: number;
    totalSetsCompleted?: number;
    TotalSetsTarget?: number;
    totalSetsTarget?: number;
    CurrentExercise?: ExerciseData;
    currentExercise?: ExerciseData;
    PreviousPerformance?: PreviousPerformance;
    previousPerformance?: PreviousPerformance;
    Exercises?: ExerciseData[];
    exercises?: ExerciseData[];
}

// Helper functions
const el = (id: string) => document.getElementById(id);

// Create app instance
const app = new App({ name: "Active Workout", version: "1.0.0" });

// State
let workoutData: ActiveWorkoutResponse | null = null;
let timerInterval: number | null = null;
let remainingSeconds = 0;

// Apply theme
function applyTheme(theme: string | undefined): void {
    document.documentElement.dataset.theme = theme || "dark";
}

// Parse tool result - handles structured content
function parseToolResult<T>(content: Array<{ type: string; text?: string; [key: string]: any }> | undefined): T | null {
    if (!content || content.length === 0) return null;

    // Try structured content first
    const structuredBlock = content.find((c) => c.type === "resource" || (c.type !== "text" && c.type !== "tool_use"));
    if (structuredBlock) {
        const { type, ...data } = structuredBlock;
        return data as T;
    }

    // Fallback to text parsing
    const textBlock = content.find((c) => c.type === "text" && c.text);
    if (!textBlock || !textBlock.text) return null;
    
    try {
        return JSON.parse(textBlock.text) as T;
    } catch (e) {
        app.sendLog({ level: "error", data: `Parse error: ${e}` });
        return null;
    }
}

// Get property with case insensitivity
function get<T>(obj: any, upper: string, lower: string): T | undefined {
    if (!obj) return undefined;
    return obj[upper] !== undefined ? obj[upper] : obj[lower];
}

// Update model context with current workout state
function updateWorkoutContext(): void {
    if (!workoutData) return;
    
    const isActive = get<boolean>(workoutData, "IsActive", "isActive");
    if (!isActive) return;
    
    const templateName = get<string>(workoutData, "TemplateName", "templateName") || "Workout";
    const currentIndex = get<number>(workoutData, "CurrentExerciseIndex", "currentExerciseIndex") || 0;
    const totalExercises = get<number>(workoutData, "TotalExercises", "totalExercises") || 0;
    const totalSetsCompleted = get<number>(workoutData, "TotalSetsCompleted", "totalSetsCompleted") || 0;
    const totalSetsTarget = get<number>(workoutData, "TotalSetsTarget", "totalSetsTarget") || 1;
    const elapsedMinutes = get<number>(workoutData, "ElapsedMinutes", "elapsedMinutes") || 0;
    const currentExercise = get<ExerciseData>(workoutData, "CurrentExercise", "currentExercise");
    
    const progress = Math.round((totalSetsCompleted / totalSetsTarget) * 100);
    const ex = currentExercise ? getExerciseData(currentExercise) : null;
    
    const context = `---
workout-name: ${templateName}
current-exercise: ${ex?.name || 'None'}
current-exercise-index: ${currentIndex + 1}
total-exercises: ${totalExercises}
sets-completed: ${totalSetsCompleted}
sets-total: ${totalSetsTarget}
progress-percent: ${progress}
elapsed-minutes: ${elapsedMinutes}
---

User is actively tracking a ${templateName} workout using the Active Workout widget.
Current exercise: ${ex?.name || 'None'} (${ex?.muscleGroup || ''})
Target: ${ex?.targetSets || 0} sets × ${ex?.targetReps || 0} reps
Completed sets for this exercise: ${ex?.completedSets || 0}/${ex?.targetSets || 0}

Overall progress: ${totalSetsCompleted}/${totalSetsTarget} sets (${progress}%)

When the user logs sets, provide brief encouraging feedback. Suggest weight adjustments if appropriate, or form tips for the current exercise.`;

    app.updateModelContext({
        content: [{ type: "text", text: context }]
    }).catch(err => app.sendLog({ level: "error", data: `Failed to update context: ${err}` }));
}

// Render no workout screen
function renderNoWorkout(): void {
    const appEl = el("app");
    if (!appEl) return;

    appEl.innerHTML = `
        <div class="no-workout">
            <div style="font-size: 4em; margin-bottom: 20px;">🏋️</div>
            <h2>No Active Workout</h2>
            <p>Start a workout from the Start Workout widget first.</p>
        </div>
    `;
}

// Render loading
function renderLoading(): void {
    const appEl = el("app");
    if (!appEl) return;

    appEl.innerHTML = `
        <div class="loading">
            <div class="spinner"></div>
            <p>Loading workout...</p>
        </div>
    `;
}

// Get exercise display data
function getExerciseData(ex: ExerciseData) {
    return {
        name: get<string>(ex, "Name", "name") || "Exercise",
        muscleGroup: get<string>(ex, "MuscleGroup", "muscleGroup") || "",
        targetSets: get<number>(ex, "TargetSets", "targetSets") || 0,
        targetReps: get<number>(ex, "TargetReps", "targetReps") || 0,
        restSeconds: get<number>(ex, "RestSeconds", "restSeconds") || 60,
        completedSets: get<number>(ex, "CompletedSets", "completedSets") || 0,
        sets: get<SetData[]>(ex, "Sets", "sets") || [],
        notes: get<string>(ex, "Notes", "notes") || ""
    };
}

// Render workout UI with exercise list
function renderWorkout(): void {
    if (!workoutData) return;

    const appEl = el("app");
    if (!appEl) return;

    const isActive = get<boolean>(workoutData, "IsActive", "isActive");
    if (!isActive) {
        renderNoWorkout();
        return;
    }

    const templateName = get<string>(workoutData, "TemplateName", "templateName") || "Workout";
    const currentIndex = get<number>(workoutData, "CurrentExerciseIndex", "currentExerciseIndex") || 0;
    const totalExercises = get<number>(workoutData, "TotalExercises", "totalExercises") || 0;
    const totalSetsCompleted = get<number>(workoutData, "TotalSetsCompleted", "totalSetsCompleted") || 0;
    const totalSetsTarget = get<number>(workoutData, "TotalSetsTarget", "totalSetsTarget") || 1;
    const elapsedMinutes = get<number>(workoutData, "ElapsedMinutes", "elapsedMinutes") || 0;
    const currentExercise = get<ExerciseData>(workoutData, "CurrentExercise", "currentExercise");
    const previousPerformance = get<PreviousPerformance>(workoutData, "PreviousPerformance", "previousPerformance");

    const progress = Math.round((totalSetsCompleted / totalSetsTarget) * 100);

    // Build exercise info
    const ex = currentExercise ? getExerciseData(currentExercise) : null;

    appEl.innerHTML = `
        <div class="workout-header">
            <div class="workout-title">${templateName}</div>
            <div class="workout-meta">
                <span>Exercise ${currentIndex + 1} of ${totalExercises}</span>
                <span>${elapsedMinutes} min</span>
            </div>
            <div class="progress-bar">
                <div class="progress-fill" style="width: ${progress}%"></div>
            </div>
            <div class="progress-text">${totalSetsCompleted}/${totalSetsTarget} sets completed (${progress}%)</div>
        </div>

        ${ex ? renderExercisePanel(ex, previousPerformance, currentIndex, totalExercises) : ''}
    `;

    // Attach event listeners
    attachEventListeners();
}

// Render exercise logging panel
function renderExercisePanel(ex: ReturnType<typeof getExerciseData>, prevPerf: PreviousPerformance | undefined, currentIndex: number, totalExercises: number): string {
    const prevSets = prevPerf ? (get<SetData[]>(prevPerf, "Sets", "sets") || []) : [];
    const prevDate = prevPerf ? get<string>(prevPerf, "Date", "date") : null;
    const defaultWeight = prevSets.length > 0 ? (get<number>(prevSets[0], "Weight", "weight") || 0) : 0;
    
    const isComplete = ex.completedSets >= ex.targetSets;
    const isLastExercise = currentIndex >= totalExercises - 1;

    return `
        <div class="set-panel">
            <div class="set-panel-header">
                <div>
                    <div class="set-panel-title">${ex.name}</div>
                    <div class="set-panel-subtitle">${ex.muscleGroup}</div>
                </div>
                <div class="exercise-badge">Set ${ex.completedSets + 1}/${ex.targetSets}</div>
            </div>

            ${ex.notes ? `<div class="exercise-notes">💡 <strong>Tip:</strong> ${ex.notes}</div>` : ''}

            <div id="restTimer" class="rest-timer">
                <div class="timer-label">REST TIME</div>
                <div class="timer-display" id="timerDisplay">0:00</div>
            </div>

            ${prevSets.length > 0 ? `
                <div class="previous-performance">
                    <h4>📊 Last time${prevDate ? ` (${new Date(prevDate).toLocaleDateString()})` : ''}</h4>
                    <div class="sets">
                        ${prevSets.map((s, i) => `<span class="set">${i + 1}: ${get<number>(s, "Reps", "reps") || 0}×${get<number>(s, "Weight", "weight") || 0}lbs</span>`).join(' • ')}
                    </div>
                </div>
            ` : ''}

            ${ex.sets.length > 0 ? `
                <div class="completed-sets">
                    <h4>✅ Completed Sets</h4>
                    <div class="set-chips">
                        ${ex.sets.map((s, i) => {
                            const reps = get<number>(s, "Reps", "reps") || 0;
                            const weight = get<number>(s, "Weight", "weight") || 0;
                            const isPR = get<boolean>(s, "IsPR", "isPR") || false;
                            return `<div class="set-chip ${isPR ? 'pr' : ''}">${i + 1}: ${reps}×${weight}lbs</div>`;
                        }).join('')}
                    </div>
                </div>
            ` : ''}

            ${!isComplete ? `
                <div class="input-row">
                    <div class="input-group">
                        <label>Reps</label>
                        <input type="number" id="repsInput" value="${ex.targetReps}" min="1" max="100">
                    </div>
                    <div class="input-group">
                        <label>Weight (lbs)</label>
                        <input type="number" id="weightInput" value="${defaultWeight}" min="0" step="5">
                    </div>
                </div>

                <div class="quick-weight">
                    <button class="weight-adj" data-adj="-10">-10</button>
                    <button class="weight-adj" data-adj="-5">-5</button>
                    <button class="weight-adj" data-adj="+5">+5</button>
                    <button class="weight-adj" data-adj="+10">+10</button>
                </div>

                <div class="pr-checkbox">
                    <input type="checkbox" id="prCheckbox">
                    <label for="prCheckbox">🏆 This is a Personal Record!</label>
                </div>

                <button class="btn btn-primary" id="logSetBtn">
                    ✅ Log Set ${ex.completedSets + 1}
                </button>
            ` : `
                <p style="text-align: center; color: var(--success); margin: 20px 0; font-weight: 600;">
                    ✅ All sets completed for this exercise!
                </p>
            `}

            <div class="action-row">
                ${!isLastExercise ? `
                    <button class="btn btn-secondary" id="nextExerciseBtn" ${!isComplete ? 'style="opacity: 0.7"' : ''}>
                        ➡️ Next Exercise
                    </button>
                ` : ''}
                <button class="btn btn-danger" id="completeWorkoutBtn">
                    🏁 ${isLastExercise && isComplete ? 'Complete Workout' : 'End Workout'}
                </button>
            </div>
        </div>
    `;
}

// Attach event listeners
function attachEventListeners(): void {
    // Log set button
    const logSetBtn = el("logSetBtn");
    if (logSetBtn) {
        logSetBtn.addEventListener("click", logSet);
    }

    // Weight adjustment buttons
    document.querySelectorAll(".weight-adj").forEach(btn => {
        btn.addEventListener("click", (e) => {
            const adj = parseInt((e.target as HTMLElement).dataset.adj || "0");
            const weightInput = el("weightInput") as HTMLInputElement;
            if (weightInput) {
                const currentWeight = parseFloat(weightInput.value) || 0;
                weightInput.value = Math.max(0, currentWeight + adj).toString();
            }
        });
    });

    // Next exercise button
    const nextExerciseBtn = el("nextExerciseBtn");
    if (nextExerciseBtn) {
        nextExerciseBtn.addEventListener("click", nextExercise);
    }

    // Complete workout button
    const completeWorkoutBtn = el("completeWorkoutBtn");
    if (completeWorkoutBtn) {
        completeWorkoutBtn.addEventListener("click", completeWorkout);
    }
}

// Log a set
async function logSet(): Promise<void> {
    const repsInput = el("repsInput") as HTMLInputElement;
    const weightInput = el("weightInput") as HTMLInputElement;
    const prCheckbox = el("prCheckbox") as HTMLInputElement;
    const logSetBtn = el("logSetBtn") as HTMLButtonElement;

    if (!repsInput || !weightInput) return;

    const reps = parseInt(repsInput.value);
    const weight = parseFloat(weightInput.value);
    const isPR = prCheckbox?.checked || false;

    if (!reps || reps <= 0) {
        alert("Please enter a valid number of reps");
        return;
    }

    if (logSetBtn) {
        logSetBtn.disabled = true;
        logSetBtn.textContent = "Logging...";
    }

    try {
        app.sendLog({ level: "info", data: `Logging set: ${reps} reps × ${weight} lbs, PR: ${isPR}` });
        
        const result = await app.callServerTool({
            name: "LogSet",
            arguments: {
                Reps: reps,
                Weight: weight,
                IsPR: isPR
            }
        });
        
        // Parse the response
        const content = result.content as Array<{ type: string; text?: string; [key: string]: any }>;
        const data = parseToolResult<any>(content);
        
        if (data) {
            const exerciseName = data.CurrentExercise || data.currentExercise || "exercise";
            const setNumber = data.SetNumber || data.setNumber || 1;
            const targetSets = data.TargetSets || data.targetSets || 3;
            const setsRemaining = data.SetsRemaining || data.setsRemaining || 0;
            const isExerciseComplete = data.IsExerciseComplete || data.isExerciseComplete || false;
            const restSeconds = data.RestSeconds || data.restSeconds || 60;
            
            // Send message to chat for encouragement
            const weightStr = weight > 0 ? ` at ${weight} lbs` : '';
            const prStr = isPR ? ' 🏆 NEW PR!' : '';
            let message = `Just logged set ${setNumber}/${targetSets} of ${exerciseName}: ${reps} reps${weightStr}.${prStr} `;
            
            if (isExerciseComplete) {
                message += `✅ Completed all sets for ${exerciseName}! `;
            } else {
                message += `${setsRemaining} sets remaining. Resting ${restSeconds} seconds. `;
            }
            message += `Give me quick encouragement!`;
            
            app.sendMessage({
                role: "user",
                content: [{ type: "text", text: message }]
            }).catch(err => app.sendLog({ level: "error", data: `Failed to send message: ${err}` }));
            
            // Update model context
            updateWorkoutContext();
            
            // Start rest timer
            if (restSeconds > 0 && !isExerciseComplete) {
                startRestTimer(restSeconds);
            }
            
            // Reload workout data to get updated state
            loadWorkout();
        }
    } catch (error) {
        app.sendLog({ level: "error", data: `Error logging set: ${error}` });
        if (logSetBtn) {
            logSetBtn.disabled = false;
            logSetBtn.textContent = "✅ Log Set";
        }
        alert(`Error logging set: ${error}`);
    }
}

// Start rest timer
function startRestTimer(seconds: number): void {
    remainingSeconds = seconds;
    const timerEl = el("restTimer");
    if (timerEl) {
        timerEl.classList.add("active");
    }

    if (timerInterval) clearInterval(timerInterval);

    updateTimerDisplay();
    timerInterval = window.setInterval(() => {
        remainingSeconds--;
        updateTimerDisplay();

        if (remainingSeconds <= 0) {
            if (timerInterval) clearInterval(timerInterval);
            if (timerEl) timerEl.classList.remove("active");
            playBeep();
        }
    }, 1000);
}

// Update timer display
function updateTimerDisplay(): void {
    const minutes = Math.floor(remainingSeconds / 60);
    const seconds = remainingSeconds % 60;
    const display = `${minutes}:${seconds.toString().padStart(2, "0")}`;
    const timerDisplay = el("timerDisplay");
    if (timerDisplay) {
        timerDisplay.textContent = display;
    }
}

// Play beep sound
function playBeep(): void {
    try {
        const audioContext = new (window.AudioContext || (window as any).webkitAudioContext)();
        const oscillator = audioContext.createOscillator();
        const gainNode = audioContext.createGain();

        oscillator.connect(gainNode);
        gainNode.connect(audioContext.destination);

        oscillator.frequency.value = 800;
        oscillator.type = "sine";

        gainNode.gain.setValueAtTime(0.3, audioContext.currentTime);
        gainNode.gain.exponentialRampToValueAtTime(0.01, audioContext.currentTime + 0.5);

        oscillator.start(audioContext.currentTime);
        oscillator.stop(audioContext.currentTime + 0.5);
    } catch (e) {
        console.log("Audio not supported");
    }
}

// Move to next exercise
async function nextExercise(): Promise<void> {
    try {
        app.sendLog({ level: "info", data: "Moving to next exercise" });
        
        const result = await app.callServerTool({
            name: "NextExercise",
            arguments: {}
        });
        
        // Parse response
        const content = result.content as Array<{ type: string; text?: string; [key: string]: any }>;
        const data = parseToolResult<any>(content);
        
        if (data) {
            const currentExercise = data.CurrentExercise || data.currentExercise;
            const exerciseName = currentExercise?.Name || currentExercise?.name || "next exercise";
            const exerciseNumber = data.ExerciseNumber || data.exerciseNumber || 1;
            const totalExercises = data.TotalExercises || data.totalExercises || 1;
            const isLastExercise = data.IsLastExercise || data.isLastExercise || false;
            
            // Notify chat about moving to next exercise
            let message = `Moving to exercise ${exerciseNumber}/${totalExercises}: ${exerciseName}. `;
            if (isLastExercise) {
                message += `This is the last exercise! `;
            }
            message += `Any tips for ${exerciseName}?`;
            
            app.sendMessage({
                role: "user",
                content: [{ type: "text", text: message }]
            }).catch(err => app.sendLog({ level: "error", data: `Failed to send message: ${err}` }));
            
            // Update model context
            updateWorkoutContext();
            
            // Reload workout
            loadWorkout();
        }
    } catch (error) {
        app.sendLog({ level: "error", data: `Error moving to next exercise: ${error}` });
        alert(`Error: ${error}`);
    }
}

// Complete workout
async function completeWorkout(): Promise<void> {
    if (!confirm("Are you sure you want to complete this workout?")) return;

    try {
        app.sendLog({ level: "info", data: "Completing workout" });
        
        const result = await app.callServerTool({
            name: "CompleteWorkout",
            arguments: {
                PerceivedEffort: 7,
                EnergyLevel: 7
            }
        });
        
        // Parse response
        const content = result.content as Array<{ type: string; text?: string; [key: string]: any }>;
        const data = parseToolResult<any>(content);
        
        if (data) {
            const totalSets = data.TotalSets || data.totalSets || 0;
            const duration = data.DurationMinutes || data.durationMinutes || 0;
            const totalVolume = data.TotalVolume || data.totalVolume || 0;
            const newPRs = data.NewPRs || data.newPRs || [];
            const summary = data.Summary || data.summary;
            const exerciseCount = summary?.ExerciseCount || summary?.exerciseCount || 0;
            const muscleGroups = summary?.MuscleGroups || summary?.muscleGroups || "";
            
            // Send completion message to chat
            let message = `🎉 Workout Complete! Stats:\n`;
            message += `• Duration: ${duration} minutes\n`;
            message += `• Total Sets: ${totalSets}\n`;
            message += `• Total Volume: ${totalVolume} lbs\n`;
            message += `• Exercises: ${exerciseCount}\n`;
            message += `• Muscle Groups: ${muscleGroups}\n`;
            
            if (newPRs.length > 0) {
                message += `• 🏆 New PRs: ${newPRs.join(", ")}\n`;
            }
            
            message += `\nGive me a celebratory summary and tell me what to focus on next time!`;
            
            app.sendMessage({
                role: "user",
                content: [{ type: "text", text: message }]
            }).catch(err => app.sendLog({ level: "error", data: `Failed to send message: ${err}` }));
            
            showCompletion(data);
        }
    } catch (error) {
        app.sendLog({ level: "error", data: `Error completing workout: ${error}` });
        alert(`Error: ${error}`);
    }
}

// Show completion screen
function showCompletion(data?: any): void {
    const appEl = el("app");
    if (!appEl) return;

    const totalSets = data?.TotalSets || data?.totalSets || get<number>(workoutData!, "TotalSetsCompleted", "totalSetsCompleted") || 0;
    const duration = data?.DurationMinutes || data?.durationMinutes || get<number>(workoutData!, "ElapsedMinutes", "elapsedMinutes") || 0;

    appEl.innerHTML = `
        <div class="completion-screen">
            <div class="completion-icon">🎉</div>
            <h2>Workout Complete!</h2>
            <p style="color: var(--text-secondary);">Great job finishing your workout!</p>
            
            <div class="completion-stats">
                <div class="stat-box">
                    <div class="value">${totalSets}</div>
                    <div class="label">Sets Completed</div>
                </div>
                <div class="stat-box">
                    <div class="value">${duration}</div>
                    <div class="label">Minutes</div>
                </div>
            </div>
            
            <p style="color: var(--text-secondary); margin-top: 20px;">
                Check the Workout Summary widget for detailed stats.
            </p>
        </div>
    `;
}

// Load workout data
async function loadWorkout(): Promise<void> {
    renderLoading();
    
    try {
        app.sendLog({ level: "info", data: "Loading active workout..." });
        
        const result = await app.callServerTool({
            name: "GetActiveWorkout",
            arguments: {}
        });

        app.sendLog({ level: "info", data: `GetActiveWorkout result: ${JSON.stringify(result)}` });

        const content = result.content as Array<{ type: string; text?: string; [key: string]: any }>;
        const data = parseToolResult<ActiveWorkoutResponse>(content);

        if (data) {
            workoutData = data;
            const isActive = get<boolean>(data, "IsActive", "isActive");
            if (isActive) {
                renderWorkout();
                // Update model context when workout is loaded
                updateWorkoutContext();
            } else {
                renderNoWorkout();
            }
        } else {
            renderNoWorkout();
        }
    } catch (error) {
        app.sendLog({ level: "error", data: `Error loading workout: ${error}` });
        renderNoWorkout();
    }
}

// Register handlers
app.ontoolinput = (params) => {
    app.sendLog({ level: "info", data: `Tool input: ${JSON.stringify(params.arguments)}` });
};

app.ontoolresult = (params) => {
    app.sendLog({ level: "info", data: `Tool result: ${JSON.stringify(params)}` });

    // Try structuredContent first
    let data: any = null;
    
    if (params.structuredContent) {
        data = params.structuredContent;
    } else if (params.content) {
        const content = params.content as Array<{ type: string; text?: string; [key: string]: any }>;
        data = parseToolResult<any>(content);
    }

    if (!data) {
        app.sendLog({ level: "warning", data: "Could not parse tool result" });
        return;
    }

    // Check for workout completion response
    const isCompleted = data.IsCompleted || data.isCompleted;
    if (isCompleted) {
        showCompletion(data);
        return;
    }

    // Check for active workout response
    const isActive = data.IsActive || data.isActive;
    if (isActive !== undefined) {
        workoutData = data as ActiveWorkoutResponse;
        if (isActive) {
            renderWorkout();
        } else {
            renderNoWorkout();
        }
        return;
    }

    // Check for log set response (has RestSeconds)
    const restSeconds = data.RestSeconds || data.restSeconds;
    if (restSeconds !== undefined) {
        // Start rest timer
        if (restSeconds > 0) {
            startRestTimer(restSeconds);
        }
        // Reload workout data to get updated state
        loadWorkout();
        return;
    }

    // Check for next exercise response (has ExerciseNumber)
    const exerciseNumber = data.ExerciseNumber || data.exerciseNumber;
    if (exerciseNumber !== undefined) {
        loadWorkout();
        return;
    }

    // Default: reload workout
    loadWorkout();
};

app.onhostcontextchanged = (ctx) => {
    if (ctx.theme) applyTheme(ctx.theme);
};

// Connect and load workout
await app.connect();
applyTheme(app.getHostContext()?.theme);
loadWorkout();
