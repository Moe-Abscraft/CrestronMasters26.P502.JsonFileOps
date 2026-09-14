import { Component, CUSTOM_ELEMENTS_SCHEMA, NgZone, OnDestroy, OnInit , TemplateRef } from '@angular/core';
import { NgbNavModule, NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  isNotification: boolean = false
  private modelRef!: NgbModalRef;
  private modelVideoRef!: NgbModalRef;
  isShowAudioIncomingCall: boolean = false
  isShowVideoIncomingCall: boolean = false
  autoShuddowntime: { hours: number; minutes: number } = { hours: 12, minutes: 0 };
  constructor() {
    document.addEventListener(
      'contextmenu',
      function (e) {
        e.preventDefault();
      },
      false
    );
  }
  ngOnInit(): void {
    
  }

  
  getTimeDifference() {
    const now = new Date();
    const currentHours = now.getHours();
    const currentMinutes = now.getMinutes();

    // Determine if autoShuddowntime represents a time later today or tomorrow
    let shutdownHours = this.autoShuddowntime.hours;
    if (shutdownHours === 12 && currentHours < 12) {
      // Handle "12" as noon or midnight based on the current time
      shutdownHours = 12;
    } else if (shutdownHours < 12 && shutdownHours <= currentHours) {
      // If the time has passed today, assume it's tomorrow
      shutdownHours += 12;
    } else if (shutdownHours === 12 && currentHours >= 12) {
      shutdownHours = 0;
    }

    // Create the Date object for the next shutdown time
    const shutdownTime = new Date();
    shutdownTime.setHours(shutdownHours);
    shutdownTime.setMinutes(this.autoShuddowntime.minutes);
    shutdownTime.setSeconds(0);

    // If shutdown time is still earlier than now, add 24 hours
    if (shutdownTime.getTime() <= now.getTime()) {
      shutdownTime.setDate(shutdownTime.getDate() + 1);
    }

    // Calculate the difference
    const timeDifferenceMs = shutdownTime.getTime() - now.getTime();
    const diffHours = Math.floor(timeDifferenceMs / (1000 * 60 * 60));
    const diffMinutes = Math.floor((timeDifferenceMs % (1000 * 60 * 60)) / (1000 * 60));

    return { hours: diffHours, minutes: diffMinutes };

  }
}
