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

interface ExerciseData {
  Name?: string;
  name?: string;
  MuscleGroup?: string;
  muscleGroup?: string;
  Sets?: SetData[];
  sets?: SetData[];
  Notes?: string;
  notes?: string;
}

interface WorkoutSummaryData {
  ExerciseCount?: number;
  exerciseCount?: number;
  TotalSets?: number;
  totalSets?: number;
  TotalVolume?: number;
  totalVolume?: number;
  MuscleGroups?: string;
  muscleGroups?: string;
}

interface CompleteWorkoutResponse {
  Success?: boolean;
  success?: boolean;
  Message?: string;
  message?: string;
  WorkoutId?: string;
  workoutId?: string;
  Date?: string;
  date?: string;
  DurationMinutes?: number;
  durationMinutes?: number;
  TotalExercises?: number;
  totalExercises?: number;
  TotalSets?: number;
  totalSets?: number;
  TotalVolume?: number;
  totalVolume?: number;
  NewPRs?: string[];
  newPRs?: string[];
  Summary?: WorkoutSummaryData;
  summary?: WorkoutSummaryData;
  Exercises?: ExerciseData[];
  exercises?: ExerciseData[];
}

interface WorkoutHistoryItem {
  Id?: string;
  id?: string;
  Date?: string;
  date?: string;
  Type?: string;
  type?: string;
  DurationMinutes?: number;
  durationMinutes?: number;
  PerceivedEffort?: number;
  perceivedEffort?: number;
  EnergyLevel?: number;
  energyLevel?: number;
  Notes?: string;
  notes?: string;
  Exercises?: ExerciseData[];
  exercises?: ExerciseData[];
}

interface GetWorkoutHistoryResponse {
  Period?: string;
  period?: string;
  TotalWorkouts?: number;
  totalWorkouts?: number;
  Workouts?: WorkoutHistoryItem[];
  workouts?: WorkoutHistoryItem[];
}

// Create app instance
const app = new App({ name: "Workout Summary", version: "1.0.0" });

// ============= STATE =============

// eslint-disable-next-line @typescript-eslint/no-unused-vars
let _summaryData: CompleteWorkoutResponse | null = null;

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

function formatDate(dateStr: string | undefined): string {
  if (!dateStr) return "Today";
  const date = new Date(dateStr);
  return date.toLocaleDateString("en-US", {
    weekday: "long",
    year: "numeric",
    month: "long",
    day: "numeric"
  });
}

function formatDuration(minutes: number): string {
  if (minutes < 60) return `${minutes} min`;
  const hours = Math.floor(minutes / 60);
  const mins = minutes % 60;
  return mins > 0 ? `${hours}h ${mins}m` : `${hours}h`;
}

function formatVolume(volume: number): string {
  if (volume >= 1000) {
    return `${(volume / 1000).toFixed(1)}k lbs`;
  }
  return `${volume.toLocaleString()} lbs`;
}

// ============= RENDER FUNCTIONS =============

function renderSummary(data: CompleteWorkoutResponse): void {
  const container = el("summary-container");
  if (!container) return;

  const date = get<string>(data, "Date", "date");
  const duration = get<number>(data, "DurationMinutes", "durationMinutes") || 0;
  const totalExercises = get<number>(data, "TotalExercises", "totalExercises") || 0;
  const totalSets = get<number>(data, "TotalSets", "totalSets") || 0;
  const totalVolume = get<number>(data, "TotalVolume", "totalVolume") || 0;
  const newPRs = get<string[]>(data, "NewPRs", "newPRs") || [];
  const summary = get<WorkoutSummaryData>(data, "Summary", "summary");
  const muscleGroups = summary ? (get<string>(summary, "MuscleGroups", "muscleGroups") || "") : "";

  // Update header
  const dateEl = el("workout-date");
  if (dateEl) dateEl.textContent = formatDate(date);

  container.innerHTML = `
    <!-- Main Stats Grid -->
    <div class="stats-grid">
      <div class="stat-card">
        <div class="stat-icon">⏱️</div>
        <div class="stat-value">${formatDuration(duration)}</div>
        <div class="stat-label">Duration</div>
      </div>
      <div class="stat-card">
        <div class="stat-icon">🏋️</div>
        <div class="stat-value">${totalExercises}</div>
        <div class="stat-label">Exercises</div>
      </div>
      <div class="stat-card">
        <div class="stat-icon">📊</div>
        <div class="stat-value">${totalSets}</div>
        <div class="stat-label">Total Sets</div>
      </div>
      <div class="stat-card highlight">
        <div class="stat-icon">💪</div>
        <div class="stat-value">${formatVolume(totalVolume)}</div>
        <div class="stat-label">Volume Lifted</div>
      </div>
    </div>

    ${muscleGroups ? `
      <div class="muscle-groups">
        <h3>💪 Muscle Groups Trained</h3>
        <div class="muscle-tags">
          ${muscleGroups.split(", ").map(mg => `<span class="muscle-tag">${mg}</span>`).join("")}
        </div>
      </div>
    ` : ""}

    ${newPRs.length > 0 ? `
      <div class="prs-section">
        <h3>🏆 New Personal Records!</h3>
        <div class="pr-list">
          ${newPRs.map(pr => `<div class="pr-item">🎯 ${pr}</div>`).join("")}
        </div>
      </div>
    ` : ""}

    <div class="motivational-message">
      <p>${getMotivationalMessage(totalSets, totalVolume)}</p>
    </div>
  `;

  hide("loading");
  hide("no-workout");
  show("summary-view");
}

function getMotivationalMessage(sets: number, volume: number): string {
  const messages = [
    "Great work! You crushed it today! 💪",
    "Another workout in the books! Keep pushing! 🔥",
    "You're building something great. Stay consistent! 🎯",
    "Hard work pays off. You're getting stronger! 📈",
    "Champions are made in the gym. You showed up! 🏆"
  ];
  
  if (volume > 10000) {
    return "Incredible volume today! You're a machine! 🚀";
  }
  if (sets > 20) {
    return "That's a lot of sets! Your dedication is inspiring! ⭐";
  }
  
  return messages[Math.floor(Math.random() * messages.length)];
}

function showNoWorkout(): void {
  hide("loading");
  hide("summary-view");
  show("no-workout");
}

// ============= DATA LOADING =============

async function loadLatestWorkout(): Promise<void> {
  show("loading");
  hide("summary-view");
  hide("no-workout");

  try {
    app.sendLog({ level: "info", data: "Loading workout history..." });

    const result = await app.callServerTool({
      name: "GetWorkoutHistory",
      arguments: { Days: 1 }
    });

    const content = result.content as Array<{ type: string; text?: string; [key: string]: any }>;
    const data = parseToolResult<GetWorkoutHistoryResponse>(content);

    const workouts = data?.Workouts || data?.workouts || [];
    
    if (workouts.length > 0) {
      // Convert the latest workout to summary format
      const latest = workouts[0];
      const exercises = latest.Exercises || latest.exercises || [];
      
      const totalVolume = exercises.reduce((sum, ex) => {
        const sets = ex.Sets || ex.sets || [];
        return sum + sets.reduce((setSum, s) => {
          const reps = s.Reps || s.reps || 0;
          const weight = s.Weight || s.weight || 0;
          return setSum + (reps * weight);
        }, 0);
      }, 0);

      const totalSets = exercises.reduce((sum, ex) => {
        const sets = ex.Sets || ex.sets || [];
        return sum + sets.length;
      }, 0);

      const muscleGroups = [...new Set(exercises.map(ex => ex.MuscleGroup || ex.muscleGroup || ""))].filter(Boolean);

      const summaryResponse: CompleteWorkoutResponse = {
        Success: true,
        Date: latest.Date || latest.date,
        DurationMinutes: latest.DurationMinutes || latest.durationMinutes || 0,
        TotalExercises: exercises.length,
        TotalSets: totalSets,
        TotalVolume: totalVolume,
        NewPRs: [],
        Summary: {
          ExerciseCount: exercises.length,
          TotalSets: totalSets,
          TotalVolume: totalVolume,
          MuscleGroups: muscleGroups.join(", ")
        }
      };

      _summaryData = summaryResponse;
      renderSummary(summaryResponse);
    } else {
      showNoWorkout();
    }
  } catch (error) {
    app.sendLog({ level: "error", data: `Error loading workout: ${error}` });
    showNoWorkout();
  }
}

// ============= EVENT HANDLERS =============

function handleStartNewWorkout(): void {
  app.updateModelContext({
    content: [{
      type: "text",
      text: "[Widget Action] User wants to start a new workout from the summary screen. Please show them the workout templates."
    }]
  }).catch(err => app.sendLog({ level: "error", data: `Failed to update context: ${err}` }));
}

// ============= INITIALIZATION =============

// Register handlers
app.ontoolinput = (params: any) => {
  app.sendLog({ level: "info", data: `Tool input: ${JSON.stringify(params.arguments)}` });
  
  const toolName = (params.name || "").toLowerCase();
  
  // If CompleteWorkout was just called, we'll get the result in ontoolresult
  if (toolName === "completeworkout") {
    show("loading");
  }
};

app.ontoolresult = (params) => {
  app.sendLog({ level: "info", data: `Tool result received` });
  
  // Try to parse as CompleteWorkoutResponse
  const content = params.content as Array<{ type: string; text?: string; [key: string]: any }>;
  const data = parseToolResult<CompleteWorkoutResponse>(content);
  
  if (data && (data.Success || data.success)) {
    _summaryData = data;
    renderSummary(data);
  }
};

app.onhostcontextchanged = (ctx) => {
  if (ctx.theme) applyTheme(ctx.theme);
};

// Set up buttons
el("newWorkoutBtn")?.addEventListener("click", handleStartNewWorkout);
el("startWorkoutBtn")?.addEventListener("click", handleStartNewWorkout);

// Connect and initialize
await app.connect();
applyTheme(app.getHostContext()?.theme);

// Load the latest workout on startup
loadLatestWorkout();
