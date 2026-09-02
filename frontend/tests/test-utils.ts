import type { ApiFetch } from "../app/types/api-fetch";

export type RequestSpy = {
  url: string;
  options?: Record<string, unknown>;
};

export function createMockApi(
  handler: (url: string, options?: Record<string, unknown>) => Promise<unknown>,
): {
  api: ApiFetch;
  lastCall: () => RequestSpy;
} {
  let last: RequestSpy = { url: "" };
  const api = ((url: unknown, options?: unknown) => {
    last = { url: String(url), options: options as Record<string, unknown> };
    return handler(String(url), options as Record<string, unknown>);
  }) as unknown as ApiFetch;

  return {
    api,
    lastCall: () => last,
  };
}
