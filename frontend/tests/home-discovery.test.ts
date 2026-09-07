import { describe, expect, it } from "bun:test";
import type { TitleSummary } from "../app/types/catalog";

function takeUniqueTitles(
  titles: TitleSummary[],
  excludedSlugs: ReadonlySet<string>,
  limit = 5,
): TitleSummary[] {
  const seen = new Set(excludedSlugs);
  return titles.filter((title) => {
    if (seen.has(title.slug) || seen.size >= excludedSlugs.size + limit)
      return false;
    seen.add(title.slug);
    return true;
  });
}

describe("Homepage title distribution and deduplication", () => {
  const sampleTitles: TitleSummary[] = [
    {
      slug: "phim-1",
      title: "Phim 1",
      genre: "Hành Động",
      year: 2026,
      type: "movie",
      posterUrl: "https://example.test/1.jpg",
      isR2Hosted: true,
    },
    {
      slug: "phim-2",
      title: "Phim 2",
      genre: "Cổ Trang",
      year: 2025,
      type: "series",
      posterUrl: "https://example.test/2.jpg",
      isR2Hosted: false,
    },
    {
      slug: "phim-3",
      title: "Phim 3",
      genre: "Tình Cảm",
      year: 2026,
      type: "movie",
      posterUrl: "https://example.test/3.jpg",
      isR2Hosted: true,
    },
    {
      slug: "phim-4",
      title: "Phim 4",
      genre: "Hài Hước",
      year: 2024,
      type: "series",
      posterUrl: "https://example.test/4.jpg",
      isR2Hosted: false,
    },
    {
      slug: "phim-5",
      title: "Phim 5",
      genre: "Kinh Dị",
      year: 2026,
      type: "movie",
      posterUrl: "https://example.test/5.jpg",
      isR2Hosted: false,
    },
  ];

  it("filters out titles matching excluded slugs", () => {
    const excluded = new Set(["phim-1", "phim-2"]);
    const result = takeUniqueTitles(sampleTitles, excluded, 3);
    expect(result.map((t) => t.slug)).toEqual(["phim-3", "phim-4", "phim-5"]);
  });

  it("respects the limit parameter", () => {
    const excluded = new Set<string>();
    const result = takeUniqueTitles(sampleTitles, excluded, 2);
    expect(result).toHaveLength(2);
    expect(result.map((t) => t.slug)).toEqual(["phim-1", "phim-2"]);
  });

  it("handles duplicate slugs gracefully", () => {
    const withDuplicates = [...sampleTitles, sampleTitles[0]];
    const excluded = new Set<string>();
    const result = takeUniqueTitles(withDuplicates, excluded, 10);
    expect(result).toHaveLength(5);
  });
});
