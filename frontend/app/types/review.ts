export type Review = {
  id: string;
  authorName: string;
  rating: number;
  comment: string | null;
  updatedAt: string;
};

export type ReviewsResponse = {
  averageRating: number;
  ratingCount: number;
  items: Review[];
};

export type CreateReviewPayload = {
  rating: number;
  comment?: string | null;
};
