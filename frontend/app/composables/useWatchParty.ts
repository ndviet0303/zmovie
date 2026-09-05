import {
  type HubConnection,
  HubConnectionBuilder,
  LogLevel,
} from "@microsoft/signalr";
import { onBeforeUnmount, ref } from "vue";
import type { DanmakuItem } from "~/components/DanmakuCanvas.vue";

export interface PartyChatMessage {
  username: string;
  message: string;
  timestamp: string;
}

export interface RoomState {
  roomId: string;
  movieSlug: string;
  episodeNumber: number;
  isPlaying: boolean;
  currentTime: number;
}

export function useWatchParty() {
  const isConnected = ref(false);
  const roomId = ref("");
  const username = ref("");
  const activeUsers = ref<string[]>([]);
  const chatMessages = ref<PartyChatMessage[]>([]);
  const roomState = ref<RoomState | null>(null);

  let connection: HubConnection | null = null;
  let onSyncPlaybackCallback:
    ((action: "play" | "pause" | "seek", time: number) => void) | null = null;
  let onDanmakuCallback: ((item: DanmakuItem) => void) | null = null;

  function onSyncPlayback(
    cb: (action: "play" | "pause" | "seek", time: number) => void,
  ) {
    onSyncPlaybackCallback = cb;
  }

  function onDanmaku(cb: (item: DanmakuItem) => void) {
    onDanmakuCallback = cb;
  }

  async function connect(
    targetRoomId: string,
    clientUsername: string,
    movieSlug: string,
    episodeNumber = 1,
  ) {
    roomId.value = targetRoomId;
    username.value = clientUsername;

    const config = useRuntimeConfig();
    const hubUrl = `${config.public.apiBaseUrl || ""}/hubs/watch-party`;

    connection = new HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on("RoomJoined", (data: any) => {
      isConnected.value = true;
      roomState.value = {
        roomId: data.roomId,
        movieSlug: data.movieSlug,
        episodeNumber: data.episodeNumber,
        isPlaying: data.isPlaying,
        currentTime: data.currentTime,
      };
      activeUsers.value = data.activeUsers || [];
    });

    connection.on("UserJoined", (data: { username: string }) => {
      if (!activeUsers.value.includes(data.username)) {
        activeUsers.value.push(data.username);
      }
    });

    connection.on("UserLeft", (data: { username: string }) => {
      activeUsers.value = activeUsers.value.filter((u) => u !== data.username);
    });

    connection.on(
      "PlaybackSynced",
      (data: { action: "play" | "pause" | "seek"; currentTime: number }) => {
        if (roomState.value) {
          roomState.value.isPlaying = data.action === "play";
          roomState.value.currentTime = data.currentTime;
        }
        if (onSyncPlaybackCallback) {
          onSyncPlaybackCallback(data.action, data.currentTime);
        }
      },
    );

    connection.on("ChatMessageReceived", (data: PartyChatMessage) => {
      chatMessages.value.push(data);
    });

    connection.on("DanmakuReceived", (data: any) => {
      if (onDanmakuCallback) {
        onDanmakuCallback({
          id: `${data.timeSeconds}-${data.content}-${Date.now()}`,
          timeSeconds: data.timeSeconds,
          content: data.content,
          color: data.color,
        });
      }
    });

    try {
      await connection.start();
      await connection.invoke(
        "JoinRoom",
        targetRoomId,
        clientUsername,
        movieSlug,
        episodeNumber,
      );
      isConnected.value = true;
    } catch (err) {
      console.warn("[WatchParty] Connection failed:", err);
      isConnected.value = false;
    }
  }

  async function syncPlay(currentTime: number) {
    if (!connection || !isConnected.value) return;
    try {
      await connection.invoke("SyncPlay", roomId.value, currentTime);
    } catch (err) {
      console.debug("[WatchParty:syncPlay]", err);
    }
  }

  async function syncPause(currentTime: number) {
    if (!connection || !isConnected.value) return;
    try {
      await connection.invoke("SyncPause", roomId.value, currentTime);
    } catch (err) {
      console.debug("[WatchParty:syncPause]", err);
    }
  }

  async function syncSeek(currentTime: number) {
    if (!connection || !isConnected.value) return;
    try {
      await connection.invoke("SyncSeek", roomId.value, currentTime);
    } catch (err) {
      console.debug("[WatchParty:syncSeek]", err);
    }
  }

  async function sendChat(message: string) {
    if (!connection || !isConnected.value || !message.trim()) return;
    try {
      await connection.invoke(
        "SendPartyChat",
        roomId.value,
        username.value,
        message.trim(),
      );
    } catch (err) {
      console.debug("[WatchParty:sendChat]", err);
    }
  }

  async function sendDanmaku(
    episodeId: string,
    timeSeconds: number,
    content: string,
    color = "#ffffff",
  ) {
    if (!connection || !isConnected.value || !content.trim()) return;
    try {
      await connection.invoke(
        "SendDanmaku",
        roomId.value,
        episodeId,
        timeSeconds,
        content.trim(),
        color,
      );
    } catch (err) {
      console.debug("[WatchParty:sendDanmaku]", err);
    }
  }

  async function disconnect() {
    if (connection) {
      try {
        await connection.invoke("LeaveRoom", roomId.value);
        await connection.stop();
      } catch {
        // ignore
      }
      connection = null;
    }
    isConnected.value = false;
  }

  onBeforeUnmount(() => {
    void disconnect();
  });

  return {
    isConnected,
    roomId,
    username,
    activeUsers,
    chatMessages,
    roomState,
    connect,
    disconnect,
    syncPlay,
    syncPause,
    syncSeek,
    sendChat,
    sendDanmaku,
    onSyncPlayback,
    onDanmaku,
  };
}
