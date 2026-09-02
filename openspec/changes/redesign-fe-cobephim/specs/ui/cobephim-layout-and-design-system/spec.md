## Purpose

Defines the global CôBéPhim design system, brand visual identity, minimalist sticky header with slide-out drawer navigation, topic cards, and sovereignty footer.

## ADDED Requirements

### Requirement: Minimalist Sticky Header and Slide-out Navigation Drawer
The application SHALL render a minimalist sticky top header displaying a hamburger menu trigger, the CôBéPhim brand logo with tagline, and a search action button, and MUST open a slide-out drawer navigation on mobile and desktop viewports when the hamburger menu is activated.

#### Scenario: Opening the slide-out navigation drawer
- **WHEN** a user clicks the hamburger menu icon in the header
- **THEN** the system SHALL animate open a slide-out navigation drawer from the left edge containing the "Thành viên" profile action, app banner, and categorized navigation links (`Thể loại`, `Phim Lẻ`, `Phim Bộ`, `Quốc gia`, `Thêm`) with an ambient backdrop overlay

#### Scenario: Closing the navigation drawer
- **WHEN** the user clicks the close icon (`X`) or the backdrop overlay outside the drawer
- **THEN** the system SHALL smoothly dismiss the navigation drawer and restore focus to the page

### Requirement: Interactive Hero Spotlight and Topic Cards
The home page SHALL render an immersive full-bleed hero banner featuring movie titles, metadata chips, synopsis, circular quick actions, and an interactive thumbnail preview switcher, followed by a "Bạn đang quan tâm gì?" section of gradient topic cards.

#### Scenario: Switching hero spotlight title via thumbnail
- **WHEN** a user clicks any preview thumbnail card in the hero slider
- **THEN** the hero banner SHALL transition its backdrop image, title, metadata pills, and description to the selected spotlight movie without reloading the page

#### Scenario: Navigating from topic cards
- **WHEN** a user clicks any topic card (e.g., "Top IMDb", "Thuyết Minh", "Phim 4K", "Netflix")
- **THEN** the application SHALL navigate to the catalog browse view pre-filtered by the chosen collection criteria

### Requirement: Vietnamese Sovereignty Footer and Social Hub
The layout SHALL render a persistent shared footer featuring a prominent sovereignty assertion banner, official CôBéPhim brand mark, community social links, information links, and contact channels.

#### Scenario: Displaying the sovereignty banner
- **WHEN** any public page renders the footer
- **THEN** the system SHALL display the highlighted banner stating "★ Hoàng Sa & Trường Sa là của Việt Nam!" above social channels and copyright information
