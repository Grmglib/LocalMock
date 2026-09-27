<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import { api } from './api.js'
import {
  buildMockUrl, collectionId, entryKey, formatResponseText, getBypassEnabled,
  getEnabled, normalizeEntry, normalizePath, parseCurl, parseHeaders,
  parseRequestBody, parseResponseBody, responseBodyText, toFileEntry, validHttpUrl,
} from './domain.js'
import CollectionTree from './components/CollectionTree.vue'
import MockEditor from './components/MockEditor.vue'
import RequestTester from './components/RequestTester.vue'
import CollectionEditor from './components/CollectionEditor.vue'
import CurlImportModal from './components/CurlImportModal.vue'

const savedCollection = localStorage.getItem('localmock-active-collection') || ''
const savedTheme = localStorage.getItem('localmock-theme-v2')
const theme = ref(savedTheme === 'light' ? 'light' : 'dark')
const mocks = ref([])
const collections = ref([])
const activeCollection = ref(savedCollection)
const expandedIds = reactive(new Set())
const activeTab = ref('configure')
const editingKey = ref('')
const form = reactive(blankMock(savedCollection))
const dirty = ref(false)
const savingMock = ref(false)
const enabledBusy = ref(false)
const mockFeedback = ref('')
const mockFeedbackError = ref(false)
const testForm = reactive({ method: 'GET', path: '', collection: savedCollection, contentType: 'application/json', headers: '', body: '' })
const testResponse = reactive({ status: '', statusCode: 0, contentType: '', time: '', body: null, error: '' })
const runningTest = ref(false)
const testFeedback = ref('')
const testFeedbackError = ref(false)
const search = ref('')
const methodFilter = ref('')
const statusFilter = ref('')
const listFeedback = ref('')
const listFeedbackError = ref(false)
const collectionPaneOpen = ref(false)
const collectionEditingId = ref('')
const collectionForm = reactive({ id: '', bypassUrl: '' })
const collectionSaving = ref(false)
const collectionFeedback = ref('')
const collectionFeedbackError = ref(false)
const collectionDirty = ref(false)
const collectionErrors = reactive({ id: '', bypassUrl: '' })
const collectionCount = ref(0)
const curlOpen = ref(false)
const curlCommand = ref('')
const curlError = ref('')
const importFile = ref(null)
const toasts = ref([])
const appVersion = ref('v…')
const updateAvailable = ref(false)
const latestVersion = ref('')
const checkingUpdate = ref(false)
const updateOverlay = reactive({ visible: false, message: 'LocalMock will restart. Please wait.' })

const isCollectionEditing = computed(() => Boolean(collectionEditingId.value))
const collectionUrl = computed(() => collectionForm.id ? new URL(`/mock/${encodeURIComponent(collectionForm.id)}`, window.location.origin).toString() : '—')
const endpointTitle = computed(() => form.path ? `${form.method} ${normalizePath(form.path)}` : 'New endpoint')
const matchingMock = computed(() => mocks.value.find((entry) => entryKey(entry) === editingKey.value))

function blankMock(collection = '') {
  return { method: 'GET', path: '', statusCode: '200', responseDelayMs: '0', responseContentType: 'application/json', responseBody: '', enabled: true, bypassEnabled: false, bypassUrl: '', collection }
}

function collectionLabel(collection) {
  return String(collection.id ?? collection.Id ?? '')
}

function collectionBypass(collection) {
  return String(collection.bypassUrl ?? collection.BypassUrl ?? '')
}

function applyTheme(value) {
  theme.value = value === 'dark' ? 'dark' : 'light'
  document.documentElement.dataset.theme = theme.value
  localStorage.setItem('localmock-theme-v2', theme.value)
}

function toast(message, error = false) {
  const id = Date.now() + Math.random()
  toasts.value.push({ id, message, error })
  window.setTimeout(() => { toasts.value = toasts.value.filter((item) => item.id !== id) }, 3500)
}

async function copyText(value) {
  if (!value || value === '—') return
  try {
    await navigator.clipboard.writeText(value)
    toast('Copied to clipboard.')
  } catch {
    toast('Could not access the clipboard.', true)
  }
}

async function refresh() {
  try {
    const [mockData, collectionData] = await Promise.all([api.mocks(), api.collections()])
    mocks.value = mockData.map(normalizeEntry)
    collections.value = collectionData
    if (activeCollection.value && !collections.value.some((item) => collectionLabel(item) === activeCollection.value)) {
      activeCollection.value = ''
    }
    localStorage.setItem('localmock-active-collection', activeCollection.value)
    if (editingKey.value) {
      const latest = mocks.value.find((entry) => entryKey(entry) === editingKey.value)
      if (latest) form.enabled = latest.enabled
    }
    listFeedback.value = ''
    listFeedbackError.value = false
  } catch (error) {
    listFeedback.value = `Failed to load endpoints: ${error.message}`
    listFeedbackError.value = true
  }
}

function activateCollection(id) {
  if (expandedIds.has(id)) expandedIds.delete(id)
  else expandedIds.add(id)
  activeCollection.value = id
  localStorage.setItem('localmock-active-collection', id)
  testForm.collection = id
  if (!editingKey.value) form.collection = id
}

function toggleGroup(id) {
  if (expandedIds.has(id)) expandedIds.delete(id)
  else expandedIds.add(id)
}

function resetMockForm(collection = activeCollection.value) {
  collectionPaneOpen.value = false
  activeCollection.value = collection
  localStorage.setItem('localmock-active-collection', collection)
  testForm.collection = collection
  expandedIds.add(collection)
  Object.assign(form, blankMock(collection))
  editingKey.value = ''
  dirty.value = false
  mockFeedback.value = ''
  activeTab.value = 'configure'
}

function fillMock(entry) {
  collectionPaneOpen.value = false
  const mock = normalizeEntry(entry)
  Object.assign(form, { ...mock, statusCode: String(mock.statusCode), responseDelayMs: String(mock.responseDelayMs), responseBody: responseBodyText(mock) })
  editingKey.value = entryKey(mock)
  activeCollection.value = mock.collection
  localStorage.setItem('localmock-active-collection', mock.collection)
  testForm.collection = mock.collection
  testForm.method = mock.method
  testForm.path = mock.path
  testResponse.body = null
  testResponse.error = ''
  mockFeedback.value = ''
  dirty.value = false
  activeTab.value = 'configure'
}

function duplicateMock(entry) {
  fillMock(entry)
  editingKey.value = ''
  form.path = `${form.path}-copy`
  dirty.value = true
}

function useMockForTest(entry) {
  collectionPaneOpen.value = false
  const mock = normalizeEntry(entry)
  testForm.method = mock.method
  testForm.path = mock.path
  testForm.collection = mock.collection
  testForm.contentType = 'application/json'
  testForm.headers = ''
  testForm.body = ''
  resetResponse()
  activeTab.value = 'test'
}

function resetResponse() {
  Object.assign(testResponse, { status: '', statusCode: 0, contentType: '', time: '', body: null, error: '' })
  testFeedback.value = ''
}

function resetTest() {
  Object.assign(testForm, { method: 'GET', path: '', collection: activeCollection.value, contentType: 'application/json', headers: '', body: '' })
  resetResponse()
}

async function toggleEnabled(entry, enabled) {
  const wasDirty = dirty.value
  try {
    await api.setMockEnabled(entry, enabled)
    await refresh()
    if (entryKey(entry) === editingKey.value) {
      form.enabled = enabled
      dirty.value = wasDirty
    }
    listFeedback.value = enabled ? 'Mock enabled.' : 'Mock disabled (collection bypass).'
    listFeedbackError.value = false
  } catch (error) {
    listFeedback.value = `Failed to update mock: ${error.message}`
    listFeedbackError.value = true
  }
}

async function onFormEnabled(enabled) {
  form.enabled = enabled
  if (!editingKey.value || entryKey(form) !== editingKey.value || !form.collection) {
    dirty.value = true
    return
  }
  enabledBusy.value = true
  const wasDirty = dirty.value
  const savedValue = matchingMock.value?.enabled ?? !enabled
  try {
    await api.setMockEnabled(matchingMock.value, enabled)
    await refresh()
    dirty.value = wasDirty
    form.enabled = enabled
  } catch (error) {
    form.enabled = savedValue
    toast(`Could not update mock: ${error.message}`, true)
  } finally {
    enabledBusy.value = false
  }
}

async function toggleBypass(entry) {
  const enabled = !getBypassEnabled(entry)
  if (enabled && !validHttpUrl(entry.bypassUrl)) {
    fillMock(entry)
    mockFeedback.value = 'Set a valid bypass URL before enabling.'
    mockFeedbackError.value = true
    return
  }
  try {
    await api.setBypass(entry, enabled, entry.bypassUrl)
    await refresh()
    toast(enabled ? 'Bypass enabled.' : 'Bypass disabled.')
  } catch (error) {
    toast(`Could not update bypass: ${error.message}`, true)
  }
}

async function saveMock() {
  const path = normalizePath(form.path)
  const statusCode = Number(form.statusCode)
  const responseDelayMs = Number(form.responseDelayMs || 0)
  if (!form.path.trim()) return showMockError('Enter the endpoint path.')
  if (!form.collection && form.bypassEnabled && !validHttpUrl(form.bypassUrl)) return showMockError('Enter a valid bypass URL (HTTP/HTTPS).')
  if (!Number.isInteger(statusCode) || statusCode < 100 || statusCode > 599) return showMockError('Enter a valid status code between 100 and 599.')
  if (!Number.isFinite(responseDelayMs) || responseDelayMs < 0) return showMockError('Enter a valid delay in milliseconds, greater than or equal to zero.')
  let responseBody
  try { responseBody = parseResponseBody(form.responseBody.trim(), form.responseContentType) }
  catch (error) { return showMockError(`Invalid response body: ${error.message}`) }

  const payload = {
    method: form.method,
    path,
    statusCode,
    responseDelayMs,
    responseContentType: form.responseContentType,
    responseBody,
    enabled: form.collection ? form.enabled : true,
    bypassEnabled: form.collection ? false : form.bypassEnabled,
    bypassUrl: form.collection ? null : (form.bypassUrl || null),
  }
  if (form.collection) payload.collection = form.collection
  savingMock.value = true
  mockFeedback.value = ''
  try {
    await api.saveMock(payload)
    await refresh()
    fillMock({ ...payload, collection: form.collection })
    mockFeedback.value = 'Mock saved successfully.'
  } catch (error) {
    showMockError(`Failed to save: ${error.message}`)
  } finally {
    savingMock.value = false
  }
}

function showMockError(message) {
  mockFeedback.value = message
  mockFeedbackError.value = true
}

async function deleteMock(entry) {
  if (!window.confirm(`Delete ${entry.method} ${entry.path}?`)) return
  try {
    await api.removeMock(entry)
    if (entryKey(entry) === editingKey.value) resetMockForm()
    await refresh()
    toast('Mock deleted.')
  } catch (error) {
    toast(`Could not delete mock: ${error.message}`, true)
  }
}

async function runTest() {
  if (!testForm.path.trim()) {
    testFeedback.value = 'Enter the path to call.'
    testFeedbackError.value = true
    return
  }
  if (testForm.method === 'GET' && testForm.body.trim()) {
    testFeedback.value = 'GET requests must not include a body.'
    testFeedbackError.value = true
    return
  }

  let headers
  let body
  try {
    headers = parseHeaders(testForm.headers)
    body = parseRequestBody(testForm.body.trim(), testForm.contentType)
  } catch (error) {
    testFeedback.value = `Invalid request: ${error.message}`
    testFeedbackError.value = true
    return
  }
  if (body !== null && testForm.contentType !== 'multipart/form-data') headers['Content-Type'] = testForm.contentType

  runningTest.value = true
  testFeedback.value = ''
  Object.assign(testResponse, { status: '…', statusCode: 0, contentType: '', time: '', body: null, error: '' })
  const start = performance.now()
  try {
    const response = await api.run(buildMockUrl(testForm.path, testForm.collection), {
      method: testForm.method,
      headers,
      ...(body === null ? {} : { body }),
    })
    const text = await response.text()
    const contentType = response.headers.get('content-type') || 'not specified'
    Object.assign(testResponse, {
      status: `${response.status} ${response.statusText}`,
      statusCode: response.status,
      contentType,
      time: `${Math.round(performance.now() - start)} ms`,
      body: formatResponseText(text, contentType),
      error: '',
    })
    testFeedback.value = response.ok ? `Request completed in ${testResponse.time}.` : `The request returned ${response.status}.`
    testFeedbackError.value = !response.ok
  } catch (error) {
    testResponse.status = 'Failed'
    testResponse.statusCode = 0
    testResponse.time = `${Math.round(performance.now() - start)} ms`
    testResponse.error = error.message
    testFeedback.value = `Execution failed: ${error.message}`
    testFeedbackError.value = true
  } finally {
    runningTest.value = false
  }
}

function openNewCollection() {
  collectionEditingId.value = ''
  Object.assign(collectionForm, { id: '', bypassUrl: '' })
  Object.assign(collectionErrors, { id: '', bypassUrl: '' })
  collectionFeedback.value = ''
  collectionFeedbackError.value = false
  collectionDirty.value = false
  collectionPaneOpen.value = true
}

function openCollection(collection) {
  collectionEditingId.value = collectionLabel(collection)
  activeCollection.value = collectionEditingId.value
  localStorage.setItem('localmock-active-collection', activeCollection.value)
  Object.assign(collectionForm, { id: collectionLabel(collection), bypassUrl: collectionBypass(collection) })
  Object.assign(collectionErrors, { id: '', bypassUrl: '' })
  collectionCount.value = mocks.value.filter((entry) => collectionId(entry) === collectionLabel(collection)).length
  collectionFeedback.value = ''
  collectionFeedbackError.value = false
  collectionDirty.value = false
  collectionPaneOpen.value = true
}

async function saveCollection() {
  const id = collectionForm.id.trim()
  const bypassUrl = collectionForm.bypassUrl.trim()
  collectionErrors.id = ''
  collectionErrors.bypassUrl = ''
  collectionFeedback.value = ''

  if (!id) collectionErrors.id = 'Enter a collection ID.'
  else if (!/^[a-zA-Z0-9_-]+$/.test(id)) collectionErrors.id = 'Use only letters, numbers, underscores, and hyphens.'
  else if (['collections', 'bypass', 'enabled'].includes(id.toLowerCase())) collectionErrors.id = 'This collection ID is reserved.'
  else if (!isCollectionEditing.value && collections.value.some((item) => collectionLabel(item) === id)) collectionErrors.id = 'A collection with this ID already exists.'

  if (!bypassUrl) collectionErrors.bypassUrl = 'Enter a base URL.'
  else if (!validHttpUrl(bypassUrl)) collectionErrors.bypassUrl = 'Enter a valid HTTP or HTTPS URL, such as https://api.example.com.'

  if (collectionErrors.id || collectionErrors.bypassUrl) {
    return
  }
  collectionSaving.value = true
  try {
    if (isCollectionEditing.value) await api.updateCollection(id, bypassUrl)
    else await api.createCollection({ id, bypassUrl })
    await refresh()
    activeCollection.value = id
    localStorage.setItem('localmock-active-collection', id)
    collectionEditingId.value = id
    collectionDirty.value = false
    collectionFeedback.value = 'Collection saved successfully.'
    collectionFeedbackError.value = false
  } catch (error) {
    collectionFeedback.value = `Failed to save collection: ${error.message}`
    collectionFeedbackError.value = true
  } finally {
    collectionSaving.value = false
  }
}

function clearCollectionForm() {
  if (isCollectionEditing.value) collectionForm.bypassUrl = ''
  else Object.assign(collectionForm, { id: '', bypassUrl: '' })
  Object.assign(collectionErrors, { id: '', bypassUrl: '' })
  collectionDirty.value = true
  collectionFeedback.value = ''
}

function onCollectionFieldInput(field) {
  collectionErrors[field] = ''
  collectionDirty.value = true
  collectionFeedback.value = ''
}

async function deleteCollection() {
  const id = collectionEditingId.value
  if (!id || !window.confirm(`Delete collection “${id}” and its mocks?`)) return
  try {
    await api.removeCollection(id)
    collectionPaneOpen.value = false
    if (activeCollection.value === id) activeCollection.value = ''
    if (form.collection === id) resetMockForm()
    await refresh()
    toast('Collection and its mocks deleted.')
  } catch (error) {
    collectionFeedback.value = `Could not delete collection: ${error.message}`
    collectionFeedbackError.value = true
  }
}

function manageCollectionMocks() {
  activeCollection.value = collectionEditingId.value
  testForm.collection = activeCollection.value
  localStorage.setItem('localmock-active-collection', activeCollection.value)
  collectionPaneOpen.value = false
  resetMockForm()
  activeTab.value = 'configure'
}

function openCurlImport() {
  curlCommand.value = ''
  curlError.value = ''
  curlOpen.value = true
}

function applyCurlImport() {
  try {
    const parsed = parseCurl(curlCommand.value)
    if (['GET', 'POST', 'PUT', 'PATCH', 'DELETE'].includes(parsed.method)) testForm.method = parsed.method
    if (parsed.path) testForm.path = normalizePath(parsed.path)
    if (['application/json', 'application/x-www-form-urlencoded', 'text/plain', 'application/xml', 'multipart/form-data'].includes(parsed.contentType)) testForm.contentType = parsed.contentType
    testForm.headers = parsed.headers.map((header) => `${header.name}: ${header.value}`).join('\n')
    if (parsed.body !== null) testForm.body = parsed.body
    curlOpen.value = false
    curlError.value = ''
    testFeedback.value = 'cURL imported successfully.'
    testFeedbackError.value = false
  } catch (error) {
    curlError.value = error.message
  }
}

function exportMocks() {
  const payload = {
    Collections: collections.value.map((collection) => ({ Id: collectionLabel(collection), BypassUrl: collectionBypass(collection) })),
    Mocks: mocks.value.map(toFileEntry),
  }
  const url = URL.createObjectURL(new Blob([JSON.stringify(payload, null, 2)], { type: 'application/json' }))
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = 'localmock-export.json'
  anchor.click()
  URL.revokeObjectURL(url)
}

async function importMocks(event) {
  const file = event.target.files?.[0]
  event.target.value = ''
  if (!file) return
  try {
    const data = JSON.parse(await file.text())
    const collectionData = data.Collections ?? data.collections ?? []
    const mockData = data.Mocks ?? data.mocks ?? (Array.isArray(data) ? data : [])
    for (const collection of collectionData) {
      const id = String(collection.Id ?? collection.id ?? '').trim()
      if (id && !collections.value.some((item) => collectionLabel(item) === id)) {
        await api.createCollection({ id, bypassUrl: collection.BypassUrl ?? collection.bypassUrl ?? '' })
      }
    }
    for (const raw of mockData) {
      const mock = normalizeEntry(raw)
      const payload = {
        method: mock.method,
        path: mock.path,
        statusCode: mock.statusCode,
        responseDelayMs: mock.responseDelayMs,
        responseContentType: mock.responseContentType,
        responseBody: mock.responseBody,
        enabled: mock.enabled,
        bypassEnabled: mock.bypassEnabled,
        bypassUrl: mock.bypassUrl,
      }
      if (mock.collection) payload.collection = mock.collection
      await api.saveMock(payload)
    }
    await refresh()
    toast(`Imported ${mockData.length} mock(s).`)
  } catch (error) {
    toast(`Import failed: ${error.message}`, true)
  }
}

async function checkUpdate() {
  checkingUpdate.value = true
  try {
    const status = await api.version()
    appVersion.value = `v${status.currentVersion ?? status.CurrentVersion ?? '…'}`
    latestVersion.value = status.latestVersion ?? status.LatestVersion ?? ''
    updateAvailable.value = Boolean(status.updateAvailable ?? status.UpdateAvailable)
    if (status.error ?? status.Error) toast(`Update check failed: ${status.error ?? status.Error}`, true)
  } catch (error) {
    try {
      const version = await api.currentVersion()
      appVersion.value = `v${version.currentVersion ?? version.CurrentVersion ?? '…'}`
    } catch { appVersion.value = 'v…' }
    toast(`Could not check for updates: ${error.message}`, true)
  } finally {
    checkingUpdate.value = false
  }
}

async function applyUpdate() {
  updateOverlay.visible = true
  updateOverlay.message = 'LocalMock is downloading the update and will restart.'
  try {
    const result = await api.startUpdate()
    const target = result.targetVersion ?? result.TargetVersion
    if (!(result.started ?? result.Started)) throw new Error(result.message ?? result.Message ?? 'Update could not be started.')
    updateOverlay.message = `Installing ${target ? `v${target}` : 'the update'}…`
    for (let attempt = 0; attempt < 45; attempt += 1) {
      await new Promise((resolve) => window.setTimeout(resolve, 2000))
      try {
        const version = await api.currentVersion()
        const current = version.currentVersion ?? version.CurrentVersion
        if (target && current === target) {
          window.location.reload()
          return
        }
      } catch { /* The local server is restarting. */ }
    }
    updateOverlay.message = 'The update is taking longer than expected. Reload this page in a moment.'
  } catch (error) {
    updateOverlay.visible = false
    toast(`Update failed: ${error.message}`, true)
  }
}

onMounted(() => {
  document.documentElement.dataset.theme = theme.value
  refresh()
  checkUpdate()
})
</script>

<template>
  <div class="app-shell">
    <div v-if="updateOverlay.visible" class="update-overlay" aria-live="assertive">
      <div class="update-overlay-card"><p class="update-overlay-title">Updating…</p><p class="update-overlay-message">{{ updateOverlay.message }}</p></div>
    </div>

    <header class="app-topbar">
      <div class="app-topbar-brand"><span class="app-topbar-eyebrow">Local Mock</span></div>
      <div class="app-topbar-context"><p class="app-topbar-page-eyebrow">Mocks</p><h1 class="app-topbar-page-title">Endpoint configuration</h1><p class="app-topbar-page-subtitle">Create, test, and manage mocked responses by collection or as standalone mocks.</p></div>
      <div class="app-topbar-actions">
        <button class="button button-secondary button-small" type="button" @click="importFile?.click()">Import JSON</button>
        <button class="button button-secondary button-small" type="button" @click="exportMocks">Export JSON</button>
        <button class="button button-secondary button-small" type="button" @click="refresh">Refresh list</button>
        <span class="app-version-label" title="Installed version">{{ appVersion }}</span>
        <button class="icon-button" type="button" :disabled="checkingUpdate" title="Check for a new version" aria-label="Check for a new version" @click="checkUpdate">↻</button>
        <button v-if="updateAvailable" class="button button-primary button-small" type="button" title="Apply the update" @click="applyUpdate">Update</button>
        <button class="icon-button" type="button" :title="theme === 'dark' ? 'Switch to light theme' : 'Switch to dark theme'" aria-label="Toggle theme" @click="applyTheme(theme === 'dark' ? 'light' : 'dark')">◐</button>
      </div>
    </header>

    <div class="app-content">
      <div id="screen-mocks" class="app-screen is-active" role="tabpanel">
        <CollectionTree
          :mocks="mocks" :collections="collections" :expanded-ids="expandedIds" :active-collection="activeCollection"
          :editing-key="editingKey" :search="search" :method-filter="methodFilter" :status-filter="statusFilter"
          :feedback="listFeedback" :feedback-error="listFeedbackError"
          @update:search="search = $event" @update:method-filter="methodFilter = $event" @update:status-filter="statusFilter = $event"
          @new-mock="resetMockForm" @new-collection="openNewCollection" @toggle-group="toggleGroup" @activate-collection="activateCollection"
          @edit-collection="openCollection" @select-mock="fillMock" @test-mock="useMockForTest"
          @toggle-mock="toggleEnabled($event, !$event.enabled)" @toggle-bypass="toggleBypass" @duplicate-mock="duplicateMock" @delete-mock="deleteMock"
        />
        <div class="app-main">
          <main class="workspace">
            <header class="workspace-heading">
              <div><p class="workspace-breadcrumb">{{ form.collection ? `Collections / ${form.collection}` : 'Mocks / No collection' }}</p><h1>{{ endpointTitle }}</h1><span class="save-state" :class="{ 'save-state--saved': editingKey && !dirty }">{{ editingKey ? (dirty ? 'Unsaved changes' : 'Saved') : 'New endpoint' }}</span></div>
              <div v-if="activeTab === 'configure'" class="workspace-heading-actions"><button class="button button-primary" type="submit" form="mock-form" :disabled="savingMock">{{ savingMock ? 'Saving…' : 'Save mock' }}</button></div>
            </header>
            <nav class="mock-view-tabs" role="tablist" aria-label="Endpoint workspace">
              <button class="mock-view-tab" :class="{ 'is-active': activeTab === 'configure' }" type="button" role="tab" :aria-selected="activeTab === 'configure'" @click="activeTab = 'configure'">Configure</button>
              <button class="mock-view-tab" :class="{ 'is-active': activeTab === 'test' }" type="button" role="tab" :aria-selected="activeTab === 'test'" @click="activeTab = 'test'">Test</button>
            </nav>
            <div class="workspace-panels">
              <MockEditor v-if="activeTab === 'configure'" :form="form" :dirty="dirty" :saving="savingMock" :enabled-busy="enabledBusy" :feedback="mockFeedback" :feedback-error="mockFeedbackError" @save="saveMock" @reset="resetMockForm" @dirty="dirty = true" @toggle-enabled="onFormEnabled" @copy="copyText" />
              <RequestTester v-else :form="testForm" :response="testResponse" :running="runningTest" :feedback="testFeedback" :feedback-error="testFeedbackError" @run="runTest" @reset="resetTest" @import-curl="openCurlImport" @copy="copyText" />
            </div>
          </main>
        </div>
      </div>

      <CollectionEditor
        v-show="collectionPaneOpen" :form="collectionForm" :editing="isCollectionEditing" :saving="collectionSaving" :count="collectionCount"
        :feedback="collectionFeedback" :feedback-error="collectionFeedbackError"
        :errors="collectionErrors"
        @save="saveCollection" @close="collectionPaneOpen = false" @reset="clearCollectionForm" @delete="deleteCollection"
        @manage="manageCollectionMocks" @copy="copyText" @field-input="onCollectionFieldInput"
      />
    </div>

    <CurlImportModal v-model:command="curlCommand" :open="curlOpen" :error="curlError" @close="curlOpen = false" @apply="applyCurlImport" />
    <div class="toast-container" aria-live="polite" aria-atomic="true"><div v-for="item in toasts" :key="item.id" class="toast" :class="item.error ? 'toast--error' : 'toast--success'"><span class="toast-message">{{ item.message }}</span></div></div>
    <input ref="importFile" type="file" accept=".json,application/json" hidden @change="importMocks">
  </div>
</template>
