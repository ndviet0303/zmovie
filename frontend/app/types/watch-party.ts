export type WatchPartyRoom = {
  roomId: string;
  movieSlug: string;
  episodeNumber: number;
  activeUserCount: number;
  isPlaying: boolean;
  currentTime: number;
  createdAt: string;
  lastUpdated: string;
};

export type CreatedWatchPartyRoom = {
  room: WatchPartyRoom;
  managementToken: string;
};

export type RoomJoinedEvent = {
  roomId: string;
  movieSlug: string;
  episodeNumber: number;
  isPlaying: boolean;
  currentTime: number;
  activeUsers: string[];
};

export type PartyDanmakuEvent = {
  episodeId: string;
  timeSeconds: number;
  content: string;
  color: string;
  timestamp: string;
};
