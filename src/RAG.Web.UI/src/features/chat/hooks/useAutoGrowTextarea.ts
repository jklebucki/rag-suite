import { MutableRefObject, useCallback, useEffect } from 'react'

interface Options {
  minRows?: number
  maxRows?: number
}

/**
 * Hook that auto-adjusts a textarea's height between a min and max number of rows.
 * - sets height to 'auto' then uses scrollHeight
 * - clamps to maxRows (shows scrollbar once exceeded)
 */
export function useAutoGrowTextarea(
  ref: MutableRefObject<HTMLTextAreaElement | null> | ((instance: HTMLTextAreaElement | null) => void) | null,
  value: string | undefined,
  options: Options = {}
) {
  const { minRows = 3, maxRows = 10 } = options

  const resize = useCallback(() => {
    const textarea = typeof ref === 'function' ? null : (ref as MutableRefObject<HTMLTextAreaElement | null>)?.current
    if (!textarea) return

    // reset height first so scrollHeight reflects content-only
    textarea.style.height = 'auto'

    // compute line height if available, otherwise fallback
    const computed = window.getComputedStyle(textarea)
    const lineHeightStr = computed.lineHeight
    let lineHeight = 20 // sensible default
    if (lineHeightStr && lineHeightStr.endsWith('px')) {
      const parsed = parseFloat(lineHeightStr)
      if (!Number.isNaN(parsed) && parsed > 0) lineHeight = parsed
    }

    const maxHeight = lineHeight * maxRows
    const minHeight = lineHeight * minRows

    const scrollHeight = textarea.scrollHeight
    const newHeight = Math.max(minHeight, Math.min(scrollHeight, maxHeight))
    textarea.style.height = `${newHeight}px`

    // ensure overflow appears if beyond max
    textarea.style.overflowY = scrollHeight > maxHeight ? 'auto' : 'hidden'
  }, [ref, minRows, maxRows])

  // run on value changes and at initial mount
  useEffect(() => {
    resize()
  }, [value, resize])
}

export default useAutoGrowTextarea
