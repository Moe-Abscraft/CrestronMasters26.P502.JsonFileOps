import { AfterViewInit, Component, NgZone, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControllerService } from '../service/controller.service';

/**
 * The warning box. Driven entirely by System.Message: a non-empty string
 * shows it, an empty one takes it away. Dismiss only hides the text the
 * panel is currently showing -- the next different message shows again.
 */
@Component({
  standalone: true,
  selector: 'app-message-box',
  templateUrl: './message-box.component.html',
  styleUrl: './message-box.component.scss',
  imports: [CommonModule]
})
export class MessageBoxComponent implements OnInit, AfterViewInit {
  message = '';
  private dismissed = '';

  constructor(private cs: ControllerService, private zone: NgZone) {}

  ngOnInit(): void {
    this.zone.run(() => this.message = this.cs.message);
  }

  ngAfterViewInit(): void {
    this.cs.messageChanged.subscribe(value => this.zone.run(() => {
      this.message = value;
      if (value !== this.dismissed) {
        this.dismissed = '';
      }
    }));
  }

  get visible(): boolean {
    return this.message.length > 0 && this.message !== this.dismissed;
  }

  dismiss() {
    this.dismissed = this.message;
  }
}
