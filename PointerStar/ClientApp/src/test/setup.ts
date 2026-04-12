import '@testing-library/jest-dom/vitest'

class MemoryStorage implements Storage {
  private readonly values = new Map<string, string>()

  public get length() {
    return this.values.size
  }

  public clear() {
    this.values.clear()
  }

  public getItem(key: string) {
    return this.values.get(key) ?? null
  }

  public key(index: number) {
    return [...this.values.keys()][index] ?? null
  }

  public removeItem(key: string) {
    this.values.delete(key)
  }

  public setItem(key: string, value: string) {
    this.values.set(key, value)
  }
}

if (
  typeof window.localStorage === 'undefined' ||
  typeof window.localStorage.clear !== 'function'
) {
  const storage = new MemoryStorage()

  Object.defineProperty(window, 'localStorage', {
    configurable: true,
    value: storage,
    writable: true,
  })
  Object.defineProperty(globalThis, 'localStorage', {
    configurable: true,
    value: storage,
    writable: true,
  })
}
