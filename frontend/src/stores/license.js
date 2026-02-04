import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { systemApi, licenseApi, userApi, dataSourceApi, reportApi } from '../api/request'
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
    if (!restrictions.value.allowedFeatures || restrictions.value.allowedFeatures.length === 0) {
      return true // 没有限制则默认允许
    }
    return restrictions.value.allowedFeatures.includes(feature)
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
    } catch (error) {
      console.error('加载许可证失败:', error)
      licenseStatus.value = 'invalid'
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
    } catch (error) {
      console.error('激活许可证失败:', error)
      ElMessage.error('许可证激活失败')
      return false
    }
  }

  const checkOperation = async (operation, resource) => {
    // 检查操作是否允许
    if (restrictions.value.isReadOnly && ['create', 'update', 'delete'].includes(operation)) {
      ElMessage.warning('许可证已过期，系统处于只读模式')
      return false
    }

    // 检查资源限制
    switch (resource) {
      case 'user':
        if (operation === 'create' && restrictions.value.maxUsers !== null) {
          // TODO: 检查当前用户数
          // const users = await userApi.getUsers({ pageIndex: 1, pageSize: 1 })
          // if (users.data.totalCount >= restrictions.value.maxUsers) {
          //   ElMessage.warning(`已达到最大用户数限制 (${restrictions.value.maxUsers})`)
          //   return false
          // }
        }
        break
      case 'report':
        if (operation === 'create' && restrictions.value.maxReports !== null) {
          // TODO: 检查当前报表数
          // const reports = await reportApi.getReports({ pageIndex: 1, pageSize: 1 })
          // if (reports.data.totalCount >= restrictions.value.maxReports) {
          //   ElMessage.warning(`已达到最大报表数限制 (${restrictions.value.maxReports})`)
          //   return false
          // }
        }
        break
      case 'datasource':
        if (operation === 'create' && restrictions.value.maxDataSources !== null) {
          // TODO: 检查当前数据源数
          // const datasources = await dataSourceApi.getDataSources({ pageIndex: 1, pageSize: 1 })
          // if (datasources.data.totalCount >= restrictions.value.maxDataSources) {
          //   ElMessage.warning(`已达到最大数据源数限制 (${restrictions.value.maxDataSources})`)
          //   return false
          // }
        }
        break
    }

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
      if (res.success) {
        if (res.data.valid) {
          licenseStatus.value = license.value?.licenseType === 'Trial' ? 'trial' : 'valid'
          if (res.data.licenseInfo) {
            license.value = res.data.licenseInfo
            updateRestrictions(res.data.licenseInfo)
          }
          return true
        } else {
          licenseStatus.value = 'expired'
          updateRestrictions(license.value)
          ElMessage.warning(res.data.message || '许可证验证失败')
          return false
        }
      }
      return false
    } catch (error) {
      console.error('验证许可证失败:', error)
      return false
    }
  }

  const getMachineCode = async () => {
    try {
      const res = await systemApi.getMachineCode()
      if (res.success) {
        return res.data
      }
      return null
    } catch (error) {
      console.error('获取机器码失败:', error)
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
