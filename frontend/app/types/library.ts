import type { TitleSummary } from "./catalog";

export type LibraryTitle = TitleSummary & {
  runtimeMinutes?: number;
};

export type HistoryItem = {
  title: LibraryTitle;
  episodeNumber: number | null;
  progressSeconds: number;
  updatedAt: string;
};

export type Library = {
  saved: LibraryTitle[];
  history: HistoryItem[];
};

export type LibraryTab = "saved" | "history";
