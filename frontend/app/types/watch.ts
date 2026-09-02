export type ViewRecordedResponse = {
  viewCount: number;
  counted: boolean;
};

export type LocalWatchProgress = {
  episodeNumber: number | null;
  progressSeconds: number;
  updatedAt: number;
};
