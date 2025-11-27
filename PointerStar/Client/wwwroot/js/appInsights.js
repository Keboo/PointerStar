// Application Insights JavaScript interop for Blazor WebAssembly
window.appInsights = window.appInsights || {
    isInitialized: false,

    // Initialize Application Insights - called from the SDK snippet in _Host.cshtml
    markInitialized: function () {
        this.isInitialized = true;
    },

    // Track exception to Application Insights
    trackException: function (error, severityLevel) {
        if (window.appInsights && window.appInsights.isInitialized && typeof window.appInsightsInstance !== 'undefined') {
            try {
                window.appInsightsInstance.trackException({
                    exception: new Error(error),
                    severityLevel: severityLevel || 3 // Default to Error
                });
            } catch (e) {
                console.error('Failed to track exception to Application Insights:', e);
            }
        } else {
            console.warn('Application Insights not initialized. Exception not tracked:', error);
        }
    },

    // Track custom event
    trackEvent: function (name, properties) {
        if (window.appInsights && window.appInsights.isInitialized && typeof window.appInsightsInstance !== 'undefined') {
            try {
                window.appInsightsInstance.trackEvent({ name: name }, properties);
            } catch (e) {
                console.error('Failed to track event to Application Insights:', e);
            }
        }
    },

    // Track trace message
    trackTrace: function (message, severityLevel) {
        if (window.appInsights && window.appInsights.isInitialized && typeof window.appInsightsInstance !== 'undefined') {
            try {
                window.appInsightsInstance.trackTrace({
                    message: message,
                    severityLevel: severityLevel || 1 // Default to Information
                });
            } catch (e) {
                console.error('Failed to track trace to Application Insights:', e);
            }
        }
    }
};
