export type ViewRecordedResponse = {
  viewCount: number;
  counted: boolean;
};

export type LocalWatchProgress = {
  episodeNumber: number | null;
  progressSeconds: number;
  updatedAt: number;
};

export type QualityOption = {
  level: number;
  label: string;
};

export type SubtitleOption = {
  index: number;
  label: string;
};

export type AudioTrackOption = {
  id: string;
  label: string;
};
