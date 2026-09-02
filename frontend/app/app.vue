<template>
  <aside class="demo-notice" role="alert" aria-label="Thông báo website demo">
    <div class="demo-notice__inner">
      <span class="demo-notice__badge">DEMO ONLY</span>
      <div>
        <p class="demo-notice__title">WEBSITE DEMO — KHÔNG PHẢI DỊCH VỤ THẬT</p>
        <p class="demo-notice__description">
          Trang web này chỉ phục vụ mục đích trình diễn. Không sử dụng cho giao
          dịch, thanh toán hoặc cung cấp nội dung thực tế.
        </p>
      </div>
    </div>
  </aside>
  <NuxtRouteAnnouncer />
  <NuxtLayout>
    <NuxtPage />
  </NuxtLayout>
  <FloatingAssistantWidget />
</template>
