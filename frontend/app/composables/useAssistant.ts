import { computed, ref } from "vue";
import {
  sendAssistantChat,
  sendAssistantFeedback,
} from "~/services/assistant.service";
import type { AssistantMessage } from "~/types/assistant";

function statusOf(error: unknown): number | undefined {
  if (!error || typeof error !== "object") return undefined;
  const candidate = error as {
    status?: unknown;
    statusCode?: unknown;
    response?: { status?: unknown };
  };
  const status =
    candidate.response?.status ?? candidate.status ?? candidate.statusCode;
  return typeof status === "number" ? status : undefined;
}

export function useAssistant() {
  const {
    locale,
    messages: i18nMessages,
    setLocale: setGlobalLocale,
  } = useLocale();
  const copy = computed(() => i18nMessages.value.assistant);

  const prompt = ref("");
  const isSending = ref(false);
  const messages = ref<AssistantMessage[]>([
    {
      role: "bot",
      text: i18nMessages.value.assistant.welcomeMessage,
    },
  ]);

  function errorText(status: number | undefined): string {
    if (status === 524 || status === 504) {
      return locale.value === "vi"
        ? `Backend đang timeout (HTTP ${status}). Vui lòng kiểm tra API/deployment.`
        : `The backend timed out (HTTP ${status}). Check the API deployment.`;
    }
    if (status && status >= 500) {
      return locale.value === "vi"
        ? `Backend đang lỗi (HTTP ${status}). Bạn thử lại sau nhé.`
        : `The backend returned an error (HTTP ${status}). Please try again.`;
    }
    return locale.value === "vi"
      ? "Mình đang gặp sự cố. Bạn thử lại sau nhé."
      : "I am having trouble right now. Please try again.";
  }

  async function send(nextPrompt = prompt.value) {
    const message = nextPrompt.trim();
    if (!message || isSending.value) return;
    messages.value.push({ role: "user", text: message });
    prompt.value = "";
    isSending.value = true;
    try {
      const assistantReply = await sendAssistantChat(message, locale.value);
      messages.value.push({
        role: "bot",
        text: assistantReply.message,
        suggestions: assistantReply.suggestions,
        recommendationId: assistantReply.recommendationId,
      });
    } catch (error) {
      const status = statusOf(error);
      messages.value.push({
        role: "bot",
        text:
          status === 401
            ? locale.value === "vi"
              ? "Bạn cần đăng nhập để dùng tìm phim theo lịch sử và sở thích cá nhân."
              : "Please sign in to use recommendations based on your history and preferences."
            : errorText(status),
      });
    } finally {
      isSending.value = false;
    }
  }

  async function recordFeedback(message: AssistantMessage, slug: string) {
    if (!message.recommendationId) return;
    try {
      await sendAssistantFeedback({
        recommendationId: message.recommendationId,
        slug,
        eventType: "click",
      });
    } catch (error) {
      console.debug("[assistant:feedback]", error);
    }
  }

  async function setLocale(nextLocale: "vi" | "en") {
    if (nextLocale === locale.value) return;
    setGlobalLocale(nextLocale);
  }

  return {
    locale,
    copy,
    prompt,
    isSending,
    messages,
    send,
    recordFeedback,
    setLocale,
  };
}
