export interface HazardScore {
  volcanoId: number;
  score: number;
  contributingEvents: string[];
  calculatedAt: string;
  methodology: string;
}
