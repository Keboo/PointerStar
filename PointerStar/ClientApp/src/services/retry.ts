export interface RetryContext {
  attempt: number
  delayMs: number
  error: unknown
}

export interface RetryWithJitterOptions {
  baseDelayMs?: number
  jitterRatio?: number
  maxAttempts?: number
  maxDelayMs?: number
  onRetry?: (context: RetryContext) => void
  random?: () => number
  shouldRetry?: (error: unknown) => boolean
  signal?: AbortSignal
  sleep?: (delayMs: number, signal?: AbortSignal) => Promise<void>
}

const transientStatusCodes = new Set([408, 425, 429, 500, 502, 503, 504])

export class RetryableRequestError extends Error {
  public readonly status: number

  public constructor(message: string, status: number) {
    super(message)
    this.name = 'RetryableRequestError'
    this.status = status
  }
}

export function isAbortError(error: unknown) {
  return error instanceof DOMException && error.name === 'AbortError'
}

export function waitForDelay(delayMs: number, signal?: AbortSignal) {
  return new Promise<void>((resolve, reject) => {
    if (signal?.aborted) {
      reject(new DOMException('The retry was cancelled.', 'AbortError'))
      return
    }

    let timeoutId: ReturnType<typeof globalThis.setTimeout>
    const onAbort = () => {
      globalThis.clearTimeout(timeoutId)
      signal?.removeEventListener('abort', onAbort)
      reject(new DOMException('The retry was cancelled.', 'AbortError'))
    }

    timeoutId = globalThis.setTimeout(() => {
      signal?.removeEventListener('abort', onAbort)
      resolve()
    }, delayMs)

    signal?.addEventListener('abort', onAbort, { once: true })
  })
}

export function getJitteredDelayMs(
  attempt: number,
  {
    baseDelayMs = 300,
    jitterRatio = 0.3,
    maxDelayMs = 3_000,
    random = Math.random,
  }: Pick<RetryWithJitterOptions, 'baseDelayMs' | 'jitterRatio' | 'maxDelayMs' | 'random'> = {},
) {
  const delayMs = Math.min(maxDelayMs, baseDelayMs * 2 ** Math.max(0, attempt - 1))
  const jitterWindow = Math.max(1, Math.round(delayMs * jitterRatio))
  const jitterOffset = Math.round((random() * 2 - 1) * jitterWindow)
  return Math.max(0, delayMs + jitterOffset)
}

export async function retryWithJitter<T>(
  operation: () => Promise<T>,
  options: RetryWithJitterOptions = {},
) {
  const {
    maxAttempts = 4,
    onRetry,
    shouldRetry = () => true,
    signal,
    sleep = waitForDelay,
  } = options

  for (let attempt = 1; ; attempt += 1) {
    try {
      return await operation()
    } catch (error) {
      if (isAbortError(error) || !shouldRetry(error) || attempt >= maxAttempts) {
        throw error
      }

      const delayMs = getJitteredDelayMs(attempt, options)
      onRetry?.({ attempt, delayMs, error })
      await sleep(delayMs, signal)
    }
  }
}

export function isTransientHttpStatus(status: number) {
  return transientStatusCodes.has(status)
}

function isRetryableFetchError(error: unknown) {
  return error instanceof RetryableRequestError || error instanceof TypeError
}

export function fetchWithRetry(
  input: RequestInfo | URL,
  init?: RequestInit,
  options?: RetryWithJitterOptions,
) {
  return retryWithJitter(async () => {
    const response = await fetch(input, init)
    if (!response.ok && isTransientHttpStatus(response.status)) {
      throw new RetryableRequestError(
        `Transient request failure: ${response.status} ${response.statusText}`,
        response.status,
      )
    }

    return response
  }, {
    ...options,
    shouldRetry: options?.shouldRetry ?? isRetryableFetchError,
  })
}
