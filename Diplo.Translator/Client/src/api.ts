import { umbHttpClient } from "@umbraco-cms/backoffice/http-client";
import { TRANSLATOR_API_BASE } from "./constants";

const BEARER_SECURITY = [{ scheme: "bearer", type: "http" }] as const;

export interface TranslatorLanguage {
  id: number;
  isoCode: string;
  cultureName: string;
  displayName: string;
  isDefault: boolean;
}

export interface TranslatorSettings {
  hasConfiguredChatProfileAlias: boolean;
  chatProfileAlias?: string | null;
}

export interface TranslationJobProgress {
  dictionaryCount: number;
  processedItemCount: number;
  attemptedTranslationCount: number;
  translationCount: number;
  errorCount: number;
  skippedCount: number;
  currentItem?: string | null;
  message: string;
}

export interface TranslationJob {
  id: string;
  fromCulture: string;
  overwrite: boolean;
  status: "Pending" | "Running" | "Completed" | "CompletedWithErrors" | "Failed" | "Cancelled";
  statusMessage: string;
  createdUtc: string;
  completedUtc?: string | null;
  progress: TranslationJobProgress;
  messages: string[];
  errors: string[];
}

export async function getLanguages(): Promise<TranslatorLanguage[]> {
  return translatorGet<TranslatorLanguage[]>("languages");
}

export async function getSettings(): Promise<TranslatorSettings> {
  return translatorGet<TranslatorSettings>("settings");
}

export async function startJob(fromCulture: string, overwrite: boolean): Promise<string> {
  const result = await translatorPost<{ jobId: string }>("jobs", { fromCulture, overwrite });
  return result.jobId;
}

export async function getJob(jobId: string): Promise<TranslationJob> {
  return translatorGet<TranslationJob>(`jobs/${jobId}`);
}

export async function cancelJob(jobId: string): Promise<void> {
  const { error } = await umbHttpClient.delete({
    url: `${TRANSLATOR_API_BASE}/jobs/${jobId}`,
    security: BEARER_SECURITY
  });

  if (error) {
    throw error;
  }
}

async function translatorGet<T>(path: string): Promise<T> {
  const { data, error } = await umbHttpClient.get<T>({
    url: `${TRANSLATOR_API_BASE}/${path}`,
    security: BEARER_SECURITY
  });

  if (error) {
    throw error;
  }

  return data as T;
}

async function translatorPost<T>(path: string, body?: unknown): Promise<T> {
  const { data, error } = await umbHttpClient.post<T>({
    url: `${TRANSLATOR_API_BASE}/${path}`,
    body,
    security: BEARER_SECURITY
  });

  if (error) {
    throw error;
  }

  return data as T;
}
