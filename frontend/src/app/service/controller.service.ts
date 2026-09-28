import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

/** Which artwork a source button shows. Derived from the source name. */
export type SourceIcon = 'laptop' | 'wireless' | 'pc' | 'cabletv';

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

/** Program audio and mic, as one object so a single Subject covers both. */
export interface AudioState {
  sourceLevel: number;   // 0-100, scaled from the 0-65535 analog
  sourceMuted: boolean;
  micLevel: number;      // 0-100
  micMuted: boolean;
}

declare var CrComLib: any;

/** How many entries the contract defines for each list. */
export const SOURCE_COUNT = 10;
export const DISPLAY_COUNT = 10;

/** System.aPreset[0..4] in the contract. */
export const PRESET_COUNT = 5;

/** Full scale of a Crestron analog join. The UI works in percent. */
export const ANALOG_MAX = 65535;

export const toPercent = (raw: number) =>
  Math.max(0, Math.min(100, Math.round(((raw ?? 0) / ANALOG_MAX) * 100)));

export const toAnalog = (percent: number) =>
  Math.max(0, Math.min(ANALOG_MAX, Math.round((percent / 100) * ANALOG_MAX)));

/**
 * Contract signal names, as authored in Contract Editor (AppContract.cse2j).
 * These strings are resolved to joins by the panel / WebXPanel using the
 * contract file -- never by CrComLib itself, so they must match exactly.
 */
export const Contract = {
  // states: control system -> panel
  LastUpdatedTime: 'System.LastUpdatedTime',
  AutoUpdateFb: 'System.AutoUpdate_Fb',
  Message: 'System.Message',
  SourceName: (i: number) => `Sources.Source[${i}].Name`,
  SourcesSelected: 'Sources.Selected',
  DisplayName: (i: number) => `Displays.Display[${i}].Name`,
  DisplayModel: (i: number) => `Displays.Display[${i}].Model`,
  DisplayRoutedSource: (i: number) => `Displays.Display[${i}].RoutedSource`,
  SourceLevelFb: 'Audio.Source_Level_Fb',
  SourceMuted: 'Audio.Source_Muted',
  MicLevelFb: 'Audio.Mic_Level_Fb',
  MicMuted: 'Audio.Mic_Muted',

  // events: panel -> control system
  ReloadConfig: 'System.ReloadConfig',
  AutoUpdate: 'System.AutoUpdate',
  Separate: 'System.Separate',
  Combine: 'System.Combine',
  SourcesSelect: 'Sources.Select',
  DisplaySelect: (i: number) => `Displays.Display[${i}].Select`,
  DisplayClear: (i: number) => `Displays.Display[${i}].Clear`,
  SourceLevel: 'Audio.Source_Level',
  SourceMute: 'Audio.Source_Mute',
  MicLevel: 'Audio.Mic_Level',
  MicMute: 'Audio.Mic_Mute',
  // Press = a tap (recall), Hold = a 3-second hold (store). The contract has
  // no preset name state, so the buttons are labelled "Preset 1".."Preset 5".
  PresetPress: (i: number) => `System.aPreset[${i}].Press`,
  PresetHold: (i: number) => `System.aPreset[${i}].Hold`,
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
  if (/cable|tv|television/.test(n)) {
    return 'cabletv';
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

  autoUpdate = false;
  autoUpdateChanged: Subject<boolean> = new Subject<boolean>();

  /** Warning text from the control system. Empty means nothing to show. */
  message = '';
  messageChanged: Subject<string> = new Subject<string>();

  audio: AudioState = { sourceLevel: 0, sourceMuted: false, micLevel: 0, micMuted: false };
  audioChanged: Subject<AudioState> = new Subject<AudioState>();

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

    CrComLib.subscribeState('b', Contract.AutoUpdateFb, (value: boolean) => {
      this.autoUpdate = !!value;
      this.autoUpdateChanged.next(this.autoUpdate);
    });

    CrComLib.subscribeState('s', Contract.Message, (value: string) => {
      this.message = value ?? '';
      this.messageChanged.next(this.message);
    });

    CrComLib.subscribeState('n', Contract.SourcesSelected, (value: number) => {
      this.selectedSource = value ?? 0;
      // Feedback is 1-based; index 0 means nothing is selected.
      this.sources.forEach((s, i) => s.selected = this.selectedSource === i + 1);
      this.selectedSourceChanged.next(this.selectedSource);
      this.sourcesChanged.next(this.sources);
    });

    // -------- audio: levels arrive as 0-65535, the UI works in percent

    CrComLib.subscribeState('n', Contract.SourceLevelFb, (value: number) => {
      this.audio.sourceLevel = toPercent(value);
      this.audioChanged.next(this.audio);
    });

    CrComLib.subscribeState('b', Contract.SourceMuted, (value: boolean) => {
      this.audio.sourceMuted = !!value;
      this.audioChanged.next(this.audio);
    });

    CrComLib.subscribeState('n', Contract.MicLevelFb, (value: number) => {
      this.audio.micLevel = toPercent(value);
      this.audioChanged.next(this.audio);
    });

    CrComLib.subscribeState('b', Contract.MicMuted, (value: boolean) => {
      this.audio.micMuted = !!value;
      this.audioChanged.next(this.audio);
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

  /** Ask the control system to flip auto update; the button state follows AutoUpdate_Fb. */
  toggleAutoUpdate() {
    this.pulseDigital(Contract.AutoUpdate);
  }

  /** Room Layout. Momentary -- the contract carries no layout feedback. */
  separateRooms() {
    this.pulseDigital(Contract.Separate);
  }

  combineRooms() {
    this.pulseDigital(Contract.Combine);
  }

  /** Route a source everywhere. The contract event is 1-based. */
  selectSource(index: number) {
    this.sendAnalog(Contract.SourcesSelect, index + 1);
  }

  /** Route the currently selected source to one display. */
  selectDisplay(index: number) {
    this.pulseDigital(Contract.DisplaySelect(index));
  }

  clearDisplay(index: number) {
    this.pulseDigital(Contract.DisplayClear(index));
  }

  /** Levels are sent as the full 0-65535 analog; the slider works in percent. */
  setSourceLevel(percent: number) {
    this.sendAnalog(Contract.SourceLevel, toAnalog(percent));
  }

  setMicLevel(percent: number) {
    this.sendAnalog(Contract.MicLevel, toAnalog(percent));
  }

  /** Mutes are momentary -- the control system flips and reports the state back. */
  toggleSourceMute() {
    this.pulseDigital(Contract.SourceMute);
  }

  toggleMicMute() {
    this.pulseDigital(Contract.MicMute);
  }

  /** A tap on a preset button: System.aPreset[i].Press. */
  recallPreset(index: number) {
    this.pulseDigital(Contract.PresetPress(index));
  }

  /** A three-second hold on a preset button: System.aPreset[i].Hold. */
  storePreset(index: number) {
    this.pulseDigital(Contract.PresetHold(index));
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
