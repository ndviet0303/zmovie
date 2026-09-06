import type { components } from "./api";

export type TitleSummary = components["schemas"]["TitleSummary"] & {
  isR2Hosted?: boolean;
  country?: string;
};

export type TitleDetail = components["schemas"]["TitleDetail"] & {
  viewCount?: number;
  actors?: string;
  directors?: string;
  country?: string;
  trailerUrl?: string;
  isR2Hosted?: boolean;
};

export type TitleListResponse = {
  items: TitleSummary[];
  total: number | string;
};

export type ScheduleEntry = {
  slug: string;
  title: string;
  posterUrl: string;
  date: string;
  episodeNumber?: number | null;
};

export type ScheduleResponse = {
  weekStart: string;
  items: ScheduleEntry[];
};

export type PersonSummary = {
  slug: string;
  name: string;
  roles: string[];
  titleCount: number;
};

export type PeopleResponse = {
  items: PersonSummary[];
  total: number;
};

export type PersonDetail = {
  slug: string;
  name: string;
  roles: string[];
  titles: TitleSummary[];
};

export type PlaybackSource = {
  provider: string;
  url: string;
  format: "hls" | "video" | "embed";
  priority: number;
  subtitleUrl?: string;
  audioTrack?: string;
};

export type PlaybackMilestones = {
  introStart?: number;
  introEnd?: number;
  outroStart?: number;
  outroEnd?: number;
};

export type PlaybackEpisode = components["schemas"]["PlaybackEpisode"] & {
  subtitleUrl?: string;
  sources?: PlaybackSource[];
  milestones?: PlaybackMilestones;
};

export type PlaybackResponse = {
  slug: string;
  title: string;
  isSeries: boolean;
  episodes: PlaybackEpisode[];
};

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
