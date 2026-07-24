export default defineNuxtPlugin(() => {
  const config = useRuntimeConfig();

  const configuredBaseUrl = String(config.public.apiBaseUrl || "/");
  const baseURL =
    import.meta.server && configuredBaseUrl === "/"
      ? "https://movie-api.ziet.dev"
      : configuredBaseUrl;
  const api = $fetch.create({ baseURL, credentials: "include" });

  return {
    provide: { api },
  };
});
