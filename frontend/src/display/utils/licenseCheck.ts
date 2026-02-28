/**
 * licenseCheck - 许可证验证工具
 * 检查车间大屏功能的许可证状态
 * @module display/utils/licenseCheck
 */

import { ElMessage } from 'element-plus'
import type { LicenseStatus } from '../types/display'

/**
 * 许可证功能列表
 */
export const LICENSE_FEATURES = {
  DISPLAY_MODE: 'workshop_display', // 车间大屏功能
  DASHBOARD: 'dashboard',            // 看板功能
  CHARTS: 'charts',                  // 图表功能
  EXPORT: 'export'                   // 导出功能
}

/**
 * API 响应类型
 */
interface LicenseApiResponse {
  isValid: boolean
  isExpired: boolean
  expiryDate: string | null
  features: string[]
  errorMessage?: string
}

/**
 * 获取许可证状态
 *
 * @returns 许可证状态
 */
export async function getLicenseStatus(): Promise<LicenseStatus> {
  try {
    const response = await fetch('/api/license/status', {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json'
      }
    })

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`)
    }

    const apiResponse = await response.json()

    if (!apiResponse.success) {
      return {
        isValid: false,
        isExpired: false,
        expiryDate: null,
        features: [],
        error: apiResponse.message || '获取许可证状态失败'
      }
    }

    const data: LicenseApiResponse = apiResponse.data

    return {
      isValid: data.isValid,
      isExpired: data.isExpired,
      expiryDate: data.expiryDate ? new Date(data.expiryDate) : null,
      features: data.features || [],
      error: data.errorMessage
    }
  } catch (error) {
    console.error('[licenseCheck] 获取许可证状态失败:', error)
    return {
      isValid: false,
      isExpired: false,
      expiryDate: null,
      features: [],
      error: error instanceof Error ? error.message : '未知错误'
    }
  }
}

/**
 * 检查是否可以使用车间大屏功能
 *
 * @param status - 许可证状态
 * @returns 是否可以使用
 */
export function canUseDisplayMode(status: LicenseStatus): boolean {
  return status.isValid && !status.isExpired && status.features.includes(LICENSE_FEATURES.DISPLAY_MODE)
}

/**
 * 检查许可证并显示错误提示（如果无效）
 *
 * @returns 许可证是否有效
 */
export async function checkDisplayLicense(): Promise<boolean> {
  const status = await getLicenseStatus()

  if (!canUseDisplayMode(status)) {
    let errorMessage = '许可证验证失败，无法使用大屏模式'

    if (status.isExpired) {
      errorMessage = '许可证已过期，无法使用大屏模式'
    } else if (!status.isValid) {
      errorMessage = status.error || '许可证无效，无法使用大屏模式'
    } else if (!status.features.includes(LICENSE_FEATURES.DISPLAY_MODE)) {
      errorMessage = '当前许可证不包含车间大屏功能'
    }

    ElMessage.error(errorMessage)
    return false
  }

  return true
}

/**
 * 获取许可证到期信息
 *
 * @param status - 许可证状态
 * @returns 到期信息文本
 */
export function getLicenseExpiryInfo(status: LicenseStatus): string {
  if (status.isExpired) {
    return '许可证已过期'
  }

  if (!status.expiryDate) {
    return '永久许可证'
  }

  const now = new Date()
  const daysUntilExpiry = Math.ceil((status.expiryDate.getTime() - now.getTime()) / (1000 * 60 * 60 * 24))

  if (daysUntilExpiry <= 0) {
    return '许可证已过期'
  } else if (daysUntilExpiry <= 7) {
    return `许可证将在 ${daysUntilExpiry} 天后过期`
  } else if (daysUntilExpiry <= 30) {
    return `许可证将在 ${daysUntilExpiry} 天后过期`
  } else {
    return `许可证有效期至 ${status.expiryDate.toLocaleDateString('zh-CN')}`
  }
}

/**
 * 许可证检查 Hook
 */
export function useLicenseCheck() {
  const status = ref<LicenseStatus>({
    isValid: false,
    isExpired: false,
    expiryDate: null,
    features: []
  })
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  /**
   * 刷新许可证状态
   */
  async function refresh(): Promise<void> {
    isLoading.value = true
    error.value = null

    try {
      status.value = await getLicenseStatus()
    } catch (err) {
      error.value = err instanceof Error ? err.message : '获取许可证状态失败'
    } finally {
      isLoading.value = false
    }
  }

  /**
   * 检查是否可以使用指定功能
   */
  function canUseFeature(feature: string): boolean {
    return status.value.isValid &&
           !status.value.isExpired &&
           status.value.features.includes(feature)
  }

  /**
   * 获取到期信息
   */
  function expiryInfo(): string {
    return getLicenseExpiryInfo(status.value)
  }

  return {
    status,
    isLoading,
    error,
    refresh,
    canUseFeature,
    expiryInfo,
    canUseDisplayMode: () => canUseDisplayMode(status.value)
  }
}

/**
 * 许可证装饰器 - 在函数执行前检查许可证
 *
 * @param fn - 要执行的函数
 * @returns 包装后的函数
 */
export function withLicenseCheck<T extends (...args: unknown[]) => unknown>(
  fn: T
): T {
  return (async (...args: unknown[]) => {
    const isValid = await checkDisplayLicense()
    if (!isValid) {
      return null
    }
    return fn(...args)
  }) as T
}
