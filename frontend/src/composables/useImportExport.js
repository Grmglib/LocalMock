import { computed, ref } from 'vue'
import { entryKey, normalizeEntry, toFileEntry, validHttpUrl } from '../domain.js'

function collectionId(item) { return String(item.id ?? item.Id ?? '').trim() }
function collectionUrl(item) { return String(item.bypassUrl ?? item.BypassUrl ?? '') }

function parseImport(text, currentCollections, currentMocks) {
  const data = JSON.parse(text)
  if (!data || typeof data !== 'object') throw new Error('Invalid import file.')
  const collections = Array.isArray(data) ? [] : (data.Collections ?? data.collections ?? [])
  const mocks = Array.isArray(data) ? data : (data.Mocks ?? data.mocks ?? [])
  if (!Array.isArray(collections) || !Array.isArray(mocks)) throw new Error('Invalid import file.')
  const parsedCollections = collections.map((item) => ({ id: collectionId(item), bypassUrl: collectionUrl(item) }))
  const parsedMocks = mocks.map(normalizeEntry)
  const collectionCounts = new Map()
  for (const item of parsedCollections) collectionCounts.set(item.id.toLowerCase(), (collectionCounts.get(item.id.toLowerCase()) || 0) + 1)
  const mockCounts = new Map()
  for (const item of parsedMocks) mockCounts.set(entryKey(item), (mockCounts.get(entryKey(item)) || 0) + 1)
  for (const item of parsedCollections) {
    item.invalid = !/^[a-zA-Z0-9_-]+$/.test(item.id) || ['collections', 'bypass', 'enabled'].includes(item.id.toLowerCase()) || !validHttpUrl(item.bypassUrl) || collectionCounts.get(item.id.toLowerCase()) > 1
  }
  const supportedMethods = ['GET', 'POST', 'PUT', 'PATCH', 'DELETE']
  for (const item of parsedMocks) {
    const missingCollection = item.collection &&
      !currentCollections.some((collection) => collectionId(collection).toLowerCase() === item.collection.toLowerCase()) &&
      !parsedCollections.some((collection) => collection.id.toLowerCase() === item.collection.toLowerCase() && !collection.invalid)
    item.invalid = !supportedMethods.includes(item.method) || item.path === '/' || !Number.isInteger(item.statusCode) || item.statusCode < 100 || item.statusCode > 599 || item.responseDelayMs < 0 || Boolean(missingCollection) || mockCounts.get(entryKey(item)) > 1
  }
  return { collections: parsedCollections, mocks: parsedMocks }
}

export function useImportExport({ api, currentMocks, currentCollections, refresh, toast, t }) {
  const open = ref(false)
  const mode = ref('import')
  const collections = ref([])
  const mocks = ref([])
  const working = ref(false)
  const error = ref('')
  const fileInput = ref(null)
  const currentMocksList = computed(() => currentMocks.value)
  const currentCollectionsList = computed(() => currentCollections.value)

  function openImport() {
    error.value = ''
    mode.value = 'import'
    fileInput.value?.click()
  }

  function openExport() {
    error.value = ''
    mode.value = 'export'
    collections.value = currentCollectionsList.value.map((item) => ({ ...item }))
    mocks.value = currentMocksList.value.map(normalizeEntry)
    open.value = true
  }

  async function readFile(event) {
    const file = event.target.files?.[0]
    event.target.value = ''
    if (!file) return
    try {
      const parsed = parseImport(await file.text(), currentCollectionsList.value, currentMocksList.value)
      if (!parsed.collections.length && !parsed.mocks.length) throw new Error('Empty import file.')
      collections.value = parsed.collections
      mocks.value = parsed.mocks
      error.value = ''
      open.value = true
    } catch {
      toast(t('invalidJson'), true)
    }
  }

  function exportSelection(selection) {
    const payload = {
      Collections: selection.collections.map((item) => ({ Id: collectionId(item), BypassUrl: collectionUrl(item) })),
      Mocks: selection.mocks.map((item) => toFileEntry(item.mock ?? item)),
    }
    const url = URL.createObjectURL(new Blob([JSON.stringify(payload, null, 2)], { type: 'application/json' }))
    const anchor = document.createElement('a')
    anchor.href = url
    anchor.download = 'localmock-export.json'
    anchor.click()
    window.setTimeout(() => URL.revokeObjectURL(url), 0)
    open.value = false
  }

  async function importSelection(selection) {
    working.value = true
    error.value = ''
    try {
      const result = await api.importBatch({
        collections: selection.collections.map((item) => ({
          id: collectionId(item),
          bypassUrl: collectionUrl(item),
          overwrite: Boolean(item.overwrite),
        })),
        mocks: selection.mocks.map(({ mock, overwrite }) => ({
          overwrite: Boolean(overwrite),
          mock: {
            collection: mock.collection || null,
            method: mock.method,
            path: mock.path,
            statusCode: mock.statusCode,
            responseDelayMs: mock.responseDelayMs,
            responseContentType: mock.responseContentType,
            responseBody: mock.responseBody,
            enabled: mock.enabled,
            bypassEnabled: mock.bypassEnabled,
            bypassUrl: mock.bypassUrl || null,
          },
        })),
      })
      await refresh()
      toast(t('importSucceeded', {
        added: result.collectionsAdded + result.mocksAdded,
        updated: result.collectionsUpdated + result.mocksUpdated,
      }))
      open.value = false
    } catch (cause) {
      error.value = t('importFailed', { message: cause.message })
    } finally {
      working.value = false
    }
  }

  function confirm(selection) {
    if (mode.value === 'export') exportSelection(selection)
    else importSelection(selection)
  }

  return { open, mode, collections, mocks, working, error, fileInput, openImport, openExport, readFile, confirm, close: () => { open.value = false } }
}
