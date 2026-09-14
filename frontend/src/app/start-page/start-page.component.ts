import { AfterViewInit, Component, NgZone, OnInit } from '@angular/core';
import { ControllerService, Source, Display } from '../service/controller.service';
import { CommonModule } from '@angular/common';

@Component({
  standalone: true,
  selector: 'app-start-page',
  templateUrl: './start-page.component.html',
  styleUrl: './start-page.component.scss',
  imports: [CommonModule]
})
export class StartPageComponent implements OnInit, AfterViewInit {
  page = 1;
  sources: Source[] = [];
  displays: Display[] = [];
  selectedSource = 0;

  /** Source icon key -> artwork. Keyed by the icon field the service sets. */
  private readonly sourceIcons: Record<string, string> = {
    laptop: 'assets/img/laptop-duotone-thin.svg',
    wireless: 'assets/img/airplay-duotone-thin.svg',
    pc: 'assets/img/computer-duotone-thin-full.svg',
  };
  readonly displayIcon = 'assets/img/tv-duotone-thin.svg';

  constructor(private cs: ControllerService, private zone: NgZone) {}

  // Initial values only -- whatever the service already holds.
  ngOnInit(): void {
    this.zone.run(() => {
      this.page = this.cs.page;
      this.sources = this.cs.sources;
      this.displays = this.cs.displays;
      this.selectedSource = this.cs.selectedSource;
    });
  }

  // Later changes -- every callback arrives from CrComLib outside Angular,
  // so each one is marshalled back in with zone.run.
  ngAfterViewInit(): void {
    this.cs.sourcesChanged.subscribe(value =>
      this.zone.run(() => this.sources = value));

    this.cs.displaysChanged.subscribe(value =>
      this.zone.run(() => this.displays = value));

    this.cs.selectedSourceChanged.subscribe(value =>
      this.zone.run(() => this.selectedSource = value));

    this.cs.pageChanged.subscribe(value =>
      this.zone.run(() => this.page = value));
  }

  /** Only entries the control system has actually named are shown. */
  get visibleSources(): Source[] {
    return this.sources.filter(s => s.name.length > 0);
  }

  get visibleDisplays(): Display[] {
    return this.displays.filter(d => d.name.length > 0);
  }

  iconFor(source: Source): string {
    return this.sourceIcons[source.icon] ?? this.sourceIcons['laptop'];
  }

  /** Index within the full array, which is what the contract joins are keyed on. */
  indexOfSource(source: Source): number {
    return this.sources.indexOf(source);
  }

  indexOfDisplay(display: Display): number {
    return this.displays.indexOf(display);
  }

  sourceSet(source: Source) {
    this.cs.selectSource(this.indexOfSource(source));
  }

  trackByIndex(index: number): number {
    return index;
  }

  setPage(page: number) {
    this.cs.pageChanged.next(page);
  }
}
