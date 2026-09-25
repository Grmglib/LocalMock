(function () {
    const form = document.getElementById("mock-form");
    const methodField = document.getElementById("method");
    const pathField = document.getElementById("path");
    const statusCodeField = document.getElementById("status-code");
    const responseDelayMsField = document.getElementById("response-delay-ms");
    const mockEnabledField = document.getElementById("mock-enabled");
    const bypassEnabledField = document.getElementById("bypass-enabled");
    const bypassUrlField = document.getElementById("bypass-url");
    const bypassUrlFieldWrapper = document.getElementById("bypass-url-field");
    const responseContentTypeField = document.getElementById("response-content-type");
    const responseBodyTypeLabel = document.getElementById("response-body-type-label");
    const responseBodyField = document.getElementById("response-body");
    const responseBodyGutter = document.getElementById("response-body-gutter");
    const endpointPreview = document.getElementById("endpoint-preview");
    const previewMethodBadge = document.getElementById("preview-method-badge");
    const formTitle = document.getElementById("form-title");
    const formFeedback = document.getElementById("form-feedback");
    const listFeedback = document.getElementById("list-feedback");
    const reloadButton = document.getElementById("reload-button");
    const resetFormButton = document.getElementById("reset-form-button");
    const newMockButton = document.getElementById("new-mock-button");
    const mockList = document.getElementById("mock-list");
    const importMocksButton = document.getElementById("import-mocks-button");
    const exportMocksButton = document.getElementById("export-mocks-button");
    const importMocksInput = document.getElementById("import-mocks-input");
    const themeToggleButton = document.getElementById("theme-toggle-button");
    const toastContainer = document.getElementById("toast-container");
    const mockSearchField = document.getElementById("mock-search");
    const filterMethodField = document.getElementById("filter-method");
    const filterStatusField = document.getElementById("filter-status");
    const collectionSelectField = document.getElementById("collection-select");
    const screenMocks = document.getElementById("screen-mocks");
    const screenCollections = document.getElementById("screen-collections");
    const navMocks = document.getElementById("nav-mocks");
    const navCollections = document.getElementById("nav-collections");
    const topbarPageEyebrow = document.getElementById("topbar-page-eyebrow");
    const topbarPageTitle = document.getElementById("topbar-page-title");
    const topbarPageSubtitle = document.getElementById("topbar-page-subtitle");

    const TOPBAR_PAGE_COPY = {
        mocks: {
            eyebrow: "Mocks",
            title: "Endpoint configuration",
            subtitle: "Create, test, and manage mocked responses by collection or as standalone mocks."
        },
        collections: {
            eyebrow: "Collections",
            title: "Collection configuration",
            subtitle: "Set the identifier and bypass URL. Endpoints without a mock in the collection are forwarded automatically."
        }
    };
    const collectionsList = document.getElementById("collections-list");
    const collectionsListFeedback = document.getElementById("collections-list-feedback");
    const collectionSearchField = document.getElementById("collection-search");
    const newCollectionSidebarButton = document.getElementById("new-collection-sidebar-button");
    const reloadCollectionsButton = document.getElementById("reload-collections-button");
    const collectionForm = document.getElementById("collection-form");
    const collectionFormIdField = document.getElementById("collection-form-id");
    const collectionFormIdFieldWrapper = document.getElementById("collection-form-id-field");
    const collectionFormBypassUrlField = document.getElementById("collection-form-bypass-url");
    const collectionFormFeedback = document.getElementById("collection-form-feedback");
    const collectionConfigSubtitle = document.getElementById("collection-config-subtitle");
    const collectionUrlPreview = document.getElementById("collection-url-preview");
    const collectionMockCount = document.getElementById("collection-mock-count");
    const saveCollectionFormButton = document.getElementById("save-collection-form-button");
    const deleteCollectionFormButton = document.getElementById("delete-collection-form-button");
    const openCollectionMocksButton = document.getElementById("open-collection-mocks-button");
    const resetCollectionFormButton = document.getElementById("reset-collection-form-button");

    const testForm = document.getElementById("test-form");
    const testMethodField = document.getElementById("test-method");
    const testPathField = document.getElementById("test-path");
    const curlModal = document.getElementById("curl-modal");
    const showCurlImportButton = document.getElementById("show-curl-import-button");
    const testCurlField = document.getElementById("test-curl");
    const importCurlButton = document.getElementById("import-curl-button");
    const cancelCurlImportButton = document.getElementById("cancel-curl-import-button");
    const testContentTypeField = document.getElementById("test-content-type");
    const testRequestHeadersField = document.getElementById("test-request-headers");
    const testRequestBodyField = document.getElementById("test-request-body");
    const testRequestBodyGutter = document.getElementById("test-request-body-gutter");
    const testEndpointPreview = document.getElementById("test-endpoint-preview");
    const testFeedback = document.getElementById("test-feedback");
    const testResponseStatus = document.getElementById("test-response-status");
    const testResponseContentType = document.getElementById("test-response-content-type");
    const testResponseTime = document.getElementById("test-response-time");
    const testResponseBody = document.getElementById("test-response-body");
    const responseEmptyState = document.getElementById("response-empty-state");
    const responseErrorState = document.getElementById("response-error-state");
    const responseErrorMessage = document.getElementById("response-error-message");
    const runTestButton = document.getElementById("run-test-button");
    const resetTestButton = document.getElementById("reset-test-button");

    const apiBaseUrl = new URL("../mock", window.location.href);
    const collectionsApiUrl = new URL("../mock/collections", window.location.href);
    const versionApiUrl = new URL("../api/version", window.location.href);
    const updateApiUrl = new URL("../api/update", window.location.href);
    const themeStorageKey = "localmock-theme";
    const activeCollectionStorageKey = "localmock-active-collection";
    const activeScreenStorageKey = "localmock-active-screen";
    const updateDismissedVersionKey = "localmock-dismissed-update-version";
    const updateCheckIntervalMs = 10 * 60 * 1000;

    const updateBanner = document.getElementById("update-banner");
    const updateBannerText = document.getElementById("update-banner-text");
    const updateApplyButton = document.getElementById("update-apply-button");
    const updateDismissButton = document.getElementById("update-dismiss-button");
    const updateOverlay = document.getElementById("update-overlay");
    const updateOverlayMessage = document.getElementById("update-overlay-message");
    const checkUpdateButton = document.getElementById("check-update-button");
    const appVersionLabel = document.getElementById("app-version-label");

    const state = {
        mocks: [],
        collections: [],
        allMocks: [],
        activeCollection: localStorage.getItem(activeCollectionStorageKey) || "",
        activeScreen: localStorage.getItem(activeScreenStorageKey) || "mocks",
        editingKey: null,
        editingCollectionId: null,
        collectionFormMode: "create",
        formDirty: false,
        lastUpdated: {}
    };

    const HTTP_STATUS_LABELS = {
        200: "OK",
        201: "Created",
        202: "Accepted",
        204: "No Content",
        400: "Bad Request",
        401: "Unauthorized",
        403: "Forbidden",
        404: "Not Found",
        409: "Conflict",
        422: "Unprocessable Entity",
        500: "Internal Server Error",
        502: "Bad Gateway",
        503: "Service Unavailable"
    };

    function getCurrentTheme() {
        return document.documentElement.getAttribute("data-theme") === "light" ? "light" : "dark";
    }

    function applyTheme(theme) {
        const normalizedTheme = theme === "light" ? "light" : "dark";
        document.documentElement.setAttribute("data-theme", normalizedTheme);
        themeToggleButton.setAttribute("title", normalizedTheme === "dark" ? "Switch to light theme" : "Switch to dark theme");
        themeToggleButton.setAttribute("aria-label", normalizedTheme === "dark" ? "Switch to light theme" : "Switch to dark theme");
    }

    function toggleTheme() {
        const nextTheme = getCurrentTheme() === "dark" ? "light" : "dark";
        localStorage.setItem(themeStorageKey, nextTheme);
        applyTheme(nextTheme);
    }

    function toKey(method, path, collection) {
        const collectionKey = getEntryCollection({ collection: collection }) || "";
        return collectionKey + "::" + method.toUpperCase() + "::" + path;
    }

    function getEntryCollection(entry) {
        const value = entry.collection ?? entry.Collection ?? "";
        const trimmed = String(value || "").trim();
        return trimmed || null;
    }

    function getActiveCollectionId() {
        return state.activeCollection || null;
    }

    function isStandaloneMode() {
        return !getActiveCollectionId();
    }

    function normalizeEntry(entry) {
        return {
            collection: getEntryCollection(entry),
            method: (entry.method || entry.Method || "GET").toUpperCase(),
            path: normalizePath(entry.path || entry.Path || "/"),
            statusCode: Number(entry.statusCode ?? entry.StatusCode ?? 200),
            responseDelayMs: getResponseDelayMs(entry),
            responseContentType: getEntryResponseContentType(entry),
            responseBody: entry.responseBody ?? entry.ResponseBody ?? null,
            enabled: getMockEnabled(entry),
            bypassEnabled: getBypassEnabled(entry),
            bypassUrl: getBypassUrl(entry)
        };
    }

    function getMockEnabled(entry) {
        if (entry.enabled !== undefined) {
            return Boolean(entry.enabled);
        }
        if (entry.Enabled !== undefined) {
            return Boolean(entry.Enabled);
        }
        return true;
    }

    function getResponseDelayMs(entry) {
        const value = Number(entry.responseDelayMs ?? entry.ResponseDelayMs ?? 0);
        return Number.isFinite(value) && value > 0 ? Math.floor(value) : 0;
    }

    function normalizePath(path) {
        const trimmed = (path || "").trim();
        if (!trimmed) {
            return "/";
        }
        return trimmed.startsWith("/") ? trimmed : "/" + trimmed;
    }

    function normalizeBypassUrl(url) {
        const trimmed = (url || "").trim();
        return trimmed || null;
    }

    function getBypassEnabled(entry) {
        return Boolean(entry.bypassEnabled ?? entry.BypassEnabled ?? false);
    }

    function getBypassUrl(entry) {
        return normalizeBypassUrl(entry.bypassUrl ?? entry.BypassUrl);
    }

    function isValidBypassUrl(url) {
        try {
            const parsedUrl = new URL(url);
            return parsedUrl.protocol === "http:" || parsedUrl.protocol === "https:";
        } catch (error) {
            return false;
        }
    }

    function formatJson(value) {
        if (value === null || value === undefined) {
            return "";
        }
        return JSON.stringify(value, null, 2);
    }

    function formatJsonString(text) {
        return JSON.stringify(JSON.parse(String(text).trim()), null, 2);
    }

    function tryAutoFormatJson(textarea, gutter) {
        const text = textarea.value.trim();
        if (!text) {
            return true;
        }

        try {
            textarea.value = formatJsonString(text);
            if (gutter) {
                updateGutter(textarea, gutter);
            }
            return true;
        } catch (error) {
            return false;
        }
    }

    function getEntryResponseContentType(entry) {
        return (entry.responseContentType || entry.ResponseContentType || "application/json").toLowerCase();
    }

    function normalizeFormUrlEncodedBody(text) {
        const trimmed = text.trim();
        if (!trimmed) {
            return "";
        }

        if (trimmed.indexOf("\n") < 0) {
            return trimmed;
        }

        const params = new URLSearchParams();
        parseKeyValueBody(trimmed, "application/x-www-form-urlencoded").forEach(function (entry) {
            params.append(entry[0], entry[1]);
        });
        return params.toString();
    }

    function formatFormBodyForEditor(text) {
        if (!text) {
            return "";
        }

        try {
            const params = new URLSearchParams(text);
            const lines = [];
            params.forEach(function (value, key) {
                lines.push(key + "=" + value);
            });
            if (lines.length) {
                return lines.join("\n");
            }
        } catch (error) {
            return text;
        }

        return text;
    }

    function parseResponseBodyForSave(text, contentType) {
        if (!text) {
            return null;
        }

        const normalizedType = (contentType || "application/json").toLowerCase();
        if (normalizedType === "application/json") {
            return JSON.parse(text);
        }

        if (normalizedType === "application/x-www-form-urlencoded") {
            return normalizeFormUrlEncodedBody(text);
        }

        return text;
    }

    function setResponseBodyFieldValue(textarea, gutter, entry) {
        const contentType = getEntryResponseContentType(entry);
        const body = entry.responseBody ?? entry.ResponseBody ?? null;

        if (body === null || body === undefined || body === "") {
            textarea.value = "";
        } else if (contentType === "application/json") {
            if (typeof body === "string") {
                try {
                    textarea.value = formatJsonString(body);
                } catch (error) {
                    textarea.value = body;
                }
            } else {
                textarea.value = formatJson(body);
            }
        } else if (contentType === "application/x-www-form-urlencoded") {
            const raw = typeof body === "string" ? body : JSON.stringify(body);
            textarea.value = formatFormBodyForEditor(raw);
        } else {
            textarea.value = typeof body === "string" ? body : JSON.stringify(body);
        }

        if (gutter) {
            updateGutter(textarea, gutter);
        }
    }

    function updateResponseBodyEditor() {
        const contentType = responseContentTypeField.value;
        responseBodyTypeLabel.textContent = "(" + contentType + ")";

        if (contentType === "application/json") {
            responseBodyField.placeholder = responseBodyField.getAttribute("data-placeholder-json") || "";
            return;
        }

        if (contentType === "application/x-www-form-urlencoded") {
            responseBodyField.placeholder = responseBodyField.getAttribute("data-placeholder-form") || "field=value";
            return;
        }

        responseBodyField.placeholder = "Response content";
    }

    function bindJsonEditor(textarea, gutter, options) {
        const onlyWhenJson = Boolean(options && options.onlyWhenJson);
        const markDirty = Boolean(options && options.markDirty);
        const whenJson = options && options.whenJson;

        function shouldFormat() {
            if (whenJson && !whenJson()) {
                return false;
            }
            if (onlyWhenJson && testContentTypeField.value !== "application/json") {
                return false;
            }
            return true;
        }

        function runFormat() {
            if (!shouldFormat()) {
                return;
            }
            tryAutoFormatJson(textarea, gutter);
        }

        textarea.addEventListener("input", function () {
            updateGutter(textarea, gutter);
            if (markDirty) {
                setFormDirty(true);
            }
        });

        textarea.addEventListener("paste", function () {
            setTimeout(function () {
                updateGutter(textarea, gutter);
            }, 0);
        });

        textarea.addEventListener("scroll", function () {
            syncGutterScroll(textarea, gutter);
        });

        textarea.addEventListener("blur", runFormat);
    }

    function parseStatusCodeValue() {
        const raw = statusCodeField.value.trim();
        if (!raw) {
            return null;
        }

        const code = Number(raw);
        if (!Number.isInteger(code) || code < 100 || code > 599) {
            return NaN;
        }

        return code;
    }

    function parseResponseDelayMsValue() {
        const raw = responseDelayMsField.value.trim();
        if (!raw) {
            return 0;
        }

        const value = Number(raw);
        if (!Number.isInteger(value) || value < 0) {
            return NaN;
        }

        return value;
    }

    function getStatusLabel(code) {
        const label = HTTP_STATUS_LABELS[code];
        return label ? code + " " + label : String(code);
    }

    function getStatusClass(code) {
        if (code >= 200 && code < 300) return "status-badge--2xx";
        if (code >= 300 && code < 400) return "status-badge--3xx";
        if (code >= 400 && code < 500) return "status-badge--4xx";
        if (code >= 500) return "status-badge--5xx";
        return "status-badge--2xx";
    }

    function getMethodClass(method) {
        return "method-badge--" + (method || "GET").toLowerCase();
    }

    function updateMethodBadges() {
        const method = methodField.value.toUpperCase();
        const cls = getMethodClass(method);
        previewMethodBadge.textContent = method;
        previewMethodBadge.className = "method-badge " + cls;
    }

    function updateGutter(textarea, gutter) {
        const lines = (textarea.value || "").split("\n").length || 1;
        const numbers = [];
        for (let i = 1; i <= lines; i += 1) {
            numbers.push(i);
        }
        gutter.textContent = numbers.join("\n");
        syncGutterScroll(textarea, gutter);
    }

    function syncGutterScroll(textarea, gutter) {
        if (!gutter) {
            return;
        }

        gutter.scrollTop = textarea.scrollTop;
    }

    function setFormDirty(dirty) {
        state.formDirty = dirty;
    }

    function updateBypassFieldState() {
        const enabled = bypassEnabledField.checked;
        bypassUrlField.disabled = !enabled;
        bypassUrlFieldWrapper.classList.toggle("is-disabled", !enabled);
    }

    function updateCollectionUiState() {
        const bypassGroup = document.querySelector(".field-group--bypass");
        const enabledGroup = document.querySelector(".field-group--enabled");
        if (bypassGroup) {
            bypassGroup.hidden = !isStandaloneMode();
        }
        if (enabledGroup) {
            enabledGroup.hidden = isStandaloneMode();
        }
    }

    function toFileEntry(entry) {
        const normalized = normalizeEntry(entry);
        const fileEntry = {
            Method: normalized.method,
            Path: normalized.path,
            StatusCode: normalized.statusCode,
            ResponseDelayMs: normalized.responseDelayMs,
            ResponseContentType: normalized.responseContentType,
            ResponseBody: normalized.responseBody,
            Enabled: normalized.enabled,
            BypassEnabled: normalized.bypassEnabled,
            BypassUrl: normalized.bypassUrl
        };
        if (normalized.collection) {
            fileEntry.Collection = normalized.collection;
        }
        return fileEntry;
    }

    function getMocksFileContent() {
        return JSON.stringify({
            Collections: state.collections.map(function (collection) {
                return {
                    Id: collection.id || collection.Id,
                    BypassUrl: collection.bypassUrl || collection.BypassUrl || ""
                };
            }),
            Mocks: state.mocks.map(toFileEntry)
        }, null, 2);
    }

    function renderCollectionSelect() {
        const previous = collectionSelectField.value;
        collectionSelectField.innerHTML = "";

        const standaloneOption = document.createElement("option");
        standaloneOption.value = "";
        standaloneOption.textContent = "No collection";
        collectionSelectField.appendChild(standaloneOption);

        state.collections.forEach(function (collection) {
            const id = collection.id || collection.Id;
            const option = document.createElement("option");
            option.value = id;
            option.textContent = id;
            collectionSelectField.appendChild(option);
        });

        const hasPrevious = Array.from(collectionSelectField.options).some(function (option) {
            return option.value === previous;
        });
        if (hasPrevious) {
            collectionSelectField.value = previous;
        } else if (state.activeCollection && Array.from(collectionSelectField.options).some(function (option) {
            return option.value === state.activeCollection;
        })) {
            collectionSelectField.value = state.activeCollection;
        } else if (state.collections.length) {
            collectionSelectField.value = state.collections[0].id || state.collections[0].Id;
        } else {
            collectionSelectField.value = "";
        }

        state.activeCollection = collectionSelectField.value;
        localStorage.setItem(activeCollectionStorageKey, state.activeCollection);
        updateCollectionUiState();
    }

    function setActiveCollection(collectionId, options) {
        state.activeCollection = collectionId || "";
        localStorage.setItem(activeCollectionStorageKey, state.activeCollection);
        if (collectionSelectField.value !== state.activeCollection) {
            collectionSelectField.value = state.activeCollection;
        }
        const skipReload = options && options.skipReload;
        if (!skipReload) {
            loadMocks();
        }
        updateEndpointPreview();
        updateTestEndpointPreview();
        updateCollectionUiState();
        renderMockList();
    }

    function getCollectionId(collection) {
        return collection.id || collection.Id || "";
    }

    function getCollectionBypassUrl(collection) {
        return collection.bypassUrl || collection.BypassUrl || "";
    }

    function countMocksForCollection(collectionId) {
        return state.allMocks.filter(function (entry) {
            return getEntryCollection(entry) === collectionId;
        }).length;
    }

    async function loadAllMocksForCounts() {
        try {
            const response = await fetch(apiBaseUrl, { headers: { Accept: "application/json" } });
            if (!response.ok) {
                return;
            }
            state.allMocks = (await response.json()).map(normalizeEntry);
        } catch (error) {
            state.allMocks = [];
        }
    }

    async function loadCollections() {
        try {
            const response = await fetch(collectionsApiUrl, {
                headers: { Accept: "application/json" }
            });
            if (!response.ok) {
                await handleResponseError(response);
            }
            state.collections = await response.json();
            await loadAllMocksForCounts();
            renderCollectionSelect();
            renderCollectionsList();
        } catch (error) {
            const message = "Failed to load collections: " + error.message;
            setFeedback(listFeedback, "error", message, { silent: true });
            setFeedback(collectionsListFeedback, "error", message);
        }
    }

    function updateTopbarPage(screenId) {
        const copy = TOPBAR_PAGE_COPY[screenId] || TOPBAR_PAGE_COPY.mocks;
        topbarPageEyebrow.textContent = copy.eyebrow;
        topbarPageTitle.textContent = copy.title;
        topbarPageSubtitle.textContent = copy.subtitle;
    }

    function switchScreen(screenId) {
        const isMocks = screenId === "mocks";
        state.activeScreen = isMocks ? "mocks" : "collections";
        localStorage.setItem(activeScreenStorageKey, state.activeScreen);

        screenMocks.classList.toggle("is-active", isMocks);
        screenMocks.hidden = !isMocks;
        screenCollections.classList.toggle("is-active", !isMocks);
        screenCollections.hidden = isMocks;

        navMocks.classList.toggle("is-active", isMocks);
        navMocks.setAttribute("aria-selected", isMocks ? "true" : "false");
        navCollections.classList.toggle("is-active", !isMocks);
        navCollections.setAttribute("aria-selected", !isMocks ? "true" : "false");

        updateTopbarPage(state.activeScreen);

        if (!isMocks) {
            loadCollections();
        }
    }

    function buildCollectionBaseUrl(collectionId) {
        if (!collectionId) {
            return "—";
        }
        return new URL("../mock/" + encodeURIComponent(collectionId) + "/", window.location.href).toString();
    }

    function updateCollectionUrlPreview() {
        const id = collectionFormIdField.value.trim();
        collectionUrlPreview.textContent = buildCollectionBaseUrl(id);
    }

    function resetCollectionForm() {
        state.editingCollectionId = null;
        state.collectionFormMode = "create";
        collectionForm.reset();
        collectionFormIdField.disabled = false;
        collectionFormIdFieldWrapper.classList.remove("is-disabled");
        deleteCollectionFormButton.hidden = true;
        openCollectionMocksButton.hidden = true;
        collectionMockCount.textContent = "—";
        collectionConfigSubtitle.textContent = "Fill in the details to create a new collection.";
        updateCollectionUrlPreview();
        setFeedback(collectionFormFeedback, "", "");
        renderCollectionsList();
    }

    function fillCollectionForm(collection) {
        const id = getCollectionId(collection);
        state.editingCollectionId = id;
        state.collectionFormMode = "edit";
        collectionFormIdField.value = id;
        collectionFormIdField.disabled = true;
        collectionFormIdFieldWrapper.classList.add("is-disabled");
        collectionFormBypassUrlField.value = getCollectionBypassUrl(collection);
        collectionMockCount.textContent = String(countMocksForCollection(id));
        collectionConfigSubtitle.textContent = "Editing collection " + id + ".";
        deleteCollectionFormButton.hidden = false;
        openCollectionMocksButton.hidden = false;
        updateCollectionUrlPreview();
        setFeedback(collectionFormFeedback, "", "");
        renderCollectionsList();
    }

    function getFilteredCollections() {
        const search = (collectionSearchField.value || "").trim().toLowerCase();
        return state.collections.filter(function (collection) {
            const id = getCollectionId(collection).toLowerCase();
            return !search || id.indexOf(search) >= 0;
        });
    }

    function renderCollectionsList() {
        const filtered = getFilteredCollections();
        collectionsList.innerHTML = "";

        if (!filtered.length) {
            const empty = document.createElement("div");
            empty.className = "mock-list-empty";
            empty.textContent = state.collections.length
                ? "No collections match the search."
                : "No collections registered. Use + to create one.";
            collectionsList.appendChild(empty);
            return;
        }

        filtered.slice().sort(function (left, right) {
            return getCollectionId(left).localeCompare(getCollectionId(right));
        }).forEach(function (collection) {
            const id = getCollectionId(collection);
            const mockCount = countMocksForCollection(id);
            const item = document.createElement("article");
            item.className = "mock-list-item";
            item.setAttribute("role", "listitem");
            item.tabIndex = 0;

            if (state.editingCollectionId === id) {
                item.classList.add("is-active");
            }

            const head = document.createElement("div");
            head.className = "mock-list-item-head";
            head.innerHTML =
                '<span class="method-badge method-badge--patch">⊞</span>' +
                '<span class="mock-list-item-path">' + escapeHtml(id) + "</span>";

            const meta = document.createElement("div");
            meta.className = "mock-list-item-meta";
            meta.innerHTML =
                '<span class="bypass-pill bypass-pill--yes">' + mockCount + " mock(s)</span>";

            item.appendChild(head);
            item.appendChild(meta);

            item.addEventListener("click", function () {
                fillCollectionForm(collection);
            });
            item.addEventListener("keydown", function (event) {
                if (event.key === "Enter" || event.key === " ") {
                    event.preventDefault();
                    fillCollectionForm(collection);
                }
            });

            collectionsList.appendChild(item);
        });
    }

    async function saveCollectionFromForm(event) {
        if (event) {
            event.preventDefault();
        }

        const id = collectionFormIdField.value.trim();
        const bypassUrl = normalizeBypassUrl(collectionFormBypassUrlField.value);

        if (!id && state.collectionFormMode === "create") {
            setFeedback(collectionFormFeedback, "error", "Enter the collection id.");
            collectionFormIdField.focus();
            return;
        }

        if (!isValidBypassUrl(bypassUrl)) {
            setFeedback(collectionFormFeedback, "error", "Enter a valid bypass URL (HTTP/HTTPS).");
            collectionFormBypassUrlField.focus();
            return;
        }

        saveCollectionFormButton.disabled = true;
        setFeedback(collectionFormFeedback, "", "");

        try {
            if (state.collectionFormMode === "create") {
                const response = await fetch(collectionsApiUrl, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ id: id, bypassUrl: bypassUrl })
                });
                if (!response.ok) {
                    await handleResponseError(response);
                }
                fillCollectionForm({ id: id, bypassUrl: bypassUrl });
            } else {
                const url = new URL(collectionsApiUrl.href + "/" + encodeURIComponent(id));
                const response = await fetch(url, {
                    method: "PATCH",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ bypassUrl: bypassUrl })
                });
                if (!response.ok) {
                    await handleResponseError(response);
                }
            }

            await loadCollections();
            if (state.collectionFormMode === "edit") {
                fillCollectionForm({ id: id, bypassUrl: bypassUrl });
            }
            setFeedback(collectionFormFeedback, "success", "Collection saved.");
            setFeedback(collectionsListFeedback, "success", "Collection saved.", { silent: true });
        } catch (error) {
            setFeedback(collectionFormFeedback, "error", error.message);
        } finally {
            saveCollectionFormButton.disabled = false;
        }
    }

    async function deleteCollectionFromForm() {
        const id = collectionFormIdField.value.trim();
        if (!id) {
            return;
        }
        const mockCount = countMocksForCollection(id);
        const confirmMessage = mockCount > 0
            ? "Delete collection " + id + " and its " + mockCount + " associated mock(s)? This action cannot be undone."
            : "Delete collection " + id + "? This action cannot be undone.";
        if (!window.confirm(confirmMessage)) {
            return;
        }

        deleteCollectionFormButton.disabled = true;
        try {
            const url = new URL(collectionsApiUrl.href + "/" + encodeURIComponent(id));
            const response = await fetch(url, { method: "DELETE" });
            if (!response.ok) {
                await handleResponseError(response);
            }
            if (state.activeCollection === id) {
                setActiveCollection("", { skipReload: true });
                resetForm();
            }
            resetCollectionForm();
            await loadCollections();
            await loadMocks();
            setFeedback(
                collectionsListFeedback,
                "success",
                mockCount > 0
                    ? "Collection and " + mockCount + " mock(s) removed."
                    : "Collection removed."
            );
        } catch (error) {
            setFeedback(collectionFormFeedback, "error", error.message);
        } finally {
            deleteCollectionFormButton.disabled = false;
        }
    }

    function openMocksForCurrentCollection() {
        const id = collectionFormIdField.value.trim();
        if (!id) {
            return;
        }
        setActiveCollection(id);
        switchScreen("mocks");
    }

    function readFileAsText(file) {
        return new Promise(function (resolve, reject) {
            const reader = new FileReader();
            reader.onload = function () {
                resolve(typeof reader.result === "string" ? reader.result : "");
            };
            reader.onerror = function () {
                reject(reader.error || new Error("could not read the selected file"));
            };
            reader.readAsText(file);
        });
    }

    async function writeToClipboard(text) {
        if (navigator.clipboard && window.isSecureContext) {
            await navigator.clipboard.writeText(text);
            return;
        }
        const helper = document.createElement("textarea");
        helper.value = text;
        helper.setAttribute("readonly", "");
        helper.style.position = "absolute";
        helper.style.left = "-9999px";
        document.body.appendChild(helper);
        helper.select();
        const copied = document.execCommand("copy");
        document.body.removeChild(helper);
        if (!copied) {
            throw new Error("the browser did not allow automatic copy");
        }
    }

    function showToast(type, message) {
        const toast = document.createElement("div");
        const title = document.createElement("div");
        const content = document.createElement("div");
        toast.className = "toast toast-" + type;
        title.className = "toast-title";
        content.className = "toast-message";
        title.textContent = type === "success" ? "Success" : "Failure";
        content.textContent = message;
        toast.appendChild(title);
        toast.appendChild(content);
        toastContainer.appendChild(toast);
        requestAnimationFrame(function () {
            toast.classList.add("is-visible");
        });
        window.setTimeout(function () {
            toast.classList.remove("is-visible");
            toast.classList.add("is-leaving");
            window.setTimeout(function () {
                toast.remove();
            }, 220);
        }, 4200);
    }

    function setFeedback(element, type, message, options) {
        const silent = Boolean(options && options.silent);
        const baseClass = element.classList.contains("sidebar-feedback") ? "sidebar-feedback" : "feedback";
        element.className = baseClass;
        element.textContent = message || "";
        if (type) {
            element.classList.add(type);
        }
        if (!silent && (type === "success" || type === "error")) {
            showToast(type, message);
        }
    }

    function buildMockUrl(path) {
        const collectionId = getActiveCollectionId();
        const normalizedPath = normalizePath(path);
        if (collectionId) {
            return new URL("../mock/" + encodeURIComponent(collectionId) + normalizedPath, window.location.href);
        }
        return new URL("../mock" + normalizedPath, window.location.href);
    }

    function updateEndpointPreview() {
        const method = methodField.value.toUpperCase();
        const path = normalizePath(pathField.value);
        const url = buildMockUrl(path);
        endpointPreview.textContent = url.toString();
        updateMethodBadges();
    }

    function updateTestEndpointPreview() {
        const path = normalizePath(testPathField.value);
        testEndpointPreview.textContent = buildMockUrl(path).toString();
    }

    function updateTestBodyPlaceholder() {
        const contentType = testContentTypeField.value;
        if (contentType === "application/json") {
            testRequestBodyField.placeholder = '{\n  "id": 1\n}';
            return;
        }
        if (contentType === "application/x-www-form-urlencoded") {
            testRequestBodyField.placeholder = "customerId=1\nname=Maria";
            return;
        }
        if (contentType === "multipart/form-data") {
            testRequestBodyField.placeholder = "field=value";
            return;
        }
        if (contentType.indexOf("xml") >= 0) {
            testRequestBodyField.placeholder = "<request>\n  <id>1</id>\n</request>";
            return;
        }
        testRequestBodyField.placeholder = "Request body";
    }

    function openCurlModal() {
        curlModal.hidden = false;
        testCurlField.focus();
    }

    function closeCurlModal() {
        curlModal.hidden = true;
    }

    function tokenizeCurlCommand(command) {
        const tokens = [];
        let current = "";
        let quote = null;
        let escaped = false;

        for (let index = 0; index < command.length; index += 1) {
            const character = command[index];
            if (escaped) {
                current += character;
                escaped = false;
                continue;
            }
            if (character === "\\") {
                escaped = true;
                continue;
            }
            if (quote) {
                if (character === quote) {
                    quote = null;
                } else {
                    current += character;
                }
                continue;
            }
            if (character === "'" || character === "\"") {
                quote = character;
                continue;
            }
            if (/\s/.test(character)) {
                if (current) {
                    tokens.push(current);
                    current = "";
                }
                continue;
            }
            current += character;
        }

        if (escaped) {
            current += "\\";
        }
        if (quote) {
            throw new Error("the cURL has an unclosed quote.");
        }
        if (current) {
            tokens.push(current);
        }
        return tokens;
    }

    function normalizeCurlCommand(command) {
        return command
            .replace(/\\\r?\n/g, " ")
            .replace(/\^\r?\n/g, " ")
            .trim();
    }

    function splitHeader(header) {
        const separatorIndex = header.indexOf(":");
        if (separatorIndex <= 0) {
            throw new Error("invalid header in cURL: " + header);
        }
        return {
            name: header.slice(0, separatorIndex).trim(),
            value: header.slice(separatorIndex + 1).trim()
        };
    }

    function formatHeadersForField(headers) {
        return headers.map(function (header) {
            return header.name + ": " + header.value;
        }).join("\n");
    }

    function normalizeCurlFormValue(value) {
        const separatorIndex = value.indexOf("=");
        if (separatorIndex <= 0) {
            return value;
        }
        const name = value.slice(0, separatorIndex).trim();
        const fieldValue = value.slice(separatorIndex + 1).trim();
        if (fieldValue.length >= 2 &&
            ((fieldValue.startsWith("\"") && fieldValue.endsWith("\"")) ||
                (fieldValue.startsWith("'") && fieldValue.endsWith("'")))) {
            return name + "=" + fieldValue.slice(1, -1);
        }
        return name + "=" + fieldValue;
    }

    function findContentType(headers) {
        const contentTypeHeader = headers.find(function (header) {
            return header.name.toLowerCase() === "content-type";
        });
        if (!contentTypeHeader) {
            return null;
        }
        return contentTypeHeader.value.split(";")[0].trim().toLowerCase();
    }

    function extractUrlFromCurlTokens(tokens) {
        const httpMethods = ["GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS"];
        for (let index = 1; index < tokens.length; index += 1) {
            const token = tokens[index];
            if (token.startsWith("-")) {
                continue;
            }
            try {
                const parsed = new URL(token);
                return parsed;
            } catch (error) {
                if (token.startsWith("http://") || token.startsWith("https://")) {
                    try {
                        return new URL(token);
                    } catch (innerError) {
                        continue;
                    }
                }
            }
        }
        return null;
    }

    function applyCurlToTestForm(parsedCurl) {
        if (parsedCurl.method) {
            const method = parsedCurl.method.toUpperCase();
            if (Array.from(testMethodField.options).some(function (opt) { return opt.value === method; })) {
                testMethodField.value = method;
            }
        }

        if (parsedCurl.url) {
            const mockBase = new URL("../mock", window.location.href);
            const mockPathPrefix = mockBase.pathname.replace(/\/$/, "");
            let path = parsedCurl.url.pathname || "/";
            if (parsedCurl.url.origin === mockBase.origin && parsedCurl.url.pathname.startsWith(mockPathPrefix)) {
                path = parsedCurl.url.pathname.slice(mockPathPrefix.length) || "/";
            }
            testPathField.value = normalizePath(path);
        }

        const contentType = findContentType(parsedCurl.headers) || parsedCurl.inferredContentType;
        const knownContentTypes = Array.from(testContentTypeField.options).map(function (option) {
            return option.value;
        });
        if (contentType && knownContentTypes.indexOf(contentType) >= 0) {
            testContentTypeField.value = contentType;
        }

        const headersWithoutContentType = parsedCurl.headers.filter(function (header) {
            return header.name.toLowerCase() !== "content-type";
        });
        if (headersWithoutContentType.length) {
            testRequestHeadersField.value = formatHeadersForField(headersWithoutContentType);
        }
        if (parsedCurl.body !== null) {
            testRequestBodyField.value = parsedCurl.body;
        }

        updateTestBodyPlaceholder();
        updateTestEndpointPreview();
        updateGutter(testRequestBodyField, testRequestBodyGutter);
    }

    function parseCurlCommand(command) {
        const tokens = tokenizeCurlCommand(normalizeCurlCommand(command));
        if (!tokens.length || tokens[0].toLowerCase() !== "curl") {
            throw new Error("enter a command that starts with curl.");
        }

        const parsed = {
            headers: [],
            body: null,
            inferredContentType: null,
            method: null,
            url: null
        };

        const appendBody = function (value, separator) {
            parsed.body = parsed.body === null ? value : parsed.body + separator + value;
        };

        for (let index = 1; index < tokens.length; index += 1) {
            const token = tokens[index];
            const nextToken = tokens[index + 1];
            const readValue = function (optionName) {
                if (nextToken === undefined) {
                    throw new Error("option " + optionName + " has no value.");
                }
                index += 1;
                return nextToken;
            };

            if (token === "-X" || token === "--request") {
                parsed.method = readValue(token).toUpperCase();
                continue;
            }

            if (token.startsWith("-X") && token.length > 2) {
                parsed.method = token.slice(2).toUpperCase();
                continue;
            }

            if (token.startsWith("--request=")) {
                parsed.method = token.slice("--request=".length).toUpperCase();
                continue;
            }

            if (token === "-H" || token === "--header") {
                parsed.headers.push(splitHeader(readValue(token)));
                continue;
            }

            if (token.startsWith("-H") && token.length > 2) {
                parsed.headers.push(splitHeader(token.slice(2)));
                continue;
            }

            if (token.startsWith("--header=")) {
                parsed.headers.push(splitHeader(token.slice("--header=".length)));
                continue;
            }

            if (token === "-d" || token === "--data" || token === "--data-raw" || token === "--data-binary") {
                appendBody(readValue(token), "&");
                parsed.inferredContentType = parsed.inferredContentType || "application/x-www-form-urlencoded";
                if (!parsed.method) parsed.method = "POST";
                continue;
            }

            if (token === "--data-urlencode") {
                appendBody(readValue(token), "&");
                parsed.inferredContentType = parsed.inferredContentType || "application/x-www-form-urlencoded";
                if (!parsed.method) parsed.method = "POST";
                continue;
            }

            if (token === "--form" || token === "--form-string" || token === "-F") {
                appendBody(normalizeCurlFormValue(readValue(token)), "\n");
                parsed.inferredContentType = parsed.inferredContentType || "multipart/form-data";
                if (!parsed.method) parsed.method = "POST";
                continue;
            }

            if (token.startsWith("--data=")) {
                appendBody(token.slice("--data=".length), "&");
                parsed.inferredContentType = parsed.inferredContentType || "application/x-www-form-urlencoded";
                if (!parsed.method) parsed.method = "POST";
                continue;
            }

            if (token.startsWith("--data-raw=")) {
                appendBody(token.slice("--data-raw=".length), "&");
                parsed.inferredContentType = parsed.inferredContentType || "application/json";
                if (!parsed.method) parsed.method = "POST";
                continue;
            }

            if (token.startsWith("--data-binary=")) {
                appendBody(token.slice("--data-binary=".length), "&");
                if (!parsed.method) parsed.method = "POST";
                continue;
            }

            if (token.startsWith("--data-urlencode=")) {
                appendBody(token.slice("--data-urlencode=".length), "&");
                parsed.inferredContentType = parsed.inferredContentType || "application/x-www-form-urlencoded";
                if (!parsed.method) parsed.method = "POST";
                continue;
            }

            if (token.startsWith("--form=")) {
                appendBody(normalizeCurlFormValue(token.slice("--form=".length)), "\n");
                parsed.inferredContentType = parsed.inferredContentType || "multipart/form-data";
                if (!parsed.method) parsed.method = "POST";
                continue;
            }

            if (token.startsWith("--form-string=")) {
                appendBody(normalizeCurlFormValue(token.slice("--form-string=".length)), "\n");
                parsed.inferredContentType = parsed.inferredContentType || "multipart/form-data";
                if (!parsed.method) parsed.method = "POST";
                continue;
            }
        }

        parsed.url = extractUrlFromCurlTokens(tokens);
        if (!parsed.method && parsed.body) {
            parsed.method = "POST";
        }
        if (!parsed.method) {
            parsed.method = "GET";
        }

        return parsed;
    }

    function importCurlToTest() {
        const command = testCurlField.value.trim();
        if (!command) {
            setFeedback(testFeedback, "error", "Paste a cURL command to import.");
            testCurlField.focus();
            return;
        }
        try {
            const parsedCurl = parseCurlCommand(command);
            applyCurlToTestForm(parsedCurl);
            closeCurlModal();
            setFeedback(testFeedback, "success", "cURL imported successfully.");
        } catch (error) {
            setFeedback(testFeedback, "error", "Failed to import cURL: " + error.message);
            testCurlField.focus();
        }
    }

    function showResponseEmpty() {
        responseEmptyState.hidden = false;
        responseErrorState.hidden = true;
        testResponseBody.hidden = true;
    }

    function showResponseError(message) {
        responseEmptyState.hidden = true;
        responseErrorState.hidden = false;
        testResponseBody.hidden = true;
        responseErrorMessage.textContent = message;
    }

    function showResponseBody(text) {
        responseEmptyState.hidden = true;
        responseErrorState.hidden = true;
        testResponseBody.hidden = false;
        testResponseBody.textContent = text;
    }

    function resetResponsePanel() {
        testResponseStatus.textContent = "—";
        testResponseContentType.textContent = "—";
        testResponseTime.textContent = "—";
        testResponseStatus.className = "stat-card-value";
        testResponseContentType.className = "stat-card-value";
        showResponseEmpty();
    }

    function resetTestForm(options) {
        const keepMethod = options && options.keepMethod;
        testForm.reset();
        testMethodField.value = keepMethod ? testMethodField.value : "GET";
        testPathField.value = "";
        testCurlField.value = "";
        testContentTypeField.value = "application/json";
        testRequestHeadersField.value = "";
        testRequestBodyField.value = "";
        setFeedback(testFeedback, "", "");
        resetResponsePanel();
        updateTestBodyPlaceholder();
        updateTestEndpointPreview();
        updateGutter(testRequestBodyField, testRequestBodyGutter);
    }

    function populateTestForm(entry, options) {
        const shouldScroll = !options || options.scroll !== false;
        testMethodField.value = (entry.method || "GET").toUpperCase();
        testPathField.value = entry.path || "";
        testCurlField.value = "";
        testContentTypeField.value = "application/json";
        testRequestHeadersField.value = "";
        testRequestBodyField.value = "";
        setFeedback(testFeedback, "", "");
        resetResponsePanel();
        updateTestBodyPlaceholder();
        updateTestEndpointPreview();
        updateGutter(testRequestBodyField, testRequestBodyGutter);
        if (shouldScroll) {
            document.querySelector(".mock-test-panel").scrollIntoView({ behavior: "smooth", block: "start" });
        }
    }

    function copyCurrentFormToTest() {
        if (!pathField.value.trim()) {
            setFeedback(testFeedback, "error", "Fill in the path before copying to the test panel.");
            pathField.focus();
            return;
        }
        populateTestForm({
            method: methodField.value,
            path: normalizePath(pathField.value)
        }, { scroll: true });
    }

    function resetForm(options) {
        const keepBody = options && options.keepBody;
        const keepMethod = options && options.keepMethod;
        form.reset();
        methodField.value = keepMethod ? methodField.value : "GET";
        statusCodeField.value = "";
        responseDelayMsField.value = "";
        pathField.value = "";
        mockEnabledField.checked = true;
        bypassEnabledField.checked = false;
        bypassUrlField.value = "";
        responseContentTypeField.value = "application/json";
        updateResponseBodyEditor();
        if (!keepBody) {
            responseBodyField.value = "";
            updateGutter(responseBodyField, responseBodyGutter);
        }
        state.editingKey = null;
        formTitle.textContent = "New mock";
        setFeedback(formFeedback, "", "");
        setFormDirty(false);
        updateBypassFieldState();
        updateEndpointPreview();
        updateMethodBadges();
        renderMockList();
    }

    function fillForm(entry) {
        methodField.value = (entry.method || "GET").toUpperCase();
        pathField.value = entry.path || "";
        statusCodeField.value = entry.statusCode != null ? String(entry.statusCode) : "";
        responseDelayMsField.value = getResponseDelayMs(entry) ? String(getResponseDelayMs(entry)) : "";
        mockEnabledField.checked = getMockEnabled(entry);
        bypassEnabledField.checked = getBypassEnabled(entry);
        bypassUrlField.value = getBypassUrl(entry) || "";
        responseContentTypeField.value = getEntryResponseContentType(entry);
        updateResponseBodyEditor();
        setResponseBodyFieldValue(responseBodyField, responseBodyGutter, entry);
        state.editingKey = toKey(methodField.value, normalizePath(pathField.value), getEntryCollection(entry));
        formTitle.textContent = "Editing mock";
        setFormDirty(false);
        updateBypassFieldState();
        updateEndpointPreview();
        populateTestForm(entry, { scroll: false });
        renderMockList();
    }

    function formatDateTime(date) {
        if (!date) return "—";
        const d = date instanceof Date ? date : new Date(date);
        if (Number.isNaN(d.getTime())) return "—";
        const pad = function (n) { return String(n).padStart(2, "0"); };
        return pad(d.getDate()) + "/" + pad(d.getMonth() + 1) + "/" + d.getFullYear() +
            " " + pad(d.getHours()) + ":" + pad(d.getMinutes());
    }

    function markMockUpdated(method, path) {
        state.lastUpdated[toKey(method, path)] = new Date();
    }

    function getFilteredMocks() {
        const search = (mockSearchField.value || "").trim().toLowerCase();
        const methodFilter = filterMethodField.value;
        const statusFilter = filterStatusField.value;

        const activeCollection = getActiveCollectionId();

        return state.mocks.filter(function (entry) {
            const normalized = normalizeEntry(entry);
            const method = normalized.method;
            const path = normalized.path.toLowerCase();
            const code = Number(normalized.statusCode || 200);
            const entryCollection = normalized.collection;

            if (activeCollection) {
                if (entryCollection !== activeCollection) {
                    return false;
                }
            } else if (entryCollection) {
                return false;
            }

            if (methodFilter && method !== methodFilter) {
                return false;
            }
            if (statusFilter) {
                const family = statusFilter.charAt(0);
                const entryFamily = String(Math.floor(code / 100));
                if (entryFamily !== family) {
                    return false;
                }
            }
            if (search && path.indexOf(search) < 0 && method.toLowerCase().indexOf(search) < 0) {
                return false;
            }
            return true;
        });
    }

    function renderMockList() {
        const filtered = getFilteredMocks();
        mockList.innerHTML = "";

        if (!filtered.length) {
            const empty = document.createElement("div");
            empty.className = "mock-list-empty";
            empty.textContent = state.mocks.length
                ? "No mocks match the filters."
                : "No mocks registered. Use + to create one.";
            mockList.appendChild(empty);
            return;
        }

        const sorted = filtered.slice().sort(function (left, right) {
            const byPath = (left.path || "").localeCompare(right.path || "");
            if (byPath !== 0) return byPath;
            return (left.method || "").localeCompare(right.method || "");
        });

        sorted.forEach(function (entry) {
            const normalized = normalizeEntry(entry);
            const method = normalized.method;
            const path = normalized.path;
            const code = Number(normalized.statusCode || 200);
            const bypassEnabled = normalized.bypassEnabled;
            const mockEnabled = normalized.enabled;
            const key = toKey(method, path, normalized.collection);

            const item = document.createElement("article");
            item.className = "mock-list-item";
            item.setAttribute("role", "listitem");
            item.tabIndex = 0;

            if (state.editingKey === key) {
                item.classList.add("is-active");
            }

            const head = document.createElement("div");
            head.className = "mock-list-item-head";
            head.innerHTML =
                '<span class="method-badge ' + getMethodClass(method) + '">' + method + "</span>" +
                '<span class="mock-list-item-path">' + escapeHtml(path) + "</span>";

            const meta = document.createElement("div");
            meta.className = "mock-list-item-meta";
            meta.innerHTML =
                '<span class="status-badge ' + getStatusClass(code) + '">' + escapeHtml(getStatusLabel(code)) + "</span>" +
                (bypassEnabled ? '<span class="bypass-pill bypass-pill--yes">Bypass</span>' : "") +
                (!isStandaloneMode() && !mockEnabled ? '<span class="bypass-pill bypass-pill--inactive">Inactive</span>' : "");

            const actions = document.createElement("div");
            actions.className = "mock-list-item-actions";
            actions.addEventListener("click", function (event) {
                event.stopPropagation();
            });

            actions.appendChild(createActionButton("Test", "▶", function () {
                populateTestForm(normalized);
                runTestCall();
            }));

            if (isStandaloneMode()) {
                const bypassLabel = bypassEnabled ? "Disable bypass" : "Enable bypass";
                const bypassBtn = createActionButton(bypassLabel, "⇄", function () {
                    toggleBypass(normalized);
                });
                bypassBtn.classList.toggle("is-bypass-on", bypassEnabled);
                actions.appendChild(bypassBtn);
            } else {
                const enabledLabel = mockEnabled ? "Disable mock" : "Enable mock";
                const enabledBtn = createActionButton(enabledLabel, "◉", function () {
                    toggleMockEnabled(normalized);
                });
                enabledBtn.classList.toggle("is-mock-on", mockEnabled);
                enabledBtn.classList.toggle("is-mock-off", !mockEnabled);
                actions.appendChild(enabledBtn);
            }

            actions.appendChild(createActionButton("Edit", "✎", function () {
                fillForm(normalized);
            }));
            actions.appendChild(createActionButton("Duplicate", "⧉", function () {
                duplicateMock(normalized);
            }));

            const deleteBtn = createActionButton("Delete", "✕", function () {
                deleteMock(normalized);
            });
            deleteBtn.classList.add("icon-button--danger");
            actions.appendChild(deleteBtn);

            item.appendChild(head);
            item.appendChild(meta);
            item.appendChild(actions);

            item.addEventListener("click", function () {
                fillForm(normalized);
            });

            item.addEventListener("keydown", function (event) {
                if (event.key === "Enter" || event.key === " ") {
                    event.preventDefault();
                    fillForm(normalized);
                }
            });

            mockList.appendChild(item);
        });
    }

    function escapeHtml(text) {
        const div = document.createElement("div");
        div.textContent = text;
        return div.innerHTML;
    }

    function createActionButton(title, icon, handler) {
        const btn = document.createElement("button");
        btn.type = "button";
        btn.className = "icon-button";
        btn.title = title;
        btn.setAttribute("aria-label", title);
        btn.innerHTML = '<span aria-hidden="true">' + icon + "</span>";
        btn.addEventListener("click", handler);
        return btn;
    }

    function duplicateMock(entry) {
        const copy = {
            collection: getEntryCollection(entry) || getActiveCollectionId(),
            method: entry.method,
            path: normalizePath((entry.path || "/") + "-copia"),
            statusCode: entry.statusCode,
            responseContentType: getEntryResponseContentType(entry),
            responseBody: entry.responseBody,
            enabled: true,
            bypassEnabled: false,
            bypassUrl: null
        };
        fillForm(copy);
        state.editingKey = null;
        formTitle.textContent = "New mock (duplicate)";
        setFormDirty(true);
        setFeedback(formFeedback, "success", "Mock duplicated. Adjust the path and save.");
    }

    async function uploadMockEntry(entry) {
        const response = await fetch(apiBaseUrl, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                collection: entry.Collection || null,
                method: entry.Method,
                path: entry.Path,
                statusCode: entry.StatusCode,
                responseContentType: entry.ResponseContentType,
                responseBody: entry.ResponseBody,
                enabled: entry.Enabled !== false,
                bypassEnabled: entry.BypassEnabled,
                bypassUrl: entry.BypassUrl
            })
        });
        if (!response.ok) {
            await handleResponseError(response);
        }
    }

    async function importMocksFromFile(event) {
        const file = event.target.files && event.target.files[0];
        event.target.value = "";
        if (!file) return;

        importMocksButton.disabled = true;
        setFeedback(listFeedback, "", "");

        try {
            const content = await readFileAsText(file);
            const parsed = JSON.parse(content);
            let entries = [];
            let collections = [];

            if (Array.isArray(parsed)) {
                entries = parsed;
            } else if (parsed && (parsed.Mocks || parsed.mocks)) {
                entries = parsed.Mocks || parsed.mocks;
                collections = parsed.Collections || parsed.collections || [];
            } else {
                throw new Error("the file must be a list of mocks or an object with Collections and Mocks");
            }

            if (!entries.length && !collections.length) {
                throw new Error("the selected file is empty");
            }

            for (let index = 0; index < collections.length; index += 1) {
                const collection = collections[index];
                const collectionId = (collection.id || collection.Id || "").trim();
                const bypassUrl = collection.bypassUrl || collection.BypassUrl;
                if (!collectionId) {
                    throw new Error("invalid collection at position " + (index + 1));
                }
                const createResponse = await fetch(collectionsApiUrl, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ id: collectionId, bypassUrl: bypassUrl })
                });
                if (createResponse.status !== 201 && createResponse.status !== 409) {
                    await handleResponseError(createResponse);
                }
            }

            const normalizedEntries = entries.map(function (item, index) {
                try {
                    return toFileEntry(item);
                } catch (error) {
                    throw new Error("invalid mock at position " + (index + 1) + ": " + error.message);
                }
            });
            for (const entry of normalizedEntries) {
                await uploadMockEntry(entry);
                markMockUpdated(entry.Method, entry.Path);
            }
            await loadCollections();
            await loadMocks();
            resetForm();
            setFeedback(listFeedback, "success", normalizedEntries.length + " mock(s) imported.");
        } catch (error) {
            setFeedback(listFeedback, "error", "Import failed: " + error.message);
        } finally {
            importMocksButton.disabled = false;
        }
    }

    function exportMocksFile() {
        try {
            const fileContent = getMocksFileContent();
            const blob = new Blob([fileContent], { type: "application/json;charset=utf-8" });
            const objectUrl = URL.createObjectURL(blob);
            const link = document.createElement("a");
            link.href = objectUrl;
            link.download = "mocks.json";
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            URL.revokeObjectURL(objectUrl);
            setFeedback(listFeedback, "success", "mocks.json file exported.");
        } catch (error) {
            setFeedback(listFeedback, "error", "Unable to export: " + error.message);
        }
    }

    async function handleResponseError(response) {
        const contentType = response.headers.get("content-type") || "";
        if (contentType.indexOf("application/json") >= 0) {
            const data = await response.json();
            throw new Error(typeof data === "string" ? data : JSON.stringify(data));
        }
        const text = await response.text();
        throw new Error(text || "The operation failed.");
    }

    function formatResponseText(text, contentType) {
        if (!text) return "No content returned.";
        const looksLikeJson = contentType.indexOf("application/json") >= 0 || /^[\[{]/.test(text.trim());
        if (!looksLikeJson) return text;
        try {
            return JSON.stringify(JSON.parse(text), null, 2);
        } catch (error) {
            return text;
        }
    }

    function parseHeaderLines(text) {
        const headers = {};
        const blockedHeaders = ["content-type", "content-length", "host", "connection", "transfer-encoding"];
        const lines = text.split(/\r?\n/);
        for (let index = 0; index < lines.length; index += 1) {
            const line = lines[index].trim();
            if (!line) continue;
            const separatorIndex = line.indexOf(":");
            if (separatorIndex <= 0) {
                throw new Error("invalid header on line " + (index + 1) + ". Use Name: Value.");
            }
            const name = line.slice(0, separatorIndex).trim();
            const value = line.slice(separatorIndex + 1).trim();
            if (!name || !value) {
                throw new Error("invalid header on line " + (index + 1) + ".");
            }
            if (blockedHeaders.indexOf(name.toLowerCase()) >= 0) {
                throw new Error("the " + name + " header is controlled automatically.");
            }
            headers[name] = value;
        }
        return headers;
    }

    function parseKeyValueBody(text, contentType) {
        const lines = text.split(/\r?\n/).filter(function (line) { return line.trim(); });
        if (!lines.length && text.trim()) return text;
        const entries = [];
        for (let index = 0; index < lines.length; index += 1) {
            const line = lines[index].trim();
            const separatorIndex = line.indexOf("=");
            if (separatorIndex <= 0) {
                throw new Error("invalid line " + (index + 1) + " for " + contentType + ".");
            }
            entries.push([line.slice(0, separatorIndex).trim(), line.slice(separatorIndex + 1)]);
        }
        return entries;
    }

    function buildRequestBody(text, contentType) {
        if (!text) return null;
        if (contentType === "application/json") {
            return JSON.stringify(JSON.parse(text));
        }
        if (contentType === "application/x-www-form-urlencoded") {
            if (text.indexOf("\n") < 0 && text.indexOf("&") >= 0) return text;
            const params = new URLSearchParams();
            parseKeyValueBody(text, contentType).forEach(function (entry) {
                params.append(entry[0], entry[1]);
            });
            return params.toString();
        }
        if (contentType === "multipart/form-data") {
            const formData = new FormData();
            parseKeyValueBody(text, contentType).forEach(function (entry) {
                formData.append(entry[0], entry[1]);
            });
            return formData;
        }
        return text;
    }

    async function loadMocks() {
        reloadButton.disabled = true;
        setFeedback(listFeedback, "", "");
        try {
            const url = new URL(apiBaseUrl);
            const activeCollection = getActiveCollectionId();
            if (activeCollection) {
                url.searchParams.set("collection", activeCollection);
            }
            const response = await fetch(url, {
                headers: { Accept: "application/json" }
            });
            if (!response.ok) {
                await handleResponseError(response);
            }
            state.mocks = (await response.json()).map(normalizeEntry);
            renderMockList();
        } catch (error) {
            renderMockList();
            setFeedback(listFeedback, "error", "Failed to load mocks: " + error.message);
        } finally {
            reloadButton.disabled = false;
        }
    }

    async function runTestCall(event) {
        if (event) event.preventDefault();

        const method = testMethodField.value.toUpperCase();
        const path = normalizePath(testPathField.value);
        const contentType = testContentTypeField.value;
        const requestHeadersText = testRequestHeadersField.value.trim();

        const requestBodyText = testRequestBodyField.value.trim();

        if (!testPathField.value.trim()) {
            setFeedback(testFeedback, "error", "Enter the path to call.");
            testPathField.focus();
            return;
        }

        if (method === "GET" && requestBodyText) {
            setFeedback(testFeedback, "error", "GET requests must not include a body.");
            testRequestBodyField.focus();
            return;
        }

        let requestBody = null;
        if (requestBodyText) {
            try {
                requestBody = buildRequestBody(requestBodyText, contentType);
            } catch (error) {
                setFeedback(testFeedback, "error", "Invalid body: " + error.message);
                return;
            }
        }

        let requestHeaders = {};
        try {
            requestHeaders = parseHeaderLines(requestHeadersText);
        } catch (error) {
            setFeedback(testFeedback, "error", error.message);
            return;
        }

        const requestUrl = buildMockUrl(path);
        const fetchOptions = { method: method, headers: requestHeaders };
        if (requestBody !== null) {
            if (contentType !== "multipart/form-data") {
                fetchOptions.headers["Content-Type"] = contentType;
            }
            fetchOptions.body = requestBody;
        }

        runTestButton.disabled = true;
        setFeedback(testFeedback, "", "");
        testResponseStatus.textContent = "…";
        testResponseContentType.textContent = "—";
        testResponseTime.textContent = "—";
        showResponseEmpty();

        const startTime = performance.now();

        try {
            const response = await fetch(requestUrl, fetchOptions);
            const elapsed = Math.round(performance.now() - startTime);
            const respContentType = response.headers.get("content-type") || "not specified";
            const responseText = await response.text();

            testResponseStatus.textContent = response.status + " " + response.statusText;
            testResponseContentType.textContent = respContentType;
            testResponseTime.textContent = elapsed + " ms";
            testResponseStatus.className = "stat-card-value " + (response.ok ? "is-success" : "is-error");

            showResponseBody(formatResponseText(responseText, respContentType));

            if (response.ok) {
                setFeedback(testFeedback, "success", "Request completed in " + elapsed + " ms.", { silent: true });
            } else {
                setFeedback(testFeedback, "error", "The request returned " + response.status + ".", { silent: true });
            }
        } catch (error) {
            const elapsed = Math.round(performance.now() - startTime);
            testResponseStatus.textContent = "Failed";
            testResponseContentType.textContent = "—";
            testResponseTime.textContent = elapsed + " ms";
            testResponseStatus.className = "stat-card-value is-error";
            showResponseError(error.message);
            setFeedback(testFeedback, "error", "Execution failed: " + error.message, { silent: true });
        } finally {
            runTestButton.disabled = false;
        }
    }

    async function saveMock(event) {
        event.preventDefault();

        const method = methodField.value.toUpperCase();
        const path = normalizePath(pathField.value);
        const statusCode = parseStatusCodeValue();
        const responseDelayMs = parseResponseDelayMsValue();
        const bypassEnabled = bypassEnabledField.checked;
        const bypassUrl = normalizeBypassUrl(bypassUrlField.value);

        if (!pathField.value.trim()) {
            setFeedback(formFeedback, "error", "Enter the endpoint path.");
            pathField.focus();
            return;
        }

        const activeCollection = getActiveCollectionId();
        if (!isStandaloneMode() && !activeCollection) {
            setFeedback(formFeedback, "error", "Select or create a collection before saving the mock.");
            return;
        }

        if (bypassEnabled && !isValidBypassUrl(bypassUrl)) {
            setFeedback(formFeedback, "error", "Enter a valid bypass URL (HTTP/HTTPS).");
            bypassUrlField.focus();
            return;
        }

        if (statusCode === null) {
            setFeedback(formFeedback, "error", "Enter the status code.");
            statusCodeField.focus();
            return;
        }

        if (Number.isNaN(statusCode)) {
            setFeedback(formFeedback, "error", "Enter a valid status code between 100 and 599.");
            statusCodeField.focus();
            return;
        }

        if (Number.isNaN(responseDelayMs)) {
            setFeedback(formFeedback, "error", "Enter a valid delay in milliseconds, greater than or equal to zero.");
            responseDelayMsField.focus();
            return;
        }

        const responseContentType = responseContentTypeField.value;
        let responseBody = null;
        const responseBodyText = responseBodyField.value.trim();

        if (responseBodyText) {
            try {
                responseBody = parseResponseBodyForSave(responseBodyText, responseContentType);
            } catch (error) {
                const message = responseContentType === "application/json"
                    ? "The JSON body is invalid: " + error.message
                    : "The form-urlencoded body is invalid: " + error.message;
                setFeedback(formFeedback, "error", message);
                responseBodyField.focus();
                return;
            }
        }

        const payload = {
            method: method,
            path: path,
            statusCode: statusCode,
            responseDelayMs: responseDelayMs,
            responseContentType: responseContentType,
            responseBody: responseBody,
            enabled: isStandaloneMode() ? true : mockEnabledField.checked,
            bypassEnabled: bypassEnabled,
            bypassUrl: bypassUrl
        };
        if (activeCollection) {
            payload.collection = activeCollection;
        }

        const submitButton = document.querySelector("button[type='submit'][form='mock-form']") ||
            form.querySelector("button[type='submit']");
        if (submitButton) {
            submitButton.disabled = true;
        }
        setFeedback(formFeedback, "", "");

        try {
            const response = await fetch(apiBaseUrl, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload)
            });
            if (!response.ok) {
                await handleResponseError(response);
            }

            state.editingKey = toKey(method, path, activeCollection);
            markMockUpdated(method, path);
            setFeedback(formFeedback, "success", "Mock saved successfully.");
            setFormDirty(false);
            await loadMocks();
            fillForm(Object.assign({}, payload, { collection: activeCollection }));
        } catch (error) {
            setFeedback(formFeedback, "error", "Failed to save: " + error.message);
        } finally {
            if (submitButton) {
                submitButton.disabled = false;
            }
        }
    }

    async function toggleMockEnabled(entry) {
        const collection = getEntryCollection(entry);
        if (!collection) {
            setFeedback(listFeedback, "error", "Enable/disable only applies to collection mocks.");
            return;
        }

        const currentEnabled = getMockEnabled(entry);
        const nextEnabled = !currentEnabled;
        setFeedback(listFeedback, "", "");

        try {
            const url = new URL(apiBaseUrl);
            url.pathname = url.pathname.replace(/\/$/, "") + "/enabled";

            const response = await fetch(url, {
                method: "PATCH",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    collection: collection,
                    method: entry.method,
                    path: entry.path,
                    enabled: nextEnabled
                })
            });

            if (!response.ok) {
                await handleResponseError(response);
            }

            await loadMocks();
            setFeedback(
                listFeedback,
                "success",
                nextEnabled ? "Mock enabled." : "Mock disabled (collection bypass).",
                { silent: true }
            );

            if (state.editingKey === toKey(entry.method, entry.path, collection)) {
                mockEnabledField.checked = nextEnabled;
            }
        } catch (error) {
            setFeedback(listFeedback, "error", "Failed to update mock: " + error.message);
        }
    }

    async function toggleBypass(entry) {
        const currentEnabled = getBypassEnabled(entry);
        const bypassUrl = getBypassUrl(entry);
        const nextEnabled = !currentEnabled;

        if (nextEnabled && !isValidBypassUrl(bypassUrl)) {
            fillForm(entry);
            setFeedback(formFeedback, "error", "Set a valid bypass URL before enabling.");
            bypassUrlField.focus();
            return;
        }

        setFeedback(listFeedback, "", "");

        try {
            const url = new URL(apiBaseUrl);
            url.pathname = url.pathname.replace(/\/$/, "") + "/bypass";

            const response = await fetch(url, {
                method: "PATCH",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    collection: getEntryCollection(entry),
                    method: entry.method,
                    path: entry.path,
                    bypassEnabled: nextEnabled,
                    bypassUrl: bypassUrl
                })
            });

            if (!response.ok) {
                await handleResponseError(response);
            }

            await loadMocks();
            setFeedback(
                listFeedback,
                "success",
                nextEnabled ? "Bypass enabled." : "Bypass disabled.",
                { silent: true }
            );

            if (state.editingKey === toKey(entry.method, entry.path, getEntryCollection(entry))) {
                bypassEnabledField.checked = nextEnabled;
                updateBypassFieldState();
            }
        } catch (error) {
            setFeedback(listFeedback, "error", "Failed to update bypass: " + error.message);
        }
    }

    async function deleteMock(entry) {
        if (!window.confirm("Delete mock " + entry.method + " " + entry.path + "?")) {
            return;
        }
        setFeedback(listFeedback, "", "");
        try {
            const url = new URL(apiBaseUrl);
            url.searchParams.set("method", entry.method);
            url.searchParams.set("path", entry.path);
            const entryCollection = getEntryCollection(entry);
            if (entryCollection) {
                url.searchParams.set("collection", entryCollection);
            }
            const response = await fetch(url, { method: "DELETE" });
            if (!response.ok) {
                await handleResponseError(response);
            }
            if (state.editingKey === toKey(entry.method, entry.path, entryCollection)) {
                resetForm();
            }
            delete state.lastUpdated[toKey(entry.method, entry.path, entryCollection)];
            setFeedback(listFeedback, "success", "Mock removed.");
            await loadMocks();
        } catch (error) {
            setFeedback(listFeedback, "error", "Failed to remove: " + error.message);
        }
    }

    function setupCopyButtons() {
        document.querySelectorAll("[data-copy-target]").forEach(function (button) {
            button.addEventListener("click", async function () {
                const targetId = button.getAttribute("data-copy-target");
                const target = document.getElementById(targetId);
                const text = (target.textContent || "").trim();
                if (!text || text === "—") {
                    showToast("error", "Nothing to copy.");
                    return;
                }
                try {
                    await writeToClipboard(text);
                    showToast("success", "URL copied.");
                } catch (error) {
                    showToast("error", "Unable to copy: " + error.message);
                }
            });
        });
    }

    function setupModalClose() {
        curlModal.querySelectorAll("[data-close-modal]").forEach(function (el) {
            el.addEventListener("click", closeCurlModal);
        });
        document.addEventListener("keydown", function (event) {
            if (event.key === "Escape" && !curlModal.hidden) {
                closeCurlModal();
            }
        });
    }

    function markFormDirtyOnInput() {
        const fields = [methodField, pathField, statusCodeField, responseDelayMsField, responseContentTypeField, mockEnabledField, bypassEnabledField, bypassUrlField, responseBodyField];
        fields.forEach(function (field) {
            field.addEventListener("input", function () { setFormDirty(true); });
            field.addEventListener("change", function () { setFormDirty(true); });
        });
    }

    reloadButton.addEventListener("click", function () {
        loadCollections().then(loadMocks);
    });
    collectionSelectField.addEventListener("change", function () {
        setActiveCollection(collectionSelectField.value);
    });
    navMocks.addEventListener("click", function () {
        switchScreen("mocks");
    });
    navCollections.addEventListener("click", function () {
        switchScreen("collections");
    });
    newCollectionSidebarButton.addEventListener("click", function () {
        resetCollectionForm();
        collectionFormIdField.focus();
    });
    reloadCollectionsButton.addEventListener("click", loadCollections);
    collectionSearchField.addEventListener("input", renderCollectionsList);
    collectionForm.addEventListener("submit", saveCollectionFromForm);
    resetCollectionFormButton.addEventListener("click", resetCollectionForm);
    deleteCollectionFormButton.addEventListener("click", deleteCollectionFromForm);
    openCollectionMocksButton.addEventListener("click", openMocksForCurrentCollection);
    collectionFormIdField.addEventListener("input", updateCollectionUrlPreview);
    importMocksButton.addEventListener("click", function () { importMocksInput.click(); });
    importMocksInput.addEventListener("change", importMocksFromFile);
    exportMocksButton.addEventListener("click", exportMocksFile);
    themeToggleButton.addEventListener("click", toggleTheme);
    resetFormButton.addEventListener("click", function () { resetForm(); });
    newMockButton.addEventListener("click", function () {
        resetForm();
        pathField.focus();
    });
    resetTestButton.addEventListener("click", function () { resetTestForm(); });
    showCurlImportButton.addEventListener("click", openCurlModal);
    importCurlButton.addEventListener("click", importCurlToTest);
    cancelCurlImportButton.addEventListener("click", closeCurlModal);
    mockSearchField.addEventListener("input", renderMockList);
    filterMethodField.addEventListener("change", renderMockList);
    filterStatusField.addEventListener("change", renderMockList);

    bypassEnabledField.addEventListener("change", function () {
        updateBypassFieldState();
        setFormDirty(true);
    });
    mockEnabledField.addEventListener("change", function () {
        setFormDirty(true);
    });

    methodField.addEventListener("change", function () {
        updateEndpointPreview();
        setFormDirty(true);
    });
    pathField.addEventListener("input", updateEndpointPreview);
    testMethodField.addEventListener("change", updateTestEndpointPreview);
    testPathField.addEventListener("input", updateTestEndpointPreview);
    testContentTypeField.addEventListener("change", updateTestBodyPlaceholder);
    responseContentTypeField.addEventListener("change", function () {
        updateResponseBodyEditor();
        setFormDirty(true);
    });

    bindJsonEditor(responseBodyField, responseBodyGutter, {
        markDirty: true,
        whenJson: function () {
            return responseContentTypeField.value === "application/json";
        }
    });
    bindJsonEditor(testRequestBodyField, testRequestBodyGutter, { onlyWhenJson: true });

    form.addEventListener("submit", saveMock);
    testForm.addEventListener("submit", runTestCall);

    setupCopyButtons();
    setupModalClose();
    markFormDirtyOnInput();

    applyTheme(getCurrentTheme());
    resetForm();
    resetTestForm();
    updateResponseBodyEditor();
    updateBypassFieldState();
    updateCollectionUiState();
    resetCollectionForm();
    if (state.activeScreen === "collections") {
        switchScreen("collections");
    } else {
        switchScreen("mocks");
    }
    loadCollections().then(loadMocks);
    setupUpdateChecker();

    function setupUpdateChecker() {
        if (!updateBanner || !updateApplyButton || !updateDismissButton) {
            return;
        }

        let latestKnownVersion = null;

        async function checkForUpdates(options) {
            const manual = Boolean(options && options.manual);

            if (manual && checkUpdateButton) {
                checkUpdateButton.disabled = true;
            }

            try {
                const response = await fetch(versionApiUrl, {
                    headers: { Accept: "application/json" },
                    cache: "no-store"
                });
                if (!response.ok) {
                    if (manual) {
                        showToast("error", "Unable to check for updates.");
                    }
                    return;
                }

                const data = await response.json();
                const latestVersion = data.latestVersion || data.LatestVersion || null;
                const currentVersion = data.currentVersion || data.CurrentVersion || "";
                const updateAvailable = Boolean(data.updateAvailable ?? data.UpdateAvailable);
                const checkError = data.error || data.Error || null;
                latestKnownVersion = latestVersion;

                if (appVersionLabel && currentVersion) {
                    appVersionLabel.textContent = "v" + currentVersion;
                    appVersionLabel.title = "Installed version: " + currentVersion;
                }

                if (!updateAvailable || !latestVersion) {
                    updateBanner.hidden = true;
                    if (manual) {
                        if (checkError) {
                            showToast("error", checkError);
                        } else {
                            showToast(
                                "success",
                                "You are already on the latest version" +
                                    (currentVersion ? " (" + currentVersion + ")" : "") +
                                    "."
                            );
                        }
                    }
                    return;
                }

                if (!manual) {
                    const dismissed = localStorage.getItem(updateDismissedVersionKey);
                    if (dismissed === latestVersion) {
                        updateBanner.hidden = true;
                        return;
                    }
                } else {
                    localStorage.removeItem(updateDismissedVersionKey);
                }

                updateBannerText.textContent =
                    "New version " + latestVersion + " available (current: " + currentVersion + ").";
                updateBanner.hidden = false;

                if (manual) {
                    showToast("success", "New version " + latestVersion + " found.");
                }
            } catch (_error) {
                if (manual) {
                    showToast("error", "Unable to check for updates.");
                }
            } finally {
                if (manual && checkUpdateButton) {
                    checkUpdateButton.disabled = false;
                }
            }
        }

        function showUpdateOverlay(message) {
            if (updateOverlayMessage) {
                updateOverlayMessage.textContent = message;
            }
            if (updateOverlay) {
                updateOverlay.hidden = false;
            }
        }

        async function waitForServiceRestart() {
            const startedAt = Date.now();
            const maxWaitMs = 120000;
            const pollMs = 2000;

            await new Promise(function (resolve) { setTimeout(resolve, 4000); });

            while (Date.now() - startedAt < maxWaitMs) {
                try {
                    const response = await fetch(versionApiUrl, {
                        headers: { Accept: "application/json" },
                        cache: "no-store"
                    });
                    if (response.ok) {
                        return true;
                    }
                } catch (_error) {
                    // Service still restarting.
                }

                await new Promise(function (resolve) { setTimeout(resolve, pollMs); });
            }

            return false;
        }

        async function applyUpdate() {
            updateApplyButton.disabled = true;
            showUpdateOverlay("Downloading and applying the new version. The service will restart.");

            try {
                const response = await fetch(updateApiUrl, {
                    method: "POST",
                    headers: { Accept: "application/json" }
                });
                const data = await response.json().catch(function () { return {}; });

                if (!response.ok && response.status !== 202) {
                    updateOverlay.hidden = true;
                    updateApplyButton.disabled = false;
                    showToast("error", data.message || data.Message || "Unable to start the update.");
                    return;
                }

                showUpdateOverlay("Restarting the service… the page will reload automatically.");

                const recovered = await waitForServiceRestart();
                if (recovered) {
                    window.location.reload();
                    return;
                }

                updateOverlay.hidden = true;
                updateApplyButton.disabled = false;
                showToast("error", "The update started, but the service took too long to come back. Reload the page manually.");
            } catch (_error) {
                showUpdateOverlay("Waiting for the service to come back…");
                const recovered = await waitForServiceRestart();
                if (recovered) {
                    window.location.reload();
                    return;
                }

                updateOverlay.hidden = true;
                updateApplyButton.disabled = false;
                showToast("error", "Failed to track the update. Check the service and reload the page.");
            }
        }

        updateApplyButton.addEventListener("click", applyUpdate);
        updateDismissButton.addEventListener("click", function () {
            if (latestKnownVersion) {
                localStorage.setItem(updateDismissedVersionKey, latestKnownVersion);
            }
            updateBanner.hidden = true;
        });

        if (checkUpdateButton) {
            checkUpdateButton.addEventListener("click", function () {
                checkForUpdates({ manual: true });
            });
        }

        checkForUpdates();
        setInterval(checkForUpdates, updateCheckIntervalMs);
    }
})();
