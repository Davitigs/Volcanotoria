import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnDestroy,
  effect,
  inject,
  input,
  output,
  viewChild,
} from '@angular/core';
import * as L from 'leaflet';
import { MarkerLayerService } from '../marker-layer.service';
import { SeismicEvent } from '@hazard/contracts';
import { Volcano } from '@hazard/contracts';

@Component({
  selector: 'app-map',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './map.component.html',
  styleUrl: './map.component.scss',
})
export class MapComponent implements AfterViewInit, OnDestroy {
  private readonly mapElement = viewChild.required<ElementRef<HTMLDivElement>>('map');
  readonly volcanoes = input<Volcano[]>([]);
  readonly events = input<SeismicEvent[]>([]);
  readonly scores = input<Record<number, number>>({});
  readonly volcanoSelected = output<Volcano>();
  private readonly markers = inject(MarkerLayerService);
  private map?: L.Map;
  private layer = L.layerGroup();
  constructor() {
    effect(() => {
      this.volcanoes();
      this.events();
      this.scores();
      queueMicrotask(() => this.render());
    });
  }
  ngAfterViewInit(): void {
    this.map = L.map(this.mapElement().nativeElement).setView([20, 10], 2);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; OpenStreetMap contributors',
    }).addTo(this.map);
    this.layer.addTo(this.map);
    setTimeout(() => this.render(), 0);
  }
  ngOnDestroy(): void {
    this.map?.remove();
  }
  render(): void {
    if (!this.map) return;
    this.layer.clearLayers();
    this.volcanoes().forEach((v) => {
      const m = this.markers.volcanoMarker(v, this.scores()[v.id] ?? 0);
      m.on('click', () => this.volcanoSelected.emit(v));
      m.bindTooltip(v.name);
      m.addTo(this.layer);
    });
    this.events().forEach((e) =>
      this.markers.seismicMarker(e).bindTooltip(`${e.place} · M${e.magnitude}`).addTo(this.layer),
    );
  }
}
