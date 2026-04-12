import { ApplicationInsights } from '@microsoft/applicationinsights-web'

let appInsights: ApplicationInsights | null = null

export function initializeTelemetry(connectionString?: string | null) {
  if (!connectionString || appInsights) {
    return
  }

  appInsights = new ApplicationInsights({
    config: {
      connectionString,
      disableExceptionTracking: false,
      enableAutoRouteTracking: true,
    },
  })

  appInsights.loadAppInsights()
  appInsights.trackPageView()
}

export function trackEvent(name: string, properties?: Record<string, string>) {
  appInsights?.trackEvent({ name }, properties)
}

export function trackTrace(message: string, severityLevel = 1) {
  appInsights?.trackTrace({ message, severityLevel })
}

export function trackException(error: Error, severityLevel = 3) {
  appInsights?.trackException({ error, severityLevel })
}
