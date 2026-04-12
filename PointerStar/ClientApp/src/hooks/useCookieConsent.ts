import { useCallback, useState } from 'react'

import {
  acceptCookies,
  hasCookieConsent,
  hasUserRespondedToConsent,
  rejectCookies,
} from '../services/cookies'

export function useCookieConsent() {
  const [state, setState] = useState(() => ({
    hasConsent: hasCookieConsent(),
    hasUserResponded: hasUserRespondedToConsent(),
  }))

  const refresh = useCallback(() => {
    setState({
      hasConsent: hasCookieConsent(),
      hasUserResponded: hasUserRespondedToConsent(),
    })
  }, [])

  const accept = useCallback(() => {
    acceptCookies()
    refresh()
  }, [refresh])

  const reject = useCallback(() => {
    rejectCookies()
    refresh()
  }, [refresh])

  return {
    ...state,
    accept,
    refresh,
    reject,
  }
}
