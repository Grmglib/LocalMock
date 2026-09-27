import { reactive, ref } from 'vue'

export function useUpdateManager({ api, t, toast }) {
  const appVersion = ref('v…')
  const updateAvailable = ref(false)
  const checking = ref(false)
  const overlay = reactive({ visible: false, message: '' })

  async function check() {
    checking.value = true
    try {
      const status = await api.version()
      appVersion.value = `v${status.currentVersion ?? status.CurrentVersion ?? '…'}`
      updateAvailable.value = Boolean(status.updateAvailable ?? status.UpdateAvailable)
      if (status.error ?? status.Error) toast(t('updateCheckFailed', { message: status.error ?? status.Error }), true)
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

  return { appVersion, updateAvailable, checking, overlay, check, apply }
}
