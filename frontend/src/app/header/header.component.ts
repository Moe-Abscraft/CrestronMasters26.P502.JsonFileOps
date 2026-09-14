import { AfterViewInit, Component, NgZone, OnInit } from '@angular/core';
import { ControllerService } from '../service/controller.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-header',
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss'
})
export class HeaderComponent implements OnInit, AfterViewInit{
  connected = false;
  page = 1;
  mode = "";
  lastUpdatedTime = '';

  constructor(private cs: ControllerService, private zone: NgZone, private router: Router) {

  }

  ngOnInit(): void {
    this.zone.run(() => {
      this.connected = this.cs.connected;
      this.page = this.cs.page;
      this.lastUpdatedTime = this.cs.lastUpdatedTime;

      this.setRoute(this.page);
    });
  }

  ngAfterViewInit(): void {
    this.cs.connectedChanged.subscribe(value => this.zone.run(() => this.connected = value));
    this.cs.lastUpdatedTimeChanged.subscribe(value =>
      this.zone.run(() => this.lastUpdatedTime = value));
    this.cs.pageChanged.subscribe(value => this.zone.run(()=> {
      this.page = value;
      if(this.page === 1) this.mode = "Config Reader";
      if(this.page === 2) this.mode = "Config Writer";
      if(this.page === 3) this.mode = "C# Wrapper";
      if(this.page === 4) this.mode = "GatherAync";
      if(this.page === 5) this.mode = "EISC";

      this.setRoute(this.page);
    }));
  }

  setRoute(mode: number) {
    switch(mode) {
      case 1:
        this.router.navigate(['configfile']);
        break;
      case 2:
        this.router.navigate(['logfile']);
        break;
      case 3:
        this.router.navigate(['csharpwrapper']);
        break;
      case 4: 
        this.router.navigate(['gatherasync']);
        break;
      case 5: 
        this.router.navigate(['eisc']);
        break;
    }
  }

  /** Pulse the ReloadConfig contract event; the time field updates when the
   *  control system pushes a new LastUpdatedTime back. */
  reloadConfig() {
    this.cs.reloadConfig();
  }

  separate() {
    this.cs.pulseDigital('1');
  }

  combine() {
    this.cs.pulseDigital('2');
  }

}
