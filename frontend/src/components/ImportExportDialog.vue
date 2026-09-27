<script setup>
import { computed, ref, watch } from 'vue'
import { entryKey, normalizeEntry } from '../domain.js'
import { useLocale } from '../useLocale.js'

const props = defineProps({
  open: Boolean,
  mode: { type: String, default: 'import' },
  collections: { type: Array, default: () => [] },
  mocks: { type: Array, default: () => [] },
  currentCollections: { type: Array, default: () => [] },
  currentMocks: { type: Array, default: () => [] },
  working: Boolean,
  error: String,
})
const emit = defineEmits(['close', 'confirm'])
const { t } = useLocale()
const selectedCollections = ref(new Set())
const selectedMocks = ref(new Set())
const replaceCollections = ref(new Set())
const replaceMocks = ref(new Set())

function idOf(collection) { return String(collection.id ?? collection.Id ?? '').trim() }
function urlOf(collection) { return String(collection.bypassUrl ?? collection.BypassUrl ?? '') }
function currentCollection(id) { return props.currentCollections.find((item) => idOf(item).toLowerCase() === id.toLowerCase()) }
function currentMock(entry) { return props.currentMocks.find((item) => entryKey(item) === entryKey(entry)) }
function entriesFor(id) { return props.mocks.filter((item) => normalizeEntry(item).collection === id) }
function invalid(item) { return Boolean(item.invalid) }

watch(() => [props.open, props.mode, props.collections, props.mocks], () => {
  if (!props.open) return
  selectedCollections.value = new Set(props.collections.filter((item) => !invalid(item) && !currentCollection(idOf(item))).map(idOf))
  selectedMocks.value = new Set(props.mocks.filter((item) => !invalid(item) && !currentMock(item)).map(entryKey))
  replaceCollections.value = new Set()
  replaceMocks.value = new Set()
  if (props.mode === 'export') {
    selectedCollections.value = new Set(props.collections.filter((item) => !invalid(item)).map(idOf))
    selectedMocks.value = new Set(props.mocks.filter((item) => !invalid(item)).map(entryKey))
  }
}, { immediate: true })

const selectedCount = computed(() => selectedCollections.value.size + selectedMocks.value.size)
const totalCount = computed(() => props.collections.length + props.mocks.length)
const unresolvedConflict = computed(() =>
  props.mode === 'import' && (
    props.mocks.some((item) => selectedMocks.value.has(entryKey(item)) && currentMock(item) && !replaceMocks.value.has(entryKey(item))) ||
    props.collections.some((item) => selectedCollections.value.has(idOf(item)) && collectionStatus(item) === t('conflictItem') && !replaceCollections.value.has(idOf(item)))
  ),
)

function setGroup(id, checked) {
  const nextCollections = new Set(selectedCollections.value)
  const nextMocks = new Set(selectedMocks.value)
  if (checked && (props.mode === 'export' || !currentCollection(id))) nextCollections.add(id)
  else nextCollections.delete(id)
  for (const mock of entriesFor(id).filter((item) => !invalid(item) && (props.mode === 'export' || !currentMock(item)))) {
    const key = entryKey(mock)
    if (checked) nextMocks.add(key)
    else nextMocks.delete(key)
  }
  selectedCollections.value = nextCollections
  selectedMocks.value = nextMocks
}

function setCollection(id, checked) {
  const next = new Set(selectedCollections.value)
  checked ? next.add(id) : next.delete(id)
  selectedCollections.value = next
}

function setMock(mock, checked) {
  const next = new Set(selectedMocks.value)
  const key = entryKey(mock)
  checked ? next.add(key) : next.delete(key)
  const id = normalizeEntry(mock).collection
  if (checked && id && !currentCollection(id)) {
    const available = props.collections.find((item) => idOf(item) === id)
    if (available) setCollection(id, true)
  }
  selectedMocks.value = next
}

function selectAll(checked) {
  selectedCollections.value = new Set(checked ? props.collections.filter((item) => !invalid(item) && (props.mode === 'export' || !currentCollection(idOf(item)))).map(idOf) : [])
  selectedMocks.value = new Set(checked ? props.mocks.filter((item) => !invalid(item) && (props.mode === 'export' || !currentMock(item))).map(entryKey) : [])
}

function toggleReplaceMock(mock, checked) {
  const next = new Set(replaceMocks.value)
  const key = entryKey(mock)
  checked ? next.add(key) : next.delete(key)
  replaceMocks.value = next
  if (checked) setMock(mock, true)
}

function toggleReplaceCollection(id, checked) {
  const next = new Set(replaceCollections.value)
  checked ? next.add(id) : next.delete(id)
  replaceCollections.value = next
  if (checked) setCollection(id, true)
}

function collectionStatus(collection) {
  if (invalid(collection)) return t('invalidItem')
  const id = idOf(collection)
  const current = currentCollection(id)
  if (!current) return t('newItem')
  return urlOf(current) === urlOf(collection) ? t('existingItem') : t('conflictItem')
}

function mockStatus(mock) { return invalid(mock) ? t('invalidItem') : props.mode === 'import' && currentMock(mock) ? t('conflictItem') : props.mode === 'export' ? t('includedItem') : t('newItem') }

function submit() {
  const selectedMocksList = props.mocks.filter((item) => selectedMocks.value.has(entryKey(item)) && !invalid(item) && (!currentMock(item) || replaceMocks.value.has(entryKey(item))))
  const requiredIds = new Set(selectedMocksList.map((item) => normalizeEntry(item).collection).filter(Boolean))
  const collectionsList = props.collections.filter((item) => {
    const id = idOf(item)
    return props.mode === 'import'
      ? selectedCollections.value.has(id) && !invalid(item) && (!currentCollection(id) || replaceCollections.value.has(id))
      : (selectedCollections.value.has(id) || requiredIds.has(id)) && !invalid(item)
  })
  const mocksList = selectedMocksList.map((item) => ({
    mock: normalizeEntry(item),
    overwrite: replaceMocks.value.has(entryKey(item)),
  }))
  emit('confirm', {
    collections: collectionsList.map((item) => ({ ...item, overwrite: replaceCollections.value.has(idOf(item)) })),
    mocks: mocksList,
  })
}
</script>

<template>
  <div v-if="open" class="modal import-export-modal" role="dialog" aria-modal="true" aria-labelledby="import-export-title" @keydown.esc="emit('close')">
    <div class="modal-backdrop" @click="emit('close')"></div>
    <section class="modal-content" @click.stop>
      <header class="modal-header">
        <div>
          <h2 id="import-export-title">{{ mode === 'import' ? t('importSelection') : t('exportSelection') }}</h2>
          <p class="modal-description">{{ mode === 'import' ? t('importHelp') : t('exportHelp') }}</p>
        </div>
        <button class="icon-button modal-close" type="button" :aria-label="t('close')" @click="emit('close')">×</button>
      </header>
      <div class="import-export-toolbar">
        <span>{{ t('selectedCount', { count: selectedCount }) }}</span>
        <div>
          <button class="button button-secondary button-small" type="button" @click="selectAll(true)">{{ t('selectAll') }}</button>
          <button class="button button-secondary button-small" type="button" @click="selectAll(false)">{{ t('clearSelection') }}</button>
        </div>
      </div>
      <div class="modal-body import-export-list" role="list">
        <section v-for="collection in collections" :key="idOf(collection)" class="import-export-group">
          <header class="import-export-group-heading">
            <label class="import-export-item">
              <input type="checkbox" :checked="selectedCollections.has(idOf(collection))" :disabled="invalid(collection) || (mode === 'import' && currentCollection(idOf(collection)) && collectionStatus(collection) !== t('conflictItem') && !entriesFor(idOf(collection)).some((mock) => !currentMock(mock)))" @change="setGroup(idOf(collection), $event.target.checked)">
              <strong>{{ idOf(collection) || t('noCollection') }}</strong>
              <span class="import-export-status">{{ mode === 'export' ? t('includedItem') : collectionStatus(collection) }}</span>
            </label>
            <label v-if="mode === 'import' && collectionStatus(collection) === t('conflictItem')" class="import-export-replace">
              <input type="checkbox" :checked="replaceCollections.has(idOf(collection))" @change="toggleReplaceCollection(idOf(collection), $event.target.checked)">
              {{ t('replaceCollection') }}
            </label>
          </header>
          <label v-for="mock in entriesFor(idOf(collection))" :key="entryKey(mock)" class="import-export-item import-export-mock">
            <input type="checkbox" :checked="selectedMocks.has(entryKey(mock))" :disabled="invalid(mock)" @change="setMock(mock, $event.target.checked)">
            <span class="method-badge" :class="`method-badge--${normalizeEntry(mock).method.toLowerCase()}`">{{ normalizeEntry(mock).method }}</span>
            <code>{{ normalizeEntry(mock).path }}</code>
            <span class="import-export-status">{{ mockStatus(mock) }}</span>
            <span v-if="mode === 'import' && currentMock(mock)" class="import-export-replace" @click.stop>
              <input type="checkbox" :checked="replaceMocks.has(entryKey(mock))" :aria-label="t('replaceExisting')" @change="toggleReplaceMock(mock, $event.target.checked)">
              {{ t('replaceExisting') }}
            </span>
          </label>
        </section>
        <section v-if="mocks.some((mock) => !normalizeEntry(mock).collection)" class="import-export-group">
          <header class="import-export-group-heading"><strong>{{ t('noCollection') }}</strong></header>
          <label v-for="mock in mocks.filter((item) => !normalizeEntry(item).collection)" :key="entryKey(mock)" class="import-export-item import-export-mock">
            <input type="checkbox" :checked="selectedMocks.has(entryKey(mock))" :disabled="invalid(mock)" @change="setMock(mock, $event.target.checked)">
            <span class="method-badge" :class="`method-badge--${normalizeEntry(mock).method.toLowerCase()}`">{{ normalizeEntry(mock).method }}</span>
            <code>{{ normalizeEntry(mock).path }}</code>
            <span class="import-export-status">{{ mockStatus(mock) }}</span>
            <span v-if="mode === 'import' && currentMock(mock)" class="import-export-replace" @click.stop>
              <input type="checkbox" :checked="replaceMocks.has(entryKey(mock))" :aria-label="t('replaceExisting')" @change="toggleReplaceMock(mock, $event.target.checked)">
              {{ t('replaceExisting') }}
            </span>
          </label>
        </section>
        <p v-if="error" class="feedback feedback--error" role="alert">{{ error }}</p>
        <p v-if="totalCount === 0" class="empty-state-desc">{{ mode === 'import' ? t('noImportItems') : t('noExportItems') }}</p>
      </div>
      <footer class="modal-footer import-export-footer">
        <button class="button button-secondary" type="button" :disabled="working" @click="emit('close')">{{ t('cancel') }}</button>
        <button class="button button-primary" type="button" :disabled="working || selectedCount === 0 || unresolvedConflict" @click="submit">{{ working ? t('running') : mode === 'import' ? t('confirmImport') : t('downloadExport') }}</button>
      </footer>
    </section>
  </div>
</template>
