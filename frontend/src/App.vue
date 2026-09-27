<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import { api } from './api.js'
import {
  buildMockUrl, collectionId, entryKey, getBypassEnabled,
  normalizeEntry, normalizePath, parseCurl,
  parseResponseBody, responseBodyText, validHttpUrl,
} from './domain.js'
import CollectionTree from './components/CollectionTree.vue'
import MockEditor from './components/MockEditor.vue'
import RequestTester from './components/RequestTester.vue'
import CollectionEditor from './components/CollectionEditor.vue'
import CurlImportModal from './components/CurlImportModal.vue'
import ImportExportDialog from './components/ImportExportDialog.vue'
import { useLocale } from './useLocale.js'
import { useImportExport } from './composables/useImportExport.js'
import { useRequestTester } from './composables/useRequestTester.js'
import { useUpdateManager } from './composables/useUpdateManager.js'

const savedCollection = localStorage.getItem('localmock-active-collection') || ''
const { locale, t, setLocale } = useLocale()
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
const test = useRequestTester({ api, activeCollection, t })
const testForm = test.form
const testResponse = test.response
const runningTest = test.running
const testFeedback = test.feedback
const testFeedbackError = test.feedbackError
const search = ref('')
const methodFilter = ref('')
const statusFilter = ref('')
const listFeedback = ref('')
const listFeedbackError = ref(false)
const mobileSidebarOpen = ref(false)
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
const toasts = ref([])
const updates = useUpdateManager({ api, t, toast })
const { appVersion, updateAvailable, checking: checkingUpdate, overlay: updateOverlay } = updates
const transfer = useImportExport({ api, currentMocks: mocks, currentCollections: collections, refresh, toast, t })
const { open: transferOpen, mode: transferMode, collections: transferCollections, mocks: transferMocks, working: transferWorking, error: transferError, fileInput: importFile } = transfer

const isCollectionEditing = computed(() => Boolean(collectionEditingId.value))
const endpointTitle = computed(() => form.path ? `${form.method} ${normalizePath(form.path)}` : t('newEndpoint'))
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
    toast(t('copied'))
  } catch {
    toast(t('clipboardError'), true)
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
    listFeedback.value = t('loadEndpointsFailed', { message: error.message })
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
  mobileSidebarOpen.value = false
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
  mobileSidebarOpen.value = false
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
  test.useMock(entry)
  activeTab.value = 'test'
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
    listFeedback.value = enabled ? t('mockEnabled') : t('mockDisabled')
    listFeedbackError.value = false
  } catch (error) {
    listFeedback.value = t('updateMockFailed', { message: error.message })
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
    toast(t('updateMockFailed', { message: error.message }), true)
  } finally {
    enabledBusy.value = false
  }
}

async function toggleBypass(entry) {
  const enabled = !getBypassEnabled(entry)
  if (enabled && !validHttpUrl(entry.bypassUrl)) {
    fillMock(entry)
    mockFeedback.value = t('validBypassBeforeEnable')
    mockFeedbackError.value = true
    return
  }
  try {
    await api.setBypass(entry, enabled, entry.bypassUrl)
    await refresh()
    toast(enabled ? t('bypassEnabled') : t('bypassDisabled'))
  } catch (error) {
    toast(t('updateBypassFailed', { message: error.message }), true)
  }
}

async function saveMock() {
  const path = normalizePath(form.path)
  const statusCode = Number(form.statusCode)
  const responseDelayMs = Number(form.responseDelayMs || 0)
  if (!form.path.trim()) return showMockError(t('endpointPathRequired'))
  if (!form.collection && form.bypassEnabled && !validHttpUrl(form.bypassUrl)) return showMockError(t('validBypassRequired'))
  if (!Number.isInteger(statusCode) || statusCode < 100 || statusCode > 599) return showMockError(t('validStatusRequired'))
  if (!Number.isFinite(responseDelayMs) || responseDelayMs < 0) return showMockError(t('validDelayRequired'))
  let responseBody
  try { responseBody = parseResponseBody(form.responseBody.trim(), form.responseContentType) }
  catch (error) { return showMockError(t('invalidResponseBody', { message: error.message })) }

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
    mockFeedback.value = t('mockSaved')
  } catch (error) {
    showMockError(t('saveMockFailed', { message: error.message }))
  } finally {
    savingMock.value = false
  }
}

function showMockError(message) {
  mockFeedback.value = message
  mockFeedbackError.value = true
}

async function deleteMock(entry) {
  if (!window.confirm(t('confirmDeleteMock', { method: entry.method, path: entry.path }))) return
  try {
    await api.removeMock(entry)
    if (entryKey(entry) === editingKey.value) resetMockForm()
    await refresh()
    toast(t('mockDeleted'))
  } catch (error) {
    toast(t('deleteMockFailed', { message: error.message }), true)
  }
}

const runTest = test.run
const resetTest = test.reset

function openNewCollection() {
  mobileSidebarOpen.value = false
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

  if (!id) collectionErrors.id = t('collectionIdRequired')
  else if (!/^[a-zA-Z0-9_-]+$/.test(id)) collectionErrors.id = t('collectionIdFormat')
  else if (['collections', 'bypass', 'enabled'].includes(id.toLowerCase())) collectionErrors.id = t('collectionIdReserved')
  else if (!isCollectionEditing.value && collections.value.some((item) => collectionLabel(item) === id)) collectionErrors.id = t('collectionIdExists')

  if (!bypassUrl) collectionErrors.bypassUrl = t('baseUrlRequired')
  else if (!validHttpUrl(bypassUrl)) collectionErrors.bypassUrl = t('validHttpUrlRequired')

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
    collectionFeedback.value = t('collectionSaved')
    collectionFeedbackError.value = false
  } catch (error) {
    collectionFeedback.value = t('saveCollectionFailed', { message: error.message })
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
  if (!id || !window.confirm(t('confirmDeleteCollection', { id }))) return
  try {
    await api.removeCollection(id)
    collectionPaneOpen.value = false
    if (activeCollection.value === id) activeCollection.value = ''
    if (form.collection === id) resetMockForm()
    await refresh()
    toast(t('collectionDeleted'))
  } catch (error) {
    collectionFeedback.value = t('deleteCollectionFailed', { message: error.message })
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
    testFeedback.value = t('importedCurl')
    testFeedbackError.value = false
  } catch (error) {
    curlError.value = error.message
  }
}

onMounted(() => {
  document.documentElement.dataset.theme = theme.value
  setLocale(locale.value)
  refresh()
  updates.check()
})
</script>

<template>
  <div class="app-shell">
      <div v-if="updateOverlay.visible" class="update-overlay" aria-live="assertive">
      <div class="update-overlay-card"><p class="update-overlay-title">{{ t('updating') }}</p><p class="update-overlay-message">{{ updateOverlay.message }}</p></div>
    </div>

    <header class="app-topbar">
      <div class="app-topbar-brand"><span class="app-topbar-eyebrow">Local Mock</span></div>
      <div class="app-topbar-context"><p class="app-topbar-page-eyebrow">{{ t('mocks') }}</p><h1 class="app-topbar-page-title">{{ t('endpointConfiguration') }}</h1><p class="app-topbar-page-subtitle">{{ t('endpointDescription') }}</p></div>
      <div class="app-topbar-actions">
        <button class="button button-secondary button-small mobile-endpoints-toggle" type="button" :aria-expanded="mobileSidebarOpen" @click="mobileSidebarOpen = !mobileSidebarOpen">{{ t('endpoints') }}</button>
        <button class="button button-secondary button-small" type="button" @click="transfer.openImport">{{ t('importJson') }}</button>
        <button class="button button-secondary button-small" type="button" @click="transfer.openExport">{{ t('exportJson') }}</button>
        <span class="app-version-label" :title="t('installedVersion')">{{ appVersion }}</span>
        <button class="icon-button" type="button" :disabled="checkingUpdate" :title="t('checkForUpdates')" :aria-label="t('checkForUpdates')" @click="updates.check">↻</button>
        <button v-if="updateAvailable" class="button button-primary button-small" type="button" :title="t('applyUpdate')" @click="updates.apply">{{ t('applyUpdate') }}</button>
        <label class="locale-picker"><span>{{ t('language') }}</span><select :value="locale" :aria-label="t('language')" @change="setLocale($event.target.value)"><option value="pt-BR">{{ t('languagePortuguese') }}</option><option value="en">{{ t('languageEnglish') }}</option></select></label>
        <button class="icon-button" type="button" :title="t('toggleTheme')" :aria-label="t('toggleTheme')" @click="applyTheme(theme === 'dark' ? 'light' : 'dark')">◐</button>
      </div>
    </header>

    <div class="app-content">
      <div id="screen-mocks" class="app-screen is-active" role="tabpanel">
        <CollectionTree
          :class="{ 'is-mobile-open': mobileSidebarOpen }"
          :mocks="mocks" :collections="collections" :expanded-ids="expandedIds" :active-collection="activeCollection"
          :editing-key="editingKey" :search="search" :method-filter="methodFilter" :status-filter="statusFilter"
          :feedback="listFeedback" :feedback-error="listFeedbackError"
          @update:search="search = $event" @update:method-filter="methodFilter = $event" @update:status-filter="statusFilter = $event"
          @new-mock="resetMockForm" @new-collection="openNewCollection" @toggle-group="toggleGroup" @activate-collection="activateCollection($event); mobileSidebarOpen = false"
          @edit-collection="openCollection" @select-mock="fillMock" @test-mock="useMockForTest"
          @toggle-mock="toggleEnabled($event, !$event.enabled)" @toggle-bypass="toggleBypass" @duplicate-mock="duplicateMock" @delete-mock="deleteMock"
        />
        <div class="app-main">
          <main class="workspace">
            <header class="workspace-heading">
              <div><p class="workspace-breadcrumb">{{ form.collection ? `${t('collections')} / ${form.collection}` : `${t('mocks')} / ${t('noCollection')}` }}</p><h1>{{ endpointTitle }}</h1><span class="save-state" :class="{ 'save-state--saved': editingKey && !dirty }">{{ editingKey ? (dirty ? t('unsavedChanges') : t('saved')) : t('newEndpoint') }}</span></div>
            </header>
            <nav class="mock-view-tabs" role="tablist" aria-label="Endpoint workspace">
              <button class="mock-view-tab" :class="{ 'is-active': activeTab === 'configure' }" type="button" role="tab" :aria-selected="activeTab === 'configure'" @click="activeTab = 'configure'">{{ t('configure') }}</button>
              <button class="mock-view-tab" :class="{ 'is-active': activeTab === 'test' }" type="button" role="tab" :aria-selected="activeTab === 'test'" @click="activeTab = 'test'">{{ t('test') }}</button>
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
    <ImportExportDialog :open="transferOpen" :mode="transferMode" :collections="transferCollections" :mocks="transferMocks" :current-collections="collections" :current-mocks="mocks" :working="transferWorking" :error="transferError" @close="transfer.close" @confirm="transfer.confirm" />
    <input ref="importFile" type="file" accept=".json,application/json" hidden @change="transfer.readFile">
  </div>
</template>
