export default defineNuxtRouteMiddleware(async (to) => {
  // /admin/** is client-rendered (see routeRules in nuxt.config.ts): the session
  // cookie is not available to the prerenderer, so gating server-side is pointless.
  if (import.meta.server) return;

  // Always re-resolve: entering the admin area is rare enough to afford one request,
  // and a cookie invalidated out of band (logout in another tab, key-ring rotation)
  // must not let a cached session render an admin shell that then 401s everywhere.
  const { fetchSession } = useAuthSession();
  const user = await fetchSession(true);

  if (!user) {
    return navigateTo({ path: "/login", query: { redirect: to.fullPath } });
  }

  if (user.role !== "admin") {
    // The API enforces this independently; this only avoids rendering a shell the
    // user cannot populate.
    return abortNavigation(
      createError({
        statusCode: 403,
        statusMessage: "Bạn không có quyền truy cập khu vực quản trị.",
        fatal: true,
      }),
    );
  }
});
