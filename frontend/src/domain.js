export function normalizePath(path) {
  const trimmed = String(path || '').trim()
  if (!trimmed) return '/'
  return trimmed.startsWith('/') ? trimmed : `/${trimmed}`
}

export function collectionId(entry) {
  const value = entry.collection ?? entry.Collection ?? ''
  return String(value || '').trim()
}

export function getEnabled(entry) {
  return Boolean(entry.enabled ?? entry.Enabled ?? true)
}

export function getBypassEnabled(entry) {
  return Boolean(entry.bypassEnabled ?? entry.BypassEnabled ?? false)
}

export function normalizeEntry(entry) {
  return {
    collection: collectionId(entry),
    method: String(entry.method || entry.Method || 'GET').toUpperCase(),
    path: normalizePath(entry.path || entry.Path || '/'),
    statusCode: Number(entry.statusCode ?? entry.StatusCode ?? 200),
    responseDelayMs: Number(entry.responseDelayMs ?? entry.ResponseDelayMs ?? 0) || 0,
    responseContentType: String(entry.responseContentType || entry.ResponseContentType || 'application/json').toLowerCase(),
    responseBody: entry.responseBody ?? entry.ResponseBody ?? null,
    enabled: getEnabled(entry),
    bypassEnabled: getBypassEnabled(entry),
    bypassUrl: entry.bypassUrl ?? entry.BypassUrl ?? '',
  }
}

export function entryKey(entry) {
  const mock = normalizeEntry(entry)
  return `${mock.collection}::${mock.method}::${mock.path}`
}

export function validHttpUrl(value) {
  try {
    const url = new URL(value)
    return url.protocol === 'http:' || url.protocol === 'https:'
  } catch {
    return false
  }
}

export function buildMockUrl(path, collection = '') {
  const prefix = collection ? `/mock/${encodeURIComponent(collection)}` : '/mock'
  return new URL(`${prefix}${normalizePath(path)}`, window.location.origin).toString()
}

export function buildCollectionUrl(id) {
  return id ? new URL(`/mock/${encodeURIComponent(id)}`, window.location.origin).toString() : '—'
}

export function responseBodyText(entry) {
  const mock = normalizeEntry(entry)
  const body = mock.responseBody
  if (body == null || body === '') return ''
  if (mock.responseContentType === 'application/json') {
    try {
      return JSON.stringify(typeof body === 'string' ? JSON.parse(body) : body, null, 2)
    } catch {
      return String(body)
    }
  }
  if (mock.responseContentType === 'application/x-www-form-urlencoded') {
    const params = new URLSearchParams(String(body))
    const lines = [...params.entries()].map(([key, value]) => `${key}=${value}`)
    return lines.length ? lines.join('\n') : String(body)
  }
  return typeof body === 'string' ? body : JSON.stringify(body)
}

function formLinesToParams(text) {
  const params = new URLSearchParams()
  for (const line of text.split(/\r?\n/).filter((item) => item.trim())) {
    const index = line.indexOf('=')
    if (index > 0) params.append(line.slice(0, index).trim(), line.slice(index + 1).trim())
    else params.append(line.trim(), '')
  }
  return params
}

export function parseResponseBody(text, contentType) {
  if (!text) return null
  if (contentType === 'application/json') return JSON.parse(text)
  if (contentType === 'application/x-www-form-urlencoded') {
    return text.includes('\n') ? formLinesToParams(text).toString() : text
  }
  return text
}

export function parseRequestBody(text, contentType) {
  if (!text) return null
  if (contentType === 'application/json') return JSON.stringify(JSON.parse(text))
  if (contentType === 'application/x-www-form-urlencoded') return formLinesToParams(text).toString()
  if (contentType === 'multipart/form-data') {
    const form = new FormData()
    for (const [key, value] of formLinesToParams(text)) form.append(key, value)
    return form
  }
  return text
}

export function parseHeaders(text) {
  const headers = {}
  for (const line of text.split(/\r?\n/).filter((item) => item.trim())) {
    const separator = line.indexOf(':')
    if (separator <= 0) throw new Error(`Invalid request header: ${line}`)
    headers[line.slice(0, separator).trim()] = line.slice(separator + 1).trim()
  }
  return headers
}

export function formatResponseText(text, contentType) {
  if (contentType.includes('json')) {
    try {
      return JSON.stringify(JSON.parse(text), null, 2)
    } catch {
      return text
    }
  }
  return text
}

function tokenizeCurl(command) {
  const tokens = []
  let current = ''
  let quote = ''
  let escaped = false
  for (const char of command.replace(/\\\r?\n/g, ' ').replace(/\^\r?\n/g, ' ').trim()) {
    if (escaped) {
      current += char
      escaped = false
    } else if (char === '\\' && quote !== "'") escaped = true
    else if (quote) {
      if (char === quote) quote = ''
      else current += char
    } else if (char === '"' || char === "'") quote = char
    else if (/\s/.test(char)) {
      if (current) tokens.push(current)
      current = ''
    } else current += char
  }
  if (quote) throw new Error('The cURL command has an unclosed quote.')
  if (escaped) current += '\\'
  if (current) tokens.push(current)
  return tokens
}

export function parseCurl(command) {
  const tokens = tokenizeCurl(command)
  if (!tokens.length || tokens[0].toLowerCase() !== 'curl') throw new Error('Enter a command that starts with curl.')
  const parsed = { method: '', url: '', headers: [], body: null, contentType: '' }
  const valueOptions = new Set(['-X', '--request', '-H', '--header', '-d', '--data', '--data-raw', '--data-binary', '--data-urlencode', '-F', '--form', '--form-string'])
  const addBody = (value, separator = '&') => { parsed.body = parsed.body == null ? value : `${parsed.body}${separator}${value}` }

  for (let index = 1; index < tokens.length; index += 1) {
    let token = tokens[index]
    if (valueOptions.has(token)) {
      if (!tokens[index + 1]) throw new Error(`Option ${token} has no value.`)
      const value = tokens[++index]
      if (token === '-X' || token === '--request') parsed.method = value.toUpperCase()
      else if (token === '-H' || token === '--header') {
        const colon = value.indexOf(':')
        if (colon <= 0) throw new Error(`Invalid header in cURL: ${value}`)
        parsed.headers.push({ name: value.slice(0, colon).trim(), value: value.slice(colon + 1).trim() })
      } else if (token === '-F' || token === '--form' || token === '--form-string') {
        addBody(value, '\n')
        parsed.contentType ||= 'multipart/form-data'
        parsed.method ||= 'POST'
      } else {
        addBody(value)
        parsed.contentType ||= token === '--data-raw' || token === '--data-binary' ? 'application/json' : 'application/x-www-form-urlencoded'
        parsed.method ||= 'POST'
      }
      continue
    }

    const match = token.match(/^(--request|--header|--data(?:-raw|-binary|-urlencode)?|--form(?:-string)?|-X|-H)(?:=|(?=[A-Z]))(.*)$/i)
    if (match) {
      const option = match[1]
      const value = match[2]
      if (option.toLowerCase().startsWith('-x') || option === '--request') parsed.method = value.toUpperCase()
      else if (option.toLowerCase().startsWith('-h') || option === '--header') {
        const colon = value.indexOf(':')
        if (colon <= 0) throw new Error(`Invalid header in cURL: ${value}`)
        parsed.headers.push({ name: value.slice(0, colon).trim(), value: value.slice(colon + 1).trim() })
      } else {
        const form = option.toLowerCase().startsWith('--form')
        addBody(value, form ? '\n' : '&')
        parsed.contentType ||= form ? 'multipart/form-data' : 'application/x-www-form-urlencoded'
        parsed.method ||= 'POST'
      }
      continue
    }

    try {
      if (!parsed.url && /^https?:\/\//i.test(token)) parsed.url = new URL(token).toString()
    } catch { /* Invalid URLs are reported when the request is run. */ }
  }

  parsed.method ||= parsed.body ? 'POST' : 'GET'
  const headerType = parsed.headers.find((header) => header.name.toLowerCase() === 'content-type')?.value.split(';')[0].trim()
  parsed.contentType = headerType || parsed.contentType || 'application/json'
  parsed.headers = parsed.headers.filter((header) => header.name.toLowerCase() !== 'content-type')
  if (parsed.url) {
    const url = new URL(parsed.url)
    if (url.origin === window.location.origin && url.pathname.startsWith('/mock')) {
      parsed.path = url.pathname.slice('/mock'.length) || '/'
    } else parsed.path = url.pathname
  }
  return parsed
}

export function toFileEntry(entry) {
  const mock = normalizeEntry(entry)
  const output = {
    Method: mock.method,
    Path: mock.path,
    StatusCode: mock.statusCode,
    ResponseDelayMs: mock.responseDelayMs,
    ResponseContentType: mock.responseContentType,
    ResponseBody: mock.responseBody,
    Enabled: mock.enabled,
    BypassEnabled: mock.bypassEnabled,
    BypassUrl: mock.bypassUrl,
  }
  if (mock.collection) output.Collection = mock.collection
  return output
}
