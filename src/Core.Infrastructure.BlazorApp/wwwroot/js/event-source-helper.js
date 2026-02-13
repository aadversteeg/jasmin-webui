// JavaScript helper for SSE using native EventSource API
window.eventSourceHelper = {
    _connections: {},
    _counter: 0,

    connect: function (url, dotNetHelper, lastEventId, eventNames, callbackName) {
        const connectionId = ++this._counter;
        dotNetHelper._connectionId = connectionId;

        // Support array or single string for event names
        var names = Array.isArray(eventNames) ? eventNames : (eventNames ? [eventNames] : ['message']);
        var cbName = callbackName || 'OnEventReceived';

        // Close any existing connection
        this.disconnect(connectionId);

        // Append lastEventId to URL if provided
        let connectUrl = url;
        if (lastEventId) {
            const separator = url.includes('?') ? '&' : '?';
            connectUrl = url + separator + 'lastEventId=' + encodeURIComponent(lastEventId);
            console.log('[EventSource] Reconnecting from event ID:', lastEventId);
        }

        console.log('[EventSource] Connecting to:', connectUrl);

        const eventSource = new EventSource(connectUrl);

        // Store connection with lastEventId tracker
        const connection = {
            eventSource: eventSource,
            lastEventId: lastEventId || null
        };

        eventSource.onopen = function (event) {
            console.log('[EventSource] Connection opened');
            dotNetHelper.invokeMethodAsync('OnConnected');
        };

        eventSource.onerror = function (event) {
            console.log('[EventSource] Error event, readyState:', eventSource.readyState);
            if (eventSource.readyState === EventSource.CLOSED) {
                dotNetHelper.invokeMethodAsync('OnDisconnected');
            } else if (eventSource.readyState === EventSource.CONNECTING) {
                // Reconnecting - notify C# about reconnecting state
                console.log('[EventSource] Reconnecting...');
                dotNetHelper.invokeMethodAsync('OnReconnecting');
            } else {
                dotNetHelper.invokeMethodAsync('OnError', 'Connection error occurred');
            }
        };

        // Register a listener for each event name
        names.forEach(function (evtName) {
            eventSource.addEventListener(evtName, function (event) {
                // Track the last event ID
                if (event.lastEventId) {
                    connection.lastEventId = event.lastEventId;
                }
                console.log('[EventSource] Received ' + evtName + ':', event.data.substring(0, 100));
                dotNetHelper.invokeMethodAsync(cbName, event.data, event.lastEventId || '');
            });
        });

        // Also listen for generic message events (fallback)
        eventSource.onmessage = function (event) {
            console.log('[EventSource] Received message:', event.data.substring(0, 100));
        };

        this._connections[connectionId] = connection;

        console.log('[EventSource] EventSource created, readyState:', eventSource.readyState);

        return connectionId;
    },

    getLastEventId: function (connectionId) {
        const connection = this._connections[connectionId];
        return connection ? connection.lastEventId : null;
    },

    disconnect: function (connectionId) {
        const connection = this._connections[connectionId];
        if (connection) {
            console.log('[EventSource] Disconnecting:', connectionId);
            connection.eventSource.close();
            delete this._connections[connectionId];
        }
    }
};
