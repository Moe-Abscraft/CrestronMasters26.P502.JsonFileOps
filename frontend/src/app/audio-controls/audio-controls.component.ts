import { AfterViewInit, Component, Input, NgZone, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AudioState, ControllerService } from '../service/controller.service';

/**
 * Program (source) or mic level, with its mute, as one vertical fader strip.
 * The start page places two of these -- `channel="mic"` on the left edge and
 * `channel="program"` on the right -- so each instance shows only its channel.
 *
 * The sliders work in percent; the service scales to the 0-65535 analog the
 * contract carries. While a finger is down the incoming feedback is ignored,
 * otherwise the control system's echo fights the drag.
 */
@Component({
  standalone: true,
  selector: 'app-audio-controls',
  templateUrl: './audio-controls.component.html',
  styleUrl: './audio-controls.component.scss',
  imports: [CommonModule]
})
export class AudioControlsComponent implements OnInit, AfterViewInit {
  @Input() channel: 'program' | 'mic' = 'program';

  sourceLevel = 0;
  micLevel = 0;
  sourceMuted = false;
  micMuted = false;

  private draggingSource = false;
  private draggingMic = false;

  constructor(private cs: ControllerService, private zone: NgZone) {}

  ngOnInit(): void {
    this.zone.run(() => this.apply(this.cs.audio));
  }

  ngAfterViewInit(): void {
    this.cs.audioChanged.subscribe(value => this.zone.run(() => this.apply(value)));
  }

  private apply(audio: AudioState) {
    if (!this.draggingSource) {
      this.sourceLevel = audio.sourceLevel;
    }
    if (!this.draggingMic) {
      this.micLevel = audio.micLevel;
    }
    this.sourceMuted = audio.sourceMuted;
    this.micMuted = audio.micMuted;
  }

  // -------------------------------------------------------------- source

  onSourceInput(value: string) {
    this.draggingSource = true;
    this.sourceLevel = Number(value);
    this.cs.setSourceLevel(this.sourceLevel);
  }

  onSourceRelease() {
    this.draggingSource = false;
  }

  toggleSourceMute() {
    this.cs.toggleSourceMute();
  }

  // -------------------------------------------------------------- mic

  onMicInput(value: string) {
    this.draggingMic = true;
    this.micLevel = Number(value);
    this.cs.setMicLevel(this.micLevel);
  }

  onMicRelease() {
    this.draggingMic = false;
  }

  toggleMicMute() {
    this.cs.toggleMicMute();
  }
}
