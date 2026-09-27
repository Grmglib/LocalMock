import { reactive, ref } from 'vue'

export function useUpdateManager({ api, t, toast }) {
  const appVersion = ref('v…')
  const latestVersion = ref('')
  const updateAvailable = ref(false)
  const checking = ref(false)
  const overlay = reactive({ visible: false, message: '' })

  async function check({ notifyWhenCurrent = false } = {}) {
    checking.value = true
    try {
      const status = await api.version()
      const current = status.currentVersion ?? status.CurrentVersion
      const latest = status.latestVersion ?? status.LatestVersion
      appVersion.value = current ? `v${String(current).replace(/^v/i, '')}` : 'v…'
      latestVersion.value = latest ? `v${String(latest).replace(/^v/i, '')}` : ''
      updateAvailable.value = Boolean(status.updateAvailable ?? status.UpdateAvailable)
      const errorMessage = status.error ?? status.Error
      if (errorMessage) toast(t('updateCheckFailed', { message: errorMessage }), true)
      else if (notifyWhenCurrent && !updateAvailable.value) toast(t('alreadyLatestVersion', { version: latestVersion.value || appVersion.value }))
    } catch (error) {
      try {
        const version = await api.currentVersion()
        appVersion.value = `v${version.currentVersion ?? version.CurrentVersion ?? '…'}`
      } catch { appVersion.value = 'v…' }
      toast(t('updateCheckFailed', { message: error.message }), true)
    } finally {
      checking.value = false
    }
  }

  async function apply() {
    overlay.visible = true
    overlay.message = t('downloadingUpdate')
    try {
      const result = await api.startUpdate()
      const target = result.targetVersion ?? result.TargetVersion
      if (!(result.started ?? result.Started)) throw new Error(result.message ?? result.Message ?? 'Update could not be started.')
      overlay.message = t('installingUpdate', { version: target ? `v${target}` : t('theUpdate') })
      for (let attempt = 0; attempt < 45; attempt += 1) {
        await new Promise((resolve) => window.setTimeout(resolve, 2000))
        try {
          const version = await api.currentVersion()
          const current = version.currentVersion ?? version.CurrentVersion
          if (target && current === target) {
            window.location.reload()
            return
          }
        } catch { /* The local server is restarting. */ }
      }
      overlay.message = t('updateTakingLong')
    } catch (error) {
      overlay.visible = false
      toast(t('updateFailed', { message: error.message }), true)
    }
  }

  return { appVersion, latestVersion, updateAvailable, checking, overlay, check, apply }
}
