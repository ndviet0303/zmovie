import { onBeforeUnmount, onMounted, ref, type Ref } from "vue";

export interface PlayerHotkeyActions {
  togglePlay: () => void;
  toggleFullscreen: () => void;
  toggleMute: () => void;
  seekDelta: (deltaSeconds: number) => void;
  adjustVolume: (deltaVolume: number) => void;
  toggleCaptions?: () => void;
  enabled?: Ref<boolean>;
}

export interface PlayerHudNotice {
  text: string;
  type:
    | "play"
    | "pause"
    | "seek-forward"
    | "seek-backward"
    | "volume"
    | "mute"
    | "captions";
}

export function usePlayerHotkeys(actions: PlayerHotkeyActions) {
  const hudNotice = ref<PlayerHudNotice | null>(null);
  let noticeTimeout: ReturnType<typeof setTimeout> | null = null;

  function showHud(text: string, type: PlayerHudNotice["type"]) {
    hudNotice.value = { text, type };
    if (noticeTimeout) clearTimeout(noticeTimeout);
    noticeTimeout = setTimeout(() => {
      hudNotice.value = null;
    }, 900);
  }

  function isInputFocused(): boolean {
    if (!import.meta.client) return false;
    const active = document.activeElement;
    if (!active) return false;
    const tag = active.tagName.toUpperCase();
    if (tag === "INPUT" || tag === "TEXTAREA" || tag === "SELECT") return true;
    if (active.getAttribute("contenteditable") === "true") return true;
    if (active.closest("[role='dialog']") || active.closest("dialog"))
      return true;
    return false;
  }

  function handleKeyDown(event: KeyboardEvent) {
    if (actions.enabled && !actions.enabled.value) return;
    if (isInputFocused()) return;

    switch (event.code) {
      case "Space":
      case "KeyK":
        event.preventDefault();
        actions.togglePlay();
        break;

      case "KeyF":
        event.preventDefault();
        actions.toggleFullscreen();
        break;

      case "KeyM":
        event.preventDefault();
        actions.toggleMute();
        showHud("Mute", "mute");
        break;

      case "ArrowLeft":
      case "KeyJ":
        event.preventDefault();
        actions.seekDelta(-10);
        showHud("-10s", "seek-backward");
        break;

      case "ArrowRight":
      case "KeyL":
        event.preventDefault();
        actions.seekDelta(10);
        showHud("+10s", "seek-forward");
        break;

      case "ArrowUp":
        event.preventDefault();
        actions.adjustVolume(0.05);
        showHud("Volume +", "volume");
        break;

      case "ArrowDown":
        event.preventDefault();
        actions.adjustVolume(-0.05);
        showHud("Volume -", "volume");
        break;

      case "KeyC":
        if (actions.toggleCaptions) {
          event.preventDefault();
          actions.toggleCaptions();
          showHud("Subtitles", "captions");
        }
        break;
    }
  }

  onMounted(() => {
    if (import.meta.client) {
      window.addEventListener("keydown", handleKeyDown);
    }
  });

  onBeforeUnmount(() => {
    if (import.meta.client) {
      window.removeEventListener("keydown", handleKeyDown);
      if (noticeTimeout) clearTimeout(noticeTimeout);
    }
  });

  return {
    hudNotice,
    showHud,
  };
}
