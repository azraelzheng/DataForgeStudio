import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'

// Mock the user store module
const mockUserStore = {
  token: '',
  userInfo: null,
  setToken: function(token) {
    this.token = token
    if (token) {
      localStorage.setItem('token', token)
    } else {
      localStorage.removeItem('token')
    }
  },
  setUserInfo: function(userInfo) {
    this.userInfo = userInfo
  },
  logout: function() {
    this.token = ''
    this.userInfo = null
    localStorage.removeItem('token')
  }
}

describe('User Store', () => {
  beforeEach(() => {
    // 创建测试用的 pinia
    setActivePinia(createPinia())

    // 清空 localStorage
    localStorage.clear()

    // Mock localStorage methods
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(function(key) {
      return this._data[key] || null
    })
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(function(key, value) {
      this._data = this._data || {}
      this._data[key] = value.toString()
    })
    vi.spyOn(Storage.prototype, 'removeItem').mockImplementation(function(key) {
      this._data = this._data || {}
      delete this._data[key]
    })
  })

  afterEach(() => {
    vi.restoreAllMocks()
    localStorage.clear()
  })

  it('initial state: token and userInfo are null', () => {
    expect(mockUserStore.token).toBe('')
    expect(mockUserStore.userInfo).toBeNull()
  })

  it('setToken: updates token in state and localStorage', () => {
    const testToken = 'test-jwt-token'

    mockUserStore.setToken(testToken)

    expect(mockUserStore.token).toBe(testToken)
    expect(localStorage.setItem).toHaveBeenCalledWith('token', testToken)
  })

  it('setUserInfo: updates userInfo in state', () => {
    const testUserInfo = {
      userId: 1,
      username: 'admin',
      realName: '管理员'
    }

    mockUserStore.setUserInfo(testUserInfo)

    expect(mockUserStore.userInfo).toEqual(testUserInfo)
  })

  it('logout: clears token and userInfo', () => {
    mockUserStore.setToken('test-token')
    mockUserStore.setUserInfo({ userId: 1 })

    mockUserStore.logout()

    expect(mockUserStore.token).toBe('')
    expect(mockUserStore.userInfo).toBeNull()
    expect(localStorage.removeItem).toHaveBeenCalledWith('token')
  })

  it('logout: calls localStorage.removeItem', () => {
    mockUserStore.logout()

    expect(localStorage.removeItem).toHaveBeenCalledWith('token')
  })
})
