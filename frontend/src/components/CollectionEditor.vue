<script setup>
import { computed } from 'vue'
import { buildCollectionUrl } from '../domain.js'
import { useLocale } from '../useLocale.js'

const props = defineProps({
  form: { type: Object, required: true },
  editing: { type: Boolean, default: false },
  saving: { type: Boolean, default: false },
  count: { type: Number, default: 0 },
  feedback: { type: String, default: '' },
  feedbackError: { type: Boolean, default: false },
  errors: { type: Object, default: () => ({ id: '', bypassUrl: '' }) },
})
const emit = defineEmits(['save', 'close', 'reset', 'delete', 'manage', 'copy', 'field-input'])
const { t } = useLocale()
const baseUrl = computed(() => buildCollectionUrl(props.form.id.trim()))
</script>

<template>
  <div id="screen-collections" class="app-screen is-active" role="tabpanel" aria-label="Collection settings">
    <div class="app-main">
      <main class="workspace workspace--collections">
        <header class="workspace-heading collection-workspace-heading">
          <div>
            <p class="workspace-breadcrumb">{{ t('collections') }}</p>
            <h1>{{ editing ? `${t('collection')} ${form.id}` : t('newCollection') }}</h1>
            <span class="save-state">{{ editing ? t('endpointCount', { count }) : t('configureCollectionHelp') }}</span>
          </div>
          <button class="button button-secondary" type="button" @click="emit('close')">{{ t('backToEndpoints') }}</button>
        </header>

        <div class="workspace-panels workspace-panels--single">
          <section class="panel collection-config-panel" aria-labelledby="collection-config-title">
            <button class="button button-secondary button-small panel-clear-button" type="button" @click="emit('reset')">{{ t('clear') }}</button>
            <header class="panel-head">
              <div class="panel-head-icon panel-head-icon--config" aria-hidden="true">⊞</div>
              <div><h2 id="collection-config-title" class="panel-title">{{ t('collectionDetails') }}</h2><p class="panel-subtitle">{{ t('collectionDetailsHelp') }}</p></div>
            </header>
            <div class="panel-body">
              <form novalidate @submit.prevent="emit('save')">
                <label class="field field--highlight">
                  <span class="field-label">{{ t('collectionId') }}</span>
                  <input v-model="form.id" type="text" placeholder="partner" autocomplete="off" pattern="[a-zA-Z0-9_-]+" :disabled="editing" :aria-invalid="Boolean(errors.id)" :aria-describedby="errors.id ? 'collection-id-hint collection-id-error' : 'collection-id-hint'" required @input="emit('field-input', 'id')">
                  <span id="collection-id-hint" class="field-hint">{{ t('collectionSlugHelp') }}</span>
                  <span v-if="errors.id" id="collection-id-error" class="field-error" role="alert">{{ errors.id }}</span>
                </label>
                <label class="field">
                  <span class="field-label">{{ t('bypassUrlBase') }}</span>
                  <input v-model="form.bypassUrl" type="url" placeholder="https://api.example.com" autocomplete="off" :aria-invalid="Boolean(errors.bypassUrl)" :aria-describedby="errors.bypassUrl ? 'collection-bypass-hint collection-bypass-error' : 'collection-bypass-hint'" required @input="emit('field-input', 'bypassUrl')">
                  <span id="collection-bypass-hint" class="field-hint">{{ t('collectionBypassHelp') }}</span>
                  <span v-if="errors.bypassUrl" id="collection-bypass-error" class="field-error" role="alert">{{ errors.bypassUrl }}</span>
                </label>
                <div v-if="editing" class="collection-stats"><div class="stat-card"><span class="stat-card-label">{{ t('mocksInCollection') }}</span><strong class="stat-card-value">{{ count }}</strong></div></div>
                <div class="endpoint-preview">
                  <span class="endpoint-preview-label">{{ t('collectionBaseUrl') }}</span>
                  <div class="endpoint-preview-row"><code class="endpoint-preview-url">{{ baseUrl }}</code><button class="icon-button icon-button--copy" type="button" :title="t('copyUrl')" :aria-label="t('copyCollectionUrl')" @click="emit('copy', baseUrl)">⧉</button></div>
                </div>
                <div class="form-footer">
                  <div class="form-footer-primary form-footer-primary--wrap">
                    <button class="button button-primary button-large" type="submit" :disabled="saving">{{ saving ? t('saving') : t('saveCollection') }}</button>
                    <button v-if="editing" class="button button-secondary button-large" type="button" @click="emit('manage')">{{ t('manageMocks') }}</button>
                    <button v-if="editing" class="button button-secondary button-large" type="button" @click="emit('delete')">{{ t('deleteCollection') }}</button>
                  </div>
                  <p v-if="feedback" class="feedback" :class="feedbackError ? 'feedback--error' : 'feedback--success'" aria-live="polite">{{ feedback }}</p>
                </div>
              </form>
            </div>
          </section>
        </div>
      </main>
    </div>
  </div>
</template>
