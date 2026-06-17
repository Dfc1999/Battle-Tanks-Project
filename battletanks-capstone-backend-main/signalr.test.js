import ws from 'k6/ws';
import { check, sleep } from 'k6';
import { Rate, Trend, Counter } from 'k6/metrics';

const connectionErrors = new Rate('connection_errors');
const moveLatency = new Trend('move_latency_ms');
const messagesReceived = new Counter('messages_received');
const messagesSent = new Counter('messages_sent');

export const options = {
    scenarios: {
        signalr_players: {
            executor: 'constant-vus',
            vus: 20,
            duration: '30s',
            exec: 'signalrMovement',
        },
    },
    thresholds: {
        move_latency_ms: ['p(95)<200'],
        connection_errors: ['rate<0.1'],
    },
};

const SIGNALR_URL = 'ws://localhost:5013/game';

export function signalrMovement() {
    const playerId = `player_load_${__VU}`;
    const playerName = `LoadPlayer${__VU}`;
    const roomId = `load-test-room-${Math.floor(__VU / 4)}`;

    const url = `${SIGNALR_URL}?roomId=${roomId}&playerId=${playerId}&playerName=${playerName}`;

    const res = ws.connect(url, {}, function (socket) {
        let connected = false;
        let lastSendTime = 0;

        socket.on('open', function () {
            connected = true;

            const negotiatePayload = JSON.stringify({ protocol: 'json', version: 1 }) + '\x1e';
            socket.send(negotiatePayload);

            sleep(1);

            for (let i = 0; i < 10; i++) {
                lastSendTime = Date.now();

                const moveMessage = JSON.stringify({
                    type: 1,
                    target: 'SendMessage',
                    arguments: [{
                        Type: 'PLAYER_MOVE',
                        Data: {
                            playerId: playerId,
                            x: Math.random() * 800,
                            y: Math.random() * 600,
                            rotation: Math.random() * 360
                        }
                    }]
                }) + '\x1e';

                socket.send(moveMessage);
                messagesSent.add(1);

                sleep(0.3);
            }

            const hitMessage = JSON.stringify({
                type: 1,
                target: 'SendMessage',
                arguments: [{
                    Type: 'PLAYER_HIT',
                    Data: {
                        targetPlayerId: `player_load_${(__VU % 20) + 1}`,
                        shooterId: playerId,
                        damage: 10
                    }
                }]
            }) + '\x1e';

            lastSendTime = Date.now();
            socket.send(hitMessage);
            messagesSent.add(1);

            sleep(2);
        });

        socket.on('message', function (data) {
            messagesReceived.add(1);

            if (lastSendTime > 0) {
                const latency = Date.now() - lastSendTime;
                moveLatency.add(latency);
            }
        });

        socket.on('error', function (e) {
            connectionErrors.add(1);
            console.log(`Error en VU ${__VU}: ${e.error()}`);
        });

        socket.on('close', function () { });

        socket.setTimeout(function () {
            socket.close();
        }, 15000);
    });

    check(res, {
        'WebSocket connection successful': (r) => r && r.status === 101,
    });

    connectionErrors.add(!res || res.status !== 101);
}

export function handleSummary(data) {
    console.log('\n========== RESUMEN PRUEBA SIGNALR ==========');
    console.log(`Mensajes enviados: ${data.metrics.messages_sent ? data.metrics.messages_sent.values.count : 0}`);
    console.log(`Mensajes recibidos: ${data.metrics.messages_received ? data.metrics.messages_received.values.count : 0}`);
    if (data.metrics.move_latency_ms) {
        console.log(`Latencia promedio: ${data.metrics.move_latency_ms.values.avg.toFixed(2)}ms`);
        console.log(`Latencia p95: ${data.metrics.move_latency_ms.values['p(95)'].toFixed(2)}ms`);
        console.log(`Latencia max (pico): ${data.metrics.move_latency_ms.values.max.toFixed(2)}ms`);
    }
    console.log(`Tasa de errores de conexion: ${data.metrics.connection_errors ? (data.metrics.connection_errors.values.rate * 100).toFixed(2) : 0}%`);
    console.log('=============================================\n');

    return {
        stdout: textSummary(data, { indent: ' ', enableColors: true }),
    };
}

import { textSummary } from 'https://jslib.k6.io/k6-summary/0.0.1/index.js';
