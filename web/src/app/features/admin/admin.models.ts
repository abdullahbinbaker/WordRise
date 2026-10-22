export type DraftStatus = 'queued' | 'inProgress' | 'aiApproved' | 'needsAttention' | 'failed' | 'published';

export const STATUS_LABELS: Record<DraftStatus, string> = {
  queued: 'في الانتظار',
  inProgress: 'قيد الكتابة',
  aiApproved: 'جاهز لمراجعتك',
  needsAttention: 'يحتاج انتباهك',
  failed: 'تعطّل',
  published: 'منشور',
};

export const CRITERION_LABELS: Record<string, string> = {
  languageAccuracy: 'سلامة اللغة والمعنى',
  levelMatch: 'مناسبة المستوى',
  vocabularyAndGrammarAdherence: 'الالتزام بالقاعدة والكلمات',
  ageAppropriateness: 'مناسبة العمر',
  culturalFit: 'المناسبة الثقافية',
  realism: 'واقعية الموقف',
  exerciseQuality: 'جودة التمارين',
};

export interface DraftListItem {
  id: string;
  scenario: string;
  grammarStep: number;
  grammarPoint: string;
  status: DraftStatus;
  revisionCount: number;
  publishedLessonNumber: number | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface GrammarPoint {
  number: number;
  title: string;
  level: string;
}

export interface Word {
  target: string;
  native: string;
  pronunciation: string;
  example: string;
  exampleNative: string;
}

export interface DialogueLine {
  speaker: string;
  isLearner: boolean;
  target: string;
  native: string;
}

export interface AnswerOption {
  text: string;
  isCorrect: boolean;
}

export interface DraftContent {
  title: string;
  words: Word[];
  dialogue: DialogueLine[];
  chatPrompt: string;
  chatOptions: AnswerOption[];
  grammar: {
    title: string;
    meaning: string;
    explanation: string;
    pattern: string;
    examples: { target: string; native: string }[];
    note: string;
  };
  listeningSentence: string;
  listeningOptions: AnswerOption[];
}

export interface Revision {
  number: number;
  passed: boolean;
  source: 'structural' | 'ai';
  summary: string;
  criteria: { criterion: string; passed: boolean; comment: string }[];
  issues: { severity: 'suggestion' | 'blocking'; where: string; problem: string; suggestion: string }[];
  createdAtUtc: string;
}

export interface DraftDetail {
  id: string;
  brief: { targetLanguage: string; nativeLanguage: string; scenario: string; grammarStep: number; grammarPoint: string; level: string; wordCount: number };
  status: DraftStatus;
  error: string | null;
  cyclesUsed: number;
  content: DraftContent | null;
  revisions: Revision[];
  feedback: { by: string; text: string; atUtc: string }[];
  publishedLessonId: string | null;
  publishedLessonNumber: number | null;
}

export interface StaffMember {
  id: string;
  email: string;
  displayName: string | null;
  roles: string[];
  twoFactorEnabled: boolean;
}

export interface TwoFactorState {
  sharedKey: string;
  recoveryCodesLeft: number;
  recoveryCodes: string[] | null;
  isTwoFactorEnabled: boolean;
}
