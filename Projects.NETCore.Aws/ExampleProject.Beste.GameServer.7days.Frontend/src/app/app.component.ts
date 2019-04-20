import { Component, ViewChild } from '@angular/core';
import { Nav, Platform } from 'ionic-angular';
import { StatusBar } from '@ionic-native/status-bar';
import { SplashScreen } from '@ionic-native/splash-screen';
import { WebsocketProvider } from '../providers/websocket-service';
import { Page, User } from './classes/classes';
import { SdaystdiePage } from '../pages/sdaystdie/sdaystdie';
import { SdaystdieUserPage } from '../pages/sdaystdieUser/sdaystdieUser';
import { LogoutPage } from '../pages/logout/logout';
import { GlobalVarsSDaysTDie } from '../providers/globalVarsSDaysTDie';

@Component({
  templateUrl: 'app.html'
})
export class MyApp {
  @ViewChild(Nav) nav: Nav;

  rootPage: any = SdaystdiePage;

  pages: Array<Page>;
  loggedinUser:User;

  constructor(public platform: Platform, public statusBar: StatusBar, public splashScreen: SplashScreen) {
    this.initializeApp();
    // used for an example of ngFor and navigation
    
    this.pages = [
      { Title: 'Applications', Component: SdaystdiePage, Visible: true,  SubPagesOpen: false, 
        SubPages: [
          { Title: '7 Days 2 Die', Component: SdaystdiePage, Visible: true,  SubPagesOpen: false, SubPages: null },
          { Title: 'Users', Component: SdaystdieUserPage, Visible: true,  SubPagesOpen: false, SubPages: null },
          { Title: 'Logout', Component: LogoutPage, Visible: false,  SubPagesOpen: false, SubPages: null }
        ]
      }
    ];
    GlobalVarsSDaysTDie.userSet.subscribe( user =>
      {
        this.pages.find(p => p.Title == "Applications")
        .SubPages.find(p => p.Title == "Logout").Visible = (user != null);       
      });
  }

  initializeApp() {
    this.platform.ready().then(() => {
      // Okay, so the platform is ready and our plugins are available.
      // Here you can do any higher level native things you might need.
      this.statusBar.styleDefault();
      this.splashScreen.hide();
    });
  }

  openPage(page:Page) {
    if(page.SubPages != null)
    {
      page.SubPagesOpen = !page.SubPagesOpen;
    }
    else
    {
      // Reset the content nav to have just this page
      // we wouldn't want the back button to show in this scenario
      this.nav.setRoot(page.Component);
    }
  }
  hasSubPages(page) {

    return false;
  }
}
