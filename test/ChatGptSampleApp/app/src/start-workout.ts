import { App } from "@modelcontextprotocol/ext-apps";

// DOM helpers (null-safe)
const el = (id: string) => document.getElementById(id);
const show = (id: string) => el(id)?.classList.remove("hidden");
const hide = (id: string) => el(id)?.classList.add("hidden");

// Types - support both uppercase and lowercase property names from server
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

interface GetWorkoutTemplatesResponse {
  Templates?: WorkoutTemplate[];
  templates?: WorkoutTemplate[];
  Count?: number;
  count?: number;
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
}

// Active workout types
interface SetData {
  setNumber: number;
  weight: number;
  reps: number;
  completed: boolean;
}

interface ActiveExercise {
  name: string;
  muscleGroup: string;
  targetSets: number;
  targetReps: number;
  restSeconds: number;
  notes: string;
  sets: SetData[];
  completed: boolean;
}

interface ActiveWorkoutState {
  sessionId: string;
  templateName: string;
  exercises: ActiveExercise[];
  currentExerciseIndex: number;
  startTime: Date;
}

// Create app instance
const app = new App({ name: "Start Workout", version: "1.0.0" });

// State
let selectedTemplate: WorkoutTemplate | null = null;
let availableTemplates: WorkoutTemplate[] = [];
let activeWorkout: ActiveWorkoutState | null = null;
let restTimerInterval: number | null = null;
let restTimeRemaining = 0;

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

// Create a single template card element
function createTemplateCard(template: WorkoutTemplate, onSelect: (template: WorkoutTemplate, card: HTMLElement) => void): HTMLElement {
  const card = document.createElement("div");
  card.className = "template-card";
  
  // Handle both uppercase and lowercase property names
  const templateId = template.Id || template.id || "";
  const templateName = template.Name || template.name || "Unknown";
  const templateType = template.Type || template.type || "";
  const templateDesc = template.Description || template.description || "";
  const templateDuration = template.EstimatedDurationMinutes || template.estimatedDurationMinutes || 0;
  const templateExercises = template.Exercises || template.exercises || [];
  
  card.dataset.templateId = templateId;
  
  // Add click handler for selection
  card.addEventListener("click", () => onSelect(template, card));
  
  // Header
  const header = document.createElement("div");
  header.className = "template-header";
  
  const name = document.createElement("div");
  name.className = "template-name";
  name.textContent = templateName;
  
  const badge = document.createElement("div");
  badge.className = "template-badge";
  badge.textContent = templateType;
  
  header.appendChild(name);
  header.appendChild(badge);
  
  // Description
  const description = document.createElement("div");
  description.className = "template-description";
  description.textContent = templateDesc;
  
  // Stats
  const stats = document.createElement("div");
  stats.className = "template-stats";
  
  const timeStat = document.createElement("div");
  timeStat.className = "stat";
  const timeIcon = document.createElement("span");
  timeIcon.className = "stat-icon";
  timeIcon.textContent = "⏱️";
  const timeText = document.createElement("span");
  timeText.textContent = `${templateDuration} min`;
  timeStat.appendChild(timeIcon);
  timeStat.appendChild(timeText);
  
  const exerciseStat = document.createElement("div");
  exerciseStat.className = "stat";
  const exerciseIcon = document.createElement("span");
  exerciseIcon.className = "stat-icon";
  exerciseIcon.textContent = "🏋️";
  const exerciseText = document.createElement("span");
  exerciseText.textContent = `${templateExercises.length} exercises`;
  exerciseStat.appendChild(exerciseIcon);
  exerciseStat.appendChild(exerciseText);
  
  stats.appendChild(timeStat);
  stats.appendChild(exerciseStat);
  
  // Exercises preview
  const preview = document.createElement("div");
  preview.className = "exercises-preview";
  
  const previewTitle = document.createElement("h4");
  previewTitle.textContent = "Exercises";
  
  const exerciseList = document.createElement("div");
  exerciseList.className = "exercise-list";
  
  templateExercises.slice(0, 6).forEach(ex => {
    const tag = document.createElement("div");
    tag.className = "exercise-tag";
    tag.textContent = ex.Name || ex.name || "Exercise";
    exerciseList.appendChild(tag);
  });
  
  preview.appendChild(previewTitle);
  preview.appendChild(exerciseList);
  
  // Assemble card
  card.appendChild(header);
  card.appendChild(description);
  card.appendChild(stats);
  card.appendChild(preview);
  
  return card;
}

// Handle template selection
function handleTemplateSelect(template: WorkoutTemplate, card: HTMLElement): void {
  // Remove selection from all cards
  document.querySelectorAll(".template-card").forEach(c => c.classList.remove("selected"));
  
  // Select this card
  card.classList.add("selected");
  selectedTemplate = template;
  
  // Enable start button
  const templateName = template.Name || template.name || "Workout";
  const startBtn = el("start-btn") as HTMLButtonElement | null;
  if (startBtn) {
    startBtn.disabled = false;
    startBtn.textContent = `Start ${templateName}`;
  }
  
  // Notify the chat that user selected a template
  const exercises = template.Exercises || template.exercises || [];
  const exerciseNames = exercises.map(e => e.Name || e.name).join(", ");
  app.sendMessage({
    role: "user",
    content: [{ type: "text", text: `I want to do the "${templateName}" workout today. It includes: ${exerciseNames}. Starting it now!` }]
  }).catch(err => app.sendLog({ level: "error", data: `Failed to send message: ${err}` }));
}

// Start the selected workout
async function startSelectedWorkout(): Promise<void> {
  const templateId = selectedTemplate?.Id || selectedTemplate?.id;
  const templateName = selectedTemplate?.Name || selectedTemplate?.name;
  app.sendLog({ level: "info", data: `startSelectedWorkout called, selectedTemplate: ${templateName}, id: ${templateId}` });
  if (!selectedTemplate || !templateId) {
    app.sendLog({ level: "info", data: "No template selected or missing ID, returning" });
    return;
  }
  
  showLoading();
  
  try {
    app.sendLog({ level: "info", data: `Calling StartWorkoutSession with TemplateId: ${templateId}` });
    const result = await app.callServerTool({
      name: "StartWorkoutSession",
      arguments: { TemplateId: templateId }
    });
    
    app.sendLog({ level: "info", data: `Start workout result: ${JSON.stringify(result)}` });
    
    const content = result.content as Array<{ type: string; text?: string; [key: string]: any }>;
    const sessionResult = parseToolResult<StartWorkoutSessionResponse>(content);
    
    const sessionId = sessionResult?.SessionId || sessionResult?.sessionId;
    if (sessionId) {
      // Initialize active workout state from the template
      initializeActiveWorkout(sessionId, selectedTemplate);
      showActiveWorkout();
    } else {
      app.sendLog({ level: "warning", data: "No SessionId in result, going back to templates" });
      showTemplates(availableTemplates);
    }
  } catch (error) {
    app.sendLog({ level: "error", data: `Error starting workout: ${error}` });
    showTemplates(availableTemplates);
  }
}

// Initialize active workout state from template
function initializeActiveWorkout(sessionId: string, template: WorkoutTemplate): void {
  const exercises = template.Exercises || template.exercises || [];
  
  activeWorkout = {
    sessionId,
    templateName: template.Name || template.name || "Workout",
    exercises: exercises.map(ex => ({
      name: ex.Name || ex.name || "Exercise",
      muscleGroup: ex.MuscleGroup || ex.muscleGroup || "",
      targetSets: ex.TargetSets || ex.targetSets || 3,
      targetReps: ex.TargetReps || ex.targetReps || 10,
      restSeconds: ex.RestSeconds || ex.restSeconds || 60,
      notes: ex.Notes || ex.notes || "",
      sets: Array.from({ length: ex.TargetSets || ex.targetSets || 3 }, (_, i) => ({
        setNumber: i + 1,
        weight: 0,
        reps: ex.TargetReps || ex.targetReps || 10,
        completed: false
      })),
      completed: false
    })),
    currentExerciseIndex: 0,
    startTime: new Date()
  };
  
  // Update model context with workout state
  updateWorkoutContext();
}

// Update model context with current workout state
function updateWorkoutContext(): void {
  if (!activeWorkout) return;
  
  const totalSets = activeWorkout.exercises.reduce((sum, ex) => sum + ex.sets.length, 0);
  const completedSets = activeWorkout.exercises.reduce((sum, ex) => sum + ex.sets.filter(s => s.completed).length, 0);
  const currentExercise = activeWorkout.exercises[activeWorkout.currentExerciseIndex];
  
  const context = `---
workout-name: ${activeWorkout.templateName}
current-exercise: ${currentExercise?.name || 'None'}
current-exercise-index: ${activeWorkout.currentExerciseIndex + 1}
total-exercises: ${activeWorkout.exercises.length}
sets-completed: ${completedSets}
sets-total: ${totalSets}
progress-percent: ${Math.round((completedSets / totalSets) * 100)}
---

User is currently doing a ${activeWorkout.templateName} workout.
Current exercise: ${currentExercise?.name} (${currentExercise?.muscleGroup})
Target: ${currentExercise?.targetSets} sets × ${currentExercise?.targetReps} reps

Exercise list:
${activeWorkout.exercises.map((ex, i) => {
  const completedSetCount = ex.sets.filter(s => s.completed).length;
  const status = ex.completed ? '✓' : (i === activeWorkout!.currentExerciseIndex ? '→' : ' ');
  return `${status} ${i + 1}. ${ex.name} (${completedSetCount}/${ex.sets.length} sets)`;
}).join('\n')}

When the user completes sets, provide brief encouraging feedback. Suggest weight increases if they're doing well, or form tips for the current exercise.`;

  app.updateModelContext({
    content: [{ type: "text", text: context }]
  }).catch(err => app.sendLog({ level: "error", data: `Failed to update context: ${err}` }));
}

// Render the active workout view
function renderActiveWorkout(): void {
  if (!activeWorkout) return;
  
  const container = el("workout-content");
  if (!container) return;
  
  const totalSets = activeWorkout.exercises.reduce((sum, ex) => sum + ex.sets.length, 0);
  const completedSets = activeWorkout.exercises.reduce((sum, ex) => sum + ex.sets.filter(s => s.completed).length, 0);
  const progress = totalSets > 0 ? Math.round((completedSets / totalSets) * 100) : 0;
  const elapsedMinutes = Math.round((Date.now() - activeWorkout.startTime.getTime()) / 60000);
  
  const currentExercise = activeWorkout.exercises[activeWorkout.currentExerciseIndex];
  
  container.innerHTML = `
    <div class="workout-header">
      <div class="workout-title">${activeWorkout.templateName}</div>
      <div class="workout-meta">
        <span>Exercise ${activeWorkout.currentExerciseIndex + 1} of ${activeWorkout.exercises.length}</span>
        <span>${elapsedMinutes} min</span>
      </div>
      <div class="progress-bar">
        <div class="progress-fill" style="width: ${progress}%"></div>
      </div>
      <div class="progress-text">${completedSets}/${totalSets} sets completed (${progress}%)</div>
    </div>
    
    <div class="exercises-list">
      ${activeWorkout.exercises.map((ex, idx) => `
        <div class="exercise-item ${idx === activeWorkout!.currentExerciseIndex ? 'active' : ''} ${ex.completed ? 'completed' : ''}" 
             data-exercise-index="${idx}">
          <div class="exercise-item-header">
            <span class="exercise-item-name">${ex.name}</span>
            <span class="exercise-item-badge ${ex.completed ? 'done' : ''}">${ex.completed ? '✓ Done' : ex.muscleGroup}</span>
          </div>
          <div class="exercise-item-info">
            <span>${ex.targetSets} sets × ${ex.targetReps} reps</span>
            <span>${ex.sets.filter(s => s.completed).length}/${ex.sets.length} completed</span>
          </div>
        </div>
      `).join('')}
    </div>
    
    ${currentExercise ? renderExerciseDetail(currentExercise, activeWorkout.currentExerciseIndex) : ''}
    
    <div id="rest-timer-container"></div>
    
    <div class="workout-actions">
      <button class="finish-workout-btn" id="finish-workout-btn">Finish Workout</button>
      <button class="cancel-workout-btn" id="cancel-workout-btn">Cancel</button>
    </div>
  `;
  
  // Add event listeners
  container.querySelectorAll('.exercise-item').forEach(item => {
    item.addEventListener('click', () => {
      const index = parseInt(item.getAttribute('data-exercise-index') || '0');
      activeWorkout!.currentExerciseIndex = index;
      renderActiveWorkout();
    });
  });
  
  // Set up set completion buttons
  container.querySelectorAll('.set-check-btn').forEach(btn => {
    btn.addEventListener('click', handleSetComplete);
  });
  
  // Set up input changes
  container.querySelectorAll('.set-input-group input').forEach(input => {
    input.addEventListener('change', handleInputChange);
  });
  
  // Set up finish/cancel buttons
  el("finish-workout-btn")?.addEventListener('click', finishWorkout);
  el("cancel-workout-btn")?.addEventListener('click', cancelWorkout);
}

// Render exercise detail panel
function renderExerciseDetail(exercise: ActiveExercise, exerciseIndex: number): string {
  return `
    <div class="exercise-detail">
      <div class="exercise-detail-header">
        <div>
          <div class="exercise-detail-title">${exercise.name}</div>
          <div class="exercise-detail-subtitle">${exercise.muscleGroup} • ${exercise.restSeconds}s rest</div>
        </div>
      </div>
      
      ${exercise.notes ? `<div class="exercise-notes">💡 ${exercise.notes}</div>` : ''}
      
      <div class="sets-section">
        <div class="sets-title">Sets</div>
        <div class="sets-grid">
          ${exercise.sets.map((set, setIdx) => `
            <div class="set-row ${set.completed ? 'completed' : ''}" data-exercise-index="${exerciseIndex}" data-set-index="${setIdx}">
              <div class="set-number">${set.setNumber}</div>
              <div class="set-input-group">
                <label>Weight (lbs)</label>
                <input type="number" class="weight-input" value="${set.weight}" data-exercise-index="${exerciseIndex}" data-set-index="${setIdx}" ${set.completed ? 'disabled' : ''}>
              </div>
              <div class="set-input-group">
                <label>Reps</label>
                <input type="number" class="reps-input" value="${set.reps}" data-exercise-index="${exerciseIndex}" data-set-index="${setIdx}" ${set.completed ? 'disabled' : ''}>
              </div>
              <button class="set-check-btn ${set.completed ? 'completed' : ''}" data-exercise-index="${exerciseIndex}" data-set-index="${setIdx}">
                ${set.completed ? '✓' : ''}
              </button>
            </div>
          `).join('')}
        </div>
      </div>
    </div>
  `;
}

// Handle input changes for weight/reps
function handleInputChange(e: Event): void {
  const input = e.target as HTMLInputElement;
  const exerciseIndex = parseInt(input.getAttribute('data-exercise-index') || '0');
  const setIndex = parseInt(input.getAttribute('data-set-index') || '0');
  
  if (!activeWorkout) return;
  
  const value = parseInt(input.value) || 0;
  
  if (input.classList.contains('weight-input')) {
    activeWorkout.exercises[exerciseIndex].sets[setIndex].weight = value;
  } else if (input.classList.contains('reps-input')) {
    activeWorkout.exercises[exerciseIndex].sets[setIndex].reps = value;
  }
}

// Handle set completion
function handleSetComplete(e: Event): void {
  const btn = e.currentTarget as HTMLButtonElement;
  const exerciseIndex = parseInt(btn.getAttribute('data-exercise-index') || '0');
  const setIndex = parseInt(btn.getAttribute('data-set-index') || '0');
  
  if (!activeWorkout) return;
  
  const set = activeWorkout.exercises[exerciseIndex].sets[setIndex];
  set.completed = !set.completed;
  
  // Check if all sets for this exercise are complete
  const exercise = activeWorkout.exercises[exerciseIndex];
  exercise.completed = exercise.sets.every(s => s.completed);
  
  // Log the set to the server and notify chat
  if (set.completed) {
    logSetToServer(exerciseIndex, setIndex, set);
    
    // Calculate progress
    const totalSets = activeWorkout.exercises.reduce((sum, ex) => sum + ex.sets.length, 0);
    const completedSets = activeWorkout.exercises.reduce((sum, ex) => sum + ex.sets.filter(s => s.completed).length, 0);
    const progress = Math.round((completedSets / totalSets) * 100);
    
    // Notify chat about completed set - ask for encouragement
    const weightStr = set.weight > 0 ? ` at ${set.weight} lbs` : '';
    const remainingSets = totalSets - completedSets;
    
    let message = `Just completed set ${set.setNumber} of ${exercise.name}: ${set.reps} reps${weightStr}. `;
    message += `Progress: ${progress}% (${remainingSets} sets remaining). `;
    
    if (exercise.completed) {
      message += `Finished all sets for ${exercise.name}! `;
      const nextExercise = activeWorkout.exercises[exerciseIndex + 1];
      if (nextExercise) {
        message += `Moving on to ${nextExercise.name} next.`;
      }
    } else {
      message += `Resting for ${exercise.restSeconds} seconds before next set.`;
    }
    message += ` Give me some quick encouragement or tips!`;
    
    app.sendMessage({
      role: "user",
      content: [{ type: "text", text: message }]
    }).catch(err => app.sendLog({ level: "error", data: `Failed to send message: ${err}` }));
    
    // Update model context with current state
    updateWorkoutContext();
    
    // Start rest timer if not the last set
    const isLastSetOfExercise = setIndex === exercise.sets.length - 1;
    if (!isLastSetOfExercise || !exercise.completed) {
      startRestTimer(exercise.restSeconds);
    }
  }
  
  renderActiveWorkout();
}

// Log set to server
async function logSetToServer(exerciseIndex: number, setIndex: number, set: SetData): Promise<void> {
  if (!activeWorkout) return;
  
  const exercise = activeWorkout.exercises[exerciseIndex];
  
  try {
    await app.callServerTool({
      name: "LogExerciseSet",
      arguments: {
        SessionId: activeWorkout.sessionId,
        ExerciseName: exercise.name,
        SetNumber: set.setNumber,
        Reps: set.reps,
        Weight: set.weight
      }
    });
    app.sendLog({ level: "info", data: `Logged set ${set.setNumber} for ${exercise.name}` });
  } catch (error) {
    app.sendLog({ level: "error", data: `Failed to log set: ${error}` });
  }
}

// Start rest timer
function startRestTimer(seconds: number): void {
  // Clear any existing timer
  if (restTimerInterval) {
    clearInterval(restTimerInterval);
  }
  
  restTimeRemaining = seconds;
  renderRestTimer();
  
  restTimerInterval = window.setInterval(() => {
    restTimeRemaining--;
    if (restTimeRemaining <= 0) {
      clearInterval(restTimerInterval!);
      restTimerInterval = null;
      hideRestTimer();
    } else {
      renderRestTimer();
    }
  }, 1000);
}

// Render rest timer
function renderRestTimer(): void {
  const container = el("rest-timer-container");
  if (!container) return;
  
  const minutes = Math.floor(restTimeRemaining / 60);
  const seconds = restTimeRemaining % 60;
  const timeStr = `${minutes}:${seconds.toString().padStart(2, '0')}`;
  
  container.innerHTML = `
    <div class="rest-timer">
      <div class="rest-timer-label">Rest Time</div>
      <div class="rest-timer-time">${timeStr}</div>
      <button class="skip-rest-btn" id="skip-rest-btn">Skip Rest</button>
    </div>
  `;
  
  el("skip-rest-btn")?.addEventListener('click', () => {
    if (restTimerInterval) {
      clearInterval(restTimerInterval);
      restTimerInterval = null;
    }
    hideRestTimer();
  });
}

// Hide rest timer
function hideRestTimer(): void {
  const container = el("rest-timer-container");
  if (container) {
    container.innerHTML = '';
  }
}

// Finish workout
async function finishWorkout(): Promise<void> {
  if (!activeWorkout) return;
  
  showLoading();
  
  try {
    await app.callServerTool({
      name: "EndWorkoutSession",
      arguments: { SessionId: activeWorkout.sessionId }
    });
    
    // Calculate stats
    const totalSets = activeWorkout.exercises.reduce((sum, ex) => sum + ex.sets.length, 0);
    const completedSets = activeWorkout.exercises.reduce((sum, ex) => sum + ex.sets.filter(s => s.completed).length, 0);
    const completedExercises = activeWorkout.exercises.filter(ex => ex.completed).length;
    const elapsedMinutes = Math.round((Date.now() - activeWorkout.startTime.getTime()) / 60000);
    const workoutName = activeWorkout.templateName;
    
    // Build summary of what was accomplished
    const exerciseSummary = activeWorkout.exercises.map(ex => {
      const completedSetCount = ex.sets.filter(s => s.completed).length;
      if (completedSetCount === 0) return null;
      const maxWeight = Math.max(...ex.sets.filter(s => s.completed).map(s => s.weight));
      const weightStr = maxWeight > 0 ? ` (max: ${maxWeight} lbs)` : '';
      return `${ex.name}: ${completedSetCount}/${ex.sets.length} sets${weightStr}`;
    }).filter(Boolean).join(", ");
    
    // Notify chat about workout completion
    const message = `🎉 Finished my ${workoutName} workout! Stats: ${completedSets}/${totalSets} sets completed across ${completedExercises}/${activeWorkout.exercises.length} exercises in ${elapsedMinutes} minutes. Summary: ${exerciseSummary}. Give me a summary and encouragement!`;
    
    app.sendMessage({
      role: "user",
      content: [{ type: "text", text: message }]
    }).catch(err => app.sendLog({ level: "error", data: `Failed to send message: ${err}` }));
    
    const successMsg = el("success-message");
    if (successMsg) {
      successMsg.textContent = `Great job! You completed ${completedSets}/${totalSets} sets in your ${workoutName} workout.`;
    }
    
    activeWorkout = null;
    
    hide("templates-view");
    hide("active-workout-view");
    hide("loading");
    show("success-view");
  } catch (error) {
    app.sendLog({ level: "error", data: `Error finishing workout: ${error}` });
    renderActiveWorkout();
    hide("loading");
    show("active-workout-view");
  }
}

// Cancel workout
async function cancelWorkout(): Promise<void> {
  if (!activeWorkout) return;
  
  if (!confirm("Are you sure you want to cancel this workout? Progress will be lost.")) {
    return;
  }
  
  try {
    await app.callServerTool({
      name: "EndWorkoutSession",
      arguments: { SessionId: activeWorkout.sessionId }
    });
  } catch (error) {
    app.sendLog({ level: "error", data: `Error canceling workout: ${error}` });
  }
  
  activeWorkout = null;
  showTemplates(availableTemplates);
}

// Show templates view
function showTemplates(templates: WorkoutTemplate[]): void {
  const container = el("templates-container");
  if (!container) {
    console.error("[showTemplates] templates-container not found!");
    return;
  }
  availableTemplates = templates;
  
  // Clear existing content
  while (container.firstChild) {
    container.removeChild(container.firstChild);
  }
  
  // Add template cards
  templates.forEach(template => {
    const card = createTemplateCard(template, handleTemplateSelect);
    container.appendChild(card);
  });
  
  // Reset selection state
  selectedTemplate = null;
  const startBtn = el("start-btn") as HTMLButtonElement | null;
  if (startBtn) {
    startBtn.disabled = true;
    startBtn.textContent = "Select a template to start";
  }
  
  // Show templates view, hide others
  hide("success-view");
  hide("active-workout-view");
  hide("loading");
  show("templates-view");
}

// Show active workout view
function showActiveWorkout(): void {
  renderActiveWorkout();
  
  hide("templates-view");
  hide("success-view");
  hide("loading");
  show("active-workout-view");
}

// Show loading
function showLoading(): void {
  hide("templates-view");
  hide("success-view");
  hide("active-workout-view");
  show("loading");
}

// Load templates from server
async function loadTemplates(): Promise<void> {
  showLoading();
  
  try {
    console.log("[loadTemplates] Loading workout templates...");
    app.sendLog({ level: "info", data: "Loading workout templates..." });
    const result = await app.callServerTool({
      name: "GetWorkoutTemplates",
      arguments: {}
    });
    
    console.log("[loadTemplates] GetWorkoutTemplates result:", result);
    app.sendLog({ level: "info", data: `GetWorkoutTemplates result: ${JSON.stringify(result)}` });
    
    const content = result.content as Array<{ type: string; text?: string; [key: string]: any }>;
    console.log("[loadTemplates] content:", content);
    const templatesResult = parseToolResult<GetWorkoutTemplatesResponse>(content);
    console.log("[loadTemplates] parsed templatesResult:", templatesResult);
    
    // Check for both uppercase and lowercase Templates/templates
    const templates = templatesResult?.Templates || templatesResult?.templates;
    if (templates && Array.isArray(templates)) {
      console.log("[loadTemplates] Showing templates:", templates.length);
      showTemplates(templates);
    } else {
      console.error("[loadTemplates] Failed to parse templates response");
      app.sendLog({ level: "error", data: "Failed to parse templates response" });
      // Show empty state or error
      showTemplates([]);
    }
  } catch (error) {
    console.error("[loadTemplates] Error loading templates:", error);
    app.sendLog({ level: "error", data: `Error loading templates: ${error}` });
    // Show empty state on error
    showTemplates([]);
  }
}

// Register handlers
app.ontoolinput = (params) => {
  console.log("[ontoolinput] Tool input:", params.arguments);
  app.sendLog({ level: "info", data: `Tool input: ${JSON.stringify(params.arguments)}` });
};

app.ontoolresult = (params) => {
  console.log("[ontoolresult] Tool result received:", params);
  app.sendLog({ level: "info", data: `Tool result received: ${JSON.stringify(params)}` });
};

app.onhostcontextchanged = (ctx) => {
  if (ctx.theme) applyTheme(ctx.theme);
};

// Set up start button click handler
el("start-btn")?.addEventListener("click", startSelectedWorkout);

// Set up back button click handler
el("back-btn")?.addEventListener("click", () => loadTemplates());

// Connect and load templates
await app.connect();
applyTheme(app.getHostContext()?.theme);
loadTemplates();