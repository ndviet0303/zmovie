import { describe, expect, it } from "bun:test";
import {
  sendAssistantChat,
  sendAssistantFeedback,
} from "../app/services/assistant.service";
import { createMockApi } from "./test-utils";

describe("assistant.service", () => {
  it("sendAssistantChat posts message and locale", async () => {
    const { api, lastCall } = createMockApi(() =>
      Promise.resolve({
        message: "Here are some movies",
        suggestions: [],
        recommendationId: "rec-1",
      }),
    );

    const res = await sendAssistantChat("romantic comedy", "vi", api);
    expect(lastCall().url).toBe("/v1/assistant/chat");
    expect(lastCall().options?.method).toBe("POST");
    expect(lastCall().options?.body).toEqual({
      message: "romantic comedy",
      locale: "vi",
    });
    expect(res.recommendationId).toBe("rec-1");
  });

  it("sendAssistantFeedback posts recommendation feedback", async () => {
    const { api, lastCall } = createMockApi(() => Promise.resolve());

    await sendAssistantFeedback(
      { recommendationId: "rec-1", slug: "my-movie", eventType: "click" },
      api,
    );
    expect(lastCall().url).toBe("/v1/assistant/feedback");
    expect(lastCall().options?.method).toBe("POST");
    expect(lastCall().options?.body).toEqual({
      recommendationId: "rec-1",
      slug: "my-movie",
      eventType: "click",
    });
  });
});
