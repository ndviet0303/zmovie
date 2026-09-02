import type { TitleSummary } from "./catalog";

export type AssistantReply = {
  message: string;
  suggestions: TitleSummary[];
  recommendationId?: string | null;
};

export type AssistantMessage = {
  role: "bot" | "user";
  text: string;
  suggestions?: TitleSummary[];
  recommendationId?: string | null;
};

export type AssistantFeedbackPayload = {
  recommendationId: string;
  slug: string;
  eventType: "click" | string;
};
