<script setup>
import { computed } from 'vue'
import { collectionId, entryKey, normalizeEntry } from '../domain.js'
import { useLocale } from '../useLocale.js'
const { t } = useLocale()

const props = defineProps({
  mocks: { type: Array, default: () => [] },
  collections: { type: Array, default: () => [] },
  expandedIds: { type: Set, required: true },
  activeCollection: { type: String, default: '' },
  editingKey: { type: String, default: '' },
  search: { type: String, default: '' },
  methodFilter: { type: String, default: '' },
  statusFilter: { type: String, default: '' },
  feedback: { type: String, default: '' },
  feedbackError: { type: Boolean, default: false },
})

const emit = defineEmits([
  'update:search', 'update:methodFilter', 'update:statusFilter', 'new-mock', 'new-collection',
  'activate-collection', 'toggle-group', 'edit-collection', 'select-mock', 'test-mock',
  'toggle-mock', 'toggle-bypass', 'duplicate-mock', 'delete-mock',
])

const groups = computed(() => {
  const visible = props.mocks.filter((entry) => {
    const mock = normalizeEntry(entry)
    const search = props.search.trim().toLowerCase()
    const status = String(mock.statusCode).charAt(0)
    return (!props.methodFilter || mock.method === props.methodFilter) &&
      (!props.statusFilter || status === props.statusFilter.charAt(0)) &&
      (!search || mock.path.toLowerCase().includes(search) || mock.method.toLowerCase().includes(search) || mock.collection.toLowerCase().includes(search))
  })
  const uncollected = props.mocks.filter((entry) => !collectionId(entry))
  const collections = [...props.collections]
    .sort((a, b) => getCollectionId(a).localeCompare(getCollectionId(b)))
    .map((collection) => {
      const id = getCollectionId(collection)
      return {
        id,
        label: id,
        collection,
        count: props.mocks.filter((entry) => collectionId(entry) === id).length,
        entries: visible.filter((entry) => collectionId(entry) === id),
      }
    })
  return [{ id: '', label: 'No collection', count: uncollected.length, entries: visible.filter((entry) => !collectionId(entry)) }, ...collections]
})

function getCollectionId(collection) {
  return String(collection.id ?? collection.Id ?? '')
}

function methodClass(method) {
  return `method-badge--${method.toLowerCase()}`
}

function statusClass(code) {
  return `status-badge--${Math.floor(code / 100)}xx`
}

function statusLabel(code) {
  const labels = { 200: 'statusOk', 201: 'statusCreated', 202: 'statusAccepted', 204: 'statusNoContent', 400: 'statusBadRequest', 401: 'statusUnauthorized', 403: 'statusForbidden', 404: 'statusNotFound', 409: 'statusConflict', 422: 'statusUnprocessable', 500: 'statusInternalError', 502: 'statusBadGateway', 503: 'statusUnavailable' }
  return labels[code] ? `${code} ${t(labels[code])}` : String(code)
}
</script>

<template>
  <aside class="mocks-sidebar" aria-labelledby="mocks-sidebar-title">
    <div class="mocks-sidebar-header">
      <div>
      <h2 id="mocks-sidebar-title" class="mocks-sidebar-title">{{ t('endpoints') }}</h2>
        <p class="mocks-sidebar-subtitle">{{ t('selectEndpoint') }}</p>
      </div>
      <button class="button button-primary button-small" type="button" :title="t('newEndpoint')" :aria-label="t('newEndpoint')" @click="emit('new-mock')">+</button>
    </div>

    <div class="collection-tree-heading">
      <span>{{ t('collections') }}</span>
      <button class="icon-button" type="button" :title="t('newCollection')" :aria-label="t('newCollection')" @click="emit('new-collection')">+</button>
    </div>

    <div class="mocks-sidebar-toolbar">
      <input :value="search" type="search" class="sidebar-search" :placeholder="t('searchPath')" :aria-label="t('searchMocks')" @input="emit('update:search', $event.target.value)">
      <div class="sidebar-filters">
        <select :value="methodFilter" class="sidebar-filter" aria-label="Filter by method" @change="emit('update:methodFilter', $event.target.value)">
          <option value="">{{ t('method') }}</option><option>GET</option><option>POST</option><option>PUT</option><option>PATCH</option><option>DELETE</option>
        </select>
        <select :value="statusFilter" class="sidebar-filter" aria-label="Filter by status" @change="emit('update:statusFilter', $event.target.value)">
          <option value="">{{ t('status') }}</option><option value="2xx">2xx</option><option value="3xx">3xx</option><option value="4xx">4xx</option><option value="5xx">5xx</option>
        </select>
      </div>
    </div>

    <div class="mock-list" role="list" aria-live="polite">
      <section v-for="group in groups" :key="group.id || '__none__'" class="collection-tree-group" :class="{ 'is-current': activeCollection === group.id, 'is-collapsed': !expandedIds.has(group.id) }">
        <div class="collection-tree-header">
          <button class="collection-tree-toggle" type="button" :aria-label="`${expandedIds.has(group.id) ? t('collapse') : t('expand')} ${group.label}`" :aria-expanded="expandedIds.has(group.id)" @click="emit('toggle-group', group.id)"></button>
          <button class="collection-tree-select" :class="{ 'is-current': activeCollection === group.id }" type="button" @click="emit('activate-collection', group.id)">
            <span class="collection-tree-name">{{ group.label || t('noCollection') }}</span><span class="collection-tree-count">{{ group.count }}</span>
          </button>
          <button class="icon-button collection-tree-add" type="button" :title="`${t('newEndpoint')} · ${group.label || t('noCollection')}`" :aria-label="`${t('newEndpoint')} · ${group.label || t('noCollection')}`" @click="emit('new-mock', group.id)">+</button>
          <button v-if="group.collection" class="icon-button collection-tree-settings" type="button" :title="t('configureCollection')" :aria-label="`${t('configureCollection')} · ${group.label}`" @click="emit('edit-collection', group.collection)">⚙</button>
        </div>

        <div class="collection-tree-items">
          <article v-for="raw in group.entries" :key="entryKey(raw)" class="mock-list-item" :class="{ 'is-active': editingKey === entryKey(raw) }" role="listitem" tabindex="0" @click="emit('select-mock', normalizeEntry(raw))" @keydown.enter="emit('select-mock', normalizeEntry(raw))" @keydown.space.prevent="emit('select-mock', normalizeEntry(raw))">
            <div class="mock-list-item-head">
              <span class="method-badge" :class="methodClass(normalizeEntry(raw).method)">{{ normalizeEntry(raw).method }}</span>
              <span class="mock-list-item-path" :title="normalizeEntry(raw).path">{{ normalizeEntry(raw).path }}</span>
            </div>
            <div class="mock-list-item-meta">
              <span class="status-badge" :class="statusClass(normalizeEntry(raw).statusCode)">{{ statusLabel(normalizeEntry(raw).statusCode) }}</span>
              <span v-if="normalizeEntry(raw).bypassEnabled" class="bypass-pill bypass-pill--yes">{{ t('bypassOrigin') }}</span>
              <span v-if="normalizeEntry(raw).collection && !normalizeEntry(raw).enabled" class="bypass-pill bypass-pill--inactive">{{ t('inactive') }}</span>
            </div>
            <div class="mock-list-item-actions" @click.stop>
              <button class="icon-button endpoint-action" type="button" :title="t('test')" :aria-label="t('test')" @click="emit('test-mock', normalizeEntry(raw))">▶</button>
              <button class="icon-button endpoint-action" type="button" :title="t('edit')" :aria-label="t('edit')" @click="emit('select-mock', normalizeEntry(raw))">✎</button>
              <details class="mock-actions-menu">
                <summary class="icon-button" :aria-label="t('actions')" :title="t('actions')">⋯</summary>
                <div class="mock-actions-popover">
                  <button v-if="normalizeEntry(raw).collection" type="button" @click="emit('toggle-mock', normalizeEntry(raw)); $event.currentTarget.closest('details').open = false">{{ normalizeEntry(raw).enabled ? t('disableMock') : t('enableMock') }}</button>
                  <button v-else type="button" @click="emit('toggle-bypass', normalizeEntry(raw)); $event.currentTarget.closest('details').open = false">{{ normalizeEntry(raw).bypassEnabled ? t('disableBypass') : t('enableBypass') }}</button>
                  <button type="button" @click="emit('duplicate-mock', normalizeEntry(raw)); $event.currentTarget.closest('details').open = false">{{ t('duplicate') }}</button>
                  <button class="is-danger" type="button" @click="emit('delete-mock', normalizeEntry(raw)); $event.currentTarget.closest('details').open = false">{{ t('delete') }}</button>
                </div>
              </details>
            </div>
          </article>
          <p v-if="!group.entries.length && group.count === 0" class="collection-tree-empty">{{ t('noEndpoints') }}</p>
        </div>
      </section>
      <p v-if="feedback" class="sidebar-feedback" :class="{ 'feedback--error': feedbackError }" aria-live="polite">{{ feedback }}</p>
      <p v-if="mocks.length && groups.every((group) => !group.entries.length)" class="mock-list-empty">{{ t('noMatches') }}</p>
    </div>
  </aside>
</template>
