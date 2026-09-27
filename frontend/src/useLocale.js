import { computed, ref } from 'vue'
import en from './locales/en.js'
import ptBR from './locales/pt-BR.js'

const catalogs = { en, 'pt-BR': ptBR }
const saved = localStorage.getItem('localmock-locale')
const locale = ref(saved || (navigator.language?.toLowerCase().startsWith('pt') ? 'pt-BR' : 'en'))

export function setLocale(value) {
  locale.value = catalogs[value] ? value : 'en'
  localStorage.setItem('localmock-locale', locale.value)
  document.documentElement.lang = locale.value
}

export function useLocale() {
  const messages = computed(() => catalogs[locale.value] || en)
  function t(key, values = {}) {
    return String(messages.value[key] ?? en[key] ?? key).replace(/\{(\w+)\}/g, (_, name) => values[name] ?? `{${name}}`)
  }
  return { locale, t, setLocale }
}
