(function () {
    "use strict";

    const PENDING_LINK_PATTERN = /\[\[([^|\]]+)\]\]/g;
    const FIELD_IDS = ["id_description", "id_short_description"];
    const DEBOUNCE_MS = 500;
    const API_URL = "/article/by-name";
    const CREATE_URL = "/article/new/";

    const config = {
        excludeId: null,
    };

    let dropdown = null;
    let activeField = null;
    let activeMatch = null;
    let debounceTimer = null;
    let abortController = null;

    function init(options) {
        config.excludeId = options.excludeId || null;

        dropdown = document.createElement("div");
        dropdown.className = "link-selector";
        dropdown.hidden = true;
        document.body.appendChild(dropdown);

        FIELD_IDS.forEach((fieldId) => {
            const field = document.getElementById(fieldId);
            if (field) {
                field.addEventListener("input", handleFieldInput);
                field.addEventListener("blur", handleFieldBlur);
            }
        });

        document.addEventListener("mousedown", handleDocumentMouseDown);
        window.addEventListener("scroll", repositionDropdown, true);
        window.addEventListener("resize", repositionDropdown);
    }

    function findPendingLink(text, cursorPos) {
        PENDING_LINK_PATTERN.lastIndex = 0;
        let match;

        while ((match = PENDING_LINK_PATTERN.exec(text)) !== null) {
            const end = match.index + match[0].length;
            if (end === cursorPos) {
                return {
                    query: match[1],
                    start: match.index,
                    end: end,
                };
            }
        }

        return null;
    }

    function handleFieldInput(event) {
        const field = event.target;
        const cursorPos = field.selectionStart;
        const pending = findPendingLink(field.value, cursorPos);

        if (!pending) {
            hideDropdown();
            return;
        }

        activeField = field;
        activeMatch = pending;
        showLoadingDropdown();

        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(() => {
            fetchArticles(pending.query);
        }, DEBOUNCE_MS);
    }

    function handleFieldBlur() {
        setTimeout(() => {
            if (!dropdown.matches(":hover") && !dropdown.querySelector(":focus-within")) {
                hideDropdown();
            }
        }, 150);
    }

    function handleDocumentMouseDown(event) {
        if (dropdown.hidden) {
            return;
        }

        if (dropdown.contains(event.target)) {
            return;
        }

        if (activeField && activeField.contains(event.target)) {
            return;
        }

        hideDropdown();
    }

    function buildApiUrl(query) {
        const params = new URLSearchParams({ query });
        if (config.excludeId) {
            params.set("exclude_id", String(config.excludeId));
        }
        return `${API_URL}?${params.toString()}`;
    }

    async function fetchArticles(query) {
        if (abortController) {
            abortController.abort();
        }

        abortController = new AbortController();

        try {
            const response = await fetch(buildApiUrl(query), {
                signal: abortController.signal,
            });

            if (!response.ok) {
                throw new Error("Search request failed");
            }

            const articles = await response.json();
            renderDropdown(articles, query);
        } catch (error) {
            if (error.name !== "AbortError") {
                renderErrorDropdown(query);
            }
        }
    }

    function showLoadingDropdown() {
        dropdown.innerHTML = '<div class="link-selector__status">Searching...</div>';
        dropdown.hidden = false;
        repositionDropdown();
    }

    function renderErrorDropdown(query) {
        dropdown.innerHTML = `
            <div class="link-selector__empty">
                <p class="link-selector__empty-text">Could not load articles.</p>
                <a class="link-selector__create" href="${CREATE_URL}">Create article</a>
            </div>
        `;
        dropdown.hidden = false;
        repositionDropdown();
    }

    function renderDropdown(articles, query) {
        if (!activeField || !activeMatch) {
            return;
        }

        if (articles.length === 0) {
            dropdown.innerHTML = `
                <div class="link-selector__empty">
                    <p class="link-selector__empty-text">No articles found for "<strong>${escapeHtml(query)}</strong>".</p>
                    <a class="link-selector__create" href="${CREATE_URL}">Create article</a>
                </div>
            `;
        } else {
            const items = articles
                .map(
                    (article) => `
                    <button
                        type="button"
                        class="link-selector__item"
                        data-id="${article.id}"
                    >
                        <span class="link-selector__title">${escapeHtml(article.title)}</span>
                        <span class="link-selector__id">#${article.id}</span>
                    </button>
                `
                )
                .join("");

            dropdown.innerHTML = `<div class="link-selector__list">${items}</div>`;

            dropdown.querySelectorAll(".link-selector__item").forEach((button) => {
                button.addEventListener("mousedown", (event) => {
                    event.preventDefault();
                    applySelection(button.dataset.id);
                });
            });
        }

        dropdown.hidden = false;
        repositionDropdown();
    }

    function applySelection(articleId) {
        if (!activeField || !activeMatch) {
            return;
        }

        const replacement = `[[${articleId}|${activeMatch.query}]]`;
        const value = activeField.value;
        activeField.value =
            value.slice(0, activeMatch.start) +
            replacement +
            value.slice(activeMatch.end);

        const cursorPos = activeMatch.start + replacement.length;
        activeField.focus();
        activeField.setSelectionRange(cursorPos, cursorPos);

        hideDropdown();
    }

    function repositionDropdown() {
        if (dropdown.hidden || !activeField) {
            return;
        }

        const rect = activeField.getBoundingClientRect();
        dropdown.style.top = `${rect.bottom + 8}px`;
        dropdown.style.left = `${rect.left}px`;
        dropdown.style.width = `${Math.max(rect.width, 280)}px`;
    }

    function hideDropdown() {
        clearTimeout(debounceTimer);

        if (abortController) {
            abortController.abort();
            abortController = null;
        }

        dropdown.hidden = true;
        dropdown.innerHTML = "";
        activeField = null;
        activeMatch = null;
    }

    function escapeHtml(value) {
        return String(value)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#39;");
    }

    document.addEventListener("DOMContentLoaded", () => {
        const root = document.querySelector("[data-article-link-selector]");
        if (!root) {
            return;
        }

        init({
            excludeId: root.dataset.articleId || null,
        });
    });
})();
