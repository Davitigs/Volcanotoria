export interface SeismicEvent {
  id: string;
  magnitude: number;
  latitude: number;
  longitude: number;
  depthKm: number;
  occurredAt: string;
  place: string;
  source: string;
  ingestedAt?: string;
}
