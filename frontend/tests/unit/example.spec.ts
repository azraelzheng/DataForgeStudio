import { describe, it, expect } from 'vitest'

describe('示例测试', () => {
  it('测试框架配置正确', () => {
    expect(1 + 1).toBe(2)
  })

  it('环境变量已设置', () => {
    expect(typeof window).toBe('object')
    expect(typeof localStorage).toBe('object')
  })
})
