export type UserRole = "member" | "admin";

export type SessionUser = {
  id: string;
  email: string;
  displayName: string;
  avatarUrl: string | null;
  role: UserRole;
};

export type Paged<T> = {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
  pageCount: number;
};

export type AdminTopTitle = {
  slug: string;
  title: string;
  posterUrl: string;
  views: number;
};

export type AdminUserSummary = {
  id: string;
  email: string;
  displayName: string;
  avatarUrl: string | null;
  role: UserRole;
  createdAt: string;
  lastSignedInAt: string;
};

export type AdminOverview = {
  titleCount: number;
  movieCount: number;
  seriesCount: number;
  featuredCount: number;
  episodeCount: number;
  genreCount: number;
  userCount: number;
  adminCount: number;
  reviewCount: number;
  averageRating: number;
  viewsLast24Hours: number;
  viewsLast7Days: number;
  topTitles: AdminTopTitle[];
  recentUsers: AdminUserSummary[];
};

export type AdminTitleSummary = {
  id: string;
  slug: string;
  vietnameseTitle: string;
  englishTitle: string;
  genre: string;
  year: number;
  type: string;
  posterUrl: string;
  runtimeMinutes: number;
  featured: boolean;
  episodeCount: number;
  updatedAt: string;
};

export type AdminTitleDetail = AdminTitleSummary & {
  vietnameseSynopsis: string;
  englishSynopsis: string;
  viewCount: number;
  reviewCount: number;
  createdAt: string;
};

export type AdminTitleEdit = {
  vietnameseTitle: string;
  englishTitle: string;
  vietnameseSynopsis: string;
  englishSynopsis: string;
  genre: string;
  year: number;
  type: string;
  posterUrl: string;
  runtimeMinutes: number;
  featured: boolean;
};

export type AdminReviewSummary = {
  id: string;
  titleSlug: string;
  titleName: string;
  userId: string;
  authorName: string;
  rating: number;
  comment: string | null;
  updatedAt: string;
};

export type AdminGenreSummary = {
  id: string;
  slug: string;
  name: string;
  titleCount: number;
  updatedAt: string;
};
