import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { HazardScore } from '@hazard/contracts';
import { SeismicEvent } from '@hazard/contracts';
import { Volcano } from '@hazard/contracts';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5001/api';
  volcanoes(): Observable<Volcano[]> {
    return this.http.get<Volcano[]>(`${this.baseUrl}/volcanoes`);
  }
  seismicEvents(since: Date): Observable<SeismicEvent[]> {
    return this.http.get<SeismicEvent[]>(`${this.baseUrl}/seismic-events`, {
      params: new HttpParams().set('since', since.toISOString()),
    });
  }
  hazardScore(id: number): Observable<HazardScore> {
    return this.http.get<HazardScore>(`${this.baseUrl}/volcanoes/${id}/hazard-score`);
  }
}
