import { Injectable } from '@angular/core';
import * as L from 'leaflet';
import { SeismicEvent } from '@hazard/contracts';
import { Volcano, VolcanoStatus } from '@hazard/contracts';

@Injectable({ providedIn: 'root' })
export class MarkerLayerService {
  volcanoMarker(v: Volcano, score: number): L.Marker {
    return L.marker([v.latitude, v.longitude], {
      icon: L.divIcon({
        className: 'volcano-marker',
        html: `<span style="background:${this.volcanoColor(score)}">${this.volcanoIcon(v.status)}</span>`,
        iconSize: [30, 30],
        iconAnchor: [15, 15],
      }),
    });
  }
  seismicMarker(e: SeismicEvent): L.CircleMarker {
    return L.circleMarker([e.latitude, e.longitude], {
      radius: Math.max(4, e.magnitude * 2.2),
      color: this.depthColor(e.depthKm),
      fillOpacity: 0.75,
      weight: 2,
    });
  }
  private volcanoColor(score: number): string {
    return score >= 70 ? '#ef4444' : score >= 35 ? '#f59e0b' : '#22c55e';
  }
  private volcanoIcon(status: VolcanoStatus): string {
    return status === VolcanoStatus.Active ? '▲' : status === VolcanoStatus.Dormant ? '△' : '•';
  }
  private depthColor(depth: number): string {
    return depth <= 10 ? '#dc2626' : depth <= 50 ? '#f59e0b' : '#2563eb';
  }
}
