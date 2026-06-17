import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend, Counter } from 'k6/metrics';

const registerErrors = new Rate('register_errors');
const registerDuration = new Trend('register_duration');
const error4xx = new Counter('errors_4xx');
const error5xx = new Counter('errors_5xx');

export const options = {
    scenarios: {
        register_load: {
            executor: 'shared-iterations',
            vus: 100,
            iterations: 100,
            maxDuration: '60s',
            exec: 'registerTest',
        },
    },
    thresholds: {
        register_duration: ['p(95)<3000'],
        errors_4xx: ['count<10'],
        errors_5xx: ['count<5'],
    },
};

const BASE_URL = 'http://localhost:5013/api/Auth';

function classifyError(status) {
    if (status >= 400 && status < 500) error4xx.add(1);
    if (status >= 500) error5xx.add(1);
}

export function registerTest() {
    const uniqueId = `${__VU}_${__ITER}_${Date.now()}`;

    const payload = JSON.stringify({
        username: `loadtest_user_${uniqueId}`,
        email: `loadtest_${uniqueId}@test.com`,
        password: 'TestPassword123!',
        firstName: 'Load',
        lastName: 'Test',
    });

    const params = {
        headers: { 'Content-Type': 'application/json' },
    };

    const res = http.post(`${BASE_URL}/register`, payload, params);

    check(res, {
        'Register - status 200': (r) => r.status === 200,
        'Register - response has success field': (r) => {
            try {
                return JSON.parse(r.body).hasOwnProperty('success');
            } catch (e) {
                return false;
            }
        },
        'Register - response time < 3000ms': (r) => r.timings.duration < 3000,
    });

    registerErrors.add(res.status !== 200);
    registerDuration.add(res.timings.duration);
    classifyError(res.status);

    sleep(0.5);
}

export function handleSummary(data) {
    console.log('\n========== RESUMEN PRUEBA DE AUTENTICACION ==========');
    console.log(`Total de requests: ${data.metrics.http_reqs.values.count}`);
    console.log(`Errores 4xx: ${data.metrics.errors_4xx ? data.metrics.errors_4xx.values.count : 0}`);
    console.log(`Errores 5xx: ${data.metrics.errors_5xx ? data.metrics.errors_5xx.values.count : 0}`);
    console.log(`Register p95: ${data.metrics.register_duration ? data.metrics.register_duration.values['p(95)'].toFixed(2) : 'N/A'}ms`);
    console.log('=====================================================\n');

    return {
        stdout: textSummary(data, { indent: ' ', enableColors: true }),
    };
}

import { textSummary } from 'https://jslib.k6.io/k6-summary/0.0.1/index.js';
