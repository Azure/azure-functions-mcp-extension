import { App } from "@modelcontextprotocol/ext-apps";

// DOM helpers
const el = (id: string) => document.getElementById(id);
const show = (id: string) => el(id)?.classList.remove("hidden");
const hide = (id: string) => el(id)?.classList.add("hidden");

// ============= TYPES =============

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

interface ExerciseTemplate {
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

interface WorkoutTemplate {
  Id?: string;
  id?: string;
  Name?: string;
  name?: string;
  Type?: string;
  type?: string;
  Description?: string;
  description?: string;
  EstimatedDurationMinutes?: number;
  estimatedDurationMinutes?: number;
  Exercises?: ExerciseTemplate[];
  exercises?: ExerciseTemplate[];
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
  CurrentExercise?: ExerciseTemplate;
  currentExercise?: ExerciseTemplate;
  PreviousPerformance?: PreviousPerformance;
  previousPerformance?: PreviousPerformance;
  Exercises?: ExerciseTemplate[];
  exercises?: ExerciseTemplate[];
}

interface GetWorkoutTemplatesResponse {
  Templates?: WorkoutTemplate[];
  templates?: WorkoutTemplate[];
  Count?: number;
  count?: number;
}

interface ExercisePreview {
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
}

interface StartWorkoutSessionResponse {
  Success?: boolean;
  success?: boolean;
  Message?: string;
  message?: string;
  SessionId?: string;
  sessionId?: string;
  TemplateName?: string;
  templateName?: string;
  Type?: string;
  type?: string;
  TotalExercises?: number;
  totalExercises?: number;
  EstimatedDurationMinutes?: number;
  estimatedDurationMinutes?: number;
  FirstExercise?: ExercisePreview;
  firstExercise?: ExercisePreview;
}

// Create app instance
const app = new App({ name: "Workout Tracker", version: "1.0.0" });

// ============= STATE =============

let selectedTemplate: WorkoutTemplate | null = null;
let availableTemplates: WorkoutTemplate[] = [];
let workoutData: ActiveWorkoutResponse | null = null;
let timerInterval: number | null = null;
let remainingSeconds = 0;
// eslint-disable-next-line @typescript-eslint/no-unused-vars
let currentView: "templates" | "workout" | "complete" = "templates";

// ============= UTILITIES =============

function applyTheme(theme: string | undefined): void {
  document.documentElement.dataset.theme = theme || "dark";
}

function parseToolResult<T>(content: Array<{ type: string; text?: string; [key: string]: any }> | undefined): T | null {
  if (!content || content.length === 0) return null;

  const structuredBlock = content.find((c) => c.type === "resource" || (c.type !== "text" && c.type !== "tool_use"));
  if (structuredBlock) {
    const { type, ...data } = structuredBlock;
    return data as T;
  }

  const textBlock = content.find((c) => c.type === "text" && c.text);
  if (!textBlock || !textBlock.text) return null;
  
  try {
    return JSON.parse(textBlock.text) as T;
  } catch (e) {
    app.sendLog({ level: "error", data: `Parse error: ${e}` });
    return null;
  }
}

function get<T>(obj: any, upper: string, lower: string): T | undefined {
  if (!obj) return undefined;
  return obj[upper] !== undefined ? obj[upper] : obj[lower];
}

// ============= TEMPLATE SELECTION VIEW =============

function createTemplateCard(template: WorkoutTemplate, onSelect: (template: WorkoutTemplate, card: HTMLElement) => void): HTMLElement {
  const card = document.createElement("div");
  card.className = "template-card";
  
  const templateId = template.Id || template.id || "";
  const templateName = template.Name || template.name || "Unknown";
  const templateType = template.Type || template.type || "";
  const templateDesc = template.Description || template.description || "";
  const templateDuration = template.EstimatedDurationMinutes || template.estimatedDurationMinutes || 0;
  const templateExercises = template.Exercises || template.exercises || [];
  
  card.dataset.templateId = templateId;
  card.addEventListener("click", () => onSelect(template, card));
  
  card.innerHTML = `
    <div class="template-header">
      <div class="template-name">${templateName}</div>
      <div class="template-badge">${templateType}</div>
    </div>
    <div class="template-description">${templateDesc}</div>
    <div class="template-stats">
      <div class="stat"><span class="stat-icon">⏱️</span><span>${templateDuration} min</span></div>
      <div class="stat"><span class="stat-icon">🏋️</span><span>${templateExercises.length} exercises</span></div>
    </div>
    <div class="exercises-preview">
      <h4>Exercises</h4>
      <div class="exercise-list">
        ${templateExercises.slice(0, 6).map(ex => `<div class="exercise-tag">${ex.Name || ex.name || "Exercise"}</div>`).join('')}
      </div>
    </div>
  `;
  
  return card;
}

function handleTemplateSelect(template: WorkoutTemplate, card: HTMLElement): void {
  document.querySelectorAll(".template-card").forEach(c => c.classList.remove("selected"));
  card.classList.add("selected");
  selectedTemplate = template;
  
  const templateName = template.Name || template.name || "Workout";
  const startBtn = el("start-btn") as HTMLButtonElement | null;
  if (startBtn) {
    startBtn.disabled = false;
    startBtn.textContent = `Start ${templateName}`;
  }
}

function showTemplatesView(templates: WorkoutTemplate[]): void {
  currentView = "templates";
  availableTemplates = templates;
  
  const container = el("templates-container");
  if (!container) return;
  
  container.innerHTML = "";
  templates.forEach(template => {
    const card = createTemplateCard(template, handleTemplateSelect);
    container.appendChild(card);
  });
  
  selectedTemplate = null;
  const startBtn = el("start-btn") as HTMLButtonElement | null;
  if (startBtn) {
    startBtn.disabled = true;
    startBtn.textContent = "Select a template to start";
  }
  
  hide("loading");
  hide("workout-view");
  hide("complete-view");
  show("templates-view");
}

async function loadTemplates(): Promise<void> {
  hide("templates-view");
  hide("workout-view");
  hide("complete-view");
  show("loading");
  
  try {
    app.sendLog({ level: "info", data: "Loading workout templates..." });
    const result = await app.callServerTool({
      name: "GetWorkoutTemplates",
      arguments: {}
    });
    
    const content = result.content as Array<{ type: string; text?: string; [key: string]: any }>;
    const templatesResult = parseToolResult<GetWorkoutTemplatesResponse>(content);
    
    const templates = templatesResult?.Templates || templatesResult?.templates;
    if (templates && Array.isArray(templates)) {
      showTemplatesView(templates);
    } else {
      app.sendLog({ level: "error", data: "Failed to parse templates response" });
      showTemplatesView([]);
    }
  } catch (error) {
    app.sendLog({ level: "error", data: `Error loading templates: ${error}` });
    showTemplatesView([]);
  }
}

// ============= START WORKOUT =============

async function startSelectedWorkout(): Promise<void> {
  const templateId = selectedTemplate?.Id || selectedTemplate?.id;
  const templateName = selectedTemplate?.Name || selectedTemplate?.name || "Workout";
  const exercises = selectedTemplate?.Exercises || selectedTemplate?.exercises || [];
  
  if (!selectedTemplate || !templateId) {
    app.sendLog({ level: "info", data: "No template selected" });
    return;
  }
  
  hide("templates-view");
  show("loading");
  
  try {
    app.sendLog({ level: "info", data: `Calling StartWorkoutSession with TemplateId: ${templateId}` });
    
    const result = await app.callServerTool({
      name: "StartWorkoutSession",
      arguments: { TemplateId: templateId }
    });
    
    // Parse the StartWorkoutSession response
    const content = result.content as Array<{ type: string; text?: string; [key: string]: any }>;
    const sessionResponse = parseToolResult<StartWorkoutSessionResponse>(content);
    
    // Build context from response data
    const sessionId = sessionResponse?.SessionId || sessionResponse?.sessionId || "unknown";
    const workoutType = sessionResponse?.Type || sessionResponse?.type || "";
    const totalExercises = sessionResponse?.TotalExercises || sessionResponse?.totalExercises || exercises.length;
    const duration = sessionResponse?.EstimatedDurationMinutes || sessionResponse?.estimatedDurationMinutes || 0;
    const firstEx = sessionResponse?.FirstExercise || sessionResponse?.firstExercise;
    const firstExerciseName = firstEx?.Name || firstEx?.name || "";
    const firstExerciseMuscle = firstEx?.MuscleGroup || firstEx?.muscleGroup || "";
    const firstExerciseSets = firstEx?.TargetSets || firstEx?.targetSets || 0;
    const firstExerciseReps = firstEx?.TargetReps || firstEx?.targetReps || 0;
    
    const exerciseList = exercises.map(e => e.Name || e.name).join(", ");
    
    // Update model context with full session details (silent, for LLM reference)
    app.updateModelContext({
      content: [{
        type: "text",
        text: `User started a ${templateName} (${workoutType}) workout session via the widget.

Session Details:
- Session ID: ${sessionId}
- Total Exercises: ${totalExercises}
- Estimated Duration: ${duration} minutes
- Exercises: ${exerciseList}

First Exercise: ${firstExerciseName} (${firstExerciseMuscle})
- Target: ${firstExerciseSets} sets × ${firstExerciseReps} reps

The user is now tracking their workout in the widget. Provide encouragement and be ready to help with form tips or motivation for ${firstExerciseName}.`
      }]
    }).catch(err => app.sendLog({ level: "error", data: `Failed to update context: ${err}` }));
    
    // After starting, load the active workout
    await loadActiveWorkout();
    
  } catch (error) {
    app.sendLog({ level: "error", data: `Error starting workout: ${error}` });
    showTemplatesView(availableTemplates);
  }
}

// ============= ACTIVE WORKOUT VIEW =============

function getExerciseData(ex: ExerciseTemplate) {
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

function renderWorkoutView(): void {
  if (!workoutData) return;
  currentView = "workout";

  const container = el("workout-container");
  if (!container) return;

  const isActive = get<boolean>(workoutData, "IsActive", "isActive");
  if (!isActive) {
    loadTemplates();
    return;
  }

  const templateName = get<string>(workoutData, "TemplateName", "templateName") || "Workout";
  const currentIndex = get<number>(workoutData, "CurrentExerciseIndex", "currentExerciseIndex") || 0;
  const totalExercises = get<number>(workoutData, "TotalExercises", "totalExercises") || 0;
  const totalSetsCompleted = get<number>(workoutData, "TotalSetsCompleted", "totalSetsCompleted") || 0;
  const totalSetsTarget = get<number>(workoutData, "TotalSetsTarget", "totalSetsTarget") || 1;
  const elapsedMinutes = get<number>(workoutData, "ElapsedMinutes", "elapsedMinutes") || 0;
  const currentExercise = get<ExerciseTemplate>(workoutData, "CurrentExercise", "currentExercise");
  const previousPerformance = get<PreviousPerformance>(workoutData, "PreviousPerformance", "previousPerformance");

  const progress = Math.round((totalSetsCompleted / totalSetsTarget) * 100);
  const ex = currentExercise ? getExerciseData(currentExercise) : null;

  container.innerHTML = `
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

  hide("loading");
  hide("templates-view");
  hide("complete-view");
  show("workout-view");

  attachWorkoutEventListeners();
}

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
        <div class="timer-label">⏱️ REST TIME</div>
        <div class="timer-display" id="timerDisplay">0:00</div>
        <button class="skip-rest-btn" id="skipRestBtn">Skip Rest →</button>
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
              return `<div class="set-chip">${i + 1}: ${reps}×${weight}lbs</div>`;
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

function attachWorkoutEventListeners(): void {
  el("logSetBtn")?.addEventListener("click", logSet);
  
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

  el("skipRestBtn")?.addEventListener("click", skipRestTimer);
  el("nextExerciseBtn")?.addEventListener("click", nextExercise);
  el("completeWorkoutBtn")?.addEventListener("click", completeWorkout);
}

async function loadActiveWorkout(): Promise<void> {
  hide("templates-view");
  hide("workout-view");
  hide("complete-view");
  show("loading");
  
  try {
    app.sendLog({ level: "info", data: "Loading active workout..." });
    
    const result = await app.callServerTool({
      name: "GetActiveWorkout",
      arguments: {}
    });

    const content = result.content as Array<{ type: string; text?: string; [key: string]: any }>;
    const data = parseToolResult<ActiveWorkoutResponse>(content);

    if (data) {
      workoutData = data;
      const isActive = get<boolean>(data, "IsActive", "isActive");
      if (isActive) {
        renderWorkoutView();
      } else {
        loadTemplates();
      }
    } else {
      loadTemplates();
    }
  } catch (error) {
    app.sendLog({ level: "error", data: `Error loading workout: ${error}` });
    loadTemplates();
  }
}

// ============= WORKOUT ACTIONS =============

async function logSet(): Promise<void> {
  const repsInput = el("repsInput") as HTMLInputElement;
  const weightInput = el("weightInput") as HTMLInputElement;
  const logSetBtn = el("logSetBtn") as HTMLButtonElement;

  if (!repsInput || !weightInput) return;

  const reps = parseInt(repsInput.value);
  const weight = parseFloat(weightInput.value);

  if (!reps || reps <= 0) {
    alert("Please enter a valid number of reps");
    return;
  }

  if (logSetBtn) {
    logSetBtn.disabled = true;
    logSetBtn.textContent = "Logging...";
  }

  try {
    app.sendLog({ level: "info", data: `Logging set: ${reps} reps × ${weight} lbs` });
    
    const result = await app.callServerTool({
      name: "LogSet",
      arguments: {
        Reps: reps,
        Weight: weight
      }
    });
    
    const content = result.content as Array<{ type: string; text?: string; [key: string]: any }>;
    const data = parseToolResult<any>(content);
    
    if (data) {
      const isExerciseComplete = data.IsExerciseComplete || data.isExerciseComplete || false;
      const setNumber = data.SetNumber || data.setNumber || 0;
      const exerciseName = workoutData?.CurrentExercise?.Name || workoutData?.CurrentExercise?.name || workoutData?.currentExercise?.Name || workoutData?.currentExercise?.name || "exercise";
      const templateName = workoutData?.TemplateName || workoutData?.templateName || "workout";
      
      // Update LLM context with set completion info
      app.updateModelContext({
        content: [{
          type: "text",
          text: `[Workout Update] User completed set ${setNumber} of ${exerciseName}: ${reps} reps × ${weight} lbs during their ${templateName} workout.${isExerciseComplete ? " All sets for this exercise are now complete!" : " Resting before next set."}`
        }]
      }).catch(err => app.sendLog({ level: "error", data: `Failed to update context: ${err}` }));
      
      await loadActiveWorkout();
      
      // Start 60 second rest timer unless exercise is complete (after render so timer element exists)
      if (!isExerciseComplete) {
        // Small delay to ensure DOM is fully updated
        setTimeout(() => {
          startRestTimer(60);
        }, 100);
      }
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

async function nextExercise(): Promise<void> {
  const previousExercise = workoutData?.CurrentExercise?.Name || workoutData?.CurrentExercise?.name || workoutData?.currentExercise?.Name || workoutData?.currentExercise?.name || "previous exercise";
  const currentIndex = get<number>(workoutData, "CurrentExerciseIndex", "currentExerciseIndex") || 0;
  const totalExercises = get<number>(workoutData, "TotalExercises", "totalExercises") || 0;
  const templateName = workoutData?.TemplateName || workoutData?.templateName || "workout";
  
  try {
    app.sendLog({ level: "info", data: "Moving to next exercise" });
    
    await app.callServerTool({
      name: "NextExercise",
      arguments: {}
    });
    
    await loadActiveWorkout();
    
    // Get the new exercise name after loading
    const newExercise = workoutData?.CurrentExercise?.Name || workoutData?.CurrentExercise?.name || workoutData?.currentExercise?.Name || workoutData?.currentExercise?.name || "next exercise";
    const newMuscleGroup = workoutData?.CurrentExercise?.MuscleGroup || workoutData?.CurrentExercise?.muscleGroup || workoutData?.currentExercise?.MuscleGroup || workoutData?.currentExercise?.muscleGroup || "";
    const targetSets = workoutData?.CurrentExercise?.TargetSets || workoutData?.CurrentExercise?.targetSets || workoutData?.currentExercise?.TargetSets || workoutData?.currentExercise?.targetSets || 0;
    const targetReps = workoutData?.CurrentExercise?.TargetReps || workoutData?.CurrentExercise?.targetReps || workoutData?.currentExercise?.TargetReps || workoutData?.currentExercise?.targetReps || 0;
    
    // Update LLM context with exercise transition
    app.updateModelContext({
      content: [{
        type: "text",
        text: `[Workout Update] User moved from ${previousExercise} to ${newExercise}${newMuscleGroup ? ` (${newMuscleGroup})` : ""} in their ${templateName} workout. Now on exercise ${currentIndex + 2} of ${totalExercises}. Target: ${targetSets} sets × ${targetReps} reps. Be ready to provide form tips or motivation!`
      }]
    }).catch(err => app.sendLog({ level: "error", data: `Failed to update context: ${err}` }));
    
  } catch (error) {
    app.sendLog({ level: "error", data: `Error moving to next exercise: ${error}` });
    alert(`Error: ${error}`);
  }
}

async function completeWorkout(): Promise<void> {
  app.sendLog({ level: "info", data: "Complete workout button clicked" });

  const templateName = workoutData?.TemplateName || workoutData?.templateName || "workout";
  const totalSetsCompleted = get<number>(workoutData, "TotalSetsCompleted", "totalSetsCompleted") || 0;
  const elapsedMinutes = get<number>(workoutData, "ElapsedMinutes", "elapsedMinutes") || 0;

  // Disable button to prevent double-clicks
  const btn = el("completeWorkoutBtn") as HTMLButtonElement;
  if (btn) {
    btn.disabled = true;
    btn.textContent = "Completing...";
  }

  try {
    app.sendLog({ level: "info", data: "Calling CompleteWorkout tool..." });
    
    const result = await app.callServerTool({
      name: "CompleteWorkout",
      arguments: {
        PerceivedEffort: 7,
        EnergyLevel: 7
      }
    });
    
    app.sendLog({ level: "info", data: "CompleteWorkout tool returned" });
    
    const content = result.content as Array<{ type: string; text?: string; [key: string]: any }>;
    const data = parseToolResult<any>(content);
    
    if (data) {
      const finalSets = data.TotalSets || data.totalSets || totalSetsCompleted;
      const finalDuration = data.DurationMinutes || data.durationMinutes || elapsedMinutes;
      
      // Update LLM context with workout completion
      app.updateModelContext({
        content: [{
          type: "text",
          text: `[Workout Complete] User finished their ${templateName} workout! 🎉 Total: ${finalSets} sets completed in ${finalDuration} minutes. Congratulate them and offer recovery tips or ask how they felt about the workout!`
        }]
      }).catch(err => app.sendLog({ level: "error", data: `Failed to update context: ${err}` }));
      
      showCompletionView(data);
    } else {
      app.sendLog({ level: "error", data: "No data returned from CompleteWorkout" });
      if (btn) {
        btn.disabled = false;
        btn.textContent = "🏁 End Workout";
      }
    }
  } catch (error) {
    app.sendLog({ level: "error", data: `Error completing workout: ${error}` });
    if (btn) {
      btn.disabled = false;
      btn.textContent = "🏁 End Workout";
    }
  }
}

// ============= TIMER =============

function startRestTimer(seconds: number): void {
  app.sendLog({ level: "info", data: `Starting rest timer for ${seconds} seconds` });
  
  remainingSeconds = seconds;
  const timerEl = el("restTimer");
  
  if (!timerEl) {
    app.sendLog({ level: "error", data: "Timer element not found!" });
    return;
  }
  
  app.sendLog({ level: "info", data: "Timer element found, showing timer" });
  timerEl.classList.add("active");
  timerEl.style.display = "block"; // Force display as backup

  if (timerInterval) clearInterval(timerInterval);

  updateTimerDisplay();
  timerInterval = window.setInterval(() => {
    remainingSeconds--;
    updateTimerDisplay();

    if (remainingSeconds <= 0) {
      if (timerInterval) clearInterval(timerInterval);
      timerEl.classList.remove("active");
      timerEl.style.display = "none";
      playBeep();
    }
  }, 1000);
}

function skipRestTimer(): void {
  if (timerInterval) clearInterval(timerInterval);
  const timerEl = el("restTimer");
  if (timerEl) {
    timerEl.classList.remove("active");
    timerEl.style.display = "none";
  }
  remainingSeconds = 0;
}

function updateTimerDisplay(): void {
  const minutes = Math.floor(remainingSeconds / 60);
  const seconds = remainingSeconds % 60;
  const display = `${minutes}:${seconds.toString().padStart(2, "0")}`;
  const timerDisplay = el("timerDisplay");
  if (timerDisplay) timerDisplay.textContent = display;
}

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

// ============= COMPLETION VIEW =============

function showCompletionView(data?: any): void {
  currentView = "complete";
  
  const container = el("complete-container");
  if (!container) return;

  const totalSets = data?.TotalSets || data?.totalSets || get<number>(workoutData!, "TotalSetsCompleted", "totalSetsCompleted") || 0;
  const duration = data?.DurationMinutes || data?.durationMinutes || get<number>(workoutData!, "ElapsedMinutes", "elapsedMinutes") || 0;

  container.innerHTML = `
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
      
      <button class="btn btn-primary" id="newWorkoutBtn" style="margin-top: 24px;">
        🏋️ Start New Workout
      </button>
    </div>
  `;

  hide("loading");
  hide("templates-view");
  hide("workout-view");
  show("complete-view");

  el("newWorkoutBtn")?.addEventListener("click", loadTemplates);
}

// ============= INITIALIZATION =============

// Register handlers
app.ontoolinput = (params: any) => {
  app.sendLog({ level: "info", data: `Tool input: ${JSON.stringify(params.arguments)}` });
  
  const toolName = (params.name || "").toLowerCase();
  
  // If GetWorkoutTemplates or StartWorkoutSession is called by host, show template selection
  if (toolName === "getworkouttemplates" || toolName === "startworkoutsession") {
    loadTemplates();
  } 
  // For GetActiveWorkout called by host, load the active workout view
  else if (toolName === "getactiveworkout") {
    loadActiveWorkout();
  }
};

app.ontoolresult = (_params) => {
  app.sendLog({ level: "info", data: `Tool result received` });
};

app.onhostcontextchanged = (ctx) => {
  if (ctx.theme) applyTheme(ctx.theme);
};

// Set up start button
el("start-btn")?.addEventListener("click", startSelectedWorkout);

// Connect and initialize
await app.connect();
applyTheme(app.getHostContext()?.theme);

// On startup, check if there's an active workout - if so, go to workout view, otherwise show templates
checkInitialState();

async function checkInitialState(): Promise<void> {
  show("loading");
  
  try {
    const result = await app.callServerTool({
      name: "GetActiveWorkout",
      arguments: {}
    });
    
    const content = result.content as Array<{ type: string; text?: string; [key: string]: any }>;
    const data = parseToolResult<ActiveWorkoutResponse>(content);
    
    if (data && (data.IsActive || data.isActive)) {
      // There's an active workout, go straight to logging view
      workoutData = data;
      renderWorkoutView();
    } else {
      // No active workout, show template selection
      loadTemplates();
    }
  } catch (error) {
    app.sendLog({ level: "error", data: `Error checking initial state: ${error}` });
    // Default to templates on error
    loadTemplates();
  }
}
