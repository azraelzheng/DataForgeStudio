import { describe, it, expect } from 'vitest'

describe('工具函数测试示例', () => {
  describe('类型检查', () => {
    it('should identify strings correctly', () => {
      expect(typeof 'hello').toBe('string')
      expect(typeof '').toBe('string')
    })

    it('should identify numbers correctly', () => {
      expect(typeof 42).toBe('number')
      expect(typeof 3.14).toBe('number')
    })

    it('should identify objects correctly', () => {
      expect(typeof {}).toBe('object')
      expect(typeof []).toBe('object') // 数组在 JavaScript 中也是对象
    })
  })

  describe('数组操作', () => {
    it('should map array elements', () => {
      const arr = [1, 2, 3]
      const doubled = arr.map(x => x * 2)
      expect(doubled).toEqual([2, 4, 6])
    })

    it('should filter array elements', () => {
      const arr = [1, 2, 3, 4, 5]
      const evens = arr.filter(x => x % 2 === 0)
      expect(evens).toEqual([2, 4])
    })

    it('should reduce array elements', () => {
      const arr = [1, 2, 3, 4, 5]
      const sum = arr.reduce((acc, x) => acc + x, 0)
      expect(sum).toBe(15)
    })
  })

  describe('字符串操作', () => {
    it('should concatenate strings', () => {
      expect('Hello' + ' ' + 'World').toBe('Hello World')
    })

    it('should convert to uppercase', () => {
      expect('hello'.toUpperCase()).toBe('HELLO')
    })

    it('should convert to lowercase', () => {
      expect('WORLD'.toLowerCase()).toBe('world')
    })
  })
})
