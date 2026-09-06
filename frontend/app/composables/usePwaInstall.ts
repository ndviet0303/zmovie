import { onMounted, ref } from "vue";

interface BeforeInstallPromptEvent extends Event {
  prompt: () => Promise<void>;
  userChoice: Promise<{ outcome: "accepted" | "dismissed" }>;
}

export function usePwaInstall() {
  const isInstallable = ref(false);
  const isInstalled = ref(false);
  const deferredPrompt = ref<BeforeInstallPromptEvent | null>(null);

  onMounted(() => {
    if (!import.meta.client) return;
    const isIosStandalone =
      "standalone" in window.navigator &&
      Boolean(Reflect.get(window.navigator, "standalone"));
    if (
      window.matchMedia("(display-mode: standalone)").matches ||
      isIosStandalone
    ) {
      isInstalled.value = true;
    }

    if ("serviceWorker" in navigator) {
      navigator.serviceWorker.register("/sw.js").catch(() => undefined);
    }

    window.addEventListener("beforeinstallprompt", (e) => {
      e.preventDefault();
      deferredPrompt.value = e as BeforeInstallPromptEvent;
      isInstallable.value = true;
    });

    window.addEventListener("appinstalled", () => {
      isInstalled.value = true;
      isInstallable.value = false;
      deferredPrompt.value = null;
    });
  });

  async function promptInstall() {
    if (!deferredPrompt.value) return false;
    await deferredPrompt.value.prompt();
    const choice = await deferredPrompt.value.userChoice;
    deferredPrompt.value = null;
    isInstallable.value = false;
    return choice.outcome === "accepted";
  }

  return {
    isInstallable,
    isInstalled,
    promptInstall,
  };
}
