import { Component, NgZone, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControllerService, PRESET_COUNT } from '../service/controller.service';

/** How long a finger has to stay down before the press counts as a store. */
export const HOLD_MS = 3000;

/** How long the "Saved" confirmation stays on the button afterwards. */
const SAVED_MS = 1800;

/**
 * Preset buttons with two actions on one button:
 *   tap            -> System.aPreset[i].Press  (recall)
 *   hold 3 seconds -> System.aPreset[i].Hold   (store), and the button says "Saved"
 *
 * Each is a 10 ms pulse sent by ControllerService once the gesture is decided,
 * so the control system never has to time the hold itself.
 *
 * The bar that sweeps across the button is a CSS transition driven by the
 * `holding` class, so it runs on the panel's compositor rather than a timer.
 */
@Component({
  standalone: true,
  selector: 'app-presets',
  templateUrl: './presets.component.html',
  styleUrl: './presets.component.scss',
  imports: [CommonModule]
})
export class PresetsComponent implements OnDestroy {
  readonly presets = Array.from({ length: PRESET_COUNT }, (_, i) => i);
  readonly holdSeconds = HOLD_MS / 1000;

  /** Index currently held down, or -1. Drives the sweep animation. */
  holding = -1;

  /** Index showing "Saved", or -1. */
  saved = -1;

  private holdTimer: any = null;
  private savedTimer: any = null;
  private storedThisPress = false;

  constructor(private cs: ControllerService, private zone: NgZone) {}

  ngOnDestroy(): void {
    this.clearHoldTimer();
    if (this.savedTimer) {
      clearTimeout(this.savedTimer);
    }
  }

  label(index: number): string {
    return `Preset ${index + 1}`;
  }

  // -------------------------------------------------------------- press / hold

  onPress(index: number, event: Event) {
    // Keep the browser from turning a long press into a text selection or
    // a context menu on the panel.
    event.preventDefault();

    this.clearHoldTimer();
    this.storedThisPress = false;
    this.holding = index;

    this.holdTimer = setTimeout(() => this.zone.run(() => {
      this.storedThisPress = true;
      this.holding = -1;
      this.cs.storePreset(index);
      this.flashSaved(index);
    }), HOLD_MS);
  }

  /** A release before the timer fires is a plain tap: recall. */
  onRelease(index: number) {
    if (this.holding !== index && !this.storedThisPress) {
      return;
    }

    const stored = this.storedThisPress;
    this.clearHoldTimer();
    this.holding = -1;
    this.storedThisPress = false;

    if (!stored) {
      this.cs.recallPreset(index);
    }
  }

  /** A finger that slides off the button does nothing at all. */
  onCancel() {
    this.clearHoldTimer();
    this.holding = -1;
    this.storedThisPress = false;
  }

  private flashSaved(index: number) {
    this.saved = index;
    if (this.savedTimer) {
      clearTimeout(this.savedTimer);
    }
    this.savedTimer = setTimeout(() => this.zone.run(() => this.saved = -1), SAVED_MS);
  }

  private clearHoldTimer() {
    if (this.holdTimer) {
      clearTimeout(this.holdTimer);
      this.holdTimer = null;
    }
  }

  trackByIndex(index: number): number {
    return index;
  }
}
