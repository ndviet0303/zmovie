import { describe, expect, it } from "bun:test";
import {
  fetchAuthMe,
  googleAuth,
  logoutAuth,
} from "../app/services/auth.service";
import { createMockApi } from "./test-utils";

describe("auth.service", () => {
  it("fetchAuthMe queries /v1/auth/me with credentials", async () => {
    const { api, lastCall } = createMockApi(() =>
      Promise.resolve({
        id: "user-1",
        email: "user@example.com",
        role: "user",
      }),
    );

    const user = await fetchAuthMe(api);
    expect(lastCall().url).toBe("/v1/auth/me");
    expect(lastCall().options?.credentials).toBe("include");
    expect(user.id).toBe("user-1");
  });

  it("logoutAuth posts to /v1/auth/logout with credentials", async () => {
    const { api, lastCall } = createMockApi(() => Promise.resolve());

    await logoutAuth(api);
    expect(lastCall().url).toBe("/v1/auth/logout");
    expect(lastCall().options?.method).toBe("POST");
    expect(lastCall().options?.credentials).toBe("include");
  });

  it("googleAuth sends credential payload to /v1/auth/google", async () => {
    const { api, lastCall } = createMockApi(() =>
      Promise.resolve({
        id: "user-google",
        email: "google@example.com",
        role: "user",
      }),
    );

    const user = await googleAuth("dummy-id-token", api);
    expect(lastCall().url).toBe("/v1/auth/google");
    expect(lastCall().options?.method).toBe("POST");
    expect(lastCall().options?.body).toEqual({ credential: "dummy-id-token" });
    expect(user.email).toBe("google@example.com");
  });
});
