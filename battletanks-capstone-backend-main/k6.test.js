import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

const errorRate = new Rate('errors');
const playerListDuration = new Trend('player_list_duration');

export const options = {
  stages: [
    { duration: '10s', target: 5 },
    { duration: '20s', target: 10 },
    { duration: '10s', target: 0 },
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'],
    errors: ['rate<0.1'],
  },
};

const BASE_URL = 'http://localhost:5013/api';

export default function () {
  const playersRes = http.get(`${BASE_URL}/Players`);

  check(playersRes, {
    'GET /api/Players - status 200': (r) => r.status === 200,
    'GET /api/Players - response time < 500ms': (r) => r.timings.duration < 500,
    'GET /api/Players - returns array': (r) => {
      try {
        const body = JSON.parse(r.body);
        return Array.isArray(body);
      } catch (e) {
        return false;
      }
    },
  });

  errorRate.add(playersRes.status !== 200);
  playerListDuration.add(playersRes.timings.duration);

  sleep(1);

  const scoresRes = http.get(`${BASE_URL}/Scores`);

  check(scoresRes, {
    'GET /api/Scores - status 200': (r) => r.status === 200,
    'GET /api/Scores - response time < 500ms': (r) => r.timings.duration < 500,
  });

  errorRate.add(scoresRes.status !== 200);

  sleep(1);

  const benchmarkRes = http.get(`${BASE_URL}/Benchmark/run`);

  check(benchmarkRes, {
    'GET /api/Benchmark/run - status 200': (r) => r.status === 200,
    'GET /api/Benchmark/run - response time < 2000ms': (r) => r.timings.duration < 2000,
  });

  errorRate.add(benchmarkRes.status !== 200);

  sleep(1);
}

export function handleSummary(data) {
  console.log('\n========== RESUMEN DE PRUEBA K6 ==========');
  console.log(`Total de requests: ${data.metrics.http_reqs.values.count}`);
  console.log(`Duración promedio: ${data.metrics.http_req_duration.values.avg.toFixed(2)}ms`);
  console.log(`Duración p95: ${data.metrics.http_req_duration.values['p(95)'].toFixed(2)}ms`);
  console.log(`Tasa de errores: ${(data.metrics.errors.values.rate * 100).toFixed(2)}%`);
  console.log('============================================\n');

  return {
    stdout: textSummary(data, { indent: ' ', enableColors: true }),
  };
}

import { textSummary } from 'https://jslib.k6.io/k6-summary/0.0.1/index.js';
