import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';

import { AppModule } from './app.module';
import { NavController } from 'ionic-angular';
import { WebsocketProvider } from '../providers/websocket-service';

platformBrowserDynamic().bootstrapModule(AppModule);


export class Main {
    impressumVisible:boolean = false;
    constructor(public navCtrl:NavController, websocket:WebsocketProvider) {
      websocket.createNewWebSocket();
      
      //this.socket = websocket.getInstance();
    }
    ionViewDidLoad() {
      
    }
  
    toggleImpressum() {
        this.impressumVisible = !this.impressumVisible;
    }
  }