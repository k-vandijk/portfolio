(function () {
    'use strict';

    if (!('serviceWorker' in navigator) || !('PushManager' in window)) {
        console.log('Push notifications not supported.');
        return;
    }

    function urlBase64ToUint8Array(base64String) {
        var padding = '='.repeat((4 - base64String.length % 4) % 4);
        var base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
        var rawData = atob(base64);
        var outputArray = new Uint8Array(rawData.length);
        for (var i = 0; i < rawData.length; i++) {
            outputArray[i] = rawData.charCodeAt(i);
        }
        return outputArray;
    }

    async function subscribeToPush() {
        try {
            var registration = await navigator.serviceWorker.ready;

            // Check if already subscribed
            var existingSubscription = await registration.pushManager.getSubscription();
            if (existingSubscription) {
                console.log('Already subscribed to push notifications.');
                return;
            }

            // Fetch VAPID public key from server
            var response = await fetch('/notifications/vapid-public-key');
            if (!response.ok) {
                console.log('Could not fetch VAPID public key.');
                return;
            }

            var data = await response.json();
            var applicationServerKey = urlBase64ToUint8Array(data.publicKey);

            var subscription = await registration.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: applicationServerKey
            });

            // Send subscription to server
            var subscriptionJson = subscription.toJSON();
            await fetch('/notifications/subscribe', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    endpoint: subscriptionJson.endpoint,
                    p256dh: subscriptionJson.keys.p256dh,
                    auth: subscriptionJson.keys.auth
                })
            });

            console.log('Push notification subscription saved.');
        } catch (err) {
            console.log('Push subscription failed:', err);
        }
    }

    subscribeToPush();
})();
