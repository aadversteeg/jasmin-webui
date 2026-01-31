// JavaScript helper for SSE using native EventSource API
window.eventSourceHelper = {
    _connections: {},
    _counter: 0,

    connect: function (url, dotNetHelper) {
        const connectionId = ++this._counter;
        dotNetHelper._connectionId = connectionId;

        console.log('[EventSource] Connecting to:', url);

        // Close any existing connection
        this.disconnect(connectionId);

        const eventSource = new EventSource(url);

        eventSource.onopen = function (event) {
            console.log('[EventSource] Connection opened');
            dotNetHelper.invokeMethodAsync('OnConnected');
        };

        eventSource.onerror = function (event) {
            console.log('[EventSource] Error event, readyState:', eventSource.readyState);
            if (eventSource.readyState === EventSource.CLOSED) {
                dotNetHelper.invokeMethodAsync('OnDisconnected');
            } else if (eventSource.readyState === EventSource.CONNECTING) {
                // Reconnecting - don't report as error yet
                console.log('[EventSource] Reconnecting...');
            } else {
                dotNetHelper.invokeMethodAsync('OnError', 'Connection error occurred');
            }
        };

        eventSource.addEventListener('mcp-server-event', function (event) {
            console.log('[EventSource] Received mcp-server-event:', event.data.substring(0, 100));
            dotNetHelper.invokeMethodAsync('OnEventReceived', event.data);
        });

        // Also listen for generic message events (fallback)
        eventSource.onmessage = function (event) {
            console.log('[EventSource] Received message:', event.data.substring(0, 100));
        };

        this._connections[connectionId] = eventSource;

        console.log('[EventSource] EventSource created, readyState:', eventSource.readyState);
    },

    disconnect: function (connectionId) {
        const eventSource = this._connections[connectionId];
        if (eventSource) {
            console.log('[EventSource] Disconnecting:', connectionId);
            eventSource.close();
            delete this._connections[connectionId];
        }
    }
};
