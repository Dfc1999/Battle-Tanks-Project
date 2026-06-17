import { Injectable } from '@angular/core';

export interface LatencySample {
    eventType: string;
    sentTimestamp: number;
    receivedTimestamp: number;
    latencyMs: number;
}

export interface BenchmarkReport {
    eventType: string;
    sampleCount: number;
    minLatency: number;
    maxLatency: number;
    avgLatency: number;
    p95Latency: number;
}

@Injectable({
    providedIn: 'root'
})
export class BenchmarkService {
    private samples: Map<string, LatencySample[]> = new Map();

    recordSample(eventType: string, sentTimestamp: number): void {
        const receivedTimestamp = Date.now();
        const latencyMs = receivedTimestamp - sentTimestamp;

        if (!this.samples.has(eventType)) {
            this.samples.set(eventType, []);
        }

        const sample: LatencySample = {
            eventType,
            sentTimestamp,
            receivedTimestamp,
            latencyMs
        };

        this.samples.get(eventType)!.push(sample);

        console.log(`[Benchmark] ${eventType}: ${latencyMs}ms (muestras: ${this.samples.get(eventType)!.length})`);
    }

    getReport(): BenchmarkReport[] {
        const reports: BenchmarkReport[] = [];

        this.samples.forEach((samples, eventType) => {
            if (samples.length === 0) return;

            const latencies = samples.map(s => s.latencyMs).sort((a, b) => a - b);
            const sum = latencies.reduce((acc, val) => acc + val, 0);
            const p95Index = Math.floor(latencies.length * 0.95);

            reports.push({
                eventType,
                sampleCount: latencies.length,
                minLatency: latencies[0],
                maxLatency: latencies[latencies.length - 1],
                avgLatency: Math.round(sum / latencies.length * 100) / 100,
                p95Latency: latencies[Math.min(p95Index, latencies.length - 1)]
            });
        });

        return reports;
    }

    printReport(): void {
        const reports = this.getReport();

        if (reports.length === 0) {
            console.log('[Benchmark] No hay muestras de latencia recolectadas aun.');
            return;
        }

        console.log('REPORTE DE BENCHMARKING - SignalR');

        reports.forEach(report => {
            console.log(`Evento: ${report.eventType.padEnd(48)}`);
            console.log(`Muestras: ${String(report.sampleCount).padEnd(44)}`);
            console.log(`Min: ${String(report.minLatency + 'ms').padEnd(49)}`);
            console.log(`Max: ${String(report.maxLatency + 'ms').padEnd(49)}`);
            console.log(`Avg: ${String(report.avgLatency + 'ms').padEnd(49)}`);
            console.log(`P95: ${String(report.p95Latency + 'ms').padEnd(49)}`);
        });

        console.log('Protocolo: SignalR (WebSocket)');
        console.log('Transporte: WebSocket con reconexion automatica');
    }

    clearSamples(): void {
        this.samples.clear();
        console.log('[Benchmark] Muestras limpiadas.');
    }

    getSampleCount(): number {
        let total = 0;
        this.samples.forEach(samples => total += samples.length);
        return total;
    }
}
