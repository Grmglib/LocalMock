<script setup>
import { computed } from 'vue'
import { buildMockUrl } from '../domain.js'
import { useLocale } from '../useLocale.js'

const props = defineProps({
  form: { type: Object, required: true },
  dirty: { type: Boolean, default: false },
  saving: { type: Boolean, default: false },
  enabledBusy: { type: Boolean, default: false },
  feedback: { type: String, default: '' },
  feedbackError: { type: Boolean, default: false },
})
const emit = defineEmits(['save', 'reset', 'dirty', 'toggle-enabled', 'copy'])
const { t } = useLocale()

const endpointUrl = computed(() => buildMockUrl(props.form.path, props.form.collection))
const bodyLines = computed(() => Math.max(1, String(props.form.responseBody || '').split('\n').length))
const bodyPlaceholder = computed(() => props.form.responseContentType === 'application/json' ? '{\n  "success": true\n}' : t('responseContentPlaceholder'))

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
          <div><h2 class="panel-title">{{ t('configureMock') }}</h2><p class="panel-subtitle">{{ t('configureMockDescription') }}</p></div>
          <div class="panel-head-actions panel-head-actions--test">
            <button class="button button-secondary button-small" type="button" @click="emit('reset')">{{ t('clear') }}</button>
            <button class="button button-primary button-small" type="submit" form="mock-form" :disabled="saving">{{ saving ? t('saving') : t('saveMock') }}</button>
          </div>
        </header>

        <div class="mock-setup-fields">
        <div class="field-row field-row--method-status">
          <label class="field field--compact">
              <span class="field-label">{{ t('httpMethod') }}</span>
            <select v-model="form.method" class="input-with-badge" required @change="emit('dirty')">
              <option>GET</option><option>POST</option><option>PUT</option><option>DELETE</option><option>PATCH</option>
            </select>
          </label>
          <label class="field field--compact">
              <span class="field-label">{{ t('statusCode') }}</span>
            <input v-model="form.statusCode" type="text" inputmode="numeric" list="status-code-suggestions" placeholder="e.g. 200" autocomplete="off" required @input="emit('dirty')">
            <datalist id="status-code-suggestions">
              <option value="200" :label="t('statusOk')"/><option value="201" :label="t('statusCreated')"/><option value="202" :label="t('statusAccepted')"/><option value="204" :label="t('statusNoContent')"/><option value="400" :label="t('statusBadRequest')"/><option value="401" :label="t('statusUnauthorized')"/><option value="403" :label="t('statusForbidden')"/><option value="404" :label="t('statusNotFound')"/><option value="409" :label="t('statusConflict')"/><option value="422" :label="t('statusUnprocessable')"/><option value="500" :label="t('statusInternalError')"/><option value="502" :label="t('statusBadGateway')"/><option value="503" :label="t('statusUnavailable')"/>
            </datalist>
          </label>
          <label class="field field--compact">
              <span class="field-label">{{ t('responseDelay') }}</span>
            <input v-model="form.responseDelayMs" type="text" inputmode="numeric" pattern="[0-9]*" placeholder="0" autocomplete="off" @input="emit('dirty')">
          </label>
        </div>

        <label class="field field--highlight">
          <span class="field-label">{{ t('path') }}</span>
          <input v-model="form.path" class="input-path" type="text" placeholder="/customers" required @input="emit('dirty')">
        </label>

        <div v-if="form.collection" class="field-group field-group--enabled">
          <div class="toggle-row">
            <label class="toggle-switch">
              <input :checked="form.enabled" type="checkbox" :disabled="enabledBusy" @change="emit('toggle-enabled', $event.target.checked)">
              <span class="toggle-slider"></span>
            </label>
              <div><span class="toggle-label">{{ t('mockEnabledLabel') }}</span><p class="field-hint">{{ t('disabledUsesBypass') }}</p></div>
          </div>
        </div>

        <div v-else class="field-group field-group--bypass">
          <div class="toggle-row">
            <label class="toggle-switch">
              <input v-model="form.bypassEnabled" type="checkbox" @change="emit('dirty')">
              <span class="toggle-slider"></span>
            </label>
              <div><span class="toggle-label">{{ t('enableBypass') }}</span><p class="field-hint">{{ t('standaloneBypassHelp') }}</p></div>
          </div>
          <label class="field" :class="{ 'is-disabled': !form.bypassEnabled }">
            <span class="field-label">{{ t('bypassUrl') }}</span>
            <input v-model="form.bypassUrl" type="url" placeholder="https://api.example.com/endpoint" :disabled="!form.bypassEnabled" @input="emit('dirty')">
          </label>
        </div>

        <div class="endpoint-preview">
          <span class="endpoint-preview-label">{{ t('endpointPreview') }}</span>
          <div class="endpoint-preview-row">
            <span class="method-badge" :class="`method-badge--${form.method.toLowerCase()}`">{{ form.method }}</span>
            <code class="endpoint-preview-url">{{ endpointUrl }}</code>
              <button class="icon-button icon-button--copy" type="button" :title="t('copyUrl')" :aria-label="t('copyMockUrl')" @click="emit('copy', endpointUrl)">⧉</button>
          </div>
        </div>
        </div>

        <div class="mock-response-fields">
        <label class="field field--compact">
          <span class="field-label">{{ t('responseContentType') }}</span>
          <select v-model="form.responseContentType" @change="emit('dirty')">
            <option value="application/json">application/json</option><option value="application/x-www-form-urlencoded">application/x-www-form-urlencoded</option><option value="text/plain">text/plain</option>
          </select>
        </label>

        <label class="field">
          <span class="field-label">{{ t('responseBody') }} <span class="field-label-muted">({{ form.responseContentType }})</span></span>
          <div class="code-editor">
            <div class="code-editor-gutter" aria-hidden="true">{{ Array.from({ length: bodyLines }, (_, index) => index + 1).join('\n') }}</div>
            <textarea v-model="form.responseBody" class="code-editor-input" rows="7" spellcheck="false" :placeholder="bodyPlaceholder" @input="emit('dirty')" @blur="formatBody"></textarea>
          </div>
        </label>
        </div>
        <p v-if="feedback" class="feedback" :class="feedbackError ? 'feedback--error' : 'feedback--success'" aria-live="polite">{{ feedback }}</p>
      </section>
    </form>
</template>
