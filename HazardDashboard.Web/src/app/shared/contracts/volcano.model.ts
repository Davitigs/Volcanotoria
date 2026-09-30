export enum VolcanoStatus {
  Active = 'Active',
  Dormant = 'Dormant',
  Extinct = 'Extinct',
  Unknown = 'Unknown',
}

export interface Volcano {
  id: number;
  name: string;
  latitude: number;
  longitude: number;
  elevationMeters: number;
  country: string;
  lastEruptionYear?: number;
  status: VolcanoStatus;
}
