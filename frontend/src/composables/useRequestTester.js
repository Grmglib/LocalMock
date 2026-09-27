import { reactive, ref } from 'vue'
import { buildMockUrl, formatResponseText, normalizeEntry, parseHeaders, parseRequestBody } from '../domain.js'

export function useRequestTester({ api, activeCollection, t }) {
  const form = reactive({ method: 'GET', path: '', collection: activeCollection.value, contentType: 'application/json', headers: '', body: '' })
  const response = reactive({ status: '', statusCode: 0, contentType: '', time: '', body: null, error: '', origin: '', headers: [] })
  const running = ref(false)
  const feedback = ref('')
  const feedbackError = ref(false)

  function resetResponse() {
    Object.assign(response, { status: '', statusCode: 0, contentType: '', time: '', body: null, error: '', origin: '', headers: [] })
    feedback.value = ''
    feedbackError.value = false
  }

  function reset() {
    Object.assign(form, { method: 'GET', path: '', collection: activeCollection.value, contentType: 'application/json', headers: '', body: '' })
    resetResponse()
  }

  function useMock(entry) {
    const mock = normalizeEntry(entry)
    form.method = mock.method
    form.path = mock.path
    form.collection = mock.collection
    form.contentType = 'application/json'
    form.headers = ''
    form.body = ''
    resetResponse()
  }

  async function run() {
    if (!form.path.trim()) {
      feedback.value = t('pathRequired')
      feedbackError.value = true
      return
    }
    if (form.method === 'GET' && form.body.trim()) {
      feedback.value = t('getBodyError')
      feedbackError.value = true
      return
    }
    let headers
    let body
    try {
      headers = parseHeaders(form.headers)
      body = parseRequestBody(form.body.trim(), form.contentType)
    } catch (error) {
      feedback.value = t('invalidRequest', { message: error.message })
      feedbackError.value = true
      return
    }
    if (body !== null && form.contentType !== 'multipart/form-data') headers['Content-Type'] = form.contentType

    running.value = true
    feedback.value = ''
    feedbackError.value = false
    Object.assign(response, { status: '…', statusCode: 0, contentType: '', time: '', body: null, error: '', origin: '', headers: [] })
    const start = performance.now()
    try {
      const result = await api.run(buildMockUrl(form.path, form.collection), {
        method: form.method,
        headers,
        ...(body === null ? {} : { body }),
      })
      const text = await result.text()
      const contentType = result.headers.get('content-type') || 'not specified'
      const source = result.headers.get('x-localmock-source')
      const headerPairs = [...result.headers.entries()]
        .filter(([name]) => name.toLowerCase() !== 'x-localmock-source')
        .sort(([left], [right]) => left.localeCompare(right))
      Object.assign(response, {
        status: `${result.status} ${result.statusText}`,
        statusCode: result.status,
        contentType,
        time: `${Math.round(performance.now() - start)} ms`,
        body: formatResponseText(text, contentType),
        error: '',
        origin: source === 'mock' || source === 'bypass' ? source : 'unknown',
        headers: headerPairs,
      })
      feedback.value = result.ok ? t('requestCompleted', { time: response.time }) : t('requestReturnedStatus', { status: result.status })
      feedbackError.value = !result.ok
    } catch (error) {
      Object.assign(response, {
        status: 'Failed',
        statusCode: 0,
        time: `${Math.round(performance.now() - start)} ms`,
        error: error.message,
        origin: 'unknown',
        headers: [],
      })
      feedback.value = t('requestFailed', { message: error.message })
      feedbackError.value = true
    } finally {
      running.value = false
    }
  }

  return { form, response, running, feedback, feedbackError, resetResponse, reset, useMock, run }
}
