import { unified } from "@astrojs/markdown-remark";
import starlight from "@astrojs/starlight";
import { defineConfig } from "astro/config";
import starlightAutoDrafts from "starlight-auto-drafts";
import starlightChangelogs from "starlight-changelogs";
import starlightGithubAlerts from "starlight-github-alerts";
import starlightHeadingBadges from "starlight-heading-badges";
import starlightImageZoom from "starlight-image-zoom";
import starlightLinksValidator from "starlight-links-validator";
import starlightLlmsTxt from "starlight-llms-txt";
import starlightPageActions from "starlight-page-actions";
import { starlightIconsPlugin } from "starlight-plugin-icons";
import starlightScrollToTop from "starlight-scroll-to-top";
import starlightSidebarTopics from "starlight-sidebar-topics";
import starlightTags from "starlight-tags";

// In GitHub Actions, the site is served under /Optionary; locally we run at the root. The gh-pages branch holds latest main at
// that root, each release under /Optionary/v<version> and pull request previews under /Optionary/pr-preview/pr-<n>, so CI sets
// DOCS_BASE to the folder a build deploys to. The version switcher reads versions.json from the root.
const root = process.env.DOCS_ROOT || (process.env.GITHUB_ACTIONS ? "/Optionary" : "");
const base = process.env.DOCS_BASE || root;

export default defineConfig({
    site: "https://rocketsurgeonsguild.github.io",
    base,
    vite: {
        define: {
            __DOCS_ROOT__: JSON.stringify(root),
        },
    },
    markdown: {
        processor: unified(),
    },
    integrations: [
        starlight({
            title: "Optionary",
            description: "Source-generated options binding and JSON Schema for .NET configuration.",
            logo: {
                light: "./src/assets/optionary-wordmark-light.svg",
                dark: "./src/assets/optionary-wordmark-dark.svg",
                alt: "Optionary",
                replacesTitle: true,
            },
            social: [
                {
                    icon: "github",
                    label: "GitHub",
                    href: "https://github.com/RocketSurgeonsGuild/Optionary",
                },
            ],
            components: {
                // Adds the docs version switcher (src/components/VersionSelect.astro) next to the theme picker.
                ThemeSelect: "./src/components/overrides/ThemeSelect.astro",
            },
            customCss: ["./src/styles/theme.css", "./src/styles/api-reference-site.css", "./src/styles/api-reference.css"],
            plugins: [
                starlightAutoDrafts(),
                starlightGithubAlerts(),
                starlightSidebarTopics(
                    [
                        {
                            label: "Getting Started",
                            link: "/",
                            icon: "open-book",
                            items: [
                                { label: "Overview", link: "/" },
                                { label: "Getting started", link: "/getting-started/" },
                                {
                                    label: "Guides",
                                    items: [
                                        { label: "Declarations", link: "/guides/declarations/" },
                                        { label: "Key styles", link: "/guides/key-styles/" },
                                        { label: "Binding and validation", link: "/guides/binding/" },
                                        { label: "Schema and editors", link: "/guides/schema-and-editors/" },
                                        { label: "Registering from a library", link: "/guides/registering-from-a-library/" },
                                        { label: "JSON joints", link: "/guides/json/" },
                                        { label: "YAML provider", link: "/guides/yaml/" },
                                        { label: "TOML with Dovetail", link: "/guides/toml/" },
                                    ],
                                },
                                {
                                    label: "Reference",
                                    items: [
                                        { label: "MSBuild properties", link: "/reference/msbuild-properties/" },
                                        { label: "Diagnostics", link: "/reference/diagnostics/" },
                                    ],
                                },
                                { label: "Troubleshooting", link: "/troubleshooting/" },
                            ],
                        },
                        {
                            label: "API Reference",
                            link: "/api/",
                            icon: "code-branch",
                            items: [{ label: "Search C# APIs", link: "/api/" }],
                        },
                        {
                            label: "Changelog",
                            link: "/changelog/",
                            icon: "list-format",
                        },
                    ],
                    {
                        // The API reference pages (src/pages/api/) build their own package/namespace/type sidebar.
                        exclude: ["/tags/**", "/changelog/**", "/api", "/api/**"],
                    },
                ),
                starlightLinksValidator({
                    errorOnRelativeLinks: false,
                    // The API reference pages are Astro routes (src/pages/api/) generated from the build's JSON, not
                    // content entries, so this plugin can't see them.
                    exclude: ["/api", "/api/**"],
                }),
                starlightHeadingBadges(),
                starlightImageZoom(),
                starlightScrollToTop(),
                starlightPageActions({
                    editLink: "https://github.com/RocketSurgeonsGuild/Optionary/edit/main/docs/src/content/docs/",
                }),
                starlightIconsPlugin(),
                starlightTags(),
                starlightChangelogs(),
                starlightLlmsTxt(),
            ],
        }),
    ],
});
