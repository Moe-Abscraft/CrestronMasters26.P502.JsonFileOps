import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

/** Which artwork a source button shows. Derived from the source name. */
export type SourceIcon = 'laptop' | 'wireless' | 'pc';

export interface Source {
  name: string;
  icon: SourceIcon;
  selected: boolean;
}

export interface Display {
  name: string;
  model: string;
  routedSource: string;
}

declare var CrComLib: any;

/** How many entries the contract defines for each list. */
export const SOURCE_COUNT = 10;
export const DISPLAY_COUNT = 10;

/**
 * Contract signal names, as authored in Contract Editor (AppContract.cse2j).
 * These strings are resolved to joins by the panel / WebXPanel using the
 * contract file -- never by CrComLib itself, so they must match exactly.
 */
/** Analog joins are 0-65535 end to end, sliders included. */
export const MAX_LEVEL = 65535;

export const Contract = {
  // states: control system -> panel
  LastUpdatedTime: 'System.LastUpdatedTime',
  Message: 'System.Message',
  AutoUpdateFb: 'System.AutoUpdate_Fb',
  AudioSourceMuted: 'Audio.Source_Muted',
  AudioMicMuted: 'Audio.Mic_Muted',
  AudioSourceLevelFb: 'Audio.Source_Level_Fb',
  AudioMicLevelFb: 'Audio.Mic_Level_Fb',
  SourceName: (i: number) => `Sources.Source[${i}].Name`,
  SourcesSelected: 'Sources.Selected',
  DisplayName: (i: number) => `Displays.Display[${i}].Name`,
  DisplayModel: (i: number) => `Displays.Display[${i}].Model`,
  DisplayRoutedSource: (i: number) => `Displays.Display[${i}].RoutedSource`,

  // events: panel -> control system
  ReloadConfig: 'System.ReloadConfig',
  AutoUpdate: 'System.AutoUpdate',
  AudioSourceMute: 'Audio.Source_Mute',
  AudioMicMute: 'Audio.Mic_Mute',
  AudioSourceLevel: 'Audio.Source_Level',
  AudioMicLevel: 'Audio.Mic_Level',
  SourcesSelect: 'Sources.Select',
  DisplaySelect: (i: number) => `Displays.Display[${i}].Select`,
  DisplayClear: (i: number) => `Displays.Display[${i}].Clear`,
} as const;

/**
 * Pick the button artwork from the source name. Matched longest-first so
 * "wireless pc" resolves to wireless rather than pc.
 */
export function iconForSource(name: string): SourceIcon {
  const n = (name || '').toLowerCase();
  if (/wireless|airplay|air ?play|cast|clickshare|byod|share|miracast|teams|zoom/.test(n)) {
    return 'wireless';
  }
  if (/laptop|notebook|hdmi|usb-?c|table|guest/.test(n)) {
    return 'laptop';
  }
  if (/\bpc\b|desktop|computer|room ?pc|tower/.test(n)) {
    return 'pc';
  }
  return 'laptop';
}

@Injectable({
  providedIn: 'root'
})
export class ControllerService {

  sources: Source[] = [];
  sourcesChanged: Subject<Source[]> = new Subject<Source[]>();

  displays: Display[] = [];
  displaysChanged: Subject<Display[]> = new Subject<Display[]>();

  /** 1-based index of the selected source; 0 = nothing selected. */
  selectedSource = 0;
  selectedSourceChanged: Subject<number> = new Subject<number>();

  connected = false;
  connectedChanged: Subject<boolean> = new Subject<boolean>();

  page: number = 1;
  pageChanged: Subject<number> = new Subject<number>();

  lastUpdatedTime = '';
  lastUpdatedTimeChanged: Subject<string> = new Subject<string>();

  /** Empty when the config loaded cleanly; otherwise the banner text to show. */
  message = '';
  messageChanged: Subject<string> = new Subject<string>();

  autoUpdate = false;
  autoUpdateChanged: Subject<boolean> = new Subject<boolean>();

  sourceMuted = false;
  sourceMutedChanged: Subject<boolean> = new Subject<boolean>();

  micMuted = false;
  micMutedChanged: Subject<boolean> = new Subject<boolean>();

  sourceLevel = 0;
  sourceLevelChanged: Subject<number> = new Subject<number>();

  micLevel = 0;
  micLevelChanged: Subject<number> = new Subject<number>();

  constructor() {
    console.log('Controller Service Started');

    for (let index = 0; index < SOURCE_COUNT; index++) {
      this.sources.push({ name: '', icon: 'laptop', selected: false });
    }
    for (let index = 0; index < DISPLAY_COUNT; index++) {
      this.displays.push({ name: '', model: '', routedSource: '' });
    }

    // Deferred so CrComLib has finished loading before anything subscribes.
    setTimeout(() => this.subscribeAll(), 0);

    this.pageChanged.subscribe(value => this.page = value);
  }

  // ---------------------------------------------------------------- subscriptions

  private subscribeAll() {
    CrComLib.subscribeState('b', `10`, (value: boolean) => {
      this.connected = value;
      this.connectedChanged.next(this.connected);
    });

    CrComLib.subscribeState('s', Contract.LastUpdatedTime, (value: string) => {
      this.lastUpdatedTime = value ?? '';
      this.lastUpdatedTimeChanged.next(this.lastUpdatedTime);
    });

    CrComLib.subscribeState('s', Contract.Message, (value: string) => {
      this.message = value ?? '';
      this.messageChanged.next(this.message);
    });

    CrComLib.subscribeState('b', Contract.AutoUpdateFb, (value: boolean) => {
      this.autoUpdate = !!value;
      this.autoUpdateChanged.next(this.autoUpdate);
    });

    CrComLib.subscribeState('b', Contract.AudioSourceMuted, (value: boolean) => {
      this.sourceMuted = !!value;
      this.sourceMutedChanged.next(this.sourceMuted);
    });

    CrComLib.subscribeState('b', Contract.AudioMicMuted, (value: boolean) => {
      this.micMuted = !!value;
      this.micMutedChanged.next(this.micMuted);
    });

    CrComLib.subscribeState('n', Contract.AudioSourceLevelFb, (value: number) => {
      this.sourceLevel = value ?? 0;
      this.sourceLevelChanged.next(this.sourceLevel);
    });

    CrComLib.subscribeState('n', Contract.AudioMicLevelFb, (value: number) => {
      this.micLevel = value ?? 0;
      this.micLevelChanged.next(this.micLevel);
    });

    CrComLib.subscribeState('n', Contract.SourcesSelected, (value: number) => {
      this.selectedSource = value ?? 0;
      // Feedback is 1-based; index 0 means nothing is selected.
      this.sources.forEach((s, i) => s.selected = this.selectedSource === i + 1);
      this.selectedSourceChanged.next(this.selectedSource);
      this.sourcesChanged.next(this.sources);
    });

    for (let i = 0; i < SOURCE_COUNT; i++) {
      const index = i;
      CrComLib.subscribeState('s', Contract.SourceName(index), (value: string) => {
        const name = value ?? '';
        this.sources[index].name = name;
        this.sources[index].icon = iconForSource(name);
        this.sourcesChanged.next(this.sources);
      });
    }

    for (let i = 0; i < DISPLAY_COUNT; i++) {
      const index = i;

      CrComLib.subscribeState('s', Contract.DisplayName(index), (value: string) => {
        this.displays[index].name = value ?? '';
        this.displaysChanged.next(this.displays);
      });

      CrComLib.subscribeState('s', Contract.DisplayRoutedSource(index), (value: string) => {
        this.displays[index].routedSource = value ?? '';
        this.displaysChanged.next(this.displays);
      });

      CrComLib.subscribeState('s', Contract.DisplayModel(index), (value: string) => {
        this.displays[index].model = value ?? '';
        this.displaysChanged.next(this.displays);
      });
    }
  }

  // ---------------------------------------------------------------- actions

  /** Ask the control system to re-read the room config from disk. */
  reloadConfig() {
    this.pulseDigital(Contract.ReloadConfig);
  }

  /** Ask the control system to flip auto update; the button follows AutoUpdate_Fb. */
  toggleAutoUpdate() {
    this.pulseDigital(Contract.AutoUpdate);
  }

  // Audio. Every one of these only sends the press -- the button and slider positions
  // come back from the control system, which gets them from the device. Never paint the
  // new state locally and hope it matches.

  toggleSourceMute() {
    this.pulseDigital(Contract.AudioSourceMute);
  }

  toggleMicMute() {
    this.pulseDigital(Contract.AudioMicMute);
  }

  setSourceLevel(level: number) {
    this.sendAnalog(Contract.AudioSourceLevel, this.clampLevel(level));
  }

  setMicLevel(level: number) {
    this.sendAnalog(Contract.AudioMicLevel, this.clampLevel(level));
  }

  private clampLevel(level: number): number {
    if (!Number.isFinite(level)) return 0;
    return Math.max(0, Math.min(MAX_LEVEL, Math.round(level)));
  }

  /** Route a source everywhere. The contract event is 1-based. */
  selectSource(index: number) {
    this.sendAnalog(Contract.SourcesSelect, index + 1);
  }

  selectDisplay(index: number) {
    this.pulseDigital(Contract.DisplaySelect(index));
  }

  clearDisplay(index: number) {
    this.pulseDigital(Contract.DisplayClear(index));
  }

  // ---------------------------------------------------------------- primitives

  pulseDigital(join: string) {
    CrComLib.publishEvent('b', join, true);
    setTimeout(() => {
      CrComLib.publishEvent('b', join, false);
    }, 10);
  }

  digitalPress(join: string) {
    CrComLib.publishEvent('b', join, true);
  }

  digitalRelease(join: string) {
    CrComLib.publishEvent('b', join, false);
  }

  sendAnalog(join: string, value: number) {
    CrComLib.publishEvent('n', join, value);
  }

  sendSerial(join: string, value: string) {
    CrComLib.publishEvent('s', join, value);
  }
}
