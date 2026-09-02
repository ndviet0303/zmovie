import type { components } from "./api";
import type { TitleSummary } from "./catalog";

export type HomeResponse = components["schemas"]["HomeResponse"];

export type TopPeriod = "day" | "week" | "month";

export type TopTitle = {
  title: TitleSummary;
  views: number;
};

export type ContinueWatching = {
  title: TitleSummary;
  episodeNumber: number | null;
  progressSeconds: number;
  updatedAt: string;
};

export type PersonalizedDiscovery = {
  continueWatching: ContinueWatching[];
  recommended: TitleSummary[];
};
