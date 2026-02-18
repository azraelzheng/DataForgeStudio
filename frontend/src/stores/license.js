import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { systemApi, licenseApi } from '../api/request'
import { ElMessage } from 'element-plus'

export const useLicenseStore = defineStore('license', () => {
  // 状态
  const license = ref(null)
  const licenseStatus = ref('unknown') // unknown, valid, expired, invalid, trial
  const restrictions = ref({
    isReadOnly: false,
    maxUsers: null,
    maxReports: null,
    maxDataSources: null,
    allowedFeatures: []
  })
  const warningMessage = ref('')
  const warningLevel = ref('info') // info, warning, error

  // 计算属性
  const canCreateUser = computed(() => {
    if (restrictions.value.isReadOnly) return false
    if (restrictions.value.maxUsers === null) return true
    return true // 这里需要根据当前用户数判断
  })

  const hasFeature = (feature) => {
    const features = restrictions.value.allowedFeatures
    if (!features || features.length === 0) {
      return true
    }
    return features.includes(feature)
  }

  const isTrial = computed(() => {
    return license.value?.licenseType === 'Trial'
  })

  const daysRemaining = computed(() => {
    if (!license.value?.expiryDate) return null
    const expiry = new Date(license.value.expiryDate)
    const now = new Date()
    const diff = Math.floor((expiry - now) / (1000 * 60 * 60 * 24))
    return diff > 0 ? diff : 0
  })

  const isExpiringSoon = computed(() => {
    const days = daysRemaining.value
    return typeof days === 'number' && days < 30 && days >= 0
  })

  const isExpired = computed(() => {
    if (!license.value?.expiryDate) return false
    const expiry = new Date(license.value.expiryDate)
    const now = new Date()
    return expiry < now
  })

  // Actions
  const loadLicense = async () => {
    try {
      const res = await licenseApi.getLicense()
      if (res.success) {
        license.value = res.data
        await updateLicenseStatus(res.data)
        return res.data
      }
      ElMessage.error(res.message || '加载许可证失败')
      licenseStatus.value = 'invalid'
      return null
    } catch {
      licenseStatus.value = 'invalid'
      ElMessage.error('加载许可证失败，请检查网络连接')
      return null
    }
  }

  const activateLicense = async (licenseKey) => {
    try {
      const res = await licenseApi.activateLicense({ licenseKey })
      if (res.success) {
        license.value = res.data
        await updateLicenseStatus(res.data)
        ElMessage.success('许可证激活成功')
        return true
      }
      return false
    } catch {
      ElMessage.error('许可证激活失败')
      return false
    }
  }

  const checkOperation = async (operation, resource) => {
    if (restrictions.value.isReadOnly && ['create', 'update', 'delete'].includes(operation)) {
      ElMessage.warning('许可证已过期，系统处于只读模式')
      return false
    }

    // 资源数量限制检查（待实现）
    return true
  }

  const updateRestrictions = (licenseData) => {
    if (!licenseData) {
      restrictions.value = {
        isReadOnly: false,
        maxUsers: null,
        maxReports: null,
        maxDataSources: null,
        allowedFeatures: []
      }
      return
    }

    restrictions.value = {
      isReadOnly: isExpired.value,
      maxUsers: licenseData.maxUsers || null,
      maxReports: licenseData.maxReports || null,
      maxDataSources: licenseData.maxDataSources || null,
      allowedFeatures: licenseData.features || []
    }

    // 更新警告信息
    updateWarningMessage()
  }

  const updateLicenseStatus = async (licenseData) => {
    if (!licenseData) {
      licenseStatus.value = 'invalid'
      warningMessage.value = '未激活许可证'
      warningLevel.value = 'warning'
      updateRestrictions(null)
      return
    }

    try {
      const res = await systemApi.validateLicense({ forceRefresh: false })
      if (res.success && res.data.valid) {
        licenseStatus.value = licenseData.licenseType === 'Trial' ? 'trial' : 'valid'
      } else {
        licenseStatus.value = 'expired'
      }
    } catch (error) {
      // 如果验证失败，检查本地过期时间
      if (isExpired.value) {
        licenseStatus.value = 'expired'
      } else {
        licenseStatus.value = 'valid'
      }
    }

    updateRestrictions(licenseData)
  }

  const updateWarningMessage = () => {
    if (isExpired.value) {
      warningMessage.value = '许可证已过期，系统处于只读模式。请续费以继续使用所有功能。'
      warningLevel.value = 'error'
    } else if (isExpiringSoon.value) {
      const days = daysRemaining.value
      if (days <= 7) {
        warningMessage.value = `许可证将在 ${days} 天后过期，请及时续费。`
        warningLevel.value = 'error'
      } else {
        warningMessage.value = `许可证将在 ${days} 天后过期，请尽快续费。`
        warningLevel.value = 'warning'
      }
    } else if (isTrial.value) {
      const days = daysRemaining.value
      warningMessage.value = `当前使用试用许可证，剩余 ${days} 天。`
      warningLevel.value = 'info'
    } else {
      warningMessage.value = ''
      warningLevel.value = 'info'
    }
  }

  const validateLicense = async (forceRefresh = false) => {
    try {
      const res = await systemApi.validateLicense({ forceRefresh })
      if (res.success && res.data.valid) {
        licenseStatus.value = license.value?.licenseType === 'Trial' ? 'trial' : 'valid'
        if (res.data.licenseInfo) {
          license.value = res.data.licenseInfo
          updateRestrictions(res.data.licenseInfo)
        }
        return true
      }
      if (res.success && !res.data.valid) {
        licenseStatus.value = 'expired'
        updateRestrictions(license.value)
        ElMessage.warning(res.data.message || '许可证验证失败')
      }
      return false
    } catch {
      return false
    }
  }

  const getMachineCode = async () => {
    try {
      const res = await systemApi.getMachineCode()
      return res.success ? res.data : null
    } catch {
      return null
    }
  }

  return {
    // 状态
    license,
    licenseStatus,
    restrictions,
    warningMessage,
    warningLevel,

    // 计算属性
    canCreateUser,
    hasFeature,
    isTrial,
    daysRemaining,
    isExpiringSoon,
    isExpired,

    // Actions
    loadLicense,
    activateLicense,
    checkOperation,
    updateRestrictions,
    validateLicense,
    getMachineCode
  }
})
