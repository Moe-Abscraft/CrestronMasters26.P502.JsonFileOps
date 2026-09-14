import { NgModule, NgZone } from '@angular/core';
import { RouterModule, Routes, Router, PreloadAllModules } from '@angular/router';
import { StartPageComponent } from './start-page/start-page.component';


const routes: Routes = [
  { path: '', component: StartPageComponent },
  { path: 'configfile', component: StartPageComponent }];

@NgModule({
  imports: [RouterModule.forRoot(routes, { useHash: true, preloadingStrategy: PreloadAllModules, initialNavigation: 'disabled' })],
  exports: [RouterModule],
})
export class AppRoutingModule {

  public constructor(private router: Router, private ngZone: NgZone) {
    this.navigate('');
    console.log("router . . .")
  }

  public navigate(path: string) {
    this.ngZone.run(() =>
      this.router.navigate([path], { skipLocationChange: true })
    );
  }
}
