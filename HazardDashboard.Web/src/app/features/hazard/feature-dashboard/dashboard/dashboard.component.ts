import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { ApiService } from '@hazard/data-access';
import { DetailPanelComponent } from '@hazard/ui-detail';
import { MapComponent } from '@hazard/ui-map';
import { HazardScore } from '@hazard/contracts';
import { SeismicEvent } from '@hazard/contracts';
import { Volcano } from '@hazard/contracts';
import { SignalrService } from '@hazard/data-access';

@Component({
  selector: 'haz-dashboard',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MapComponent, DetailPanelComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  private readonly api = inject(ApiService);
  readonly live = inject(SignalrService);

  readonly volcanoes = signal<Volcano[]>([]);
  readonly events = signal<SeismicEvent[]>([]);
  readonly selected = signal<Volcano | null>(null);
  readonly score = signal<HazardScore | null>(null);
  readonly scores = signal<Record<number, number>>({});
  readonly days = signal(7);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly liveEvents = computed(() => this.live.newEvents());

  ngOnInit(): void {
    this.load();

    this.api.volcanoes().subscribe({
      next: (volcanoes) => this.volcanoes.set(volcanoes),
      error: (error) => this.error.set(this.message(error)),
    });

    void this.live.connect();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    const since = new Date(Date.now() - this.days() * 86400000);
    this.api.seismicEvents(since).subscribe({
      next: (events) => {
        this.events.set(events);
        this.loading.set(false);
      },
      error: (error) => {
        this.error.set(this.message(error));
        this.loading.set(false);
      },
    });
  }

  select(volcano: Volcano): void {
    this.selected.set(volcano);
    this.score.set(null);

    this.api.hazardScore(volcano.id).subscribe({
      next: (score) => {
        this.score.set(score);
        this.scores.update((all) => ({ ...all, [volcano.id]: score.score }));
      },
      error: (error) => this.error.set(this.message(error)),
    });
  }

  close(): void {
    this.selected.set(null);
  }

  mergedEvents(): SeismicEvent[] {
    const all = [...this.events(), ...this.liveEvents()];
    return [...new Map(all.map((event) => [event.id, event])).values()];
  }

  private message(error: any): string {
    return error?.error?.detail ?? 'Unable to load monitoring data.';
  }
}
