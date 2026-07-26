<script setup lang="ts">
const props = withDefaults(
  defineProps<{ text?: "signin_with" | "signup_with" }>(),
  { text: "signin_with" },
);
const button = ref<HTMLElement | null>(null);
const error = ref("");
const config = useRuntimeConfig();
const route = useRoute();
const { $api } = useNuxtApp();
const { fetchSession } = useAuthSession();

/**
 * Only same-site absolute paths are honoured, so a crafted
 * `?redirect=https://evil.example` cannot turn login into an open redirect.
 */
function safeRedirectTarget() {
  const requested = route.query.redirect;
  const value = Array.isArray(requested) ? requested[0] : requested;
  if (typeof value !== "string") return "/";
  return value.startsWith("/") && !value.startsWith("//") ? value : "/";
}

declare global {
  interface Window {
    google?: {
      accounts: {
        id: {
          initialize: (options: {
            client_id: string;
            callback: (response: { credential: string }) => void;
            auto_select?: boolean;
          }) => void;
          renderButton: (
            parent: HTMLElement,
            options: {
              type: "standard";
              theme: "outline";
              size: "large";
              text: "signin_with" | "signup_with";
              shape: "pill";
              width: number;
            },
          ) => void;
        };
      };
    };
  }
}

function loadGoogleScript() {
  if (window.google) return Promise.resolve();
  return new Promise<void>((resolve, reject) => {
    const script = document.createElement("script");
    script.src = "https://accounts.google.com/gsi/client";
    script.async = true;
    script.defer = true;
    script.onload = () => resolve();
    script.onerror = () => reject(new Error("Không thể tải Google Sign-In."));
    document.head.appendChild(script);
  });
}

async function signIn(response: { credential: string }) {
  try {
    await $api("/v1/auth/google", {
      method: "POST",
      credentials: "include",
      body: { credential: response.credential },
    });
    // Refresh shared session state so the navbar and admin middleware see the
    // new role without another navigation.
    await fetchSession(true);
    await navigateTo(safeRedirectTarget());
  } catch {
    error.value = "Không thể đăng nhập với Google. Vui lòng thử lại.";
  }
}

onMounted(async () => {
  const clientId = config.public.googleClientId;
  if (!clientId || !button.value) {
    error.value = "Google Sign-In chưa được cấu hình.";
    return;
  }
  try {
    await loadGoogleScript();
    window.google?.accounts.id.initialize({
      client_id: clientId,
      callback: signIn,
      auto_select: false,
    });
    window.google?.accounts.id.renderButton(button.value, {
      type: "standard",
      theme: "outline",
      size: "large",
      text: props.text,
      shape: "pill",
      width: button.value.clientWidth || 360,
    });
  } catch (cause) {
    error.value =
      cause instanceof Error ? cause.message : "Không thể tải Google Sign-In.";
  }
});
</script>

<template>
  <div ref="button" class="min-h-12 w-full overflow-hidden rounded-xl" />
  <p
    v-if="error"
    class="mt-3 text-center text-xs text-destructive"
    role="alert"
  >
    {{ error }}
  </p>
</template>
