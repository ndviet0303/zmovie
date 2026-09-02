import type { HubConnection } from "@microsoft/signalr";
import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import { onBeforeUnmount, ref } from "vue";
import type { DanmakuItem } from "~/types/danmaku";
import {
  fetchTimedDanmaku,
  sendDanmakuComment,
} from "~/services/danmaku.service";

export function useDanmaku(
  slugRef: Ref<string>,
  episodeNumberRef: Ref<number>,
) {
  const danmakus = ref<DanmakuItem[]>([]);
  const isEnabled = ref(true);
  const opacity = ref(0.85);
  const fontSize = ref(18);
  const speedMultiplier = ref(1.0);
  const isConnected = ref(false);

  let hubConnection: HubConnection | null = null;

  async function loadComments() {
    if (!slugRef.value || !episodeNumberRef.value) return;
    try {
      const items = await fetchTimedDanmaku(
        slugRef.value,
        episodeNumberRef.value,
      );
      danmakus.value = items;
    } catch {
      // fallback to existing or empty
    }
  }

  async function connectHub() {
    if (!import.meta.client) return;
    if (hubConnection) await disconnectHub();

    const config = useRuntimeConfig();
    const hubUrl = `${config.public.apiBase || ""}/hubs/danmaku`;

    try {
      const connection = new HubConnectionBuilder()
        .withUrl(hubUrl)
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Warning)
        .build();

      connection.on("DanmakuReceived", (dto: DanmakuItem) => {
        if (
          dto.titleSlug === slugRef.value &&
          dto.episodeNumber === episodeNumberRef.value
        ) {
          danmakus.value.push(dto);
        }
      });

      await connection.start();
      hubConnection = connection;
      isConnected.value = true;

      await connection.invoke(
        "JoinPlayback",
        slugRef.value,
        episodeNumberRef.value,
      );
    } catch {
      isConnected.value = false;
    }
  }

  async function disconnectHub() {
    if (hubConnection) {
      try {
        await hubConnection.invoke(
          "LeavePlayback",
          slugRef.value,
          episodeNumberRef.value,
        );
        await hubConnection.stop();
      } catch {
        // ignore
      }
      hubConnection = null;
      isConnected.value = false;
    }
  }

  async function submitDanmaku(
    timeSeconds: number,
    content: string,
    color = "#ffffff",
    authorName = "Viewer",
  ) {
    const text = content.trim();
    if (!text) return;

    // Send via SignalR if connected
    if (hubConnection && isConnected.value) {
      try {
        await hubConnection.invoke(
          "SendDanmaku",
          slugRef.value,
          episodeNumberRef.value,
          timeSeconds,
          text,
          color,
          authorName,
        );
      } catch {
        // fallback to REST
        await sendDanmakuComment(slugRef.value, episodeNumberRef.value, {
          timeSeconds,
          content: text,
          color,
          authorName,
        });
      }
    } else {
      await sendDanmakuComment(slugRef.value, episodeNumberRef.value, {
        timeSeconds,
        content: text,
        color,
        authorName,
      });
    }
  }

  onBeforeUnmount(() => {
    disconnectHub();
  });

  return {
    danmakus,
    isEnabled,
    opacity,
    fontSize,
    speedMultiplier,
    isConnected,
    loadComments,
    connectHub,
    disconnectHub,
    submitDanmaku,
  };
}
