<script setup>
import { computed } from 'vue'
import { buildMockUrl } from '../domain.js'

const props = defineProps({
  form: { type: Object, required: true },
  dirty: { type: Boolean, default: false },
  saving: { type: Boolean, default: false },
  enabledBusy: { type: Boolean, default: false },
  feedback: { type: String, default: '' },
  feedbackError: { type: Boolean, default: false },
})
const emit = defineEmits(['save', 'reset', 'dirty', 'toggle-enabled', 'copy'])

const endpointUrl = computed(() => buildMockUrl(props.form.path, props.form.collection))
const bodyLines = computed(() => Math.max(1, String(props.form.responseBody || '').split('\n').length))
const bodyPlaceholder = computed(() => props.form.responseContentType === 'application/json' ? '{\n  "success": true\n}' : 'Response content')

function formatBody(event) {
  if (props.form.responseContentType !== 'application/json' || !event.target.value.trim()) return
  try { props.form.responseBody = JSON.stringify(JSON.parse(event.target.value), null, 2) } catch { /* Invalid JSON stays editable and is reported on save. */ }
}
</script>

<template>
    <form id="mock-form" class="panel-body" novalidate @submit.prevent="emit('save')">
      <section class="panel mock-config-panel">
        <header class="panel-head">
          <div class="panel-head-icon panel-head-icon--config" aria-hidden="true">⚙</div>
          <div><h2 class="panel-title">Configure mock</h2><p class="panel-subtitle">Define the behavior of the mocked endpoint.</p></div>
          <div class="panel-head-actions panel-head-actions--test">
            <button class="button button-secondary button-small" type="button" @click="emit('reset')">Clear</button>
          </div>
        </header>

        <div class="field-row field-row--method-status">
          <label class="field field--compact">
            <span class="field-label">HTTP method</span>
            <select v-model="form.method" class="input-with-badge" required @change="emit('dirty')">
              <option>GET</option><option>POST</option><option>PUT</option><option>DELETE</option><option>PATCH</option>
            </select>
          </label>
          <label class="field field--compact">
            <span class="field-label">Status code</span>
            <input v-model="form.statusCode" type="text" inputmode="numeric" list="status-code-suggestions" placeholder="e.g. 200" autocomplete="off" required @input="emit('dirty')">
            <datalist id="status-code-suggestions">
              <option value="200" label="OK"/><option value="201" label="Created"/><option value="202" label="Accepted"/><option value="204" label="No Content"/><option value="400" label="Bad Request"/><option value="401" label="Unauthorized"/><option value="403" label="Forbidden"/><option value="404" label="Not Found"/><option value="409" label="Conflict"/><option value="422" label="Unprocessable Entity"/><option value="500" label="Internal Server Error"/><option value="502" label="Bad Gateway"/><option value="503" label="Service Unavailable"/>
            </datalist>
          </label>
          <label class="field field--compact">
            <span class="field-label">Response delay (ms)</span>
            <input v-model="form.responseDelayMs" type="text" inputmode="numeric" pattern="[0-9]*" placeholder="0" autocomplete="off" @input="emit('dirty')">
          </label>
        </div>

        <label class="field field--highlight">
          <span class="field-label">Path</span>
          <input v-model="form.path" class="input-path" type="text" placeholder="/customers" required @input="emit('dirty')">
        </label>

        <div v-if="form.collection" class="field-group field-group--enabled">
          <div class="toggle-row">
            <label class="toggle-switch">
              <input :checked="form.enabled" type="checkbox" :disabled="enabledBusy" @change="emit('toggle-enabled', $event.target.checked)">
              <span class="toggle-slider"></span>
            </label>
            <div><span class="toggle-label">Mock enabled</span><p class="field-hint">When disabled, the collection uses bypass for this endpoint.</p></div>
          </div>
        </div>

        <div v-else class="field-group field-group--bypass">
          <div class="toggle-row">
            <label class="toggle-switch">
              <input v-model="form.bypassEnabled" type="checkbox" @change="emit('dirty')">
              <span class="toggle-slider"></span>
            </label>
            <div><span class="toggle-label">Enable bypass</span><p class="field-hint">Standalone mocks: forwards to the real URL when enabled.</p></div>
          </div>
          <label class="field" :class="{ 'is-disabled': !form.bypassEnabled }">
            <span class="field-label">Bypass URL</span>
            <input v-model="form.bypassUrl" type="url" placeholder="https://api.example.com/endpoint" :disabled="!form.bypassEnabled" @input="emit('dirty')">
          </label>
        </div>

        <div class="endpoint-preview">
          <span class="endpoint-preview-label">Endpoint preview</span>
          <div class="endpoint-preview-row">
            <span class="method-badge" :class="`method-badge--${form.method.toLowerCase()}`">{{ form.method }}</span>
            <code class="endpoint-preview-url">{{ endpointUrl }}</code>
            <button class="icon-button icon-button--copy" type="button" title="Copy URL" aria-label="Copy mock URL" @click="emit('copy', endpointUrl)">⧉</button>
          </div>
        </div>

        <label class="field field--compact">
          <span class="field-label">Response Content-Type</span>
          <select v-model="form.responseContentType" @change="emit('dirty')">
            <option value="application/json">application/json</option><option value="application/x-www-form-urlencoded">application/x-www-form-urlencoded</option><option value="text/plain">text/plain</option>
          </select>
        </label>

        <label class="field">
          <span class="field-label">Response body <span class="field-label-muted">({{ form.responseContentType }})</span></span>
          <div class="code-editor">
            <div class="code-editor-gutter" aria-hidden="true">{{ Array.from({ length: bodyLines }, (_, index) => index + 1).join('\n') }}</div>
            <textarea v-model="form.responseBody" class="code-editor-input" rows="7" spellcheck="false" :placeholder="bodyPlaceholder" @input="emit('dirty')" @blur="formatBody"></textarea>
          </div>
        </label>
        <p v-if="feedback" class="feedback" :class="feedbackError ? 'feedback--error' : 'feedback--success'" aria-live="polite">{{ feedback }}</p>
      </section>
    </form>
</template>
