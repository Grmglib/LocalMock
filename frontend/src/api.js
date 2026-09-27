async function request(url, options) {
  const response = await fetch(url, options)
  if (!response.ok) {
    const message = (await response.text()).trim()
    throw new Error(message || `${response.status} ${response.statusText}`)
  }
  return response
}

const json = (value) => ({
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(value),
})

export const api = {
  async mocks() {
    return (await request('/mock', { headers: { Accept: 'application/json' } })).json()
  },
  async collections() {
    return (await request('/mock/collections', { headers: { Accept: 'application/json' } })).json()
  },
  saveMock(value) {
    return request('/mock', { method: 'POST', ...json(value) })
  },
  removeMock(entry) {
    const params = new URLSearchParams({ method: entry.method, path: entry.path })
    if (entry.collection) params.set('collection', entry.collection)
    return request(`/mock?${params}`, { method: 'DELETE' })
  },
  setMockEnabled(entry, enabled) {
    return request('/mock/enabled', {
      method: 'PATCH',
      ...json({ collection: entry.collection, method: entry.method, path: entry.path, enabled }),
    })
  },
  setBypass(entry, bypassEnabled, bypassUrl) {
    return request('/mock/bypass', {
      method: 'PATCH',
      ...json({
        collection: entry.collection,
        method: entry.method,
        path: entry.path,
        bypassEnabled,
        bypassUrl,
      }),
    })
  },
  createCollection(value) {
    return request('/mock/collections', { method: 'POST', ...json(value) })
  },
  updateCollection(id, bypassUrl) {
    return request(`/mock/collections/${encodeURIComponent(id)}`, {
      method: 'PATCH',
      ...json({ bypassUrl }),
    })
  },
  removeCollection(id) {
    return request(`/mock/collections/${encodeURIComponent(id)}`, { method: 'DELETE' })
  },
  run(url, options) {
    return fetch(url, options)
  },
  version() {
    return request('/api/version', { headers: { Accept: 'application/json' } }).then((response) => response.json())
  },
  currentVersion() {
    return request('/api/version/current', { headers: { Accept: 'application/json' } }).then((response) => response.json())
  },
  startUpdate() {
    return request('/api/update', { method: 'POST' }).then((response) => response.json())
  },
}
