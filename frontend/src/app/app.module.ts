import { APP_INITIALIZER, CUSTOM_ELEMENTS_SCHEMA, NgModule, NgZone } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { getWebXPanel, runsInContainerApp } from '@crestron/ch5-webxpanel';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { StartPageComponent } from './start-page/start-page.component';
import { HttpClientModule } from '@angular/common/http';
import { APP_BASE_HREF } from '@angular/common';
import { HeaderComponent } from './header/header.component';
import { ControllerService } from './service/controller.service';
import { FormsModule } from '@angular/forms';

const {
  isActive,
  WebXPanel,
  WebXPanelConfigParams,
  WebXPanelEvents,
  getVersion,
  getBuildDate,
} = getWebXPanel(!runsInContainerApp());

const configuration = {
  host: '192.168.2.21',
  ipId: '30',
  roomId: 'SNT'
};

const webXPanelFactory = () => () => {
  if (!isActive) {
    WebXPanel.initialize(configuration);
  }
};

console.log(`Crestron WebXPanel version: ${getVersion()}`);
console.log(`Crestron WebXPanel build date: ${getBuildDate()}`);

@NgModule({
  declarations: [AppComponent, HeaderComponent],
  imports: [
    BrowserModule,
    AppRoutingModule,
    BrowserAnimationsModule,
    NgbModule,
    StartPageComponent,
    FormsModule,
    HttpClientModule
  ],
  providers: [
    { provide: APP_BASE_HREF, useValue: './' },
    { provide: APP_INITIALIZER, useFactory: webXPanelFactory, multi: true }
  ],
  bootstrap: [AppComponent],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
})
export class AppModule {
  constructor(private ngZone: NgZone, private cs: ControllerService) {
    WebXPanel.addEventListener(
      WebXPanelEvents.NOT_AUTHORIZED,
      ({ detail }: any) => {
        console.log('Crestron WebXPanel Not authorized: ', detail);
        window.location.href = detail.redirectTo;
        this.cs.connectedChanged.next(false);
      }
    );

    WebXPanel.addEventListener(
      WebXPanelEvents.CONNECT_CIP,
      ({ detail }: any) => {
        var { url, ipId, roomId } = detail;
        console.log(
          `Crestron XPanel Connected to ${url}, 0x${ipId
            .toString(16)
            .padStart(2, '0')}, ${roomId}`
        );
        this.cs.connectedChanged.next(true);
      }
    );

    WebXPanel.addEventListener(
      WebXPanelEvents.DISCONNECT_CIP,
      ({ detail }: any) => {
        var { reason } = detail;
        console.log(`Crestron XPanel Disconnected from CIP. Reason: ${reason}`);
        this.cs.connectedChanged.next(false);
      }
    );

    WebXPanel.addEventListener(WebXPanelEvents.ERROR_WS, ({ detail }: any) => {
      var { reason } = detail;
      console.log(`Crestron XPanel Websocket Error: ${reason}`);
      this.cs.connectedChanged.next(false);
    });

    if (isActive) {
      var entries = this.GetQueryParameters();

      WebXPanelConfigParams.host = entries['host'] ?? '';
      WebXPanelConfigParams.ipId = (entries['ipid'] ?? '').toLowerCase();
      WebXPanelConfigParams.roomId = entries['roomid'] ?? '';
      WebXPanelConfigParams.tokenSource = entries['tokensource'];
      WebXPanelConfigParams.tokenUrl = entries['tokenurl'] ?? '';
      WebXPanelConfigParams.authToken = entries['authtoken'] ?? 'eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpZCI6ImVlNDBlMzY0LTVkNGUtNDRkOC1hMDczLTlkZGE0Yzc0ZjM5MSIsImx2IjoiRGVmYXVsdCBMZXZlbCIsInZlciI6IjEuMCIsImV4cGkiOiIwIn0.8UoiTQjskcWZV0PmByEZ2TTrvY0FWJqg8FbkNvaxrls';
       
      console.log(
        'Crestron WebXPanelConfigParams: ' +
          JSON.stringify(WebXPanelConfigParams)
      );
     
      this.ngZone.runOutsideAngular(() =>
        WebXPanel.initialize(WebXPanelConfigParams)
      );
    }
  }

  GetQueryParameters() {
    var url = new URL(window.location.href);
    var urlParameters: any = new URLSearchParams(url.search);
    var queries: any = {};
    for (var [key, value] of urlParameters) {
      queries[key.toLowerCase()] = value;
    }
    return queries;
  }
}
