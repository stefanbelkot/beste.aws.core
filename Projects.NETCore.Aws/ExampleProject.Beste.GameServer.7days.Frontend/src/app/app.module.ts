import { BrowserModule } from '@angular/platform-browser';
import { ErrorHandler, NgModule } from '@angular/core';
import { IonicApp, IonicErrorHandler, IonicModule } from 'ionic-angular';

import { MyApp } from './app.component';

import { StatusBar } from '@ionic-native/status-bar';
import { SplashScreen } from '@ionic-native/splash-screen';
import { WebsocketProvider } from '../providers/websocket-service';
import { QuillModule } from 'ngx-quill' 
import { SdaystdiePage } from '../pages/sdaystdie/sdaystdie';
import { ChangePasswordPage } from '../modals/change-password/change-password';
import { SdaystdieUserPage } from '../pages/sdaystdieUser/sdaystdieUser';
import { LogoutPage } from '../pages/logout/logout';
import { ChartsModule } from 'ng2-charts';

@NgModule({
  declarations: [
    MyApp,
    SdaystdiePage,
    SdaystdieUserPage,
    ChangePasswordPage,
    LogoutPage
  ],
  imports: [
    BrowserModule,
    IonicModule.forRoot(MyApp),
    QuillModule,
    ChartsModule
  ],
  bootstrap: [IonicApp],
  entryComponents: [
    MyApp,
    SdaystdiePage,
    SdaystdieUserPage,
    ChangePasswordPage,
    LogoutPage
  ],
  providers: [
    StatusBar,
    SplashScreen,
    {provide: ErrorHandler, useClass: IonicErrorHandler},
    WebsocketProvider,
    
  ]
})
export class AppModule {}
