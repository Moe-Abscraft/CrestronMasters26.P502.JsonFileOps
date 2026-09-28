import { AfterViewInit, Component, NgZone, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControllerService } from '../service/controller.service';

/**
 * Reload Config, Auto Update and the last-updated time, shown in the page footer.
 * The time only changes when the control system pushes System.LastUpdatedTime,
 * and the Auto Update button state follows System.AutoUpdate_Fb.
 */
@Component({
  standalone: true,
  selector: 'app-config-status',
  templateUrl: './config-status.component.html',
  styleUrl: './config-status.component.scss',
  imports: [CommonModule]
})
export class ConfigStatusComponent implements OnInit, AfterViewInit {
  lastUpdatedTime = '';
  autoUpdate = false;

  constructor(private cs: ControllerService, private zone: NgZone) {}

  ngOnInit(): void {
    this.zone.run(() => {
      this.lastUpdatedTime = this.cs.lastUpdatedTime;
      this.autoUpdate = this.cs.autoUpdate;
    });
  }

  ngAfterViewInit(): void {
    this.cs.lastUpdatedTimeChanged.subscribe(value =>
      this.zone.run(() => this.lastUpdatedTime = value));
    this.cs.autoUpdateChanged.subscribe(value =>
      this.zone.run(() => this.autoUpdate = value));
  }

  reloadConfig() {
    this.cs.reloadConfig();
  }

  toggleAutoUpdate() {
    this.cs.toggleAutoUpdate();
  }

  /** A touch panel keeps focus on the last button tapped, which Bootstrap
   *  paints as pressed. Dropping focus on release lets the button go back. */
  release(event: Event) {
    (event.currentTarget as HTMLElement | null)?.blur();
  }
}
