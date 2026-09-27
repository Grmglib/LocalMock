<script setup>
import { useLocale } from '../useLocale.js'
defineProps({ open: Boolean, command: String, error: String })
const emit = defineEmits(['update:command', 'close', 'apply'])
const { t } = useLocale()
</script>

<template>
  <div v-if="open" class="modal" role="dialog" aria-modal="true" aria-labelledby="curl-modal-title" @keydown.esc="emit('close')">
    <div class="modal-backdrop" @click="emit('close')"></div>
    <div class="modal-content">
      <header class="modal-header"><h2 id="curl-modal-title">{{ t('importCurl') }}</h2><button class="icon-button modal-close" type="button" :aria-label="t('close')" @click="emit('close')">×</button></header>
      <div class="modal-body">
        <label class="field"><span class="field-label">{{ t('pasteCurl') }}</span><textarea rows="8" spellcheck="false" placeholder="curl 'http://localhost:5183/mock/partner/customers' -H 'Authorization: Bearer token' -H 'Content-Type: application/json' --data '{&quot;id&quot;:1}'" :value="command" @input="emit('update:command', $event.target.value)"></textarea></label>
        <p v-if="error" class="feedback feedback--error" role="alert">{{ error }}</p>
      </div>
      <footer class="modal-footer"><button class="button button-secondary" type="button" @click="emit('close')">{{ t('cancel') }}</button><button class="button button-primary" type="button" @click="emit('apply')">{{ t('applyCurl') }}</button></footer>
    </div>
  </div>
</template>
