## Purpose

Enriches movie metadata with actors, directors, country of origin, preview trailers, TMDB 4K enrichment, and automated SEO structured data, while enabling multi-faceted filtering on the browse page.

## Requirements

### Requirement: Enriched Title Metadata Representation
The system SHALL store and expose comprehensive movie metadata including actor lists with real profile photos and character names, director biographies, country tags, official YouTube trailer embeds, and official 4K backdrops and posters enriched from The Movie Database (TMDB) API and Cloudflare R2 demo titles, and MUST present title details in a two-column layout featuring a floating poster, metadata chips (`IMDb`, `T16`, release year, season/episode progress), circular cast avatars, and a weekly top 10 ranking leaderboard.

#### Scenario: Displaying movie detail with NguonC metadata
- **WHEN** a user visits a movie detail page populated from NguonC or R2 demo data
- **THEN** the system SHALL display the list of starring actors with circular photo avatars, director names, country of production, status badge ("Đã chiếu"), action bar (`▶ Xem Ngay`, favorites, share, review rating), and tabbed views for episodes, gallery, and recommendations

#### Scenario: Playing trailer in a preview modal
- **WHEN** a user clicks the "Watch Trailer" button on a title card or detail page
- **THEN** the system SHALL open a modal dialog embedding the responsive trailer video player without navigating away from the page

#### Scenario: Displaying movie detail with TMDB 4K enrichment
- **WHEN** a user visits a movie detail page enriched by TMDB
- **THEN** the system SHALL display high-resolution 4K backdrops, official YouTube trailer embeds, actor portrait avatars with character names, and verified release information

#### Scenario: TMDB fallback to catalog crawler metadata
- **WHEN** a title cannot be matched against TMDB API
- **THEN** the system SHALL seamlessly fall back to default metadata imported from NguonC or OPhim without broken image links or missing fields

#### Scenario: Displaying weekly top 10 leaderboard
- **WHEN** a user views the detail page sidebar
- **THEN** the system SHALL render a ranked list from 1 to 10 featuring outlined rank numbers, poster thumbnails, title names, and release metadata

### Requirement: Multi-Faceted Catalog Browse Filter
The catalog browse page SHALL allow users to toggle an expandable multi-tier filter panel supporting simultaneous filtering by Country (`Quốc gia`), Media Type (`Loại phim`), Age Rating (`Xếp hạng`), and Genres (`Thể loại`), and MUST render titles in a responsive grid displaying badge overlays (`Song ngữ`, `4K`, `P.Đề`, `T.Minh`).

#### Scenario: Applying combined genre and country filters
- **WHEN** a user selects multiple genres (e.g. Action and Thriller) and filters by Country (e.g. South Korea)
- **THEN** the browse list SHALL display only titles matching all selected criteria and update the browser URL query parameters

#### Scenario: Resetting active filters
- **WHEN** a user clicks the "Clear Filters" action
- **THEN** the browse page SHALL reset all facet selections to default and display the full catalog list

#### Scenario: Toggling expandable filter panel
- **WHEN** a user clicks the "Bộ lọc" toggle button
- **THEN** the system SHALL expand or collapse the full multi-tier filter matrix without obscuring the active movie results

### Requirement: Automated Rich SEO and Structured Data Generation
The system SHALL automatically generate search engine optimized metadata, OpenGraph tags, dynamic social sharing preview cards, and Schema.org JSON-LD structured data for every movie and series in the catalog.

#### Scenario: Search engine crawler reads title page
- **WHEN** a web crawler (e.g. Googlebot) requests a title detail or watch page
- **THEN** the server-rendered HTML response SHALL include Schema.org `Movie` or `VideoObject` structured JSON-LD data specifying title, duration, rating, upload date, and thumbnail URL

#### Scenario: Social sharing link preview generation
- **WHEN** a user shares a title link on social messaging platforms (Facebook, Zalo, Telegram)
- **THEN** the platform SHALL serve OpenGraph meta tags (`og:image`, `og:title`, `og:description`, `og:video`) presenting an optimized branded preview card
