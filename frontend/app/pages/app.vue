<script setup lang="ts">
import {
  Apple,
  CheckCircle2,
  Download,
  Laptop,
  Monitor,
  Smartphone,
  Sparkles,
} from "@lucide/vue";

useHead({ title: "Cài đặt ứng dụng — ZMovie" });

const { isInstallable, isInstalled, promptInstall } = usePwaInstall();
const activePlatform = ref<"ios" | "android" | "desktop">("ios");
const installSuccess = ref(false);

async function handleInstall() {
  const result = await promptInstall();
  if (result) installSuccess.value = true;
}

onMounted(() => {
  if (!import.meta.client) return;
  const ua = navigator.userAgent.toLowerCase();
  if (ua.includes("iphone") || ua.includes("ipad") || ua.includes("ipod")) {
    activePlatform.value = "ios";
  } else if (ua.includes("android")) {
    activePlatform.value = "android";
  } else {
    activePlatform.value = "desktop";
  }
});
</script>

<template>
  <main class="min-h-screen bg-background text-foreground">
    <AppNavbar />
    <section class="mx-auto max-w-360 px-5 pb-24 pt-12 lg:px-12">
      <!-- Hero Banner -->
      <div
        class="relative overflow-hidden rounded-3xl border border-white/10 bg-gradient-to-br from-[#1d1e2c] via-[#12131a] to-[#0c0d12] p-8 sm:p-12 lg:p-16"
      >
        <div class="relative z-10 max-w-2xl">
          <span
            class="inline-flex items-center gap-2 rounded-full border border-primary/30 bg-primary/10 px-3.5 py-1 text-xs font-bold text-primary"
          >
            <Sparkles class="size-3.5" /> Ứng dụng web tiến bộ (PWA)
          </span>
          <h1
            class="font-display mt-4 text-3xl font-extrabold tracking-tight sm:text-4xl lg:text-5xl"
          >
            Cài đặt ZMovie về thiết bị của bạn
          </h1>
          <p
            class="mt-4 text-sm leading-relaxed text-muted-foreground sm:text-base"
          >
            Mở ứng dụng ngay từ màn hình chính, xem phim toàn màn hình không
            dính thanh địa chỉ, tải nhanh hơn và mượt mà hơn.
          </p>

          <div class="mt-8 flex flex-wrap items-center gap-4">
            <button
              v-if="isInstallable"
              class="inline-flex items-center gap-2.5 rounded-2xl bg-primary px-6 py-3.5 text-sm font-black text-primary-foreground shadow-[0_0_30px_rgba(255,216,117,0.3)] transition hover:scale-105 active:scale-95"
              @click="handleInstall"
            >
              <Download class="size-4.5" />
              <span>Cài đặt ZMovie ngay</span>
            </button>
            <div
              v-else-if="isInstalled || installSuccess"
              class="inline-flex items-center gap-2 rounded-2xl border border-emerald-500/30 bg-emerald-500/10 px-6 py-3.5 text-sm font-bold text-emerald-300"
            >
              <CheckCircle2 class="size-5 text-emerald-400" />
              <span>Đã cài đặt ứng dụng trên thiết bị</span>
            </div>
            <a
              href="#guide"
              class="inline-flex items-center gap-2 rounded-2xl border border-white/10 bg-white/5 px-6 py-3.5 text-sm font-semibold transition hover:border-primary/50"
            >
              <span>Xem hướng dẫn thủ công</span>
            </a>
          </div>
        </div>

        <div
          class="pointer-events-none absolute -right-12 -top-12 size-96 rounded-full bg-primary/10 blur-3xl"
        />
      </div>

      <!-- Feature Grid -->
      <div class="mt-12 grid gap-6 sm:grid-cols-3">
        <div class="rounded-2xl border border-white/8 bg-[#171717] p-6">
          <div
            class="grid size-12 place-items-center rounded-xl bg-primary/10 text-primary"
          >
            <Smartphone class="size-6" />
          </div>
          <h3 class="mt-4 font-bold">Khởi động tức thì</h3>
          <p class="mt-2 text-xs leading-relaxed text-muted-foreground">
            Biểu tượng ứng dụng ngay ngoài màn hình chính, không cần mở trình
            duyệt rồi gõ địa chỉ.
          </p>
        </div>

        <div class="rounded-2xl border border-white/8 bg-[#171717] p-6">
          <div
            class="grid size-12 place-items-center rounded-xl bg-primary/10 text-primary"
          >
            <Monitor class="size-6" />
          </div>
          <h3 class="mt-4 font-bold">Trải nghiệm toàn màn hình</h3>
          <p class="mt-2 text-xs leading-relaxed text-muted-foreground">
            Giao diện rạp chiếu không viền, ẩn thanh điều hướng trình duyệt để
            tập trung hoàn toàn vào nội dung.
          </p>
        </div>

        <div class="rounded-2xl border border-white/8 bg-[#171717] p-6">
          <div
            class="grid size-12 place-items-center rounded-xl bg-primary/10 text-primary"
          >
            <Download class="size-6" />
          </div>
          <h3 class="mt-4 font-bold">Tiết kiệm bộ nhớ</h3>
          <p class="mt-2 text-xs leading-relaxed text-muted-foreground">
            Dung lượng cài đặt siêu nhẹ dưới 2MB, tự động cập nhật phiên bản mới
            nhất mỗi khi có phim mới.
          </p>
        </div>
      </div>

      <!-- Installation Guide Section -->
      <section id="guide" class="mt-16">
        <div class="text-center">
          <h2
            class="font-display text-2xl font-bold tracking-tight sm:text-3xl"
          >
            Hướng dẫn cài đặt theo thiết bị
          </h2>
          <p class="mt-2 text-sm text-muted-foreground">
            Chọn nền tảng của bạn để xem từng bước chi tiết
          </p>
        </div>

        <!-- Platform Tabs -->
        <div class="mt-8 flex justify-center gap-2">
          <button
            class="inline-flex items-center gap-2 rounded-full px-5 py-2.5 text-xs font-bold transition"
            :class="
              activePlatform === 'ios'
                ? 'bg-primary text-primary-foreground shadow-md'
                : 'bg-white/5 text-muted-foreground hover:text-foreground'
            "
            @click="activePlatform = 'ios'"
          >
            <Apple class="size-4" />
            <span>iPhone / iPad (Safari)</span>
          </button>
          <button
            class="inline-flex items-center gap-2 rounded-full px-5 py-2.5 text-xs font-bold transition"
            :class="
              activePlatform === 'android'
                ? 'bg-primary text-primary-foreground shadow-md'
                : 'bg-white/5 text-muted-foreground hover:text-foreground'
            "
            @click="activePlatform = 'android'"
          >
            <Smartphone class="size-4" />
            <span>Android (Chrome)</span>
          </button>
          <button
            class="inline-flex items-center gap-2 rounded-full px-5 py-2.5 text-xs font-bold transition"
            :class="
              activePlatform === 'desktop'
                ? 'bg-primary text-primary-foreground shadow-md'
                : 'bg-white/5 text-muted-foreground hover:text-foreground'
            "
            @click="activePlatform = 'desktop'"
          >
            <Laptop class="size-4" />
            <span>Máy tính (Chrome / Edge)</span>
          </button>
        </div>

        <!-- Guide Steps -->
        <div class="mt-8 mx-auto max-w-3xl">
          <!-- iOS Guide -->
          <div
            v-if="activePlatform === 'ios'"
            class="space-y-4 rounded-3xl border border-white/8 bg-[#171717] p-6 sm:p-8"
          >
            <div class="flex items-start gap-4">
              <span
                class="grid size-8 shrink-0 place-items-center rounded-full bg-primary/20 text-sm font-bold text-primary"
                >1</span
              >
              <div>
                <strong class="text-sm font-semibold text-foreground"
                  >Mở trang web bằng trình duyệt Safari</strong
                >
                <p class="mt-1 text-xs text-muted-foreground">
                  Đảm bảo bạn đang truy cập ZMovie trên Safari mặc định của iOS.
                </p>
              </div>
            </div>

            <div class="flex items-start gap-4">
              <span
                class="grid size-8 shrink-0 place-items-center rounded-full bg-primary/20 text-sm font-bold text-primary"
                >2</span
              >
              <div>
                <strong class="text-sm font-semibold text-foreground"
                  >Nhấn nút Chia sẻ (Share)</strong
                >
                <p class="mt-1 text-xs text-muted-foreground">
                  Nút biểu tượng hình vuông có mũi tên hướng lên ở thanh công cụ
                  dưới cùng.
                </p>
              </div>
            </div>

            <div class="flex items-start gap-4">
              <span
                class="grid size-8 shrink-0 place-items-center rounded-full bg-primary/20 text-sm font-bold text-primary"
                >3</span
              >
              <div>
                <strong class="text-sm font-semibold text-foreground"
                  >Chọn "Thêm vào Màn hình chính" (Add to Home Screen)</strong
                >
                <p class="mt-1 text-xs text-muted-foreground">
                  Cuộn xuống trong menu chia sẻ và chọn mục này.
                </p>
              </div>
            </div>

            <div class="flex items-start gap-4">
              <span
                class="grid size-8 shrink-0 place-items-center rounded-full bg-primary/20 text-sm font-bold text-primary"
                >4</span
              >
              <div>
                <strong class="text-sm font-semibold text-foreground"
                  >Nhấn "Thêm" (Add) ở góc phải trên</strong
                >
                <p class="mt-1 text-xs text-muted-foreground">
                  Biểu tượng ZMovie sẽ xuất hiện trên màn hình chính như một ứng
                  dụng thực thụ.
                </p>
              </div>
            </div>
          </div>

          <!-- Android Guide -->
          <div
            v-else-if="activePlatform === 'android'"
            class="space-y-4 rounded-3xl border border-white/8 bg-[#171717] p-6 sm:p-8"
          >
            <div class="flex items-start gap-4">
              <span
                class="grid size-8 shrink-0 place-items-center rounded-full bg-primary/20 text-sm font-bold text-primary"
                >1</span
              >
              <div>
                <strong class="text-sm font-semibold text-foreground"
                  >Mở bằng Chrome trên điện thoại Android</strong
                >
                <p class="mt-1 text-xs text-muted-foreground">
                  Truy cập địa chỉ ZMovie trong trình duyệt Google Chrome.
                </p>
              </div>
            </div>

            <div class="flex items-start gap-4">
              <span
                class="grid size-8 shrink-0 place-items-center rounded-full bg-primary/20 text-sm font-bold text-primary"
                >2</span
              >
              <div>
                <strong class="text-sm font-semibold text-foreground"
                  >Nhấn menu ba chấm (⋮) góc trên bên phải</strong
                >
                <p class="mt-1 text-xs text-muted-foreground">
                  Mở danh sách tùy chọn trình duyệt.
                </p>
              </div>
            </div>

            <div class="flex items-start gap-4">
              <span
                class="grid size-8 shrink-0 place-items-center rounded-full bg-primary/20 text-sm font-bold text-primary"
                >3</span
              >
              <div>
                <strong class="text-sm font-semibold text-foreground"
                  >Chọn "Cài đặt ứng dụng" hoặc "Thêm vào MH chính"</strong
                >
                <p class="mt-1 text-xs text-muted-foreground">
                  Xác nhận cài đặt khi bảng thông báo hiện lên.
                </p>
              </div>
            </div>
          </div>

          <!-- Desktop Guide -->
          <div
            v-else
            class="space-y-4 rounded-3xl border border-white/8 bg-[#171717] p-6 sm:p-8"
          >
            <div class="flex items-start gap-4">
              <span
                class="grid size-8 shrink-0 place-items-center rounded-full bg-primary/20 text-sm font-bold text-primary"
                >1</span
              >
              <div>
                <strong class="text-sm font-semibold text-foreground"
                  >Dùng Chrome hoặc Microsoft Edge</strong
                >
                <p class="mt-1 text-xs text-muted-foreground">
                  Mở trang web trên trình duyệt hỗ trợ Progressive Web App.
                </p>
              </div>
            </div>

            <div class="flex items-start gap-4">
              <span
                class="grid size-8 shrink-0 place-items-center rounded-full bg-primary/20 text-sm font-bold text-primary"
                >2</span
              >
              <div>
                <strong class="text-sm font-semibold text-foreground"
                  >Nhấn biểu tượng Cài đặt trên thanh địa chỉ</strong
                >
                <p class="mt-1 text-xs text-muted-foreground">
                  Biểu tượng máy tính nhỏ có mũi tên tải xuống ở cuối thanh URL.
                </p>
              </div>
            </div>

            <div class="flex items-start gap-4">
              <span
                class="grid size-8 shrink-0 place-items-center rounded-full bg-primary/20 text-sm font-bold text-primary"
                >3</span
              >
              <div>
                <strong class="text-sm font-semibold text-foreground"
                  >Nhấn "Cài đặt" để mở trong cửa sổ riêng biệt</strong
                >
                <p class="mt-1 text-xs text-muted-foreground">
                  Ứng dụng sẽ có lối tắt riêng trên Desktop và thanh Taskbar /
                  Dock.
                </p>
              </div>
            </div>
          </div>
        </div>
      </section>
    </section>
  </main>
</template>
