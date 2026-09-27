<script setup>
import { computed } from 'vue'
import { buildMockUrl } from '../domain.js'
import { useLocale } from '../useLocale.js'

const props = defineProps({
  form: { type: Object, required: true },
  response: { type: Object, required: true },
  running: { type: Boolean, default: false },
  feedback: { type: String, default: '' },
  feedbackError: { type: Boolean, default: false },
})
const emit = defineEmits(['run', 'reset', 'import-curl', 'copy'])
const { t } = useLocale()

const requestUrl = computed(() => buildMockUrl(props.form.path, props.form.collection))
const bodyPlaceholder = computed(() => {
  if (props.form.contentType === 'application/json') return '{\n  "id": 1\n}'
  if (props.form.contentType === 'application/x-www-form-urlencoded' || props.form.contentType === 'multipart/form-data') return 'customerId=1\nname=Maria'
  if (props.form.contentType.includes('xml')) return '<request>\n  <id>1</id>\n</request>'
  return t('requestBodyPlaceholder')
})
const bodyLines = computed(() => Math.max(1, String(props.form.body || '').split('\n').length))
</script>

<template>
  <section class="panel mock-test-panel">
    <header class="panel-head">
      <div class="panel-head-icon panel-head-icon--test" aria-hidden="true">▶</div>
      <div><h2 class="panel-title">{{ t('testRequest') }}</h2><p class="panel-subtitle">{{ t('testDescription') }}</p></div>
      <div class="panel-head-actions">
        <button class="button button-secondary button-small" type="button" @click="emit('reset')">{{ t('clearTest') }}</button>
        <button class="icon-button icon-button--compact" type="button" :title="t('importCurl')" :aria-label="t('importCurl')" @click="emit('import-curl')">⇩</button>
        <button class="button button-primary button-small" type="submit" form="test-form" :disabled="running">{{ running ? t('running') : t('runRequest') }}</button>
      </div>
    </header>

    <form id="test-form" class="panel-body test-form" novalidate @submit.prevent="emit('run')">
      <div class="test-request-fields">
      <div class="field-row">
        <label class="field field--compact">
          <span class="field-label">{{ t('httpMethod') }}</span>
          <select v-model="form.method"><option>GET</option><option>POST</option><option>PUT</option><option>DELETE</option><option>PATCH</option></select>
        </label>
        <label class="field field--grow">
          <span class="field-label">{{ t('path') }}</span>
          <input v-model="form.path" type="text" placeholder="/customers" required>
        </label>
      </div>

      <label class="field field--compact">
        <span class="field-label">{{ t('contentType') }}</span>
        <select v-model="form.contentType"><option>application/json</option><option>application/x-www-form-urlencoded</option><option>text/plain</option><option>application/xml</option><option>multipart/form-data</option></select>
      </label>

      <label class="field">
        <span class="field-label">{{ t('requestHeaders') }}</span>
        <textarea v-model="form.headers" class="code-editor-input code-editor-input--compact" rows="3" spellcheck="false" placeholder="Authorization: Bearer token&#10;X-Correlation-Id: 123"></textarea>
      </label>

      <label class="field">
        <span class="field-label">{{ t('requestBody') }}</span>
        <div class="code-editor">
          <div class="code-editor-gutter" aria-hidden="true">{{ Array.from({ length: bodyLines }, (_, index) => index + 1).join('\n') }}</div>
          <textarea v-model="form.body" class="code-editor-input" rows="7" spellcheck="false" :placeholder="bodyPlaceholder"></textarea>
        </div>
      </label>

      <div class="endpoint-preview">
        <span class="field-label">{{ t('requestUrl') }}</span>
        <div class="endpoint-preview-row">
          <code class="endpoint-preview-url">{{ requestUrl }}</code>
          <button class="icon-button icon-button--copy" type="button" :title="t('copyUrl')" :aria-label="t('copyRequestUrl')" @click="emit('copy', requestUrl)">⧉</button>
        </div>
      </div>
      <p v-if="feedback" class="feedback" :class="feedbackError ? 'feedback--error' : 'feedback--success'" aria-live="polite">{{ feedback }}</p>
      </div>

      <div class="response-section">
        <div class="response-section-heading">
          <h3>{{ t('response') }}</h3>
          <button class="button button-secondary button-small" type="button" :disabled="!response.body" @click="emit('copy', response.body)">{{ t('copyResponse') }}</button>
        </div>
        <div class="response-stats">
          <div class="stat-card"><span class="stat-card-label">{{ t('status') }}</span><strong class="stat-card-value" :class="response.error || (response.statusCode && response.statusCode >= 400) ? 'is-error' : ''">{{ response.status || '—' }}</strong></div>
          <div class="stat-card"><span class="stat-card-label">{{ t('contentType') }}</span><strong class="stat-card-value">{{ response.contentType || '—' }}</strong></div>
          <div class="stat-card"><span class="stat-card-label">{{ t('responseTime') }}</span><strong class="stat-card-value">{{ response.time || '—' }}</strong></div>
          <div class="stat-card"><span class="stat-card-label">{{ t('responseOrigin') }}</span><strong class="stat-card-value" :class="`response-origin--${response.origin || 'unknown'}`">{{ response.origin === 'mock' ? t('mockOrigin') : response.origin === 'bypass' ? t('bypassOrigin') : t('unknownOrigin') }}</strong></div>
        </div>
        <details v-if="response.headers.length" class="response-headers">
          <summary>{{ t('responseHeaders') }} <span>{{ response.headers.length }}</span></summary>
          <button class="button button-secondary button-small" type="button" @click="emit('copy', response.headers.map(([name, value]) => `${name}: ${value}`).join('\n'))">{{ t('copyHeaders') }}</button>
          <dl><template v-for="([name, value]) in response.headers" :key="name"><dt>{{ name }}</dt><dd>{{ value }}</dd></template></dl>
        </details>
        <div v-if="response.error" class="empty-state empty-state--error"><div class="empty-state-icon">!</div><h3 class="empty-state-title">{{ t('requestError') }}</h3><p class="empty-state-desc">{{ response.error }}</p></div>
        <pre v-else-if="response.body !== null" class="response-body">{{ response.body }}</pre>
        <div v-else class="empty-state"><div class="empty-state-icon">{ }</div><h3 class="empty-state-title">{{ t('noResponse') }}</h3><p class="empty-state-desc">{{ t('runToView') }}</p></div>
      </div>
    </form>
  </section>
</template>
