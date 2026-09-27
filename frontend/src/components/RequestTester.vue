<script setup>
import { computed } from 'vue'
import { buildMockUrl } from '../domain.js'

const props = defineProps({
  form: { type: Object, required: true },
  response: { type: Object, required: true },
  running: { type: Boolean, default: false },
  feedback: { type: String, default: '' },
  feedbackError: { type: Boolean, default: false },
})
const emit = defineEmits(['run', 'reset', 'import-curl', 'copy'])

const requestUrl = computed(() => buildMockUrl(props.form.path, props.form.collection))
const bodyPlaceholder = computed(() => {
  if (props.form.contentType === 'application/json') return '{\n  "id": 1\n}'
  if (props.form.contentType === 'application/x-www-form-urlencoded' || props.form.contentType === 'multipart/form-data') return 'customerId=1\nname=Maria'
  if (props.form.contentType.includes('xml')) return '<request>\n  <id>1</id>\n</request>'
  return 'Request body'
})
const bodyLines = computed(() => Math.max(1, String(props.form.body || '').split('\n').length))
</script>

<template>
  <section class="panel mock-test-panel">
    <header class="panel-head">
      <div class="panel-head-icon panel-head-icon--test" aria-hidden="true">▶</div>
      <div><h2 class="panel-title">Test request</h2><p class="panel-subtitle">Call the mocked endpoint and inspect the response.</p></div>
      <div class="panel-head-actions">
        <button class="button button-primary button-small" type="submit" form="test-form" :disabled="running">{{ running ? 'Running…' : 'Run request' }}</button>
        <button class="button button-secondary button-small" type="button" @click="emit('reset')">Clear test</button>
        <button class="icon-button icon-button--compact" type="button" title="Import cURL" aria-label="Import cURL" @click="emit('import-curl')">⇩</button>
      </div>
    </header>

    <form id="test-form" class="panel-body test-form" novalidate @submit.prevent="emit('run')">
      <div class="field-row">
        <label class="field field--compact">
          <span class="field-label">HTTP method</span>
          <select v-model="form.method"><option>GET</option><option>POST</option><option>PUT</option><option>DELETE</option><option>PATCH</option></select>
        </label>
        <label class="field field--grow">
          <span class="field-label">Path</span>
          <input v-model="form.path" type="text" placeholder="/customers" required>
        </label>
      </div>

      <label class="field field--compact">
        <span class="field-label">Content-Type</span>
        <select v-model="form.contentType"><option>application/json</option><option>application/x-www-form-urlencoded</option><option>text/plain</option><option>application/xml</option><option>multipart/form-data</option></select>
      </label>

      <label class="field">
        <span class="field-label">Request headers</span>
        <textarea v-model="form.headers" class="code-editor-input code-editor-input--compact" rows="3" spellcheck="false" placeholder="Authorization: Bearer token&#10;X-Correlation-Id: 123"></textarea>
      </label>

      <label class="field">
        <span class="field-label">Request body</span>
        <div class="code-editor">
          <div class="code-editor-gutter" aria-hidden="true">{{ Array.from({ length: bodyLines }, (_, index) => index + 1).join('\n') }}</div>
          <textarea v-model="form.body" class="code-editor-input" rows="7" spellcheck="false" :placeholder="bodyPlaceholder"></textarea>
        </div>
      </label>

      <div class="endpoint-preview">
        <span class="field-label">Request URL</span>
        <div class="endpoint-preview-row">
          <code class="endpoint-preview-url">{{ requestUrl }}</code>
          <button class="icon-button icon-button--copy" type="button" title="Copy URL" aria-label="Copy request URL" @click="emit('copy', requestUrl)">⧉</button>
        </div>
      </div>
      <p v-if="feedback" class="feedback" :class="feedbackError ? 'feedback--error' : 'feedback--success'" aria-live="polite">{{ feedback }}</p>

      <div class="response-section">
        <div class="response-section-heading">
          <h3>Response</h3>
          <button class="button button-secondary button-small" type="button" :disabled="!response.body" @click="emit('copy', response.body)">Copy response</button>
        </div>
        <div class="response-stats">
          <div class="stat-card"><span class="stat-card-label">Status</span><strong class="stat-card-value" :class="response.error || (response.statusCode && response.statusCode >= 400) ? 'is-error' : ''">{{ response.status || '—' }}</strong></div>
          <div class="stat-card"><span class="stat-card-label">Content-Type</span><strong class="stat-card-value">{{ response.contentType || '—' }}</strong></div>
          <div class="stat-card"><span class="stat-card-label">Response time</span><strong class="stat-card-value">{{ response.time || '—' }}</strong></div>
        </div>
        <div v-if="response.error" class="empty-state empty-state--error"><div class="empty-state-icon">!</div><h3 class="empty-state-title">Request error</h3><p class="empty-state-desc">{{ response.error }}</p></div>
        <pre v-else-if="response.body !== null" class="response-body">{{ response.body }}</pre>
        <div v-else class="empty-state"><div class="empty-state-icon">{ }</div><h3 class="empty-state-title">The response will appear here</h3><p class="empty-state-desc">Run a request to view the mock response.</p></div>
      </div>
    </form>
  </section>
</template>
