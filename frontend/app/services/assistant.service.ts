import type { ApiFetch } from "~/types/api-fetch";
import type {
  AssistantFeedbackPayload,
  AssistantReply,
} from "~/types/assistant";

function resolveApi(api?: ApiFetch): ApiFetch {
  return api ?? (useNuxtApp().$api as ApiFetch);
}

export function sendAssistantChat(
  message: string,
  locale?: string,
  api?: ApiFetch,
): Promise<AssistantReply> {
  return resolveApi(api)<AssistantReply>("/v1/assistant/chat", {
    method: "POST",
    credentials: "include",
    body: {
      message,
      locale,
    },
  });
}

export function sendAssistantFeedback(
  payload: AssistantFeedbackPayload,
  api?: ApiFetch,
): Promise<void> {
  return resolveApi(api)("/v1/assistant/feedback", {
    method: "POST",
    credentials: "include",
    body: payload,
  }).then(() => undefined);
}
