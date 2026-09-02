import type { components } from "./api";

export type TitleSummary = components["schemas"]["TitleSummary"];
export type TitleDetail = components["schemas"]["TitleDetail"];
export type TitleListResponse = components["schemas"]["TitleListResponse"];
export type PlaybackResponse = components["schemas"]["PlaybackResponse"];
export type PlaybackEpisode = components["schemas"]["PlaybackEpisode"];

/**
 * Convenience alias for TitleSummary across public catalog/discovery views
 */
export type Title = TitleSummary;

export type SortOrder = "latest" | "oldest" | "title";

export type TitleFilter = {
  genre: string;
  type: "all" | "movie" | "series";
  sort: SortOrder;
  query?: string;
};
