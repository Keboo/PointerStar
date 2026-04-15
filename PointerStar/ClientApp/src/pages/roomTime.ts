export function getElapsedTimeLabel(startAt?: string | null, offsetMs = 0) {
  if (!startAt) {
    return ''
  }

  const elapsedMs = Math.max(0, Date.now() + offsetMs - new Date(startAt).getTime())
  const totalSeconds = Math.floor(elapsedMs / 1_000)
  const hours = Math.floor(totalSeconds / 3_600)
  const seconds = (totalSeconds % 60).toString().padStart(2, '0')

  if (hours > 0) {
    const minutes = Math.floor((totalSeconds % 3_600) / 60)
      .toString()
      .padStart(2, '0')

    return `${hours}:${minutes}:${seconds}`
  }

  const minutes = Math.floor(totalSeconds / 60)
    .toString()
    .padStart(2, '0')

  return `${minutes}:${seconds}`
}
