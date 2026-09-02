import { computed, onMounted, ref } from "vue";
import {
  fetchCatalogTitleBySlug,
  fetchCatalogTitles,
  fetchTitleReviews,
} from "~/services/catalog.service";
import {
  fetchUserLibrary,
  removeTitleFromLibrary,
  saveTitleToLibrary,
  submitTitleReview,
} from "~/services/library.service";

export async function useMovieDetail() {
  const route = useRoute();
  const { locale, setLocale: setGlobalLocale } = useLocale();
  const slug = computed(() => String(route.params.slug));

  const { data: title, error } = await useAsyncData(
    () => `movie-${slug.value}-${locale.value}`,
    () => fetchCatalogTitleBySlug(slug.value, locale.value),
  );

  const { data: catalog } = await useAsyncData(
    () => `movie-recommendations-${locale.value}`,
    () => fetchCatalogTitles({ locale: locale.value }),
  );

  const { data: reviews, refresh: refreshReviews } = await useAsyncData(
    () => `movie-reviews-${slug.value}`,
    () => fetchTitleReviews(slug.value),
  );

  const isSaved = ref(false);
  const isTrailerOpen = ref(false);
  const actionNotice = ref("");
  const reviewRating = ref(0);
  const reviewComment = ref("");
  const isSubmittingReview = ref(false);

  const copy = computed(() =>
    locale.value === "vi"
      ? {
          watch: "Xem ngay",
          trailer: "Trailer",
          saved: "Đã lưu",
          save: "Lưu phim",
          info: "Thông tin chi tiết",
          cast: "Dàn diễn viên & ê-kíp",
          related: "Có thể bạn sẽ thích",
          minutes: "phút",
          movie: "Phim lẻ",
          series: "Phim bộ",
          director: "Đạo diễn",
          country: "Quốc gia",
          language: "Ngôn ngữ",
          vietnam: "Việt Nam",
          vietnamese: "Tiếng Việt",
          savedNotice: "Đã thêm vào danh sách.",
          removedNotice: "Đã bỏ khỏi danh sách.",
          signIn: "Hãy đăng nhập để lưu phim.",
          copied: "Đã sao chép liên kết.",
          reviews: "Đánh giá & bình luận",
          yourRating: "Điểm của bạn",
          commentPlaceholder: "Chia sẻ cảm nhận về bộ phim này…",
          publish: "Đăng đánh giá",
          noReviews: "Chưa có đánh giá nào. Hãy là người đầu tiên!",
          submitted: "Đã lưu đánh giá của bạn.",
        }
      : {
          watch: "Watch now",
          trailer: "Trailer",
          saved: "Saved",
          save: "Save title",
          info: "Title information",
          cast: "Cast & crew",
          related: "You may also like",
          minutes: "min",
          movie: "Movie",
          series: "Series",
          director: "Director",
          country: "Country",
          language: "Language",
          vietnam: "Vietnam",
          vietnamese: "Vietnamese",
          savedNotice: "Added to your list.",
          removedNotice: "Removed from your list.",
          signIn: "Sign in to save titles.",
          copied: "Link copied.",
          reviews: "Ratings & reviews",
          yourRating: "Your rating",
          commentPlaceholder: "Share what you think about this title…",
          publish: "Publish review",
          noReviews: "No reviews yet. Be the first!",
          submitted: "Your review has been saved.",
        },
  );

  useZMovieSeo({
    title: computed(() => title.value?.title ?? "Chi tiết phim"),
    description: computed(
      () =>
        title.value?.synopsis ??
        "Xem thông tin, trailer và đánh giá phim trên ZMovie.",
    ),
    image: computed(() => title.value?.posterUrl),
    type: "video.movie",
  });

  const related = computed(() =>
    (catalog.value?.items ?? [])
      .filter((item) => item.slug !== title.value?.slug)
      .slice(0, 5),
  );

  async function setLocale(nextLocale: "vi" | "en") {
    if (nextLocale === locale.value) return;
    setGlobalLocale(nextLocale);
    await refreshNuxtData([
      `movie-${slug.value}-${nextLocale}`,
      `movie-recommendations-${nextLocale}`,
    ]);
  }

  async function loadSavedState() {
    try {
      const library = await fetchUserLibrary(locale.value);
      isSaved.value = library.saved.some(
        (item) => item.slug === title.value?.slug,
      );
    } catch {
      isSaved.value = false;
    }
  }

  async function toggleSaved() {
    if (!title.value) return;
    try {
      if (isSaved.value) {
        await removeTitleFromLibrary(title.value.slug);
      } else {
        await saveTitleToLibrary(title.value.slug);
      }
      isSaved.value = !isSaved.value;
      actionNotice.value = isSaved.value
        ? copy.value.savedNotice
        : copy.value.removedNotice;
    } catch {
      actionNotice.value = copy.value.signIn;
    }
  }

  function openTrailer() {
    isTrailerOpen.value = true;
  }

  function closeTrailer() {
    isTrailerOpen.value = false;
  }

  async function shareTitle() {
    if (!title.value) return;
    try {
      if (navigator.share) {
        await navigator.share({
          title: title.value.title,
          url: window.location.href,
        });
      } else {
        await navigator.clipboard.writeText(window.location.href);
        actionNotice.value = copy.value.copied;
      }
    } catch {
      // Clipboard/share failures do not block the movie details page.
    }
  }

  async function submitReview() {
    if (!title.value || reviewRating.value < 1 || isSubmittingReview.value)
      return;
    isSubmittingReview.value = true;
    try {
      await submitTitleReview(title.value.slug, {
        rating: reviewRating.value,
        comment: reviewComment.value || null,
      });
      reviewComment.value = "";
      actionNotice.value = copy.value.submitted;
      await refreshReviews();
    } catch {
      actionNotice.value = copy.value.signIn;
    } finally {
      isSubmittingReview.value = false;
    }
  }

  function formattedDate(value: string) {
    return new Intl.DateTimeFormat(locale.value === "vi" ? "vi-VN" : "en-US", {
      day: "numeric",
      month: "short",
      year: "numeric",
    }).format(new Date(value));
  }

  onMounted(() => {
    void loadSavedState();
  });

  return {
    locale,
    title,
    error,
    catalog,
    reviews,
    isSaved,
    isTrailerOpen,
    actionNotice,
    reviewRating,
    reviewComment,
    isSubmittingReview,
    copy,
    related,
    formattedDate,
    setLocale,
    toggleSaved,
    openTrailer,
    closeTrailer,
    shareTitle,
    submitReview,
  };
}
