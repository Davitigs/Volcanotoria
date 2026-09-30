import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { HazardScore } from '@hazard/contracts';
import { Volcano } from '@hazard/contracts';

@Component({
  selector: 'app-detail-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './detail-panel.component.html',
  styleUrl: './detail-panel.component.scss',
})
export class DetailPanelComponent {
  readonly volcano = input<Volcano | null>(null);
  readonly score = input<HazardScore | null>(null);
  readonly closed = output<void>();
}
